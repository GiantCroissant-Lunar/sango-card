using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Splat;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SangoCard.Build.Editor.Module;

using SangoCard.Build.Editor.Module.Attributes;

/// <summary>
/// Minimal bootstrapper that borrows shared services and makes sure the build module
/// has a functioning logger during batch-mode execution.
/// </summary>
internal partial class Bootstrap
{
    private static ILogger _logger = new NullLogger<Bootstrap>();

    [OrderedEditorModulePhase(6571010, OrderedEditorModulePhaseAttribute.Phase.LoadBegin)]
    public static void OnEditorLoadBegin()
    {
        InitializeLogger();
        Log.LogDebug("AssetInOut Tool Module Initialized - Shared services ready");
    }

    [OrderedEditorModulePhase(6571010, OrderedEditorModulePhaseAttribute.Phase.UnloadBegin)]
    public static void OnEditorUnloadBegin()
    {
        Log.LogDebug("AssetInOut Tool Module Unloading - nothing to tear down");
    }

    private static ILogger Log => _logger;

    private static void InitializeLogger()
    {
        try
        {
            var loggerFactory = Locator.Current.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger<Bootstrap>() ?? new NullLogger<Bootstrap>();
        }
        catch (Exception ex)
        {
            _logger = new NullLogger<Bootstrap>();
            Debug.LogWarning($"Failed to resolve ILoggerFactory for Build module: {ex.Message}");
        }
    }
}
