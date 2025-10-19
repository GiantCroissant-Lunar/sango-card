# Build Preparation Configs

This directory contains configuration files for the two-phase build preparation system.

## Overview

The build preparation system uses two distinct config types:

### Phase 1: Preparation (code-quality → cache)

- **Config:** `preparation.json`
- **Input:** `projects/code-quality/Packages/`
- **Output:** `build/preparation/cache/`
- **Purpose:** Extract and cache third-party packages from code-quality project

### Phase 2: Injection (cache → client)

- **Config:** `injection.json`
- **Input:** `build/preparation/cache/`
- **Output:** `projects/client/Packages/`
- **Purpose:** Inject cached packages into client project for builds

## File Structure

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
