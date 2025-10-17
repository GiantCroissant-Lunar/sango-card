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

### ✅ Task 1.1: Project Setup (Complete)

- .NET 8.0 project created
- NuGet packages configured
- Solution structure established
- EditorConfig configured
- Test project created

### 🔄 Task 1.2: DI Setup (In Progress)

- Host builder setup
- Service registration
- Logging configuration
- MessagePipe integration

### ⏳ Upcoming Tasks

- Task 1.3: PathResolver implementation
- Task 1.4: Core models
- Task 2.x: Services (Config, Cache, Validation, Preparation)
- Task 3.x: Code patchers (C#, JSON, Unity, Text)
- Task 4.x: CLI commands
- Task 5.x: TUI views
- Task 6.x: Integration and testing

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
