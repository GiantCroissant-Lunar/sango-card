---
doc_id: DOC-2025-00203
title: Multi-Stage Injection System - User Guide
doc_type: guide
status: active
canonical: true
created: 2025-10-19
tags: [build, injection, multi-stage, workflow, guide]
summary: >
  User guide for the multi-stage dependency injection system supporting
  different injection points throughout the build lifecycle.
---

# Multi-Stage Injection System - User Guide

## Overview

The multi-stage injection system extends the build preparation workflow to support different dependency injection points throughout the build lifecycle. Instead of injecting all dependencies at once, dependencies are injected at the appropriate stage where they're needed.

## Benefits

- **Separation of Concerns**: Test dependencies don't pollute production builds
- **Platform-Specific Support**: Native dependencies (CocoaPods, Gradle) injected at the right time
- **Better Performance**: Only inject what's needed per stage
- **Cleaner Builds**: Automatic cleanup of stage-specific dependencies

## Injection Stages

### Stage 1: Pre-Test Injection

**When**: Before `TestPreBuild`  
**Purpose**: Test-specific dependencies for validation  
**Cleaned**: After pre-build tests complete

**Example use cases**:
- Test frameworks and extensions
- Mock libraries
- Code coverage tools

### Stage 2: Pre-Build Injection

**When**: Before `BuildUnity`  
**Purpose**: Core Unity build dependencies  
**Cleaned**: After successful build (configurable)

**Example use cases**:
- Production packages (UniTask, MessagePack)
- Core game assemblies
- Build-time code patches

### Stage 3: Post-Build Injection

**When**: After `BuildUnity`, before `TestPostBuild`  
**Purpose**: Runtime testing dependencies  
**Cleaned**: After post-build tests complete

**Example use cases**:
- Integration test helpers
- Runtime test frameworks
- Mock services for integration tests

### Stage 4: Pre-Native-Build Injection

**When**: After Unity build, before native build (Xcode/Gradle)  
**Purpose**: Platform-specific native dependencies  
**Cleaned**: After native build completes (configurable)

**Example use cases for iOS**:
- CocoaPods dependencies (Podfile)
- Custom iOS frameworks
- Xcode project patches
- Info.plist modifications

**Example use cases for Android**:
- Gradle dependencies
- Custom Android libraries
- AndroidManifest.xml patches
- ProGuard configurations

### Stage 5: Post-Native-Build Injection

**When**: After native build, before packaging  
**Purpose**: Final packaging modifications  
**Cleaned**: After packaging complete

**Example use cases**:
- Code signing certificates
- Store metadata
- Distribution profiles

## Configuration

### Multi-Stage Configuration Format

Create a configuration file with version `2.0` to enable multi-stage injection:

```json
{
  "version": "2.0",
  "description": "Multi-stage injection configuration",
  "injectionStages": {
    "preTest": {
      "enabled": false,
      "packages": [],
      "assemblies": [],
      "patches": [],
      "cleanupAfter": true
    },
    "preBuild": {
      "enabled": true,
      "packages": [
        {
          "name": "com.cysharp.unitask",
          "version": "2.5.10",
          "source": "build/preparation/cache/com.cysharp.unitask",
          "target": "projects/client/Packages/com.cysharp.unitask"
        }
      ],
      "cleanupAfter": false
    },
    "postBuild": {
      "enabled": false,
      "cleanupAfter": true
    },
    "preNativeBuild": {
      "enabled": false,
      "platforms": {
        "ios": {
          "enabled": false,
          "files": [],
          "patches": [],
          "commands": []
        },
        "android": {
          "enabled": false
        }
      },
      "cleanupAfter": false
    },
    "postNativeBuild": {
      "enabled": false,
      "cleanupAfter": true
    }
  }
}
```

### Legacy Configuration Support

Existing v1.0 configurations (without `injectionStages`) continue to work and map to the `preBuild` stage automatically.

## Usage

### Using Individual Stages

```bash
# Inject only pre-test dependencies
task build:prepare:inject-pre-test

# Inject only pre-build dependencies (equivalent to legacy PrepareClient)
task build:prepare:inject-pre-build

# Inject only post-build dependencies
task build:prepare:inject-post-build

# Inject only pre-native-build dependencies
task build:prepare:inject-pre-native-build
```

### Using Full Multi-Stage Workflow

```bash
# Full build with all enabled stages
task build:unity:multi-stage
```

This executes:
1. `InjectPreTest` (if enabled)
2. `TestPreBuild`
3. `CleanupPreTest` (if cleanupAfter: true)
4. `InjectPreBuild` (if enabled)
5. `BuildUnity`
6. `InjectPostBuild` (if enabled)
7. `TestPostBuild`
8. `CleanupPostBuild` (if cleanupAfter: true)
9. `CleanupAfterBuild`

### Using Legacy Workflow

```bash
# Legacy single-stage injection (still supported)
task build:prepare:client
task build:unity:with-prep
```

## Real-World Examples

### Example 1: iOS Build with CocoaPods

**Scenario**: Unity game with Firebase SDK via CocoaPods

**Configuration**:
```json
{
  "version": "2.0",
  "injectionStages": {
    "preBuild": {
      "enabled": true,
      "packages": [
        "com.cysharp.unitask",
        "com.contractwork.sangocard.game"
      ]
    },
    "preNativeBuild": {
      "enabled": true,
      "platforms": {
        "ios": {
          "enabled": true,
          "files": [
            {
              "source": "ios-dependencies/Podfile",
              "target": "Builds/iOS/Podfile"
            },
            {
              "source": "firebase/GoogleService-Info.plist",
              "target": "Builds/iOS/GoogleService-Info.plist"
            }
          ],
          "commands": [
            "pod install --project-directory=Builds/iOS"
          ]
        }
      }
    }
  }
}
```

**Workflow**:
```bash
# Build Unity project
task build:unity

# Inject CocoaPods dependencies
task build:prepare:inject-pre-native-build --build-target iOS

# Build Xcode project
task build:ios:native
```

### Example 2: Test-Driven Development

**Scenario**: Development workflow with test-specific dependencies

**Configuration**:
```json
{
  "version": "2.0",
  "injectionStages": {
    "preTest": {
      "enabled": true,
      "packages": [
        "com.unity.test-framework.performance"
      ],
      "cleanupAfter": true
    },
    "preBuild": {
      "enabled": true,
      "packages": [
        "com.cysharp.unitask"
      ]
    }
  }
}
```

**Workflow**:
```bash
# Run tests with test-specific dependencies
task build:test:validate

# Test dependencies automatically cleaned up after tests
# Build proceeds with only production dependencies
task build:unity
```

### Example 3: Android with ProGuard

**Scenario**: Android build with custom ProGuard rules

**Configuration**:
```json
{
  "version": "2.0",
  "injectionStages": {
    "preBuild": {
      "enabled": true,
      "packages": [
        "com.cysharp.unitask"
      ]
    },
    "preNativeBuild": {
      "enabled": true,
      "platforms": {
        "android": {
          "enabled": true,
          "files": [
            {
              "source": "android-dependencies/proguard-rules.pro",
              "target": "Builds/Android/proguard-rules.pro"
            }
          ],
          "patches": [
            {
              "type": "gradle",
              "file": "Builds/Android/build.gradle",
              "operations": [
                {
                  "action": "add",
                  "path": "android.buildTypes.release.minifyEnabled",
                  "value": "true"
                }
              ]
            }
          ]
        }
      }
    }
  }
}
```

## Migration from Legacy System

### Step 1: Update Configuration Version

Change `version` from `1.0` to `2.0` and wrap existing configuration in `injectionStages.preBuild`:

**Before (v1.0)**:
```json
{
  "version": "1.0",
  "packages": [...]
}
```

**After (v2.0)**:
```json
{
  "version": "2.0",
  "injectionStages": {
    "preBuild": {
      "enabled": true,
      "packages": [...]
    }
  }
}
```

### Step 2: Enable Additional Stages

Add other stages as needed:

```json
{
  "version": "2.0",
  "injectionStages": {
    "preTest": {
      "enabled": true,
      "packages": [...],
      "cleanupAfter": true
    },
    "preBuild": {
      "enabled": true,
      "packages": [...]
    }
  }
}
```

### Step 3: Update Build Commands

Replace:
```bash
task build:prepare:client
```

With:
```bash
task build:prepare:inject-pre-build
```

Or use the full multi-stage workflow:
```bash
task build:unity:multi-stage
```

## Troubleshooting

### Stage Not Running

**Symptom**: Injection stage is skipped

**Solutions**:
1. Check `enabled: true` in configuration
2. Verify stage conditions are met (e.g., Unity build completed for postBuild)
3. Check logs for stage skip reasons

### Cleanup Not Working

**Symptom**: Injected files remain after stage completes

**Solutions**:
1. Verify `cleanupAfter: true` in configuration
2. Check that injection stage succeeded
3. Use manual cleanup: `task build:prepare:restore`

### Native Build Dependencies Missing

**Symptom**: CocoaPods or Gradle dependencies not found

**Solutions**:
1. Ensure Unity build completed before preNativeBuild
2. Verify platform-specific configuration is enabled
3. Check file paths are relative to repository root
4. Verify commands execute successfully (check logs)

## Best Practices

1. **Use cleanupAfter for test stages**: Prevent test dependencies from polluting builds
2. **Keep preBuild cleanup disabled**: Allow debugging on failure
3. **Platform-specific configs**: Use separate configurations for iOS/Android
4. **Test configuration changes**: Use dry-run before applying: `task build:prepare:dry-run`
5. **Version control**: Commit multi-stage configs to repository
6. **Document stage purposes**: Use `description` fields in configuration

## See Also

- [Multi-Stage Injection Design Specification](../docs/_inbox/multi-stage-injection-design.md)
- [Build Preparation System](injection-implementation-summary.md)
- [Nuke Build Failure Handling](../docs/BUILD-REPORTING-HANDOVER.md)
