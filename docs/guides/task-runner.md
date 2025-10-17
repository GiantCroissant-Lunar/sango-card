---
doc_id: DOC-2025-00070
title: Task Runner Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:
- task
- build
- automation
summary: Complete guide to using Task for building and development.
---

# Task Runner Integration Guide

This project uses [Task](https://taskfile.dev) as a task runner to simplify build workflows and provide a unified interface for common development tasks.

## Installation

### Windows (via winget)

```powershell
winget install Task.Task
```

### Windows (via Scoop)

```powershell
scoop install task
```

### Windows (via Chocolatey)

```powershell
choco install go-task
```

### macOS (via Homebrew)

```bash
brew install go-task/tap/go-task
```

### Linux (via snap)

```bash
snap install task --classic
```

### Linux (via script)

```bash
sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin
```

### Or use the helper

```bash
task install-task
```

## Quick Start

After installation, verify Task is working:

```bash
task --version
```

List all available tasks:

```bash
task --list
```

Show detailed project information:

```bash
task info
```

## Common Tasks

### Setup & Development

```bash
# Initial setup - restore all dependencies
task setup

# Complete development workflow (clean + restore + build)
task dev

# Run CI pipeline (clean + restore + build + test)
task ci
```

### Building

```bash
# Build with Nuke (default)
task build

# Build Unity project for Windows
task build:unity

# Build Unity project for Android
task build:unity:android

# Build Unity project for iOS
task build:unity:ios

# Build Unity project for WebGL
task build:unity:webgl

# Rebuild (clean + build)
task rebuild
```

### Testing

```bash
# Run all tests
task test

# Run Unity tests specifically
task test:unity
```

### Cleaning

```bash
# Clean all build artifacts
task clean

# Clean only Nuke build artifacts
task clean:nuke

# Clean only Unity artifacts
task clean:unity

# Clean only output directory
task clean:output
```

### Packaging

```bash
# Export Unity package
task package
```

### Advanced Usage

#### Run Custom Nuke Targets

You can run any Nuke target directly:

```bash
# Run custom Nuke target
task nuke -- CleanUnity

# Run Nuke target with parameters
task nuke -- BuildUnity --unity-build-target Android --unity-path "C:\Program Files\Unity\Hub\Editor\2023.1.0f1\Editor\Unity.exe"
```

#### View Documentation

```bash
# Open main README
task docs

# Open Unity build component documentation
task docs:unity
```

## Task Integration with Nuke

Task acts as a convenient wrapper around the Nuke build system:

```
┌─────────────┐
│    Task     │  ← Simple, human-friendly commands
└──────┬──────┘
       │
       ▼
┌─────────────┐
│    Nuke     │  ← Powerful C# build automation
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Unity Build │  ← Unity project compilation
└─────────────┘
```

## Benefits of Using Task

1. **Simplicity**: Easy-to-remember commands (`task build` vs `./build/nuke/build.ps1 Compile`)
2. **Cross-platform**: Same commands work on Windows, macOS, and Linux
3. **Discoverability**: `task --list` shows all available commands
4. **Documentation**: Built-in descriptions for each task
5. **Consistency**: Standardized interface across different projects
6. **IDE Integration**: Works with VS Code Task Runner, JetBrains, etc.

## IDE Integration

### VS Code

Install the [Task extension](https://marketplace.visualstudio.com/items?itemName=task.vscode-task):

```bash
code --install-extension task.vscode-task
```

Then use `Ctrl+Shift+P` → "Tasks: Run Task" → Select a task

### JetBrains IDEs (Rider, IntelliJ)

1. Open Run/Debug Configurations
2. Add "Shell Script" configuration
3. Set script to `task <task-name>`

## Customization

You can customize tasks by editing `Taskfile.yml` in the project root. Common customizations:

### Override Variables

```bash
# Use custom Unity project path
task build:unity UNITY_PROJECT=./path/to/unity

# Use custom output directory
task build OUTPUT_DIR=./custom-output
```

### Add New Tasks

Edit `Taskfile.yml` and add your custom tasks:

```yaml
tasks:
  my-custom-task:
    desc: My custom task description
    cmds:
      - echo "Running my custom task"
      - ./my-script.sh
```

## Task File Structure

The `Taskfile.yml` is organized into categories:

- **Setup tasks** (`setup:*`) - Environment initialization
- **Clean tasks** (`clean:*`) - Artifact cleanup
- **Build tasks** (`build:*`) - Compilation and building
- **Test tasks** (`test:*`) - Running tests
- **Package tasks** (`package`) - Creating distributable packages
- **Workflow tasks** (`dev`, `ci`) - Complete workflows
- **Utility tasks** (`nuke`, `docs`) - Helper commands

## Tips & Tricks

### Silent Mode

Run tasks without output (only errors):

```bash
task --silent build
```

### Dry Run

See what commands would be executed without running them:

```bash
task --dry build
```

### List All Tasks with Descriptions

```bash
task --list-all
```

### Run Multiple Tasks

```bash
task clean setup build test
```

### Task Dependencies

Tasks automatically handle dependencies. For example, `task dev` runs:

1. `clean`
2. `setup`
3. `build`

### Environment Variables

Set environment variables for a single task:

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 task build
```

## Troubleshooting

### Task not found

Make sure Task is in your PATH:

```bash
which task  # Linux/macOS
where task  # Windows
```

### Permission denied (Linux/macOS)

Make sure the Nuke build scripts are executable:

```bash
chmod +x ./build/nuke/build.sh
```

### Unity path not found

Specify Unity path explicitly:

```bash
task nuke -- BuildUnity --unity-path "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
```

## Migration from Direct Nuke Calls

Before (using Nuke directly):

```bash
# Windows
.\build\nuke\build.ps1 Compile
.\build\nuke\build.ps1 BuildUnity --unity-build-target Android

# Linux/macOS
./build/nuke/build.sh Compile
./build/nuke/build.sh BuildUnity --unity-build-target Android
```

After (using Task):

```bash
# Same on all platforms
task build
task build:unity:android
```

## Further Reading

- [Task Documentation](https://taskfile.dev)
- [Task GitHub Repository](https://github.com/go-task/task)
- [Nuke Build Documentation](https://nuke.build)
- [Unity Build Component README](./build/nuke/build/Components/README.md)

## Support

For issues with:

- **Task runner itself**: See [Task GitHub Issues](https://github.com/go-task/task/issues)
- **Nuke build system**: Check `./build/nuke/build/` directory
- **Unity builds**: See Unity build component documentation
- **Project-specific issues**: Create an issue in this repository
