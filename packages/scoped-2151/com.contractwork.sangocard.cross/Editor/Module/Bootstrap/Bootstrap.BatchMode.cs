using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace SangoCard.Cross.Editor.Module;

// using Plate.Shared.MEditor.Logging.Providers;

// ReSharper disable once UnusedType.Global
internal partial class Bootstrap
{
    // For the first/root/shared package (e.g., the one registering the logger factory),
    // using ForceBootstrapInBatchmode with [InitializeOnLoadMethod] is necessary to guarantee
    // initialization in batchmode, since EditorApplication.delayCall is unreliable there.
    // For other package modules (that depend on the shared/root module), you should continue to
    // use the [OrderedEditorModulePhase] system for initialization order in the Unity Editor (interactive mode).
    // This system works well in the editor because EditorApplication.delayCall is processed reliably.
    [UnityEditor.InitializeOnLoadMethod]
    public static void ForceBootstrapInBatchMode()
    {
        if (!Application.isBatchMode)
        {
            return;
        }

        Debug.Log("[Bootstrap] ForceBootstrapInBatchmode called (batchmode=True)");
        Initialize();
    }
}
