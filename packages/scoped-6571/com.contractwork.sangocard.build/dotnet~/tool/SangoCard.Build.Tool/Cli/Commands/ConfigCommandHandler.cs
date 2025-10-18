using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;

namespace SangoCard.Build.Tool.Cli.Commands;

/// <summary>
/// Handles config-related CLI commands.
/// </summary>
public class ConfigCommandHandler
{
    private readonly ConfigService _configService;
    private readonly ValidationService _validationService;
    private readonly SourceManagementService _sourceManagementService;
    private readonly BatchManifestService _batchManifestService;
    private readonly PathResolver _paths;
    private readonly ILogger<ConfigCommandHandler> _logger;

    public ConfigCommandHandler(
        ConfigService configService,
        ValidationService validationService,
        SourceManagementService sourceManagementService,
        BatchManifestService batchManifestService,
        PathResolver paths,
        ILogger<ConfigCommandHandler> logger)
    {
        _configService = configService;
        _validationService = validationService;
        _sourceManagementService = sourceManagementService;
        _batchManifestService = batchManifestService;
        _paths = paths;
        _logger = logger;
    }

    public async Task CreateAsync(string outputRelativePath, string? description)
    {
        var config = _configService.CreateNew(description);
        await _configService.SaveAsync(config, outputRelativePath);

        Console.WriteLine($"Created config at: {_paths.Resolve(outputRelativePath)}");
    }

    public async Task ValidateAsync(string fileRelativePath, string level)
    {
        var config = await _configService.LoadAsync(fileRelativePath);
        var parsedLevel = ParseLevel(level);
        var result = _validationService.Validate(config, parsedLevel);

        Console.WriteLine(result.Summary);
        if (result.Errors.Count > 0)
        {
            Console.WriteLine("Errors:");
            foreach (var e in result.Errors)
            {
                Console.WriteLine($"- [{e.Code}] {e.Message}{(string.IsNullOrEmpty(e.File) ? string.Empty : $" ({e.File})")}");
            }
        }
        if (result.Warnings.Count > 0)
        {
            Console.WriteLine("Warnings:");
            foreach (var w in result.Warnings)
            {
                Console.WriteLine($"- [{w.Code}] {w.Message}{(string.IsNullOrEmpty(w.File) ? string.Empty : $" ({w.File})")}");
            }
        }

        Environment.ExitCode = result.IsValid ? 0 : 2;
    }

    public async Task AddPackageAsync(string configRelativePath, string name, string version, string? sourceRelativePath = null, string? targetRelativePath = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Error: Package name is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            Console.Error.WriteLine("Error: Package version is required");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);

        // Generate default paths if not provided
        var source = sourceRelativePath ?? $"build/preparation/cache/{name}-{version}.tgz";
        var target = targetRelativePath ?? $"projects/client/Packages/{name}-{version}.tgz";

        var package = new UnityPackageReference
        {
            Name = name,
            Version = version,
            Source = source,
            Target = target
        };

        _configService.AddPackage(config, package);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added package: {name}@{version}");
        Console.WriteLine($"  Source: {_paths.Resolve(source)}");
        Console.WriteLine($"  Target: {_paths.Resolve(target)}");
    }

    public async Task AddAssemblyAsync(string configRelativePath, string name, string sourceRelativePath, string? targetRelativePath = null, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Error: Assembly name is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceRelativePath))
        {
            Console.Error.WriteLine("Error: Source path is required");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);

        // Generate default target path if not provided
        var target = targetRelativePath ?? $"projects/client/Assets/Plugins/{name}.dll";

        var assembly = new AssemblyReference
        {
            Name = name,
            Version = version,
            Source = sourceRelativePath,
            Target = target
        };

        _configService.AddAssembly(config, assembly);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added assembly: {name}");
        Console.WriteLine($"  Source: {_paths.Resolve(sourceRelativePath)}");
        Console.WriteLine($"  Target: {_paths.Resolve(target)}");
    }

    public async Task AddDefineAsync(string configRelativePath, string symbol, string? platform = null)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            Console.Error.WriteLine("Error: Symbol is required");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);
        _configService.AddDefineSymbol(config, symbol, platform);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added scripting define symbol: {symbol}");
        if (!string.IsNullOrEmpty(platform))
        {
            Console.WriteLine($"  Platform: {platform}");
        }
    }

    public async Task AddPatchAsync(string configRelativePath, string fileRelativePath, string patchType, string search, string replace, string? mode = null, string? operation = null, bool optional = false)
    {
        if (string.IsNullOrWhiteSpace(fileRelativePath))
        {
            Console.Error.WriteLine("Error: File path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(patchType))
        {
            Console.Error.WriteLine("Error: Patch type is required (csharp, json, unity, text)");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            Console.Error.WriteLine("Error: Search pattern is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(replace))
        {
            Console.Error.WriteLine("Error: Replace value is required");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);
        var patch = new CodePatch
        {
            File = fileRelativePath,
            Type = ParsePatchType(patchType),
            Search = search,
            Replace = replace,
            Mode = ParsePatchMode(mode),
            Operation = operation,
            Optional = optional
        };

        _configService.AddPatch(config, patch);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added {patchType} patch to: {fileRelativePath}");
        Console.WriteLine($"  Search: {search}");
        Console.WriteLine($"  Replace: {replace}");
        if (!string.IsNullOrEmpty(operation))
        {
            Console.WriteLine($"  Operation: {operation}");
        }
        else
        {
            Console.WriteLine($"  Mode: {patch.Mode}");
        }
        Console.WriteLine($"  Optional: {optional}");
    }

    private static ValidationLevel ParseLevel(string level)
    {
        return level?.Trim().ToLowerInvariant() switch
        {
            "schema" => ValidationLevel.Schema,
            "fileexistence" => ValidationLevel.FileExistence,
            "unitypackages" => ValidationLevel.UnityPackages,
            "full" or _ => ValidationLevel.Full
        };
    }

    private static PatchType ParsePatchType(string patchType)
    {
        return patchType?.Trim().ToLowerInvariant() switch
        {
            "csharp" or "cs" => PatchType.CSharp,
            "json" => PatchType.Json,
            "yaml" or "unity" or "unityasset" => PatchType.UnityAsset,
            "text" or "regex" => PatchType.Text,
            _ => PatchType.Text
        };
    }

    private static PatchMode ParsePatchMode(string? mode)
    {
        return mode?.Trim().ToLowerInvariant() switch
        {
            "replace" or null => PatchMode.Replace,
            "insertbefore" or "before" => PatchMode.InsertBefore,
            "insertafter" or "after" => PatchMode.InsertAfter,
            "delete" or "remove" => PatchMode.Delete,
            _ => PatchMode.Replace
        };
    }

    /// <summary>
    /// Adds a source to preparation manifest (Phase 1: Preparation).
    /// Implements SPEC-BLD-PREP-002 requirement R-BLD-PREP-021.
    /// </summary>
    public async Task AddSourceAsync(
        string sourcePath,
        string cacheAs,
        string type,
        string manifestPath,
        bool dryRun = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.Error.WriteLine("Error: Source path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(cacheAs))
        {
            Console.Error.WriteLine("Error: Cache name (--cache-as) is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            Console.Error.WriteLine("Error: Type is required (package, assembly, or asset)");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            Console.Error.WriteLine("Error: Manifest path is required");
            Environment.ExitCode = 1;
            return;
        }

        // Validate type
        var validTypes = new[] { PreparationItemType.Package, PreparationItemType.Assembly, PreparationItemType.Asset };
        if (!validTypes.Contains(type.ToLowerInvariant()))
        {
            Console.Error.WriteLine($"Error: Invalid type '{type}'. Must be one of: {string.Join(", ", validTypes)}");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            var manifest = await _sourceManagementService.LoadOrCreateManifestAsync(manifestPath);
            var result = await _sourceManagementService.AddSourceAsync(manifest, sourcePath, cacheAs, type, dryRun);

            if (!result.Success)
            {
                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                Environment.ExitCode = 1;
                return;
            }

            if (dryRun)
            {
                Console.WriteLine("[DRY RUN] Operation: Add source to preparation manifest");
                Console.WriteLine($"  Source: {result.SourcePath}");
                Console.WriteLine($"  Cache As: {cacheAs}");
                Console.WriteLine($"  Type: {type}");
                Console.WriteLine($"  Manifest: {manifestPath}");
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] Would copy:");
                Console.WriteLine($"  From: {result.SourcePath}");
                Console.WriteLine($"  To: {result.CachePath}");
                Console.WriteLine($"  Files: {result.FileCount} files ({FormatBytes(result.TotalSize)})");
                if (result.DirectoryCount > 0)
                {
                    Console.WriteLine($"  Directories: {result.DirectoryCount}");
                }
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] Would update manifest:");
                Console.WriteLine($"  + Add item: {cacheAs} ({type})");
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] No changes made. Remove --dry-run to execute.");
            }
            else
            {
                await _sourceManagementService.SaveManifestAsync(manifest, manifestPath);

                Console.WriteLine($"✓ Added {type} to manifest: {cacheAs}");
                Console.WriteLine($"  Source: {result.SourcePath}");
                Console.WriteLine($"  Cache:  {result.CachePath}");
                Console.WriteLine($"  Manifest: {_paths.Resolve(manifestPath)}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Failed to add source to manifest");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Adds an injection mapping to build config (Phase 2: Build Injection).
    /// Implements SPEC-BLD-PREP-002 requirement R-BLD-PREP-021.
    /// </summary>
    public async Task AddInjectionAsync(
        string sourceRelativePath,
        string targetRelativePath,
        string type,
        string configPath,
        string? name = null,
        string? version = null,
        bool dryRun = false)
    {
        if (string.IsNullOrWhiteSpace(sourceRelativePath))
        {
            Console.Error.WriteLine("Error: Source path (from cache) is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(targetRelativePath))
        {
            Console.Error.WriteLine("Error: Target path (in client) is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            Console.Error.WriteLine("Error: Type is required (package, assembly, or asset)");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(configPath))
        {
            Console.Error.WriteLine("Error: Config path is required");
            Environment.ExitCode = 1;
            return;
        }

        // Auto-detect name if not provided
        var itemName = name ?? Path.GetFileNameWithoutExtension(sourceRelativePath);

        try
        {
            var config = await _configService.LoadAsync(configPath);

            if (dryRun)
            {
                Console.WriteLine("[DRY RUN] Operation: Add injection to build config");
                Console.WriteLine($"  Cache Source: {sourceRelativePath}");
                Console.WriteLine($"  Client Target: {targetRelativePath}");
                Console.WriteLine($"  Type: {type}");
                Console.WriteLine($"  Config: {configPath}");
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] Would add injection:");
                Console.WriteLine($"  Name: {itemName}");
                Console.WriteLine($"  Version: {version ?? "(not specified)"}");
                Console.WriteLine($"  Source: {_paths.Resolve(sourceRelativePath)}");
                Console.WriteLine($"  Target: {_paths.Resolve(targetRelativePath)}");
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] Would update config:");
                Console.WriteLine($"  + Add {type}: {itemName}");
                Console.WriteLine();
                Console.WriteLine("[DRY RUN] No changes made. Remove --dry-run to execute.");
            }
            else
            {
                _sourceManagementService.AddInjection(config, sourceRelativePath, targetRelativePath, type, itemName, version);
                await _configService.SaveAsync(config, configPath);

                Console.WriteLine($"✓ Added {type} injection: {itemName}");
                Console.WriteLine($"  Source: {_paths.Resolve(sourceRelativePath)}");
                Console.WriteLine($"  Target: {_paths.Resolve(targetRelativePath)}");
                Console.WriteLine($"  Config: {_paths.Resolve(configPath)}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Failed to add injection to config");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Adds multiple items from a batch manifest (Phase 1 or Phase 2).
    /// Implements SPEC-BLD-PREP-002 requirement R-BLD-PREP-025.
    /// </summary>
    public async Task AddBatchAsync(
        string batchManifestPath,
        string outputPath,
        string configType,
        bool dryRun = false,
        bool continueOnError = false)
    {
        if (string.IsNullOrWhiteSpace(batchManifestPath))
        {
            Console.Error.WriteLine("Error: Batch manifest path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Console.Error.WriteLine("Error: Output config path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(configType))
        {
            Console.Error.WriteLine("Error: Config type is required (source or injection)");
            Environment.ExitCode = 1;
            return;
        }

        var validConfigTypes = new[] { "source", "injection" };
        var normalizedType = configType.ToLowerInvariant();
        if (!validConfigTypes.Contains(normalizedType))
        {
            Console.Error.WriteLine($"Error: Invalid config type '{configType}'. Must be 'source' or 'injection'");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            // Load batch manifest
            Console.WriteLine($"Loading batch manifest: {batchManifestPath}");
            var batchManifest = await _batchManifestService.LoadBatchManifestAsync(batchManifestPath);

            // Validate batch manifest
            var validation = _batchManifestService.ValidateBatchManifest(batchManifest);
            if (!validation.IsValid)
            {
                Console.Error.WriteLine("Batch manifest validation failed:");
                foreach (var error in validation.Errors)
                {
                    Console.Error.WriteLine($"  - {error}");
                }
                Environment.ExitCode = 1;
                return;
            }

            if (validation.Warnings.Count > 0)
            {
                Console.WriteLine("Warnings:");
                foreach (var warning in validation.Warnings)
                {
                    Console.WriteLine($"  - {warning}");
                }
                Console.WriteLine();
            }

            var totalItems = batchManifest.Packages.Count + batchManifest.Assemblies.Count + batchManifest.Assets.Count;
            Console.WriteLine($"Processing {totalItems} items from batch manifest...");
            Console.WriteLine();

            BatchProcessingResult? result = null;

            if (normalizedType == "source")
            {
                // Phase 1: Add sources to preparation manifest
                var manifest = await _sourceManagementService.LoadOrCreateManifestAsync(outputPath);
                result = await _sourceManagementService.ProcessBatchSourcesAsync(
                    batchManifest,
                    manifest,
                    dryRun,
                    continueOnError);

                if (!dryRun && result.SuccessCount > 0)
                {
                    await _sourceManagementService.SaveManifestAsync(manifest, outputPath);
                }
            }
            else // injection
            {
                // Phase 2: Add injections to build config
                var config = await _configService.LoadAsync(outputPath);
                result = _sourceManagementService.ProcessBatchInjections(
                    batchManifest,
                    config,
                    dryRun,
                    continueOnError);

                if (!dryRun && result.SuccessCount > 0)
                {
                    await _configService.SaveAsync(config, outputPath);
                }
            }

            // Print results
            Console.WriteLine();
            Console.WriteLine("─────────────────────────────────────────────");
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Total items:     {totalItems}");
            Console.WriteLine($"  Successful:      {result.SuccessCount}");
            Console.WriteLine($"  Failed:          {result.FailureCount}");
            Console.WriteLine("─────────────────────────────────────────────");

            if (result.SuccessfulItems.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("✓ Successful items:");
                foreach (var item in result.SuccessfulItems)
                {
                    Console.WriteLine($"  - {item}");
                }
            }

            if (result.FailedItems.Count > 0)
            {
                Console.WriteLine();
                Console.Error.WriteLine("✗ Failed items:");
                foreach (var (item, error) in result.FailedItems)
                {
                    Console.Error.WriteLine($"  - {item}: {error}");
                }
            }

            if (dryRun)
            {
                Console.WriteLine();
                Console.WriteLine("No changes made (dry run mode).");
            }
            else if (result.SuccessCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine($"Configuration updated: {_paths.Resolve(outputPath)}");
            }

            Environment.ExitCode = result.FailureCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            _logger.LogError(ex, "Failed to process batch manifest");
            Environment.ExitCode = 1;
        }
    }

    /// <summary>
    /// Formats bytes into human-readable format (KB, MB, GB).
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}
