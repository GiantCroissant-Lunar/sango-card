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

        // config add-source
        var addSourceCommand = new Command("add-source", "Add a source to preparation manifest (Phase 1)");
        var sourcePathOption = new Option<string>(
            aliases: new[] { "--source", "-s" },
            description: "Source path (absolute or relative to git root)"
        ) { IsRequired = true };
        var cacheAsOption = new Option<string>(
            aliases: new[] { "--cache-as", "-n" },
            description: "Name to use in cache directory"
        ) { IsRequired = true };
        var typeOption = new Option<string>(
            aliases: new[] { "--type", "-t" },
            description: "Type: package, assembly, or asset"
        ) { IsRequired = true };
        var manifestOption = new Option<string>(
            aliases: new[] { "--manifest", "-m" },
            description: "Preparation manifest file path (relative to git root)"
        ) { IsRequired = true };
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            getDefaultValue: () => false,
            description: "Preview without applying changes"
        );

        addSourceCommand.AddOption(sourcePathOption);
        addSourceCommand.AddOption(cacheAsOption);
        addSourceCommand.AddOption(typeOption);
        addSourceCommand.AddOption(manifestOption);
        addSourceCommand.AddOption(dryRunOption);
        addSourceCommand.SetHandler(async (string source, string cacheAs, string type, string manifest, bool dryRun) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.AddSourceAsync(source, cacheAs, type, manifest, dryRun);
        }, sourcePathOption, cacheAsOption, typeOption, manifestOption, dryRunOption);

        // config add-injection
        var addInjectionCommand = new Command("add-injection", "Add injection mapping to build config (Phase 2)");
        var injSourceOption = new Option<string>(
            aliases: new[] { "--source", "-s" },
            description: "Source path in cache (relative to git root)"
        ) { IsRequired = true };
        var injTargetOption = new Option<string>(
            aliases: new[] { "--target", "-t" },
            description: "Target path in client project (relative to git root)"
        ) { IsRequired = true };
        var injTypeOption = new Option<string>(
            aliases: new[] { "--type" },
            description: "Type: package, assembly, or asset"
        ) { IsRequired = true };
        var injConfigOption = new Option<string>(
            aliases: new[] { "--config", "-c" },
            description: "Build injection config file path (relative to git root)"
        ) { IsRequired = true };
        var injNameOption = new Option<string?>(
            aliases: new[] { "--name", "-n" },
            description: "Item name (auto-detected if omitted)"
        );
        var injVersionOption = new Option<string?>(
            aliases: new[] { "--version", "-v" },
            description: "Item version (optional)"
        );
        var injDryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            getDefaultValue: () => false,
            description: "Preview without applying changes"
        );

        addInjectionCommand.AddOption(injSourceOption);
        addInjectionCommand.AddOption(injTargetOption);
        addInjectionCommand.AddOption(injTypeOption);
        addInjectionCommand.AddOption(injConfigOption);
        addInjectionCommand.AddOption(injNameOption);
        addInjectionCommand.AddOption(injVersionOption);
        addInjectionCommand.AddOption(injDryRunOption);
        addInjectionCommand.SetHandler(async (string source, string target, string type, string config, string? name, string? version, bool dryRun) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.AddInjectionAsync(source, target, type, config, name, version, dryRun);
        }, injSourceOption, injTargetOption, injTypeOption, injConfigOption, injNameOption, injVersionOption, injDryRunOption);

        // config add-batch
        var addBatchCommand = new Command("add-batch", "Add multiple items from batch manifest file");
        var batchManifestOption = new Option<string>(
            aliases: new[] { "--manifest", "-m" },
            description: "Batch manifest file path (YAML or JSON)"
        ) { IsRequired = true };
        var batchOutputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output config file path (relative to git root)"
        ) { IsRequired = true };
        var batchConfigTypeOption = new Option<string>(
            aliases: new[] { "--config-type", "-t" },
            description: "Config type: 'source' (Phase 1) or 'injection' (Phase 2)"
        ) { IsRequired = true };
        var batchDryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            getDefaultValue: () => false,
            description: "Preview without applying changes"
        );
        var batchContinueOnErrorOption = new Option<bool>(
            aliases: new[] { "--continue-on-error" },
            getDefaultValue: () => false,
            description: "Continue processing on failures"
        );

        addBatchCommand.AddOption(batchManifestOption);
        addBatchCommand.AddOption(batchOutputOption);
        addBatchCommand.AddOption(batchConfigTypeOption);
        addBatchCommand.AddOption(batchDryRunOption);
        addBatchCommand.AddOption(batchContinueOnErrorOption);
        addBatchCommand.SetHandler(async (string manifest, string output, string configType, bool dryRun, bool continueOnError) =>
        {
            var handler = _host.Services.GetRequiredService<ConfigCommandHandler>();
            await handler.AddBatchAsync(manifest, output, configType, dryRun, continueOnError);
        }, batchManifestOption, batchOutputOption, batchConfigTypeOption, batchDryRunOption, batchContinueOnErrorOption);

        configCommand.AddCommand(createCommand);
        configCommand.AddCommand(validateCommand);
        configCommand.AddCommand(addSourceCommand);
        configCommand.AddCommand(addInjectionCommand);
        configCommand.AddCommand(addBatchCommand);

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

        // NEW: inject command (recommended - two-phase workflow)
        var inject = new Command("inject", "Inject preparation into target project (Phase 2)");
        var configOption = new Option<string>(
            aliases: new[] { "--config", "-c" },
            description: "Configuration file path (relative to git root)"
        ) { IsRequired = true };
        var targetOption = new Option<string>(
            aliases: new[] { "--target", "-t" },
            description: "Target project path (must be 'projects/client/' per R-BLD-060)"
        ) { IsRequired = true };
        var stageOption = new Option<string?>(
            aliases: new[] { "--stage", "-s" },
            description: "Injection stage (for v2.0 multi-stage configs: preTest, preBuild, postBuild, preNativeBuild, postNativeBuild)"
        );
        var platformOption = new Option<string?>(
            aliases: new[] { "--platform", "-p" },
            description: "Platform for stage execution (e.g., ios, android)"
        );
        var levelOption = new Option<string>(
            aliases: new[] { "--level", "-l" },
            getDefaultValue: () => "Full",
            description: "Validation level before running: Schema, FileExistence, UnityPackages, Full"
        );
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "Show detailed output"
        );
        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            getDefaultValue: () => false,
            description: "Proceed despite validation errors"
        );

        inject.AddOption(configOption);
        inject.AddOption(targetOption);
        inject.AddOption(stageOption);
        inject.AddOption(platformOption);
        inject.AddOption(levelOption);
        inject.AddOption(verboseOption);
        inject.AddOption(forceOption);
        inject.SetHandler(async (string config, string target, string? stage, string? platform, string level, bool verbose, bool force) =>
        {
            var handler = _host.Services.GetRequiredService<PrepareCommandHandler>();
            await handler.InjectAsync(config, target, stage, platform, level, verbose, force);
        }, configOption, targetOption, stageOption, platformOption, levelOption, verboseOption, forceOption);

        // DEPRECATED: run command (backward compatibility)
        var run = new Command("run", "[DEPRECATED] Use 'inject --target projects/client/' instead");
        var runConfigOption = new Option<string>(
            aliases: new[] { "--config", "-c" },
            description: "Configuration file path (relative to git root)"
        ) { IsRequired = true };
        var runLevelOption = new Option<string>(
            aliases: new[] { "--level", "-l" },
            getDefaultValue: () => "Full",
            description: "Validation level before running: Schema, FileExistence, UnityPackages, Full"
        );

        run.AddOption(runConfigOption);
        run.AddOption(runLevelOption);
        run.SetHandler(async (string config, string level) =>
        {
            Console.WriteLine("⚠️  WARNING: 'prepare run' is deprecated.");
            Console.WriteLine("   Use 'prepare inject --target projects/client/' instead.");
            Console.WriteLine("   This command will be removed in the next major version.");
            Console.WriteLine();

            var handler = _host.Services.GetRequiredService<PrepareCommandHandler>();
            await handler.InjectAsync(config, "projects/client/", level, verbose: false, force: false);
        }, runConfigOption, runLevelOption);

        prepare.AddCommand(inject);
        prepare.AddCommand(run);
        return prepare;
    }
}
