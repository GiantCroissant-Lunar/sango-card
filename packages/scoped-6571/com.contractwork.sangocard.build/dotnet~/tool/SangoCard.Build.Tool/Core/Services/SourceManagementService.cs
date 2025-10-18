using System.Text.Json;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Utilities;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Service for managing manual source additions to preparation manifests and build configs.
/// Implements the two-config architecture from SPEC-BLD-PREP-002.
/// </summary>
public class SourceManagementService
{
    private readonly PathResolver _paths;
    private readonly ILogger<SourceManagementService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SourceManagementService(PathResolver paths, ILogger<SourceManagementService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>
    /// Loads a preparation manifest from file.
    /// Creates a new one if it doesn't exist.
    /// </summary>
    public async Task<PreparationManifest> LoadOrCreateManifestAsync(string manifestRelativePath)
    {
        var fullPath = _paths.Resolve(manifestRelativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogInformation("Manifest not found, creating new: {Path}", fullPath);
            return new PreparationManifest
            {
                Id = Path.GetFileNameWithoutExtension(manifestRelativePath),
                Title = "Preparation Manifest",
                CacheDirectory = "build/preparation/cache"
            };
        }

        var json = await File.ReadAllTextAsync(fullPath);
        var manifest = JsonSerializer.Deserialize<PreparationManifest>(json, _jsonOptions);

        if (manifest == null)
        {
            throw new InvalidOperationException($"Failed to deserialize manifest: {fullPath}");
        }

        return manifest;
    }

    /// <summary>
    /// Saves a preparation manifest to file.
    /// </summary>
    public async Task SaveManifestAsync(PreparationManifest manifest, string manifestRelativePath)
    {
        var fullPath = _paths.Resolve(manifestRelativePath);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created directory: {Directory}", directory);
        }

        var json = JsonSerializer.Serialize(manifest, _jsonOptions);
        await File.WriteAllTextAsync(fullPath, json);

        _logger.LogInformation("Saved manifest: {Path}", fullPath);
    }

    /// <summary>
    /// Adds a source to the preparation manifest and copies it to cache.
    /// </summary>
    /// <param name="manifest">The preparation manifest to update.</param>
    /// <param name="sourcePath">Source path (absolute or relative).</param>
    /// <param name="cacheAs">Name to use in cache.</param>
    /// <param name="type">Type of item (package, assembly, asset).</param>
    /// <param name="dryRun">If true, only preview without applying changes.</param>
    /// <returns>Result of the operation.</returns>
    public async Task<SourceAdditionResult> AddSourceAsync(
        PreparationManifest manifest,
        string sourcePath,
        string cacheAs,
        string type,
        bool dryRun = false)
    {
        var result = new SourceAdditionResult { Success = false };

        try
        {
            // Resolve source path (can be absolute or relative)
            var resolvedSource = ResolveSourcePath(sourcePath);

            if (!PathExists(resolvedSource))
            {
                result.ErrorMessage = $"Source path does not exist: {resolvedSource}";
                return result;
            }

            // Determine cache path
            var cacheDir = manifest.CacheDirectory ?? "build/preparation/cache";
            var cachePath = Path.Combine(cacheDir, cacheAs);
            var resolvedCachePath = _paths.Resolve(cachePath);

            result.SourcePath = resolvedSource;
            result.CachePath = resolvedCachePath;
            result.CacheRelativePath = cachePath;

            // Check if item already exists in manifest
            var existingItem = manifest.Items.FirstOrDefault(i =>
                i.CacheAs.Equals(cacheAs, StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {
                result.ErrorMessage = $"Item with cacheAs '{cacheAs}' already exists in manifest";
                return result;
            }

            if (dryRun)
            {
                // Calculate file/directory info for preview
                result.FileCount = CountFiles(resolvedSource);
                result.DirectoryCount = CountDirectories(resolvedSource);
                result.TotalSize = CalculateSize(resolvedSource);
                result.Success = true;
                result.DryRun = true;
                return result;
            }

            // Copy source to cache
            await CopyToCacheAsync(resolvedSource, resolvedCachePath);

            // Add item to manifest
            manifest.Items.Add(new PreparationItem
            {
                Source = sourcePath,
                CacheAs = cacheAs,
                Type = type
            });

            result.Success = true;
            _logger.LogInformation("Added source to manifest: {CacheAs}", cacheAs);

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to add source: {Source}", sourcePath);
            return result;
        }
    }

    /// <summary>
    /// Copies a file or directory to the cache.
    /// </summary>
    private async Task CopyToCacheAsync(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            // Copy directory
            CopyDirectory(source, destination);
            _logger.LogInformation("Copied directory to cache: {Source} -> {Destination}", source, destination);
        }
        else if (File.Exists(source))
        {
            // Copy file
            var destDir = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            await Task.Run(() => File.Copy(source, destination, overwrite: true));
            _logger.LogInformation("Copied file to cache: {Source} -> {Destination}", source, destination);
        }
        else
        {
            throw new FileNotFoundException($"Source not found: {source}");
        }
    }

    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
    private void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy subdirectories
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }

    /// <summary>
    /// Resolves a source path (handles absolute, relative, UNC paths).
    /// </summary>
    private string ResolveSourcePath(string path)
    {
        // If already absolute, return as-is
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        // Otherwise, resolve relative to git root
        return _paths.Resolve(path);
    }

    /// <summary>
    /// Checks if a path exists (file or directory).
    /// </summary>
    private bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <summary>
    /// Adds an injection mapping to a build config.
    /// </summary>
    public void AddInjection(
        PreparationConfig config,
        string cacheRelativePath,
        string targetRelativePath,
        string type,
        string name,
        string? version = null)
    {
        switch (type.ToLowerInvariant())
        {
            case PreparationItemType.Package:
                config.Packages.Add(new UnityPackageReference
                {
                    Name = name,
                    Version = version ?? "1.0.0",
                    Source = cacheRelativePath,
                    Target = targetRelativePath
                });
                _logger.LogInformation("Added package injection: {Name}", name);
                break;

            case PreparationItemType.Assembly:
                config.Assemblies.Add(new AssemblyReference
                {
                    Name = name,
                    Version = version,
                    Source = cacheRelativePath,
                    Target = targetRelativePath
                });
                _logger.LogInformation("Added assembly injection: {Name}", name);
                break;

            case PreparationItemType.Asset:
                config.AssetManipulations.Add(new AssetManipulation
                {
                    Operation = AssetOperation.Copy,
                    Source = cacheRelativePath,
                    Target = targetRelativePath
                });
                _logger.LogInformation("Added asset injection: {Source} -> {Target}", cacheRelativePath, targetRelativePath);
                break;

            default:
                throw new ArgumentException($"Unknown type: {type}. Must be 'package', 'assembly', or 'asset'.");
        }
    }

    /// <summary>
    /// Processes a batch manifest for source collection (Phase 1).
    /// </summary>
    public async Task<BatchProcessingResult> ProcessBatchSourcesAsync(
        BatchManifest batchManifest,
        PreparationManifest targetManifest,
        bool dryRun = false,
        bool continueOnError = false)
    {
        var result = new BatchProcessingResult();
        var cacheDir = targetManifest.CacheDirectory ?? "build/preparation/cache";

        // Process packages
        foreach (var package in batchManifest.Packages)
        {
            try
            {
                var cacheAs = string.IsNullOrEmpty(package.Version)
                    ? package.Name
                    : $"{package.Name}-{package.Version}";

                var addResult = await AddSourceAsync(
                    targetManifest,
                    package.Source,
                    cacheAs,
                    PreparationItemType.Package,
                    dryRun);

                if (addResult.Success)
                {
                    result.SuccessCount++;
                    result.SuccessfulItems.Add($"Package: {package.Name}");
                }
                else
                {
                    result.FailureCount++;
                    result.FailedItems.Add(($"Package: {package.Name}", addResult.ErrorMessage ?? "Unknown error"));

                    if (!continueOnError)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Package: {package.Name}", ex.Message));
                _logger.LogError(ex, "Failed to process package: {Name}", package.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        // Process assemblies
        foreach (var assembly in batchManifest.Assemblies)
        {
            try
            {
                var cacheAs = string.IsNullOrEmpty(assembly.Version)
                    ? assembly.Name
                    : $"{assembly.Name}-{assembly.Version}";

                var addResult = await AddSourceAsync(
                    targetManifest,
                    assembly.Source,
                    cacheAs,
                    PreparationItemType.Assembly,
                    dryRun);

                if (addResult.Success)
                {
                    result.SuccessCount++;
                    result.SuccessfulItems.Add($"Assembly: {assembly.Name}");
                }
                else
                {
                    result.FailureCount++;
                    result.FailedItems.Add(($"Assembly: {assembly.Name}", addResult.ErrorMessage ?? "Unknown error"));

                    if (!continueOnError)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Assembly: {assembly.Name}", ex.Message));
                _logger.LogError(ex, "Failed to process assembly: {Name}", assembly.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        // Process assets
        foreach (var asset in batchManifest.Assets)
        {
            try
            {
                var addResult = await AddSourceAsync(
                    targetManifest,
                    asset.Source,
                    asset.Name,
                    PreparationItemType.Asset,
                    dryRun);

                if (addResult.Success)
                {
                    result.SuccessCount++;
                    result.SuccessfulItems.Add($"Asset: {asset.Name}");
                }
                else
                {
                    result.FailureCount++;
                    result.FailedItems.Add(($"Asset: {asset.Name}", addResult.ErrorMessage ?? "Unknown error"));

                    if (!continueOnError)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Asset: {asset.Name}", ex.Message));
                _logger.LogError(ex, "Failed to process asset: {Name}", asset.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Processes a batch manifest for injection mappings (Phase 2).
    /// </summary>
    public BatchProcessingResult ProcessBatchInjections(
        BatchManifest batchManifest,
        PreparationConfig targetConfig,
        bool dryRun = false,
        bool continueOnError = false)
    {
        var result = new BatchProcessingResult();
        var cacheDir = "build/preparation/cache";

        // Process packages
        foreach (var package in batchManifest.Packages)
        {
            try
            {
                var cacheAs = string.IsNullOrEmpty(package.Version)
                    ? package.Name
                    : $"{package.Name}-{package.Version}";

                var cacheRelativePath = Path.Combine(cacheDir, cacheAs);
                var targetPath = package.Target ?? $"projects/client/Packages/{package.Name}";

                if (!dryRun)
                {
                    AddInjection(targetConfig, cacheRelativePath, targetPath, PreparationItemType.Package, package.Name, package.Version);
                }

                result.SuccessCount++;
                result.SuccessfulItems.Add($"Package: {package.Name}");
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Package: {package.Name}", ex.Message));
                _logger.LogError(ex, "Failed to add package injection: {Name}", package.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        // Process assemblies
        foreach (var assembly in batchManifest.Assemblies)
        {
            try
            {
                var cacheAs = string.IsNullOrEmpty(assembly.Version)
                    ? assembly.Name
                    : $"{assembly.Name}-{assembly.Version}";

                var cacheRelativePath = Path.Combine(cacheDir, cacheAs);
                var targetPath = assembly.Target ?? $"projects/client/Assets/Plugins/{assembly.Name}";

                if (!dryRun)
                {
                    AddInjection(targetConfig, cacheRelativePath, targetPath, PreparationItemType.Assembly, assembly.Name, assembly.Version);
                }

                result.SuccessCount++;
                result.SuccessfulItems.Add($"Assembly: {assembly.Name}");
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Assembly: {assembly.Name}", ex.Message));
                _logger.LogError(ex, "Failed to add assembly injection: {Name}", assembly.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        // Process assets
        foreach (var asset in batchManifest.Assets)
        {
            try
            {
                var cacheRelativePath = Path.Combine(cacheDir, asset.Name);

                if (!dryRun)
                {
                    AddInjection(targetConfig, cacheRelativePath, asset.Target, PreparationItemType.Asset, asset.Name, null);
                }

                result.SuccessCount++;
                result.SuccessfulItems.Add($"Asset: {asset.Name}");
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.FailedItems.Add(($"Asset: {asset.Name}", ex.Message));
                _logger.LogError(ex, "Failed to add asset injection: {Name}", asset.Name);

                if (!continueOnError)
                {
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Counts the number of files in a path (file or directory).
    /// </summary>
    private int CountFiles(string path)
    {
        if (File.Exists(path))
        {
            return 1;
        }
        else if (Directory.Exists(path))
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
        }
        return 0;
    }

    /// <summary>
    /// Counts the number of directories in a path.
    /// </summary>
    private int CountDirectories(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length;
        }
        return 0;
    }

    /// <summary>
    /// Calculates the total size of files in a path (file or directory).
    /// </summary>
    private long CalculateSize(string path)
    {
        if (File.Exists(path))
        {
            return new FileInfo(path).Length;
        }
        else if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            return files.Sum(f => new FileInfo(f).Length);
        }
        return 0;
    }
}

/// <summary>
/// Result of batch processing operations.
/// </summary>
public class BatchProcessingResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> SuccessfulItems { get; set; } = new();
    public List<(string Item, string Error)> FailedItems { get; set; } = new();
}

/// <summary>
/// Result of a source addition operation.
/// </summary>
public class SourceAdditionResult
{
    public bool Success { get; set; }
    public bool DryRun { get; set; }
    public string? SourcePath { get; set; }
    public string? CachePath { get; set; }
    public string? CacheRelativePath { get; set; }
    public string? ErrorMessage { get; set; }
    public int FileCount { get; set; }
    public int DirectoryCount { get; set; }
    public long TotalSize { get; set; }
}
