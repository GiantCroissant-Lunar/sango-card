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

        // Find assemblies (.dll files)
        var assemblyFiles = Directory.GetFiles(sourceAbsolutePath, "*.dll", SearchOption.AllDirectories);
        foreach (var assemblyFile in assemblyFiles)
        {
            var item = await AddAssemblyToCacheAsync(assemblyFile, cacheAbsolutePath, sourceRelativePath, config);
            addedItems.Add(item);
        }

        _logger.LogInformation(
            "Cache populated: {PackageCount} packages, {AssemblyCount} assemblies",
            packageFiles.Length,
            assemblyFiles.Length
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
}
