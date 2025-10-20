---
doc_id: DOC-2025-00202
title: Multi-Stage Injection System Design
doc_type: spec
status: draft
canonical: true
created: 2025-10-19
tags: [build, injection, multi-stage, ios, android, xcode, gradle]
summary: >
  Design specification for multi-stage dependency injection system supporting
  different injection points throughout the build lifecycle (pre-test, pre-build,
  post-build, pre-native-build, post-native-build).
source:
  author: agent
  agent: claude
---

# Multi-Stage Injection System Design

## Problem Statement

Current injection system injects all dependencies before Unity build starts. However, different build stages may require different dependencies or configurations:

1. **Pre-Test Phase** - Test-specific dependencies (mocks, test frameworks)
2. **Pre-Unity-Build** - Core dependencies for Unity compilation
3. **Post-Unity-Build** - Dependencies for built artifacts
4. **Pre-Native-Build** (iOS/Android) - Native project dependencies (CocoaPods, Gradle)
5. **Post-Native-Build** - Final packaging dependencies

## Current Limitation

```bash
# Current flow (single injection point)
PrepareClient → Inject ALL → TestPreBuild → BuildUnity → TestPostBuild → Cleanup
```

**Issues:**
- Test-only dependencies pollute production builds
- Native build dependencies injected too early (unused until native build)
- No way to inject platform-specific native dependencies
- Cannot clean up test dependencies before production build

## Proposed Solution: Multi-Stage Injection

### Injection Stages

#### Stage 1: Pre-Test Injection
**When**: Before `TestPreBuild`  
**Purpose**: Test-specific dependencies  
**Cleaned**: After pre-build tests complete

**Contents:**
- Test frameworks (NUnit, Unity Test Framework extensions)
- Mock libraries
- Test utilities
- Code coverage tools

#### Stage 2: Pre-Build Injection
**When**: Before `BuildUnity`  
**Purpose**: Core Unity build dependencies  
**Cleaned**: After successful build (if configured)

**Contents:**
- Production packages (UniTask, MessagePack, etc.)
- Production assemblies
- Build-time code patches

#### Stage 3: Post-Build Injection
**When**: After `BuildUnity`, before `TestPostBuild`  
**Purpose**: Runtime testing dependencies  
**Cleaned**: After post-build tests complete

**Contents:**
- Integration test helpers
- Runtime test frameworks
- Mock services for integration tests

#### Stage 4: Pre-Native-Build Injection (iOS/Android)
**When**: After Unity build, before Xcode/Gradle build  
**Purpose**: Platform-specific native dependencies  
**Cleaned**: After native build completes

**Contents for iOS:**
- CocoaPods dependencies
- Custom iOS frameworks
- Xcode project patches
- Info.plist modifications

**Contents for Android:**
- Gradle dependencies
- Custom Android libraries
- AndroidManifest.xml patches
- ProGuard configurations

#### Stage 5: Post-Native-Build Injection
**When**: After native build, before packaging  
**Purpose**: Final packaging modifications  
**Cleaned**: After packaging complete

**Contents:**
- Code signing certificates
- Store metadata
- Distribution profiles

## Architecture Design

### Configuration Structure

```json
{
  "injectionStages": {
    "preTest": {
      "enabled": true,
      "packages": [
        "com.unity.test-framework.extensions"
      ],
      "assemblies": [
        "TestUtilities.dll"
      ],
      "patches": [],
      "cleanupAfter": true
    },
    "preBuild": {
      "enabled": true,
      "packages": [
        "com.cysharp.unitask",
        "com.neuecc.messagepack"
      ],
      "assemblies": [
        "MessagePack.dll"
      ],
      "patches": [
        {
          "type": "json",
          "file": "Packages/manifest.json",
          "operations": []
        }
      ],
      "cleanupAfter": false
    },
    "postBuild": {
      "enabled": true,
      "packages": [],
      "assemblies": [
        "IntegrationTestHelpers.dll"
      ],
      "patches": [],
      "cleanupAfter": true
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
            }
          ],
          "patches": [
            {
              "type": "text",
              "file": "Builds/iOS/Info.plist",
              "operations": []
            }
          ],
          "commands": [
            "pod install --project-directory=Builds/iOS"
          ]
        },
        "android": {
          "enabled": true,
          "files": [
            {
              "source": "android-dependencies/build.gradle",
              "target": "Builds/Android/build.gradle"
            }
          ],
          "patches": [
            {
              "type": "text",
              "file": "Builds/Android/AndroidManifest.xml",
              "operations": []
            }
          ],
          "commands": [
            "gradle dependencies"
          ]
        }
      },
      "cleanupAfter": false
    },
    "postNativeBuild": {
      "enabled": true,
      "files": [],
      "patches": [],
      "cleanupAfter": true
    }
  }
}
```

### Nuke Build Targets

```csharp
// Pre-test injection
Target InjectPreTest => _ => _
    .Description("Inject test-specific dependencies")
    .Executes(() =>
    {
        RunInjection("preTest");
    });

// Pre-build injection (current behavior)
Target InjectPreBuild => _ => _
    .Description("Inject core build dependencies")
    .Executes(() =>
    {
        RunInjection("preBuild");
    });

// Post-build injection
Target InjectPostBuild => _ => _
    .Description("Inject post-build test dependencies")
    .Executes(() =>
    {
        RunInjection("postBuild");
    });

// Pre-native-build injection
Target InjectPreNativeBuild => _ => _
    .Description("Inject platform-specific native dependencies")
    .Executes(() =>
    {
        var platform = UnityBuildTarget; // "iOS" or "Android"
        RunInjection("preNativeBuild", platform);
    });

// Cleanup stages
Target CleanupPreTest => _ => _
    .AssuredAfterFailure()
    .Executes(() =>
    {
        if (ShouldCleanupStage("preTest"))
        {
            CleanupInjection("preTest");
        }
    });

Target CleanupPostBuild => _ => _
    .AssuredAfterFailure()
    .Executes(() =>
    {
        if (ShouldCleanupStage("postBuild"))
        {
            CleanupInjection("postBuild");
        }
    });
```

### Build Workflow with Multi-Stage Injection

```csharp
Target BuildiOSWithTests => _ => _
    .Description("Full iOS build with multi-stage injection")
    .DependsOn(InjectPreTest)
    .DependsOn(TestPreBuild)
    .DependsOn(CleanupPreTest)
    .DependsOn(InjectPreBuild)
    .DependsOn(BuildUnity) // Unity build to iOS Xcode project
    .DependsOn(InjectPostBuild)
    .DependsOn(TestPostBuild)
    .DependsOn(CleanupPostBuild)
    .DependsOn(InjectPreNativeBuild) // iOS-specific injection
    .DependsOn(BuildNativeiOS) // Xcode build
    .DependsOn(CleanupAfterBuild)
    .Executes(() =>
    {
        Serilog.Log.Information("✅ Full iOS build with tests complete");
    });
```

## Use Cases

### Use Case 1: iOS Build with CocoaPods

```
Flow: InjectPreBuild → BuildUnity (generates Xcode project) →
      InjectPreNativeBuild (inject Podfile, run pod install) →
      BuildNativeiOS (xcodebuild)
```

**Benefits:**
- Podfile only injected when Xcode project exists
- Native dependencies managed separately from Unity dependencies
- Clean separation of Unity vs native dependencies

### Use Case 2: Android Build with Custom Gradle

```
Flow: InjectPreBuild → BuildUnity (generates Gradle project) →
      InjectPreNativeBuild (inject build.gradle modifications) →
      BuildNativeAndroid (gradle build)
```

**Benefits:**
- Gradle dependencies injected after Unity export
- Platform-specific configurations separate from Unity build
- Support for custom native libraries

### Use Case 3: Test-Only Dependencies

```
Flow: InjectPreTest (test frameworks) → TestPreBuild →
      CleanupPreTest (remove test deps) → InjectPreBuild →
      BuildUnity (clean production build)
```

**Benefits:**
- Test dependencies don't pollute production builds
- Faster production builds (no test overhead)
- Clear separation of test vs production dependencies

### Use Case 4: Integration Tests with Service Mocks

```
Flow: BuildUnity → InjectPostBuild (mock services) →
      TestPostBuild (integration tests with mocks) →
      CleanupPostBuild (remove mocks)
```

**Benefits:**
- Runtime test dependencies only present during testing
- Production artifacts remain clean
- Support for different test environments

## Implementation Phases

### Phase 1: Foundation (Current)
- ✅ Single pre-build injection
- ✅ Basic cleanup after build
- ✅ Git reset mechanism

### Phase 2: Multi-Stage Core
- 🔲 Add pre-test injection stage
- 🔲 Add post-build injection stage
- 🔲 Stage-specific cleanup targets
- 🔲 Configuration schema for multiple stages

### Phase 3: Native Build Support
- 🔲 Add pre-native-build injection
- 🔲 Platform-specific injection (iOS/Android)
- 🔲 Command execution support (pod install, gradle)
- 🔲 Native project patching

### Phase 4: Advanced Features
- 🔲 Post-native-build injection
- 🔲 Conditional injection based on build type
- 🔲 Injection templates
- 🔲 Dependency conflict detection

## Configuration Examples

### Example 1: Test-Driven Development

```json
{
  "injectionStages": {
    "preTest": {
      "enabled": true,
      "packages": [
        "com.unity.test-framework.performance",
        "com.unity.test-framework.ui"
      ],
      "cleanupAfter": true
    },
    "preBuild": {
      "enabled": true,
      "packages": [
        "com.cysharp.unitask"
      ],
      "cleanupAfter": false
    }
  }
}
```

### Example 2: iOS with Firebase

```json
{
  "injectionStages": {
    "preBuild": {
      "enabled": true,
      "packages": ["com.cysharp.unitask"]
    },
    "preNativeBuild": {
      "enabled": true,
      "platforms": {
        "ios": {
          "enabled": true,
          "files": [
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

### Example 3: Android with ProGuard

```json
{
  "injectionStages": {
    "preNativeBuild": {
      "enabled": true,
      "platforms": {
        "android": {
          "enabled": true,
          "files": [
            {
              "source": "proguard/proguard-rules.pro",
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

## Benefits

### Developer Experience
- Clear separation of concerns (test vs build vs native)
- Faster iteration (only inject what's needed)
- Easier debugging (cleaner dependency graph)

### Build Performance
- Reduced injection overhead (stage-specific)
- Faster test cycles (test deps separate)
- Parallel-friendly (independent stages)

### Maintainability
- Modular configuration (stage-based)
- Platform-specific handling (iOS/Android)
- Clear cleanup boundaries

### CI/CD Integration
- Fine-grained control over build stages
- Better caching opportunities (stage-specific)
- Flexible workflow composition

## Migration Strategy

### Step 1: Backward Compatibility
Keep `PrepareClient` as alias for `InjectPreBuild` to maintain existing workflows.

### Step 2: Gradual Adoption
Introduce new stages as opt-in, with current behavior as default.

### Step 3: Configuration Migration
Provide migration tool to convert single-stage configs to multi-stage.

### Step 4: Full Migration
Once all projects migrated, deprecate single-stage injection.

## Open Questions

1. **Cleanup Granularity**: Should cleanup be automatic or manual per stage?
   - Proposal: Automatic with `cleanupAfter` flag, manual override available

2. **Stage Dependencies**: Should stages have explicit dependencies?
   - Proposal: Yes, use Nuke's `.DependsOn()` for ordering

3. **Partial Injection**: Support injecting only subset of stage?
   - Proposal: Use `enabled` flag per stage and per platform

4. **Injection Rollback**: What if native build fails after injection?
   - Proposal: `AssuredAfterFailure` cleanup targets

5. **Caching Strategy**: How to cache between stages?
   - Proposal: Stage-specific cache directories

## See Also

- [Build Preparation Injection System](injection-implementation-summary.md)
- [Nuke Build Failure Handling](nuke-failure-handling.md)
- `.agent/base/40-build.md` - Build rules and guidelines
