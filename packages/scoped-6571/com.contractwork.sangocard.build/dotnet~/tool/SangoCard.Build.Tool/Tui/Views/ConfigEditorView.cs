using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Config editor view - Terminal.Gui v2 implementation.
/// Allows loading, editing, and saving preparation configurations.
/// </summary>
public class ConfigEditorView : View
{
    private readonly ConfigService _configService;
    private readonly PathResolver _pathResolver;
    private readonly ILogger<ConfigEditorView> _logger;
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly ISubscriber<ConfigSavedMessage> _configSaved;
    private readonly List<IDisposable> _subscriptions = new();

    private PreparationConfig? _currentConfig;
    private string? _currentConfigPath;

    // UI Controls
    private TextField? _pathField;
    private Label? _statusLabel;
    private ListView? _packagesList;
    private ListView? _assembliesList;
    private ListView? _patchesList;
    private Button? _loadButton;
    private Button? _saveButton;
    private Button? _newButton;

    public ConfigEditorView(
        ConfigService configService,
        PathResolver pathResolver,
        ILogger<ConfigEditorView> logger,
        ISubscriber<ConfigLoadedMessage> configLoaded,
        ISubscriber<ConfigSavedMessage> configSaved)
    {
        _configService = configService;
        _pathResolver = pathResolver;
        _logger = logger;
        _configLoaded = configLoaded;
        _configSaved = configSaved;

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        InitializeComponent();
        SubscribeToMessages();
    }

    private void InitializeComponent()
    {
        // Title
        var titleLabel = new Label()
        {
            Text = "Configuration Editor",
            X = 1,
            Y = 0
        };

        // Config file path section
        var pathLabel = new Label()
        {
            Text = "Config Path:",
            X = 1,
            Y = 2
        };

        _pathField = new TextField()
        {
            Text = "build/preparation/configs/dev.json",
            X = Pos.Right(pathLabel) + 1,
            Y = 2,
            Width = 50
        };

        _loadButton = new Button()
        {
            Text = "Load",
            X = Pos.Right(_pathField) + 2,
            Y = 2
        };
        _loadButton.Accepting += OnLoadConfig;

        _newButton = new Button()
        {
            Text = "New",
            X = Pos.Right(_loadButton) + 2,
            Y = 2
        };
        _newButton.Accepting += OnNewConfig;

        _saveButton = new Button()
        {
            Text = "Save",
            X = Pos.Right(_newButton) + 2,
            Y = 2,
            Enabled = false
        };
        _saveButton.Accepting += OnSaveConfig;

        // Status label
        _statusLabel = new Label()
        {
            Text = "No configuration loaded",
            X = 1,
            Y = 4,
            ColorScheme = new ColorScheme()
        };

        // Packages section
        var packagesFrame = new FrameView()
        {
            Title = "Unity Packages (0)",
            X = 1,
            Y = 6,
            Width = Dim.Percent(50)! - 1,
            Height = Dim.Percent(40)!
        };

        _packagesList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };

        var addPackageBtn = new Button()
        {
            Text = "Add Package",
            X = 0,
            Y = Pos.Bottom(_packagesList)
        };
        addPackageBtn.Accepting += OnAddPackage;

        packagesFrame.Add(_packagesList, addPackageBtn);

        // Assemblies section
        var assembliesFrame = new FrameView()
        {
            Title = "Assemblies (0)",
            X = Pos.Right(packagesFrame) + 1,
            Y = 6,
            Width = Dim.Fill()! - 1,
            Height = Dim.Percent(40)!
        };

        _assembliesList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };

        var addAssemblyBtn = new Button()
        {
            Text = "Add Assembly",
            X = 0,
            Y = Pos.Bottom(_assembliesList)
        };
        addAssemblyBtn.Accepting += OnAddAssembly;

        assembliesFrame.Add(_assembliesList, addAssemblyBtn);

        // Code patches section
        var patchesFrame = new FrameView()
        {
            Title = "Code Patches (0)",
            X = 1,
            Y = Pos.Bottom(packagesFrame) + 1,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()
        };

        _patchesList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };

        var addPatchBtn = new Button()
        {
            Text = "Add Patch",
            X = 0,
            Y = Pos.Bottom(_patchesList)
        };
        addPatchBtn.Accepting += OnAddPatch;

        patchesFrame.Add(_patchesList, addPatchBtn);

        Add(titleLabel, pathLabel, _pathField, _loadButton, _newButton, _saveButton, _statusLabel,
            packagesFrame, assembliesFrame, patchesFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_configLoaded.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _currentConfig = msg.Config;
                _currentConfigPath = msg.FilePath;
                UpdateUI();
                _statusLabel!.Text = $"Loaded: {msg.FilePath}";
                _saveButton!.Enabled = true;
            });
        }));

        _subscriptions.Add(_configSaved.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Saved: {msg.FilePath}";
            });
        }));
    }

    private async void OnLoadConfig(object? sender, EventArgs e)
    {
        try
        {
            var path = _pathField!.Text ?? "";
            _statusLabel!.Text = $"Loading {path}...";

            _currentConfig = await _configService.LoadAsync(path);
            _currentConfigPath = path;
            UpdateUI();

            _statusLabel.Text = $"Loaded: {path}";
            _saveButton!.Enabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Load Error", ex.Message, "OK");
        }
    }

    private void OnNewConfig(object? sender, EventArgs e)
    {
        _currentConfig = new PreparationConfig
        {
            Version = "1.0",
            Description = "New preparation configuration"
        };
        _currentConfigPath = null;
        UpdateUI();
        _statusLabel!.Text = "New configuration created";
        _saveButton!.Enabled = true;
    }

    private async void OnSaveConfig(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Save Error", "No configuration to save", "OK");
            return;
        }

        try
        {
            var path = _currentConfigPath ?? _pathField!.Text ?? "";
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.ErrorQuery("Save Error", "Please specify a file path", "OK");
                return;
            }

            _statusLabel!.Text = $"Saving {path}...";
            await _configService.SaveAsync(_currentConfig, path);
            _currentConfigPath = path;
            _statusLabel.Text = $"Saved: {path}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Save Error", ex.Message, "OK");
        }
    }

    private void OnAddPackage(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No configuration loaded", "OK");
            return;
        }

        var dialog = new Dialog()
        {
            Title = "Add Unity Package",
            Width = 60,
            Height = 12
        };

        var nameLabel = new Label() { Text = "Name:", X = 1, Y = 1 };
        var nameField = new TextField() { X = 15, Y = 1, Width = 40 };

        var versionLabel = new Label() { Text = "Version:", X = 1, Y = 2 };
        var versionField = new TextField() { X = 15, Y = 2, Width = 40 };

        var sourceLabel = new Label() { Text = "Source:", X = 1, Y = 3 };
        var sourceField = new TextField() { X = 15, Y = 3, Width = 40 };

        var targetLabel = new Label() { Text = "Target:", X = 1, Y = 4 };
        var targetField = new TextField() { X = 15, Y = 4, Width = 40 };

        var okBtn = new Button() { Text = "Add", IsDefault = true, X = 1, Y = 6 };
        okBtn.Accepting += (s, args) =>
        {
            var pkg = new UnityPackageReference
            {
                Name = nameField.Text ?? "",
                Version = versionField.Text ?? "",
                Source = sourceField.Text ?? "",
                Target = targetField.Text ?? ""
            };
            _currentConfig.Packages.Add(pkg);
            UpdateUI();
            Application.RequestStop(dialog);
        };

        var cancelBtn = new Button() { Text = "Cancel", X = 15, Y = 6 };
        cancelBtn.Accepting += (s, args) => Application.RequestStop(dialog);

        dialog.Add(nameLabel, nameField, versionLabel, versionField, sourceLabel, sourceField,
            targetLabel, targetField, okBtn, cancelBtn);

        Application.Run(dialog);
    }

    private void OnAddAssembly(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No configuration loaded", "OK");
            return;
        }

        var dialog = new Dialog()
        {
            Title = "Add Assembly",
            Width = 60,
            Height = 10
        };

        var nameLabel = new Label() { Text = "Name:", X = 1, Y = 1 };
        var nameField = new TextField() { X = 15, Y = 1, Width = 40 };

        var sourceLabel = new Label() { Text = "Source:", X = 1, Y = 2 };
        var sourceField = new TextField() { X = 15, Y = 2, Width = 40 };

        var targetLabel = new Label() { Text = "Target:", X = 1, Y = 3 };
        var targetField = new TextField() { X = 15, Y = 3, Width = 40 };

        var okBtn = new Button() { Text = "Add", IsDefault = true, X = 1, Y = 5 };
        okBtn.Accepting += (s, args) =>
        {
            var asm = new AssemblyReference
            {
                Name = nameField.Text ?? "",
                Source = sourceField.Text ?? "",
                Target = targetField.Text ?? ""
            };
            _currentConfig.Assemblies.Add(asm);
            UpdateUI();
            Application.RequestStop(dialog);
        };

        var cancelBtn = new Button() { Text = "Cancel", X = 15, Y = 5 };
        cancelBtn.Accepting += (s, args) => Application.RequestStop(dialog);

        dialog.Add(nameLabel, nameField, sourceLabel, sourceField, targetLabel, targetField, okBtn, cancelBtn);
        Application.Run(dialog);
    }

    private void OnAddPatch(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No configuration loaded", "OK");
            return;
        }

        var dialog = new Dialog()
        {
            Title = "Add Code Patch",
            Width = 70,
            Height = 14
        };

        var typeLabel = new Label() { Text = "Type:", X = 1, Y = 1 };
        var typeField = new TextField() { Text = "CSharp", X = 15, Y = 1, Width = 20 };

        var targetLabel = new Label() { Text = "Target File:", X = 1, Y = 2 };
        var targetField = new TextField() { X = 15, Y = 2, Width = 50 };

        var searchLabel = new Label() { Text = "Search:", X = 1, Y = 3 };
        var searchField = new TextField() { X = 15, Y = 3, Width = 50 };

        var replaceLabel = new Label() { Text = "Replace:", X = 1, Y = 4 };
        var replaceField = new TextField() { X = 15, Y = 4, Width = 50 };

        var descLabel = new Label() { Text = "Description:", X = 1, Y = 5 };
        var descField = new TextField() { X = 15, Y = 5, Width = 50 };

        var okBtn = new Button() { Text = "Add", IsDefault = true, X = 1, Y = 6 };
        okBtn.Accepting += (s, args) =>
        {
            var patchType = typeField.Text?.ToLowerInvariant() switch
            {
                "json" => PatchType.Json,
                "unityasset" => PatchType.UnityAsset,
                "text" => PatchType.Text,
                _ => PatchType.CSharp
            };

            var patch = new CodePatch
            {
                Type = patchType,
                File = targetField.Text ?? "",
                Search = searchField.Text ?? "",
                Replace = replaceField.Text ?? "",
                Description = descField.Text
            };
            _currentConfig.CodePatches.Add(patch);
            UpdateUI();
            Application.RequestStop(dialog);
        };

        var cancelBtn = new Button() { Text = "Cancel", X = 15, Y = 7 };
        cancelBtn.Accepting += (s, args) => Application.RequestStop(dialog);

        dialog.Add(typeLabel, typeField, targetLabel, targetField, searchLabel, searchField,
            replaceLabel, replaceField, descLabel, descField, okBtn, cancelBtn);

        Application.Run(dialog);
    }

    private void UpdateUI()
    {
        if (_currentConfig == null) return;

        // Update packages list
        var pkgItems = _currentConfig.Packages
            .Select(p => $"{p.Name} v{p.Version}")
            .ToList();
        _packagesList!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(pkgItems));

        // Update assemblies list
        var asmItems = _currentConfig.Assemblies
            .Select(a => $"{a.Name} -> {a.Target}")
            .ToList();
        _assembliesList!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(asmItems));

        // Update patches list
        var patchItems = _currentConfig.CodePatches
            .Select(p => $"[{p.Type}] {p.File}")
            .ToList();
        _patchesList!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(patchItems));

        // Update frame titles with counts
        ((FrameView)_packagesList.SuperView!).Title = $"Unity Packages ({_currentConfig.Packages.Count})";
        ((FrameView)_assembliesList.SuperView!).Title = $"Assemblies ({_currentConfig.Assemblies.Count})";
        ((FrameView)_patchesList.SuperView!).Title = $"Code Patches ({_currentConfig.CodePatches.Count})";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var sub in _subscriptions)
            {
                sub.Dispose();
            }
            _subscriptions.Clear();
        }
        base.Dispose(disposing);
    }
}
