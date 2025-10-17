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
}
