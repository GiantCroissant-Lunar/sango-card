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
            new(Key.F2, "Config", () => SwitchToConfigView()),
            new(Key.F3, "Cache", () => SwitchToCacheView()),
            new(Key.F4, "Validate", () => SwitchToValidationView()),
            new(Key.F5, "Prepare", () => SwitchToPreparationView()),
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
                    new("_Config Editor", "", () => SwitchToConfigView()),
                    new("C_ache Manager", "", () => SwitchToCacheView()),
                    new("_Validation", "", () => SwitchToValidationView()),
                    new("_Preparation", "", () => SwitchToPreparationView())
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
    F1 - Help          F6 - (Future view)
    F2 - Config        F7 - (Future view)
    F3 - Cache         F8 - (Future view)
    F4 - Validation    F9 - (Future view)
    F5 - Preparation   F10 - Quit

FEATURES:
    • Configuration Management - Create and edit preparation configs
    • Cache Management - Manage Unity packages and assemblies cache
    • Validation - Validate configs before execution
    • Preparation - Execute build preparation with progress tracking

STATUS:
    ✅ Core Infrastructure - Complete
    ✅ Services Layer - Complete
    ✅ Code Patchers - Complete
    ✅ CLI Commands - Complete (14 commands)
    ✅ TUI Views - Complete (4 views)
    ⏳ Testing - Next Phase

Use the menu bar (Alt+letter) or function keys to navigate.
Press F10 or select File > Quit to exit.

Version: 1.0.0-alpha
Build: 2025-10-17
"
        };

        _contentFrame.Add(welcomeText);
        _logger.LogDebug("Showing welcome view");
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
            "  F2  - Config Editor\n" +
            "  F3  - Cache Manager\n" +
            "  F4  - Validation View\n" +
            "  F5  - Preparation View\n" +
            "  F10 - Quit Application\n\n" +
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
            "  ✅ Code Patching (4 types)\n" +
            "  ✅ CLI Interface (14 commands)\n" +
            "  ⏳ TUI Interface (in progress)\n\n" +
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
