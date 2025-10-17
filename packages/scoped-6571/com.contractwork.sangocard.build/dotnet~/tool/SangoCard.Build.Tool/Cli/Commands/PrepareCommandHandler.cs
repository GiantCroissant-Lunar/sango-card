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

    public async Task RunAsync(string configRelativePath, string validationLevel)
    {
        var config = await _configService.LoadAsync(configRelativePath);

        var level = ParseLevel(validationLevel);
        var validation = _validationService.Validate(config, level);
        Console.WriteLine(validation.Summary);
        if (!validation.IsValid)
        {
            foreach (var e in validation.Errors)
            {
                Console.WriteLine($"- [{e.Code}] {e.Message}{(string.IsNullOrEmpty(e.File) ? string.Empty : $" ({e.File})")}");
            }
            Environment.ExitCode = 2;
            return;
        }

        var result = await _preparationService.RunAsync(config, configRelativePath);
        Console.WriteLine($"Preparation complete: copied={result.Copied}, moved={result.Moved}, deleted={result.Deleted}, patched={result.Patched}");
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
