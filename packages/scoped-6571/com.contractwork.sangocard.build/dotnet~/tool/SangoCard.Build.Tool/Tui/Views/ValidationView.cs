using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Messages;
using Terminal.Gui;

namespace SangoCard.Build.Tool.Tui.Views;

/// <summary>
/// Validation view - Terminal.Gui v2 implementation.
/// Validates preparation configurations at different levels.
/// </summary>
public class ValidationView : View
{
    private readonly ConfigService _configService;
    private readonly ValidationService _validationService;
    private readonly ILogger<ValidationView> _logger;
    private readonly ISubscriber<ValidationStartedMessage> _validationStarted;
    private readonly ISubscriber<ValidationCompletedMessage> _validationCompleted;
    private readonly ISubscriber<ValidationErrorFoundMessage> _validationError;
    private readonly ISubscriber<ValidationWarningFoundMessage> _validationWarning;
    private readonly List<IDisposable> _subscriptions = new();

    private PreparationConfig? _currentConfig;
    private ValidationResult? _lastResult;

    // UI Controls
    private TextField? _configPathField;
    private RadioGroup? _validationLevelGroup;
    private Label? _statusLabel;
    private Button? _loadButton;
    private Button? _validateButton;
    private ListView? _errorsListView;
    private ListView? _warningsListView;
    private TextView? _summaryTextView;
    private ProgressBar? _progressBar;

    public ValidationView(
        ConfigService configService,
        ValidationService validationService,
        ILogger<ValidationView> logger,
        ISubscriber<ValidationStartedMessage> validationStarted,
        ISubscriber<ValidationCompletedMessage> validationCompleted,
        ISubscriber<ValidationErrorFoundMessage> validationError,
        ISubscriber<ValidationWarningFoundMessage> validationWarning)
    {
        _configService = configService;
        _validationService = validationService;
        _logger = logger;
        _validationStarted = validationStarted;
        _validationCompleted = validationCompleted;
        _validationError = validationError;
        _validationWarning = validationWarning;

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
            Text = "Configuration Validation",
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

        // Validation level section
        var levelLabel = new Label()
        {
            Text = "Validation Level:",
            X = 1,
            Y = 4
        };

        _validationLevelGroup = new RadioGroup()
        {
            X = Pos.Right(levelLabel) + 1,
            Y = 4,
            RadioLabels = new[] { "Schema", "File Existence", "Unity Packages", "Full" },
            SelectedItem = 3 // Default to Full
        };

        _validateButton = new Button()
        {
            Text = "Validate",
            X = Pos.Right(_validationLevelGroup) + 2,
            Y = 4,
            Enabled = false
        };
        _validateButton.Accepting += OnValidate;

        // Progress bar
        _progressBar = new ProgressBar()
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill()! - 1,
            Visible = false
        };

        // Status label
        _statusLabel = new Label()
        {
            Text = "Load a configuration to begin validation",
            X = 1,
            Y = 7
        };

        // Summary section
        var summaryFrame = new FrameView()
        {
            Title = "Validation Summary",
            X = 1,
            Y = 9,
            Width = Dim.Fill()! - 1,
            Height = 8
        };

        _summaryTextView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = "No validation performed yet."
        };

        summaryFrame.Add(_summaryTextView);

        // Errors section
        var errorsFrame = new FrameView()
        {
            Title = "Errors (0)",
            X = 1,
            Y = Pos.Bottom(summaryFrame) + 1,
            Width = Dim.Percent(50)! - 1,
            Height = Dim.Percent(50)!
        };

        _errorsListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        errorsFrame.Add(_errorsListView);

        // Warnings section
        var warningsFrame = new FrameView()
        {
            Title = "Warnings (0)",
            X = Pos.Right(errorsFrame) + 1,
            Y = Pos.Bottom(summaryFrame) + 1,
            Width = Dim.Fill()! - 1,
            Height = Dim.Percent(50)!
        };

        _warningsListView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        warningsFrame.Add(_warningsListView);

        Add(titleLabel, pathLabel, _configPathField, _loadButton, levelLabel, _validationLevelGroup,
            _validateButton, _progressBar, _statusLabel, summaryFrame, errorsFrame, warningsFrame);
    }

    private void SubscribeToMessages()
    {
        _subscriptions.Add(_validationStarted.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _statusLabel!.Text = $"Validating at level: {msg.Level}...";
                _progressBar!.Visible = true;
                _progressBar.Fraction = 0.3f;
            });
        }));

        _subscriptions.Add(_validationCompleted.Subscribe(msg =>
        {
            Application.Invoke(() =>
            {
                _lastResult = msg.Result;
                UpdateValidationResults();
                _progressBar!.Visible = false;

                var status = msg.Result.IsValid ? "✓ PASSED" : "✗ FAILED";
                _statusLabel!.Text = $"Validation {status} - {msg.Result.Errors.Count} errors, {msg.Result.Warnings.Count} warnings";
            });
        }));

        _subscriptions.Add(_validationError.Subscribe(msg =>
        {
            _logger.LogWarning("Validation error: {Message}", msg.Error.Message);
        }));

        _subscriptions.Add(_validationWarning.Subscribe(msg =>
        {
            _logger.LogInformation("Validation warning: {Message}", msg.Warning.Message);
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
            _validateButton!.Enabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config");
            _statusLabel!.Text = $"Error: {ex.Message}";
            _validateButton!.Enabled = false;
            MessageBox.ErrorQuery("Load Error", ex.Message, "OK");
        }
    }

    private void OnValidate(object? sender, EventArgs e)
    {
        if (_currentConfig == null)
        {
            MessageBox.ErrorQuery("Validation Error", "No configuration loaded", "OK");
            return;
        }

        try
        {
            var selectedLevel = _validationLevelGroup!.SelectedItem;
            var level = selectedLevel switch
            {
                0 => ValidationLevel.Schema,
                1 => ValidationLevel.FileExistence,
                2 => ValidationLevel.UnityPackages,
                3 => ValidationLevel.Full,
                _ => ValidationLevel.Full
            };

            _statusLabel!.Text = $"Validating at level: {level}...";
            _progressBar!.Visible = true;
            _progressBar.Fraction = 0.5f;
            _validateButton!.Enabled = false;

            // Run validation synchronously (it's fast enough)
            _lastResult = _validationService.Validate(_currentConfig, level);

            UpdateValidationResults();
            _progressBar.Visible = false;
            _validateButton.Enabled = true;

            var status = _lastResult.IsValid ? "✓ PASSED" : "✗ FAILED";
            _statusLabel.Text = $"Validation {status} - {_lastResult.Errors.Count} errors, {_lastResult.Warnings.Count} warnings";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            _statusLabel!.Text = $"Error: {ex.Message}";
            _progressBar!.Visible = false;
            _validateButton!.Enabled = true;
            MessageBox.ErrorQuery("Validation Error", ex.Message, "OK");
        }
    }

    private void UpdateValidationResults()
    {
        if (_lastResult == null) return;

        // Update summary
        var summary = $@"Validation Level: {_lastResult.Level}
Status: {(_lastResult.IsValid ? "PASSED ✓" : "FAILED ✗")}
Errors: {_lastResult.Errors.Count}
Warnings: {_lastResult.Warnings.Count}

{_lastResult.Summary}
";
        _summaryTextView!.Text = summary;

        // Update errors list
        var errorItems = _lastResult.Errors
            .Select(e => $"[{e.Code}] {e.File ?? "General"}: {e.Message}")
            .ToList();
        _errorsListView!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(errorItems));

        // Update warnings list
        var warningItems = _lastResult.Warnings
            .Select(w => $"[{w.Code}] {w.File ?? "General"}: {w.Message}")
            .ToList();
        _warningsListView!.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(warningItems));

        // Update frame titles
        ((FrameView)_errorsListView.SuperView!).Title = $"Errors ({_lastResult.Errors.Count})";
        ((FrameView)_warningsListView.SuperView!).Title = $"Warnings ({_lastResult.Warnings.Count})";
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
