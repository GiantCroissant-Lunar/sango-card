using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Messages;
using SangoCard.Build.Tool.Tui.Views;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui;

/// <summary>
/// Terminal GUI host using Terminal.Gui v2.
/// Provides an interactive TUI with navigation and multiple views.
/// </summary>
public class TuiHost : Toplevel
{
    private readonly ILogger<TuiHost> _logger;
    private readonly IServiceProvider _serviceProvider;

    // Message subscribers
    private readonly ISubscriber<ValidationCompletedMessage> _validationCompleted;
    private readonly ISubscriber<PreparationCompletedMessage> _prepCompleted;
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly ISubscriber<CachePopulatedMessage> _cachePopulated;

    private Window? _mainWindow;
    private MenuBar? _menuBar;
    private FrameView? _contentFrame;
    private StatusBar? _statusBar;

    private readonly List<IDisposable> _subscriptions = new();

    public TuiHost(
        ILogger<TuiHost> logger,
        IServiceProvider serviceProvider,
        ISubscriber<ValidationCompletedMessage> validationCompleted,
        ISubscriber<PreparationCompletedMessage> prepCompleted,
        ISubscriber<ConfigLoadedMessage> configLoaded,
        ISubscriber<CachePopulatedMessage> cachePopulated)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _validationCompleted = validationCompleted;
        _prepCompleted = prepCompleted;
        _configLoaded = configLoaded;
        _cachePopulated = cachePopulated;

        InitializeComponent();
        SubscribeToMessages();
    }

    private void InitializeComponent()
    {
        // Create menu bar
        _menuBar = CreateMenuBar();

        // Create main window
        _mainWindow = new Window()
        {
            Title = "Sango Card Build Preparation Tool",
            X = 0,
            Y = 1, // Leave room for menu
            Width = Dim.Fill(),
            Height = Dim.Fill(1) // Leave room for status bar
        };

        // Create content frame
        _contentFrame = new FrameView()
        {
            Title = "Welcome",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Show welcome view by default
        ShowWelcomeView();

        _mainWindow.Add(_contentFrame);

        // Create status bar with updated navigation
        _statusBar = new StatusBar(new Shortcut[]
        {
            new(Key.F1, "Help", () => ShowHelpDialog()),
            new(Key.F2, "Config Type", () => SwitchToConfigTypeView()),
            new(Key.F3, "Config", () => SwitchToConfigView()),
            new(Key.F4, "Cache", () => SwitchToCacheView()),
            new(Key.F5, "Manual", () => SwitchToManualSourcesView()),
            new(Key.F6, "Validate", () => SwitchToValidationView()),
            new(Key.F7, "Prepare", () => SwitchToPreparationView()),
            new(Key.F10, "Quit", () => RequestQuit())
        });

        // Add all to toplevel
        Add(_menuBar, _mainWindow, _statusBar);
    }

    public Task<int> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Initialize Terminal.Gui
            Application.Init();

            try
            {
                // Run the application with this as the top-level
                Application.Run(this);

                return Task.FromResult(0);
            }
            finally
            {
                Application.Shutdown();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TUI error: {Message}", ex.Message);
            return Task.FromResult(1);
        }
        finally
        {
            // Cleanup subscriptions
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
        }
    }

    private MenuBar CreateMenuBar()
    {
        var menu = new MenuBar()
        {
            Menus =
            [
                new MenuBarItem("_File", new MenuItem[]
                {
                    new("_Open Config...", "", () => OpenConfigDialog()),
                    new("_Save Config", "", () => SaveConfigDialog()),
                    null!, // Separator
                    new("_Quit", "", () => RequestQuit())
                }),
                new MenuBarItem("_View", new MenuItem[]
                {
                    new("_Welcome", "", () => ShowWelcomeView()),
                    new("Config _Type Selection", "", () => SwitchToConfigTypeView()),
                    new("_Config Editor", "", () => SwitchToConfigView()),
                    new("C_ache Manager", "", () => SwitchToCacheView()),
                    new("_Manual Sources", "", () => SwitchToManualSourcesView()),
                    new("_Validation", "", () => SwitchToValidationView()),
                    new("_Preparation", "", () => SwitchToPreparationView())
                }),
                new MenuBarItem("_Manage", new MenuItem[]
                {
                    new("_Preparation Sources (Phase 1)", "", () => SwitchToPreparationSourcesView()),
                    new("_Build Injections (Phase 2)", "", () => SwitchToBuildInjectionsView())
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new("_About", "", () => ShowAboutDialog()),
                    new("_Help", "", () => ShowHelpDialog())
                })
            ]
        };
        return menu;
    }

    private void ShowWelcomeView()
    {
        if (_contentFrame == null) return;

        _contentFrame.Title = "Welcome";
        _contentFrame.RemoveAll();

        var welcomeText = new TextView()
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = Dim.Fill(1),
            ReadOnly = true,
            Text = @"
╔════════════════════════════════════════════════════════════════╗
║     Sango Card Build Preparation Tool - Terminal UI           ║
╚════════════════════════════════════════════════════════════════╝

Welcome to the Build Preparation Tool!

This tool helps you manage Unity build preparation configurations,
including package management, cache operations, and build execution.

QUICK START:
    F1 - Help             F6 - Validation
    F2 - Config Type      F7 - Preparation
    F3 - Config Editor    F10 - Quit
    F4 - Cache Manager
    F5 - Manual Sources

WORKFLOW GUIDE:

Two-Phase System:
    1. PREPARATION SOURCES (Phase 1)
       → Collect packages/assemblies from external locations
       → Items copied to local cache
       → Use: Manage → Preparation Sources (dedicated CRUD screen)
       → Or: F5 Manual Sources (quick add)

    2. BUILD INJECTIONS (Phase 2)
       → Define what gets injected to Unity client
       → Maps cache items to client targets
       → Use: Manage → Build Injections (full CRUD screen)

MENU SHORTCUTS:
    • View - Switch between different screens
    • Manage - Full CRUD for Phase 1 & 2 configs:
      - Preparation Sources (source → cache)
      - Build Injections (cache → client) NEW!
    • Help - Documentation and about

FEATURES:
    • Config Type Selection - Understand and choose config types
    • Configuration Management - Create and edit configs
    • Cache Management - Manage Unity packages and assemblies cache
    • Manual Source Management - Quick add packages/assemblies
    • Preparation Sources Management - Full CRUD for Phase 1
    • Build Injections Management - Full CRUD for Phase 2 (NEW!)
    • Validation - Validate configs before execution
    • Preparation - Execute build preparation with progress tracking

STATUS:
    ✅ Core Infrastructure - Complete
    ✅ Services Layer - Complete
    ✅ Code Patchers - Complete
    ✅ CLI Commands - Complete (14 commands)
    ✅ TUI Views - 8 views
    ✅ Management Screens - Phase 3.2 Complete!

Use the menu bar (Alt+letter) or function keys to navigate.
Press F10 or select File > Quit to exit.

Version: 1.0.0-alpha
Build: 2025-10-17
"
        };

        _contentFrame.Add(welcomeText);
        _logger.LogDebug("Showing welcome view");
    }

    private void SwitchToConfigTypeView()
    {
        var view = _serviceProvider.GetRequiredService<ConfigTypeSelectionView>();
        SwitchToView("Config Type Selection", view);
    }

    private void SwitchToConfigView()
    {
        var view = _serviceProvider.GetRequiredService<ConfigEditorView>();
        SwitchToView("Configuration Editor", view);
    }

    private void SwitchToCacheView()
    {
        var view = _serviceProvider.GetRequiredService<CacheManagementView>();
        SwitchToView("Cache Management", view);
    }

    private void SwitchToValidationView()
    {
        var view = _serviceProvider.GetRequiredService<ValidationView>();
        SwitchToView("Validation", view);
    }

    private void SwitchToPreparationView()
    {
        var view = _serviceProvider.GetRequiredService<PreparationExecutionView>();
        SwitchToView("Preparation Execution", view);
    }

    private void SwitchToManualSourcesView()
    {
        var view = _serviceProvider.GetRequiredService<ManualSourcesView>();
        SwitchToView("Manual Source Management", view);
    }

    private void SwitchToPreparationSourcesView()
    {
        var view = _serviceProvider.GetRequiredService<PreparationSourcesManagementView>();
        SwitchToView("Preparation Sources Management", view);
    }

    private void SwitchToBuildInjectionsView()
    {
        var view = _serviceProvider.GetRequiredService<BuildInjectionsManagementView>();
        SwitchToView("Build Injections Management", view);
    }

    private void SwitchToView(string viewName, View view)
    {
        if (_contentFrame == null) return;

        _contentFrame.Title = viewName;
        _contentFrame.RemoveAll();
        _contentFrame.Add(view);
        _logger.LogDebug("Switched to view: {ViewName}", viewName);
    }

    private void OpenConfigDialog()
    {
        var dialog = new Dialog()
        {
            Title = "Open Configuration",
            Width = 60,
            Height = 10
        };

        var pathLabel = new Label() { Text = "Config path:", X = 1, Y = 1 };
        var pathEntry = new TextField()
        {
            Text = "build/preparation/configs/dev.json",
            X = Pos.Right(pathLabel) + 1,
            Y = 1,
            Width = 40
        };

        var okButton = new Button() { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            MessageBox.Query("Info", $"Config loading will be implemented in service layer.\nPath: {pathEntry.Text}", "OK");
            Application.RequestStop(dialog);
        };

        var cancelButton = new Button() { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop(dialog);

        dialog.Add(pathLabel, pathEntry, okButton, cancelButton);
        Application.Run(dialog);
    }

    private void SaveConfigDialog()
    {
        MessageBox.Query("Save Config", "Config saving will be implemented in service layer.", "OK");
    }

    private void ShowHelpDialog()
    {
        MessageBox.Query("Help",
            "Build Preparation Tool - TUI Help\n\n" +
            "FUNCTION KEYS:\n" +
            "  F1  - Show Help\n" +
            "  F2  - Config Type Selection\n" +
            "  F3  - Config Editor\n" +
            "  F4  - Cache Manager\n" +
            "  F5  - Manual Sources (Quick Add)\n" +
            "  F6  - Validation View\n" +
            "  F7  - Preparation View\n" +
            "  F10 - Quit Application\n\n" +
            "MENU BAR:\n" +
            "  File - Open/Save configs\n" +
            "  View - Switch between views\n" +
            "  Manage - Full CRUD screens:\n" +
            "    • Preparation Sources (Phase 1)\n" +
            "    • Build Injections (Phase 2) NEW!\n" +
            "  Help - Documentation and about\n\n" +
            "WORKFLOW:\n" +
            "  1. Start with F2 to understand config types\n" +
            "  2. Use Manage → Preparation Sources for source collection\n" +
            "  3. Use Manage → Build Injections for injection mapping\n" +
            "  4. Use F6 to validate, F7 to prepare\n\n" +
            "NAVIGATION:\n" +
            "  Tab/Shift+Tab - Move between controls\n" +
            "  Arrow Keys - Navigate lists/menus\n" +
            "  Enter - Activate/Select\n" +
            "  Alt+Letter - Access menu items\n\n" +
            "For more information, see the documentation.",
            "OK");
    }

    private void ShowAboutDialog()
    {
        MessageBox.Query("About",
            "Sango Card Build Preparation Tool\n" +
            "Version: 1.0.0-alpha\n" +
            "Build Date: 2025-10-17\n\n" +
            "A tool for managing Unity build preparation\n" +
            "configurations, caching, and execution.\n\n" +
            "Features:\n" +
            "  ✅ Configuration Management\n" +
            "  ✅ Cache Management\n" +
            "  ✅ Manual Source Management (Quick Add)\n" +
            "  ✅ Preparation Sources (Full CRUD)\n" +
            "  ✅ Build Injections (Full CRUD) NEW!\n" +
            "  ✅ Config Type Selection\n" +
            "  ✅ Code Patching (4 types)\n" +
            "  ✅ CLI Interface (14 commands)\n" +
            "  ✅ TUI Interface (8 views)\n" +
            "  ✅ Wave 3.2 Complete!\n\n" +
            "Licensed under MIT",
            "OK");
    }

    private void RequestQuit()
    {
        var result = MessageBox.Query("Confirm Exit", "Are you sure you want to quit?", "Yes", "No");
        if (result == 0)
        {
            Application.RequestStop();
        }
    }

    private void SubscribeToMessages()
    {
        // Subscribe to key messages for logging
        _subscriptions.Add(_configLoaded.Subscribe(msg =>
            _logger.LogInformation("Config loaded: {Path}", msg.FilePath)));

        _subscriptions.Add(_cachePopulated.Subscribe(msg =>
            _logger.LogInformation("Cache populated: {Count} items from {Source}", msg.ItemCount, msg.SourcePath)));

        _subscriptions.Add(_validationCompleted.Subscribe(msg =>
            _logger.LogInformation("Validation: {Result}", msg.Result.IsValid ? "Passed" : "Failed")));

        _subscriptions.Add(_prepCompleted.Subscribe(msg =>
            _logger.LogInformation("Preparation complete: {Ops} operations in {Duration}s",
                msg.Copied + msg.Moved + msg.Deleted + msg.Patched, msg.Duration.TotalSeconds)));
    }
}
