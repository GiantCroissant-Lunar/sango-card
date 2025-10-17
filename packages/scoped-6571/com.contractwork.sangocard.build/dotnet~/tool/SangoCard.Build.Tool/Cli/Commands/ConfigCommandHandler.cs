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
    private readonly PathResolver _paths;
    private readonly ILogger<ConfigCommandHandler> _logger;

    public ConfigCommandHandler(
        ConfigService configService,
        ValidationService validationService,
        PathResolver paths,
        ILogger<ConfigCommandHandler> logger)
    {
        _configService = configService;
        _validationService = validationService;
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
}
