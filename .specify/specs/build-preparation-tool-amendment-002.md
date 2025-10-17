---
id: SPEC-BLD-PREP-002
title: "Build Preparation Tool - Two-Config Architecture & Manual Source Control"
status: draft
created: 2025-10-17
updated: 2025-10-17
author: AI Assistant
relates_to:
  - build-preparation-tool.md
  - build-preparation-tool-amendment-001.md
tags:
  - build
  - preparation
  - enhancement
  - cli
  - tui
  - architecture
---

# Build Preparation Tool Amendment 002: Two-Config Architecture & Manual Source Control

## Overview

Major architectural enhancement introducing a two-config system that cleanly separates preparation concerns from build concerns, plus manual source control for scattered dependencies.

## Problem Statement

### Problem 1: Mixed Concerns in Single Config

The current config mixes two distinct phases:
- **Preparation Phase**: What to collect into cache (source → cache)
- **Build Phase**: What to inject into client (cache → client, code patches, symbols)

This creates confusion about:
- Which paths are preparation vs build concerns
- When each operation happens
- How to reuse cache with different build configs

### Problem 2: Scattered Source Locations

The existing `cache populate` command only supports scanning a single source directory:

```bash
dotnet run -- cache populate --source projects/code-quality
```

**Real-world scenario:**
- Unity packages in: `projects/code-quality/Library/PackageCache/org.nuget.*`
- NuGet assemblies in: `projects/code-quality/Assets/Packages/*`
- Custom packages in: `packages/scoped-*/`
- External dependencies in: `C:/external-libs/`
- Downloaded packages in: `D:/downloads/unity-packages/`

### Problem 3: Inflexible Path Assumptions

Current design assumes:
- Assemblies always go to `Assets/Plugins/`
- Packages always go to `Packages/`

But users need flexibility to place items anywhere in the client project.

### Current Workaround

Users must:
1. Manually copy files to cache
2. Manually edit JSON config
3. Risk configuration errors

This is error-prone and doesn't leverage the tool's validation capabilities.

## Requirements

### R-BLD-PREP-020: Two-Config Architecture

**Priority:** Critical  
**Status:** New

The tool SHALL support two distinct configuration types:

1. **Preparation Manifest** (`preparation-sources.json`)
   - Defines what to collect into cache
   - Maps: external sources → cache
   - No build-time concerns

2. **Build Injection Config** (`build-injection.json`)
   - Defines what to inject into client
   - Maps: cache → client targets
   - Includes: packages, assemblies, assets, code patches, scripting symbols
   - No path restrictions on targets

### R-BLD-PREP-021: Manual Source Addition (CLI)

**Priority:** High  
**Status:** New

The tool SHALL provide CLI commands to manually add sources from any location to the preparation manifest.

**Commands:**

```bash
# Add a source to preparation manifest
config add-source --source <path> --cache-as <name> --type <type> --manifest <file>

# Add injection mapping to build config
config add-injection --source <cache-path> --target <client-path> --type <type> --config <file>

# Batch operations
config add-batch --manifest <batch-file> --output <config-file>
```

### R-BLD-PREP-022: Automatic Cache Population

**Priority:** High  
**Status:** New

When adding items manually, the tool SHALL automatically:
1. Copy the source file/folder to the cache
2. Update the config with correct cache paths
3. Validate the copied item
4. Report success/failure

### R-BLD-PREP-023: Manual Item Addition (TUI)

**Priority:** Medium  
**Status:** New

The TUI SHALL provide interactive screens for:
1. Browsing filesystem to select sources
2. Specifying target locations
3. Previewing changes before applying
4. Batch operations

### R-BLD-PREP-024: Source Path Flexibility

**Priority:** High  
**Status:** New

The tool SHALL accept source paths:
- Absolute paths (any drive/location)
- Relative paths (to git root)
- UNC paths (network shares)
- Mixed path separators (normalize automatically)

### R-BLD-PREP-025: Manifest-Based Batch Addition

**Priority:** Medium  
**Status:** New

The tool SHALL support a manifest file format for batch operations:

```yaml
# preparation-manifest.yml
packages:
  - source: D:/downloads/unity-packages/com.example.package
    name: com.example.package
    version: 1.0.0
    target: projects/client/Packages/com.example.package

  - source: projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.asyncinterfaces@328a307bd65b
    name: org.nuget.microsoft.bcl.asyncinterfaces
    version: 328a307bd65b
    target: projects/client/Packages/org.nuget.microsoft.bcl.asyncinterfaces

assemblies:
  - source: C:/external-libs/MyCustomLib.dll
    name: MyCustomLib
    version: 2.0.0
    target: projects/client/Assets/Plugins/MyCustomLib.dll

  - source: projects/code-quality/Assets/Packages/Polly.8.6.2
    name: Polly
    version: 8.6.2
    target: projects/client/Assets/Plugins/Polly.8.6.2
```

## Detailed Design

### Configuration Architecture

#### Config 1: Preparation Manifest

**File:** `build/preparation/sources/<id>.json`  
**Purpose:** Define what to collect into cache (Phase 1)

```json
{
  "version": "1.0",
  "id": "unity-packages",
  "title": "Unity Packages from Code Quality",
  "description": "Unity packages extracted from code-quality project Library/PackageCache",
  "cacheDirectory": "build/preparation/cache/unity",
  "items": [
    {
      "source": "D:/downloads/com.example.package",
      "cacheAs": "com.example.package",
      "type": "package"
    },
    {
      "source": "projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.asyncinterfaces@328a307bd65b",
      "cacheAs": "org.nuget.microsoft.bcl.asyncinterfaces",
      "type": "package"
    },
    {
      "source": "C:/external-libs/Polly.8.6.2",
      "cacheAs": "Polly.8.6.2",
      "type": "assembly"
    },
    {
      "source": "D:/assets/CustomTextures",
      "cacheAs": "CustomTextures",
      "type": "asset"
    }
  ]
}
```

**Fields:**
- `id`: Unique identifier (kebab-case, required)
- `title`: Human-readable title (required)
- `description`: Detailed description (optional)
- `cacheDirectory`: Where to store cached items (optional, defaults to `build/preparation/cache`)
- `source`: Absolute or relative path to source (any location)
- `cacheAs`: Name to use in cache directory
- `type`: `package`, `assembly`, or `asset`

**Phase 1 Command:**
```bash
cache populate --manifest sources/my-sources.json
```

**Result:** All items copied to `build/preparation/cache/<cacheAs>`

#### Config 2: Build Injection Config

**File:** `build/preparation/configs/<id>.json`  
**Purpose:** Define what to inject into client and build operations (Phase 2)

```json
{
  "version": "1.0",
  "id": "production",
  "title": "Production Build Configuration",
  "description": "Production build with optimizations, API endpoints, and release symbols",
  "packages": [
    {
      "name": "com.example.package",
      "version": "1.0.0",
      "source": "build/preparation/cache/com.example.package",
      "target": "projects/client/Packages/com.example.package"
    },
    {
      "name": "org.nuget.microsoft.bcl.asyncinterfaces",
      "version": "328a307bd65b",
      "source": "build/preparation/cache/org.nuget.microsoft.bcl.asyncinterfaces",
      "target": "projects/client/Packages/org.nuget.microsoft.bcl.asyncinterfaces"
    }
  ],
  "assemblies": [
    {
      "name": "Polly",
      "version": "8.6.2",
      "source": "build/preparation/cache/Polly.8.6.2",
      "target": "projects/client/Assets/MyCustomLibs/Polly.8.6.2"
    }
  ],
  "assetManipulations": [
    {
      "type": "copy",
      "source": "build/preparation/cache/CustomTextures",
      "target": "projects/client/Assets/Resources/Textures"
    }
  ],
  "codePatches": [
    {
      "file": "projects/client/Assets/Scripts/Config.cs",
      "patches": [
        {
          "search": "const string API_URL = \"localhost\"",
          "replace": "const string API_URL = \"production.example.com\""
        }
      ]
    }
  ],
  "scriptingDefineSymbols": {
    "add": ["PRODUCTION_BUILD", "ENABLE_LOGGING"],
    "remove": ["DEBUG_MODE", "DEVELOPMENT"],
    "platform": "StandaloneWindows64",
    "clearExisting": false
  }
}
```

**Key Points:**
- `source`: Always points to cache
- `target`: Any path in client project (no restrictions)
- Includes all build-time operations

**Phase 2 Command:**
```bash
prepare inject --config configs/production.json
```

### CLI Commands

#### `config add-source`

**Purpose:** Add a source to preparation manifest

```bash
dotnet run -- config add-source \
  --source <source-path> \
  --cache-as <cache-name> \
  --type <package|assembly|asset> \
  --manifest <manifest-file> \
  [--dry-run]
```

**Parameters:**
- `--source` (required): Absolute or relative path to source (any location)
- `--cache-as` (required): Name to use in cache
- `--type` (required): Type of item
- `--manifest` (required): Preparation manifest file
- `--dry-run` (optional): Preview without applying

**Example:**
```bash
dotnet run -- config add-source \
  --source "D:/downloads/com.example.package" \
  --cache-as "com.example.package" \
  --type package \
  --manifest "build/preparation/sources/my-sources.json"
```

#### `config add-injection`

**Purpose:** Add injection mapping to build config

```bash
dotnet run -- config add-injection \
  --source <cache-path> \
  --target <client-path> \
  --type <package|assembly> \
  --config <config-file> \
  [--name <name>] \
  [--version <version>] \
  [--dry-run]
```

**Parameters:**
- `--source` (required): Path in cache (e.g., `build/preparation/cache/MyLib`)
- `--target` (required): Target path in client (any location)
- `--type` (required): Type of item
- `--config` (required): Build injection config file
- `--name` (optional): Item name (auto-detect if omitted)
- `--version` (optional): Version
- `--dry-run` (optional): Preview without applying

**Example:**
```bash
dotnet run -- config add-injection \
  --source "build/preparation/cache/Polly.8.6.2" \
  --target "projects/client/Assets/MyLibs/Polly" \
  --type assembly \
  --name "Polly" \
  --version "8.6.2" \
  --config "build/preparation/configs/production.json"
```

#### `config add-batch`

**Purpose:** Batch add from manifest

```bash
dotnet run -- config add-batch \
  --manifest <batch-manifest> \
  --output <config-file> \
  --config-type <source|injection> \
  [--dry-run] \
  [--continue-on-error]
```

**Parameters:**
- `--manifest` (required): Batch manifest file (YAML/JSON)
- `--output` (required): Output config file
- `--config-type` (required): `source` or `injection`
- `--dry-run` (optional): Preview
- `--continue-on-error` (optional): Continue on failures

**Example:**
```bash
dotnet run -- config add-batch \
  --manifest "my-dependencies.yml" \
  --output "build/preparation/sources/all-sources.json" \
  --config-type source
```

### TUI Enhancement

#### New Menu: "Manual Sources"

```
┌─ Build Preparation Tool ─────────────────────────────────┐
│                                                           │
│  [1] Auto-scan directory (existing)                      │
│  [2] Manual source management (NEW)                      │
│  [3] Validate configuration                              │
│  [4] Execute preparation                                 │
│  [5] Exit                                                │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

#### Manual Source Management Screen

```
┌─ Manual Source Management ───────────────────────────────┐
│                                                           │
│  Config: build/preparation/configs/default.json          │
│                                                           │
│  [1] Add Package                                         │
│  [2] Add Assembly                                        │
│  [3] Add from Manifest                                   │
│  [4] View Current Items                                  │
│  [5] Remove Item                                         │
│  [6] Back                                                │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

#### Add Package Flow

```
┌─ Add Package ────────────────────────────────────────────┐
│                                                           │
│  Source Path:                                            │
│  > [Browse...] D:\downloads\com.example.package          │
│                                                           │
│  Package Name:                                           │
│  > com.example.package                                   │
│                                                           │
│  Version (optional):                                     │
│  > 1.0.0                                                 │
│                                                           │
│  Target Path (default: projects/client/Packages/...):    │
│  > projects/client/Packages/com.example.package          │
│                                                           │
│  [Preview] [Add] [Cancel]                                │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

### Configuration Schema Update

No schema changes required. The tool will populate the existing schema:

```json
{
  "packages": [
    {
      "name": "...",
      "version": "...",
      "source": "build/preparation/cache/...",  // Always cache path
      "target": "projects/client/Packages/..."
    }
  ],
  "assemblies": [
    {
      "name": "...",
      "version": "...",
      "source": "build/preparation/cache/...",  // Always cache path
      "target": "projects/client/Assets/Plugins/..."
    }
  ]
}
```

**Key principle:** The config always references cache paths. The manual commands handle copying from scattered sources to cache.

### Manifest File Format

Support both YAML and JSON:

**YAML (recommended for readability):**
```yaml
version: "1.0"
description: "Manual dependency manifest"

packages:
  - source: D:/downloads/unity-packages/com.example.package
    name: com.example.package
    version: 1.0.0
    target: projects/client/Packages/com.example.package

  - source: projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.asyncinterfaces@328a307bd65b
    name: org.nuget.microsoft.bcl.asyncinterfaces
    version: 328a307bd65b
    # target is optional, will use default

assemblies:
  - source: C:/external-libs/MyCustomLib.dll
    name: MyCustomLib
    version: 2.0.0

  - source: projects/code-quality/Assets/Packages/Polly.8.6.2
    name: Polly
    version: 8.6.2
```

**JSON (for programmatic generation):**
```json
{
  "version": "1.0",
  "description": "Manual dependency manifest",
  "packages": [
    {
      "source": "D:/downloads/unity-packages/com.example.package",
      "name": "com.example.package",
      "version": "1.0.0",
      "target": "projects/client/Packages/com.example.package"
    }
  ],
  "assemblies": [
    {
      "source": "C:/external-libs/MyCustomLib.dll",
      "name": "MyCustomLib",
      "version": "2.0.0"
    }
  ]
}
```

## Implementation Plan

### Phase 1: CLI Commands (Priority 1)

**Tasks:**
1. Implement `config add-package` command
2. Implement `config add-assembly` command
3. Add file/folder copy logic with validation
4. Add dry-run support
5. Add unit tests
6. Update CLI help documentation

**Estimated effort:** 2-3 days

### Phase 2: Manifest Support (Priority 2)

**Tasks:**
1. Define manifest schema (YAML/JSON)
2. Implement manifest parser
3. Implement `config add-batch` command
4. Add batch operation error handling
5. Add manifest validation
6. Add unit tests
7. Create example manifests

**Estimated effort:** 1-2 days

### Phase 3: TUI Enhancement (Priority 3)

**Tasks:**
1. Add "Manual Sources" menu
2. Implement file browser for source selection
3. Implement add package/assembly screens
4. Add preview functionality
5. Add item removal screen
6. Update TUI navigation
7. Add integration tests

**Estimated effort:** 3-4 days

## Usage Examples

### Example 1: Add Single Package from External Location

```bash
# Add a Unity package downloaded from Asset Store
dotnet run -- config add-package \
  --source "D:/AssetStore/com.unity.cinemachine" \
  --config "build/preparation/configs/default.json" \
  --name "com.unity.cinemachine" \
  --version "2.9.7"
```

**Result:**
- Copies `D:/AssetStore/com.unity.cinemachine` → `build/preparation/cache/com.unity.cinemachine`
- Adds to config:
  ```json
  {
    "name": "com.unity.cinemachine",
    "version": "2.9.7",
    "source": "build/preparation/cache/com.unity.cinemachine",
    "target": "projects/client/Packages/com.unity.cinemachine"
  }
  ```

### Example 2: Add Multiple Items from Scattered Locations

Create `dependencies.yml`:
```yaml
version: "1.0"
packages:
  - source: projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.asyncinterfaces@328a307bd65b
    name: org.nuget.microsoft.bcl.asyncinterfaces
    version: 328a307bd65b

  - source: projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.cryptography@2e2de2a65161
    name: org.nuget.microsoft.bcl.cryptography
    version: 2e2de2a65161

assemblies:
  - source: projects/code-quality/Assets/Packages/Polly.8.6.2
    name: Polly
    version: 8.6.2

  - source: C:/external-libs/CustomLib.dll
    name: CustomLib
    version: 1.0.0
```

Run:
```bash
dotnet run -- config add-batch \
  --manifest "dependencies.yml" \
  --config "build/preparation/configs/default.json"
```

**Output:**
```
Processing manifest: dependencies.yml
[1/4] Adding package: org.nuget.microsoft.bcl.asyncinterfaces... ✓
[2/4] Adding package: org.nuget.microsoft.bcl.cryptography... ✓
[3/4] Adding assembly: Polly... ✓
[4/4] Adding assembly: CustomLib... ✓

Summary:
  Packages added: 2
  Assemblies added: 2
  Failed: 0

Configuration updated: build/preparation/configs/default.json
```

### Example 3: Preview Changes (Dry Run)

```bash
dotnet run -- config add-package \
  --source "D:/downloads/com.example.package" \
  --config "build/preparation/configs/default.json" \
  --name "com.example.package" \
  --dry-run
```

**Output:**
```
[DRY RUN] Would perform the following actions:

1. Copy source:
   From: D:/downloads/com.example.package
   To:   build/preparation/cache/com.example.package

2. Add to config (build/preparation/configs/default.json):
   {
     "name": "com.example.package",
     "version": null,
     "source": "build/preparation/cache/com.example.package",
     "target": "projects/client/Packages/com.example.package"
   }

No changes made (dry run mode).
```

## Backward Compatibility

### Existing Functionality Preserved

All existing commands remain unchanged:
- `cache populate --source <dir>` - Still works for single-directory scanning
- `prepare inject` - Works with configs created by any method
- `validate` - Validates all configs regardless of creation method

### Migration Path

Users can mix approaches:
1. Use `cache populate` for bulk scanning
2. Use `config add-package/add-assembly` for specific items
3. Use `config add-batch` for organized dependency lists

## Testing Strategy

### Unit Tests

- Command parsing and validation
- File copy operations
- Config update logic
- Manifest parsing
- Path normalization
- Error handling

### Integration Tests

- End-to-end add-package flow
- End-to-end add-assembly flow
- Batch manifest processing
- Mixed source locations (absolute, relative, UNC)
- Config validation after additions

### Manual Testing Scenarios

1. Add package from external drive
2. Add assembly from network share
3. Add multiple items from manifest
4. Mix auto-scan with manual additions
5. TUI workflow for manual sources

## Documentation Updates

### Files to Update

1. `docs/guides/build-preparation-workflow.md`
   - Add "Manual Source Management" section
   - Add manifest examples

2. `packages/.../tool/README.md`
   - Document new CLI commands
   - Add manifest schema reference

3. Create new guide: `docs/guides/manual-dependency-management.md`
   - Detailed examples
   - Best practices
   - Troubleshooting

### CLI Help Text

Update `--help` output for all new commands with clear examples.

## Success Criteria

1. ✅ Can add individual packages/assemblies from any location via CLI
2. ✅ Can add multiple items via manifest file
3. ✅ TUI provides interactive manual source management
4. ✅ All paths (absolute, relative, UNC) are supported
5. ✅ Dry-run mode works for all operations
6. ✅ Config validation passes after manual additions
7. ✅ Backward compatible with existing workflows
8. ✅ Documentation complete and accurate

## Future Enhancements

### Post-Amendment 002

- **Import from existing Unity project**: Scan another Unity project and import its packages
- **Dependency resolution**: Automatically include transitive dependencies
- **Version conflict detection**: Warn when adding conflicting versions
- **Source tracking**: Remember original source locations for updates
- **Update command**: Re-copy from original source when version changes

## References

- **Parent Spec:** `build-preparation-tool.md`
- **Related:** `build-preparation-tool-amendment-001.md` (Folder support)
- **Rule:** R-BLD-010 (Task runner usage)
- **Rule:** R-BLD-060 (Client read-only)

## Changelog

- **2025-10-17**: Initial draft (Amendment 002)
