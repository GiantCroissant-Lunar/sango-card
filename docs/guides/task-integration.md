---
doc_id: DOC-2025-00183
title: Task Integration
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [task-integration]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00133
title: Task Integration
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [task-integration]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00073
title: Task Runner Integration
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- task
- integration
- ci-cd
summary: Technical overview of Task runner integration with build system.

---

# Task Integration Summary

This document summarizes the Task (go-task/task) integration into the Sango Card project.

> **📚 Documentation Index**: See [INDEX.md](INDEX.md) for complete documentation navigation.

## What Was Added

### Core Files

1. **`../../Taskfile.yml`** - Main task runner configuration
   - 40+ pre-configured tasks for common workflows
   - Cross-platform support (Windows, macOS, Linux)
   - Integrated with existing Nuke build system
   - Organized task categories (setup, build, test, clean, etc.)

2. **`docs/task/README.md`** - Comprehensive documentation (this directory)
   - Installation instructions for all platforms
   - Usage guide with examples
   - Task descriptions and workflows
   - Troubleshooting section
   - IDE integration guide

3. **`../../.taskfiles/unity.yml`** - Unity-specific task extensions (optional)
   - Unity project management tasks
   - Platform listing
   - Cache management
   - Log viewing
   - Deep cleaning utilities

4. **`docs/task/QUICK_REFERENCE.md`** - Quick reference card
   - One-liner installation commands
   - Essential command reference
   - Common workflows
   - Tips and tricks

### Support Files

5. **`../../scripts/install-task.ps1`** - Windows installation script
   - Tries multiple installation methods (winget, scoop, choco)
   - Falls back to manual installation
   - Automatically adds to PATH

6. **`../../scripts/install-task.sh`** - Linux/macOS installation script
   - Tries package managers (brew, snap)
   - Falls back to official install script
   - Shell RC configuration

7. **`../../.vscode/tasks.json`** - VS Code integration
   - Pre-configured VS Code tasks
   - Keyboard shortcuts support
   - Panel management

8. **`../../.github/workflows/task-ci.yml`** - CI/CD example
   - Multi-platform CI workflow
   - Task-based build pipeline
   - Artifact uploading

### Documentation Updates

9. **`../../README.md`** - Updated with Task information
    - Added Task to prerequisites
    - Updated getting started section
    - Added workflow examples

11. **`../../.gitignore`** - Updated to exclude Task output
    - `.task/` directory
    - `output/` directory

## Features

### Task Categories

#### Setup & Initialization

- `task setup` - Complete environment setup
- `task setup:dotnet` - Restore .NET dependencies
- `task setup:unity` - Verify Unity installation

#### Building

- `task build` - Build with Nuke
- `task build:unity` - Build Unity for Windows
- `task build:unity:android` - Build for Android
- `task build:unity:ios` - Build for iOS
- `task build:unity:webgl` - Build for WebGL

#### Testing

- `task test` - Run all tests
- `task test:unity` - Run Unity tests

#### Cleaning

- `task clean` - Clean all artifacts
- `task clean:nuke` - Clean Nuke artifacts
- `task clean:unity` - Clean Unity artifacts
- `task clean:output` - Clean output directory

#### Workflows

- `task dev` - Complete development workflow (clean + setup + build)
- `task ci` - Full CI pipeline (clean + setup + build + test)
- `task rebuild` - Clean and rebuild

#### Utilities

- `task info` - Display project information
- `task docs` - Open documentation
- `task nuke -- <target>` - Run custom Nuke targets
- `task install-task` - Install Task runner

### Key Benefits

1. **Simplicity** - Single command interface across platforms
2. **Discoverability** - `task --list` shows all available commands
3. **Cross-platform** - Same commands on Windows, macOS, Linux
4. **IDE Integration** - Works with VS Code, JetBrains, etc.
5. **Documentation** - Built-in help and descriptions
6. **Extensibility** - Easy to add custom tasks
7. **Performance** - Parallel task execution
8. **Compatibility** - Works alongside existing Nuke builds

## Installation

### Quick Install

**Windows:**

```powershell
.\scripts\install-task.ps1
# or
winget install Task.Task
```

**macOS:**

```bash
brew install go-task/tap/go-task
```

**Linux:**

```bash
sh -c "$(curl -sL https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin
```

### Verification

```bash
task --version
task --list
```

## Usage Examples

### Daily Development

```bash
# First time
task setup
task build

# Daily workflow
task dev

# Before committing
task ci
```

### Building for Different Platforms

```bash
task build:unity                # Windows
task build:unity:android        # Android
task build:unity:ios            # iOS
task build:unity:webgl          # WebGL
```

### Running Custom Nuke Targets

```bash
task nuke -- CleanUnity
task nuke -- BuildUnity --unity-build-target Android
```

### IDE Integration

- **VS Code**: `Ctrl+Shift+P` → "Tasks: Run Task"
- **JetBrains**: Create Shell Script run configuration with `task <name>`

## Architecture

```
┌──────────────────┐
│   Developer      │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   Task Runner    │ ← Taskfile.yml (this integration)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   Nuke Build     │ ← Existing build system
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│   Unity Build    │ ← IUnityBuild component
└──────────────────┘
```

## File Structure

```
sango-card/
├── Taskfile.yml                    # Main task configuration
├── README.md                       # Updated main README
│
├── docs/task/                      # Task documentation
│   ├── INDEX.md                    # Documentation index
│   ├── README.md                   # Complete guide
│   ├── GETTING_STARTED.md          # New user guide
│   ├── QUICK_REFERENCE.md          # Quick reference card
│   └── INTEGRATION.md              # This file
│
├── .taskfiles/                     # Task modules (optional)
│   └── unity.yml                   # Unity-specific tasks
│
├── scripts/                        # Installation scripts
│   ├── install-task.ps1            # Windows installer
│   ├── install-task.sh             # Linux/macOS installer
│   └── setup-completion.sh         # Shell completion
│
├── .vscode/                        # VS Code integration
│   └── tasks.json                  # Pre-configured tasks
│
├── .github/workflows/              # CI/CD
│   └── task-ci.yml                 # Task-based workflow
│
├── build/nuke/build/               # Existing Nuke build
│   ├── Build.cs                    # Updated with IUnityBuild
│   └── Components/
│       ├── IUnityBuild.cs          # Unity component
│       └── README.md               # Component docs
│
└── .gitignore                      # Updated
```

## Migration Path

### For Existing Users

No changes required! Existing Nuke commands still work:

- `./build/nuke/build.ps1 Compile`
- `./build/nuke/build.sh BuildUnity`

### For New Users

Use Task for simplified experience:

- `task build`
- `task build:unity`

### Gradual Adoption

1. Install Task: `./scripts/install-task.ps1`
2. Try basic commands: `task setup`, `task build`
3. Learn more: `task --list`, see `TASK.md`
4. Use as primary interface or alongside Nuke

## Customization

### Adding New Tasks

Edit `Taskfile.yml`:

```yaml
tasks:
  my-task:
    desc: My custom task
    cmds:
      - echo "Hello from my task"
      - ./my-script.sh
```

### Overriding Variables

```bash
task build:unity UNITY_PROJECT=./custom/path OUTPUT_DIR=./custom-output
```

### Environment Variables

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 task build
```

## Next Steps

1. **Install Task** - Run `../../scripts/install-task.ps1` (Windows) or `../../scripts/install-task.sh` (Linux/macOS)
2. **Verify Installation** - Run `task --version`
3. **Explore Tasks** - Run `task --list`
4. **Read Documentation** - See [README.md](README.md) or [INDEX.md](INDEX.md) for detailed guide
5. **Try It Out** - Run `task dev` to test the complete workflow

## Support Resources

- **Task Documentation**: <https://taskfile.dev>
- **Task GitHub**: <https://github.com/go-task/task>
- **Project Documentation**: See [INDEX.md](INDEX.md)
- **Complete Guide**: [README.md](README.md)
- **Quick Reference**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
- **Getting Started**: [GETTING_STARTED.md](GETTING_STARTED.md)
- **Unity Component**: `../../build/nuke/build/Components/README.md`

## Troubleshooting

### Task not found

```bash
# Verify installation
task --version

# Check PATH
which task  # Linux/macOS
where task  # Windows

# Reinstall
./scripts/install-task.ps1  # Windows
./scripts/install-task.sh   # Linux/macOS
```

### Unity path not found

```bash
# Specify Unity path explicitly
task nuke -- BuildUnity --unity-path "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
```

### Permission denied (Linux/macOS)

```bash
chmod +x ./build/nuke/build.sh
chmod +x ./scripts/*.sh
```

## Feedback

If you encounter issues or have suggestions:

1. Check [README.md](README.md) troubleshooting section
2. Review [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
3. Create an issue in the repository
4. Consult Task documentation at <https://taskfile.dev>

---

**Version**: 1.0  
**Date**: October 2025  
**Integration**: Complete ✓
