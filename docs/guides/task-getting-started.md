---
doc_id: DOC-2025-00181
title: Task Getting Started
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [task-getting-started]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00131
title: Task Getting Started
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [task-getting-started]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00071
title: Task Runner Getting Started
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- task
- quickstart
- setup
summary: Quick start guide for Task runner setup and basic usage.

---

# Getting Started with Task

Welcome! This guide will help you get up and running with Task in the Sango Card project.

## Step 1: Install Task

Choose the method that works best for your platform:

### Windows

```powershell
# Automated installer (recommended)
.\scripts\install-task.ps1

# Or use winget
winget install Task.Task

# Or use Scoop
scoop install task

# Or use Chocolatey
choco install go-task
```

### macOS

```bash
# Homebrew (recommended)
brew install go-task/tap/go-task
```

### Linux

```bash
# Snap
snap install task --classic

# Or universal installer
sh -c "$(curl -sL https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin
```

## Step 2: Verify Installation

After installation, restart your terminal and verify:

```bash
task --version
```

You should see something like: `Task version: v3.x.x`

## Step 3: Explore Available Tasks

List all available tasks:

```bash
task --list
```

Or get detailed information:

```bash
task info
```

## Step 4: Your First Task

Try running your first task:

```bash
task setup
```

This will restore all project dependencies and prepare your development environment.

## Step 5: Common Workflows

### Development Workflow

```bash
# Clean, setup, and build everything
task dev
```

### Building

```bash
# Build with Nuke
task build

# Build Unity project
task build:unity

# Build for specific platforms
task build:unity:android
task build:unity:ios
```

### Testing

```bash
# Run all tests
task test

# Run Unity tests
task test:unity
```

### Cleaning

```bash
# Clean everything
task clean

# Clean specific parts
task clean:nuke
task clean:unity
task clean:output
```

## Step 6: Run Complete CI Pipeline

Test everything before committing:

```bash
task ci
```

This runs: clean → setup → build → test

## IDE Integration

### VS Code

1. The project already has `.vscode/tasks.json` configured
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
3. Type "Tasks: Run Task"
4. Select from available tasks

**Keyboard shortcuts:**

- `Ctrl+Shift+B` - Run build task
- `Ctrl+Shift+T` - Run test task

### JetBrains Rider/IntelliJ

1. Open "Run/Debug Configurations"
2. Add new "Shell Script" configuration
3. Script: `task <task-name>`
4. Working directory: Project root

## Advanced Usage

### Running Custom Nuke Targets

You can run any Nuke target through Task:

```bash
# Run custom target
task nuke -- CleanUnity

# With parameters
task nuke -- BuildUnity --unity-build-target Android --unity-path "C:\Path\To\Unity.exe"
```

### Multiple Tasks in Sequence

Run multiple tasks one after another:

```bash
task clean setup build test
```

### Override Variables

```bash
task build:unity UNITY_PROJECT=./custom/path OUTPUT_DIR=./custom-output
```

### Silent Mode

Run without output (only errors shown):

```bash
task --silent build
```

### Dry Run

See what would be executed without running:

```bash
task --dry build
```

## Shell Completion (Optional)

Enable tab completion for Task commands:

### Linux/macOS

```bash
./scripts/setup-completion.sh
```

### PowerShell (Windows)

```powershell
task --completion powershell >> $PROFILE
```

Then restart your terminal and try:

```bash
task <TAB>
```

## Understanding Task Output

Task provides clear output with:

- ✓ Success indicators
- ✗ Error indicators
- Colored output for readability
- Command execution details

Example:

```
task: [setup:dotnet] dotnet restore
✓ Setup complete!
```

## Troubleshooting

### Task not found after installation

**Windows:**

```powershell
# Check PATH
$env:Path -split ';' | Select-String task

# Restart terminal or refresh PATH
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
```

**Linux/macOS:**

```bash
# Check if in PATH
which task

# Add to PATH (if needed)
export PATH="$HOME/.local/bin:$PATH"
```

### Permission denied (Linux/macOS)

Make scripts executable:

```bash
chmod +x ./build/nuke/build.sh
chmod +x ./scripts/*.sh
```

### Unity path not found

Specify Unity path explicitly:

```bash
task nuke -- BuildUnity --unity-path "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity"
```

### Task seems slow

Task caches results. Clean cache if needed:

```bash
rm -rf .task/
```

## Next Steps

1. **Read full documentation**: See [Complete Guide](README.md) for comprehensive details
2. **Explore Unity builds**: Check `../../build/nuke/build/Components/README.md`
3. **Customize tasks**: Edit `../../Taskfile.yml` to add your own tasks
4. **Check quick reference**: See [Quick Reference](QUICK_REFERENCE.md) for command cheat sheet

## Getting Help

### Built-in Help

```bash
# General help
task --help

# Task-specific help
task <task-name> --help

# List with descriptions
task --list-all
```

### Documentation

- Project docs: [Complete Guide](README.md)
- Task website: <https://taskfile.dev>
- Task GitHub: <https://github.com/go-task/task>

### Common Commands Reference

| What you want | Command |
|---------------|---------|
| List all tasks | `task --list` |
| Setup environment | `task setup` |
| Build project | `task build` |
| Run tests | `task test` |
| Clean artifacts | `task clean` |
| Full dev workflow | `task dev` |
| CI pipeline | `task ci` |
| Build Unity (Windows) | `task build:unity` |
| Build Unity (Android) | `task build:unity:android` |
| Show project info | `task info` |
| Open docs | `task docs` |

## Tips for Success

1. **Always start with setup**: `task setup` ensures all dependencies are ready
2. **Use dev workflow during development**: `task dev` is your friend
3. **Run ci before committing**: `task ci` catches issues early
4. **Explore with --list**: Discover tasks you didn't know existed
5. **Use tab completion**: Makes task names easier to remember
6. **Check task info**: `task info` shows project configuration
7. **Read error messages**: Task provides helpful error context

## What's Different from Nuke?

Task is a **wrapper** around Nuke, not a replacement:

| Task | Nuke |
|------|------|
| Simple commands | Full C# power |
| Cross-platform scripts | Cross-platform builds |
| YAML configuration | C# code |
| Quick common tasks | Complex build logic |
| Entry point for devs | Build engine |

**Use Task for**: Daily development workflows, common operations
**Use Nuke for**: Complex build logic, custom build steps, advanced scenarios

Both work together seamlessly! Task calls Nuke behind the scenes.

---

**Ready to go?** Try this:

```bash
task setup
task build
task test
```

Welcome aboard! 🚀
