using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Config type selection view - helps users choose between source collection and injection mapping configs.
/// This view provides guidance on the two-phase workflow.
/// </summary>
public class ConfigTypeSelectionView : View
{
    private readonly ILogger<ConfigTypeSelectionView> _logger;

    public event EventHandler<ConfigTypeSelectedEventArgs>? ConfigTypeSelected;

    public ConfigTypeSelectionView(ILogger<ConfigTypeSelectionView> logger)
    {
        _logger = logger;

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Title
        var titleLabel = new Label()
        {
            Text = "Select Configuration Type",
            X = 1,
            Y = 0
        };

        // Description frame
        var descFrame = new FrameView()
        {
            Title = "Two-Phase Workflow",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 1,
            Height = 12
        };

        var descText = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = @"The build preparation system uses a TWO-PHASE workflow:

PHASE 1: SOURCE COLLECTION (Preparation Sources)
  • Define what packages, assemblies, and assets to collect
  • Sources can be from ANYWHERE (external drives, network shares, etc.)
  • Items are copied to a LOCAL CACHE
  • Config tracks: SOURCE → CACHE mapping

PHASE 2: INJECTION MAPPING (Build Injections)
  • Define what gets injected into the Unity client
  • Uses items from the cache (populated in Phase 1)
  • Config tracks: CACHE → CLIENT mapping
  • Supports code patching and transformations

Choose the config type you want to work with:"
        };

        descFrame.Add(descText);

        // Selection frame
        var selectionFrame = new FrameView()
        {
            Title = "Select Type",
            X = 1,
            Y = 15,
            Width = Dim.Fill()! - 1,
            Height = 16
        };

        // Phase 1 option
        var phase1Button = new Button()
        {
            Text = "Phase 1: Preparation Sources",
            X = 2,
            Y = 1,
            Width = 40
        };
        phase1Button.Accepting += (s, e) => OnSelectConfigType(ConfigType.PreparationSources);

        var phase1Desc = new Label()
        {
            Text = "Manage source collection from external locations.\nUse this to add packages, assemblies, and assets\nto the local cache.",
            X = 2,
            Y = 3,
            Width = Dim.Fill()! - 2
        };

        var phase1Examples = new Label()
        {
            Text = "Examples: Add UPM packages, copy DLLs from network,\nimport assets from external drives",
            X = 2,
            Y = 6,
            Width = Dim.Fill()! - 2
        };

        // Phase 2 option
        var phase2Button = new Button()
        {
            Text = "Phase 2: Build Injections",
            X = 2,
            Y = 9,
            Width = 40
        };
        phase2Button.Accepting += (s, e) => OnSelectConfigType(ConfigType.BuildInjections);

        var phase2Desc = new Label()
        {
            Text = "Manage injection mappings to Unity client.\nUse this to define what gets copied to client\nfrom the cache.",
            X = 2,
            Y = 11,
            Width = Dim.Fill()! - 2
        };

        var phase2Examples = new Label()
        {
            Text = "Examples: Inject cached packages to Packages/,\ncopy DLLs to Assets/, apply code patches",
            X = 2,
            Y = 14,
            Width = Dim.Fill()! - 2
        };

        selectionFrame.Add(phase1Button, phase1Desc, phase1Examples,
            phase2Button, phase2Desc, phase2Examples);

        // Info section
        var infoLabel = new Label()
        {
            Text = "💡 TIP: Most users start with Phase 1 to populate the cache, then move to Phase 2 for injections.",
            X = 1,
            Y = Pos.Bottom(selectionFrame) + 1
        };

        Add(titleLabel, descFrame, selectionFrame, infoLabel);
    }

    private void OnSelectConfigType(ConfigType configType)
    {
        _logger.LogDebug("Config type selected: {ConfigType}", configType);
        ConfigTypeSelected?.Invoke(this, new ConfigTypeSelectedEventArgs(configType));
    }
}

/// <summary>
/// Event arguments for config type selection.
/// </summary>
public class ConfigTypeSelectedEventArgs : EventArgs
{
    public ConfigType ConfigType { get; }

    public ConfigTypeSelectedEventArgs(ConfigType configType)
    {
        ConfigType = configType;
    }
}

/// <summary>
/// Represents the type of configuration being worked with.
/// </summary>
public enum ConfigType
{
    /// <summary>
    /// Phase 1: Source collection - manages source → cache mappings
    /// </summary>
    PreparationSources,

    /// <summary>
    /// Phase 2: Injection mapping - manages cache → client mappings
    /// </summary>
    BuildInjections
}
