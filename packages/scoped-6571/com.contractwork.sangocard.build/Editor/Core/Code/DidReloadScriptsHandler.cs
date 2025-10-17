using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SangoCard.Build.Editor.Code;

internal class DidReloadScripts
{
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        Debug.Log("Scripts have been reloaded!");
    }
}
