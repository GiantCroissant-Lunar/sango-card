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
    public async Task<PreparationCompletedMessage> ExecuteAsync(
        PreparationConfig config,
        string? configRelativePath = null,
        bool dryRun = false,
        bool validate = true)
    {
        _logger.LogInformation("Preparation started{DryRun}", dryRun ? " (DRY-RUN)" : "");
        _prepStarted.Publish(new PreparationStartedMessage(configRelativePath));

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

        // 1) Copy Unity packages
        foreach (var pkg in config.Packages)
        {
            var src = _paths.Resolve(pkg.Source);
            var dst = _paths.Resolve(pkg.Target);

            if (!dryRun)
            {
                EnsureDirectoryOf(dst);
                File.Copy(src, dst, overwrite: true);
                _modifiedFiles.Add(pkg.Target);
            }

            copied++;
            _logger.LogInformation("{Action}Copied package: {Name} -> {Target}",
                dryRun ? "[DRY-RUN] " : "", pkg.Name, pkg.Target);
            _fileCopied.Publish(new FileCopiedMessage(pkg.Source, pkg.Target));
        }

        // 2) Copy assemblies
        foreach (var asm in config.Assemblies)
        {
            var src = _paths.Resolve(asm.Source);
            var dst = _paths.Resolve(asm.Target);

            if (!dryRun)
            {
                EnsureDirectoryOf(dst);
                File.Copy(src, dst, overwrite: true);
                _modifiedFiles.Add(asm.Target);
            }

            copied++;
            _logger.LogInformation("{Action}Copied assembly: {Name} -> {Target}",
                dryRun ? "[DRY-RUN] " : "", asm.Name, asm.Target);
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

        var summary = new PreparationCompletedMessage(copied, moved, deleted, patched);
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
}
