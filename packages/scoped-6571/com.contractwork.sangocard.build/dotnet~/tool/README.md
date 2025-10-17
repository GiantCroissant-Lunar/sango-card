# Sango Card Build Preparation Tool

A .NET CLI/TUI tool for managing Unity build preparation configurations with reactive architecture and git root-based path resolution.

## Features

- **CLI Mode:** Command-line interface for automation and CI/CD
- **TUI Mode:** Interactive terminal UI for human configuration
- **Git Root Path Resolution:** All paths relative to repository root
- **Reactive Architecture:** Real-time state updates with ReactiveUI
- **Message-Driven:** Loose coupling via MessagePipe
- **Code Patching:** Syntax-aware patching for C#, JSON, Unity assets
- **Auto-Validation:** Multi-level validation with clear errors

## Quick Start

### Build

```bash
dotnet build
```

### Run CLI Mode

```bash
dotnet run -- config create --output "build/preparation/configs/build-preparation.json"
dotnet run -- cache populate --source "projects/code-quality"
dotnet run -- prepare run --config "..." --client "..." --build-target "StandaloneWindows64"
```

### Run TUI Mode

```bash
dotnet run -- tui
```

## Project Structure

```
SangoCard.Build.Tool/
├── Program.cs                  # Entry point
├── Cli/                        # CLI mode
│   ├── CliHost.cs
│   └── Commands/
├── Tui/                        # TUI mode
│   ├── TuiHost.cs
│   └── Views/
├── Core/                       # Core logic
│   ├── Models/
│   ├── Services/
│   ├── Patchers/
│   └── Utilities/
├── Messages/                   # MessagePipe messages
└── State/                      # Reactive state

SangoCard.Build.Tool.Tests/
└── Unit/                       # Unit tests
```

## Development Status

### ✅ Epic 1-2: Core Infrastructure & Services (Complete)

**Task 1.1: Project Setup**

- .NET 8.0 project created with all dependencies
- Solution structure established
- EditorConfig configured
- Test project created

**Task 1.2: DI Setup**

- Host builder with Microsoft.Extensions.Hosting
- Service registration and DI container
- Logging configuration
- MessagePipe integration

**Task 1.3: PathResolver**

- Git root detection
- Path resolution relative to git root
- Cross-platform path handling

**Task 1.4: Core Models**

- PreparationConfig, AssetManipulation, CodePatch
- ScriptingDefineSymbols, CacheItem, ValidationResult
- JSON serialization support

**Task 2.1: ConfigService**

- Load/Save JSON configurations
- Add/Remove packages, assemblies, defines, patches
- MessagePipe event publishing

**Task 2.2: CacheService**

- Populate from code-quality
- Add/Remove cached items
- Bidirectional config-cache sync

**Task 2.3: ValidationService**

- 4-level validation (Schema, FileExistence, UnityPackages, Full)
- Clear error messages with file paths
- Validation summary

**Task 2.4: ManifestService**

- Read/Write Unity manifest.json
- Add/Remove packages
- Format preservation

**Task 2.5: PreparationService** ✅ JUST COMPLETED

- ExecuteAsync() with full orchestration
- Backup/restore mechanism
- Rollback on error
- Dry-run mode support
- Pre-execution validation
- Progress reporting via MessagePipe
- 10/10 unit tests passing

### ⏳ Next: Task 3.1 - Patcher Interface & Base (Wave 6)

**Upcoming Tasks:**

- Epic 3: Code Patchers (Tasks 3.1-3.5)
- Epic 4: CLI Implementation (Tasks 4.1-4.3)
- Epic 5: TUI Implementation (Tasks 5.1-5.5)
- Epic 6: Testing & Integration (Tasks 6.1-6.4)
- Epic 7: Documentation & Deployment (Tasks 7.1-7.3)

## Technology Stack

- **.NET 8.0** - Latest LTS
- **Microsoft.Extensions.Hosting** - DI, logging, configuration
- **MessagePipe** - High-performance messaging
- **ReactiveUI** - Reactive state management
- **System.CommandLine** - CLI framework
- **Terminal.Gui** - TUI framework
- **Roslyn** - C# code analysis
- **System.Text.Json** - JSON manipulation
- **YamlDotNet** - YAML parsing (Unity assets)

## Documentation

- **Specification:** `.specify/specs/build-preparation-tool.md`
- **RFC:** `docs/rfcs/RFC-001-build-preparation-tool.md`
- **Tasks:** `.specify/tasks/build-preparation-tool-tasks-v2.md`
- **Agent Rules:** `.agent/base/30-path-resolution.md`

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Publishing

```bash
# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained

# Output: bin/Release/net8.0/win-x64/publish/SangoCard.Build.Tool.exe
```

## License

Internal tool for Sango Card project.
