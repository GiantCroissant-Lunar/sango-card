using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Build injections management view - dedicated screen for managing Phase 2 configs.
/// Provides full CRUD operations for build injection configs (cache → client mappings).
/// </summary>
public class BuildInjectionsManagementView : View
{
    private readonly ConfigService _configService;
    private readonly PathResolver _pathResolver;
    private readonly ILogger<BuildInjectionsManagementView> _logger;
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly ISubscriber<ConfigSavedMessage> _configSaved;
    private readonly List<IDisposable> _subscriptions = new();

    private string _currentConfigPath = "build/preparation/configs/default.json";
    private PreparationConfig? _currentConfig;
    private string _currentSection = "packages"; // packages, assemblies, assets

    // UI Controls
    private Label? _configPathLabel;
    private Label? _statusLabel;
    private RadioGroup? _sectionSelector;
    private ListView? _itemsListView;
    private Button? _loadButton;
    private Button? _saveButton;
    private Button? _newButton;
    private Button? _addItemButton;
    private Button? _editItemButton;
    private Button? _removeItemButton;
    private Button? _previewButton;
    private Label? _itemCountLabel;

    public BuildInjectionsManagementView(
        ConfigService configService,
        PathResolver pathResolver,
        ILogger<BuildInjectionsManagementView> logger,
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
            Text = "Build Injections Management (Phase 2)",
            X = 1,
            Y = 0
        };

        // Config path section
        var pathLabel = new Label()
        {
            Text = "Config:",
            X = 1,
            Y = 2
        };

        _configPathLabel = new Label()
        {
            Text = _currentConfigPath,
            X = Pos.Right(pathLabel) + 1,
            Y = 2,
            Width = 60
        };

        _loadButton = new Button()
        {
            Text = "Load...",
            X = Pos.Right(_configPathLabel) + 2,
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
            Y = 2
        };
        _saveButton.Accepting += OnSaveConfig;

        // Status label
        _statusLabel = new Label()
        {
            Text = "No config loaded",
            X = 1,
            Y = 4
        };

        // Section selector
        var selectorLabel = new Label()
        {
            Text = "Section:",
            X = 1,
            Y = 6
        };

        _sectionSelector = new RadioGroup()
        {
            X = Pos.Right(selectorLabel) + 1,
            Y = 6,
            RadioLabels = new[] { "Packages", "Assemblies", "Assets" },
            SelectedItem = 0,
            Orientation = Orientation.Horizontal
        };
        _sectionSelector.SelectedItemChanged += (sender, args) =>
        {
            _currentSection = args.SelectedItem switch
            {
                0 => "packages",
                1 => "assemblies",
                2 => "assets",
                _ => "packages"
            };
            RefreshItemsList();
        };

        // Items frame
        var itemsFrame = new FrameView()
        {
            Title = "Injection Items",
            X = 1,
            Y = 8,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()! - 12
        };

        _itemsListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };

        _itemCountLabel = new Label()
        {
            Text = "0 items",
            X = 0,
            Y = Pos.Bottom(_itemsListView)
        };

        itemsFrame.Add(_itemsListView, _itemCountLabel);

        // Action buttons frame
        var actionsFrame = new FrameView()
        {
            Title = "Actions",
            X = 1,
            Y = Pos.Bottom(itemsFrame) + 1,
            Width = Dim.Fill()! - 1,
            Height = 4
        };

        _addItemButton = new Button()
        {
            Text = "Add Item",
            X = 1,
            Y = 0
        };
        _addItemButton.Accepting += OnAddItem;

        _editItemButton = new Button()
        {
            Text = "Edit Item",
            X = Pos.Right(_addItemButton) + 2,
            Y = 0
        };
        _editItemButton.Accepting += OnEditItem;

        _removeItemButton = new Button()
        {
            Text = "Remove Item",
            X = Pos.Right(_editItemButton) + 2,
            Y = 0
        };
        _removeItemButton.Accepting += OnRemoveItem;

        _previewButton = new Button()
        {
            Text = "Preview Operations",
            X = Pos.Right(_removeItemButton) + 2,
            Y = 0
        };
        _previewButton.Accepting += OnPreview;

        actionsFrame.Add(_addItemButton, _editItemButton, _removeItemButton, _previewButton);

        Add(titleLabel, pathLabel, _configPathLabel, _loadButton, _newButton, _saveButton,
            _statusLabel, selectorLabel, _sectionSelector, itemsFrame, actionsFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_configLoaded.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Config loaded: {msg.FilePath}";
            });
        }));

        _subscriptions.Add(_configSaved.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Config saved: {msg.FilePath}";
            });
        }));
    }

    private async void OnLoadConfig(object? sender, EventArgs e)
    {
        var dialog = new OpenDialog()
        {
            Title = "Load Build Config",
            AllowsMultipleSelection = false
        };

        Application.Run(dialog);

        if (!dialog.Canceled && dialog.Path != null)
        {
            try
            {
                _currentConfigPath = dialog.Path.ToString() ?? _currentConfigPath;
                await LoadCurrentConfigAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load config");
                MessageBox.ErrorQuery("Error", $"Failed to load config:\n{ex.Message}", "OK");
            }
        }
    }

    private void OnNewConfig(object? sender, EventArgs e)
    {
        var dialog = new Dialog()
        {
            Title = "New Build Config",
            Width = 70,
            Height = 14
        };

        var pathLabel = new Label()
        {
            Text = "Save As:",
            X = 1,
            Y = 1
        };

        var pathField = new TextField()
        {
            Text = "build/preparation/configs/new-config.json",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        var descLabel = new Label()
        {
            Text = "Description:",
            X = 1,
            Y = 4
        };

        var descField = new TextField()
        {
            Text = "New build preparation config",
            X = 1,
            Y = 5,
            Width = Dim.Fill()! - 2
        };

        var createButton = new Button()
        {
            Text = "Create",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 3
        };
        createButton.Accepting += async (s, ev) =>
        {
            try
            {
                _currentConfigPath = pathField.Text?.ToString() ?? _currentConfigPath;
                _currentConfig = new PreparationConfig
                {
                    Version = "1.0",
                    Description = descField.Text?.ToString() ?? "New config",
                    Packages = new List<UnityPackageReference>(),
                    Assemblies = new List<AssemblyReference>(),
                    AssetManipulations = new List<AssetManipulation>(),
                    CodePatches = new List<CodePatch>()
                };

                await _configService.SaveAsync(_currentConfig, _currentConfigPath);
                RefreshItemsList();
                _statusLabel!.Text = $"Created new config: {_currentConfigPath}";
                _configPathLabel!.Text = _currentConfigPath;

                Application.RequestStop(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create config");
                MessageBox.ErrorQuery("Error", $"Failed to create config:\n{ex.Message}", "OK");
            }
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(pathLabel, pathField, descLabel, descField, createButton, cancelButton);
        Application.Run(dialog);
    }

    private async void OnSaveConfig(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No config loaded", "OK");
            return;
        }

        try
        {
            await _configService.SaveAsync(_currentConfig, _currentConfigPath);
            _statusLabel!.Text = $"Saved config: {_currentConfigPath}";
            MessageBox.Query("Success", $"Config saved successfully to:\n{_currentConfigPath}", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save config");
            MessageBox.ErrorQuery("Error", $"Failed to save config:\n{ex.Message}", "OK");
        }
    }

    private void OnAddItem(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No config loaded. Create or load a config first.", "OK");
            return;
        }

        switch (_currentSection)
        {
            case "packages":
                ShowAddEditPackageDialog(null);
                break;
            case "assemblies":
                ShowAddEditAssemblyDialog(null);
                break;
            case "assets":
                ShowAddEditAssetDialog(null);
                break;
        }
    }

    private void OnEditItem(object? sender, EventArgs e)
    {
        if (_currentConfig == null || _itemsListView == null)
        {
            return;
        }

        var selectedIndex = _itemsListView.SelectedItem;
        if (selectedIndex < 0)
        {
            MessageBox.ErrorQuery("Error", "Please select an item to edit.", "OK");
            return;
        }

        switch (_currentSection)
        {
            case "packages":
                if (selectedIndex < _currentConfig.Packages.Count)
                {
                    ShowAddEditPackageDialog(_currentConfig.Packages[selectedIndex]);
                }
                break;
            case "assemblies":
                if (selectedIndex < _currentConfig.Assemblies.Count)
                {
                    ShowAddEditAssemblyDialog(_currentConfig.Assemblies[selectedIndex]);
                }
                break;
            case "assets":
                if (selectedIndex < _currentConfig.AssetManipulations.Count)
                {
                    ShowAddEditAssetDialog(_currentConfig.AssetManipulations[selectedIndex]);
                }
                break;
        }
    }

    private void OnRemoveItem(object? sender, EventArgs e)
    {
        if (_currentConfig == null || _itemsListView == null)
        {
            return;
        }

        var selectedIndex = _itemsListView.SelectedItem;
        if (selectedIndex < 0)
        {
            MessageBox.ErrorQuery("Error", "Please select an item to remove.", "OK");
            return;
        }

        string itemDesc = "";
        switch (_currentSection)
        {
            case "packages":
                if (selectedIndex < _currentConfig.Packages.Count)
                {
                    var pkg = _currentConfig.Packages[selectedIndex];
                    itemDesc = $"Package: {pkg.Name} v{pkg.Version}\nSource: {pkg.Source}\nTarget: {pkg.Target}";
                }
                break;
            case "assemblies":
                if (selectedIndex < _currentConfig.Assemblies.Count)
                {
                    var asm = _currentConfig.Assemblies[selectedIndex];
                    itemDesc = $"Assembly: {asm.Name}\nSource: {asm.Source}\nTarget: {asm.Target}";
                }
                break;
            case "assets":
                if (selectedIndex < _currentConfig.AssetManipulations.Count)
                {
                    var asset = _currentConfig.AssetManipulations[selectedIndex];
                    itemDesc = $"Asset: {asset.Operation}\nSource: {asset.Source}\nTarget: {asset.Target}";
                }
                break;
        }

        var result = MessageBox.Query("Confirm Remove",
            $"Remove this item from config?\n\n{itemDesc}\n\n" +
            $"Note: This only removes from config. Cached files are not deleted.",
            "Remove", "Cancel");

        if (result == 0)
        {
            switch (_currentSection)
            {
                case "packages":
                    _currentConfig.Packages.RemoveAt(selectedIndex);
                    break;
                case "assemblies":
                    _currentConfig.Assemblies.RemoveAt(selectedIndex);
                    break;
                case "assets":
                    _currentConfig.AssetManipulations.RemoveAt(selectedIndex);
                    break;
            }

            RefreshItemsList();
            _statusLabel!.Text = "Item removed";
        }
    }

    private void OnPreview(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Error", "No config loaded", "OK");
            return;
        }

        var preview = $"BUILD INJECTION CONFIG PREVIEW\n\n" +
                     $"Description: {_currentConfig.Description}\n" +
                     $"Packages: {_currentConfig.Packages.Count}\n" +
                     $"Assemblies: {_currentConfig.Assemblies.Count}\n" +
                     $"Assets: {_currentConfig.AssetManipulations.Count}\n" +
                     $"Patches: {_currentConfig.CodePatches.Count}\n\n";

        preview += "OPERATIONS:\n";

        foreach (var pkg in _currentConfig.Packages)
        {
            preview += $"\nPACKAGE: {pkg.Name} v{pkg.Version}\n";
            preview += $"  {pkg.Source} → {pkg.Target}\n";
        }

        foreach (var asm in _currentConfig.Assemblies)
        {
            preview += $"\nASSEMBLY: {asm.Name}\n";
            preview += $"  {asm.Source} → {asm.Target}\n";
        }

        foreach (var asset in _currentConfig.AssetManipulations)
        {
            preview += $"\nASSET: {asset.Operation}\n";
            preview += $"  {asset.Source} → {asset.Target}\n";
        }

        MessageBox.Query("Preview", preview, "OK");
    }

    private void ShowAddEditPackageDialog(UnityPackageReference? existingPackage)
    {
        var isEdit = existingPackage != null;
        var dialog = new Dialog()
        {
            Title = isEdit ? "Edit Package Injection" : "Add Package Injection",
            Width = 80,
            Height = 22
        };

        // Name
        var nameLabel = new Label() { Text = "Package Name:", X = 1, Y = 1 };
        var nameField = new TextField()
        {
            Text = existingPackage?.Name ?? "",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        // Version
        var versionLabel = new Label() { Text = "Version:", X = 1, Y = 4 };
        var versionField = new TextField()
        {
            Text = existingPackage?.Version ?? "",
            X = 1,
            Y = 5,
            Width = Dim.Fill()! - 2
        };

        // Source (from cache)
        var sourceLabel = new Label() { Text = "Source (from cache):", X = 1, Y = 7 };
        var sourceField = new TextField()
        {
            Text = existingPackage?.Source ?? "build/preparation/cache/",
            X = 1,
            Y = 8,
            Width = Dim.Fill()! - 2
        };

        // Target (to client)
        var targetLabel = new Label() { Text = "Target (in client):", X = 1, Y = 10 };
        var targetField = new TextField()
        {
            Text = existingPackage?.Target ?? "projects/client/Packages/",
            X = 1,
            Y = 11,
            Width = Dim.Fill()! - 2
        };

        // Auto-fill example
        var infoLabel = new Label()
        {
            Text = "Example: Source: build/preparation/cache/com.unity.addressables.tgz\n" +
                   "         Target: projects/client/Packages/com.unity.addressables.tgz",
            X = 1,
            Y = 13
        };

        var saveButton = new Button()
        {
            Text = isEdit ? "Update" : "Add",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 3
        };
        saveButton.Accepting += (s, ev) =>
        {
            HandleSavePackage(dialog, isEdit, existingPackage, nameField, versionField, sourceField, targetField);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(nameLabel, nameField, versionLabel, versionField,
            sourceLabel, sourceField, targetLabel, targetField, infoLabel,
            saveButton, cancelButton);

        Application.Run(dialog);
    }

    private void HandleSavePackage(
        Dialog dialog,
        bool isEdit,
        UnityPackageReference? existingPackage,
        TextField nameField,
        TextField versionField,
        TextField sourceField,
        TextField targetField)
    {
        var name = nameField.Text?.ToString() ?? "";
        var version = versionField.Text?.ToString() ?? "";
        var source = sourceField.Text?.ToString() ?? "";
        var target = targetField.Text?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            MessageBox.ErrorQuery("Validation Error", "Name, source, and target are required.", "OK");
            return;
        }

        if (isEdit && existingPackage != null)
        {
            existingPackage.Name = name;
            existingPackage.Version = version;
            existingPackage.Source = source;
            existingPackage.Target = target;
            _statusLabel!.Text = $"Updated package: {name}";
        }
        else
        {
            _currentConfig!.Packages.Add(new UnityPackageReference
            {
                Name = name,
                Version = version,
                Source = source,
                Target = target
            });
            _statusLabel!.Text = $"Added package: {name}";
        }

        RefreshItemsList();
        Application.RequestStop(dialog);
    }

    private void ShowAddEditAssemblyDialog(AssemblyReference? existingAssembly)
    {
        var isEdit = existingAssembly != null;
        var dialog = new Dialog()
        {
            Title = isEdit ? "Edit Assembly Injection" : "Add Assembly Injection",
            Width = 80,
            Height = 22
        };

        // Name
        var nameLabel = new Label() { Text = "Assembly Name:", X = 1, Y = 1 };
        var nameField = new TextField()
        {
            Text = existingAssembly?.Name ?? "",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        // Version (optional)
        var versionLabel = new Label() { Text = "Version (optional):", X = 1, Y = 4 };
        var versionField = new TextField()
        {
            Text = existingAssembly?.Version ?? "",
            X = 1,
            Y = 5,
            Width = Dim.Fill()! - 2
        };

        // Source
        var sourceLabel = new Label() { Text = "Source (from cache):", X = 1, Y = 7 };
        var sourceField = new TextField()
        {
            Text = existingAssembly?.Source ?? "build/preparation/cache/",
            X = 1,
            Y = 8,
            Width = Dim.Fill()! - 2
        };

        // Target
        var targetLabel = new Label() { Text = "Target (in client):", X = 1, Y = 10 };
        var targetField = new TextField()
        {
            Text = existingAssembly?.Target ?? "projects/client/Assets/Plugins/",
            X = 1,
            Y = 11,
            Width = Dim.Fill()! - 2
        };

        var infoLabel = new Label()
        {
            Text = "Example: Source: build/preparation/cache/Newtonsoft.Json.dll\n" +
                   "         Target: projects/client/Assets/Plugins/Newtonsoft.Json.dll",
            X = 1,
            Y = 13
        };

        var saveButton = new Button()
        {
            Text = isEdit ? "Update" : "Add",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 3
        };
        saveButton.Accepting += (s, ev) =>
        {
            HandleSaveAssembly(dialog, isEdit, existingAssembly, nameField, versionField, sourceField, targetField);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(nameLabel, nameField, versionLabel, versionField,
            sourceLabel, sourceField, targetLabel, targetField, infoLabel,
            saveButton, cancelButton);

        Application.Run(dialog);
    }

    private void HandleSaveAssembly(
        Dialog dialog,
        bool isEdit,
        AssemblyReference? existingAssembly,
        TextField nameField,
        TextField versionField,
        TextField sourceField,
        TextField targetField)
    {
        var name = nameField.Text?.ToString() ?? "";
        var version = versionField.Text?.ToString();
        var source = sourceField.Text?.ToString() ?? "";
        var target = targetField.Text?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            MessageBox.ErrorQuery("Validation Error", "Name, source, and target are required.", "OK");
            return;
        }

        if (isEdit && existingAssembly != null)
        {
            existingAssembly.Name = name;
            existingAssembly.Version = version;
            existingAssembly.Source = source;
            existingAssembly.Target = target;
            _statusLabel!.Text = $"Updated assembly: {name}";
        }
        else
        {
            _currentConfig!.Assemblies.Add(new AssemblyReference
            {
                Name = name,
                Version = version,
                Source = source,
                Target = target
            });
            _statusLabel!.Text = $"Added assembly: {name}";
        }

        RefreshItemsList();
        Application.RequestStop(dialog);
    }

    private void ShowAddEditAssetDialog(AssetManipulation? existingAsset)
    {
        var isEdit = existingAsset != null;
        var dialog = new Dialog()
        {
            Title = isEdit ? "Edit Asset Manipulation" : "Add Asset Manipulation",
            Width = 80,
            Height = 20
        };

        // Operation
        var opLabel = new Label() { Text = "Operation:", X = 1, Y = 1 };
        var opRadio = new RadioGroup()
        {
            X = 1,
            Y = 2,
            RadioLabels = new[] { "Copy", "Move", "Delete" },
            SelectedItem = existingAsset?.Operation switch
            {
                AssetOperation.Copy => 0,
                AssetOperation.Move => 1,
                AssetOperation.Delete => 2,
                _ => 0
            }
        };

        // Source
        var sourceLabel = new Label() { Text = "Source:", X = 1, Y = 5 };
        var sourceField = new TextField()
        {
            Text = existingAsset?.Source ?? "",
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 2
        };

        // Target
        var targetLabel = new Label() { Text = "Target:", X = 1, Y = 8 };
        var targetField = new TextField()
        {
            Text = existingAsset?.Target ?? "",
            X = 1,
            Y = 9,
            Width = Dim.Fill()! - 2
        };

        var saveButton = new Button()
        {
            Text = isEdit ? "Update" : "Add",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 3
        };
        saveButton.Accepting += (s, ev) =>
        {
            HandleSaveAsset(dialog, isEdit, existingAsset, opRadio, sourceField, targetField);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(opLabel, opRadio, sourceLabel, sourceField, targetLabel, targetField,
            saveButton, cancelButton);

        Application.Run(dialog);
    }

    private void HandleSaveAsset(
        Dialog dialog,
        bool isEdit,
        AssetManipulation? existingAsset,
        RadioGroup opRadio,
        TextField sourceField,
        TextField targetField)
    {
        var operation = opRadio.SelectedItem switch
        {
            0 => AssetOperation.Copy,
            1 => AssetOperation.Move,
            2 => AssetOperation.Delete,
            _ => AssetOperation.Copy
        };
        var source = sourceField.Text?.ToString() ?? "";
        var target = targetField.Text?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            MessageBox.ErrorQuery("Validation Error", "Source and target are required.", "OK");
            return;
        }

        if (isEdit && existingAsset != null)
        {
            existingAsset.Operation = operation;
            existingAsset.Source = source;
            existingAsset.Target = target;
            _statusLabel!.Text = $"Updated asset manipulation";
        }
        else
        {
            _currentConfig!.AssetManipulations.Add(new AssetManipulation
            {
                Operation = operation,
                Source = source,
                Target = target
            });
            _statusLabel!.Text = $"Added asset manipulation";
        }

        RefreshItemsList();
        Application.RequestStop(dialog);
    }

    private async Task LoadCurrentConfigAsync()
    {
        try
        {
            _statusLabel!.Text = "Loading config...";
            _currentConfig = await _configService.LoadAsync(_currentConfigPath);
            _configPathLabel!.Text = _currentConfigPath;
            RefreshItemsList();
            _statusLabel.Text = $"Loaded: {_currentConfig.Description} " +
                               $"({_currentConfig.Packages.Count} pkgs, " +
                               $"{_currentConfig.Assemblies.Count} asms, " +
                               $"{_currentConfig.AssetManipulations.Count} assets)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            _statusLabel!.Text = $"Error loading config: {ex.Message}";
            throw;
        }
    }

    private void RefreshItemsList()
    {
        if (_currentConfig == null || _itemsListView == null)
        {
            return;
        }

        var items = new List<string>();
        int count = 0;

        switch (_currentSection)
        {
            case "packages":
                foreach (var pkg in _currentConfig.Packages)
                {
                    items.Add($"{pkg.Name} v{pkg.Version} | {pkg.Source} → {pkg.Target}");
                }
                count = _currentConfig.Packages.Count;
                break;

            case "assemblies":
                foreach (var asm in _currentConfig.Assemblies)
                {
                    var ver = string.IsNullOrEmpty(asm.Version) ? "" : $" v{asm.Version}";
                    items.Add($"{asm.Name}{ver} | {asm.Source} → {asm.Target}");
                }
                count = _currentConfig.Assemblies.Count;
                break;

            case "assets":
                foreach (var asset in _currentConfig.AssetManipulations)
                {
                    items.Add($"[{asset.Operation.ToString().ToUpper()}] {asset.Source} → {asset.Target}");
                }
                count = _currentConfig.AssetManipulations.Count;
                break;
        }

        _itemsListView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));
        _itemCountLabel!.Text = $"{count} item(s)";
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
