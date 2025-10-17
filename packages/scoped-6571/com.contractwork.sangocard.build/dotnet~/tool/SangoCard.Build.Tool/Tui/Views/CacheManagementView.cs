using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Cache management view - Terminal.Gui v2 implementation.
/// Allows populating, listing, and cleaning the preparation cache.
/// </summary>
public class CacheManagementView : View
{
    private readonly CacheService _cacheService;
    private readonly PathResolver _pathResolver;
    private readonly ILogger<CacheManagementView> _logger;
    private readonly ISubscriber<CachePopulatedMessage> _cachePopulated;
    private readonly ISubscriber<CacheCleanedMessage> _cacheCleaned;
    private readonly ISubscriber<CacheItemAddedMessage> _cacheItemAdded;
    private readonly List<IDisposable> _subscriptions = new();

    private List<CacheItem> _cacheItems = new();

    // UI Controls
    private TextField? _sourcePathField;
    private TextField? _cachePathField;
    private Label? _statusLabel;
    private ListView? _cacheListView;
    private Label? _cacheStatsLabel;
    private Button? _populateButton;
    private Button? _refreshButton;
    private Button? _cleanButton;
    private ProgressBar? _progressBar;

    public CacheManagementView(
        CacheService cacheService,
        PathResolver pathResolver,
        ILogger<CacheManagementView> logger,
        ISubscriber<CachePopulatedMessage> cachePopulated,
        ISubscriber<CacheCleanedMessage> cacheCleaned,
        ISubscriber<CacheItemAddedMessage> cacheItemAdded)
    {
        _cacheService = cacheService;
        _pathResolver = pathResolver;
        _logger = logger;
        _cachePopulated = cachePopulated;
        _cacheCleaned = cacheCleaned;
        _cacheItemAdded = cacheItemAdded;

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        InitializeComponent();
        SubscribeToMessages();
        _ = LoadCacheItemsAsync();
    }

    private void InitializeComponent()
    {
        // Title
        var titleLabel = new Label()
        {
            Text = "Cache Management",
            X = 1,
            Y = 0
        };

        // Source path section
        var sourceLabel = new Label()
        {
            Text = "Source Path:",
            X = 1,
            Y = 2
        };

        _sourcePathField = new TextField()
        {
            Text = "projects/code-quality",
            X = Pos.Right(sourceLabel) + 1,
            Y = 2,
            Width = 50
        };

        // Cache path section
        var cacheLabel = new Label()
        {
            Text = "Cache Path:",
            X = 1,
            Y = 3
        };

        _cachePathField = new TextField()
        {
            Text = CacheService.DefaultCacheDirectory,
            X = Pos.Right(cacheLabel) + 1,
            Y = 3,
            Width = 50
        };

        // Buttons
        _populateButton = new Button()
        {
            Text = "Populate Cache",
            X = 1,
            Y = 5
        };
        _populateButton.Accepting += OnPopulateCache;

        _refreshButton = new Button()
        {
            Text = "Refresh",
            X = Pos.Right(_populateButton) + 2,
            Y = 5
        };
        _refreshButton.Accepting += OnRefreshCache;

        _cleanButton = new Button()
        {
            Text = "Clean Cache",
            X = Pos.Right(_refreshButton) + 2,
            Y = 5
        };
        _cleanButton.Accepting += OnCleanCache;

        // Progress bar
        _progressBar = new ProgressBar()
        {
            X = Pos.Right(_cleanButton) + 2,
            Y = 5,
            Width = Dim.Fill(),
            Visible = false
        };

        // Status label
        _statusLabel = new Label()
        {
            Text = "Ready",
            X = 1,
            Y = 7
        };

        // Cache stats
        _cacheStatsLabel = new Label()
        {
            Text = "Cache: 0 items, 0 bytes",
            X = Pos.Right(_statusLabel) + 10,
            Y = 7
        };

        // Cache list frame
        var cacheFrame = new FrameView()
        {
            Title = "Cache Contents",
            X = 1,
            Y = 9,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()! - 1
        };

        _cacheListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        cacheFrame.Add(_cacheListView);

        Add(titleLabel, sourceLabel, _sourcePathField, cacheLabel, _cachePathField,
            _populateButton, _refreshButton, _cleanButton, _progressBar,
            _statusLabel, _cacheStatsLabel, cacheFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_cachePopulated.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Populated: {msg.ItemCount} items from {msg.SourcePath}";
                _progressBar!.Visible = false;
                _ = LoadCacheItemsAsync();
            });
        }));

        _subscriptions.Add(_cacheCleaned.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Cleaned: {msg.RemovedCount} items removed";
                _ = LoadCacheItemsAsync();
            });
        }));

        _subscriptions.Add(_cacheItemAdded.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _logger.LogDebug("Cache item added: {Path}", msg.Item.Path);
            });
        }));
    }

    private async void OnPopulateCache(object? sender, EventArgs e)
    {
        try
        {
            var sourcePath = _sourcePathField!.Text ?? "projects/code-quality";
            var cachePath = _cachePathField!.Text ?? CacheService.DefaultCacheDirectory;

            _statusLabel!.Text = $"Populating cache from {sourcePath}...";
            _progressBar!.Visible = true;
            _progressBar.Fraction = 0.5f;
            _populateButton!.Enabled = false;

            var items = await _cacheService.PopulateFromDirectoryAsync(sourcePath, cachePath);

            _statusLabel.Text = $"Success: Added {items.Count} items to cache";
            _progressBar.Visible = false;
            _populateButton.Enabled = true;

            await LoadCacheItemsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to populate cache");
            _statusLabel!.Text = $"Error: {ex.Message}";
            _progressBar!.Visible = false;
            _populateButton!.Enabled = true;
            MessageBox.ErrorQuery("Cache Error", ex.Message, "OK");
        }
    }

    private async void OnRefreshCache(object? sender, EventArgs e)
    {
        _statusLabel!.Text = "Refreshing cache list...";
        await LoadCacheItemsAsync();
        _statusLabel.Text = "Cache list refreshed";
    }

    private async void OnCleanCache(object? sender, EventArgs e)
    {
        var result = MessageBox.Query("Confirm Clean",
            "Are you sure you want to clean the cache?\nThis will remove all cached items.",
            "Yes", "No");

        if (result != 0) return;

        try
        {
            var cachePath = _cachePathField!.Text ?? CacheService.DefaultCacheDirectory;
            _statusLabel!.Text = "Cleaning cache...";

            var removed = _cacheService.CleanCache(cachePath);

            _statusLabel.Text = $"Cache cleaned successfully - {removed} items removed";
            await LoadCacheItemsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean cache");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Clean Error", ex.Message, "OK");
        }
    }

    private async Task LoadCacheItemsAsync()
    {
        try
        {
            var cachePath = _cachePathField!.Text ?? CacheService.DefaultCacheDirectory;
            _cacheItems = await _cacheService.ListCacheAsync(cachePath);

            var displayItems = _cacheItems
                .Select(item => $"{item.Type,-12} {item.Name,-40} {FormatSize(item.Size),10}")
                .ToList();

            Application.Invoke(() =>
            {
                _cacheListView!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(displayItems));

                var totalSize = _cacheItems.Sum(i => i.Size);
                _cacheStatsLabel!.Text = $"Cache: {_cacheItems.Count} items, {FormatSize(totalSize)}";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cache items");
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Error loading cache: {ex.Message}";
            });
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
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
