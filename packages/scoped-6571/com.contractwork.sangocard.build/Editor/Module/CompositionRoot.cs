using System;
using System.Linq;
using Pure.DI;

namespace SangoCard.Build.Editor.Module;

// using Plate.BuildAssistant.Editor.ProjectSettings;

/// <summary>
/// Pure.DI composition root for the SangoCard Tool module
/// This generates optimized DI code at compile time
/// </summary>
internal partial class CompositionRoot
{
    // RFC010: Temporarily disabled Pure.DI setup due to compilation errors
    // TODO: Re-enable once SettingsViewModel dependencies are resolved
    /*
    // Pure.DI setup - this generates code at compile time
    // RFC010: Conditional compilation for DI setup based on ReactiveUI availability
    #if HAS_NUGET_SYSTEM_REACTIVE
    private static void Setup() => DI.Setup(nameof(CompositionRoot))
        // Bind services as singletons
        // .Bind<IPackageService>().As(Lifetime.Singleton).To<PackageService>()
        // .Bind<IExportService>().As(Lifetime.Singleton).To<ExportService>()
        // Bind ViewModels as transient (new instance each time)
        .Bind<SettingsViewModel>().As(Lifetime.Transient).To<SettingsViewModel>()
        // Define roots for what we want to resolve
        // .Root<IPackageService>("PackageService")
        // .Root<IExportService>("ExportService")
        .Root<SettingsViewModel>("SettingsViewModel");
    #else
    // Placeholder setup when ReactiveUI is not available
    private static void Setup() => DI.Setup(nameof(CompositionRoot))
        // No bindings when dependencies are not available
        .Root<object>("PlaceholderRoot");
    #endif
    */

    // Minimal setup to avoid Pure.DI errors
    private static void Setup() => DI
        .Setup(nameof(CompositionRoot))
        .Bind<string>().To(_ => "PlaceholderService")
        .Root<string>("PlaceholderRoot");
}
