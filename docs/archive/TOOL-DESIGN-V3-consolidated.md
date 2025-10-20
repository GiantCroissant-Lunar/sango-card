---
doc_id: DOC-2025-00198
title: TOOL DESIGN V3
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [tool-design-v3]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00148
title: TOOL DESIGN V3
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [tool-design-v3]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00136
title: TOOL DESIGN V3
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [tool-design-v3]
summary: >
  (Add summary here)
source:
  author: system
---
# Build Preparation Tool Design v3 - Final

## Technology Stack

### Core Framework

- **.NET 8.0** - Latest LTS
- **Microsoft.Extensions.Hosting** - Generic host for DI, logging, configuration
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Logging** - Logging infrastructure
- **Microsoft.Extensions.Configuration** - Configuration management

### Reactive & Messaging

- **System.Reactive (Rx.NET)** - Reactive extensions for event streams
- **ReactiveUI** - MVVM framework with reactive bindings
- **ObservableCollections (Cysharp)** - High-performance observable collections
- **MessagePipe (Cysharp)** - High-performance in-memory messaging

### UI & CLI

- **System.CommandLine** - CLI framework
- **Terminal.Gui v2** - Modern TUI framework (replaces Spectre.Console)

### Code Analysis

- **Microsoft.CodeAnalysis.CSharp** (Roslyn) - C# syntax manipulation
- **System.Text.Json** - JSON manipulation
- **YamlDotNet** - Base YAML parsing (extended for Unity)

## Architecture Overview

### Reactive Architecture with MessagePipe

```
┌─────────────────────────────────────────────────────────┐
│                    Application Host                      │
│  (Microsoft.Extensions.Hosting.IHost)                   │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌────────────┐      ┌──────────────┐                  │
│  │ CLI Mode   │      │  TUI Mode    │                  │
│  │ (System.   │      │ (Terminal.   │                  │
│  │ CommandLine│      │  Gui v2)     │                  │
│  └─────┬──────┘      └──────┬───────┘                  │
│        │                    │                           │
│        └────────┬───────────┘                           │
│                 │                                        │
│        ┌────────▼────────────┐                          │
│        │  Message Bus        │                          │
│        │  (MessagePipe)      │                          │
│        └────────┬────────────┘                          │
│                 │                                        │
│     ┌───────────┼───────────┐                           │
│     │           │           │                           │
│  ┌──▼───┐  ┌───▼────┐  ┌──▼────┐                      │
│  │Config│  │ Cache  │  │Prepare│                       │
│  │Service│  │Service │  │Service│                      │
│  └──┬───┘  └───┬────┘  └──┬────┘                      │
│     │          │           │                            │
│     └──────────┼───────────┘                            │
│                │                                         │
│        ┌───────▼────────┐                               │
│        │  Reactive      │                               │
│        │  State Store   │                               │
│        │  (ReactiveUI)  │                               │
│        └────────────────┘                               │
└─────────────────────────────────────────────────────────┘
```

### Message-Driven Communication

All components communicate via **MessagePipe** for loose coupling:

```csharp
// Messages
public record ConfigLoadedMessage(PreparationConfig Config);
public record CacheUpdatedMessage(CacheItem Item, CacheOperation Operation);
public record ValidationResultMessage(ValidationResult Result);
public record PreparationProgressMessage(string Stage, int Progress, int Total);
public record ErrorMessage(string Message, Exception Exception);

// Publishers
public class ConfigService
{
    private readonly IPublisher<ConfigLoadedMessage> _configPublisher;

    public async Task<PreparationConfig> LoadAsync(string path)
    {
        var config = await LoadFromFileAsync(path);
        await _configPublisher.PublishAsync(new ConfigLoadedMessage(config));
        return config;
    }
}

// Subscribers
public class TuiViewModel : ReactiveObject
{
    public TuiViewModel(ISubscriber<ConfigLoadedMessage> configSubscriber)
    {
        configSubscriber.Subscribe(msg =>
        {
            Config = msg.Config;
            this.RaisePropertyChanged(nameof(Config));
        });
    }
}
```

## Project Structure

```
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
├── SangoCard.Build.Tool.sln
└── SangoCard.Build.Tool/
    ├── SangoCard.Build.Tool.csproj
    │
    ├── Program.cs                      # Host builder, DI setup
    ├── HostBuilderExtensions.cs        # Service registration
    │
    ├── Cli/                            # CLI Mode
    │   ├── CliHost.cs                  # CLI entry point
    │   ├── Commands/
    │   │   ├── ConfigCommands.cs
    │   │   ├── CacheCommands.cs
    │   │   ├── PrepareCommands.cs
    │   │   └── RestoreCommands.cs
    │   └── Handlers/                   # Command handlers
    │       ├── ConfigCommandHandler.cs
    │       └── ...
    │
    ├── Tui/                            # TUI Mode (Terminal.Gui v2)
    │   ├── TuiHost.cs                  # TUI entry point
    │   ├── Views/                      # Terminal.Gui views
    │   │   ├── MainWindow.cs
    │   │   ├── CacheManagementView.cs
    │   │   ├── ConfigEditorView.cs
    │   │   └── ValidationView.cs
    │   ├── ViewModels/                 # ReactiveUI ViewModels
    │   │   ├── MainViewModel.cs
    │   │   ├── CacheViewModel.cs
    │   │   ├── ConfigViewModel.cs
    │   │   └── ValidationViewModel.cs
    │   └── Controls/                   # Custom TUI controls
    │       ├── FileSelector.cs
    │       ├── ListEditor.cs
    │       └── PropertyGrid.cs
    │
    ├── Core/                           # Core Business Logic
    │   ├── Models/
    │   │   ├── PreparationConfig.cs
    │   │   ├── AssetManipulation.cs
    │   │   ├── CodePatch.cs
    │   │   ├── ScriptingDefineSymbol.cs
    │   │   ├── CacheItem.cs
    │   │   └── ValidationResult.cs
    │   │
    │   ├── Services/
    │   │   ├── ConfigService.cs        # Config CRUD
    │   │   ├── CacheService.cs         # Cache management
    │   │   ├── PreparationService.cs   # Preparation execution
    │   │   ├── ValidationService.cs    # Multi-level validation
    │   │   ├── PathResolver.cs         # Git root-based paths
    │   │   └── ManifestService.cs      # manifest.json manipulation
    │   │
    │   ├── Patchers/                   # Code patching strategies
    │   │   ├── IPatcher.cs
    │   │   ├── CSharpPatcher.cs        # Roslyn-based
    │   │   ├── JsonPatcher.cs          # System.Text.Json
    │   │   ├── UnityAssetPatcher.cs    # Unity YAML
    │   │   └── TextPatcher.cs          # Regex/string
    │   │
    │   └── Utilities/
    │       ├── GitHelper.cs            # Git root detection
    │       ├── FileSystemHelper.cs
    │       └── UnityYamlParser.cs      # Custom Unity YAML
    │
    ├── Messages/                       # MessagePipe messages
    │   ├── ConfigMessages.cs
    │   ├── CacheMessages.cs
    │   ├── PreparationMessages.cs
    │   └── ValidationMessages.cs
    │
    ├── State/                          # Reactive state management
    │   ├── AppState.cs                 # Global app state
    │   ├── ConfigState.cs              # Config state
    │   └── CacheState.cs               # Cache state
    │
    └── publish/                        # Build output
        └── SangoCard.Build.Tool.exe
```

## Dependency Injection Setup

### Program.cs

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MessagePipe;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// MessagePipe
builder.Services.AddMessagePipe();

// Core Services
builder.Services.AddSingleton<PathResolver>();
builder.Services.AddSingleton<GitHelper>();
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<PreparationService>();
builder.Services.AddSingleton<ValidationService>();
builder.Services.AddSingleton<ManifestService>();

// Patchers
builder.Services.AddSingleton<IPatcher, CSharpPatcher>();
builder.Services.AddSingleton<IPatcher, JsonPatcher>();
builder.Services.AddSingleton<IPatcher, UnityAssetPatcher>();
builder.Services.AddSingleton<IPatcher, TextPatcher>();

// State
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<ConfigState>();
builder.Services.AddSingleton<CacheState>();

// Mode-specific
if (args.Contains("--mode") && args[Array.IndexOf(args, "--mode") + 1] == "tui")
{
    // TUI Mode
    builder.Services.AddSingleton<TuiHost>();
    builder.Services.AddTransient<MainViewModel>();
    builder.Services.AddTransient<CacheViewModel>();
    builder.Services.AddTransient<ConfigViewModel>();
}
else
{
    // CLI Mode (default)
    builder.Services.AddSingleton<CliHost>();
}

var host = builder.Build();

// Run appropriate mode
if (args.Contains("--mode") && args[Array.IndexOf(args, "--mode") + 1] == "tui")
{
    var tuiHost = host.Services.GetRequiredService<TuiHost>();
    await tuiHost.RunAsync();
}
else
{
    var cliHost = host.Services.GetRequiredService<CliHost>();
    await cliHost.RunAsync(args);
}
```

## Reactive State Management

### AppState (ReactiveUI)

```csharp
using ReactiveUI;
using System.Reactive.Linq;
using ObservableCollections;

public class AppState : ReactiveObject
{
    private string _gitRoot;
    private PreparationConfig _currentConfig;
    private bool _isLoading;

    public string GitRoot
    {
        get => _gitRoot;
        set => this.RaiseAndSetIfChanged(ref _gitRoot, value);
    }

    public PreparationConfig CurrentConfig
    {
        get => _currentConfig;
        set => this.RaiseAndSetIfChanged(ref _currentConfig, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    // Observable collections for TUI binding
    public ObservableList<CacheItem> CacheItems { get; } = new();
    public ObservableList<ValidationError> ValidationErrors { get; } = new();

    // Reactive commands
    public ReactiveCommand<string, PreparationConfig> LoadConfig { get; }
    public ReactiveCommand<Unit, Unit> ValidateConfig { get; }

    public AppState(
        ConfigService configService,
        ValidationService validationService,
        ISubscriber<ConfigLoadedMessage> configSubscriber,
        ISubscriber<CacheUpdatedMessage> cacheSubscriber)
    {
        // Subscribe to messages
        configSubscriber.Subscribe(msg => CurrentConfig = msg.Config);
        cacheSubscriber.Subscribe(msg => UpdateCacheItems(msg));

        // Setup reactive commands
        LoadConfig = ReactiveCommand.CreateFromTask<string, PreparationConfig>(
            async path => await configService.LoadAsync(path));

        ValidateConfig = ReactiveCommand.CreateFromTask(
            async () => await validationService.ValidateAllAsync(CurrentConfig));

        // React to config changes
        this.WhenAnyValue(x => x.CurrentConfig)
            .Where(c => c != null)
            .Subscribe(config => OnConfigChanged(config));
    }

    private void UpdateCacheItems(CacheUpdatedMessage msg)
    {
        switch (msg.Operation)
        {
            case CacheOperation.Add:
                CacheItems.Add(msg.Item);
                break;
            case CacheOperation.Remove:
                CacheItems.Remove(msg.Item);
                break;
        }
    }
}
```

## Terminal.Gui v2 TUI Implementation

### MainWindow

```csharp
using Terminal.Gui;
using ReactiveUI;
using System.Reactive.Disposables;

public class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CompositeDisposable _disposables = new();

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        Title = "Sango Card Build Preparation Tool";

        // Menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Open Config", "", () => OpenConfig()),
                new MenuItem("_Save Config", "", () => SaveConfig()),
                null,
                new MenuItem("_Exit", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Cache", new MenuItem[]
            {
                new MenuItem("_Populate", "", () => PopulateCache()),
                new MenuItem("_Add Package", "", () => AddPackage()),
                new MenuItem("_Clean", "", () => CleanCache())
            }),
            new MenuBarItem("_Prepare", new MenuItem[]
            {
                new MenuItem("_Run", "", () => RunPreparation()),
                new MenuItem("_Validate", "", () => ValidateConfig()),
                new MenuItem("_Dry Run", "", () => DryRun())
            })
        });

        Add(menu);

        // Main content area
        var contentFrame = new FrameView("Content")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        // Tab view
        var tabView = new TabView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        tabView.AddTab(new TabView.Tab("Cache", new CacheManagementView(_viewModel.CacheViewModel)), true);
        tabView.AddTab(new TabView.Tab("Config", new ConfigEditorView(_viewModel.ConfigViewModel)), false);
        tabView.AddTab(new TabView.Tab("Validation", new ValidationView(_viewModel.ValidationViewModel)), false);

        contentFrame.Add(tabView);
        Add(contentFrame);

        // Status bar
        var statusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.F1, "~F1~ Help", () => ShowHelp()),
            new StatusItem(Key.F5, "~F5~ Refresh", () => Refresh()),
            new StatusItem(Key.F10, "~F10~ Exit", () => Application.RequestStop())
        });

        Add(statusBar);

        // Bind to ViewModel
        BindViewModel();
    }

    private void BindViewModel()
    {
        // React to loading state
        _viewModel.WhenAnyValue(x => x.IsLoading)
            .Subscribe(isLoading =>
            {
                // Show/hide loading indicator
                Application.MainLoop.Invoke(() =>
                {
                    // Update UI on main thread
                });
            })
            .DisposeWith(_disposables);

        // React to validation errors
        _viewModel.ValidationErrors
            .ObserveAddChanged()
            .Subscribe(error =>
            {
                Application.MainLoop.Invoke(() =>
                {
                    MessageBox.ErrorQuery("Validation Error", error.Message, "OK");
                });
            })
            .DisposeWith(_disposables);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### CacheManagementView

```csharp
using Terminal.Gui;
using ObservableCollections;

public class CacheManagementView : View
{
    private readonly CacheViewModel _viewModel;
    private readonly ListView _packageList;
    private readonly ListView _assemblyList;

    public CacheManagementView(CacheViewModel viewModel)
    {
        _viewModel = viewModel;

        // Packages section
        var packagesFrame = new FrameView("Unity Packages")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(3)
        };

        _packageList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Bind to observable collection
        _viewModel.Packages.ObserveAddChanged()
            .Subscribe(pkg => Application.MainLoop.Invoke(() => UpdatePackageList()));

        packagesFrame.Add(_packageList);
        Add(packagesFrame);

        // Assemblies section
        var assembliesFrame = new FrameView("Assemblies")
        {
            X = Pos.Right(packagesFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };

        _assemblyList = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _viewModel.Assemblies.ObserveAddChanged()
            .Subscribe(asm => Application.MainLoop.Invoke(() => UpdateAssemblyList()));

        assembliesFrame.Add(_assemblyList);
        Add(assembliesFrame);

        // Buttons
        var btnAdd = new Button("Add")
        {
            X = 0,
            Y = Pos.Bottom(packagesFrame),
        };
        btnAdd.Clicked += () => _viewModel.AddPackageCommand.Execute().Subscribe();

        var btnRemove = new Button("Remove")
        {
            X = Pos.Right(btnAdd) + 1,
            Y = Pos.Bottom(packagesFrame),
        };
        btnRemove.Clicked += () => _viewModel.RemovePackageCommand.Execute().Subscribe();

        var btnPopulate = new Button("Populate from code-quality")
        {
            X = Pos.Right(btnRemove) + 1,
            Y = Pos.Bottom(packagesFrame),
        };
        btnPopulate.Clicked += () => _viewModel.PopulateCacheCommand.Execute().Subscribe();

        Add(btnAdd, btnRemove, btnPopulate);
    }

    private void UpdatePackageList()
    {
        _packageList.SetSource(_viewModel.Packages.Select(p => p.Name).ToList());
    }

    private void UpdateAssemblyList()
    {
        _assemblyList.SetSource(_viewModel.Assemblies.Select(a => a.Name).ToList());
    }
}
```

## CLI Mode Implementation

### CliHost

```csharp
using System.CommandLine;
using Microsoft.Extensions.Logging;

public class CliHost
{
    private readonly ILogger<CliHost> _logger;
    private readonly IServiceProvider _services;

    public CliHost(ILogger<CliHost> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public async Task<int> RunAsync(string[] args)
    {
        var rootCommand = new RootCommand("Sango Card Build Preparation Tool");

        // Config commands
        var configCommand = new Command("config", "Manage configuration");
        configCommand.AddCommand(CreateConfigCreateCommand());
        configCommand.AddCommand(CreateConfigAddPackageCommand());
        configCommand.AddCommand(CreateConfigAddDefineCommand());
        rootCommand.AddCommand(configCommand);

        // Cache commands
        var cacheCommand = new Command("cache", "Manage preparation cache");
        cacheCommand.AddCommand(CreateCachePopulateCommand());
        cacheCommand.AddCommand(CreateCacheAddCommand());
        rootCommand.AddCommand(cacheCommand);

        // Prepare commands
        var prepareCommand = new Command("prepare", "Execute preparation");
        prepareCommand.AddCommand(CreatePrepareRunCommand());
        prepareCommand.AddCommand(CreatePrepareValidateCommand());
        rootCommand.AddCommand(prepareCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private Command CreatePrepareRunCommand()
    {
        var command = new Command("run", "Run preparation");

        var configOption = new Option<string>(
            "--config",
            "Path to preparation config (relative to git root)");
        configOption.IsRequired = true;

        var clientOption = new Option<string>(
            "--client",
            "Path to client project (relative to git root)");
        clientOption.IsRequired = true;

        var buildTargetOption = new Option<string>(
            "--build-target",
            "Unity build target");

        var dryRunOption = new Option<bool>(
            "--dry-run",
            "Perform dry run without making changes");

        command.AddOption(configOption);
        command.AddOption(clientOption);
        command.AddOption(buildTargetOption);
        command.AddOption(dryRunOption);

        command.SetHandler(async (config, client, buildTarget, dryRun) =>
        {
            var handler = _services.GetRequiredService<PrepareCommandHandler>();
            await handler.RunAsync(config, client, buildTarget, dryRun);
        }, configOption, clientOption, buildTargetOption, dryRunOption);

        return command;
    }
}
```

## Package Dependencies

### SangoCard.Build.Tool.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  </PropertyGroup>

  <ItemGroup>
    <!-- Hosting & DI -->
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />

    <!-- Reactive & Messaging -->
    <PackageReference Include="System.Reactive" Version="6.0.1" />
    <PackageReference Include="ReactiveUI" Version="20.2.45" />
    <PackageReference Include="ObservableCollections" Version="3.3.4" />
    <PackageReference Include="MessagePipe" Version="1.8.1" />

    <!-- CLI & TUI -->
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
    <PackageReference Include="Terminal.Gui" Version="2.0.0" />

    <!-- Code Analysis -->
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="YamlDotNet" Version="15.1.0" />
  </ItemGroup>
</Project>
```

## Next Steps

1. **Create project structure**
2. **Implement PathResolver with git root detection**
3. **Setup DI and MessagePipe**
4. **Implement core services (Config, Cache, Preparation)**
5. **Implement patchers (C#, JSON, Unity)**
6. **Implement CLI commands**
7. **Implement TUI with Terminal.Gui v2**
8. **Test and integrate with Nuke**

**Ready to start implementation?**
