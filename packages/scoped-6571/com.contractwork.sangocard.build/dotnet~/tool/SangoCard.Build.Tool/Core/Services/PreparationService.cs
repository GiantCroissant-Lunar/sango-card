using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using System.IO.Compression;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Executes build preparation according to a PreparationConfig.
/// Includes backup/restore, rollback, dry-run, and validation support.
/// </summary>
public class PreparationService
{
    private readonly PathResolver _paths;
    private readonly ValidationService _validation;
    private readonly IEnumerable<IPatcher> _patchers;
    private readonly ILogger<PreparationService> _logger;
    private readonly IPublisher<PreparationStartedMessage> _prepStarted;
    private readonly IPublisher<PreparationCompletedMessage> _prepCompleted;
    private readonly IPublisher<FileCopiedMessage> _fileCopied;
    private readonly IPublisher<FileMovedMessage> _fileMoved;
    private readonly IPublisher<FileDeletedMessage> _fileDeleted;

    private string? _backupPath;
    private readonly List<string> _modifiedFiles = new();

    public PreparationService(
        PathResolver paths,
        ValidationService validation,
        IEnumerable<IPatcher> patchers,
        ILogger<PreparationService> logger,
        IPublisher<PreparationStartedMessage> prepStarted,
        IPublisher<PreparationCompletedMessage> prepCompleted,
        IPublisher<FileCopiedMessage> fileCopied,
        IPublisher<FileMovedMessage> fileMoved,
        IPublisher<FileDeletedMessage> fileDeleted)
    {
        _paths = paths;
        _validation = validation;
        _patchers = patchers;
        _logger = logger;
        _prepStarted = prepStarted;
        _prepCompleted = prepCompleted;
        _fileCopied = fileCopied;
        _fileMoved = fileMoved;
        _fileDeleted = fileDeleted;
    }

    /// <summary>
    /// Executes preparation with validation, backup, and error handling.
    /// </summary>
    /// <param name="config">Preparation configuration.</param>
    /// <param name="configRelativePath">Optional relative path to config file.</param>
    /// <param name="dryRun">If true, simulates changes without modifying files.</param>
    /// <param name="validate">If true, validates config before execution.</param>
    /// <returns>Preparation result summary.</returns>
    /// <summary>
    /// Execute injection for a specific stage from a multi-stage config (v2.0).
    /// </summary>
    public async Task<PreparationCompletedMessage> ExecuteStageAsync(
        InjectionStage stage,
        string? platform = null,
        string? configRelativePath = null,
        bool dryRun = false)
    {
        _logger.LogInformation("Stage '{StageName}' injection started{DryRun}", stage.Name, dryRun ? " (DRY-RUN)" : "");
        _prepStarted.Publish(new PreparationStartedMessage(configRelativePath, "client", dryRun));

        try
        {
            // Step 1: Create backup (unless dry-run)
            if (!dryRun)
            {
                await CreateBackupForStageAsync(stage);
            }

            // Step 2: Execute stage injection
            var result = await RunStageAsync(stage, platform, dryRun);

            // Step 3: Clean up backup on success (unless dry-run)
            if (!dryRun && _backupPath != null)
            {
                CleanupBackup();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stage '{StageName}' injection failed: {Message}", stage.Name, ex.Message);

            // Rollback changes if not dry-run
            if (!dryRun && _backupPath != null)
            {
                await RollbackAsync();
            }

            throw;
        }
    }

    public async Task<PreparationCompletedMessage> ExecuteAsync(
        PreparationConfig config,
        string? configRelativePath = null,
        bool dryRun = false,
        bool validate = true)
    {
        _logger.LogInformation("Preparation started{DryRun}", dryRun ? " (DRY-RUN)" : "");
        _prepStarted.Publish(new PreparationStartedMessage(configRelativePath, "client", dryRun));

        try
        {
            // Step 1: Validate configuration if requested
            if (validate)
            {
                _logger.LogInformation("Validating configuration...");
                var validationResult = _validation.Validate(config, ValidationLevel.Full);

                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Configuration validation failed: {validationResult.Summary}");
                }

                _logger.LogInformation("Validation passed");
            }

            // Step 2: Create backup (unless dry-run)
            if (!dryRun)
            {
                await CreateBackupAsync(config);
            }

            // Step 3: Execute preparation
            var result = await RunAsync(config, configRelativePath, dryRun);

            // Step 4: Clean up backup on success (unless dry-run)
            if (!dryRun && _backupPath != null)
            {
                CleanupBackup();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preparation failed: {Message}", ex.Message);

            // Rollback changes if not dry-run
            if (!dryRun && _backupPath != null)
            {
                await RollbackAsync();
            }

            throw;
        }
    }

    /// <summary>
    /// Restores files from the most recent backup.
    /// </summary>
    /// <param name="backupPath">Optional specific backup path. If null, uses the last backup.</param>
    public async Task RestoreAsync(string? backupPath = null)
    {
        var restorePath = backupPath ?? _backupPath;

        if (string.IsNullOrEmpty(restorePath) || !Directory.Exists(restorePath))
        {
            throw new InvalidOperationException("No backup found to restore");
        }

        _logger.LogInformation("Restoring from backup: {BackupPath}", restorePath);

        try
        {
            // Extract backup archive
            var tempDir = Path.Combine(Path.GetTempPath(), $"restore_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            var archivePath = Path.Combine(restorePath, "backup.zip");
            if (File.Exists(archivePath))
            {
                ZipFile.ExtractToDirectory(archivePath, tempDir, overwriteFiles: true);
            }

            // Restore each file
            var gitRoot = _paths.GitRoot;
            foreach (var file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(tempDir, file);
                var targetPath = Path.Combine(gitRoot, relativePath);

                EnsureDirectoryOf(targetPath);
                File.Copy(file, targetPath, overwrite: true);

                _logger.LogDebug("Restored: {File}", relativePath);
            }

            // Clean up temp directory
            Directory.Delete(tempDir, recursive: true);

            _logger.LogInformation("Restore completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore failed: {Message}", ex.Message);
            throw;
        }
    }
    /// <summary>
    /// Runs preparation steps as defined by the config (internal implementation).
    /// </summary>
    private async Task<PreparationCompletedMessage> RunAsync(
        PreparationConfig config,
        string? configRelativePath = null,
        bool dryRun = false)
    {
        _modifiedFiles.Clear();

        var copied = 0;
        var moved = 0;
        var deleted = 0;
        var patched = 0;

        // 1) Copy Unity packages (supports both files and folders)
        foreach (var pkg in config.Packages)
        {
            var src = ResolveCacheSource(pkg.Source);
            var dst = _paths.Resolve(pkg.Target);

            if (!dryRun)
            {
                // Support both files and directories
                if (Directory.Exists(src))
                {
                    // Copy directory recursively
                    CopyDirectory(src, dst, overwrite: true);
                    _modifiedFiles.Add(pkg.Target);
                }
                else if (File.Exists(src))
                {
                    // Copy single file
                    EnsureDirectoryOf(dst);
                    File.Copy(src, dst, overwrite: true);
                    _modifiedFiles.Add(pkg.Target);
                }
                else
                {
                    throw new FileNotFoundException($"Package source not found: {src}");
                }
            }

            copied++;
            var sourceType = Directory.Exists(src) ? "folder" : "file";
            _logger.LogInformation("{Action}Copied package ({Type}): {Name} -> {Target}",
                dryRun ? "[DRY-RUN] " : "", sourceType, pkg.Name, pkg.Target);
            _fileCopied.Publish(new FileCopiedMessage(pkg.Source, pkg.Target));
        }

        // 2) Copy assemblies (supports both files and folders)
        foreach (var asm in config.Assemblies)
        {
            var src = ResolveCacheSource(asm.Source);
            var dst = _paths.Resolve(asm.Target);

            if (!dryRun)
            {
                // Support both files and directories
                if (Directory.Exists(src))
                {
                    // Copy directory recursively
                    CopyDirectory(src, dst, overwrite: true);
                    _modifiedFiles.Add(asm.Target);
                }
                else if (File.Exists(src))
                {
                    // Copy single file
                    EnsureDirectoryOf(dst);
                    File.Copy(src, dst, overwrite: true);
                    _modifiedFiles.Add(asm.Target);
                }
                else
                {
                    throw new FileNotFoundException($"Assembly source not found: {src}");
                }
            }

            copied++;
            var sourceType = Directory.Exists(src) ? "folder" : "file";
            _logger.LogInformation("{Action}Copied assembly ({Type}): {Name} -> {Target}",
                dryRun ? "[DRY-RUN] " : "", sourceType, asm.Name, asm.Target);
            _fileCopied.Publish(new FileCopiedMessage(asm.Source, asm.Target));
        }

        // 3) Asset manipulations
        foreach (var op in config.AssetManipulations)
        {
            switch (op.Operation)
            {
                case AssetOperation.Copy:
                    if (string.IsNullOrWhiteSpace(op.Source))
                        throw new ArgumentException("Copy operation requires Source", nameof(op.Source));
                    if (!dryRun)
                    {
                        CopyPath(op.Source!, op.Target, op.Overwrite);
                        _modifiedFiles.Add(op.Target);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY-RUN] Would copy: {Source} -> {Target}", op.Source, op.Target);
                        _fileCopied.Publish(new FileCopiedMessage(op.Source!, op.Target));
                    }
                    copied++;
                    break;

                case AssetOperation.Move:
                    if (string.IsNullOrWhiteSpace(op.Source))
                        throw new ArgumentException("Move operation requires Source", nameof(op.Source));
                    if (!dryRun)
                    {
                        MovePath(op.Source!, op.Target, op.Overwrite);
                        _modifiedFiles.Add(op.Target);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY-RUN] Would move: {Source} -> {Target}", op.Source, op.Target);
                        _fileMoved.Publish(new FileMovedMessage(op.Source!, op.Target));
                    }
                    moved++;
                    break;

                case AssetOperation.Delete:
                    if (!dryRun)
                    {
                        DeletePath(op.Target);
                    }
                    else
                    {
                        _logger.LogInformation("[DRY-RUN] Would delete: {Target}", op.Target);
                        _fileDeleted.Publish(new FileDeletedMessage(op.Target));
                    }
                    deleted++;
                    break;
            }
        }

        // 4) Apply code patches
        foreach (var patch in config.CodePatches)
        {
            var targetAbs = _paths.Resolve(patch.File);
            if (!_paths.FileExists(patch.File))
            {
                if (patch.Optional)
                {
                    _logger.LogWarning("Optional patch skipped, file not found: {File}", patch.File);
                    continue;
                }
                else
                {
                    throw new FileNotFoundException($"Patch target not found: {targetAbs}");
                }
            }

            var patcher = _patchers.FirstOrDefault(p => p.PatchType == patch.Type);
            if (patcher == null)
            {
                _logger.LogWarning("No patcher registered for type: {Type}", patch.Type);
                continue;
            }

            var validation = await patcher.ValidatePatchAsync(targetAbs, patch);
            if (!validation.TargetFound && !patch.Optional)
            {
                _logger.LogWarning("Patch search did not match target, skipping: {File}", patch.File);
                continue;
            }

            var result = await patcher.ApplyPatchAsync(targetAbs, patch, dryRun);
            if (result.Success && result.Modified)
            {
                patched++;
                if (!dryRun)
                {
                    _modifiedFiles.Add(patch.File);
                }
            }
            else if (!result.Success)
            {
                _logger.LogError("Failed to apply patch to {File}: {Message}", patch.File, result.Message);
            }
        }

        // 5) Scripting define symbols (no-op placeholder)
        if (config.ScriptingDefineSymbols != null)
        {
            _logger.LogInformation("{Action}Scripting define symbols requested: +{Add} -{Remove} (platform: {Platform}, clear: {Clear})",
                dryRun ? "[DRY-RUN] " : "",
                string.Join(',', config.ScriptingDefineSymbols.Add ?? new()),
                string.Join(',', config.ScriptingDefineSymbols.Remove ?? new()),
                config.ScriptingDefineSymbols.Platform,
                config.ScriptingDefineSymbols.ClearExisting);
        }

        var summary = new PreparationCompletedMessage(copied, moved, deleted, patched, TimeSpan.Zero, null);
        _prepCompleted.Publish(summary);
        _logger.LogInformation("Preparation complete: copied={Copied}, moved={Moved}, deleted={Deleted}, patched={Patched}",
            copied, moved, deleted, patched);
        return summary;
    }

    /// <summary>
    /// Creates a backup of all files that will be modified.
    /// </summary>
    private Task CreateBackupAsync(PreparationConfig config)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        _backupPath = Path.Combine(Path.GetTempPath(), $"build_prep_backup_{timestamp}");
        Directory.CreateDirectory(_backupPath);

        _logger.LogInformation("Creating backup at: {BackupPath}", _backupPath);

        var gitRoot = _paths.GitRoot;
        var backupFiles = new List<string>();

        // Collect all files that will be modified
        foreach (var pkg in config.Packages)
        {
            if (_paths.FileExists(pkg.Target))
            {
                backupFiles.Add(pkg.Target);
            }
        }

        foreach (var asm in config.Assemblies)
        {
            if (_paths.FileExists(asm.Target))
            {
                backupFiles.Add(asm.Target);
            }
        }

        foreach (var op in config.AssetManipulations)
        {
            if (_paths.FileExists(op.Target))
            {
                backupFiles.Add(op.Target);
            }
        }

        foreach (var patch in config.CodePatches)
        {
            if (_paths.FileExists(patch.File))
            {
                backupFiles.Add(patch.File);
            }
        }

        // Create backup archive
        var archivePath = Path.Combine(_backupPath, "backup.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            foreach (var fileRelative in backupFiles.Distinct())
            {
                var absolutePath = _paths.Resolve(fileRelative);
                if (File.Exists(absolutePath))
                {
                    archive.CreateEntryFromFile(absolutePath, fileRelative);
                    _logger.LogDebug("Backed up: {File}", fileRelative);
                }
            }
        }

        _logger.LogInformation("Backup created: {Count} files", backupFiles.Distinct().Count());
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rolls back changes using the backup.
    /// </summary>
    private async Task RollbackAsync()
    {
        if (string.IsNullOrEmpty(_backupPath))
        {
            _logger.LogWarning("No backup path set, cannot rollback");
            return;
        }

        _logger.LogWarning("Rolling back changes...");

        try
        {
            await RestoreAsync(_backupPath);
            _logger.LogInformation("Rollback completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Cleans up the backup directory.
    /// </summary>
    private void CleanupBackup()
    {
        if (string.IsNullOrEmpty(_backupPath) || !Directory.Exists(_backupPath))
        {
            return;
        }

        try
        {
            Directory.Delete(_backupPath, recursive: true);
            _logger.LogDebug("Backup cleaned up: {BackupPath}", _backupPath);
            _backupPath = null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup backup: {Message}", ex.Message);
        }
    }

    private void EnsureDirectoryOf(string absoluteFilePath)
    {
        var dir = Path.GetDirectoryName(absoluteFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
    {
        // Create target directory if it doesn't exist
        Directory.CreateDirectory(targetDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, overwrite);
        }

        // Recursively copy subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(subDir, targetSubDir, overwrite);
        }
    }

    private void CopyPath(string sourceRelative, string targetRelative, bool overwrite)
    {
        var src = _paths.Resolve(sourceRelative);
        var dst = _paths.Resolve(targetRelative);
        if (Directory.Exists(src))
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(src, file);
                var destFile = Path.Combine(dst, rel);
                EnsureDirectoryOf(destFile);
                File.Copy(file, destFile, overwrite);
            }
        }
        else
        {
            EnsureDirectoryOf(dst);
            File.Copy(src, dst, overwrite);
        }
        _fileCopied.Publish(new FileCopiedMessage(sourceRelative, targetRelative));
    }

    private void MovePath(string sourceRelative, string targetRelative, bool overwrite)
    {
        var src = _paths.Resolve(sourceRelative);
        var dst = _paths.Resolve(targetRelative);
        EnsureDirectoryOf(dst);
        if (File.Exists(dst) && overwrite)
        {
            File.Delete(dst);
        }
        File.Move(src, dst);
        _fileMoved.Publish(new FileMovedMessage(sourceRelative, targetRelative));
    }

    private void DeletePath(string targetRelative)
    {
        var dst = _paths.Resolve(targetRelative);
        if (Directory.Exists(dst))
        {
            Directory.Delete(dst, recursive: true);
        }
        else if (File.Exists(dst))
        {
            File.Delete(dst);
        }
        _fileDeleted.Publish(new FileDeletedMessage(targetRelative));
    }

    /// <summary>
    /// Resolves a cache source path with hash-tolerance.
    /// For paths like "build/preparation/cache/com.cysharp.unitask",
    /// this will match "build/preparation/cache/com.cysharp.unitask@15a4a7657f99".
    /// </summary>
    /// <param name="sourcePath">The source path from config.</param>
    /// <returns>The resolved absolute path, with hash suffix if needed.</returns>
    private string ResolveCacheSource(string sourcePath)
    {
        // First try exact match
        var exactPath = _paths.Resolve(sourcePath);
        if (File.Exists(exactPath) || Directory.Exists(exactPath))
        {
            return exactPath;
        }

        // If not found, look for cache folders with hash suffixes
        var normalizedPath = sourcePath.Replace('\\', '/');
        var lastSlash = normalizedPath.LastIndexOf('/');
        if (lastSlash == -1)
        {
            return exactPath; // Return exact path if no directory separator
        }

        var parentDir = normalizedPath.Substring(0, lastSlash);
        var baseName = normalizedPath.Substring(lastSlash + 1);

        // Check if parent directory exists
        if (!_paths.DirectoryExists(parentDir))
        {
            return exactPath; // Return exact path if parent doesn't exist
        }

        // Get the absolute parent path
        var absoluteParent = _paths.Resolve(parentDir);

        // Find folders that match the base name with optional @hash suffix
        try
        {
            var matchingFolders = Directory.GetDirectories(absoluteParent)
                .Where(dir =>
                {
                    var dirName = Path.GetFileName(dir);
                    // Match exact name or name with @hash suffix
                    return dirName == baseName ||
                           (dirName.StartsWith(baseName + "@") && dirName.Length > baseName.Length + 1);
                })
                .ToList();

            if (matchingFolders.Count > 0)
            {
                // Return the first match (should only be one in well-formed cache)
                return matchingFolders[0];
            }
        }
        catch
        {
            // Ignore errors and fall through to return exact path
        }

        // No match found, return exact path (will fail later with clear error)
        return exactPath;
    }

    /// <summary>
    /// Run stage injection (v2.0 multi-stage workflow).
    /// </summary>
    private async Task<PreparationCompletedMessage> RunStageAsync(
        InjectionStage stage,
        string? platform = null,
        bool dryRun = false)
    {
        _modifiedFiles.Clear();

        var copied = 0;
        var moved = 0;
        var deleted = 0;
        var patched = 0;

        // 1) Copy Unity packages (if defined in stage)
        if (stage.Packages != null)
        {
            foreach (var pkg in stage.Packages)
            {
                var src = ResolveCacheSource(pkg.Source);
                var dst = _paths.Resolve(pkg.Target);

                if (!dryRun)
                {
                    if (Directory.Exists(src))
                    {
                        CopyDirectory(src, dst, overwrite: true);
                        _modifiedFiles.Add(pkg.Target);
                    }
                    else if (File.Exists(src))
                    {
                        EnsureDirectoryOf(dst);
                        File.Copy(src, dst, overwrite: true);
                        _modifiedFiles.Add(pkg.Target);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Package source not found: {src}");
                    }
                }

                copied++;
                var sourceType = Directory.Exists(src) ? "folder" : "file";
                _logger.LogInformation("{Action}Copied package ({Type}): {Name} -> {Target}",
                    dryRun ? "[DRY-RUN] " : "", sourceType, pkg.Name, pkg.Target);
                _fileCopied.Publish(new FileCopiedMessage(pkg.Source, pkg.Target));
            }
        }

        // 2) Copy assemblies (if defined in stage)
        if (stage.Assemblies != null)
        {
            foreach (var asm in stage.Assemblies)
            {
                var src = ResolveCacheSource(asm.Source);
                var dst = _paths.Resolve(asm.Target);

                if (!dryRun)
                {
                    if (Directory.Exists(src))
                    {
                        CopyDirectory(src, dst, overwrite: true);
                        _modifiedFiles.Add(asm.Target);
                    }
                    else if (File.Exists(src))
                    {
                        EnsureDirectoryOf(dst);
                        File.Copy(src, dst, overwrite: true);
                        _modifiedFiles.Add(asm.Target);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Assembly source not found: {src}");
                    }
                }

                copied++;
                _logger.LogInformation("{Action}Copied assembly: {Name} -> {Target}",
                    dryRun ? "[DRY-RUN] " : "", asm.Name, asm.Target);
                _fileCopied.Publish(new FileCopiedMessage(asm.Source, asm.Target));
            }
        }

        // 3) Perform asset manipulations (if defined in stage)
        if (stage.AssetManipulations != null)
        {
            foreach (var op in stage.AssetManipulations)
            {
                switch (op.Operation)
                {
                    case AssetOperation.Copy:
                        if (string.IsNullOrWhiteSpace(op.Source))
                            throw new ArgumentException("Copy operation requires Source", nameof(op.Source));
                        if (!dryRun)
                        {
                            CopyPath(op.Source!, op.Target, op.Overwrite);
                        }
                        copied++;
                        _logger.LogInformation("{Action}Copied: {Source} -> {Target}",
                            dryRun ? "[DRY-RUN] " : "", op.Source, op.Target);
                        break;

                    case AssetOperation.Move:
                        if (string.IsNullOrWhiteSpace(op.Source))
                            throw new ArgumentException("Move operation requires Source", nameof(op.Source));
                        if (!dryRun)
                        {
                            MovePath(op.Source!, op.Target, op.Overwrite);
                        }
                        moved++;
                        _logger.LogInformation("{Action}Moved: {Source} -> {Target}",
                            dryRun ? "[DRY-RUN] " : "", op.Source, op.Target);
                        break;

                    case AssetOperation.Delete:
                        if (!dryRun)
                        {
                            DeletePath(op.Target);
                        }
                        deleted++;
                        _logger.LogInformation("{Action}Deleted: {Target}",
                            dryRun ? "[DRY-RUN] " : "", op.Target);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown asset operation: {op.Operation}");
                }
            }
        }

        // 4) Apply code patches (if defined in stage)
        if (stage.CodePatches != null)
        {
            foreach (var patch in stage.CodePatches)
            {
                var patcher = _patchers.FirstOrDefault(p => p.PatchType == patch.Type);
                if (patcher == null)
                {
                    _logger.LogWarning("No patcher found for type {PatchType} (file: {File})", patch.Type, patch.File);
                    continue;
                }

                var filePath = _paths.Resolve(patch.File);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Patch target file not found: {File}", patch.File);
                    continue;
                }

                if (!dryRun)
                {
                    var patchResult = await patcher.ApplyPatchAsync(filePath, patch, dryRun: false);
                    if (!patchResult.Success)
                    {
                        _logger.LogWarning("Patch failed for {File}: {Message}", patch.File, patchResult.Message);
                        if (!patch.Optional)
                        {
                            throw new InvalidOperationException($"Required patch failed for {patch.File}: {patchResult.Message}");
                        }
                    }
                    else
                    {
                        _modifiedFiles.Add(patch.File);
                    }
                }

                patched++;
                _logger.LogInformation("{Action}Patched: {File}",
                    dryRun ? "[DRY-RUN] " : "", patch.File);
            }
        }

        _logger.LogInformation("Stage '{StageName}' injection complete: {Copied} copied, {Moved} moved, {Deleted} deleted, {Patched} patched",
            stage.Name, copied, moved, deleted, patched);

        var result = new PreparationCompletedMessage(copied, moved, deleted, patched, TimeSpan.Zero, null);
        _prepCompleted.Publish(result);
        return await Task.FromResult(result);
    }

    /// <summary>
    /// Create backup for stage-specific files.
    /// </summary>
    private async Task CreateBackupForStageAsync(InjectionStage stage)
    {
        _backupPath = Path.Combine(_paths.GitRoot, "build", ".backup", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(_backupPath);

        _logger.LogInformation("Creating backup: {BackupPath}", _backupPath);

        var filesToBackup = new List<string>();

        // Collect files that will be modified
        if (stage.Packages != null)
        {
            foreach (var pkg in stage.Packages)
            {
                var dst = _paths.Resolve(pkg.Target);
                if (Directory.Exists(dst) || File.Exists(dst))
                {
                    filesToBackup.Add(pkg.Target);
                }
            }
        }

        if (stage.Assemblies != null)
        {
            foreach (var asm in stage.Assemblies)
            {
                var dst = _paths.Resolve(asm.Target);
                if (Directory.Exists(dst) || File.Exists(dst))
                {
                    filesToBackup.Add(asm.Target);
                }
            }
        }

        if (stage.AssetManipulations != null)
        {
            foreach (var op in stage.AssetManipulations)
            {
                // For delete/move, backup the target
                var dst = _paths.Resolve(op.Target);
                if (Directory.Exists(dst) || File.Exists(dst))
                {
                    filesToBackup.Add(op.Target);
                }
            }
        }

        if (stage.CodePatches != null)
        {
            foreach (var patch in stage.CodePatches)
            {
                var filePath = _paths.Resolve(patch.File);
                if (File.Exists(filePath))
                {
                    filesToBackup.Add(patch.File);
                }
            }
        }

        // Create backup archive
        if (filesToBackup.Count > 0)
        {
            var archivePath = Path.Combine(_backupPath, "backup.zip");
            using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
            {
                foreach (var fileRelative in filesToBackup.Distinct())
                {
                    var fullPath = _paths.Resolve(fileRelative);
                    if (File.Exists(fullPath))
                    {
                        archive.CreateEntryFromFile(fullPath, fileRelative.Replace('\\', '/'));
                        _logger.LogDebug("Backed up: {File}", fileRelative);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        // Backup directory contents
                        foreach (var file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                        {
                            var relativePath = Path.GetRelativePath(_paths.GitRoot, file);
                            archive.CreateEntryFromFile(file, relativePath.Replace('\\', '/'));
                        }
                    }
                }
            }

            _logger.LogInformation("Backup created: {Count} files", filesToBackup.Count);
        }

        await Task.CompletedTask;
    }
}
