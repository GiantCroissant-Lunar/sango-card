using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pure.DI;

namespace SangoCard.Cross.Editor.Module;

/// <summary>
/// Pure.DI composition root for the SangoCard Tool module
/// This generates optimized DI code at compile time
/// </summary>
internal partial class CompositionRoot
{
    private static readonly string SettingsBaseDirectory = Path.Combine(
        Define.PackageName,
        "Package Resources",
        "settings");

    // Minimal setup to avoid Pure.DI errors
    private static void Setup() => DI
        .Setup(nameof(CompositionRoot))
        // Bind services as singletons
        .Bind<ILoggerFactory>()
        .As(Lifetime.Singleton)
        .To<ILoggerFactory>(_ =>
            Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                try
                {
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Path.GetFullPath(SettingsBaseDirectory))
                        .AddJsonFile("appsettings.json")
                        .Build();

                    builder
                        .AddConfiguration(configuration)
                        .SetMinimumLevel(LogLevel.Debug);
                    // Use sinks for logging
                    // .AddProvider(new Plate.Shared.Logging.Providers.UnityConsoleLoggerProvider())
                    // .AddProvider(new Logging.Providers.CliPipeLoggerProvider());
                }
                catch (Exception ex)
                {
                    builder
                        .SetMinimumLevel(LogLevel.Debug);
                    // .AddProvider(new Plate.Shared.Logging.Providers.UnityConsoleLoggerProvider())
                    // .AddProvider(new Logging.Providers.CliPipeLoggerProvider());
                }
            })
        )
        .Root<ILoggerFactory>("LoggerFactory");
}
