---
doc_id: DOC-2025-00042
title: Build Preparation Tool Specification
doc_type: spec
status: active
canonical: true
supersedes: []
created: 2025-10-16
updated: 2025-10-17
tags: [build, unity, tooling, roslyn, cli, tui]
summary: >
  Automated build preparation tool for Unity projects with reactive architecture,
  git root-based path resolution, and Roslyn-based code patching capabilities.
source:
  author: human
related: [RFC-2025-00001]
---

# Build Preparation Tool Specification

**ID:** SPEC-BPT-001
**Status:** Active
**Created:** 2025-10-16
**Updated:** 2025-10-17
**Owner:** Build System Team
**Related RFC:** RFC-001

## Overview

A .NET CLI/TUI tool for managing Unity build preparation configurations with reactive architecture, git root-based path resolution, and robust code patching capabilities.

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

## User Stories

### US-1: As a developer, I want to interactively manage build preparation configs

**Priority:** High
**Story Points:** 8

**Acceptance Criteria:**

- [ ] Launch TUI with `tool tui`
- [ ] Navigate between Cache, Config, and Validation views
- [ ] Add/remove packages from cache
- [ ] Edit scripting defines
- [ ] Add/remove code patches
- [ ] Save changes to config
- [ ] Real-time validation feedback

**Tasks:**

- [ ] Implement Terminal.Gui v2 MainWindow
- [ ] Implement CacheManagementView
- [ ] Implement ConfigEditorView
- [ ] Implement ValidationView
- [ ] Bind ViewModels with ReactiveUI
- [ ] Implement keyboard shortcuts

### US-2: As a build system, I want to execute preparation via CLI

**Priority:** High
**Story Points:** 5

**Acceptance Criteria:**

- [ ] Run preparation with `tool prepare run --config <path> --client <path>`
- [ ] Exit code 0 on success, non-zero on failure
- [ ] Progress output to stdout
- [ ] Errors to stderr
- [ ] Support dry-run mode
- [ ] Complete in < 30 seconds for typical project

**Tasks:**

- [ ] Implement System.CommandLine commands
- [ ] Implement PrepareCommandHandler
- [ ] Implement progress reporting
- [ ] Implement error handling
- [ ] Add dry-run support

### US-3: As a developer, I want unambiguous path resolution

**Priority:** Critical
**Story Points:** 3

**Acceptance Criteria:**

- [ ] All paths relative to git root
- [ ] Auto-detect git root on startup
- [ ] Clear error if git root not found
- [ ] Display git root in verbose mode
- [ ] Cross-platform path handling

**Tasks:**

- [ ] Implement PathResolver
- [ ] Implement GitHelper
- [ ] Add git root detection algorithm
- [ ] Add path validation
- [ ] Add cross-platform tests

### US-4: As a developer, I want robust C# code patching

**Priority:** High
**Story Points:** 8

**Acceptance Criteria:**

- [ ] Remove using statements without breaking code
- [ ] Replace expressions syntax-aware
- [ ] Preserve formatting and comments
- [ ] Validate patch before applying
- [ ] Rollback on error

**Tasks:**

- [ ] Implement CSharpPatcher with Roslyn
- [ ] Implement RemoveUsing operation
- [ ] Implement ReplaceExpression operation
- [ ] Implement validation logic
- [ ] Add rollback mechanism
- [ ] Add unit tests

### US-5: As a developer, I want to populate cache from code-quality

**Priority:** Medium
**Story Points:** 5

**Acceptance Criteria:**

- [ ] Run `tool cache populate --source projects/code-quality`
- [ ] Auto-detect Microsoft.Extensions packages
- [ ] Copy packages to cache
- [ ] Update config automatically
- [ ] Show progress
- [ ] Skip already cached items

**Tasks:**

- [ ] Implement CacheService.PopulateAsync
- [ ] Implement package detection
- [ ] Implement copy logic
- [ ] Implement config update
- [ ] Add progress reporting

### US-6: As a developer, I want multi-level validation

**Priority:** High
**Story Points:** 5

**Acceptance Criteria:**

- [ ] Validate JSON schema
- [ ] Validate file existence
- [ ] Validate Unity package validity
- [ ] Validate code patch applicability
- [ ] Clear error messages with file paths
- [ ] Summary with error count

**Tasks:**

- [ ] Implement ValidationService
- [ ] Implement schema validation
- [ ] Implement file existence checks
- [ ] Implement Unity package validation
- [ ] Implement code patch validation
- [ ] Format validation results

## Technical Requirements

### TR-1: Git Root Path Resolution

**Priority:** Critical

**Requirements:**

- MUST auto-detect git root by walking up directory tree
- MUST validate git root exists before any operation
- MUST resolve all paths relative to git root
- MUST fail with clear error if git root not found
- MUST support cross-platform paths

**Validation:**

```csharp
[Fact]
public void PathResolver_DetectsGitRoot()
{
    var resolver = new PathResolver(configPath);
    Assert.NotNull(resolver.GitRoot);
    Assert.True(Directory.Exists(Path.Combine(resolver.GitRoot, ".git")));
}

[Fact]
public void PathResolver_ResolvesRelativePath()
{
    var resolver = new PathResolver(configPath);
    var resolved = resolver.Resolve("build/preparation/configs/test.json");
    Assert.StartsWith(resolver.GitRoot, resolved);
}
```

### TR-2: Reactive State Management

**Priority:** High

**Requirements:**

- MUST use ReactiveUI for state management
- MUST use ObservableCollections for UI binding
- MUST use MessagePipe for component communication
- MUST update UI reactively on state changes
- MUST handle concurrent updates safely

**Validation:**

```csharp
[Fact]
public async Task AppState_ReactsToConfigLoaded()
{
    var state = new AppState(services);
    var configLoaded = false;

    state.WhenAnyValue(x => x.CurrentConfig)
        .Where(c => c != null)
        .Subscribe(_ => configLoaded = true);

    await publisher.PublishAsync(new ConfigLoadedMessage(config));

    Assert.True(configLoaded);
    Assert.Equal(config, state.CurrentConfig);
}
```

### TR-3: Code Patching with Roslyn

**Priority:** High

**Requirements:**

- MUST use Roslyn for C# syntax manipulation
- MUST preserve formatting and trivia
- MUST validate syntax after patching
- MUST support: remove_using, replace, replace_block, remove_block
- MUST rollback on error

**Validation:**

```csharp
[Fact]
public void CSharpPatcher_RemovesUsing()
{
    var code = @"
        using System;
        using Unity.VisualScripting;

        public class Test { }
    ";

    var patcher = new CSharpPatcher();
    patcher.RemoveUsing(filePath, "Unity.VisualScripting");

    var result = File.ReadAllText(filePath);
    Assert.DoesNotContain("using Unity.VisualScripting;", result);
    Assert.Contains("using System;", result);
}
```

### TR-4: Terminal.Gui v2 TUI

**Priority:** High

**Requirements:**

- MUST use Terminal.Gui v2 for TUI
- MUST support keyboard navigation
- MUST support mouse input
- MUST update UI reactively
- MUST handle window resize

**Validation:**

- Manual testing with TUI
- Screenshot comparison tests
- Keyboard navigation tests

### TR-5: CLI with System.CommandLine

**Priority:** High

**Requirements:**

- MUST use System.CommandLine for CLI
- MUST support all commands in RFC
- MUST return appropriate exit codes
- MUST output progress to stdout
- MUST output errors to stderr

**Validation:**

```csharp
[Fact]
public async Task Cli_PrepareRun_ReturnsZeroOnSuccess()
{
    var args = new[] { "prepare", "run", "--config", configPath, "--client", clientPath };
    var exitCode = await cliHost.RunAsync(args);
    Assert.Equal(0, exitCode);
}
```

### TR-6: Bidirectional Cache-Config Sync

**Priority:** Medium

**Requirements:**

- MUST update config when cache modified
- MUST validate cache when config modified
- MUST maintain consistency
- MUST handle concurrent modifications

**Validation:**

```csharp
[Fact]
public async Task CacheService_AddPackage_UpdatesConfig()
{
    var initialCount = config.GetPackageCount();

    await cacheService.AddPackageAsync(packagePath);

    var updatedConfig = await configService.LoadAsync(configPath);
    Assert.Equal(initialCount + 1, updatedConfig.GetPackageCount());
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
    │   └── Commands/
    │       ├── ConfigCommandHandler.cs
    │       ├── CacheCommandHandler.cs
    │       └── PrepareCommandHandler.cs
    │
    ├── Core/                           # Core Business Logic
    │   ├── Models/
    │   │   ├── PreparationConfig.cs
    │   │   ├── AssetManipulation.cs
    │   │   ├── CodePatch.cs
    │   │   ├── ScriptingDefineSymbols.cs
    │   │   ├── CacheItem.cs
    │   │   └── ValidationResult.cs
    │   │
    │   ├── Services/
    │   │   ├── ConfigService.cs        # Config CRUD
    │   │   ├── CacheService.cs         # Cache management
    │   │   ├── PreparationService.cs   # Preparation execution
    │   │   └── ValidationService.cs    # Multi-level validation
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
    │       ├── PathResolver.cs         # Git root-based paths
    │       └── UnityYamlParser.cs      # Custom Unity YAML
    │
    ├── Messages/                       # MessagePipe messages
    │   ├── ConfigMessages.cs
    │   ├── CacheMessages.cs
    │   ├── PreparationMessages.cs
    │   └── ValidationMessages.cs
    │
    └── publish/                        # Build output
        └── SangoCard.Build.Tool.exe
```

## Dependencies

### External Dependencies

- .NET 8.0 SDK
- Git (for git root detection)
- Unity 6000.2.x (for testing)

### NuGet Packages

- Microsoft.Extensions.Hosting ^8.0.0
- Microsoft.Extensions.DependencyInjection ^8.0.0
- Microsoft.Extensions.Logging ^8.0.0
- System.Reactive ^6.0.1
- ReactiveUI ^20.2.45
- ObservableCollections ^3.3.4
- MessagePipe ^1.8.1
- System.CommandLine ^2.0.0-beta4
- Terminal.Gui ^2.0.0
- Microsoft.CodeAnalysis.CSharp ^4.8.0
- System.Text.Json ^8.0.0
- YamlDotNet ^15.1.0

## Testing Strategy

### Unit Tests (Target: 80% coverage)

- PathResolver git root detection
- Each patcher strategy
- Validation logic
- Config serialization
- Message handling

### Integration Tests

- CLI command execution
- Cache population
- Preparation execution
- Config-cache sync

### End-to-End Tests

- Full preparation → build → restore cycle
- TUI workflow (manual)
- Error handling

### Performance Tests

- Preparation completes in < 30 seconds
- TUI responsive (< 100ms updates)
- Large config handling (1000+ items)

## Documentation

### User Documentation

- [ ] Installation guide
- [ ] Quick start guide
- [ ] CLI command reference
- [ ] TUI user guide
- [ ] Configuration format reference
- [ ] Troubleshooting guide

### Developer Documentation

- [ ] Architecture overview
- [ ] Adding new patcher types
- [ ] Adding new CLI commands
- [ ] Testing guide
- [ ] Contributing guide

## Rollout Plan

### Phase 1: Alpha (Week 1-3)

- Core infrastructure
- Services implementation
- Code patchers
- Internal testing

### Phase 2: Beta (Week 4-5)

- CLI mode complete
- TUI mode complete
- External testing with dev team
- Bug fixes

### Phase 3: RC (Week 6)

- Nuke integration
- Full documentation
- Performance optimization
- Security review

### Phase 4: GA (Week 7)

- Production release
- Training sessions
- Migration from old system
- Monitoring and support

## Success Metrics

### Adoption Metrics

- 100% of builds use tool within 1 month
- 0 manual config edits after 2 weeks
- 90% developer satisfaction

### Performance Metrics

- Preparation: < 30 seconds (p95)
- TUI startup: < 2 seconds
- Validation: < 5 seconds

### Quality Metrics

- 99% preparation success rate
- < 5 bugs per month after GA
- 80% code coverage

### Usability Metrics

- New config creation: < 5 minutes
- Config modification: < 2 minutes
- Learning curve: < 1 hour

## Risk Assessment

### High Risk

- **Unity YAML parsing complexity**
  - Mitigation: Start with simple cases, expand gradually
  - Fallback: Use text-based patching for complex cases

- **Terminal.Gui v2 stability**
  - Mitigation: Extensive testing, report issues upstream
  - Fallback: Use Spectre.Console for CLI-only version

### Medium Risk

- **Roslyn performance**
  - Mitigation: Cache parsed trees, use async operations
  - Fallback: Limit to small files, warn on large files

- **Cross-platform compatibility**
  - Mitigation: Test on Windows, Linux, macOS
  - Fallback: Document platform-specific issues

### Low Risk

- **MessagePipe learning curve**
  - Mitigation: Good documentation, examples
  - Fallback: Direct method calls if needed

## Open Issues

### Issue #1: Unity YAML Format Variations

**Status:** Open
**Priority:** High
**Description:** Unity YAML format varies between versions
**Resolution:** Start with Unity 6000.2.x, add version detection later

### Issue #2: Concurrent Config Modifications

**Status:** Open
**Priority:** Medium
**Description:** How to handle multiple users editing same config?
**Resolution:** File locking + timestamp checking, warn on conflicts

### Issue #3: Large Config Performance

**Status:** Open
**Priority:** Low
**Description:** Performance with 1000+ items in config
**Resolution:** Lazy loading, pagination in TUI

## Approval

**Spec Author:** Build System Team
**Reviewed By:** [Pending]
**Approved By:** [Pending]
**Date:** [Pending]

## Changelog

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-17 | 0.2.0 | Consolidated from TOOL-DESIGN-V3 and original spec | Build System Team |
| 2025-10-17 | 0.1.0 | Initial draft | Build System Team |

## See Also

- **RFC**: docs/rfcs/RFC-001-build-preparation-tool.md
- **Implementation**: packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
- **Archived Versions**:
  - docs/archive/TOOL-DESIGN.md
  - docs/archive/TOOL-DESIGN-V2.md
  - docs/archive/TOOL-DESIGN-V3.md
