using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Manual source management view - Terminal.Gui v2 implementation.
/// Allows interactive addition of packages, assemblies, and assets from anywhere.
/// </summary>
public class ManualSourcesView : View
{
    private readonly SourceManagementService _sourceManagementService;
    private readonly BatchManifestService _batchManifestService;
    private readonly ConfigService _configService;
    private readonly PathResolver _pathResolver;
    private readonly ILogger<ManualSourcesView> _logger;
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly List<IDisposable> _subscriptions = new();

    private string _currentConfigPath = "build/preparation/configs/default.json";

    // UI Controls
    private Label? _configPathLabel;
    private Label? _statusLabel;
    private Button? _addPackageButton;
    private Button? _addAssemblyButton;
    private Button? _addAssetButton;
    private Button? _addFromManifestButton;
    private Button? _viewItemsButton;
    private Button? _removeItemButton;
    private Button? _changeConfigButton;

    public ManualSourcesView(
        SourceManagementService sourceManagementService,
        BatchManifestService batchManifestService,
        ConfigService configService,
        PathResolver pathResolver,
        ILogger<ManualSourcesView> logger,
        ISubscriber<ConfigLoadedMessage> configLoaded)
    {
        _sourceManagementService = sourceManagementService;
        _batchManifestService = batchManifestService;
        _configService = configService;
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
            Text = "Manual Source Management",
            X = 1,
            Y = 0
        };

        // Current config section
        var configLabel = new Label()
        {
            Text = "Current Config:",
            X = 1,
            Y = 2
        };

        _configPathLabel = new Label()
        {
            Text = _currentConfigPath,
            X = Pos.Right(configLabel) + 1,
            Y = 2
        };

        _changeConfigButton = new Button()
        {
            Text = "Change...",
            X = Pos.Right(_configPathLabel) + 2,
            Y = 2
        };
        _changeConfigButton.Accepting += OnChangeConfig;

        // Status label
        _statusLabel = new Label()
        {
            Text = "Ready - Select an operation below",
            X = 1,
            Y = 4
        };

        // Main action buttons frame
        var actionsFrame = new FrameView()
        {
            Title = "Operations",
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 1,
            Height = 10
        };

        _addPackageButton = new Button()
        {
            Text = "Add Package (Phase 1: Source)",
            X = 2,
            Y = 0
        };
        _addPackageButton.Accepting += OnAddPackageSource;

        _addAssemblyButton = new Button()
        {
            Text = "Add Assembly (Phase 1: Source)",
            X = 2,
            Y = 1
        };
        _addAssemblyButton.Accepting += OnAddAssemblySource;

        _addAssetButton = new Button()
        {
            Text = "Add Asset (Phase 1: Source)",
            X = 2,
            Y = 2
        };
        _addAssetButton.Accepting += OnAddAssetSource;

        _addFromManifestButton = new Button()
        {
            Text = "Add from Batch Manifest",
            X = 2,
            Y = 4
        };
        _addFromManifestButton.Accepting += OnAddFromManifest;

        _viewItemsButton = new Button()
        {
            Text = "View Current Items",
            X = 2,
            Y = 6
        };
        _viewItemsButton.Accepting += OnViewItems;

        _removeItemButton = new Button()
        {
            Text = "Remove Item",
            X = 2,
            Y = 7
        };
        _removeItemButton.Accepting += OnRemoveItem;

        actionsFrame.Add(_addPackageButton, _addAssemblyButton, _addAssetButton,
            _addFromManifestButton, _viewItemsButton, _removeItemButton);

        // Info section
        var infoFrame = new FrameView()
        {
            Title = "Information",
            X = 1,
            Y = 17,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()! - 1
        };

        var infoText = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = @"MANUAL SOURCE MANAGEMENT

This screen allows you to add packages, assemblies, and assets from any location
on your system to the build preparation configuration.

TWO-PHASE WORKFLOW:

Phase 1: Source Collection (What you manage here)
  • Add sources from anywhere (external drives, network shares, etc.)
  • Sources are copied to cache
  • Config tracks: source → cache

Phase 2: Injection Mapping (Use CLI for now - TUI coming soon)
  • Define what gets injected into client
  • Config tracks: cache → client targets
  • Use: dotnet run -- config add-injection

OPERATIONS:

• Add Package/Assembly/Asset: Browse filesystem and select source location
• Add from Manifest: Batch import from YAML/JSON manifest file
• View Items: See all items currently in the configuration
• Remove Item: Remove a source from the configuration

SUPPORTED PATHS:
  • Absolute paths (any drive/location)
  • Relative paths (to git root)
  • UNC paths (network shares)
"
        };

        infoFrame.Add(infoText);

        Add(titleLabel, configLabel, _configPathLabel, _changeConfigButton,
            _statusLabel, actionsFrame, infoFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_configLoaded.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _currentConfigPath = msg.FilePath;
                _configPathLabel!.Text = _currentConfigPath;
                _statusLabel!.Text = $"Config loaded: {msg.FilePath}";
            });
        }));
    }

    private void OnChangeConfig(object? sender, EventArgs e)
    {
        var dialog = new Dialog()
        {
            Title = "Select Configuration",
            Width = 70,
            Height = 12
        };

        var label = new Label()
        {
            Text = "Config Path:",
            X = 1,
            Y = 1
        };

        var pathField = new TextField()
        {
            Text = _currentConfigPath,
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        var browseButton = new Button()
        {
            Text = "Browse...",
            X = 1,
            Y = 3
        };
        browseButton.Accepting += (s, ev) =>
        {
            var openDialog = new OpenDialog()
            {
                Title = "Select Configuration File",
                AllowsMultipleSelection = false
            };

            Application.Run(openDialog);

            if (!openDialog.Canceled && openDialog.Path != null)
            {
                pathField.Text = openDialog.Path.ToString();
            }
        };

        var okButton = new Button()
        {
            Text = "OK",
            IsDefault = true,
            X = Pos.Center() - 10,
            Y = Pos.Bottom(dialog) - 3
        };
        okButton.Accepting += (s, ev) =>
        {
            _currentConfigPath = pathField.Text?.ToString() ?? _currentConfigPath;
            _configPathLabel!.Text = _currentConfigPath;
            _statusLabel!.Text = $"Config changed to: {_currentConfigPath}";
            Application.RequestStop(dialog);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 2,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(label, pathField, browseButton, okButton, cancelButton);
        Application.Run(dialog);
    }

    private void OnAddPackageSource(object? sender, EventArgs e)
    {
        ShowAddSourceDialog("Package", "package");
    }

    private void OnAddAssemblySource(object? sender, EventArgs e)
    {
        ShowAddSourceDialog("Assembly", "assembly");
    }

    private void OnAddAssetSource(object? sender, EventArgs e)
    {
        ShowAddSourceDialog("Asset", "asset");
    }

    private void ShowAddSourceDialog(string typeName, string typeId)
    {
        var dialog = new Dialog()
        {
            Title = $"Add {typeName} Source",
            Width = 80,
            Height = 20
        };

        // Source path
        var sourceLabel = new Label()
        {
            Text = "Source Path:",
            X = 1,
            Y = 1
        };

        var sourceField = new TextField()
        {
            Text = "",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        var sourceBrowseButton = new Button()
        {
            Text = "Browse...",
            X = 1,
            Y = 3
        };
        sourceBrowseButton.Accepting += (s, ev) =>
        {
            var openDialog = new OpenDialog()
            {
                Title = $"Select {typeName} Source",
                AllowsMultipleSelection = false
            };

            Application.Run(openDialog);

            if (!openDialog.Canceled && openDialog.Path != null)
            {
                sourceField.Text = openDialog.Path.ToString();
            }
        };

        // Cache name
        var cacheNameLabel = new Label()
        {
            Text = "Cache Name:",
            X = 1,
            Y = 5
        };

        var cacheNameField = new TextField()
        {
            Text = "",
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 2
        };

        // Auto-fill cache name from source
        sourceField.TextChanged += (oldText, newText) =>
        {
            if (string.IsNullOrEmpty(cacheNameField.Text?.ToString()))
            {
                var path = newText.ToString() ?? "";
                var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                cacheNameField.Text = name;
            }
        };

        // Info label
        var infoLabel = new Label()
        {
            Text = $"This will copy the source to cache as specified.",
            X = 1,
            Y = 8
        };

        // Preview button
        var previewButton = new Button()
        {
            Text = "Preview",
            X = 1,
            Y = 10
        };
        previewButton.Accepting += (s, ev) =>
        {
            var source = sourceField.Text?.ToString() ?? "";
            var cacheName = cacheNameField.Text?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(cacheName))
            {
                MessageBox.ErrorQuery("Validation Error", "Source path and cache name are required.", "OK");
                return;
            }

            var preview = $"PREVIEW:\n\n" +
                         $"Type: {typeName}\n" +
                         $"Source: {source}\n" +
                         $"Cache As: {cacheName}\n" +
                         $"Config: {_currentConfigPath}\n\n" +
                         $"Action: Copy source to cache and add to config";

            MessageBox.Query("Preview", preview, "OK");
        };

        // Action buttons
        var addButton = new Button()
        {
            Text = "Add",
            IsDefault = true,
            X = Pos.Center() - 15,
            Y = Pos.Bottom(dialog) - 3
        };
        addButton.Accepting += async (s, ev) =>
        {
            await HandleAddSource(dialog, sourceField.Text?.ToString() ?? "",
                cacheNameField.Text?.ToString() ?? "", typeId);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(sourceLabel, sourceField, sourceBrowseButton,
            cacheNameLabel, cacheNameField, infoLabel,
            previewButton, addButton, cancelButton);

        Application.Run(dialog);
    }

    private async Task HandleAddSource(Dialog dialog, string source, string cacheName, string type)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(cacheName))
            {
                MessageBox.ErrorQuery("Validation Error", "Source path and cache name are required.", "OK");
                return;
            }

            _statusLabel!.Text = $"Adding {type} source...";
            Application.RequestStop(dialog);

            // Load or create the manifest
            var manifest = await _sourceManagementService.LoadOrCreateManifestAsync(_currentConfigPath);

            // Add the source using SourceManagementService
            var result = await _sourceManagementService.AddSourceAsync(
                manifest,
                source,
                cacheName,
                type,
                dryRun: false
            );

            if (result.Success)
            {
                // Save the updated manifest
                await _sourceManagementService.SaveManifestAsync(manifest, _currentConfigPath);

                _statusLabel.Text = $"Successfully added {type}: {cacheName}";
                MessageBox.Query("Success", $"{type} source added successfully!\n\nSource: {source}\nCache Name: {cacheName}", "OK");
            }
            else
            {
                _statusLabel.Text = $"Failed: {result.ErrorMessage}";
                MessageBox.ErrorQuery("Error", $"Failed to add source:\n{result.ErrorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add source");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Error", $"Failed to add source:\n{ex.Message}", "OK");
        }
    }

    private void OnAddFromManifest(object? sender, EventArgs e)
    {
        var dialog = new Dialog()
        {
            Title = "Add from Batch Manifest",
            Width = 80,
            Height = 16
        };

        // Manifest path
        var manifestLabel = new Label()
        {
            Text = "Manifest File:",
            X = 1,
            Y = 1
        };

        var manifestField = new TextField()
        {
            Text = "",
            X = 1,
            Y = 2,
            Width = Dim.Fill()! - 2
        };

        var browsButton = new Button()
        {
            Text = "Browse...",
            X = 1,
            Y = 3
        };
        browsButton.Accepting += (s, ev) =>
        {
            var openDialog = new OpenDialog()
            {
                Title = "Select Batch Manifest",
                AllowsMultipleSelection = false
            };

            Application.Run(openDialog);

            if (!openDialog.Canceled && openDialog.Path != null)
            {
                manifestField.Text = openDialog.Path.ToString();
            }
        };

        // Config type radio
        var configTypeLabel = new Label()
        {
            Text = "Config Type:",
            X = 1,
            Y = 5
        };

        var sourceRadio = new RadioGroup()
        {
            X = 1,
            Y = 6,
            RadioLabels = new[] { "Phase 1: Source Collection", "Phase 2: Injection Mapping" },
            SelectedItem = 0
        };

        // Continue on error checkbox
        var continueCheckbox = new CheckBox()
        {
            Text = "Continue on error",
            X = 1,
            Y = 9,
            CheckedState = CheckState.UnChecked
        };

        // Info
        var infoLabel = new Label()
        {
            Text = "Manifest can be YAML or JSON format. See documentation for schema.",
            X = 1,
            Y = 11
        };

        // Action buttons
        var addButton = new Button()
        {
            Text = "Add Batch",
            IsDefault = true,
            X = Pos.Center() - 15,
            Y = Pos.Bottom(dialog) - 3
        };
        addButton.Accepting += async (s, ev) =>
        {
            await HandleAddBatch(dialog, manifestField.Text?.ToString() ?? "",
                sourceRadio.SelectedItem == 0, continueCheckbox.CheckedState == CheckState.Checked);
        };

        var cancelButton = new Button()
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = Pos.Bottom(dialog) - 3
        };
        cancelButton.Accepting += (s, ev) => Application.RequestStop(dialog);

        dialog.Add(manifestLabel, manifestField, browsButton,
            configTypeLabel, sourceRadio, continueCheckbox, infoLabel,
            addButton, cancelButton);

        Application.Run(dialog);
    }

    private async Task HandleAddBatch(Dialog dialog, string manifestPath, bool isSourcePhase, bool continueOnError)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                MessageBox.ErrorQuery("Validation Error", "Manifest file path is required.", "OK");
                return;
            }

            _statusLabel!.Text = "Processing batch manifest...";
            Application.RequestStop(dialog);

            // Load the batch manifest
            var batchManifest = await _batchManifestService.LoadBatchManifestAsync(manifestPath);

            // Load or create the target manifest/config
            if (isSourcePhase)
            {
                var manifest = await _sourceManagementService.LoadOrCreateManifestAsync(_currentConfigPath);
                var result = await _sourceManagementService.ProcessBatchSourcesAsync(batchManifest, manifest, continueOnError);

                // Save the updated manifest
                await _sourceManagementService.SaveManifestAsync(manifest, _currentConfigPath);

                ShowBatchResult(result);
            }
            else
            {
                var config = await _configService.LoadAsync(_currentConfigPath);
                var result = _sourceManagementService.ProcessBatchInjections(batchManifest, config, false, continueOnError);

                // Save the updated config
                await _configService.SaveAsync(config, _currentConfigPath);

                ShowBatchResult(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process batch");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Error", $"Failed to process batch:\n{ex.Message}", "OK");
        }
    }

    private void ShowBatchResult(BatchProcessingResult result)
    {
        var totalCount = result.SuccessCount + result.FailureCount;
        var summary = $"Batch processing complete!\n\n" +
                     $"Success: {result.SuccessCount}\n" +
                     $"Failed: {result.FailureCount}\n" +
                     $"Total: {totalCount}";

        if (result.FailedItems.Count > 0)
        {
            summary += "\n\nErrors:\n";
            foreach (var (item, error) in result.FailedItems.Take(5))
            {
                summary += $"  {item}: {error}\n";
            }
            if (result.FailedItems.Count > 5)
            {
                summary += $"\n... and {result.FailedItems.Count - 5} more";
            }
        }

        _statusLabel!.Text = $"Batch complete: {result.SuccessCount} success, {result.FailureCount} failed";
        MessageBox.Query("Batch Complete", summary, "OK");
    }

    private async void OnViewItems(object? sender, EventArgs e)
    {
        try
        {
            _statusLabel!.Text = "Loading configuration...";

            var config = await _configService.LoadAsync(_currentConfigPath);

            var items = new List<string>();
            items.Add("=== PACKAGES ===");
            foreach (var pkg in config.Packages)
            {
                items.Add($"  {pkg.Name} v{pkg.Version}");
                items.Add($"    Source: {pkg.Source}");
                items.Add($"    Target: {pkg.Target}");
                items.Add("");
            }

            items.Add("=== ASSEMBLIES ===");
            foreach (var asm in config.Assemblies)
            {
                items.Add($"  {asm.Name} v{asm.Version}");
                items.Add($"    Source: {asm.Source}");
                items.Add($"    Target: {asm.Target}");
                items.Add("");
            }

            if (config.AssetManipulations.Count > 0)
            {
                items.Add("=== ASSETS ===");
                foreach (var asset in config.AssetManipulations)
                {
                    items.Add($"  {asset.Operation}: {asset.Source} → {asset.Target}");
                    items.Add("");
                }
            }

            var dialog = new Dialog()
            {
                Title = $"Items in {Path.GetFileName(_currentConfigPath)}",
                Width = Dim.Percent(80),
                Height = Dim.Percent(80)
            };

            var listView = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill()! - 2,
                Height = Dim.Fill()! - 4
            };
            listView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(items));

            var closeButton = new Button()
            {
                Text = "Close",
                X = Pos.Center(),
                Y = Pos.Bottom(dialog) - 2
            };
            closeButton.Accepting += (s, ev) => Application.RequestStop(dialog);

            dialog.Add(listView, closeButton);
            Application.Run(dialog);

            _statusLabel.Text = $"Showing {config.Packages.Count} packages, {config.Assemblies.Count} assemblies";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config items");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Error", $"Failed to load config:\n{ex.Message}", "OK");
        }
    }

    private void OnRemoveItem(object? sender, EventArgs e)
    {
        MessageBox.Query("Remove Item",
            "Item removal is not yet implemented in this phase.\n\n" +
            "For now, manually edit the configuration file or use CLI commands.",
            "OK");
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
