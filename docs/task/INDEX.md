# Task Runner Documentation

This directory contains all documentation related to the Task runner integration for the Sango Card project.

## 📚 Documentation Index

### Getting Started
- **[Getting Started Guide](GETTING_STARTED.md)** - Step-by-step guide for new users
  - Installation instructions for all platforms
  - First steps and verification
  - Common workflows
  - IDE integration
  - Troubleshooting

### Daily Use
- **[Quick Reference](QUICK_REFERENCE.md)** - Command cheat sheet
  - Installation one-liners
  - Essential commands table
  - Common workflows
  - Tips and tricks
  - Troubleshooting quick fixes

### Complete Guide
- **[Task Guide (README)](README.md)** - Comprehensive documentation
  - Full installation guide
  - Complete task reference
  - Advanced usage
  - IDE integration
  - Migration guide
  - Detailed troubleshooting

### Technical Details
- **[Integration Summary](INTEGRATION.md)** - Technical overview
  - What was added
  - Architecture explanation
  - File structure
  - Customization guide
  - Migration path

### Reference
- **[Alternatives to Make](ALTERNATIVES.md)** - Why Task instead of Make/Just/etc.
  - Comparison of build tools
  - Migration guides
  - Shell alias examples
  - Recommendations

## 🚀 Quick Links

### For New Users
1. Start with [Getting Started Guide](GETTING_STARTED.md)
2. Install Task using the scripts in `../../scripts/`
3. Run `task --list` to see available commands
4. Check [Quick Reference](QUICK_REFERENCE.md) for common commands

### For Experienced Users
- [Quick Reference](QUICK_REFERENCE.md) - Fast command lookup
- [Complete Guide](README.md) - Deep dive into features

### For Integration/DevOps
- [Integration Summary](INTEGRATION.md) - Architecture and technical details

## 📁 Related Files

### Configuration
- `../../Taskfile.yml` - Main task configuration
- `../../.taskfiles/unity.yml` - Unity-specific tasks

### Installation Scripts
- `../../scripts/install-task.ps1` - Windows installer
- `../../scripts/install-task.sh` - Linux/macOS installer
- `../../scripts/setup-completion.sh` - Shell completion

### IDE Integration
- `../../.vscode/tasks.json` - VS Code tasks

### CI/CD
- `../../.github/workflows/task-ci.yml` - GitHub Actions example

## 🎯 Common Tasks Quick Reference

| Command | Description |
|---------|-------------|
| `task --list` | Show all available tasks |
| `task setup` | Setup development environment |
| `task build` | Build the project |
| `task test` | Run tests |
| `task clean` | Clean build artifacts |
| `task dev` | Full development workflow |
| `task ci` | Complete CI pipeline |

## 📞 Support

- **Task Website**: https://taskfile.dev
- **Task GitHub**: https://github.com/go-task/task
- **Project README**: ../../README.md

## 🗂️ Documentation Structure

```
docs/task/
├── INDEX.md              ← You are here
├── GETTING_STARTED.md    ← New user guide
├── QUICK_REFERENCE.md    ← Command cheat sheet
├── README.md             ← Complete guide
├── INTEGRATION.md        ← Technical details
└── ALTERNATIVES.md       ← Why Task instead of Make
```

---

**Need help?** Start with [Getting Started Guide](GETTING_STARTED.md) or check the [Quick Reference](QUICK_REFERENCE.md).
