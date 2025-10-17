using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SangoCard.Build.Tool;

/// <summary>
/// Extension methods for configuring services in the host builder.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Registers all core services for the build preparation tool.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBuildToolServices(this IServiceCollection services, string[] args)
    {
        // MessagePipe for message-driven architecture
        services.AddMessagePipe(options =>
        {
            options.EnableCaptureStackTrace = false; // Disable for performance
            options.InstanceLifetime = InstanceLifetime.Singleton;
        });

        // Core utilities
        services.AddSingleton<Core.Utilities.GitHelper>();
        services.AddSingleton<Core.Utilities.PathResolver>();

        // Core services
        services.AddSingleton<Core.Services.ConfigService>();
        services.AddSingleton<Core.Services.CacheService>();
        services.AddSingleton<Core.Services.ValidationService>();
        services.AddSingleton<Core.Services.PreparationService>();
        services.AddSingleton<Core.Services.SourceManagementService>();
        services.AddSingleton<Core.Services.BatchManifestService>();
        // services.AddSingleton<ManifestService>(); // Task 2.4

        // Code patchers
        services.AddSingleton<Core.Patchers.IPatcher, Core.Patchers.TextPatcher>();
        services.AddSingleton<Core.Patchers.IPatcher, Core.Patchers.JsonPatcher>();
        services.AddSingleton<Core.Patchers.IPatcher, Core.Patchers.CSharpPatcher>();
        services.AddSingleton<Core.Patchers.IPatcher, Core.Patchers.UnityAssetPatcher>();

        // CLI services
        services.AddSingleton<SangoCard.Build.Tool.Cli.CliHost>();
        services.AddTransient<SangoCard.Build.Tool.Cli.Commands.ConfigCommandHandler>();
        services.AddTransient<SangoCard.Build.Tool.Cli.Commands.CacheCommandHandler>();
        services.AddTransient<SangoCard.Build.Tool.Cli.Commands.PrepareCommandHandler>();

        // Reactive state (will be implemented in Task 5.x)
        // services.AddSingleton<AppState>();
        // services.AddSingleton<ConfigState>();
        // services.AddSingleton<CacheState>();

        // Mode-specific services
        var mode = DetermineMode(args);
        if (mode == ToolMode.Tui)
        {
            // TUI services
            services.AddSingleton<Tui.TuiHost>();
            services.AddTransient<Tui.Views.CacheManagementView>();
            services.AddTransient<Tui.Views.ConfigEditorView>();
            services.AddTransient<Tui.Views.ConfigTypeSelectionView>();
            services.AddTransient<Tui.Views.ValidationView>();
            services.AddTransient<Tui.Views.PreparationExecutionView>();
            services.AddTransient<Tui.Views.ManualSourcesView>();
            services.AddTransient<Tui.Views.PreparationSourcesManagementView>();
        }

        return services;
    }

    /// <summary>
    /// Configures logging for the build preparation tool.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The logging builder for chaining.</returns>
    public static ILoggingBuilder ConfigureBuildToolLogging(this ILoggingBuilder logging, string[] args)
    {
        logging.ClearProviders();
        logging.AddConsole(options =>
        {
            options.FormatterName = "simple";
        });

        // Set log level based on verbosity flag
        var logLevel = LogLevel.Information;
        if (args.Contains("--verbose") || args.Contains("-v"))
        {
            logLevel = LogLevel.Debug;
        }
        else if (args.Contains("--quiet") || args.Contains("-q"))
        {
            logLevel = LogLevel.Warning;
        }

        logging.SetMinimumLevel(logLevel);

        return logging;
    }

    private static ToolMode DetermineMode(string[] args)
    {
        // Check for --mode argument
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--mode" || args[i] == "-m")
            {
                return args[i + 1].ToLowerInvariant() switch
                {
                    "tui" => ToolMode.Tui,
                    "cli" => ToolMode.Cli,
                    _ => ToolMode.Cli
                };
            }
        }

        // Check for tui command
        if (args.Length > 0 && args[0].Equals("tui", StringComparison.OrdinalIgnoreCase))
        {
            return ToolMode.Tui;
        }

        // Default to CLI mode
        return ToolMode.Cli;
    }
}
