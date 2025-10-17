using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Executes build preparation according to a PreparationConfig.
/// </summary>
public class PreparationService
{
    private readonly PathResolver _paths;
    private readonly IEnumerable<IPatcher> _patchers;
    private readonly ILogger<PreparationService> _logger;
    private readonly IPublisher<PreparationStartedMessage> _prepStarted;
    private readonly IPublisher<PreparationCompletedMessage> _prepCompleted;
    private readonly IPublisher<FileCopiedMessage> _fileCopied;
    private readonly IPublisher<FileMovedMessage> _fileMoved;
    private readonly IPublisher<FileDeletedMessage> _fileDeleted;

    public PreparationService(
        PathResolver paths,
        IEnumerable<IPatcher> patchers,
        ILogger<PreparationService> logger,
        IPublisher<PreparationStartedMessage> prepStarted,
        IPublisher<PreparationCompletedMessage> prepCompleted,
        IPublisher<FileCopiedMessage> fileCopied,
        IPublisher<FileMovedMessage> fileMoved,
        IPublisher<FileDeletedMessage> fileDeleted)
    {
        _paths = paths;
        _patchers = patchers;
        _logger = logger;
        _prepStarted = prepStarted;
        _prepCompleted = prepCompleted;
        _fileCopied = fileCopied;
        _fileMoved = fileMoved;
        _fileDeleted = fileDeleted;
    }

    /// <summary>
    /// Runs preparation steps as defined by the config.
    /// </summary>
    public async Task<PreparationCompletedMessage> RunAsync(PreparationConfig config, string? configRelativePath = null)
    {
        _logger.LogInformation("Preparation started");
        _prepStarted.Publish(new PreparationStartedMessage(configRelativePath));

        var copied = 0;
        var moved = 0;
        var deleted = 0;
        var patched = 0;

        // 1) Copy Unity packages
        foreach (var pkg in config.Packages)
        {
            var src = _paths.Resolve(pkg.Source);
            var dst = _paths.Resolve(pkg.Target);
            EnsureDirectoryOf(dst);
            File.Copy(src, dst, overwrite: true);
            copied++;
            _logger.LogInformation("Copied package: {Name} -> {Target}", pkg.Name, pkg.Target);
            _fileCopied.Publish(new FileCopiedMessage(pkg.Source, pkg.Target));
        }

        // 2) Copy assemblies
        foreach (var asm in config.Assemblies)
        {
            var src = _paths.Resolve(asm.Source);
            var dst = _paths.Resolve(asm.Target);
            EnsureDirectoryOf(dst);
            File.Copy(src, dst, overwrite: true);
            copied++;
            _logger.LogInformation("Copied assembly: {Name} -> {Target}", asm.Name, asm.Target);
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
                    CopyPath(op.Source!, op.Target, op.Overwrite);
                    copied++;
                    break;
                case AssetOperation.Move:
                    if (string.IsNullOrWhiteSpace(op.Source))
                        throw new ArgumentException("Move operation requires Source", nameof(op.Source));
                    MovePath(op.Source!, op.Target, op.Overwrite);
                    moved++;
                    break;
                case AssetOperation.Delete:
                    DeletePath(op.Target);
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

            var canApply = await patcher.CanApplyPatchAsync(targetAbs, patch);
            if (!canApply && !patch.Optional)
            {
                _logger.LogWarning("Patch search did not match target, skipping: {File}", patch.File);
                continue;
            }

            var ok = await patcher.ApplyPatchAsync(targetAbs, patch);
            if (ok)
            {
                patched++;
            }
        }

        // 5) Scripting define symbols (no-op placeholder)
        if (config.ScriptingDefineSymbols != null)
        {
            _logger.LogInformation("Scripting define symbols requested: +{Add} -{Remove} (platform: {Platform}, clear: {Clear})",
                string.Join(',', config.ScriptingDefineSymbols.Add ?? new()),
                string.Join(',', config.ScriptingDefineSymbols.Remove ?? new()),
                config.ScriptingDefineSymbols.Platform,
                config.ScriptingDefineSymbols.ClearExisting);
        }

        var summary = new PreparationCompletedMessage(copied, moved, deleted, patched);
        _prepCompleted.Publish(summary);
        _logger.LogInformation("Preparation complete: copied={Copied}, moved={Moved}, deleted={Deleted}, patched={Patched}", copied, moved, deleted, patched);
        return summary;
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
