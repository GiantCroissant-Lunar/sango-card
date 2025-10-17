---
doc_id: DOC-2025-00201
title: Build Preparation Tool API Reference
doc_type: reference
status: active
canonical: true
created: 2025-10-17
updated: 2025-10-17
tags: [api, build-tool, reference, dotnet]
summary: Technical API reference for the Build Preparation Tool's programmatic interfaces and CLI commands
---

# Build Preparation Tool API Reference

Technical reference for the Build Preparation Tool's programmatic interfaces.

## Table of Contents

- [CLI Commands](#cli-commands)
- [Configuration Schemas](#configuration-schemas)
- [Exit Codes](#exit-codes)
- [Environment Variables](#environment-variables)
- [File Formats](#file-formats)
- [Services API](#services-api)

## CLI Commands

### Global Options

All commands support these global options:

| Option | Description | Default |
|--------|-------------|---------|
| `--version` | Display version information | - |
| `-h, --help` | Show help and usage | - |
| `--verbose` | Enable verbose logging | `false` |
| `--quiet` | Suppress non-error output | `false` |

### Command: `config`

Manage preparation configuration files.

#### `config list`

List all configuration files in the current context.

**Syntax**:
```bash
config list [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--path` | string | Directory to search | Current directory |
| `--recursive` | flag | Search subdirectories | `false` |

**Exit Codes**:
- `0` - Success
- `1` - Error

#### `config show`

Display configuration file contents.

**Syntax**:
```bash
config show --path <file> [options]
```

**Options**:
| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `--path` | string | Yes | Config file path |
| `--format` | enum | No | Output format: `json`, `yaml`, `table` |

**Exit Codes**:
- `0` - Success
- `1` - File not found
- `2` - Invalid format

#### `config create`

Create a new configuration file.

**Syntax**:
```bash
config create --type <type> --output <file> [options]
```

**Options**:
| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `--type` | enum | Yes | Config type: `manifest`, `build` |
| `--output` | string | Yes | Output file path |
| `--template` | string | No | Template to use |

**Exit Codes**:
- `0` - Success
- `1` - Invalid type
- `2` - File already exists
- `3` - Write error

### Command: `cache`

Manage the preparation cache.

#### `cache list`

List cached items.

**Syntax**:
```bash
cache list [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--type` | enum | Filter by type: `packages`, `assemblies`, `assets` |
| `--format` | enum | Output format: `list`, `tree`, `json` |

**Exit Codes**:
- `0` - Success
- `1` - Cache not found

#### `cache info`

Display cache statistics.

**Syntax**:
```bash
cache info [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--detailed` | flag | Show detailed stats |

**Output**:
```text
Cache Location: /path/to/cache
Total Items: 42
  Packages: 15
  Assemblies: 12
  Assets: 15
Total Size: 2.3 GB
```

**Exit Codes**:
- `0` - Success

#### `cache clear`

Clear cache contents.

**Syntax**:
```bash
cache clear [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--type` | enum | Clear specific type only |
| `--force` | flag | Skip confirmation |

**Exit Codes**:
- `0` - Success
- `1` - User cancelled
- `2` - Permission error

#### `cache verify`

Verify cache integrity.

**Syntax**:
```bash
cache verify [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--fix` | flag | Attempt to fix issues |

**Exit Codes**:
- `0` - Cache valid
- `1` - Issues found
- `2` - Fix failed

### Command: `validate`

Validate configuration files.

#### Syntax

```bash
validate [options]
```

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--manifest` | string | Manifest file to validate |
| `--config` | string | Build config to validate |
| `--strict` | flag | Enable strict validation |

**Exit Codes**:
- `0` - Validation passed
- `1` - Validation failed
- `2` - File not found
- `3` - Invalid JSON

**Validation Rules**:
- JSON syntax correctness
- Required fields present
- Path existence checks
- Type validations
- Cross-reference validation

### Command: `prepare`

Execute Phase 1 preparation.

#### `prepare run`

Run preparation from manifest.

**Syntax**:
```bash
prepare run --manifest <file> [options]
```

**Options**:
| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `--manifest` | string | Yes | Manifest file path |
| `--dry-run` | flag | No | Preview without executing |
| `--force` | flag | No | Overwrite existing cache items |
| `--parallel` | int | No | Max parallel operations (default: 4) |

**Exit Codes**:
- `0` - Success
- `1` - Validation failed
- `2` - Preparation failed
- `3` - Partial success (some items failed)

**Progress Output**:
```text
Preparing packages...
  [1/3] com.unity.addressables ✓
  [2/3] com.unity.ui.toolkit ✓
  [3/3] com.custom.gameplay ✓

Preparing assemblies...
  [1/2] GameplayCore.dll ✓
  [2/2] NetworkingLib.dll ✓

✓ Preparation complete!
  Total: 5 items
  Success: 5
  Failed: 0
```

### Command: `tui`

Launch Terminal UI mode.

**Syntax**:
```bash
tui
```

**Or via mode flag**:
```bash
--mode tui
```

**Exit Codes**:
- `0` - Normal exit
- `1` - TUI error
- `2` - Terminal not supported

## Configuration Schemas

### PreparationManifest Schema

**JSON Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["version"],
  "properties": {
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$",
      "description": "Schema version (e.g., 1.0.0)"
    },
    "packages": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "sourcePath", "targetFileName"],
        "properties": {
          "name": {
            "type": "string",
            "description": "Package identifier"
          },
          "sourcePath": {
            "type": "string",
            "description": "Absolute path to package source"
          },
          "targetFileName": {
            "type": "string",
            "pattern": ".*\\.tgz$",
            "description": "Target .tgz filename in cache"
          }
        }
      }
    },
    "assemblies": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "sourcePath"],
        "properties": {
          "name": {
            "type": "string",
            "description": "Assembly identifier"
          },
          "sourcePath": {
            "type": "string",
            "pattern": ".*\\.dll$",
            "description": "Absolute path to .dll file"
          }
        }
      }
    },
    "assets": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "sourcePath"],
        "properties": {
          "name": {
            "type": "string",
            "description": "Asset identifier"
          },
          "sourcePath": {
            "type": "string",
            "description": "Absolute path to asset file/directory"
          }
        }
      }
    }
  }
}
```

### PreparationConfig Schema

**JSON Schema**:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["version", "clientPath"],
  "properties": {
    "version": {
      "type": "string",
      "pattern": "^\\d+\\.\\d+\\.\\d+$"
    },
    "clientPath": {
      "type": "string",
      "description": "Absolute path to Unity client project"
    },
    "packages": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["sourceFileName", "targetPath"],
        "properties": {
          "sourceFileName": {
            "type": "string",
            "description": "Filename in cache"
          },
          "targetPath": {
            "type": "string",
            "description": "Relative path in client"
          }
        }
      }
    },
    "assemblies": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["sourceFileName", "targetPath"],
        "properties": {
          "sourceFileName": {
            "type": "string"
          },
          "targetPath": {
            "type": "string"
          }
        }
      }
    },
    "assets": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["sourcePattern", "targetPath", "operation"],
        "properties": {
          "sourcePattern": {
            "type": "string",
            "description": "Glob pattern"
          },
          "targetPath": {
            "type": "string",
            "description": "Relative path in client"
          },
          "operation": {
            "type": "string",
            "enum": ["Copy", "Move", "Delete"],
            "description": "Operation type"
          }
        }
      }
    }
  }
}
```

## Exit Codes

Standard exit codes used across all commands:

| Code | Meaning | Description |
|------|---------|-------------|
| `0` | Success | Operation completed successfully |
| `1` | General Error | Unspecified error occurred |
| `2` | Validation Error | Configuration validation failed |
| `3` | File Not Found | Required file not found |
| `4` | Permission Error | Insufficient permissions |
| `5` | Network Error | Network operation failed |
| `6` | Partial Success | Some operations succeeded, others failed |
| `99` | Fatal Error | Unrecoverable error |

## Environment Variables

### Configuration

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `BUILD_PREP_CACHE_DIR` | Path | `~/.sangocard/cache` | Cache directory |
| `BUILD_PREP_CONFIG_DIR` | Path | `~/.sangocard/config` | Config directory |
| `BUILD_PREP_LOG_LEVEL` | Enum | `Information` | Log level: `Trace`, `Debug`, `Information`, `Warning`, `Error` |
| `BUILD_PREP_LOG_FILE` | Path | - | Log file path (optional) |

### Runtime

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `DOTNET_ENVIRONMENT` | String | `Production` | Runtime environment |
| `NO_COLOR` | Flag | `false` | Disable color output |
| `TERM` | String | - | Terminal type |

### Git Integration

| Variable | Type | Default | Description |
|----------|------|---------|-------------|
| `GIT_DIR` | Path | Auto-detected | Git directory override |
| `BUILD_PREP_GIT_ROOT` | Path | Auto-detected | Git root override |

## File Formats

### Cache Structure

```text
~/.sangocard/cache/
├── packages/
│   ├── com.unity.addressables-1.21.0.tgz
│   ├── com.unity.ui.toolkit-2.0.0.tgz
│   └── com.custom.gameplay-0.5.0.tgz
├── assemblies/
│   ├── GameplayCore.dll
│   ├── NetworkingLib.dll
│   └── AnalyticsSDK.dll
└── assets/
    ├── Characters/
    ├── UI/
    └── Audio/
```

### Manifest File Locations

Default search paths:

1. Current directory
2. `./config/`
3. `~/.sangocard/config/`
4. Git root + `/config/`

### Naming Conventions

**Manifest files**:
- Pattern: `*-manifest.json` or `*-sources.json`
- Example: `unity-packages-manifest.json`

**Config files**:
- Pattern: `*-config.json` or `*-build.json`
- Example: `client-build-config.json`

## Services API

The tool is built with dependency injection. Core services:

### IPreparationService

```csharp
public interface IPreparationService
{
    /// <summary>
    /// Execute preparation from manifest
    /// </summary>
    Task<PreparationResult> PrepareAsync(
        PreparationManifest manifest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate manifest
    /// </summary>
    Task<ValidationResult> ValidateManifestAsync(
        PreparationManifest manifest);
}
```

### ICacheService

```csharp
public interface ICacheService
{
    /// <summary>
    /// Get cache information
    /// </summary>
    Task<CacheInfo> GetCacheInfoAsync();

    /// <summary>
    /// Clear cache
    /// </summary>
    Task ClearCacheAsync(CacheType? type = null);

    /// <summary>
    /// Verify cache integrity
    /// </summary>
    Task<VerificationResult> VerifyCacheAsync();
}
```

### IConfigurationService

```csharp
public interface IConfigurationService
{
    /// <summary>
    /// Load manifest from file
    /// </summary>
    Task<PreparationManifest> LoadManifestAsync(string path);

    /// <summary>
    /// Save manifest to file
    /// </summary>
    Task SaveManifestAsync(
        PreparationManifest manifest,
        string path);

    /// <summary>
    /// Load build config from file
    /// </summary>
    Task<PreparationConfig> LoadConfigAsync(string path);

    /// <summary>
    /// Save build config to file
    /// </summary>
    Task SaveConfigAsync(
        PreparationConfig config,
        string path);
}
```

### IGitRootResolver

```csharp
public interface IGitRootResolver
{
    /// <summary>
    /// Resolve git repository root
    /// </summary>
    string ResolveGitRoot();

    /// <summary>
    /// Resolve path relative to git root
    /// </summary>
    string ResolveRelativePath(string path);
}
```

## Message Types

The tool uses MessagePipe for reactive messaging:

### PreparationProgressMessage

```csharp
public record PreparationProgressMessage(
    int CurrentItem,
    int TotalItems,
    string ItemName,
    ProgressState State);

public enum ProgressState
{
    Started,
    InProgress,
    Completed,
    Failed
}
```

### CacheUpdatedMessage

```csharp
public record CacheUpdatedMessage(
    CacheUpdateType UpdateType,
    string ItemPath);

public enum CacheUpdateType
{
    ItemAdded,
    ItemRemoved,
    CacheCleared
}
```

### ValidationMessage

```csharp
public record ValidationMessage(
    ValidationSeverity Severity,
    string Message,
    string? FieldPath = null);

public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}
```

## TUI API

### View Navigation

```csharp
public interface IViewManager
{
    /// <summary>
    /// Show view
    /// </summary>
    void ShowView<TView>() where TView : View;

    /// <summary>
    /// Navigate back
    /// </summary>
    void NavigateBack();

    /// <summary>
    /// Show dialog
    /// </summary>
    Task<DialogResult> ShowDialogAsync(Dialog dialog);
}
```

### View Lifecycle

All TUI views implement:

```csharp
public abstract class BaseView : View
{
    /// <summary>
    /// Called when view is loaded
    /// </summary>
    protected virtual Task OnLoadAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when view is unloading
    /// </summary>
    protected virtual Task OnUnloadAsync() => Task.CompletedTask;

    /// <summary>
    /// Handle keyboard input
    /// </summary>
    protected virtual bool OnKeyPress(Key key) => false;
}
```

## Error Handling

### Error Response Format

CLI error output format:

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Configuration validation failed",
    "details": [
      {
        "field": "packages[0].sourcePath",
        "error": "Path does not exist"
      }
    ]
  }
}
```

### Common Error Codes

| Code | Description |
|------|-------------|
| `VALIDATION_FAILED` | Config validation error |
| `FILE_NOT_FOUND` | Required file missing |
| `PERMISSION_DENIED` | Insufficient permissions |
| `CACHE_ERROR` | Cache operation failed |
| `GIT_ERROR` | Git operation failed |
| `NETWORK_ERROR` | Network operation failed |

## Version Compatibility

### API Versioning

The tool follows semantic versioning:

- **Major**: Breaking API changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes

### Config Version Compatibility

| Tool Version | Manifest Version | Config Version |
|--------------|------------------|----------------|
| 1.x.x | 1.0.0 | 1.0.0 |
| 2.x.x | 1.0.0, 2.0.0 | 1.0.0, 2.0.0 |

The tool validates config version on load and rejects incompatible versions.

---

**Version**: 1.0.0  
**Last Updated**: 2025-10-17  
**Maintainer**: Sango Card Build Team
