---
doc_id: DOC-2025-00113
title: Build Preparation Workflow
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [build-preparation-workflow]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00065
title: Build Preparation Workflow
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- build
- workflow
- unity
- preparation
summary: Complete guide to the build preparation workflow for Unity client builds.

---

# Build Preparation Workflow

**Version:** 1.0.0  
**Last Updated:** 2025-10-17  
**Rule Reference:** R-BLD-060

## Overview

This document describes the complete build preparation workflow for the Sango Card Unity client. The workflow ensures that `projects/client` remains read-only outside of build operations, respecting its status as a standalone Git repository.

## Core Principles

### R-BLD-060 Compliance

**Rule:** `projects/client` is a standalone Git repository and MUST remain read-only except during build execution.

**Implications:**

- No manual modifications to `projects/client` outside builds
- All preparation must use a two-phase approach
- Git reset required before any injection
- Automatic cleanup after builds

## Two-Phase Workflow

The build preparation uses a two-phase approach to maintain R-BLD-060 compliance:

### Phase 1: Cache Population (Safe Anytime)

**Purpose:** Gather all required references into the build cache without touching the client project.

**Location:** `build/preparation/cache/`

**Source:** `projects/code-quality`

**Command:**

```bash
# Using Task (recommended - R-BLD-010)
task build:prepare:cache

# Or using the tool directly
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool
dotnet run -- cache populate --source projects/code-quality
```

**What Happens:**

1. Tool reads configuration from `build/preparation/configs/default.json`
2. Scans `projects/code-quality` for required packages
3. Copies packages to `build/preparation/cache/`
4. Updates manifest files in cache
5. **Does NOT touch `projects/client`**

**When to Run:**

- After updating dependencies in `projects/code-quality`
- Before starting a build
- Can be run multiple times safely
- Can be run outside of build context

### Phase 2: Client Injection (Build-Time Only)

**Purpose:** Inject cached references into the client project during build execution.

**Target:** `projects/client/`

**Source:** `build/preparation/cache/`

**Command:**

```bash
# Using Task (recommended - R-BLD-010, includes git reset)
task build:prepare:client

# Or using the tool directly (requires --target validation)
cd packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool
dotnet run -- prepare inject --config build/preparation/configs/default.json --target projects/client/
```

**What Happens:**

1. **Git reset:** `git reset --hard` on `projects/client/` (Nuke only)
2. Tool validates target path is `projects/client/`
3. Reads from `build/preparation/cache/`
4. Injects packages into `projects/client/Packages/`
5. Updates `projects/client/Packages/manifest.json`
6. Applies scripting definition symbols
7. Applies code patches (if configured)

**When to Run:**

- **ONLY during build execution via Task**
- Never manually outside of builds
- Always after Phase 1 (cache populate)

**Safety Checks:**

- Target path MUST be `projects/client/` (validated)
- Cache MUST exist (validated)
- Git reset MUST complete successfully (Nuke)

## Configuration System

### Configuration File Location

**Default:** `build/preparation/configs/default.json`

**Structure:**

```json
{
  "version": "1.0",
  "packages": [
    {
      "name": "com.example.package",
      "source": "cache",
      "destination": "Packages/com.example.package"
    }
  ],
  "scriptingDefines": [
    "CUSTOM_DEFINE",
    "FEATURE_FLAG"
  ],
  "codePatches": [
    {
      "type": "csharp",
      "file": "Assets/Scripts/Example.cs",
      "operations": [
        {
          "type": "remove_using",
          "namespace": "Unity.VisualScripting"
        }
      ]
    }
  ]
}
```

### Configuration Management

**Create new config:**

```bash
dotnet run -- config create --output build/preparation/configs/custom.json
```

**Validate config:**

```bash
dotnet run -- config validate --file build/preparation/configs/default.json --level Full
```

**Edit config:**

- Use TUI mode: `dotnet run -- tui`
- Or edit JSON directly (with validation)

## Build Tool Reference

### Tool Location

**Path:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/`

**Project:** `SangoCard.Build.Tool.csproj`

### CLI Commands

#### Cache Commands

```bash
# Populate cache from source
cache populate --source <path> [--config <path>]

# List cached items
cache list

# Clean cache
cache clean
```

#### Prepare Commands

```bash
# Inject from cache to client (Phase 2)
prepare inject --config <path> --target <path>

# Dry-run (preview changes without applying)
prepare inject --config <path> --target <path> --dry-run

# Restore client to clean state
prepare restore
```

#### Config Commands

```bash
# Create new configuration
config create --output <path>

# Validate configuration
config validate --file <path> [--level <Basic|Standard|Full|Strict>]
```

### TUI Mode

**Launch:**

```bash
dotnet run -- tui
```

**Features:**

- F2: Config Editor
- F3: Cache Management
- F4: Validation
- F5: Preparation Execution
- F10: Exit

## Task Integration (R-BLD-010)

### Available Tasks

#### build:prepare:cache

**Description:** Populate preparation cache (safe anytime)

**Command:** `task build:prepare:cache`

**What it does:**

- Runs `cache populate --source projects/code-quality`
- Safe to run anytime
- No client modification

#### build:prepare:client

**Description:** Inject preparation into client (build-time only)

**Command:** `task build:prepare:client`

**Dependencies:** build:prepare:cache

**What it does:**

1. Runs `git reset --hard` on `projects/client/`
2. Runs `prepare inject --target projects/client/`
3. Validates all operations

**⚠️ WARNING:** Only run during builds!

#### build:prepare:restore

**Description:** Restore client to clean state

**Command:** `task build:prepare:restore`

**What it does:**

- Runs `git reset --hard` on `projects/client/`
- Removes all injected files

#### build:unity:prepared

**Description:** Full build workflow with preparation

**Command:** `task build:unity:prepared`

**Dependencies:** build:prepare:cache, build:prepare:client

**What it does:**

1. Prepares cache
2. Injects into client
3. Builds Unity project
4. Restores client (cleanup)

#### build:prepare:validate

**Description:** Validate preparation configuration

**Command:** `task build:prepare:validate`

**What it does:**

- Validates config file
- Checks cache existence
- Verifies paths

#### build:prepare:dry-run

**Description:** Preview preparation changes

**Command:** `task build:prepare:dry-run`

**What it does:**

- Shows what would be injected
- No actual modifications
- Useful for debugging

### Implementation Details

**Task Runner:** `Taskfile.yml` (R-BLD-010)

**Nuke Backend:** `build/nuke/build/Build.Preparation.cs`

**Key Features:**

- Task runner abstraction layer
- Partial class pattern (R-CODE-090)
- Git integration for reset/restore
- Proper error handling
- Progress reporting

## Reference Management

### Package Sources

**Primary Source:** `projects/code-quality`

**Why:** This project contains all Microsoft.Extensions.* packages and other shared dependencies that need to be injected into the client.

**Cache Location:** `build/preparation/cache/`

**Client Destination:** `projects/client/Packages/`

### Package Detection

The tool automatically detects packages in the source project by:

1. Scanning `Packages/manifest.json`
2. Identifying Microsoft.Extensions.* packages
3. Copying package folders to cache
4. Updating cache manifest

### Manifest Management

**Source Manifest:** `projects/code-quality/Packages/manifest.json`

**Cache Manifest:** `build/preparation/cache/manifest.json`

**Client Manifest:** `projects/client/Packages/manifest.json`

**Update Flow:**

1. Read source manifest
2. Extract required dependencies
3. Write to cache manifest
4. Inject into client manifest (Phase 2 only)

## Scripting Definition Symbols

### Purpose

Apply custom scripting defines to the Unity client project to enable/disable features.

### Configuration

```json
{
  "scriptingDefines": [
    "CUSTOM_FEATURE",
    "DEBUG_MODE",
    "EXPERIMENTAL_API"
  ]
}
```

### Application

**When:** During Phase 2 (inject)

**Where:** `projects/client/ProjectSettings/ProjectSettings.asset`

**How:** Unity YAML patching via `UnityAssetPatcher`

### Validation

The tool validates that:

- Symbols are valid C# identifiers
- No duplicate symbols
- No conflicts with Unity built-in defines

## Code Patching

### Supported Patch Types

#### 1. C# Patching (Roslyn-based)

**Patcher:** `CSharpPatcher`

**Operations:**

- `remove_using` - Remove using statements
- `replace` - Replace expressions syntax-aware
- `replace_block` - Replace code blocks
- `remove_block` - Remove code blocks

**Example:**

```json
{
  "type": "csharp",
  "file": "Assets/Scripts/Example.cs",
  "operations": [
    {
      "type": "remove_using",
      "namespace": "Unity.VisualScripting"
    }
  ]
}
```

#### 2. JSON Patching

**Patcher:** `JsonPatcher`

**Operations:**

- Path-based modifications
- Add/remove/update JSON properties

**Example:**

```json
{
  "type": "json",
  "file": "ProjectSettings/ProjectSettings.json",
  "operations": [
    {
      "type": "update",
      "path": "$.playerSettings.companyName",
      "value": "Sango Card"
    }
  ]
}
```

#### 3. Unity YAML Patching

**Patcher:** `UnityAssetPatcher`

**Operations:**

- Modify Unity asset files
- Update project settings
- Modify scene files

**Example:**

```json
{
  "type": "unity_yaml",
  "file": "ProjectSettings/ProjectSettings.asset",
  "operations": [
    {
      "type": "update",
      "path": "PlayerSettings.scriptingDefineSymbols",
      "value": "CUSTOM_DEFINE"
    }
  ]
}
```

#### 4. Text Patching (Regex-based)

**Patcher:** `TextPatcher`

**Operations:**

- Regex find/replace
- Line-based modifications

**Example:**

```json
{
  "type": "text",
  "file": "Assets/Resources/config.txt",
  "operations": [
    {
      "type": "replace",
      "pattern": "DEBUG=false",
      "replacement": "DEBUG=true"
    }
  ]
}
```

### Patch Application

**When:** During Phase 2 (inject)

**Order:**

1. Package injection
2. Manifest updates
3. Scripting defines
4. Code patches (in config order)

**Validation:**

- Syntax validation before applying
- Rollback on error
- Detailed error messages

## Complete Build Flow

### Standard Build Sequence

```bash
# 1. Prepare cache (can be done ahead of time)
task build:prepare:cache

# 2. Full build with preparation
task build:unity:prepared
```

**What happens internally:**

```
BuildUnityWithPreparation
├─ PrepareCache (if not already done)
│  └─ cache populate --source projects/code-quality
├─ PrepareClient
│  ├─ git reset --hard (projects/client)
│  └─ prepare inject --target projects/client/
├─ BuildUnity
│  └─ Unity build process
└─ RestoreClient
   └─ git reset --hard (projects/client)
```

### Development Workflow

**For developers working on build preparation:**

```bash
# 1. Update code-quality project with new dependencies
cd projects/code-quality
# ... make changes ...

# 2. Populate cache
task build:prepare:cache

# 3. Test injection (dry-run)
task build:prepare:dry-run

# 4. Validate configuration
task build:prepare:validate

# 5. Run full build
task build:unity:prepared
```

### CI/CD Workflow

**For automated builds:**

```yaml
steps:
  - name: Validate Preparation
    run: task build:prepare:validate

  - name: Build Unity
    run: task build:unity:prepared
```

## Troubleshooting

### Error: "Target must be 'projects/client/'"

**Cause:** Invalid target path provided to `prepare inject`

**Solution:** Always use `projects/client/` as target

```bash
# ✅ Correct
prepare inject --config <path> --target projects/client/

# ❌ Wrong
prepare inject --config <path> --target projects/other/
```

### Error: "Cache files not found"

**Cause:** Phase 2 run before Phase 1

**Solution:** Run cache populate first

```bash
task build:prepare:cache
task build:prepare:client
```

### Warning: "prepare run is deprecated"

**Cause:** Using old command

**Solution:** Migrate to two-phase workflow

```bash
# OLD (deprecated)
prepare run --config <path>

# NEW (recommended)
cache populate --source projects/code-quality
prepare inject --config <path> --target projects/client/
```

### Build Errors After Injection

**Check:**

1. .NET 8.0 SDK installed
2. All NuGet packages restored
3. Git root detected correctly
4. Unity project at `projects/client/`
5. Cache populated successfully

**Debug:**

```bash
# Validate configuration
task build:prepare:validate

# Dry-run to see what would be injected
task build:prepare:dry-run

# Check cache contents
dotnet run -- cache list
```

## Best Practices

### DO

✅ Always use Task runner for builds (R-BLD-010)
✅ Run `task build:prepare:cache` before builds
✅ Use dry-run to preview changes
✅ Validate configuration regularly
✅ Keep cache up-to-date with code-quality
✅ Use TUI for interactive config editing
✅ Document custom configurations

### DON'T

❌ Manually modify `projects/client` outside builds
❌ Run `task build:prepare:client` outside build workflow
❌ Skip cache population
❌ Ignore validation errors
❌ Edit client manifest directly
❌ Commit injected files to client repo
❌ Use deprecated `prepare run` command
❌ Call `nuke` directly (use `task` instead)

## Migration from Old Workflow

If you were using the old single-phase workflow:

**Old:**

```bash
tool prepare run --config <path>
```

**New:**

```bash
# Step 1: Populate cache
tool cache populate --source projects/code-quality

# Step 2: Inject (during build only)
tool prepare inject --config <path> --target projects/client/
```

**Or use Task (recommended - R-BLD-010):**

```bash
task build:unity:prepared
```

## Related Documentation

- **Tool README:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/README.md`
- **Migration Guide:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/MIGRATION-GUIDE.md`
- **Nuke Component README:** `build/nuke/build/Components/README.md`
- **Spec:** `.specify/specs/build-preparation-tool.md`
- **Amendment:** `.specify/specs/build-preparation-tool-amendment-001.md`

## Rule References

- **R-BLD-010:** Use Task runner for all build operations
- **R-BLD-060:** Client read-only outside builds
- **R-SPEC-010:** Spec-kit workflow
- **R-CODE-090:** Partial class pattern
- **R-CODE-110:** Cross-platform paths
- **R-GIT-020:** No secrets in commits
- **R-SEC-020:** Security best practices

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-17 | Initial documentation |

---

**For questions or issues, refer to the tool's README or consult the build system team.**
