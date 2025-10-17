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

### Two-Phase Workflow (Recommended)

The tool uses a **two-phase workflow** to comply with R-BLD-060 (projects/client is read-only outside build operations):

**Phase 1: Populate Cache (Safe Anytime)**

```bash
# Gather references into cache - safe to run anytime
dotnet run -- cache populate --source projects/code-quality
```

**Phase 2: Inject to Client (Build-Time Only)**

```bash
# Inject from cache to client - ONLY during builds
dotnet run -- prepare inject --config build/preparation/configs/default.json --target projects/client/
```

### Run CLI Mode

```bash
# Create new configuration
dotnet run -- config create --output build/preparation/configs/build-preparation.json

# Phase 1: Populate cache
dotnet run -- cache populate --source projects/code-quality

# Phase 2: Inject to client (with validation)
dotnet run -- prepare inject --config build/preparation/configs/default.json --target projects/client/ --verbose

# Validate configuration
dotnet run -- config validate --file build/preparation/configs/default.json --level Full

# Dry-run (preview changes)
dotnet run -- prepare inject --config <path> --target projects/client/ --dry-run
```

### Run TUI Mode

```bash
# Interactive terminal UI
dotnet run -- tui

# Navigate with:
# - F2: Config Editor
# - F3: Cache Management
# - F4: Validation
# - F5: Preparation Execution
# - F10: Exit
```

### Deprecated Command (Backward Compatibility)

```bash
# OLD: prepare run (deprecated, shows warning)
dotnet run -- prepare run --config <path>

# This redirects to: prepare inject --target projects/client/
# Will be removed in v2.0.0
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

**Task 2.5: PreparationService** ✅ COMPLETE

- ExecuteAsync() with full orchestration
- Backup/restore mechanism
- Rollback on error
- Dry-run mode support
- Pre-execution validation
- Progress reporting via MessagePipe
- 10/10 unit tests passing

### ✅ Wave 7-8: CLI & TUI Complete

**Task 4.1-4.3: CLI Commands** ✅ COMPLETE (14 commands)

- Config commands (create, load, save, add-package, add-assembly, patch add)
- Cache commands (populate, list)
- Prepare commands (run, dry-run, restore)
- Validation commands (validate)

**Task 5.1: TUI Foundation** ✅ COMPLETE

- Terminal.Gui v2.0.0 integration
- Application lifecycle with proper init/shutdown
- MenuBar with File/View/Help menus
- StatusBar with F-key shortcuts
- Welcome screen with navigation
- View switching infrastructure
- MessagePipe subscriptions for logging

**Task 5.2-5.5: TUI Views** ✅ COMPLETE (1,395 lines)

All 4 views fully implemented with service integration:

- **ConfigEditorView** (422 lines)
  - Load/save configurations
  - Create new configs
  - Add/edit packages, assemblies, code patches
  - Real-time UI updates via MessagePipe
  - Input validation and error handling

- **CacheManagementView** (285 lines)
  - Populate cache from source directory
  - List cache contents with formatted display
  - Clean cache with confirmation
  - Progress tracking
  - Statistics panel (item count, total size)

- **ValidationView** (305 lines)
  - Load configurations
  - Select validation level (4 levels)
  - Execute validation with progress
  - Display errors/warnings in split view
  - Detailed validation summary

- **PreparationExecutionView** (383 lines)
  - Load configurations
  - Dry-run mode toggle (defaults ON for safety)
  - Optional pre-execution validation
  - Real-time execution log with timestamps
  - Live operation statistics
  - File operation tracking
  - User confirmations for safety

**Technology Stack Updates:**

- ✅ Terminal.Gui upgraded to v2.0.0
- ✅ Microsoft.CodeAnalysis.CSharp upgraded to 4.10.0
- ✅ All views migrated to Terminal.Gui v2 API
- ✅ MessagePipe integration for reactive updates
- ✅ Application builds and runs successfully (0 errors, 2 pre-existing warnings)

### ⏳ Next: Wave 9-11 - Testing & Documentation

**Wave 9: Integration & Testing (4 tasks)** ✅ COMPLETE

- Comprehensive manual testing guide (10 scenarios)
- Test plan documentation
- Build validation (0 errors)
- Existing test suite verified (86% coverage, 14 files)

**Deliverables**:

- TUI_MANUAL_TESTING_GUIDE.md - 10 detailed test scenarios
- WAVE_9_TEST_PLAN.md - Complete test strategy
- 60+ existing tests passing (5 pre-existing patcher failures noted)

**Wave 10-11: Documentation & Deployment (3 tasks)** ⏳ NEXT

- User documentation
- Developer documentation
- Package as dotnet tool
- NuGet publishing

### 📊 Overall Progress

- **Waves Complete**: 1-9 (Foundation → Services → Patchers → CLI → TUI → Testing)
- **Waves Remaining**: 10-11 (Documentation & Deployment)
- **Overall Progress**: ~90% (31/34 tasks complete)
- **Production Readiness**: ✅ Ready for documentation & deployment

## CLI Commands Reference

### Config Commands

**Create New Configuration:**

```bash
dotnet run -- config create --output <path> [--description <text>]

# Example:
dotnet run -- config create --output build/preparation/configs/my-config.json --description "My build config"
```

**Validate Configuration:**

```bash
dotnet run -- config validate --file <path> [--level <Schema|FileExistence|UnityPackages|Full>]

# Examples:
dotnet run -- config validate --file build/preparation/configs/default.json
dotnet run -- config validate --file build/preparation/configs/default.json --level Full
```

### Cache Commands

**Populate Cache:**

```bash
dotnet run -- cache populate --source <path> [--config <path>]

# Examples:
dotnet run -- cache populate --source projects/code-quality
dotnet run -- cache populate --source projects/code-quality --config build/preparation/configs/default.json
```

**List Cache Contents:**

```bash
dotnet run -- cache list
```

**Clean Cache:**

```bash
dotnet run -- cache clean
```

### Prepare Commands

**Inject Preparation (Phase 2):**

```bash
dotnet run -- prepare inject --config <path> --target <path> [--level <level>] [--verbose] [--force]

# Examples:
dotnet run -- prepare inject --config build/preparation/configs/default.json --target projects/client/
dotnet run -- prepare inject --config <path> --target projects/client/ --verbose
dotnet run -- prepare inject --config <path> --target projects/client/ --force  # Skip validation errors
```

**Dry-Run (Preview Changes):**

```bash
dotnet run -- prepare inject --config <path> --target projects/client/ --dry-run

# Shows what would be changed without actually modifying files
```

**Restore from Backup:**

```bash
dotnet run -- prepare restore [--backup-path <path>] [--verbose]
```

**Deprecated: Run (Backward Compatibility):**

```bash
dotnet run -- prepare run --config <path> [--level <level>]

# ⚠️ DEPRECATED: Use 'prepare inject --target projects/client/' instead
# Shows deprecation warning but still works
```

### Validation Levels

- **Schema:** Validates JSON schema only
- **FileExistence:** Schema + checks if referenced files exist
- **UnityPackages:** FileExistence + validates Unity package references
- **Full:** All validations (recommended)

### Exit Codes

- **0:** Success
- **1:** File not found or invalid arguments
- **2:** Validation failed
- **3:** Execution/injection failed

## TUI Mode Guide

### Views

**Config Editor (F2):**

- Load/save configurations
- Create new configs
- Add packages, assemblies, code patches
- Real-time validation
- MessagePipe reactive updates

**Cache Management (F3):**

- Populate cache from source
- List cache contents
- View statistics (count, size)
- Clean cache with confirmation

**Validation (F4):**

- Load configurations
- Select validation level
- Execute validation
- View errors/warnings
- Detailed summaries

**Preparation Execution (F5):**

- Load configurations
- Toggle dry-run mode (default: ON)
- Optional pre-validation
- Real-time execution log
- Live statistics
- Safety confirmations

### Keyboard Shortcuts

- **F1:** Help
- **F2:** Config Editor
- **F3:** Cache Management
- **F4:** Validation
- **F5:** Preparation Execution
- **F10:** Exit
- **Esc:** Close dialogs/return to previous view
- **Tab:** Navigate between controls
- **Enter:** Activate buttons/confirm
- **Space:** Toggle checkboxes

## Nuke Integration

The tool integrates with Nuke build system for automated workflows.

### Nuke Targets

**PrepareCache (Phase 1):**

```bash
nuke PrepareCache

# Populates cache from projects/code-quality
# Safe to run anytime, no client modification
```

**PrepareClient (Phase 2):**

```bash
nuke PrepareClient

# Performs git reset --hard on projects/client/
# Injects from cache to client
# Build-time only!
```

**RestoreClient:**

```bash
nuke RestoreClient

# Performs git reset --hard on projects/client/
# Cleans up after builds
```

**BuildUnityWithPreparation:**

```bash
nuke BuildUnityWithPreparation

# Full workflow:
# 1. PrepareCache
# 2. PrepareClient (with git reset)
# 3. BuildUnity
# 4. RestoreClient (cleanup)
```

**ValidatePreparation:**

```bash
nuke ValidatePreparation

# Validates configuration without executing
```

**DryRunPreparation:**

```bash
nuke DryRunPreparation

# Shows what would be injected without modifying files
```

### CI/CD Integration

**GitHub Actions Example:**

```yaml
name: Build Unity

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Validate Preparation
        run: nuke ValidatePreparation

      - name: Build Unity with Preparation
        run: nuke BuildUnityWithPreparation
```

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
