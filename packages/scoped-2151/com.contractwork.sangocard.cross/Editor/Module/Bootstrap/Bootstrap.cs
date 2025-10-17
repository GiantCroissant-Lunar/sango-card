using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// ReSharper disable once CheckNamespace
namespace SangoCard.Cross.Editor.Module;

using SangoCard.Cross.Editor.Module.Attributes;

// ReSharper disable once UnusedType.Global
internal partial class Bootstrap
{
    private static ILogger _logger = new NullLogger<Bootstrap>();

    protected Bootstrap()
    {
    }

    // ReSharper disable once UnusedMember.Global
    [OrderedEditorModulePhase(2151010, OrderedEditorModulePhaseAttribute.Phase.LoadBegin)]
    public static void OnEditorLoadBegin()
    {
        Initialize();

        // Upgrade to composition root logger after initialization
        UpgradeLogger();

        Log.LogDebug("Shared Bootstrap initialization completed with upgraded logger");
    }

    // ReSharper disable once UnusedMember.Global
    [OrderedEditorModulePhase(2151010, OrderedEditorModulePhaseAttribute.Phase.UnloadBegin)]
    public static void OnEditorUnloadBegin()
    {
        Log.LogDebug("Shared Module Unloading - Cleaning up services");
        ResetServices();

        Log.LogDebug("Shared Module Unloading completed");
    }

    private static ILogger Log => _logger;

    private static void UpgradeLogger()
    {
        try
        {
            // Now that services should be available, try to get logger from Splat
            var loggerFactory = Splat.Locator.Current.GetService<ILoggerFactory>();
            if (loggerFactory == null)
            {
                return;
            }

            _logger = loggerFactory.CreateLogger<Bootstrap>();
            Log.LogDebug("Successfully upgraded to registered logger factory");
        }
        catch (Exception ex)
        {
            // Log the error with current logger and keep existing logger
            Debug.LogWarning($"Failed to upgrade logger: {ex.Message}. Keeping current logger.");
        }
    }

    private static void Initialize()
    {
        try
        {
            // Reset any existing services on domain reload
            ResetServices();

            // Register core shared services (logger factory, etc.)
            RegisterCoreServices();

            Log.LogDebug("✅ Shared Module services registered successfully");

            // Verify service registration
            VerifyServices();
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Failed to initialize Shared Module. Stack trace: {exStackTrace}", ex.StackTrace);
        }
    }

    private static void RegisterCoreServices()
    {
        Debug.Log("[Bootstrap] RegisterCoreServices: Registering CompositionRoot and ILoggerFactory in Splat (batchmode=" + Application.isBatchMode + ")");
        var mutable = Locator.CurrentMutable;

        mutable.RegisterConstant(new CompositionRoot(), typeof(CompositionRoot));
        var cr = Splat.Locator.Current.GetService<CompositionRoot>();
        var loggerFactory = cr?.Resolve<ILoggerFactory>();
        mutable.RegisterConstant(loggerFactory, typeof(ILoggerFactory));
    }

    private static void VerifyServices()
    {
        var loggerFactory = Splat.Locator.Current.GetService<ILoggerFactory>();
        if (loggerFactory != null)
        {
            Debug.Log("✅ ILoggerFactory is available for dependency resolution");
        }
        else
        {
            Debug.Log("⚠️ ILoggerFactory registration may have failed");
        }
    }

    private static void ResetServices()
    {
        var mutable = Locator.CurrentMutable;

        // Unregister services that should be reset on domain reload
        // Be careful not to unregister services that other modules depend on

        // Note: Don't reset logger factory here as other modules might depend on it

        mutable.UnregisterCurrent<ILoggerFactory>();
        mutable.UnregisterCurrent<CompositionRoot>();
    }
}
