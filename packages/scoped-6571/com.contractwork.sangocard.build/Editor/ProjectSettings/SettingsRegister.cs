// Copyright (c) GiantCroissant. All rights reserved.

using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SangoCard.Build.Editor.ProjectSettings;

internal static class SettingsRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        var provider = new SettingsProvider(
            Path.Combine("Project", "Sango Card", "Settings.SangoCard.Build"),
            SettingsScope.Project)
        {
            label = "Cross",
            activateHandler = ActivateHandler,
        };

        return provider;
    }

    private static void ActivateHandler(
        string searchContext,
        VisualElement rootElement)
    {
        var settings = Settings.GetSerializedSettings();

        _ = searchContext;

        var viewModel = new SettingsViewModel();
        var view = new SettingsView(viewModel, rootElement);

        rootElement.Bind(settings);
    }
}
