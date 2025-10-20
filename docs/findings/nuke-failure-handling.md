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

### 2. Test Build with Pre/Post-Build Phases

#### Pre-Build Tests (Validation) - Fast Feedback

**Purpose:** Validate code before expensive build operation

```csharp
Target TestPreBuild => _ => _
    .DependsOn(TestEditMode)       // Unit tests (no build required)
    .DependsOn(TestCodeQuality)    // Static analysis, linting
    .ProceedAfterFailure()         // Continue even if some fail
    .Executes(() => { /* Report validation results */ });
```

**Pre-Build Test Types:**
- **TestEditMode** - Unity Edit Mode tests (unit tests)
- **TestCodeQuality** - Static analysis, code quality checks
- **Benefit** - Catch issues early, before wasting time on build

#### Post-Build Tests (Runtime) - Artifact Validation

**Purpose:** Validate the actual built artifact

```csharp
Target TestPostBuild => _ => _
    .DependsOn(TestPlayMode)       // Runtime tests
    .DependsOn(TestIntegration)    // Integration tests
    .ProceedAfterFailure()         // Continue even if some fail
    .Executes(() => { /* Report runtime test results */ });
```

**Post-Build Test Types:**
- **TestPlayMode** - Unity Play Mode tests (runtime behavior)
- **TestIntegration** - Integration tests on built executable
- **Benefit** - Comprehensive validation of deployable artifact

#### BuildWithTests Workflow - Full Pipeline

```csharp
Target BuildWithTests => _ => _
    .DependsOn(PrepareClient)
    .DependsOn(((ITestBuild)this).TestPreBuild)      // Validate first
    .DependsOn(((IUnityBuild)this).BuildUnity)       // Then build
    .DependsOn(((ITestBuild)this).TestPostBuild)     // Then test artifact
    .DependsOn(CleanupAfterBuild)                    // Always cleanup
    .Executes(() =>
    {
        var preBuildOK = SucceededTargets.Contains(TestPreBuild);
        var buildOK = SucceededTargets.Contains(BuildUnity);
        var postBuildOK = SucceededTargets.Contains(TestPostBuild);

        if (preBuildOK && buildOK && postBuildOK)
        {
            Serilog.Log.Information("✅ All stages passed");
        }
    });
```

**Flow:**
1. `PrepareClient` - Inject dependencies
2. `TestPreBuild` - Validate code (fast, catch issues early)
3. `BuildUnity` - Build project (only if validation passes)
4. `TestPostBuild` - Test built artifact (comprehensive)
5. `CleanupAfterBuild` - Clean if build succeeded (always runs)

#### ValidateOnly Workflow - Fast Development Loop

```csharp
Target ValidateOnly => _ => _
    .DependsOn(PrepareClient)
    .DependsOn(((ITestBuild)this).TestPreBuild)
    .DependsOn(CleanupAfterBuild)
    .Executes(() => { /* Quick validation feedback */ });
```

**Purpose:** Rapid iteration during development  
**Flow:** Inject → Validate → Cleanup (no expensive build)  
**Use Case:** Edit code → Run validation → Fix issues → Repeat

## Usage

### Build Without Tests (Fast)

```bash
# Standard build with automatic cleanup
task build:prepare:client

# Or directly via Nuke
nuke BuildUnityWithPreparation
```

### Build With Full Test Suite

```bash
# Pre-build validation → Build → Post-build testing
nuke BuildWithTests
```

### Validation Only (No Build)

```bash
# Fast feedback during development
nuke ValidateOnly
```

**Use Cases:**
- Quick code validation before committing
- Rapid iteration: edit → validate → fix → repeat
- Pre-commit hooks (fast validation)

### Manual Test Execution

```bash
# Pre-build tests only (validation)
nuke TestPreBuild

# Post-build tests only (runtime, requires built artifact)
nuke TestPostBuild

# All tests (pre + post)
nuke TestAll
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
