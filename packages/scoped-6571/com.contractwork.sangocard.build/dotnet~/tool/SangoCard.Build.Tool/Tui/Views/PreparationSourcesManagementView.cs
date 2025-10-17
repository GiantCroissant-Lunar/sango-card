using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Preparation sources management view - dedicated screen for managing Phase 1 configs.
/// Provides full CRUD operations for preparation source manifests.
/// </summary>
public class PreparationSourcesManagementView : View
{
    private readonly SourceManagementService _sourceManagementService;
    private readonly PathResolver _pathResolver;
    private readonly ILogger<PreparationSourcesManagementView> _logger;
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly List<IDisposable> _subscriptions = new();

    private string _currentManifestPath = "build/preparation/manifests/default.json";
    private PreparationManifest? _currentManifest;

    // UI Controls
    private Label? _manifestPathLabel;
    private Label? _statusLabel;
    private ListView? _itemsListView;
    private Button? _loadButton;
    private Button? _saveButton;
    private Button? _newButton;
    private Button? _addItemButton;
    private Button? _editItemButton;
    private Button? _removeItemButton;
    private Button? _previewButton;
    private Label? _itemCountLabel;

    public PreparationSourcesManagementView(
        SourceManagementService sourceManagementService,
        PathResolver pathResolver,
        ILogger<PreparationSourcesManagementView> logger,
        ISubscriber<ConfigLoadedMessage> configLoaded)
    {
        _sourceManagementService = sourceManagementService;
        _pathResolver = pathResolver;
        _logger = logger;
        _configLoaded = configLoaded;

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
            Text = "Preparation Sources Management (Phase 1)",
            X = 1,
            Y = 0
        };

        // Manifest path section
        var pathLabel = new Label()
        {
            Text = "Manifest:",
            X = 1,
            Y = 2
        };

        _manifestPathLabel = new Label()
        {
            Text = _currentManifestPath,
            X = Pos.Right(pathLabel) + 1,
            Y = 2,
            Width = 60
        };

        _loadButton = new Button()
        {
            Text = "Load...",
            X = Pos.Right(_manifestPathLabel) + 2,
            Y = 2
        };
        _loadButton.Accepting += OnLoadManifest;

        _newButton = new Button()
        {
            Text = "New",
            X = Pos.Right(_loadButton) + 2,
            Y = 2
        };
        _newButton.Accepting += OnNewManifest;

        _saveButton = new Button()
        {
            Text = "Save",
            X = Pos.Right(_newButton) + 2,
            Y = 2
        };
        _saveButton.Accepting += OnSaveManifest;

        // Status label
        _statusLabel = new Label()
        {
            Text = "No manifest loaded",
            X = 1,
            Y = 4
        };

        // Items frame
        var itemsFrame = new FrameView()
        {
            Title = "Source Items",
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()! - 10
        };

        _itemsListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };
        // Note: SelectedItemChanged event will be handled via OpenSelectedItem command

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

        Add(titleLabel, pathLabel, _manifestPathLabel, _loadButton, _newButton, _saveButton,
            _statusLabel, itemsFrame, actionsFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_configLoaded.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"External config loaded: {msg.FilePath}";
            });
        }));
    }

    private async void OnLoadManifest(object? sender, EventArgs e)
    {
        var dialog = new OpenDialog()
        {
            Title = "Load Preparation Manifest",
            AllowsMultipleSelection = false
        };

        Application.Run(dialog);

        if (!dialog.Canceled && dialog.Path != null)
        {
            try
            {
                _currentManifestPath = dialog.Path.ToString() ?? _currentManifestPath;
                await LoadCurrentManifestAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load manifest");
                MessageBox.ErrorQuery("Error", $"Failed to load manifest:\n{ex.Message}", "OK");
            }
        }
    }

    private async void OnNewManifest(object? sender, EventArgs e)
    {
        var dialog = new Dialog()
        {
            Title = "New Preparation Manifest",
            Width = 70,
            Height = 16
        };

        var pathLabel = new Label()
        {
            Text = "Save As:",
            X = 1,
            Y = 1
        };

        var pathField = new TextField()
        {
            Text = "build/preparation/manifests/new-manifest.json",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        var idLabel = new Label()
        {
            Text = "Manifest ID:",
            X = 1,
            Y = 4
        };

        var idField = new TextField()
        {
            Text = "new-manifest",
            X = 1,
            Y = 5,
            Width = Dim.Fill()! - 2
        };

        var titleLabel = new Label()
        {
            Text = "Title:",
            X = 1,
            Y = 7
        };

        var titleField = new TextField()
        {
            Text = "New Preparation Manifest",
            X = 1,
            Y = 8,
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
                _currentManifestPath = pathField.Text?.ToString() ?? _currentManifestPath;
                _currentManifest = new PreparationManifest
                {
                    Id = idField.Text?.ToString() ?? "new-manifest",
                    Title = titleField.Text?.ToString() ?? "New Manifest",
                    CacheDirectory = "build/preparation/cache",
                    Items = new List<PreparationItem>()
                };

                await _sourceManagementService.SaveManifestAsync(_currentManifest, _currentManifestPath);
                RefreshItemsList();
                _statusLabel!.Text = $"Created new manifest: {_currentManifestPath}";
                _manifestPathLabel!.Text = _currentManifestPath;

                Application.RequestStop(dialog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create manifest");
                MessageBox.ErrorQuery("Error", $"Failed to create manifest:\n{ex.Message}", "OK");
            }
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(pathLabel, pathField, idLabel, idField, titleLabel, titleField,
            createButton, cancelButton);

        Application.Run(dialog);
    }

    private async void OnSaveManifest(object? sender, EventArgs e)
    {
        if (_currentManifest == null)
        {
            MessageBox.ErrorQuery("Error", "No manifest loaded", "OK");
            return;
        }

        try
        {
            await _sourceManagementService.SaveManifestAsync(_currentManifest, _currentManifestPath);
            _statusLabel!.Text = $"Saved manifest: {_currentManifestPath}";
            MessageBox.Query("Success", $"Manifest saved successfully to:\n{_currentManifestPath}", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save manifest");
            MessageBox.ErrorQuery("Error", $"Failed to save manifest:\n{ex.Message}", "OK");
        }
    }

    private void OnAddItem(object? sender, EventArgs e)
    {
        if (_currentManifest == null)
        {
            MessageBox.ErrorQuery("Error", "No manifest loaded. Create or load a manifest first.", "OK");
            return;
        }

        ShowAddEditItemDialog(null);
    }

    private void OnEditItem(object? sender, EventArgs e)
    {
        if (_currentManifest == null || _itemsListView == null)
        {
            return;
        }

        var selectedIndex = _itemsListView.SelectedItem;
        if (selectedIndex < 0 || selectedIndex >= _currentManifest.Items.Count)
        {
            MessageBox.ErrorQuery("Error", "Please select an item to edit.", "OK");
            return;
        }

        var item = _currentManifest.Items[selectedIndex];
        ShowAddEditItemDialog(item);
    }

    private void OnRemoveItem(object? sender, EventArgs e)
    {
        if (_currentManifest == null || _itemsListView == null)
        {
            return;
        }

        var selectedIndex = _itemsListView.SelectedItem;
        if (selectedIndex < 0 || selectedIndex >= _currentManifest.Items.Count)
        {
            MessageBox.ErrorQuery("Error", "Please select an item to remove.", "OK");
            return;
        }

        var item = _currentManifest.Items[selectedIndex];
        var result = MessageBox.Query("Confirm Remove",
            $"Remove this item from manifest?\n\n" +
            $"Type: {item.Type}\n" +
            $"Source: {item.Source}\n" +
            $"Cache As: {item.CacheAs}\n\n" +
            $"Note: This only removes from manifest. Cached files are not deleted.",
            "Remove", "Cancel");

        if (result == 0)
        {
            _currentManifest.Items.RemoveAt(selectedIndex);
            RefreshItemsList();
            _statusLabel!.Text = $"Removed item: {item.CacheAs}";
        }
    }

    private void OnPreview(object? sender, EventArgs e)
    {
        if (_currentManifest == null)
        {
            MessageBox.ErrorQuery("Error", "No manifest loaded", "OK");
            return;
        }

        var preview = $"PREPARATION MANIFEST PREVIEW\n\n" +
                     $"ID: {_currentManifest.Id}\n" +
                     $"Title: {_currentManifest.Title}\n" +
                     $"Cache Directory: {_currentManifest.CacheDirectory}\n" +
                     $"Items: {_currentManifest.Items.Count}\n\n";

        preview += "OPERATIONS:\n";
        foreach (var item in _currentManifest.Items)
        {
            preview += $"\n{item.Type.ToUpper()}:\n";
            preview += $"  Source: {item.Source}\n";
            preview += $"  → Cache As: {item.CacheAs}\n";
        }

        MessageBox.Query("Preview", preview, "OK");
    }

    private void ShowAddEditItemDialog(PreparationItem? existingItem)
    {
        var isEdit = existingItem != null;
        var dialog = new Dialog()
        {
            Title = isEdit ? "Edit Source Item" : "Add Source Item",
            Width = 80,
            Height = 20
        };

        // Type selection
        var typeLabel = new Label()
        {
            Text = "Type:",
            X = 1,
            Y = 1
        };

        var typeRadio = new RadioGroup()
        {
            X = 1,
            Y = 2,
            RadioLabels = new[] { "Package", "Assembly", "Asset" },
            SelectedItem = existingItem?.Type switch
            {
                PreparationItemType.Package => 0,
                PreparationItemType.Assembly => 1,
                PreparationItemType.Asset => 2,
                _ => 0
            }
        };

        // Source path
        var sourceLabel = new Label()
        {
            Text = "Source Path:",
            X = 1,
            Y = 5
        };

        var sourceField = new TextField()
        {
            Text = existingItem?.Source ?? "",
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 2
        };

        var sourceBrowseButton = new Button()
        {
            Text = "Browse...",
            X = 1,
            Y = 7
        };
        sourceBrowseButton.Accepting += (s, ev) =>
        {
            var openDialog = new OpenDialog()
            {
                Title = "Select Source",
                AllowsMultipleSelection = false
            };

            Application.Run(openDialog);

            if (!openDialog.Canceled && openDialog.Path != null)
            {
                sourceField.Text = openDialog.Path.ToString();
            }
        };

        // Cache As
        var cacheAsLabel = new Label()
        {
            Text = "Cache As:",
            X = 1,
            Y = 9
        };

        var cacheAsField = new TextField()
        {
            Text = existingItem?.CacheAs ?? "",
            X = 1,
            Y = 10,
            Width = Dim.Fill()! - 2
        };

        // Auto-fill cache name from source
        sourceField.TextChanged += (oldText, newText) =>
        {
            if (string.IsNullOrEmpty(cacheAsField.Text?.ToString()))
            {
                var path = newText.ToString() ?? "";
                var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                cacheAsField.Text = name;
            }
        };

        // Action buttons
        var saveButton = new Button()
        {
            Text = isEdit ? "Update" : "Add",
            IsDefault = true,
            X = Pos.Center() - 15,
            Y = Pos.Bottom(dialog) - 3
        };
        saveButton.Accepting += async (s, ev) =>
        {
            await HandleSaveItem(dialog, isEdit, existingItem, typeRadio, sourceField, cacheAsField);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(typeLabel, typeRadio, sourceLabel, sourceField, sourceBrowseButton,
            cacheAsLabel, cacheAsField, saveButton, cancelButton);

        Application.Run(dialog);
    }

    private async Task HandleSaveItem(
        Dialog dialog,
        bool isEdit,
        PreparationItem? existingItem,
        RadioGroup typeRadio,
        TextField sourceField,
        TextField cacheAsField)
    {
        try
        {
            var source = sourceField.Text?.ToString() ?? "";
            var cacheAs = cacheAsField.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(cacheAs))
            {
                MessageBox.ErrorQuery("Validation Error", "Source path and Cache As are required.", "OK");
                return;
            }

            var type = typeRadio.SelectedItem switch
            {
                0 => PreparationItemType.Package,
                1 => PreparationItemType.Assembly,
                2 => PreparationItemType.Asset,
                _ => PreparationItemType.Package
            };

            if (isEdit && existingItem != null)
            {
                // Update existing item
                existingItem.Source = source;
                existingItem.CacheAs = cacheAs;
                existingItem.Type = type;
                _statusLabel!.Text = $"Updated item: {cacheAs}";
            }
            else
            {
                // Check for duplicates
                var duplicate = _currentManifest!.Items.FirstOrDefault(i =>
                    i.CacheAs.Equals(cacheAs, StringComparison.OrdinalIgnoreCase));

                if (duplicate != null)
                {
                    MessageBox.ErrorQuery("Error", $"An item with cache name '{cacheAs}' already exists.", "OK");
                    return;
                }

                // Add new item
                _currentManifest!.Items.Add(new PreparationItem
                {
                    Source = source,
                    CacheAs = cacheAs,
                    Type = type
                });
                _statusLabel!.Text = $"Added item: {cacheAs}";
            }

            RefreshItemsList();
            Application.RequestStop(dialog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save item");
            MessageBox.ErrorQuery("Error", $"Failed to save item:\n{ex.Message}", "OK");
        }
    }

    private async Task LoadCurrentManifestAsync()
    {
        try
        {
            _statusLabel!.Text = "Loading manifest...";
            _currentManifest = await _sourceManagementService.LoadOrCreateManifestAsync(_currentManifestPath);
            _manifestPathLabel!.Text = _currentManifestPath;
            RefreshItemsList();
            _statusLabel.Text = $"Loaded: {_currentManifest.Title} ({_currentManifest.Items.Count} items)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load manifest");
            _statusLabel!.Text = $"Error loading manifest: {ex.Message}";
            throw;
        }
    }

    private void RefreshItemsList()
    {
        if (_currentManifest == null || _itemsListView == null)
        {
            return;
        }

        var items = new List<string>();
        foreach (var item in _currentManifest.Items)
        {
            items.Add($"[{item.Type.ToUpper()}] {item.CacheAs} ← {item.Source}");
        }

        _itemsListView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));
        _itemCountLabel!.Text = $"{_currentManifest.Items.Count} item(s)";
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
