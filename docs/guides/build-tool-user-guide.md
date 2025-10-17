---
doc_id: DOC-2025-00200
title: Build Preparation Tool User Guide
doc_type: guide
status: active
canonical: true
created: 2025-10-17
updated: 2025-10-17
tags: [build-tool, tui, cli, unity, user-guide]
summary: Comprehensive user guide for the Sango Card Build Preparation Tool's CLI and TUI interfaces
---

# Build Preparation Tool User Guide

Complete guide for using the Sango Card Build Preparation Tool's CLI and TUI interfaces.

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Understanding the Two-Phase System](#understanding-the-two-phase-system)
- [CLI Usage](#cli-usage)
- [TUI Usage](#tui-usage)
- [Configuration Files](#configuration-files)
- [Common Workflows](#common-workflows)
- [Troubleshooting](#troubleshooting)
- [Advanced Usage](#advanced-usage)

## Overview

The Build Preparation Tool is a .NET CLI/TUI application that manages Unity build preparation in two phases:

1. **Phase 1: Preparation Sources** - Collect packages/assemblies from external locations into a local cache
2. **Phase 2: Build Injections** - Inject cached items into the Unity client project

### Key Features

- ✅ **Dual Interface** - Command-line (CLI) and Terminal UI (TUI) modes
- ✅ **Two-Phase System** - Separation of source collection and build injection
- ✅ **Git-Aware** - Automatic git root detection for path resolution
- ✅ **Rich TUI** - Full CRUD management screens with Terminal.Gui v2
- ✅ **Validation** - Built-in config validation before execution
- ✅ **Progress Tracking** - Real-time progress for long operations

### Architecture

```text
External Sources → [Phase 1: Preparation] → Local Cache → [Phase 2: Injection] → Unity Client
```

## Installation

### Prerequisites

- .NET 8.0 SDK or later
- Unity 2022.3 LTS or compatible version
- Windows, macOS, or Linux

### Build the Tool

```bash
# From repository root
task build

# Or using Nuke directly
./build.ps1  # Windows
./build.sh   # Linux/macOS
```

### Locate the Tool

After building, the tool is located at:

```text
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/bin/Debug/net8.0/win-x64/SangoCard.Build.Tool.dll
```

### Create an Alias (Optional)

**PowerShell** (add to your profile):

```powershell
function buildprep {
    dotnet "D:\path\to\SangoCard.Build.Tool.dll" $args
}
```

**Bash/Zsh** (add to `.bashrc` or `.zshrc`):

```bash
alias buildprep='dotnet /path/to/SangoCard.Build.Tool.dll'
```

## Quick Start

### Option 1: TUI Mode (Recommended for Beginners)

```bash
dotnet path/to/SangoCard.Build.Tool.dll tui
```

The TUI provides an interactive interface with:
- Visual menus and navigation
- Step-by-step wizards
- Full CRUD screens for managing configs
- Built-in help and documentation

### Option 2: CLI Mode (For Automation)

```bash
# View available commands
dotnet path/to/SangoCard.Build.Tool.dll --help

# Run preparation
dotnet path/to/SangoCard.Build.Tool.dll prepare run --manifest path/to/manifest.json

# Validate a config
dotnet path/to/SangoCard.Build.Tool.dll validate --config path/to/config.json
```

## Understanding the Two-Phase System

### Phase 1: Preparation Sources (Source → Cache)

**Purpose**: Collect packages and assemblies from external locations into a centralized cache.

**Configuration**: Uses `PreparationManifest.json` files

**What it does**:
- Copies Unity packages (`.tgz` files) to cache
- Copies DLL assemblies to cache
- Copies asset files/directories to cache

**Example manifest**:

```json
{
  "version": "1.0.0",
  "packages": [
    {
      "name": "com.unity.addressables",
      "sourcePath": "D:/Unity/Packages/com.unity.addressables",
      "targetFileName": "com.unity.addressables-1.21.0.tgz"
    }
  ],
  "assemblies": [
    {
      "name": "GameplayCore",
      "sourcePath": "D:/Projects/Assemblies/GameplayCore.dll"
    }
  ],
  "assets": [
    {
      "name": "CharacterPrefabs",
      "sourcePath": "D:/Projects/Assets/Characters"
    }
  ]
}
```

### Phase 2: Build Injections (Cache → Client)

**Purpose**: Inject cached items into the Unity client project with specific operations.

**Configuration**: Uses `PreparationConfig.json` files

**What it does**:
- Copies packages from cache to client `Packages/` directory
- Copies assemblies from cache to specified client paths
- Performs asset operations (Copy/Move/Delete) in client project

**Example config**:

```json
{
  "version": "1.0.0",
  "clientPath": "D:/Projects/SangoCard/Client",
  "packages": [
    {
      "sourceFileName": "com.unity.addressables-1.21.0.tgz",
      "targetPath": "Packages"
    }
  ],
  "assemblies": [
    {
      "sourceFileName": "GameplayCore.dll",
      "targetPath": "Assets/Plugins/Gameplay"
    }
  ],
  "assets": [
    {
      "sourcePattern": "Characters/**/*",
      "targetPath": "Assets/Game/Characters",
      "operation": "Copy"
    }
  ]
}
```

## CLI Usage

### Global Options

```bash
--version       Show version information
-?, -h, --help  Show help and usage information
```

### Available Commands

#### 1. Config Management

```bash
# List configs
dotnet tool.dll config list

# Show config details
dotnet tool.dll config show --path path/to/config.json

# Create new config
dotnet tool.dll config create --type manifest --output new-manifest.json
dotnet tool.dll config create --type build --output new-config.json
```

#### 2. Cache Management

```bash
# List cache contents
dotnet tool.dll cache list

# Show cache statistics
dotnet tool.dll cache info

# Clear cache
dotnet tool.dll cache clear

# Verify cache integrity
dotnet tool.dll cache verify
```

#### 3. Validation

```bash
# Validate manifest
dotnet tool.dll validate --manifest path/to/manifest.json

# Validate build config
dotnet tool.dll validate --config path/to/config.json

# Validate both
dotnet tool.dll validate --manifest manifest.json --config config.json
```

#### 4. Preparation (Phase 1)

```bash
# Run preparation
dotnet tool.dll prepare run --manifest path/to/manifest.json

# Dry run (preview only)
dotnet tool.dll prepare run --manifest manifest.json --dry-run

# With verbose output
dotnet tool.dll prepare run --manifest manifest.json --verbose
```

#### 5. TUI Mode

```bash
# Launch interactive TUI
dotnet tool.dll tui

# Or use --mode flag
dotnet tool.dll --mode tui
```

### CLI Examples

**Example 1: Complete Phase 1 Setup**

```bash
# 1. Validate manifest
dotnet tool.dll validate --manifest sources.json

# 2. Preview changes (dry run)
dotnet tool.dll prepare run --manifest sources.json --dry-run

# 3. Execute preparation
dotnet tool.dll prepare run --manifest sources.json

# 4. Verify cache
dotnet tool.dll cache verify
```

**Example 2: Automated Build Script**

```bash
#!/bin/bash
set -e

TOOL="dotnet path/to/SangoCard.Build.Tool.dll"

echo "Step 1: Validating configurations..."
$TOOL validate --manifest prep-manifest.json --config build-config.json

echo "Step 2: Running Phase 1 (Preparation)..."
$TOOL prepare run --manifest prep-manifest.json

echo "Step 3: Verifying cache..."
$TOOL cache verify

echo "Step 4: Running Phase 2 (Injection)..."
# Phase 2 commands would go here (not yet implemented in CLI)

echo "Build preparation complete!"
```

## TUI Usage

### Launching the TUI

```bash
dotnet path/to/SangoCard.Build.Tool.dll tui
```

### Navigation

- **Arrow Keys** - Move between menu items and options
- **Enter** - Select/Confirm
- **Escape** - Go back/Cancel
- **Tab** - Move between fields in forms
- **F1-F10** - Function key shortcuts (displayed in status bar)

### Main Menu

The TUI welcome screen shows:

```text
┌─ Sango Card Build Preparation Tool ─┐
│                                      │
│  F1 - Help                           │
│  F2 - Config Type Selection          │
│  F3 - Config Editor                  │
│  F4 - Cache Manager                  │
│  F5 - Manual Sources                 │
│  F6 - Validation                     │
│  F7 - Preparation                    │
│  F10 - Quit                          │
│                                      │
└──────────────────────────────────────┘
```

### Menu Structure

```text
File
├── Quit (F10)

View
├── Config Type Selection (F2)
├── Config Editor (F3)
├── Cache Manager (F4)
├── Manual Sources (F5)
├── Validation (F6)
└── Preparation (F7)

Manage
├── Preparation Sources  ← Phase 1 CRUD
└── Build Injections     ← Phase 2 CRUD

Help
├── About
└── Documentation
```

### Key Features

#### 1. Config Type Selection (F2)

Learn about the two config types:
- **PreparationManifest** - Phase 1 (source → cache)
- **PreparationConfig** - Phase 2 (cache → client)

Interactive guide explaining:
- Purpose of each type
- When to use each
- Configuration structure
- Examples

#### 2. Preparation Sources Management

**Full CRUD interface for Phase 1 manifests**:

- **Load Manifest** - Browse and load existing manifest files
- **Create New** - Start with empty manifest
- **Add Items** - Add packages, assemblies, or assets
- **Edit Items** - Modify existing entries
- **Remove Items** - Delete entries with confirmation
- **Save Manifest** - Save changes to file
- **Preview** - See all manifest contents

**Add Package Form**:
```text
┌─ Add Package ─────────────┐
│ Name: ___________________│
│ Source Path: ____________│
│ Target File: ____________│
│                           │
│  [Save]  [Cancel]        │
└───────────────────────────┘
```

#### 3. Build Injections Management

**Full CRUD interface for Phase 2 configs**:

- **Multi-Section** - Switch between Packages/Assemblies/Assets
- **Load Config** - Browse and load existing configs
- **Create New** - Start with empty config
- **Section Switcher** - Toggle between item types
- **Add/Edit/Remove** - Per section CRUD operations
- **Save Config** - Save all sections to file
- **Preview** - See all config contents

**Section Switcher**:
```text
[ Packages ] [ Assemblies ] [ Assets ]
     ↑            ↓            ↓
  Active     Available    Available
```

**Add Asset Form** (with operation selector):
```text
┌─ Add Asset ──────────────────┐
│ Source Pattern: ____________│
│ Target Path: _______________│
│ Operation:                   │
│   ( ) Copy                   │
│   ( ) Move                   │
│   (•) Delete                 │
│                              │
│   [Save]  [Cancel]          │
└──────────────────────────────┘
```

#### 4. Manual Sources (F5)

Quick add interface for single packages or assemblies without full manifest editing.

#### 5. Cache Manager (F4)

View and manage the preparation cache:
- List all cached items
- View statistics
- Clear cache
- Verify integrity

#### 6. Validation (F6)

Validate configurations before execution:
- Select manifest and/or config files
- Run validation checks
- View detailed error messages
- See validation results

#### 7. Preparation (F7)

Execute Phase 1 preparation:
- Select manifest
- Preview operations
- Confirm execution
- Watch progress in real-time
- View completion summary

### TUI Workflows

**Workflow 1: Complete Phase 1 Setup**

1. Press **F2** to learn about config types
2. Navigate to **Manage → Preparation Sources**
3. Select **Load Manifest** (or Create New)
4. Browse to your manifest file
5. Review loaded items in preview
6. Make edits if needed (Add/Edit/Remove)
7. **Save Manifest**
8. Press **F7** for Preparation
9. Confirm and execute
10. Review completion status

**Workflow 2: Complete Phase 2 Setup**

1. Ensure Phase 1 complete (cache populated)
2. Navigate to **Manage → Build Injections**
3. Select **Load Config** (or Create New)
4. Browse to your config file
5. Switch between sections (Packages/Assemblies/Assets)
6. Review each section's items
7. Make edits if needed per section
8. **Save Config**
9. (Phase 2 execution via TUI - coming soon)

**Workflow 3: Quick Package Addition**

1. Press **F5** for Manual Sources
2. Select **Add Package**
3. Fill in package details
4. Save
5. Item added to manifest
6. Press **F7** to prepare

## Configuration Files

### PreparationManifest.json Structure

```json
{
  "version": "1.0.0",
  "packages": [
    {
      "name": "string",              // Package identifier
      "sourcePath": "string",         // Absolute path to package source
      "targetFileName": "string"      // .tgz filename in cache
    }
  ],
  "assemblies": [
    {
      "name": "string",              // Assembly identifier
      "sourcePath": "string"          // Absolute path to .dll file
    }
  ],
  "assets": [
    {
      "name": "string",              // Asset identifier
      "sourcePath": "string"          // Absolute path to asset file/directory
    }
  ]
}
```

### PreparationConfig.json Structure

```json
{
  "version": "1.0.0",
  "clientPath": "string",            // Absolute path to Unity client project
  "packages": [
    {
      "sourceFileName": "string",     // Filename in cache (must match Phase 1)
      "targetPath": "string"          // Relative path in client (usually "Packages")
    }
  ],
  "assemblies": [
    {
      "sourceFileName": "string",     // Filename in cache (must match Phase 1)
      "targetPath": "string"          // Relative path in client (e.g., "Assets/Plugins")
    }
  ],
  "assets": [
    {
      "sourcePattern": "string",      // Glob pattern (e.g., "**/*.prefab")
      "targetPath": "string",         // Relative path in client
      "operation": "Copy|Move|Delete" // Operation type
    }
  ]
}
```

### Asset Operations

- **Copy** - Copy files from cache to client (cache unchanged)
- **Move** - Move files from cache to client (removes from cache)
- **Delete** - Delete files matching pattern from client (cache unchanged)

### Glob Patterns

Supported in `sourcePattern` for assets:

- `*` - Match any characters except directory separator
- `**` - Match any characters including directory separator
- `?` - Match single character
- `[abc]` - Match character set
- `{a,b}` - Match alternatives

Examples:
- `**/*.cs` - All C# files recursively
- `Prefabs/**/*` - All files under Prefabs directory
- `*.{dll,so,dylib}` - Native libraries with any extension

## Common Workflows

### Workflow: Setting Up a New Project

**Step 1: Create Preparation Manifest**

```bash
# Launch TUI
dotnet tool.dll tui

# Navigate: Manage → Preparation Sources → Create New
# Add your packages, assemblies, and assets
# Save as: project-sources.json
```

**Step 2: Run Phase 1 Preparation**

```bash
# CLI approach
dotnet tool.dll prepare run --manifest project-sources.json

# Or TUI approach
# Press F7, select manifest, confirm
```

**Step 3: Create Build Config**

```bash
# TUI: Manage → Build Injections → Create New
# Add injection rules for cached items
# Save as: project-build.json
```

**Step 4: Execute Phase 2** (when available)

```bash
# Future CLI command
dotnet tool.dll inject run --config project-build.json
```

### Workflow: Updating Dependencies

```bash
# 1. Edit manifest to update source paths or add new items
# TUI: Manage → Preparation Sources → Load → Edit → Save

# 2. Re-run preparation
dotnet tool.dll prepare run --manifest updated-sources.json

# 3. Update build config if needed
# TUI: Manage → Build Injections → Load → Edit → Save

# 4. Re-run injection
# (when Phase 2 CLI available)
```

### Workflow: Validating Before CI/CD

```bash
#!/bin/bash
# pre-build-validate.sh

TOOL="dotnet path/to/SangoCard.Build.Tool.dll"

echo "Validating configurations..."
if ! $TOOL validate --manifest sources.json --config build.json; then
    echo "❌ Validation failed!"
    exit 1
fi

echo "✅ All validations passed!"
exit 0
```

## Troubleshooting

### Common Issues

#### Issue: "Tool DLL not found"

**Solution**: Ensure you've built the project first:

```bash
task build
# Or
./build.ps1
```

#### Issue: "Invalid manifest/config"

**Cause**: JSON syntax errors or missing required fields

**Solution**: Use validation command:

```bash
dotnet tool.dll validate --manifest your-file.json
```

Or use TUI Validation (F6) for detailed error messages.

#### Issue: "Source path not found"

**Cause**: Absolute paths in manifest don't exist on current machine

**Solution**:
- Update paths in manifest
- Use TUI: Manage → Preparation Sources → Edit items
- Ensure source files exist before running preparation

#### Issue: "Permission denied"

**Cause**: Insufficient permissions for file operations

**Solution**:
- Run with elevated permissions (Windows: Run as Administrator)
- Check file/directory permissions
- Ensure cache directory is writable

#### Issue: "TUI not rendering correctly"

**Cause**: Terminal doesn't support ANSI colors or required features

**Solution**:
- Use modern terminal (Windows Terminal, iTerm2, etc.)
- Check terminal settings for ANSI support
- Try different terminal emulator

### Debug Mode

Enable verbose logging:

```bash
# CLI
dotnet tool.dll prepare run --manifest file.json --verbose

# Or set environment variable
set DOTNET_ENVIRONMENT=Development  # Windows
export DOTNET_ENVIRONMENT=Development  # Linux/macOS
```

### Getting Help

1. **Built-in Help**: Press **F1** in TUI or run `--help` in CLI
2. **Documentation**: See `docs/guides/` directory
3. **Specification**: Read `docs/specs/build-preparation-tool.md`
4. **Issues**: Check GitHub issues or create new one

## Advanced Usage

### Custom Cache Location

Set cache directory via environment variable:

```bash
export BUILD_PREP_CACHE_DIR=/custom/cache/path
dotnet tool.dll prepare run --manifest sources.json
```

### Git Root Resolution

The tool automatically detects git repository root for path resolution:

```text
Repository Root: D:/Projects/SangoCard/
Relative paths resolved from: Repository Root
Absolute paths: Used as-is
```

### Scripting with CLI

**PowerShell Script Example**:

```powershell
$tool = "dotnet path/to/SangoCard.Build.Tool.dll"

# Validate
& $tool validate --manifest sources.json
if ($LASTEXITCODE -ne 0) {
    Write-Error "Validation failed"
    exit 1
}

# Prepare
& $tool prepare run --manifest sources.json --verbose

# Check cache
& $tool cache info
```

**Bash Script Example**:

```bash
#!/bin/bash
set -euo pipefail

TOOL="dotnet path/to/SangoCard.Build.Tool.dll"

validate() {
    echo "Validating $1..."
    $TOOL validate --manifest "$1" || {
        echo "Validation failed!"
        return 1
    }
}

prepare() {
    echo "Running preparation with $1..."
    $TOOL prepare run --manifest "$1" --verbose
}

validate "sources.json"
prepare "sources.json"

echo "✅ Preparation complete!"
```

### Integration with Build Systems

**Nuke Build Example**:

```csharp
Target PrepareUnityDependencies => _ => _
    .Description("Prepare Unity build dependencies")
    .Executes(() =>
    {
        var toolPath = RootDirectory / "path" / "to" / "SangoCard.Build.Tool.dll";
        var manifest = RootDirectory / "config" / "sources.json";

        DotNetRun(s => s
            .SetProjectFile(toolPath)
            .SetApplicationArguments($"prepare run --manifest {manifest}"));
    });
```

**GitHub Actions Example**:

```yaml
name: Prepare Unity Build
on: [push]

jobs:
  prepare:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build Tool
        run: task build

      - name: Validate Config
        run: |
          dotnet path/to/tool.dll validate \
            --manifest config/sources.json

      - name: Run Preparation
        run: |
          dotnet path/to/tool.dll prepare run \
            --manifest config/sources.json

      - name: Verify Cache
        run: dotnet path/to/tool.dll cache verify
```

## Next Steps

- **Learn Architecture**: Read [Build Preparation Tool Specification](../specs/build-preparation-tool.md)
- **Run Tests**: See [Integration Testing Guide](./build-tool-integration-testing-checklist.md)
- **Contribute**: Check CONTRIBUTING.md
- **API Reference**: See [API Documentation](./build-tool-api-reference.md)

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-17  
**Maintainer**: Sango Card Build Team
