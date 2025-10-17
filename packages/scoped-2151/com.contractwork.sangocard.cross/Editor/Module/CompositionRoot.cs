using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SangoCard.Cross.Editor.Module;

#if SANGOCARD_PURE_DI
using Pure.DI;

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
                }
                catch (Exception)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
            })
        )
        .Root<ILoggerFactory>("LoggerFactory");
}
#else
/// <summary>
/// Fallback CompositionRoot without Pure.DI. Provides minimal Resolve<T> support.
/// </summary>
internal class CompositionRoot
{
    public T? Resolve<T>()
    {
        if (typeof(T) == typeof(ILoggerFactory))
        {
            var factory = LoggerFactory.Create(builder =>
            {
                try
                {
                    // Optional: load configuration if available, but avoid requiring Logging.Configuration extensions
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Path.GetFullPath(Path.Combine(Define.PackageName, "Package Resources", "settings")))
                        .AddJsonFile("appsettings.json", optional: true)
                        .Build();

                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                catch
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
            });
            return (T)(object)factory;
        }

        return default;
    }
}
#endif
