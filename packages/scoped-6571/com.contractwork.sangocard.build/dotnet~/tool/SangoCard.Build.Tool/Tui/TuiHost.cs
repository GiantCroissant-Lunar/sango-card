using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Messages;

namespace SangoCard.Build.Tool.Tui;

/// <summary>
/// Minimal TUI host that subscribes to MessagePipe events and streams them to the console.
/// </summary>
public class TuiHost
{
    private readonly ILogger<TuiHost> _logger;

    // Validation
    private readonly ISubscriber<ValidationStartedMessage> _validationStarted;
    private readonly ISubscriber<ValidationCompletedMessage> _validationCompleted;
    private readonly ISubscriber<ValidationErrorFoundMessage> _validationError;
    private readonly ISubscriber<ValidationWarningFoundMessage> _validationWarning;

    // Preparation
    private readonly ISubscriber<PreparationStartedMessage> _prepStarted;
    private readonly ISubscriber<PreparationCompletedMessage> _prepCompleted;
    private readonly ISubscriber<FileCopiedMessage> _fileCopied;
    private readonly ISubscriber<FileMovedMessage> _fileMoved;
    private readonly ISubscriber<FileDeletedMessage> _fileDeleted;

    // Config
    private readonly ISubscriber<ConfigLoadedMessage> _configLoaded;
    private readonly ISubscriber<ConfigSavedMessage> _configSaved;
    private readonly ISubscriber<ConfigModifiedMessage> _configModified;

    // Cache
    private readonly ISubscriber<CachePopulatedMessage> _cachePopulated;
    private readonly ISubscriber<CacheItemAddedMessage> _cacheItemAdded;
    private readonly ISubscriber<CacheCleanedMessage> _cacheCleaned;

    public TuiHost(
        ILogger<TuiHost> logger,
        // Validation
        ISubscriber<ValidationStartedMessage> validationStarted,
        ISubscriber<ValidationCompletedMessage> validationCompleted,
        ISubscriber<ValidationErrorFoundMessage> validationError,
        ISubscriber<ValidationWarningFoundMessage> validationWarning,
        // Preparation
        ISubscriber<PreparationStartedMessage> prepStarted,
        ISubscriber<PreparationCompletedMessage> prepCompleted,
        ISubscriber<FileCopiedMessage> fileCopied,
        ISubscriber<FileMovedMessage> fileMoved,
        ISubscriber<FileDeletedMessage> fileDeleted,
        // Config
        ISubscriber<ConfigLoadedMessage> configLoaded,
        ISubscriber<ConfigSavedMessage> configSaved,
        ISubscriber<ConfigModifiedMessage> configModified,
        // Cache
        ISubscriber<CachePopulatedMessage> cachePopulated,
        ISubscriber<CacheItemAddedMessage> cacheItemAdded,
        ISubscriber<CacheCleanedMessage> cacheCleaned)
    {
        _logger = logger;
        _validationStarted = validationStarted;
        _validationCompleted = validationCompleted;
        _validationError = validationError;
        _validationWarning = validationWarning;
        _prepStarted = prepStarted;
        _prepCompleted = prepCompleted;
        _fileCopied = fileCopied;
        _fileMoved = fileMoved;
        _fileDeleted = fileDeleted;
        _configLoaded = configLoaded;
        _configSaved = configSaved;
        _configModified = configModified;
        _cachePopulated = cachePopulated;
        _cacheItemAdded = cacheItemAdded;
        _cacheCleaned = cacheCleaned;
    }

    public Task<int> StartAsync(CancellationToken cancellationToken = default)
    {
        Console.Clear();
        Console.WriteLine("Sango Card Build Preparation Tool - TUI Mode");
        Console.WriteLine("Streaming events. Press 'Q' to quit.\n");

        var disposables = new List<IDisposable>
        {
            // Validation
            _validationStarted.Subscribe(m => Console.WriteLine($"[validation] started: {m.Level}")),
            _validationCompleted.Subscribe(m => Console.WriteLine($"[validation] completed: valid={m.Result.IsValid}, errors={m.Result.Errors.Count}, warnings={m.Result.Warnings.Count}")),
            _validationError.Subscribe(m => Console.WriteLine($"[validation] error {m.Error.Code}: {m.Error.Message}")),
            _validationWarning.Subscribe(m => Console.WriteLine($"[validation] warning {m.Warning.Code}: {m.Warning.Message}")),

            // Preparation
            _prepStarted.Subscribe(m => Console.WriteLine($"[prepare] started (config: {m.ConfigPath ?? "(none)"})")),
            _prepCompleted.Subscribe(m => Console.WriteLine($"[prepare] completed: copied={m.Copied}, moved={m.Moved}, deleted={m.Deleted}, patched={m.Patched}")),
            _fileCopied.Subscribe(m => Console.WriteLine($"[prepare] copy: {m.Source} -> {m.Target}")),
            _fileMoved.Subscribe(m => Console.WriteLine($"[prepare] move: {m.Source} -> {m.Target}")),
            _fileDeleted.Subscribe(m => Console.WriteLine($"[prepare] delete: {m.Path}")),

            // Config
            _configLoaded.Subscribe(_ => Console.WriteLine("[config] loaded")),
            _configSaved.Subscribe(_ => Console.WriteLine("[config] saved")),
            _configModified.Subscribe(_ => Console.WriteLine("[config] modified")),

            // Cache
            _cachePopulated.Subscribe(m => Console.WriteLine($"[cache] populated: {m.ItemCount} items from {m.SourcePath}")),
            _cacheItemAdded.Subscribe(m => Console.WriteLine($"[cache] item added: {m.Item.Name} ({m.Item.Type})")),
            _cacheCleaned.Subscribe(m => Console.WriteLine($"[cache] cleaned: {m.RemovedCount} item(s)"))
        };

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }
                Thread.Sleep(50);
            }
        }
        finally
        {
            foreach (var d in disposables)
            {
                d.Dispose();
            }
        }

        return Task.FromResult(0);
    }
}
