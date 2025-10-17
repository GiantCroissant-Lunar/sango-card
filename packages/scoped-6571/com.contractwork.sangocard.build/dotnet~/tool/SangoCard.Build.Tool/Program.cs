using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SangoCard.Build.Tool;

/// <summary>
/// Entry point for the Sango Card Build Preparation Tool.
/// </summary>
internal class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code (0 for success, non-zero for failure).</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configure services using extension methods
            builder.Services.AddBuildToolServices(args);

            // Configure logging using extension methods
            builder.Logging.ConfigureBuildToolLogging(args);

            var host = builder.Build();

            // Log startup
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Sango Card Build Preparation Tool v{Version}", "1.0.0");
            logger.LogDebug("Git Root Path Resolution: Enabled");
            logger.LogDebug("MessagePipe: Configured");

            // Determine mode and run appropriate host
            var mode = DetermineMode(args);
            logger.LogInformation("Running in {Mode} mode", mode);

            return mode switch
            {
                ToolMode.Tui => await RunTuiMode(host),
                ToolMode.Cli => await RunCliMode(host, args),
                _ => throw new InvalidOperationException($"Unknown mode: {mode}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
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

    private static async Task<int> RunCliMode(IHost host, string[] args)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        try
        {
            var cli = host.Services.GetRequiredService<SangoCard.Build.Tool.Cli.CliHost>();
            return await cli.RunAsync(args);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CLI execution failed");
            return 1;
        }
    }

    private static async Task<int> RunTuiMode(IHost host)
    {
        // TUI mode will be implemented in Task 5.1
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("TUI mode - Not yet implemented");

        Console.WriteLine("Sango Card Build Preparation Tool - TUI Mode");
        Console.WriteLine("TUI mode - Coming soon!");

        return 0;
    }
}

/// <summary>
/// Tool execution mode.
/// </summary>
internal enum ToolMode
{
    /// <summary>
    /// Command-line interface mode (non-interactive).
    /// </summary>
    Cli,

    /// <summary>
    /// Terminal user interface mode (interactive).
    /// </summary>
    Tui
}
