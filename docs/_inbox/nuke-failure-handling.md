---
doc_id: DOC-2025-00201
title: Nuke Build Failure Handling with AssuredAfterFailure and ProceedAfterFailure
doc_type: guide
status: active
canonical: true
created: 2025-10-19
tags: [nuke, build, testing, failure-handling, cleanup]
summary: >
  Implementation guide for robust failure handling in Nuke build workflows using
  AssuredAfterFailure for cleanup and ProceedAfterFailure for test continuation.
source:
  author: agent
  agent: claude
---

# Nuke Build Failure Handling

## Overview

Enhanced Nuke build system with explicit failure handling using `AssuredAfterFailure` and `ProceedAfterFailure` attributes. Ensures proper cleanup after builds and allows test execution to continue even when tests fail.

## Architecture

### Key Concepts

**AssuredAfterFailure**
- Target executes regardless of dependency success/failure
- Guarantees cleanup logic runs
- Used for: Post-build cleanup, resource deallocation, state restoration

**ProceedAfterFailure**
- Allows workflow to continue past failed targets
- Non-failing targets still execute
- Used for: Test suites, optional validation steps

## Implementation

### 1. Build Preparation with Cleanup

#### CleanupAfterBuild Target

```csharp
Target CleanupAfterBuild => _ => _
    .AssuredAfterFailure()
    .After(((IUnityBuild)this).BuildUnity)
    .OnlyWhenDynamic(() => IsServerBuild || SucceededTargets.Contains(((IUnityBuild)this).BuildUnity))
    .Executes(() =>
    {
        if (SucceededTargets.Contains(((IUnityBuild)this).BuildUnity))
        {
            // Build succeeded - clean up injected files
            Git("reset --hard", workingDirectory: ClientProject);
        }
        else
        {
            // Build failed - preserve state for debugging
            Serilog.Log.Warning("Client state preserved for debugging");
        }
    });
```

**Behavior:**
- ✅ Build succeeds → `git reset --hard` cleans client
- ✅ Build fails → Injected files preserved for debugging
- ✅ Always runs (guaranteed by `AssuredAfterFailure`)

#### BuildUnityWithPreparation Workflow

```csharp
Target BuildUnityWithPreparation => _ => _
    .DependsOn(PrepareClient)
    .DependsOn(((IUnityBuild)this).BuildUnity)
    .DependsOn(CleanupAfterBuild)
    .Executes(() => { /* Summary logging */ });
```

**Flow:**
1. `PrepareCache` - Populate cache from code-quality
2. `PrepareClient` - Git reset → Inject packages/assemblies
3. `BuildUnity` - Build Unity project
4. `CleanupAfterBuild` - Conditional cleanup (always runs)

### 2. Test Build with Failure Tolerance

#### ITestBuild Interface

```csharp
interface ITestBuild : INukeBuild
{
    Target TestAll => _ => _
        .DependsOn(TestEditMode)
        .DependsOn(TestPlayMode)
        .ProceedAfterFailure()  // Continue even if tests fail
        .Executes(() =>
        {
            if (FailedTargets.Count > 0)
            {
                Serilog.Log.Warning("Some tests failed");
            }
        });
}
```

**Behavior:**
- EditMode tests run
- PlayMode tests run (even if EditMode failed)
- Build continues regardless of test results

#### BuildWithTests Workflow

```csharp
Target BuildWithTests => _ => _
    .DependsOn(PrepareClient)
    .DependsOn(((IUnityBuild)this).BuildUnity)
    .DependsOn(((ITestBuild)this).TestAll)
    .DependsOn(CleanupAfterBuild)
    .Executes(() =>
    {
        var buildSucceeded = SucceededTargets.Contains(((IUnityBuild)this).BuildUnity);
        var testsSucceeded = SucceededTargets.Contains(((ITestBuild)this).TestAll);

        if (buildSucceeded && !testsSucceeded)
        {
            Serilog.Log.Warning("Build OK but tests failed");
        }
    });
```

**Flow:**
1. `PrepareClient` - Inject dependencies
2. `BuildUnity` - Build project
3. `TestAll` - Run all tests (continues on failure)
4. `CleanupAfterBuild` - Clean if build succeeded (always runs)

## Usage

### Build Without Tests

```bash
# Standard build with automatic cleanup
task build:prepare:client

# Or directly via Nuke
nuke BuildUnityWithPreparation
```

### Build With Tests

```bash
# Build + tests (continues if tests fail)
nuke BuildWithTests
```

### Manual Cleanup

```bash
# After investigating a failed build
task build:prepare:restore

# Or directly
nuke RestoreClient
```

## Benefits

### Explicit Failure Handling

**Before (implicit):**
```csharp
.Executes(() => {
    // Cleanup here - only runs if dependencies succeed
    Git("reset --hard");
});
```

**After (explicit):**
```csharp
.AssuredAfterFailure()
.OnlyWhenDynamic(() => SucceededTargets.Contains(BuildUnity))
.Executes(() => {
    // Guaranteed to run, with explicit success check
});
```

### Test Continuation

**Before:** Tests stop at first failure
**After:** All test suites run, comprehensive results

### Debugging Support

**Build failure:** Injected state preserved automatically
**Manual cleanup:** Available via `RestoreClient` target

## File Structure

```text
build/nuke/build/
├── Build.Preparation.cs              # Preparation targets with CleanupAfterBuild
├── Build.TestBuild.cs                # Test workflow implementation
├── Components/
│   └── ITestBuild.cs                 # Test interface with ProceedAfterFailure
```

## Target Attributes Reference

| Attribute | Purpose | Example Use Case |
|-----------|---------|------------------|
| `AssuredAfterFailure()` | Runs regardless of dependency results | Cleanup, resource deallocation |
| `ProceedAfterFailure()` | Allows continuation past failures | Test suites, multi-step validation |
| `OnlyWhenDynamic()` | Conditional execution at runtime | Success-dependent cleanup |
| `.After()` | Execution ordering without dependency | Cleanup after build |

## Best Practices

1. **Use AssuredAfterFailure for cleanup**
   - Resource deallocation
   - State restoration
   - Temporary file removal

2. **Use ProceedAfterFailure for tests**
   - Run all test suites
   - Gather comprehensive results
   - Don't fail fast

3. **Combine with OnlyWhenDynamic**
   - Check `SucceededTargets` / `FailedTargets`
   - Conditional cleanup based on build success
   - Preserve debugging state on failure

4. **Document behavior clearly**
   - Explain why targets use these attributes
   - Describe success/failure scenarios
   - Guide users on manual cleanup

## Compliance

- ✅ **R-BLD-060**: Git reset before injection
- ✅ **R-CODE-090**: Partial class separation
- ✅ Explicit failure handling
- ✅ Debugging support (preserved state on failure)
- ✅ Test continuation (comprehensive results)

## See Also

- [Nuke Build Targets Documentation](https://nuke.build/docs/fundamentals/targets/)
- [Build Preparation Injection System](injection-implementation-summary.md)
- `.agent/base/40-build.md` - Build rules and guidelines
