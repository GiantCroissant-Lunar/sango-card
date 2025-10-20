# Build Preparation & Injection Configurations

This directory contains configurations for the two-phase build preparation and injection system.

## ⚠️ Configuration Version Status

| Config | Version | Purpose | Status |
|--------|---------|---------|--------|
| `preparation.json` | v1.0 | Cache population (sources) | ✅ **ACTIVE** |
| `multi-stage-injection.json` | v2.0 | Multi-stage injection (operations) | ✅ **CURRENT** |
| `injection.json` | v1.0 | Single-stage injection | ⚠️ **DEPRECATED** |

**For V1.0 deprecation details, see:** [docs/_inbox/v1-config-deprecation.md](../../../docs/_inbox/v1-config-deprecation.md)

## 📦 Clear Separation of Concerns

### Phase 1: Preparation (Cache Population)

**Config:** `preparation.json` (v1.0 - still used)  
**Purpose:** Define what to copy FROM code-quality TO cache  
**Contains ONLY:**

- Package sources: `code-quality/Library/PackageCache/pkg@hash` → `build/preparation/cache/pkg`
- Assembly sources: `code-quality/Assets/Packages/asm.dll` → `build/preparation/cache/asm.dll`

**Does NOT contain:** injection operations, asset manipulations, code patches, or scripting symbols

### Phase 2: Injection (Build Operations)

**Config:** `multi-stage-injection.json` (v2.0 - CURRENT)  
**Purpose:** Define what to inject FROM cache TO client + ALL build operations  
**Contains:**

- ✅ Package/assembly targets: `cache/pkg` → `projects/client/Packages/pkg`
- ✅ Asset manipulations: copy/move/delete operations
- ✅ Code patches: search/replace in source files
- ✅ Scripting define symbols: add/remove compiler symbols
- ✅ Platform-specific operations: iOS/Android configurations

**References:** `cacheSource` points to `preparation.json` for cache population

## Quick Start

### V2.0 Multi-Stage (Recommended)

```bash
# Step 1: Populate cache from code-quality (uses preparation.json)
task build:cache
# or: nuke PrepareCache

# Step 2: Multi-stage injection + build (uses multi-stage-injection.json)
task build:unity:multi-stage
# or: nuke BuildWithMultiStage --MultiStageConfig build/configs/preparation/multi-stage-injection.json
```

**Config:** `multi-stage-injection.json`

**Features:**

- Sequential injection stages (preTest, preBuild, postBuild, etc.)
- Platform-specific configurations (iOS/Android)
- Conditional stage execution
- Better organization and extensibility

## Configuration Formats

### V2.0 Multi-Stage Format (CURRENT)

```json
{
  "version": "2.0",
  "description": "Multi-stage injection configuration",
  "cacheSource": "build/configs/preparation/preparation.json",
  "injectionStages": [
    {
      "name": "preBuild",
      "enabled": true,
      "description": "Core build dependencies",
      "packages": [...],
      "assemblies": [...],
      "assetManipulations": [...],
      "codePatches": [...]
    },
    {
      "name": "postBuild",
      "enabled": false,
      "description": "Runtime test dependencies",
      "packages": [...]
    }
  ]
}
```

**Schema:** `build/nuke/build/Schemas/multi-stage-injection.schema.json`

### V1.0 Single-File Format (DEPRECATED)

```json
{
  "version": "1.0",
  "id": "unique-identifier",
  "title": "Human-readable title",
  "description": "Optional description",
  "packages": [
    {
      "name": "package-name",
      "version": "1.0.0",
      "source": "build/preparation/cache/package-name",
      "target": "projects/client/Packages/package-name"
    }
  ],
  "assemblies": [
    {
      "name": "assembly-name",
      "version": "1.0.0",
      "source": "build/preparation/cache/assembly.dll",
      "target": "projects/client/Assets/Plugins/assembly.dll"
    }
  ],
  "assetManipulations": [
    {
      "type": "copy",
      "source": "build/preparation/cache/assets",
      "target": "projects/client/Assets/Resources"
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
    "add": ["PRODUCTION_BUILD"],
    "remove": ["DEBUG_MODE"],
    "platform": "StandaloneWindows64",
    "clearExisting": false
  }
}
```

## Current Active Configs

### 📦 `preparation.json` (Cache Population - v1.0)

Defines package/assembly sources for cache population from code-quality project.

**Purpose:** Cache population ONLY (no injection operations)

**Schema:** `build/nuke/build/Schemas/preparation.schema.json`

**Contains:**

- Package sources: `projects/code-quality/Library/PackageCache/pkg@hash` → `build/preparation/cache/pkg`
- Assembly sources: `projects/code-quality/Assets/Packages/asm.dll` → `build/preparation/cache/asm.dll`

**Usage:**

```bash
nuke PrepareCache --PreparationConfig build/configs/preparation/preparation.json
```

### 🚀 `multi-stage-injection.json` (Multi-Stage Injection - v2.0 CURRENT)

Defines multi-stage injection configuration with full build operations.

**Purpose:** Injection operations with stage-specific control

**Schema:** `build/nuke/build/Schemas/multi-stage-injection.schema.json`

**Contains:**

- Injection stages (preTest, preBuild, postBuild, preNativeBuild, postNativeBuild)
- Package/assembly targets: `cache/pkg` → `projects/client/Packages/pkg`
- Asset manipulations: delete/copy/move operations
- Code patches: search/replace in files
- Scripting define symbols: compiler symbol management
- Platform-specific configurations

**Usage:**

```bash
nuke BuildWithMultiStage --MultiStageConfig build/configs/preparation/multi-stage-injection.json
```

### ⚠️ `injection.json` (Single-Stage Injection - v1.0 DEPRECATED)

Legacy single-stage injection config. Use `multi-stage-injection.json` instead.

## Two-Phase Workflow

### Phase 1: Preparation (Cache Population)

1. Run `PrepareCache` to populate cache from code-quality
2. Uses `preparation.json` to define sources
3. Output: `build/preparation/cache/` populated with packages & assemblies

### Phase 2: Injection (Build Operations)

1. Run `BuildWithMultiStage` for multi-stage injection + build
2. Uses `multi-stage-injection.json` to define injection stages
3. References `preparation.json` via `cacheSource` for cache population
4. Executes stages: preTest → preBuild → BuildUnity → postBuild → etc.
5. Each stage can inject packages, manipulate assets, patch code, set symbols

## JSON Schema Support

All configs have JSON schemas for IDE validation and autocomplete:

- **`preparation.schema.json`** - Cache population (v1.0)
- **`injection.schema.json`** - Single-stage injection (v1.0 DEPRECATED)
- **`multi-stage-injection.schema.json`** - Multi-stage injection (v2.0 CURRENT)

**Benefits:**

- ✅ IDE autocomplete for properties
- ✅ Real-time validation
- ✅ Inline documentation
- ✅ Type checking for values

## Config Structure

### `preparation.json` (Cache Population - v1.0)

**Purpose:** Define sources for cache population (NO injection operations)

```json
{
  "version": "1.0",
  "description": "Cache population sources",
  "packages": [
    {
      "name": "com.package.name",
      "version": "1.0.0",
      "source": "projects/code-quality/Library/PackageCache/com.package.name@hash",
      "target": "build/preparation/cache/com.package.name"
    }
  ],
  "assemblies": [
    {
      "name": "Assembly.Name",
      "version": "1.0.0",
      "source": "projects/code-quality/Assets/Packages/Assembly.Name.1.0.0",
      "target": "build/preparation/cache/Assembly.Name.1.0.0"
    }
  ]
}
```

**Note:** Does NOT contain: `assetManipulations`, `codePatches`, `scriptingDefineSymbols` - those belong in injection configs.

### `multi-stage-injection.json` (Multi-Stage Injection - v2.0)

**Purpose:** Define injection stages WITH full build operations

```json
{
  "version": "2.0",
  "description": "Multi-stage injection with operations",
  "cacheSource": "build/configs/preparation/preparation.json",
  "injectionStages": [
    {
      "name": "preBuild",
      "enabled": true,
      "description": "Core build dependencies",
      "cleanupAfter": false,
      "packages": [
        {
          "name": "com.package.name",
          "version": "1.0.0",
          "source": "build/preparation/cache/com.package.name",
          "target": "projects/client/Packages/com.package.name"
        }
      ],
      "assemblies": [...],
      "assetManipulations": [
        {
          "type": "delete",
          "target": "projects/client/Assets/Scripts/Generated",
          "description": "Remove generated code to force regeneration"
        }
      ],
      "codePatches": [
        {
          "file": "projects/client/Assets/Scripts/Config.cs",
          "patches": [
            {
              "search": "DEBUG_MODE",
              "replace": "RELEASE_MODE"
            }
          ]
        }
      ],
      "scriptingDefineSymbols": {
        "add": ["PRODUCTION_BUILD"],
        "remove": ["DEBUG_MODE"],
        "platform": "StandaloneWindows64",
        "clearExisting": false
      }
    }
  ]
}
```
