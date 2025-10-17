// Copyright (c) GiantCroissant. All rights reserved.

using System.IO;
using UnityEditor;
using UnityEngine;

namespace SangoCard.Build.Editor.ProjectSettings
{
    public class Settings : ScriptableObject
    {
        private static readonly string SettingsPath = Path.Combine(
            "ProjectSettings",
            "Packages",
            "com.contractwork.sangocard.build",
            "Settings.json");

        internal static SerializedObject GetSerializedSettings() => new (GetOrCreateSettings());

        private static Settings GetOrCreateSettings()
        {
            var settings = CreateInstance<Settings>();
            if (File.Exists(SettingsPath))
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), settings);
            }

            return settings;
        }
    }
}
