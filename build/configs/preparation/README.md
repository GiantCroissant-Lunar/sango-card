# Build Preparation Configs

This directory contains configuration files for the multi-stage build preparation system.

## ⚠️ Configuration Version Status

| Version | Status | Recommended For |
|---------|--------|----------------|
| **V2.0 Multi-Stage** | ✅ **CURRENT** | All new development |
| V1.0 Single-File | ⚠️ **DEPRECATED** | Legacy compatibility only |

**For V1.0 deprecation details, see:** [docs/_inbox/v1-config-deprecation.md](../../../docs/_inbox/v1-config-deprecation.md)

## Quick Start

### V2.0 Multi-Stage (Recommended)

```bash
# Use multi-stage build workflow
task build:unity:multi-stage

# Or via Nuke directly
nuke BuildWithMultiStage --MultiStageConfig build/configs/preparation/multi-stage-preparation.json
```

**Config:** `multi-stage-preparation.json`

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

**Schema:** `multi-stage-schema.json`

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

## Current Configs

### `preparation.json` (Phase 1)

Preparation manifest defining which packages to extract from code-quality and cache.

**Usage:**

```bash
# Dry-run to see what would be cached
dotnet run --project build/preparation -- prepare \
  --config build/preparation/configs/preparation.json \
  --dry-run

# Execute preparation (populate cache)
dotnet run --project build/preparation -- prepare \
  --config build/preparation/configs/preparation.json
```

### `injection.json` (Phase 2)

Injection config defining which cached packages to inject into client project.

**Usage:**

```bash
# Dry-run to see what would be injected
dotnet run --project build/preparation -- inject \
  --config build/preparation/configs/injection.json \
  --dry-run

# Execute injection (populate client)
dotnet run --project build/preparation -- inject \
  --config build/preparation/configs/injection.json
```

## Workflow

1. **Phase 1:** Run preparation to populate cache from code-quality
2. **Phase 2:** Run injection to populate client from cache
3. **Build:** Execute Unity build with prepared client project

## Format

All configs use **JSON format only**.

### JSON Schemas

Both config types have JSON schemas for validation and IDE support:

- **`build/nuke/build/Schemas/preparation.schema.json`** - Schema for Phase 1 configs
- **`build/nuke/build/Schemas/injection.schema.json`** - Schema for Phase 2 configs

Schemas are part of the NUKE build system definition, while configs in this directory are dynamic runtime configurations.

**Benefits:**

- ✅ IDE autocomplete for properties
- ✅ Real-time validation
- ✅ Inline documentation
- ✅ Type checking for values

### Structure

**Phase 1 (preparation.json):**

- `packages[]` - Package definitions with source/target paths (required)
- `assemblies[]` - Assembly definitions (required, can be empty)

**Phase 2 (injection.json):**

- `packages[]` - Package definitions with source/target paths (required)
- `assemblies[]` - Assembly definitions (required, can be empty)
- `assetManipulations[]` - Asset operations (optional)
- `codePatches[]` - Code modifications (optional)
- `scriptingDefineSymbols` - Compiler symbols (optional)
