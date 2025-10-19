# Build Configuration Files

This directory contains source-controlled build configuration files, separate from generated build artifacts.

## Directory Structure

```text
build/
├── configs/              # Source-controlled configs (tracked in git)
│   ├── preparation/      # Build preparation injection configs
│   │   ├── preparation.json              # Main v1.0 config (legacy)
│   │   ├── injection.json                # Alternative v1.0 config
│   │   ├── multi-stage-preparation.json  # v2.0 multi-stage config template
│   │   ├── multi-stage-schema.json       # JSON schema for v2.0 configs
│   │   ├── DESIGN.md                     # Preparation system design
│   │   └── README.md                     # Preparation config documentation
│   └── (future config types...)          # e.g., deployment/, testing/, etc.
├── preparation/          # Generated artifacts (ignored by git)
│   ├── cache/            # Cached packages and assemblies
│   ├── assemblies/       # Compiled assemblies
│   └── batch-examples/   # Generated example files
└── nuke/                 # Build scripts (tracked in git)
```

## Key Concepts

### Source vs Artifacts

- **`build/configs/`** - Source-controlled configuration files
  - Tracked in git
  - Modified by developers
  - Used as input to build processes

- **`build/preparation/`** - Generated artifacts and caches
  - Ignored by git (except specific files)
  - Generated during build
  - Can be safely deleted and regenerated

### Configuration Types

#### Preparation Configs (`preparation/`)

Configuration for the build preparation injection system that manages dependencies and code patches.

**Formats**:

- **v1.0** (legacy) - Single-stage injection, simple package list
- **v2.0** (multi-stage) - 5-stage injection with platform-specific support

**See**: `preparation/README.md` for detailed documentation

#### Future Config Types

This structure is extensible for additional config types:

- `deployment/` - Deployment configurations
- `testing/` - Test execution configurations
- `platforms/` - Platform-specific build settings

## Usage

### In Taskfile.yml

All task commands reference configs from `build/configs/`:

```yaml
task build:prepare:client CONFIG=preparation
# Uses: build/configs/preparation/preparation.json

task build:unity:multi-stage CONFIG=multi-stage-preparation
# Uses: build/configs/preparation/multi-stage-preparation.json
```

### In Nuke Build Scripts

Default config path in `Build.Preparation.cs`:

```csharp
public AbsolutePath PreparationConfig =
    RootDirectory / "build" / "configs" / "preparation" / "preparation.json";
```

### Creating New Configs

1. Create config file in appropriate subdirectory
2. Follow schema/format for that config type
3. Commit to git
4. Reference in task commands or build scripts

## Migration Notes

**Changed in**: October 2025

Configurations moved from `build/preparation/configs/` to `build/configs/preparation/` to better separate source-controlled configs from generated artifacts.

**Old structure** (deprecated):

```text
build/
├── preparation/
│   ├── configs/       # OLD LOCATION (mixed source + artifacts)
│   └── cache/
```

**New structure** (current):

```text
build/
├── configs/           # NEW: Source-controlled only
│   └── preparation/
├── preparation/       # Artifacts only
│   └── cache/
```

## Benefits of New Structure

1. **Clear Separation**: Source configs vs generated artifacts
2. **Better Gitignore**: `build/preparation/*` ignored, `build/configs/` tracked
3. **Extensibility**: Easy to add new config types alongside `preparation/`
4. **Consistency**: Aligns with common patterns (e.g., `.github/workflows/` for CI configs)

## See Also

- `preparation/README.md` - Preparation injection system documentation
- `preparation/DESIGN.md` - Preparation system design details
- `docs/guides/build-multi-stage-injection.md` - Multi-stage injection guide
- `nuke/` - Nuke build scripts
