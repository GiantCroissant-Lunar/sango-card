using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;

namespace SangoCard.Build.Tool.Cli.Commands;

/// <summary>
/// Handles prepare-run CLI command.
/// </summary>
public class PrepareCommandHandler
{
    private readonly ConfigService _configService;
    private readonly ValidationService _validationService;
    private readonly PreparationService _preparationService;
    private readonly PathResolver _paths;
    private readonly ILogger<PrepareCommandHandler> _logger;

    public PrepareCommandHandler(
        ConfigService configService,
        ValidationService validationService,
        PreparationService preparationService,
        PathResolver paths,
        ILogger<PrepareCommandHandler> logger)
    {
        _configService = configService;
        _validationService = validationService;
        _preparationService = preparationService;
        _paths = paths;
        _logger = logger;
    }

    /// <summary>
    /// Injects preparation into target project (Phase 2 of two-phase workflow).
    /// </summary>
    public async Task InjectAsync(string configRelativePath, string targetPath, string? stage = null, string? platform = null, string validationLevel = "full", bool verbose = false, bool force = false)
    {
        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            Console.Error.WriteLine("Error: Config path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            Console.Error.WriteLine("Error: --target is required");
            Console.Error.WriteLine("Usage: prepare inject --config <path> --target projects/client/");
            Environment.ExitCode = 1;
            return;
        }

        // Validate target path (R-BLD-060: Only allow projects/client/)
        var normalizedTarget = targetPath.Replace('\\', '/').TrimEnd('/');
        if (normalizedTarget != "projects/client")
        {
            Console.Error.WriteLine($"Error: Target must be 'projects/client/' per R-BLD-060");
            Console.Error.WriteLine($"       Got: {targetPath}");
            Console.Error.WriteLine($"       Expected: projects/client/");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            // Load config with auto-detection of v1.0 vs v2.0
            PreparationConfig config;
            if (!string.IsNullOrEmpty(stage))
            {
                // Multi-stage mode: Load v2.0 config and extract specific stage
                var multiStageConfig = await _configService.LoadMultiStageAsync(configRelativePath);

                if (verbose)
                {
                    Console.WriteLine($"=== Multi-Stage Mode (v{multiStageConfig.Version}) ===");
                    Console.WriteLine($"Stage: {stage}");
                    if (!string.IsNullOrEmpty(platform))
                    {
                        Console.WriteLine($"Platform: {platform}");
                    }
                }

                // Find the requested stage
                var injectionStage = multiStageConfig.InjectionStages.FirstOrDefault(s =>
                    s.Name.Equals(stage, StringComparison.OrdinalIgnoreCase));

                if (injectionStage == null)
                {
                    Console.Error.WriteLine($"Error: Stage '{stage}' not found in configuration");
                    Console.Error.WriteLine($"Available stages: {string.Join(", ", multiStageConfig.InjectionStages.Select(s => s.Name))}");
                    Environment.ExitCode = 1;
                    return;
                }

                if (!injectionStage.Enabled)
                {
                    Console.WriteLine($"Stage '{stage}' is disabled - skipping injection");
                    Environment.ExitCode = 0;
                    return;
                }

                // Convert stage to v1.0 config for execution (platform handling is in ConvertStageToV1)
                config = _configService.ConvertStageToV1(multiStageConfig, stage);

                if (verbose)
                {
                    Console.WriteLine($"Loaded {config.Packages.Count} packages, {config.Assemblies.Count} assemblies, {config.AssetManipulations.Count} manipulations");
                }
            }
            else
            {
                // Legacy v1.0 mode
                config = await _configService.LoadAsync(configRelativePath);
            }

            // Validate cache exists (Phase 1 must be complete)
            var missingCache = new List<string>();
            foreach (var pkg in config.Packages)
            {
                if (!CacheSourceExists(pkg.Source))
                {
                    missingCache.Add(pkg.Source);
                }
            }
            foreach (var asm in config.Assemblies)
            {
                if (!CacheSourceExists(asm.Source))
                {
                    missingCache.Add(asm.Source);
                }
            }

            if (missingCache.Count > 0)
            {
                Console.Error.WriteLine("Error: Cache files not found. Run 'cache populate' first (Phase 1).");
                Console.Error.WriteLine("\nMissing files:");
                foreach (var file in missingCache.Take(5))
                {
                    Console.Error.WriteLine($"  - {file}");
                }
                if (missingCache.Count > 5)
                {
                    Console.Error.WriteLine($"  ... and {missingCache.Count - 5} more");
                }
                Environment.ExitCode = 1;
                return;
            }

            // Validate config
            var level = ParseLevel(validationLevel);
            var validation = _validationService.Validate(config, level);

            if (verbose)
            {
                Console.WriteLine("=== Validation Results ===");
            }
            Console.WriteLine(validation.Summary);

            if (!validation.IsValid)
            {
                if (verbose || validation.Errors.Count > 0)
                {
                    Console.Error.WriteLine("\nValidation Errors:");
                    foreach (var e in validation.Errors)
                    {
                        Console.Error.WriteLine($"  [{e.Code}] {e.Message}{(string.IsNullOrEmpty(e.File) ? string.Empty : $" ({e.File})")}");
                    }
                }

                if (!force)
                {
                    Console.Error.WriteLine("\nInjection aborted due to validation errors. Use --force to proceed anyway.");
                    Environment.ExitCode = 2;
                    return;
                }
                else
                {
                    Console.WriteLine("\nWarning: Proceeding despite validation errors (--force specified)");
                }
            }

            if (validation.Warnings.Count > 0 && verbose)
            {
                Console.WriteLine("\nValidation Warnings:");
                foreach (var w in validation.Warnings)
                {
                    Console.WriteLine($"  [{w.Code}] {w.Message}{(string.IsNullOrEmpty(w.File) ? string.Empty : $" ({w.File})")}");
                }
            }

            // Execute injection
            if (verbose)
            {
                Console.WriteLine("\n=== Phase 2: Injecting to Client ===");
                Console.WriteLine($"Config: {_paths.Resolve(configRelativePath)}");
                Console.WriteLine($"Target: {targetPath}");
            }

            var startTime = DateTime.UtcNow;
            var result = await _preparationService.ExecuteAsync(config, configRelativePath, dryRun: false, validate: false);
            var duration = DateTime.UtcNow - startTime;

            // Display results
            Console.WriteLine("\n=== Injection Complete ===");
            Console.WriteLine($"Duration: {duration.TotalSeconds:F2}s");
            Console.WriteLine($"Files copied: {result.Copied}");
            Console.WriteLine($"Files moved: {result.Moved}");
            Console.WriteLine($"Files deleted: {result.Deleted}");
            Console.WriteLine($"Patches applied: {result.Patched}");

            if (verbose)
            {
                Console.WriteLine($"Total operations: {result.Copied + result.Moved + result.Deleted + result.Patched}");
            }

            Environment.ExitCode = 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: Config file not found: {ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Injection failed: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
            Environment.ExitCode = 3;
        }
    }

    /// <summary>
    /// [DEPRECATED] Use InjectAsync instead. Kept for backward compatibility.
    /// </summary>
    public async Task RunAsync(string configRelativePath, string validationLevel = "full", bool verbose = false, bool force = false)
    {
        // Redirect to InjectAsync with default target
        await InjectAsync(configRelativePath, "projects/client/", null, null, validationLevel, verbose, force);
    }

    public async Task DryRunAsync(string configRelativePath, string validationLevel = "full", bool verbose = false)
    {
        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            Console.Error.WriteLine("Error: Config path is required");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            var config = await _configService.LoadAsync(configRelativePath);

            // Validate config
            var level = ParseLevel(validationLevel);
            var validation = _validationService.Validate(config, level);

            if (verbose)
            {
                Console.WriteLine("=== Validation Results ===");
            }
            Console.WriteLine(validation.Summary);

            if (!validation.IsValid)
            {
                Console.Error.WriteLine("\nValidation Errors:");
                foreach (var e in validation.Errors)
                {
                    Console.Error.WriteLine($"  [{e.Code}] {e.Message}{(string.IsNullOrEmpty(e.File) ? string.Empty : $" ({e.File})")}");
                }
                Console.Error.WriteLine("\nDry-run aborted due to validation errors.");
                Environment.ExitCode = 2;
                return;
            }

            if (validation.Warnings.Count > 0 && verbose)
            {
                Console.WriteLine("\nValidation Warnings:");
                foreach (var w in validation.Warnings)
                {
                    Console.WriteLine($"  [{w.Code}] {w.Message}{(string.IsNullOrEmpty(w.File) ? string.Empty : $" ({w.File})")}");
                }
            }

            // Execute dry-run
            Console.WriteLine("\n=== DRY-RUN MODE (no files will be modified) ===");
            if (verbose)
            {
                Console.WriteLine($"Config: {_paths.Resolve(configRelativePath)}");
            }

            var startTime = DateTime.UtcNow;
            var result = await _preparationService.ExecuteAsync(config, configRelativePath, dryRun: true, validate: false);
            var duration = DateTime.UtcNow - startTime;

            // Display what would happen
            Console.WriteLine("\n=== Dry-Run Results ===");
            Console.WriteLine($"Duration: {duration.TotalSeconds:F2}s");
            Console.WriteLine($"Files that would be copied: {result.Copied}");
            Console.WriteLine($"Files that would be moved: {result.Moved}");
            Console.WriteLine($"Files that would be deleted: {result.Deleted}");
            Console.WriteLine($"Patches that would be applied: {result.Patched}");
            Console.WriteLine($"\nTotal operations: {result.Copied + result.Moved + result.Deleted + result.Patched}");
            Console.WriteLine("\nNo files were actually modified (dry-run mode).");

            Environment.ExitCode = 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Error: Config file not found: {ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Dry-run failed: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
            Environment.ExitCode = 3;
        }
    }

    public async Task RestoreAsync(string? backupPath = null, bool verbose = false)
    {
        try
        {
            if (verbose && !string.IsNullOrEmpty(backupPath))
            {
                Console.WriteLine($"=== Restoring from Backup ===");
                Console.WriteLine($"Backup path: {_paths.Resolve(backupPath)}");
            }
            else if (verbose)
            {
                Console.WriteLine("=== Restoring from Last Backup ===");
            }

            var startTime = DateTime.UtcNow;
            await _preparationService.RestoreAsync(backupPath);
            var duration = DateTime.UtcNow - startTime;

            Console.WriteLine("\n=== Restore Complete ===");
            Console.WriteLine($"Duration: {duration.TotalSeconds:F2}s");
            Console.WriteLine("All files have been restored from backup.");

            Environment.ExitCode = 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine("No backup found. Run 'prepare run' first to create a backup.");
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: Restore failed: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"\nStack trace:\n{ex.StackTrace}");
            }
            Environment.ExitCode = 3;
        }
    }

    private static ValidationLevel ParseLevel(string? level)
    {
        return level?.Trim().ToLowerInvariant() switch
        {
            "schema" => ValidationLevel.Schema,
            "fileexistence" => ValidationLevel.FileExistence,
            "unitypackages" => ValidationLevel.UnityPackages,
            "full" or null or "" => ValidationLevel.Full,
            _ => ValidationLevel.Full
        };
    }

    /// <summary>
    /// Checks if a cache source exists, tolerating hash suffixes in folder names.
    /// For packages like "com.cysharp.unitask", this will match "com.cysharp.unitask@15a4a7657f99".
    /// </summary>
    /// <param name="sourcePath">The source path to check (e.g., "build/preparation/cache/com.cysharp.unitask").</param>
    /// <returns>True if the exact path exists OR a path with hash suffix exists.</returns>
    private bool CacheSourceExists(string sourcePath)
    {
        // First check exact match
        if (_paths.Exists(sourcePath))
        {
            return true;
        }

        // If not found, check for cache folders with hash suffixes
        // Extract the parent directory and base name
        var normalizedPath = sourcePath.Replace('\\', '/');
        var lastSlash = normalizedPath.LastIndexOf('/');
        if (lastSlash == -1)
        {
            return false; // No directory separator, can't be a cache path
        }

        var parentDir = normalizedPath.Substring(0, lastSlash);
        var baseName = normalizedPath.Substring(lastSlash + 1);

        // Check if parent directory exists
        if (!_paths.DirectoryExists(parentDir))
        {
            return false;
        }

        // Get the absolute parent path
        var absoluteParent = _paths.Resolve(parentDir);

        // Check for folders that match the base name with optional @hash suffix
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

            return matchingFolders.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}
