using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Preparation execution view - Terminal.Gui v2 implementation.
/// Executes build preparation with real-time progress tracking.
/// </summary>
public class PreparationExecutionView : View
{
    private readonly ConfigService _configService;
    private readonly PreparationService _preparationService;
    private readonly ILogger<PreparationExecutionView> _logger;
    private readonly ISubscriber<PreparationStartedMessage> _prepStarted;
    private readonly ISubscriber<PreparationCompletedMessage> _prepCompleted;
    private readonly ISubscriber<PreparationFailedMessage> _prepFailed;
    private readonly ISubscriber<PreparationProgressMessage> _prepProgress;
    private readonly ISubscriber<FileCopiedMessage> _fileCopied;
    private readonly ISubscriber<FileMovedMessage> _fileMoved;
    private readonly ISubscriber<FileDeletedMessage> _fileDeleted;
    private readonly List<IDisposable> _subscriptions = new();

    private PreparationConfig? _currentConfig;
    private bool _isExecuting;
    private DateTime _executionStart;

    // UI Controls
    private TextField? _configPathField;
    private CheckBox? _dryRunCheckBox;
    private CheckBox? _validateCheckBox;
    private Label? _statusLabel;
    private Button? _loadButton;
    private Button? _executeButton;
    private Button? _stopButton;
    private ProgressBar? _progressBar;
    private Label? _progressLabel;
    private TextView? _logTextView;
    private Label? _statsLabel;

    private int _filesCopied;
    private int _filesMoved;
    private int _filesDeleted;
    private int _filesPatched;

    public PreparationExecutionView(
        ConfigService configService,
        PreparationService preparationService,
        ILogger<PreparationExecutionView> logger,
        ISubscriber<PreparationStartedMessage> prepStarted,
        ISubscriber<PreparationCompletedMessage> prepCompleted,
        ISubscriber<PreparationFailedMessage> prepFailed,
        ISubscriber<PreparationProgressMessage> prepProgress,
        ISubscriber<FileCopiedMessage> fileCopied,
        ISubscriber<FileMovedMessage> fileMoved,
        ISubscriber<FileDeletedMessage> fileDeleted)
    {
        _configService = configService;
        _preparationService = preparationService;
        _logger = logger;
        _prepStarted = prepStarted;
        _prepCompleted = prepCompleted;
        _prepFailed = prepFailed;
        _prepProgress = prepProgress;
        _fileCopied = fileCopied;
        _fileMoved = fileMoved;
        _fileDeleted = fileDeleted;

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
            Text = "Preparation Execution",
            X = 1,
            Y = 0
        };

        // Config path section
        var pathLabel = new Label()
        {
            Text = "Config Path:",
            X = 1,
            Y = 2
        };

        _configPathField = new TextField()
        {
            Text = "build/preparation/configs/dev.json",
            X = Pos.Right(pathLabel) + 1,
            Y = 2,
            Width = 50
        };

        _loadButton = new Button()
        {
            Text = "Load",
            X = Pos.Right(_configPathField) + 2,
            Y = 2
        };
        _loadButton.Accepting += OnLoadConfig;

        // Options section
        _dryRunCheckBox = new CheckBox()
        {
            Text = "Dry Run (simulation mode)",
            X = 1,
            Y = 4,
            CheckedState = CheckState.Checked
        };

        _validateCheckBox = new CheckBox()
        {
            Text = "Validate before execution",
            X = Pos.Right(_dryRunCheckBox) + 5,
            Y = 4,
            CheckedState = CheckState.Checked
        };

        // Action buttons
        _executeButton = new Button()
        {
            Text = "Execute Preparation",
            X = 1,
            Y = 6,
            Enabled = false
        };
        _executeButton.Accepting += OnExecute;

        _stopButton = new Button()
        {
            Text = "Stop",
            X = Pos.Right(_executeButton) + 2,
            Y = 6,
            Enabled = false
        };
        _stopButton.Accepting += OnStop;

        // Progress section
        _progressBar = new ProgressBar()
        {
            X = 1,
            Y = 8,
            Width = Dim.Fill()! - 1,
            Visible = false
        };

        _progressLabel = new Label()
        {
            Text = "Ready",
            X = 1,
            Y = 9
        };

        // Status label
        _statusLabel = new Label()
        {
            Text = "Load a configuration to begin",
            X = 1,
            Y = 10
        };

        // Stats label
        _statsLabel = new Label()
        {
            Text = "Copied: 0 | Moved: 0 | Deleted: 0 | Patched: 0",
            X = 1,
            Y = 11
        };

        // Execution log
        var logFrame = new FrameView()
        {
            Title = "Execution Log",
            X = 1,
            Y = 13,
            Width = Dim.Fill()! - 1,
            Height = Dim.Fill()! - 1
        };

        _logTextView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = "No execution started yet.\n\nLoad a configuration and click 'Execute Preparation' to begin."
        };

        logFrame.Add(_logTextView);

        Add(titleLabel, pathLabel, _configPathField, _loadButton, _dryRunCheckBox, _validateCheckBox,
            _executeButton, _stopButton, _progressBar, _progressLabel, _statusLabel, _statsLabel, logFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_prepStarted.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _executionStart = DateTime.UtcNow;
                _filesCopied = _filesMoved = _filesDeleted = _filesPatched = 0;

                var mode = msg.IsDryRun ? "DRY-RUN" : "EXECUTION";
                AppendLog($"=== Preparation {mode} Started ===");
                AppendLog($"Client Path: {msg.ClientPath}");
                AppendLog($"Config: {msg.ConfigPath ?? "Inline"}");
                AppendLog($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                AppendLog("");

                _progressBar!.Visible = true;
                _progressBar.Fraction = 0.1f;
                _progressLabel!.Text = "Starting preparation...";
                _executeButton!.Enabled = false;
                _stopButton!.Enabled = true;
                _isExecuting = true;
            });
        }));

        _subscriptions.Add(_prepCompleted.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                var duration = DateTime.UtcNow - _executionStart;
                AppendLog("");
                AppendLog("=== Preparation Completed Successfully ===");
                AppendLog($"Files Copied: {msg.Copied}");
                AppendLog($"Files Moved: {msg.Moved}");
                AppendLog($"Files Deleted: {msg.Deleted}");
                AppendLog($"Files Patched: {msg.Patched}");
                AppendLog($"Duration: {msg.Duration.TotalSeconds:F2}s");
                if (!string.IsNullOrEmpty(msg.BackupPath))
                    AppendLog($"Backup: {msg.BackupPath}");
                AppendLog("");

                _progressBar!.Visible = false;
                _progressLabel!.Text = "Completed";
                _statusLabel!.Text = $"✓ Success - {msg.Copied + msg.Moved + msg.Deleted + msg.Patched} operations in {msg.Duration.TotalSeconds:F1}s";
                _executeButton!.Enabled = true;
                _stopButton!.Enabled = false;
                _isExecuting = false;

                UpdateStats(msg.Copied, msg.Moved, msg.Deleted, msg.Patched);
            });
        }));

        _subscriptions.Add(_prepFailed.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                AppendLog("");
                AppendLog("=== Preparation Failed ===");
                AppendLog($"Error: {msg.Error}");
                AppendLog($"Rolled Back: {msg.WasRolledBack}");
                AppendLog("");

                _progressBar!.Visible = false;
                _progressLabel!.Text = "Failed";
                _statusLabel!.Text = $"✗ Failed - {msg.Error}";
                _executeButton!.Enabled = true;
                _stopButton!.Enabled = false;
                _isExecuting = false;

                MessageBox.ErrorQuery("Preparation Failed", msg.Error, "OK");
            });
        }));

        _subscriptions.Add(_prepProgress.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                var fraction = msg.TotalSteps > 0 ? (float)msg.CurrentStep / msg.TotalSteps : 0f;
                _progressBar!.Fraction = fraction;
                _progressLabel!.Text = $"Step {msg.CurrentStep}/{msg.TotalSteps}: {msg.Message}";
                AppendLog($"[{msg.CurrentStep}/{msg.TotalSteps}] {msg.Message}");
            });
        }));

        _subscriptions.Add(_fileCopied.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _filesCopied++;
                AppendLog($"  COPY: {msg.Source} -> {msg.Target}");
                UpdateStats(_filesCopied, _filesMoved, _filesDeleted, _filesPatched);
            });
        }));

        _subscriptions.Add(_fileMoved.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _filesMoved++;
                AppendLog($"  MOVE: {msg.Source} -> {msg.Target}");
                UpdateStats(_filesCopied, _filesMoved, _filesDeleted, _filesPatched);
            });
        }));

        _subscriptions.Add(_fileDeleted.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _filesDeleted++;
                AppendLog($"  DELETE: {msg.Path}");
                UpdateStats(_filesCopied, _filesMoved, _filesDeleted, _filesPatched);
            });
        }));
    }

    private async void OnLoadConfig(object? sender, EventArgs e)
    {
        try
        {
            var path = _configPathField!.Text ?? "";
            _statusLabel!.Text = $"Loading {path}...";

            _currentConfig = await _configService.LoadAsync(path);

            _statusLabel.Text = $"Loaded: {path}";
            _executeButton!.Enabled = true;

            AppendLog($"Configuration loaded from: {path}");
            AppendLog($"  Packages: {_currentConfig.Packages.Count}");
            AppendLog($"  Assemblies: {_currentConfig.Assemblies.Count}");
            AppendLog($"  Code Patches: {_currentConfig.CodePatches.Count}");
            AppendLog("");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            _statusLabel!.Text = $"Error: {ex.Message}";
            _executeButton!.Enabled = false;
            MessageBox.ErrorQuery("Load Error", ex.Message, "OK");
        }
    }

    private async void OnExecute(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Execution Error", "No configuration loaded", "OK");
            return;
        }

        var isDryRun = _dryRunCheckBox!.CheckedState == CheckState.Checked;
        var shouldValidate = _validateCheckBox!.CheckedState == CheckState.Checked;

        if (!isDryRun)
        {
            var result = MessageBox.Query("Confirm Execution",
                "This will modify the client project files.\nAre you sure you want to proceed?",
                "Yes", "No");

            if (result != 0) return;
        }

        try
        {
            var configPath = _configPathField!.Text ?? "";
            _statusLabel!.Text = isDryRun ? "Running preparation (DRY-RUN)..." : "Running preparation...";

            await _preparationService.ExecuteAsync(_currentConfig, configPath, isDryRun, shouldValidate);

            _statusLabel.Text = isDryRun ? "Dry-run completed" : "Preparation completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preparation failed");
            _statusLabel!.Text = $"Error: {ex.Message}";
            MessageBox.ErrorQuery("Execution Error", ex.Message, "OK");

            _progressBar!.Visible = false;
            _executeButton!.Enabled = true;
            _stopButton!.Enabled = false;
            _isExecuting = false;
        }
    }

    private void OnStop(object? sender, EventArgs e)
    {
        if (!_isExecuting) return;

        var result = MessageBox.Query("Confirm Stop",
            "Are you sure you want to stop the preparation?\nThis may leave the project in an inconsistent state.",
            "Yes", "No");

        if (result == 0)
        {
            AppendLog("=== STOPPED BY USER ===");
            _statusLabel!.Text = "Stopped by user";
            _progressBar!.Visible = false;
            _executeButton!.Enabled = true;
            _stopButton!.Enabled = false;
            _isExecuting = false;
        }
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logTextView!.Text += $"[{timestamp}] {message}\n";

        // Auto-scroll to bottom
        _logTextView.MoveEnd();
    }

    private void UpdateStats(int copied, int moved, int deleted, int patched)
    {
        _statsLabel!.Text = $"Copied: {copied} | Moved: {moved} | Deleted: {deleted} | Patched: {patched}";
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
