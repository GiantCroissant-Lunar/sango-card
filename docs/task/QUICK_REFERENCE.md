# Task Quick Reference Card

## Installation One-Liners

```bash
# Windows (PowerShell)
winget install Task.Task

# macOS
brew install go-task/tap/go-task

# Linux
sh -c "$(curl -sL https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin
```

## Essential Commands

| Command | Description |
|---------|-------------|
| `task` | Show available tasks |
| `task --list` | List all tasks with descriptions |
| `task info` | Show project information |
| `task setup` | Setup development environment |
| `task build` | Build the project |
| `task test` | Run all tests |
| `task clean` | Clean build artifacts |
| `task dev` | Full dev workflow (clean + setup + build) |
| `task ci` | CI pipeline (clean + setup + build + test) |

## Build Commands

| Command | Description |
|---------|-------------|
| `task build` | Build with Nuke |
| `task build:unity` | Build Unity (Windows 64-bit) |
| `task build:unity:android` | Build Unity for Android |
| `task build:unity:ios` | Build Unity for iOS |
| `task build:unity:webgl` | Build Unity for WebGL |
| `task rebuild` | Clean + Build |

## Unity Commands

| Command | Description |
|---------|-------------|
| `task build:unity` | Build Unity project |
| `task test:unity` | Run Unity tests |
| `task clean:unity` | Clean Unity artifacts |
| `task package` | Export Unity package |

## Clean Commands

| Command | Description |
|---------|-------------|
| `task clean` | Clean all artifacts |
| `task clean:nuke` | Clean Nuke build artifacts |
| `task clean:unity` | Clean Unity artifacts |
| `task clean:output` | Clean output directory |

## Advanced Usage

```bash
# Run custom Nuke target
task nuke -- CleanUnity

# Run Nuke with parameters
task nuke -- BuildUnity --unity-build-target Android

# Multiple tasks in sequence
task clean setup build test

# Silent mode
task --silent build

# Dry run (show commands without executing)
task --dry build

# Run from any directory
task -d /path/to/project build
```

## VS Code Integration

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
2. Type "Tasks: Run Task"
3. Select your task

Or use keyboard shortcuts:
- `Ctrl+Shift+B` - Run default build task
- `Ctrl+Shift+T` - Run default test task

## Common Workflows

### First Time Setup
```bash
task setup
task build
```

### Daily Development
```bash
task dev          # Clean + Setup + Build
```

### Before Committing
```bash
task ci           # Full CI pipeline
```

### Building for Multiple Platforms
```bash
task build:unity                    # Windows
task build:unity:android            # Android
task build:unity:ios                # iOS
```

### Debugging Build Issues
```bash
task clean                          # Clean everything
task setup                          # Restore dependencies
task build                          # Try building again
```

### Custom Nuke Target
```bash
task nuke -- YourCustomTarget --parameter value
```

## Environment Variables

```bash
# Disable .NET telemetry
DOTNET_CLI_TELEMETRY_OPTOUT=1 task build

# Custom Unity project path
task build:unity UNITY_PROJECT=./custom/path
```

## Troubleshooting

### Task not found
```bash
# Check if Task is installed
task --version

# Check if it's in PATH
which task        # Linux/macOS
where task        # Windows
```

### Permission denied
```bash
# Make scripts executable (Linux/macOS)
chmod +x ./build/nuke/build.sh
```

### Unity path not found
```bash
# Specify Unity path
task nuke -- BuildUnity --unity-path "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"
```

## Documentation

- Main README: `../../README.md`
- Complete Guide: [README.md](README.md)
- Getting Started: [GETTING_STARTED.md](GETTING_STARTED.md)
- Integration Details: [INTEGRATION.md](INTEGRATION.md)
- Unity Component: `../../build/nuke/build/Components/README.md`
- Task Website: https://taskfile.dev

## Tips

1. **Tab Completion**: Enable shell completion for Task
   ```bash
   # Bash
   task --completion bash > /etc/bash_completion.d/task
   
   # Zsh
   task --completion zsh > /usr/local/share/zsh/site-functions/_task
   
   # PowerShell
   task --completion powershell > $PROFILE
   ```

2. **Watch Mode**: Use with `entr` or `watchexec` for continuous builds
   ```bash
   ls **/*.cs | entr task build
   ```

3. **IDE Integration**: Most IDEs support Task natively or via plugins

4. **Custom Tasks**: Edit `Taskfile.yml` to add project-specific tasks

5. **Parallel Execution**: Task automatically runs independent tasks in parallel

---

**Need help?** Run `task --help` or `task <task-name> --help`
