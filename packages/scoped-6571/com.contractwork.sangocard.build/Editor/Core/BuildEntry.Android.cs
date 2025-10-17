using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SangoCard.Build.Editor;

public partial class BuildEntry
{
    public static bool GetExportAsGoogleAndroidProject(ScriptableObject? buildProfile)
    {
        if (!buildProfile)
        {
            return false;
        }

        // Step 1: Get m_PlatformBuildProfile
        var platformField = buildProfile.GetType().GetField("m_PlatformBuildProfile", BindingFlags.NonPublic | BindingFlags.Instance);
        if (platformField == null)
        {
            return false;
        }

        var platformSettings = platformField.GetValue(buildProfile);
        if (platformSettings == null)
        {
            return false;
        }

        // Step 2: Check if it's the Android settings
        var platformSettingsType = platformSettings.GetType();
        if (platformSettingsType.Name != "AndroidPlatformBuildSettings")
        {
            return false;
        }

        // Step 3: Get the export flag
        var exportField = platformSettingsType.GetField("m_ExportAsGoogleAndroidProject", BindingFlags.NonPublic | BindingFlags.Instance);
        if (exportField == null)
        {
            return false;
        }

        return (bool)exportField.GetValue(platformSettings);
    }
}
