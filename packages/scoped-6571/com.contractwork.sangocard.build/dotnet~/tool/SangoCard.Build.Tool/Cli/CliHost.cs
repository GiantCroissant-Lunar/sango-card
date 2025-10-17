using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SangoCard.Build.Tool.Cli.Commands;

namespace SangoCard.Build.Tool.Cli;

/// <summary>
/// CLI host for command-line interface mode.
/// </summary>
public class CliHost
{
    private readonly IHost _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliHost"/> class.
    /// </summary>
    public CliHost(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Runs the CLI with the provided arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> RunAsync(string[] args)
    {
        var rootCommand = new RootCommand("Sango Card Build Preparation Tool");

        // Add commands
        rootCommand.AddCommand(CreateConfigCommand());
        rootCommand.AddCommand(CreateCacheCommand());
        rootCommand.AddCommand(CreateValidateCommand());
        rootCommand.AddCommand(CreatePrepareCommand());

        return await rootCommand.InvokeAsync(args);
    }

    private Command CreateConfigCommand()
    {
        var configCommand = new Command("config", "Manage preparation configuration");

        // config create
        var createCommand = new Command("create", "Create a new configuration file");
        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path (relative to git root)"
        ) { IsRequired = true };
        var descriptionOption = new Option<string?>(
            aliases: new[] { "--description", "-d" },
            description: "Configuration description"
        );

        createCommand.AddOption(outputOption);
        createCommand.AddOption(descriptionOption);
        createCommand.SetHandler(async (string output, string? description) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.CreateAsync(output, description);
        }, outputOption, descriptionOption);

        // config validate
        var validateCommand = new Command("validate", "Validate a configuration file");
        var fileOption = new Option<string>(
            aliases: new[] { "--file", "-f" },
            description: "Configuration file path (relative to git root)"
        ) { IsRequired = true };
        var levelOption = new Option<string>(
            aliases: new[] { "--level", "-l" },
            getDefaultValue: () => "Full",
            description: "Validation level: Schema, FileExistence, UnityPackages, Full"
        );

        validateCommand.AddOption(fileOption);
        validateCommand.AddOption(levelOption);
        validateCommand.SetHandler(async (string file, string level) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.ValidateAsync(file, level);
        }, fileOption, levelOption);

        configCommand.AddCommand(createCommand);
        configCommand.AddCommand(validateCommand);

        return configCommand;
    }

    private Command CreateCacheCommand()
    {
        var cacheCommand = new Command("cache", "Manage preparation cache");

        // cache populate
        var populateCommand = new Command("populate", "Populate cache from source directory");
        var sourceOption = new Option<string>(
            aliases: new[] { "--source", "-s" },
            description: "Source directory path (relative to git root)"
        ) { IsRequired = true };
        var configOption = new Option<string?>(
            aliases: new[] { "--config", "-c" },
            description: "Configuration file to update (optional)"
        );

        populateCommand.AddOption(sourceOption);
        populateCommand.AddOption(configOption);
        populateCommand.SetHandler(async (string source, string? config) =>
        {
            var handler = _host.Services.GetRequiredService<CacheCommandHandler>();
            await handler.PopulateAsync(source, config);
        }, sourceOption, configOption);

        // cache list
        var listCommand = new Command("list", "List cache contents");
        listCommand.SetHandler(async () =>
        {
            var handler = _host.Services.GetRequiredService<CacheCommandHandler>();
            await handler.ListAsync();
        });

        // cache clean
        var cleanCommand = new Command("clean", "Clean cache");
        cleanCommand.SetHandler(async () =>
        {
            var handler = _host.Services.GetRequiredService<CacheCommandHandler>();
            await handler.CleanAsync();
        });

        cacheCommand.AddCommand(populateCommand);
        cacheCommand.AddCommand(listCommand);
        cacheCommand.AddCommand(cleanCommand);

        return cacheCommand;
    }

    private Command CreateValidateCommand()
    {
        var validateCommand = new Command("validate", "Validate a configuration file");
        var fileOption = new Option<string>(
            aliases: new[] { "--file", "-f" },
            description: "Configuration file path (relative to git root)"
        ) { IsRequired = true };
        var levelOption = new Option<string>(
            aliases: new[] { "--level", "-l" },
            getDefaultValue: () => "Full",
            description: "Validation level: Schema, FileExistence, UnityPackages, Full"
        );

        validateCommand.AddOption(fileOption);
        validateCommand.AddOption(levelOption);
        validateCommand.SetHandler(async (string file, string level) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.ValidateAsync(file, level);
        }, fileOption, levelOption);

        return validateCommand;
    }

    private Command CreatePrepareCommand()
    {
        var prepare = new Command("prepare", "Execute build preparation");

        var run = new Command("run", "Run preparation from a config file");
        var configOption = new Option<string>(
            aliases: new[] { "--config", "-c" },
            description: "Configuration file path (relative to git root)"
        ) { IsRequired = true };
        var levelOption = new Option<string>(
            aliases: new[] { "--level", "-l" },
            getDefaultValue: () => "Full",
            description: "Validation level before running: Schema, FileExistence, UnityPackages, Full"
        );

        run.AddOption(configOption);
        run.AddOption(levelOption);
        run.SetHandler(async (string config, string level) =>
        {
            var handler = _host.Services.GetRequiredService<PrepareCommandHandler>();
            await handler.RunAsync(config, level);
        }, configOption, levelOption);

        prepare.AddCommand(run);
        return prepare;
    }
}
