---
doc_id: DOC-2025-00191
title: RFC 001 Build Preparation Tool
doc_type: rfc
status: active
canonical: false
created: 2025-10-17
tags: [rfc-001-build-preparation-tool]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00141
title: RFC 001 Build Preparation Tool
doc_type: rfc
status: active
canonical: false
created: 2025-10-17
tags: [rfc-001-build-preparation-tool]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: RFC-2025-00001
title: Build Preparation Tool
doc_type: rfc
status: active
canonical: true
created: '2025-10-17'
tags:

- build
- unity
- tooling
- rfc
summary: >
  RFC for a .NET CLI/TUI tool for managing Unity build preparation configurations
  with reactive architecture and git root-based path resolution.
supersedes: []
related:
- DOC-2025-00042

---

# RFC-001: Build Preparation Tool

**Status:** Draft
**Created:** 2025-10-17
**Author:** Build System Team
**Related:** R-BLD-010, R-PATH-010

## Summary

Create a .NET CLI/TUI tool for managing Unity build preparation configurations. This tool will handle cache management, configuration editing, code patching, and preparation execution with a reactive architecture and git root-based path resolution.

## Motivation

### Problem Statement

The Unity build process for Sango Card requires complex preparation steps:

1. Copying Unity packages to client project
2. Copying plain DLLs to specific locations
3. Modifying manifest.json
4. Applying code patches
5. Managing scripting define symbols
6. Removing conflicting assets

Currently, these operations are:

- Manually configured in JSON files
- Error-prone due to path ambiguity
- Difficult to validate
- Hard to maintain and update
- No interactive management interface

### Goals

1. **Unambiguous Path Resolution:** All paths relative to git root
2. **Interactive Management:** TUI for human configuration
3. **Automation-Friendly:** CLI for build system integration
4. **Type-Safe:** Strong typing with C# models
5. **Reactive:** Real-time state updates across UI
6. **Robust Code Patching:** Syntax-aware patching for C#, JSON, Unity assets
7. **Bidirectional Sync:** Cache ↔ Config automatic updates
8. **Full Validation:** Multi-level validation with clear errors

### Non-Goals

1. Unity Editor integration (separate concern)
2. Remote asset downloading (future RFC)
3. Multi-repository support (single repo only)
4. GUI application (TUI is sufficient)

## Detailed Design

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Generic Host (MS Extensions)                │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────┐                    ┌──────────┐          │
│  │ CLI Mode │◄───────────────────►│ TUI Mode │          │
│  └────┬─────┘                    └────┬─────┘          │
│       │                               │                 │
│       └───────────┬───────────────────┘                 │
│                   │                                      │
│          ┌────────▼────────┐                            │
│          │   Message Bus   │                            │
│          │  (MessagePipe)  │                            │
│          └────────┬────────┘                            │
│                   │                                      │
│       ┌───────────┼───────────┐                         │
│       │           │           │                         │
│  ┌────▼───┐  ┌───▼────┐  ┌──▼────┐                    │
│  │Config  │  │ Cache  │  │Prepare│                     │
│  │Service │  │Service │  │Service│                     │
│  └────┬───┘  └───┬────┘  └──┬────┘                    │
│       │          │           │                          │
│       └──────────┼───────────┘                          │
│                  │                                       │
│         ┌────────▼────────┐                             │
│         │ Reactive State  │                             │
│         │   (ReactiveUI)  │                             │
│         └─────────────────┘                             │
└─────────────────────────────────────────────────────────┘
```

### Technology Stack

**Core:**

- .NET 8.0 (LTS)
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

**Reactive & Messaging:**

- System.Reactive (Rx.NET)
- ReactiveUI
- ObservableCollections (Cysharp)
- MessagePipe (Cysharp)

**UI & CLI:**

- System.CommandLine
- Terminal.Gui v2

**Code Analysis:**

- Microsoft.CodeAnalysis.CSharp (Roslyn)
- System.Text.Json
- YamlDotNet (extended for Unity)

### Path Resolution Strategy

**Rule:** All paths are relative to **git repository root**.

**Git Root Detection:**

```csharp
public class PathResolver
{
    private readonly string _gitRoot;

    public PathResolver(string configPath)
    {
        _gitRoot = DetectGitRoot(configPath);
        if (_gitRoot == null)
            throw new InvalidOperationException(
                "Git root not found. Must be inside repository.");
    }

    private string DetectGitRoot(string startPath)
    {
        var current = Path.GetDirectoryName(Path.GetFullPath(startPath));
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    public string Resolve(string relativePath) =>
        Path.GetFullPath(Path.Combine(_gitRoot, relativePath));
}
```

**Benefits:**

- Unambiguous base path
- All agents agree on same base
- Portable within repository
- Clear error messages

### Code Patching Strategies

#### 1. C# Files (Roslyn)

```csharp
public class CSharpPatcher : IPatcher
{
    public void RemoveUsing(string filePath, string usingNamespace)
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath));
        var root = tree.GetRoot();

        var usings = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name.ToString() == usingNamespace);

        root = root.RemoveNodes(usings, SyntaxRemoveOptions.KeepNoTrivia);
        File.WriteAllText(filePath, root.ToFullString());
    }
}
```

#### 2. JSON Files (System.Text.Json)

```csharp
public class JsonPatcher : IPatcher
{
    public void SetValue(string filePath, string jsonPath, object value)
    {
        var json = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(json);
        // Use JSON path to navigate and modify
        // Serialize back
    }
}
```

#### 3. Unity Asset Files (Custom YAML)

```csharp
public class UnityAssetPatcher : IPatcher
{
    public void ModifyScriptingDefines(
        string projectSettingsPath,
        BuildTargetGroup targetGroup,
        List<string> add,
        List<string> remove)
    {
        var asset = UnityYamlParser.Parse(projectSettingsPath);
        var playerSettings = asset.FindObject("PlayerSettings");

        var defines = playerSettings
            .GetProperty("scriptingDefineSymbols")
            .GetValue((int)targetGroup)
            .Split(';')
            .ToList();

        defines.AddRange(add);
        defines.RemoveAll(d => remove.Contains(d));

        playerSettings
            .GetProperty("scriptingDefineSymbols")
            .SetValue((int)targetGroup, string.Join(";", defines.Distinct()));

        UnityYamlParser.Save(asset, projectSettingsPath);
    }
}
```

### Message-Driven Communication

All components communicate via MessagePipe:

```csharp
// Messages
public record ConfigLoadedMessage(PreparationConfig Config);
public record CacheUpdatedMessage(CacheItem Item, CacheOperation Operation);
public record ValidationResultMessage(ValidationResult Result);
public record PreparationProgressMessage(string Stage, int Progress, int Total);

// Publisher
public class ConfigService
{
    private readonly IPublisher<ConfigLoadedMessage> _publisher;

    public async Task<PreparationConfig> LoadAsync(string path)
    {
        var config = await LoadFromFileAsync(path);
        await _publisher.PublishAsync(new ConfigLoadedMessage(config));
        return config;
    }
}

// Subscriber
public class TuiViewModel : ReactiveObject
{
    public TuiViewModel(ISubscriber<ConfigLoadedMessage> subscriber)
    {
        subscriber.Subscribe(msg =>
        {
            Config = msg.Config;
            this.RaisePropertyChanged(nameof(Config));
        });
    }
}
```

### CLI Mode

**Commands:**

```bash
# Config management
tool config create --output "build/preparation/configs/build-preparation.json"
tool config add-package --config "..." --source "..." --destination "..."
tool config add-define --config "..." --name "LOCAL_BUILD" --operation add

# Cache management
tool cache populate --source "projects/code-quality" --destination "build/preparation"
tool cache add package --source "..." --destination "..."

# Preparation execution
tool prepare run --config "..." --client "..." --build-target "StandaloneWindows64"
tool prepare validate --config "..." --check-files

# Restore
tool restore run --client "..." --method git
```

### TUI Mode

**Terminal.Gui v2 Interface:**

```
╔═══════════════════════════════════════════════════════════╗
║  Sango Card Build Preparation Tool                       ║
╠═══════════════════════════════════════════════════════════╣
║  File  Cache  Prepare  Help                              ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  ┌─ Cache ────────────┐  ┌─ Config ──────────────────┐  ║
║  │ Unity Packages (4) │  │ Preparation: Base         │  ║
║  │ ✓ ms.ext.logging   │  │                           │  ║
║  │ ✓ ms.ext.logging.. │  │ Defines (3):              │  ║
║  │ ✓ ms.ext.depinj    │  │ ✓ LOCAL_BUILD (add)       │  ║
║  │ ✓ ms.ext.depinj... │  │ ✗ ODIN_INSPECTOR (remove) │  ║
║  │                    │  │                           │  ║
║  │ Assemblies (2)     │  │ Asset Manipulations (12): │  ║
║  │ ✓ System.Command.. │  │ ▸ Add MS.Ext packages     │  ║
║  │ ✓ Splat.dll        │  │ ▸ Add custom packages     │  ║
║  └────────────────────┘  │ ▼ Code Patches (3)        │  ║
║                          │   ✓ CreateFormation...    │  ║
║  [Add] [Remove] [Pop..]  │   ✓ RankMatchModel.cs     │  ║
║                          └───────────────────────────┘  ║
║                                                           ║
╠═══════════════════════════════════════════════════════════╣
║  F1 Help | F5 Refresh | F10 Exit                         ║
╚═══════════════════════════════════════════════════════════╝
```

### Validation Levels

**Level 1: Schema Validation**

- JSON structure valid
- Required fields present
- Types correct

**Level 2: File Existence**

- All source files exist
- All destination paths valid
- Cache items present

**Level 3: Unity Package Validity**

- package.json valid
- .asmdef files valid
- Dependencies resolvable

**Level 4: Code Patch Applicability**

- Target files exist
- Patterns found
- Syntax valid after patch

### Configuration Format

```json
{
  "version": "3.0",
  "pathResolution": {
    "strategy": "git-root",
    "gitRoot": null
  },
  "metadata": {
    "author": "Build System",
    "lastModified": "2025-10-17T09:27:00Z",
    "description": "Sango Card build preparation"
  },
  "preparations": [
    {
      "name": "Prepare for build - Sango Card",
      "platform": "any",
      "buildTarget": "all",
      "mode": "legacy",
      "scriptingDefineSymbols": [
        {
          "name": "LOCAL_BUILD",
          "status": { "presence": "present", "operation": "add" },
          "enable": true
        }
      ],
      "codePatches": [
        {
          "targetFile": {
            "path": "projects/client/Assets/Scripts/Example.cs"
          },
          "patches": [
            {
              "type": "remove_using",
              "pattern": "using Unity.VisualScripting;"
            }
          ],
          "enable": true
        }
      ],
      "assetManipulations": [
        {
          "category": "Add Microsoft.Extensions packages",
          "assets": [
            {
              "source": {
                "path": "build/preparation/unity-packages/org.nuget.microsoft.extensions.logging@xxx"
              },
              "destination": {
                "path": "projects/client/Packages/org.nuget.microsoft.extensions.logging@xxx"
              },
              "status": { "presence": "present", "operation": "copy" },
              "enable": true
            }
          ]
        }
      ]
    }
  ]
}
```

## Implementation Plan

### Phase 1: Core Infrastructure (Week 1)

- [ ] Create .NET 8.0 project
- [ ] Setup DI container with Microsoft.Extensions.Hosting
- [ ] Implement PathResolver with git root detection
- [ ] Implement GitHelper
- [ ] Setup MessagePipe
- [ ] Create core models (PreparationConfig, etc.)

### Phase 2: Services (Week 2)

- [ ] Implement ConfigService (CRUD operations)
- [ ] Implement CacheService (cache management)
- [ ] Implement ValidationService (4 levels)
- [ ] Implement ManifestService (manifest.json manipulation)
- [ ] Unit tests for services

### Phase 3: Code Patchers (Week 3)

- [ ] Implement IPatcher interface
- [ ] Implement CSharpPatcher (Roslyn)
- [ ] Implement JsonPatcher (System.Text.Json)
- [ ] Implement UnityAssetPatcher (custom YAML)
- [ ] Implement TextPatcher (regex)
- [ ] Unit tests for patchers

### Phase 4: CLI Mode (Week 4)

- [ ] Setup System.CommandLine
- [ ] Implement config commands
- [ ] Implement cache commands
- [ ] Implement prepare commands
- [ ] Implement restore commands
- [ ] Integration tests

### Phase 5: TUI Mode (Week 5)

- [ ] Setup Terminal.Gui v2
- [ ] Implement MainWindow
- [ ] Implement CacheManagementView
- [ ] Implement ConfigEditorView
- [ ] Implement ValidationView
- [ ] Bind ViewModels with ReactiveUI

### Phase 6: Integration & Testing (Week 6)

- [ ] Integrate with Nuke build system
- [ ] End-to-end testing
- [ ] Performance testing
- [ ] Documentation
- [ ] User guide

## Testing Strategy

### Unit Tests

- PathResolver git root detection
- Each patcher strategy
- Validation logic
- Config serialization/deserialization

### Integration Tests

- CLI command execution
- Cache population from code-quality
- Preparation execution on test project
- Config ↔ Cache bidirectional sync

### End-to-End Tests

- Full preparation → build → restore cycle
- TUI workflow simulation
- Error handling and recovery

## Security Considerations

1. **Path Traversal:** Validate all paths stay within git root
2. **Code Injection:** Sanitize all patch patterns
3. **File Permissions:** Check write permissions before operations
4. **Git Operations:** Validate git repository state

## Performance Considerations

1. **Reactive Updates:** Use throttling for high-frequency updates
2. **File Operations:** Use async I/O for large files
3. **Caching:** Cache parsed syntax trees for repeated operations
4. **Parallel Operations:** Use parallel processing for independent operations

## Migration Strategy

### From Existing Config

1. Tool reads old config format
2. Converts paths to git root-relative
3. Validates converted config
4. Saves in new format
5. Keeps backup of old config

### Rollback Plan

1. Keep old config as `.bak`
2. Tool can revert to old format
3. Document manual rollback steps

## Alternatives Considered

### 1. PowerShell Scripts

**Pros:** Simple, no compilation needed
**Cons:** No type safety, hard to maintain, no TUI

### 2. Python Tool

**Pros:** Cross-platform, rich ecosystem
**Cons:** Dependency management, no strong typing, slower

### 3. Unity Editor Extension

**Pros:** Native Unity integration
**Cons:** Requires Unity running, can't run in CI easily

**Decision:** .NET tool with CLI/TUI provides best balance of type safety, performance, and usability.

## Open Questions

1. **Q:** Should we support multiple config files?
   **A:** Yes, but one active config at a time. Use `--config` flag.

2. **Q:** How to handle Unity version differences?
   **A:** Validate Unity version in config, warn if mismatch.

3. **Q:** Should TUI support undo/redo?
   **A:** Yes, implement command pattern for TUI operations.

4. **Q:** How to handle concurrent modifications?
   **A:** File locking + timestamp checking.

## Success Metrics

1. **Usability:** Developers can create/modify configs in < 5 minutes
2. **Reliability:** 99% success rate for preparation execution
3. **Performance:** Preparation completes in < 30 seconds
4. **Maintainability:** New operations added in < 1 day
5. **Adoption:** 100% of builds use this tool within 1 month

## References

- [R-PATH-010: Path Resolution Rules](.agent/base/30-path-resolution.md)
- [R-BLD-010: Build System Rules](.agent/base/20-rules.md)
- [Spec-Kit Documentation](docs/SPEC-KIT.md)
- [Terminal.Gui v2 Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [MessagePipe Documentation](https://github.com/Cysharp/MessagePipe)

## Appendix A: Project Structure

```
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
├── SangoCard.Build.Tool.sln
└── SangoCard.Build.Tool/
    ├── SangoCard.Build.Tool.csproj
    ├── Program.cs
    ├── Cli/                    # CLI mode
    ├── Tui/                    # TUI mode
    ├── Core/                   # Core logic
    │   ├── Models/
    │   ├── Services/
    │   ├── Patchers/
    │   └── Utilities/
    ├── Messages/               # MessagePipe messages
    ├── State/                  # Reactive state
    └── Tests/
        ├── Unit/
        ├── Integration/
        └── EndToEnd/
```

## Appendix B: Message Catalog

```csharp
// Config messages
public record ConfigLoadedMessage(PreparationConfig Config);
public record ConfigSavedMessage(string Path);
public record ConfigValidatedMessage(ValidationResult Result);

// Cache messages
public record CacheUpdatedMessage(CacheItem Item, CacheOperation Operation);
public record CachePopulatedMessage(int PackageCount, int AssemblyCount);
public record CacheCleanedMessage();

// Preparation messages
public record PreparationStartedMessage(string ConfigPath, string ClientPath);
public record PreparationProgressMessage(string Stage, int Progress, int Total);
public record PreparationCompletedMessage(TimeSpan Duration);
public record PreparationFailedMessage(string Error, Exception Exception);

// Validation messages
public record ValidationStartedMessage();
public record ValidationCompletedMessage(ValidationResult Result);
public record ValidationErrorMessage(string Error, string FilePath);
```

## Appendix C: CLI Command Reference

```bash
# Config commands
tool config create [--output <path>] [--template <name>]
tool config add-package --config <path> --source <path> --destination <path> [--category <name>]
tool config add-assembly --config <path> --source <path> --destination <path>
tool config add-define --config <path> --name <symbol> --operation <add|remove>
tool config add-patch --config <path> --file <path> --type <type> --pattern <pattern> [--replacement <text>]
tool config remove <type> --config <path> --identifier <id>
tool config list <type> --config <path>

# Cache commands
tool cache populate --source <path> --destination <path> [--packages-only]
tool cache add <type> --source <path> --destination <path>
tool cache list [--cache <path>] [--type <packages|assemblies>]
tool cache clean [--cache <path>] [--type <packages|assemblies>]

# Prepare commands
tool prepare run --config <path> --client <path> --build-target <target> [--dry-run]
tool prepare validate --config <path> [--check-files] [--check-patches]

# Restore commands
tool restore run --client <path> --method <git|backup> [--backup-path <path>]

# TUI mode
tool tui [--config <path>]
```
