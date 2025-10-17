using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace SangoCard.Build.Editor.ProjectSettings;

internal class SettingsView
{
    private readonly SettingsViewModel _viewModel;
    private readonly VisualElement _rootElement;

    public SettingsView(SettingsViewModel viewModel, VisualElement rootElement)
    {
        _viewModel = viewModel;
        _rootElement = rootElement;

        Initialize();
    }

    private void Initialize()
    {
        var templatePath = Path.Combine("Assets", "Editor", "ProjectSettings", "SettingsView.uxml");
        var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
        template.CloneTree(_rootElement);

        // Bind the view model to the UI elements here
        // For example:
        // _rootElement.Q<TextField>("SomeField").BindProperty(_viewModel.SomeProperty);
    }
}
