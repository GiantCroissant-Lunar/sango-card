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
        }

        var items = await _cacheService.PopulateFromDirectoryAsync(sourceRelativePath, CacheService.DefaultCacheDirectory, config);

        Console.WriteLine($"Populated cache with {items.Count} item(s) from: {_paths.Resolve(sourceRelativePath)}");

        if (config != null && !string.IsNullOrWhiteSpace(configRelativePath))
        {
            await _configService.SaveAsync(config, configRelativePath);
            Console.WriteLine($"Updated config: {_paths.Resolve(configRelativePath)}");
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
}
