using System.Security.Cryptography;
using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Service for managing the preparation cache.
/// </summary>
public class CacheService
{
    private readonly PathResolver _pathResolver;
    private readonly ConfigService _configService;
    private readonly ILogger<CacheService> _logger;
    private readonly IPublisher<CachePopulatedMessage> _cachePopulatedPublisher;
    private readonly IPublisher<CacheItemAddedMessage> _cacheItemAddedPublisher;
    private readonly IPublisher<CacheItemRemovedMessage> _cacheItemRemovedPublisher;
    private readonly IPublisher<CacheCleanedMessage> _cacheCleanedPublisher;

    /// <summary>
    /// Default cache directory relative to git root.
    /// </summary>
    public const string DefaultCacheDirectory = "build/preparation/cache";

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    public CacheService(
        PathResolver pathResolver,
        ConfigService configService,
        ILogger<CacheService> logger,
        IPublisher<CachePopulatedMessage> cachePopulatedPublisher,
        IPublisher<CacheItemAddedMessage> cacheItemAddedPublisher,
        IPublisher<CacheItemRemovedMessage> cacheItemRemovedPublisher,
        IPublisher<CacheCleanedMessage> cacheCleanedPublisher)
    {
        _pathResolver = pathResolver;
        _configService = configService;
        _logger = logger;
        _cachePopulatedPublisher = cachePopulatedPublisher;
        _cacheItemAddedPublisher = cacheItemAddedPublisher;
        _cacheItemRemovedPublisher = cacheItemRemovedPublisher;
        _cacheCleanedPublisher = cacheCleanedPublisher;
    }

    /// <summary>
    /// Populates the cache from all source paths defined in the config.
    /// </summary>
    /// <param name="config">Configuration containing source paths for packages and assemblies.</param>
    /// <param name="cacheRelativePath">Cache directory path (relative to git root). Defaults to build/preparation/cache.</param>
    /// <returns>List of cache items that were added.</returns>
    public async Task<List<CacheItem>> PopulateFromConfigAsync(
        PreparationConfig config,
        string? cacheRelativePath = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;
        var cacheAbsolutePath = _pathResolver.Resolve(cacheRelativePath);
        _pathResolver.EnsureDirectory(cacheRelativePath);

        var addedItems = new List<CacheItem>();

        _logger.LogInformation("Populating cache from config: {PackageCount} packages, {AssemblyCount} assemblies",
            config.Packages.Count, config.Assemblies.Count);

        // Copy packages directly from their source paths
        foreach (var package in config.Packages)
        {
            var packageSourcePath = _pathResolver.Resolve(package.Source);
            if (Directory.Exists(packageSourcePath))
            {
                var packageName = Path.GetFileName(packageSourcePath);
                var targetPath = Path.Combine(cacheAbsolutePath, packageName);

                _logger.LogDebug("Copying package: {Name} from {Source}", package.Name, package.Source);
                CopyDirectory(packageSourcePath, targetPath, overwrite: true);

                var dirInfo = new DirectoryInfo(targetPath);
                var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                var cacheItem = new CacheItem
                {
                    Type = CacheItemType.UnityPackage,
                    Name = package.Name,
                    Version = package.Version,
                    Path = _pathResolver.MakeRelative(targetPath),
                    Size = totalSize,
                    Hash = null,
                    AddedDate = DateTime.UtcNow,
                    Source = package.Source
                };

                addedItems.Add(cacheItem);
                _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));
            }
            else
            {
                _logger.LogWarning("Package source not found: {Source}", packageSourcePath);
            }
        }

        // Copy assemblies directly from their source paths
        foreach (var assembly in config.Assemblies)
        {
            var assemblySourcePath = _pathResolver.Resolve(assembly.Source);
            if (Directory.Exists(assemblySourcePath))
            {
                var assemblyName = Path.GetFileName(assemblySourcePath);
                var targetPath = Path.Combine(cacheAbsolutePath, assemblyName);

                _logger.LogDebug("Copying assembly directory: {Name} from {Source}", assembly.Name, assembly.Source);
                CopyDirectory(assemblySourcePath, targetPath, overwrite: true);

                var dirInfo = new DirectoryInfo(targetPath);
                var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

                var cacheItem = new CacheItem
                {
                    Type = CacheItemType.Assembly,
                    Name = assembly.Name,
                    Version = assembly.Version,
                    Path = _pathResolver.MakeRelative(targetPath),
                    Size = totalSize,
                    Hash = null,
                    AddedDate = DateTime.UtcNow,
                    Source = assembly.Source
                };

                addedItems.Add(cacheItem);
                _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));
            }
            else if (File.Exists(assemblySourcePath) && assemblySourcePath.EndsWith(".dll"))
            {
                // Handle standalone DLL files
                var fileName = Path.GetFileName(assemblySourcePath);
                var targetPath = Path.Combine(cacheAbsolutePath, fileName);

                _logger.LogDebug("Copying assembly file: {Name} from {Source}", assembly.Name, assembly.Source);
                File.Copy(assemblySourcePath, targetPath, overwrite: true);

                var cacheItem = await CreateCacheItemAsync(targetPath, CacheItemType.Assembly, assembly.Name, assembly.Version, assembly.Source);

                addedItems.Add(cacheItem);
                _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));
            }
            else
            {
                _logger.LogWarning("Assembly source not found: {Source}", assemblySourcePath);
            }
        }

        _logger.LogInformation(
            "Cache populated from config: {ItemCount} items copied to cache",
            addedItems.Count
        );

        _cachePopulatedPublisher.Publish(new CachePopulatedMessage(addedItems.Count, "config"));

        return addedItems;
    }

    /// <summary>
    /// Populates the cache from a source directory (e.g., code-quality).
    /// </summary>
    /// <param name="sourceRelativePath">Source directory path (relative to git root).</param>
    /// <param name="cacheRelativePath">Cache directory path (relative to git root). Defaults to build/preparation/cache.</param>
    /// <param name="config">Optional configuration to auto-update with discovered items.</param>
    /// <returns>List of cache items that were added.</returns>
    public async Task<List<CacheItem>> PopulateFromDirectoryAsync(
        string sourceRelativePath,
        string? cacheRelativePath = null,
        PreparationConfig? config = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;

        var sourceAbsolutePath = _pathResolver.Resolve(sourceRelativePath);
        var cacheAbsolutePath = _pathResolver.Resolve(cacheRelativePath);

        _logger.LogInformation("Populating cache from: {Source} to {Cache}", sourceAbsolutePath, cacheAbsolutePath);

        if (!Directory.Exists(sourceAbsolutePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceAbsolutePath}");
        }

        // Ensure cache directory exists
        _pathResolver.EnsureDirectory(cacheRelativePath);

        var addedItems = new List<CacheItem>();

        // Find Unity packages (.tgz files)
        var packageFiles = Directory.GetFiles(sourceAbsolutePath, "*.tgz", SearchOption.AllDirectories);
        foreach (var packageFile in packageFiles)
        {
            var item = await AddPackageToCacheAsync(packageFile, cacheAbsolutePath, sourceRelativePath, config);
            addedItems.Add(item);
        }

        // Find Unity package directories (from PackageCache)
        var packageDirs = new List<string>();
        var packageCachePath = Path.Combine(sourceAbsolutePath, "Library", "PackageCache");
        if (Directory.Exists(packageCachePath))
        {
            packageDirs = Directory.GetDirectories(packageCachePath, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach (var packageDir in packageDirs)
            {
                var item = await AddPackageDirectoryToCacheAsync(packageDir, cacheAbsolutePath, sourceRelativePath, config);
                if (item != null)
                {
                    addedItems.Add(item);
                }
            }
        }

        // Find assemblies (from Assets/Packages ONLY - not from Library or other Unity build artifacts)
        // Assemblies can be either directories or standalone DLL files
        var assemblyDirs = new List<string>();
        var assemblyFiles = new List<string>();
        var assetsPackagesPath = Path.Combine(sourceAbsolutePath, "Assets", "Packages");
        if (Directory.Exists(assetsPackagesPath))
        {
            // Find assembly directories
            assemblyDirs = Directory.GetDirectories(assetsPackagesPath, "*", SearchOption.TopDirectoryOnly).ToList();
            foreach (var assemblyDir in assemblyDirs)
            {
                var item = await AddAssemblyDirectoryToCacheAsync(assemblyDir, cacheAbsolutePath, sourceRelativePath, config);
                if (item != null)
                {
                    addedItems.Add(item);
                }
            }

            // Find standalone assembly DLL files
            assemblyFiles = Directory.GetFiles(assetsPackagesPath, "*.dll", SearchOption.TopDirectoryOnly).ToList();
            foreach (var assemblyFile in assemblyFiles)
            {
                var item = await AddAssemblyToCacheAsync(assemblyFile, cacheAbsolutePath, sourceRelativePath, config);
                addedItems.Add(item);
            }
        }

        _logger.LogDebug("Skipping Unity build artifacts in Library/ (Bee, ScriptAssemblies, etc.)");

        _logger.LogInformation(
            "Cache populated: {PackageCount} packages ({TgzCount} .tgz, {PackageDirCount} directories), {AssemblyCount} assemblies ({AssemblyDirCount} directories, {AssemblyFileCount} files)",
            packageFiles.Length + packageDirs.Count,
            packageFiles.Length,
            packageDirs.Count,
            assemblyDirs.Count + assemblyFiles.Count,
            assemblyDirs.Count,
            assemblyFiles.Count
        );

        // Publish message
        _cachePopulatedPublisher.Publish(new CachePopulatedMessage(addedItems.Count, sourceRelativePath));

        return addedItems;
    }

    /// <summary>
    /// Adds a Unity package to the cache.
    /// </summary>
    /// <param name="config">Configuration to update.</param>
    /// <param name="packageName">Package name (e.g., com.unity.addressables).</param>
    /// <param name="packageVersion">Package version.</param>
    /// <param name="sourceFilePath">Source file path (absolute).</param>
    /// <param name="cacheRelativePath">Cache directory (relative to git root).</param>
    /// <returns>The created cache item.</returns>
    public async Task<CacheItem> AddPackageAsync(
        PreparationConfig config,
        string packageName,
        string packageVersion,
        string sourceFilePath,
        string? cacheRelativePath = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;

        var fileName = $"{packageName}-{packageVersion}.tgz";
        var cacheFilePath = Path.Combine(_pathResolver.Resolve(cacheRelativePath), fileName);

        _logger.LogDebug("Adding package to cache: {Package}@{Version}", packageName, packageVersion);

        // Copy file to cache
        File.Copy(sourceFilePath, cacheFilePath, overwrite: true);

        // Create cache item
        var cacheItem = await CreateCacheItemAsync(cacheFilePath, CacheItemType.UnityPackage, packageName, packageVersion);

        // Update config
        var packageRef = new UnityPackageReference
        {
            Name = packageName,
            Version = packageVersion,
            Source = _pathResolver.MakeRelative(cacheFilePath),
            Target = $"projects/client/Packages/{fileName}" // Default target
        };
        _configService.AddPackage(config, packageRef);

        // Publish message
        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    /// <summary>
    /// Adds an assembly to the cache.
    /// </summary>
    /// <param name="config">Configuration to update.</param>
    /// <param name="assemblyName">Assembly name.</param>
    /// <param name="assemblyVersion">Assembly version (optional).</param>
    /// <param name="sourceFilePath">Source file path (absolute).</param>
    /// <param name="cacheRelativePath">Cache directory (relative to git root).</param>
    /// <returns>The created cache item.</returns>
    public async Task<CacheItem> AddAssemblyAsync(
        PreparationConfig config,
        string assemblyName,
        string? assemblyVersion,
        string sourceFilePath,
        string? cacheRelativePath = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;

        var fileName = Path.GetFileName(sourceFilePath);
        var cacheFilePath = Path.Combine(_pathResolver.Resolve(cacheRelativePath), fileName);

        _logger.LogDebug("Adding assembly to cache: {Assembly}", assemblyName);

        // Copy file to cache
        File.Copy(sourceFilePath, cacheFilePath, overwrite: true);

        // Create cache item
        var cacheItem = await CreateCacheItemAsync(cacheFilePath, CacheItemType.Assembly, assemblyName, assemblyVersion);

        // Update config
        var assemblyRef = new AssemblyReference
        {
            Name = assemblyName,
            Version = assemblyVersion,
            Source = _pathResolver.MakeRelative(cacheFilePath),
            Target = $"projects/client/Assets/Plugins/{fileName}" // Default target
        };
        _configService.AddAssembly(config, assemblyRef);

        // Publish message
        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    /// <summary>
    /// Lists all items in the cache.
    /// </summary>
    /// <param name="cacheRelativePath">Cache directory (relative to git root).</param>
    /// <returns>List of cache items.</returns>
    public async Task<List<CacheItem>> ListCacheAsync(string? cacheRelativePath = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;

        var cacheAbsolutePath = _pathResolver.Resolve(cacheRelativePath);

        if (!Directory.Exists(cacheAbsolutePath))
        {
            _logger.LogWarning("Cache directory does not exist: {Path}", cacheAbsolutePath);
            return new List<CacheItem>();
        }

        var items = new List<CacheItem>();

        // List packages
        var packageFiles = Directory.GetFiles(cacheAbsolutePath, "*.tgz");
        foreach (var file in packageFiles)
        {
            var (name, version) = ParsePackageFileName(Path.GetFileName(file));
            var item = await CreateCacheItemAsync(file, CacheItemType.UnityPackage, name, version);
            items.Add(item);
        }

        // List assemblies
        var assemblyFiles = Directory.GetFiles(cacheAbsolutePath, "*.dll");
        foreach (var file in assemblyFiles)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var item = await CreateCacheItemAsync(file, CacheItemType.Assembly, name, null);
            items.Add(item);
        }

        _logger.LogDebug("Cache contains {Count} items", items.Count);

        return items;
    }

    /// <summary>
    /// Cleans the cache by removing all items.
    /// </summary>
    /// <param name="cacheRelativePath">Cache directory (relative to git root).</param>
    /// <returns>Number of items removed.</returns>
    public int CleanCache(string? cacheRelativePath = null)
    {
        cacheRelativePath ??= DefaultCacheDirectory;

        var cacheAbsolutePath = _pathResolver.Resolve(cacheRelativePath);

        if (!Directory.Exists(cacheAbsolutePath))
        {
            _logger.LogWarning("Cache directory does not exist: {Path}", cacheAbsolutePath);
            return 0;
        }

        var files = Directory.GetFiles(cacheAbsolutePath);
        var count = files.Length;

        foreach (var file in files)
        {
            File.Delete(file);
            _logger.LogDebug("Deleted cache file: {File}", file);
        }

        _logger.LogInformation("Cache cleaned: {Count} items removed", count);

        // Publish message
        _cacheCleanedPublisher.Publish(new CacheCleanedMessage(count));

        return count;
    }

    private async Task<CacheItem> AddPackageToCacheAsync(
        string sourceFilePath,
        string cacheDirectory,
        string source,
        PreparationConfig? config)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var targetPath = Path.Combine(cacheDirectory, fileName);

        // Copy to cache
        File.Copy(sourceFilePath, targetPath, overwrite: true);

        // Parse package name and version
        var (name, version) = ParsePackageFileName(fileName);

        // Create cache item
        var cacheItem = await CreateCacheItemAsync(targetPath, CacheItemType.UnityPackage, name, version, source);

        // Auto-update config if provided
        if (config != null)
        {
            var packageRef = new UnityPackageReference
            {
                Name = name,
                Version = version,
                Source = _pathResolver.MakeRelative(targetPath),
                Target = $"projects/client/Packages/{fileName}"
            };
            _configService.AddPackage(config, packageRef);
        }

        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    private async Task<CacheItem?> AddPackageDirectoryToCacheAsync(
        string sourceDirectoryPath,
        string cacheDirectory,
        string source,
        PreparationConfig? config)
    {
        var dirName = Path.GetFileName(sourceDirectoryPath);

        // Parse Unity package directory name (e.g., "com.cysharp.messagepipe@fb95a3138269")
        var (name, hash) = ParseUnityPackageDirectoryName(dirName);
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogWarning("Could not parse package directory name: {DirName}", dirName);
            return null;
        }

        // Read package.json to get version
        var packageJsonPath = Path.Combine(sourceDirectoryPath, "package.json");
        string? version = null;
        if (File.Exists(packageJsonPath))
        {
            try
            {
                var packageJson = await File.ReadAllTextAsync(packageJsonPath);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(packageJson);
                if (jsonDoc.RootElement.TryGetProperty("version", out var versionElement))
                {
                    version = versionElement.GetString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read version from package.json: {Path}", packageJsonPath);
            }
        }

        // Target directory in cache
        var targetPath = Path.Combine(cacheDirectory, name);

        // Copy directory to cache
        CopyDirectory(sourceDirectoryPath, targetPath, overwrite: true);

        // For directory-based cache items, we'll use the directory size
        var dirInfo = new DirectoryInfo(targetPath);
        var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

        // Create cache item (using directory path, not a single file)
        var cacheItem = new CacheItem
        {
            Type = CacheItemType.UnityPackage,
            Name = name,
            Version = version,
            Path = _pathResolver.MakeRelative(targetPath),
            Size = totalSize,
            Hash = null, // Directory hash would be complex, skip for now
            AddedDate = DateTime.UtcNow,
            Source = source
        };

        // Auto-update config if provided
        if (config != null)
        {
            var packageRef = new UnityPackageReference
            {
                Name = name,
                Version = version ?? "unknown",
                Source = _pathResolver.MakeRelative(targetPath),
                Target = $"projects/client/Packages/{name}"
            };
            _configService.AddPackage(config, packageRef);
        }

        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    private async Task<CacheItem?> AddAssemblyDirectoryToCacheAsync(
        string sourceDirectoryPath,
        string cacheDirectory,
        string source,
        PreparationConfig? config)
    {
        var dirName = Path.GetFileName(sourceDirectoryPath);

        // Parse assembly directory name (e.g., "CliWrap.3.8.2" -> name: "CliWrap", version: "3.8.2")
        var (name, version) = ParseAssemblyDirectoryName(dirName);
        if (string.IsNullOrEmpty(name))
        {
            _logger.LogWarning("Could not parse assembly directory name: {DirName}", dirName);
            return null;
        }

        // Target directory in cache
        var targetPath = Path.Combine(cacheDirectory, dirName);

        // Copy directory to cache
        CopyDirectory(sourceDirectoryPath, targetPath, overwrite: true);

        // For directory-based cache items, we'll use the directory size
        var dirInfo = new DirectoryInfo(targetPath);
        var totalSize = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

        // Create cache item
        var cacheItem = new CacheItem
        {
            Type = CacheItemType.Assembly,
            Name = name,
            Version = version,
            Path = _pathResolver.MakeRelative(targetPath),
            Size = totalSize,
            Hash = null, // Directory hash would be complex, skip for now
            AddedDate = DateTime.UtcNow,
            Source = source
        };

        // Auto-update config if provided
        if (config != null)
        {
            var assemblyRef = new AssemblyReference
            {
                Name = name,
                Version = version,
                Source = _pathResolver.MakeRelative(targetPath),
                Target = $"projects/client/Assets/Plugins/{dirName}"
            };
            _configService.AddAssembly(config, assemblyRef);
        }

        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    private async Task<CacheItem> AddAssemblyToCacheAsync(
        string sourceFilePath,
        string cacheDirectory,
        string source,
        PreparationConfig? config)
    {
        var fileName = Path.GetFileName(sourceFilePath);
        var targetPath = Path.Combine(cacheDirectory, fileName);

        // Copy to cache
        File.Copy(sourceFilePath, targetPath, overwrite: true);

        var name = Path.GetFileNameWithoutExtension(fileName);

        // Create cache item
        var cacheItem = await CreateCacheItemAsync(targetPath, CacheItemType.Assembly, name, null, source);

        // Auto-update config if provided
        if (config != null)
        {
            var assemblyRef = new AssemblyReference
            {
                Name = name,
                Source = _pathResolver.MakeRelative(targetPath),
                Target = $"projects/client/Assets/Plugins/{fileName}"
            };
            _configService.AddAssembly(config, assemblyRef);
        }

        _cacheItemAddedPublisher.Publish(new CacheItemAddedMessage(cacheItem));

        return cacheItem;
    }

    private async Task<CacheItem> CreateCacheItemAsync(
        string filePath,
        CacheItemType type,
        string name,
        string? version,
        string? source = null)
    {
        var fileInfo = new FileInfo(filePath);
        var hash = await ComputeFileHashAsync(filePath);

        return new CacheItem
        {
            Type = type,
            Name = name,
            Version = version,
            Path = _pathResolver.MakeRelative(filePath),
            Size = fileInfo.Length,
            Hash = hash,
            AddedDate = DateTime.UtcNow,
            Source = source
        };
    }

    private async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream);
        return Convert.ToHexString(hashBytes);
    }

    private (string name, string version) ParsePackageFileName(string fileName)
    {
        // Expected format: com.unity.addressables-1.21.2.tgz
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var lastDashIndex = nameWithoutExtension.LastIndexOf('-');

        if (lastDashIndex > 0)
        {
            var name = nameWithoutExtension.Substring(0, lastDashIndex);
            var version = nameWithoutExtension.Substring(lastDashIndex + 1);
            return (name, version);
        }

        // Fallback if no version found
        return (nameWithoutExtension, "unknown");
    }

    private (string name, string hash) ParseUnityPackageDirectoryName(string dirName)
    {
        // Expected format: com.cysharp.messagepipe@fb95a3138269
        var atIndex = dirName.IndexOf('@');

        if (atIndex > 0)
        {
            var name = dirName.Substring(0, atIndex);
            var hash = dirName.Substring(atIndex + 1);
            return (name, hash);
        }

        // Fallback if no @ found - treat entire name as package name
        return (dirName, string.Empty);
    }

    private (string name, string? version) ParseAssemblyDirectoryName(string dirName)
    {
        // Expected format: CliWrap.3.8.2 or MessagePack.3.1.3
        // Find the last dot followed by a number (version separator)
        var lastDotIndex = dirName.LastIndexOf('.');

        if (lastDotIndex > 0 && lastDotIndex < dirName.Length - 1)
        {
            var afterDot = dirName.Substring(lastDotIndex + 1);
            // Check if what follows the dot starts with a digit (version number)
            if (char.IsDigit(afterDot[0]))
            {
                // Find where the version starts (could be Major.Minor.Patch)
                var versionStartIndex = lastDotIndex;
                for (int i = lastDotIndex - 1; i >= 0; i--)
                {
                    if (dirName[i] == '.' && i > 0 && char.IsDigit(dirName[i + 1]))
                    {
                        versionStartIndex = i;
                    }
                    else if (!char.IsDigit(dirName[i]) && dirName[i] != '.')
                    {
                        break;
                    }
                }

                var name = dirName.Substring(0, versionStartIndex);
                var version = dirName.Substring(versionStartIndex + 1);
                return (name, version);
            }
        }

        // Fallback if no version found - treat entire name as assembly name
        return (dirName, null);
    }

    private void CopyDirectory(string sourceDir, string targetDir, bool overwrite = false)
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
}
