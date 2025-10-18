using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;

namespace SangoCard.Build.Tool.Cli.Commands;

/// <summary>
/// Handles cache-related CLI commands.
/// </summary>
public class CacheCommandHandler
{
    private readonly CacheService _cacheService;
    private readonly ConfigService _configService;
    private readonly PathResolver _paths;
    private readonly ILogger<CacheCommandHandler> _logger;

    public CacheCommandHandler(
        CacheService cacheService,
        ConfigService configService,
        PathResolver paths,
        ILogger<CacheCommandHandler> logger)
    {
        _cacheService = cacheService;
        _configService = configService;
        _paths = paths;
        _logger = logger;
    }

    public async Task PopulateAsync(string sourceRelativePath, string? configRelativePath)
    {
        PreparationConfig? config = null;
        if (!string.IsNullOrWhiteSpace(configRelativePath))
        {
            config = await _configService.LoadAsync(configRelativePath);

            // If config is provided, populate from all source directories defined in the config
            var allItems = await _cacheService.PopulateFromConfigAsync(config, CacheService.DefaultCacheDirectory);

            Console.WriteLine($"Populated cache with {allItems.Count} item(s) from config sources");

            await _configService.SaveAsync(config, configRelativePath);
            Console.WriteLine($"Updated config: {_paths.Resolve(configRelativePath)}");
        }
        else
        {
            // Legacy behavior: populate from a single source directory
            var items = await _cacheService.PopulateFromDirectoryAsync(sourceRelativePath, CacheService.DefaultCacheDirectory, null);
            Console.WriteLine($"Populated cache with {items.Count} item(s) from: {_paths.Resolve(sourceRelativePath)}");
        }
    }

    public async Task ListAsync()
    {
        var items = await _cacheService.ListCacheAsync();
        Console.WriteLine($"Cache contains {items.Count} item(s)");
        foreach (var item in items)
        {
            Console.WriteLine($"- {item.Type}: {item.Name}{(string.IsNullOrEmpty(item.Version) ? string.Empty : $"@{item.Version}")}, {item.Path}, {item.Size} bytes");
        }
    }

    public Task CleanAsync()
    {
        var count = _cacheService.CleanCache();
        Console.WriteLine($"Removed {count} item(s) from cache");
        return Task.CompletedTask;
    }

    public async Task AddPackageAsync(string configRelativePath, string name, string version, string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            Console.Error.WriteLine("Error: Config path is required");
            Environment.ExitCode = 1;
            return;
        }

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

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.Error.WriteLine("Error: Source file path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (!File.Exists(sourceFilePath))
        {
            Console.Error.WriteLine($"Error: Source file not found: {sourceFilePath}");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);
        var cacheItem = await _cacheService.AddPackageAsync(config, name, version, sourceFilePath);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added package to cache: {name}@{version}");
        Console.WriteLine($"  Cache path: {_paths.Resolve(cacheItem.Path)}");
        Console.WriteLine($"  Size: {cacheItem.Size} bytes");
        Console.WriteLine($"Updated config: {_paths.Resolve(configRelativePath)}");
    }

    public async Task AddAssemblyAsync(string configRelativePath, string name, string sourceFilePath, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(configRelativePath))
        {
            Console.Error.WriteLine("Error: Config path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Error: Assembly name is required");
            Environment.ExitCode = 1;
            return;
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.Error.WriteLine("Error: Source file path is required");
            Environment.ExitCode = 1;
            return;
        }

        if (!File.Exists(sourceFilePath))
        {
            Console.Error.WriteLine($"Error: Source file not found: {sourceFilePath}");
            Environment.ExitCode = 1;
            return;
        }

        var config = await _configService.LoadAsync(configRelativePath);
        var cacheItem = await _cacheService.AddAssemblyAsync(config, name, version, sourceFilePath);
        await _configService.SaveAsync(config, configRelativePath);

        Console.WriteLine($"Added assembly to cache: {name}");
        if (!string.IsNullOrEmpty(version))
        {
            Console.WriteLine($"  Version: {version}");
        }
        Console.WriteLine($"  Cache path: {_paths.Resolve(cacheItem.Path)}");
        Console.WriteLine($"  Size: {cacheItem.Size} bytes");
        Console.WriteLine($"Updated config: {_paths.Resolve(configRelativePath)}");
    }
}
