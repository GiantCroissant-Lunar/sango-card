
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SangoCard.Build.Editor.Module;

using SangoCard.Cross.Editor.Module.Attributes;

// ReSharper disable once UnusedType.Global
internal partial class Bootstrap
{
    private static ILogger _logger = new NullLogger<Bootstrap>();

    // ReSharper disable once UnusedMember.Global
    [OrderedEditorModulePhase(6571010, OrderedEditorModulePhaseAttribute.Phase.LoadBegin)]
    public static void OnEditorLoadBegin()
    {
        // Get logger from shared module's registered ILoggerFactory
        InitializeLogger();

        Log.LogDebug("AssetInOut Tool Module Initialized - Setting up Pure.DI + Splat");
        Initialize();

        Log.LogDebug("AssetInOut Bootstrap initialization completed");
    }

    // ReSharper disable once UnusedMember.Global
    [OrderedEditorModulePhase(6571010, OrderedEditorModulePhaseAttribute.Phase.UnloadBegin)]
    public static void OnEditorUnloadBegin()
    {
        Log.LogDebug("AssetInOut Tool Module Unloading - Cleaning up services");
        ResetServices();

        Log.LogDebug("AssetInOut Tool Module Unloading completed");
    }

    private static ILogger Log => _logger;

    private static void InitializeLogger()
    {
        try
        {
            // Get logger factory from shared module (should already be registered)
            var loggerFactory = Splat.Locator.Current.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<Bootstrap>() ?? new NullLogger<Bootstrap>();
        }
        catch (Exception ex)
        {
            // If shared module's logger factory isn't available, fall back to NullLogger
            _logger = new NullLogger<Bootstrap>();
            UnityEngine.Debug.LogWarning($"Failed to get ILoggerFactory from shared module: {ex.Message}");
        }
    }

    private static void Initialize()
    {
        try
        {
            Log.LogDebug("Starting Pure.DI composition root initialization...");

            // Reset any existing composition root on domain reload
            ResetServices();

            Log.LogDebug("Creating Pure.DI composition root...");

            // Create Pure.DI composition root and store it in Splat
            var compositionRoot = new CompositionRoot();
            Splat.Locator.CurrentMutable.RegisterConstant(compositionRoot, typeof(CompositionRoot));

            Log.LogDebug("??Pure.DI composition root registered in Splat successfully");

            // Verify composition root registration
            VerifyCompositionRoot();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Failed to initialize Pure.DI composition root");
            Log.LogError("Stack trace: {StackTrace}", ex.StackTrace);
        }
    }

    private static void ResetServices()
    {
        var mutable = Locator.CurrentMutable;
        mutable.UnregisterCurrent<CompositionRoot>();
        // Note: Don't reset ILoggerFactory - that belongs to the shared module
    }

    private static void VerifyCompositionRoot()
    {
        var verifyRoot = Splat.Locator.Current.GetService<CompositionRoot>();
        if (verifyRoot == null)
        {
            Log.LogWarning("?��? CompositionRoot registration may have failed");
            return;
        }

        Log.LogDebug("??CompositionRoot is ready for dependency resolution");

        // Test individual service resolution - let any exceptions bubble up to main Initialize method
        // var packageService = verifyRoot.PackageService;
        // var exportService = verifyRoot.ExportService;
        Log.LogDebug("??Services can be resolved from CompositionRoot");
    }
}
