# SPEC AMENDMENT: Build Preparation Tool - Two-Phase Workflow

**Amendment ID:** SPEC-BPT-001-AMD-001  
**Parent Spec:** SPEC-BPT-001  
**Status:** Complete  
**Created:** 2025-10-17  
**Completed:** 2025-10-17  
**Reason:** Align with R-BLD-060 (projects/client is read-only outside build operations)

## Summary

Modify the preparation workflow to use a two-phase approach:

1. **Phase 1 (Cache Preparation):** Gather references into `build/preparation/cache/`
2. **Phase 2 (Injection):** Copy from cache to `projects/client/` during build only

This ensures `projects/client/` is never modified outside the build process, respecting the standalone Git repository constraint.

## Problem Statement

Current implementation violates R-BLD-060:

- `prepare run` directly modifies `projects/client/`
- No separation between cache preparation and client injection
- Cannot be safely run outside build execution

**Current Flow:**

```
tool prepare run --config <path>
  ├─ Reads from: build/preparation/cache/
  └─ Writes to: projects/client/ ❌ VIOLATES R-BLD-060
```

**Required Flow:**

```
# Before build (safe, no client modification)
tool cache populate --source projects/code-quality

# During build only (Nuke controls this)
tool prepare inject --config <path> --target projects/client/
  ├─ Reads from: build/preparation/cache/
  └─ Writes to: projects/client/ ✅ OK during build
```

## Changes to User Stories

### US-2: Modified - Two-Phase Build Preparation

**Priority:** High  
**Story Points:** 6 (increased from 5)

**Acceptance Criteria:**

- [ ] Run cache preparation with `tool cache populate --source <path> [--config <path>]`
- [ ] Run injection with `tool prepare inject --config <path> --target <path>`
- [ ] `cache populate` MUST NOT modify target project
- [ ] `prepare inject` MUST validate target is within allowed paths
- [ ] Exit code 0 on success, non-zero on failure
- [ ] Progress output to stdout
- [ ] Errors to stderr
- [ ] Support dry-run mode for both phases
- [ ] Complete in < 30 seconds for typical project

**Tasks:**

- [ ] Rename `prepare run` to `prepare inject`
- [ ] Add `--target` parameter to `prepare inject`
- [ ] Add path validation (ensure target is `projects/client/`)
- [ ] Update `cache populate` to work standalone
- [ ] Implement System.CommandLine commands
- [ ] Implement PrepareCommandHandler with two-phase support
- [ ] Implement progress reporting
- [ ] Implement error handling
- [ ] Add dry-run support for both phases
- [ ] Update documentation and examples

### New US-7: As a build system, I want safe two-phase preparation

**Priority:** Critical  
**Story Points:** 3

**Acceptance Criteria:**

- [ ] Cache preparation can run anytime without modifying client
- [ ] Injection only runs during Nuke build execution
- [ ] Injection validates target path is `projects/client/`
- [ ] Clear error if injection attempted outside build context
- [ ] Nuke integration performs `git reset --hard` before injection

**Tasks:**

- [ ] Add target path validation in PrepareCommandHandler
- [ ] Document safe usage patterns
- [ ] Update Nuke integration (Task 6.1)
- [ ] Add integration tests for two-phase workflow

## Technical Changes

### Command Structure

**Before:**

```bash
tool prepare run --config <path>
```

**After:**

```bash
# Phase 1: Cache preparation (safe anytime)
tool cache populate --source projects/code-quality [--config <path>]

# Phase 2: Injection (build-time only)
tool prepare inject --config <path> --target projects/client/
```

### Configuration Model

No changes to `PreparationConfig.cs` - paths remain relative to git root.

### Service Changes

**PreparationService.cs:**

- Rename `ExecuteAsync()` to `InjectAsync()`
- Add `targetBasePath` parameter for validation
- Validate all target paths start with `targetBasePath`
- Throw exception if target outside allowed path

**CacheService.cs:**

- Ensure `PopulateAsync()` never touches client project
- Only writes to `build/preparation/cache/`

### CLI Changes

**PrepareCommandHandler.cs:**

```csharp
// Old: prepare run
public async Task RunAsync(string configRelativePath, ...)

// New: prepare inject
public async Task InjectAsync(
    string configRelativePath,
    string targetPath,  // NEW: must be "projects/client/"
    string validationLevel = "full",
    bool verbose = false,
    bool force = false)
{
    // Validate target path
    if (!targetPath.StartsWith("projects/client/"))
    {
        throw new ArgumentException(
            "Target must be 'projects/client/' per R-BLD-060");
    }

    // Rest of implementation...
}
```

## Nuke Integration Changes (Task 6.1)

**Build.Preparation.cs:**

```csharp
Target PrepareCache => _ => _
    .Description("Populate preparation cache (safe anytime)")
    .Executes(() =>
    {
        DotNetRun(s => s
            .SetProjectFile(ToolProject)
            .SetApplicationArguments(
                "cache populate " +
                "--source projects/code-quality " +
                $"--config {ConfigPath}")
        );
    });

Target PrepareClient => _ => _
    .Description("Inject preparation into client (build-time only)")
    .DependsOn(PrepareCache)
    .Executes(() =>
    {
        // R-BLD-060: Reset client before injection
        Git("reset --hard", workingDirectory: ClientPath);

        DotNetRun(s => s
            .SetProjectFile(ToolProject)
            .SetApplicationArguments(
                "prepare inject " +
                $"--config {ConfigPath} " +
                "--target projects/client/")
        );
    });

Target RestoreClient => _ => _
    .Description("Restore client to clean state")
    .Executes(() =>
    {
        Git("reset --hard", workingDirectory: ClientPath);
    });
```

## Validation Rules

### Phase 1: Cache Populate

- ✅ Can run anytime
- ✅ Can run multiple times
- ✅ Only writes to `build/preparation/cache/`
- ❌ MUST NOT touch `projects/client/`

### Phase 2: Inject

- ✅ Only runs during Nuke build
- ✅ Requires cache to be populated
- ✅ Only writes to `projects/client/`
- ❌ MUST NOT run outside build context
- ❌ Target path MUST be validated

## Migration Path

### Existing Behavior

```bash
# Old command (deprecated)
tool prepare run --config <path>
```

### New Behavior

```bash
# Step 1: Populate cache
tool cache populate --source projects/code-quality

# Step 2: Inject (during build only)
tool prepare inject --config <path> --target projects/client/
```

### Backward Compatibility

**Option 1: Deprecation Warning (Recommended)**

- Keep `prepare run` with deprecation warning
- Redirect to `prepare inject` with default target
- Remove in next major version

**Option 2: Breaking Change**

- Remove `prepare run` immediately
- Force migration to two-phase workflow
- Update all documentation

**Recommendation:** Option 1 for smoother transition.

## Testing Requirements

### Unit Tests

- [ ] PrepareCommandHandler validates target path
- [ ] CacheService never touches client
- [ ] InjectAsync rejects invalid target paths

### Integration Tests

- [ ] Two-phase workflow end-to-end
- [ ] Cache populate → inject → build succeeds
- [ ] Inject without cache fails gracefully
- [ ] Inject with invalid target fails

### E2E Tests

- [ ] Full Nuke build with two-phase preparation
- [ ] Git reset before injection works
- [ ] Restore after build works

## Documentation Updates

### Files to Update

- [ ] `.specify/specs/build-preparation-tool.md` (US-2)
- [ ] `.specify/tasks/build-preparation-tool-tasks.md` (Task 4.4)
- [ ] `packages/.../tool/README.md`
- [ ] `docs/rfcs/RFC-001-build-preparation-tool.md`
- [ ] `build/nuke/build/Components/README.md`

### New Documentation

- [ ] Two-phase workflow guide
- [ ] Safe usage patterns
- [ ] Migration guide from old commands

## Success Metrics

- [ ] `projects/client/` never modified outside build
- [ ] Cache preparation runs in < 10 seconds
- [ ] Injection runs in < 20 seconds
- [ ] Zero accidental client modifications
- [ ] All builds use two-phase workflow

## Risks & Mitigation

### Risk 1: Breaking Existing Workflows

**Mitigation:** Deprecation period with warnings, clear migration guide

### Risk 2: Confusion About Two Phases

**Mitigation:** Clear documentation, error messages guide users

### Risk 3: Nuke Integration Complexity

**Mitigation:** Well-tested integration, clear examples

## Approval

**Amendment Author:** Build System Team  
**Reviewed By:** [Pending]  
**Approved By:** [Pending]  
**Date:** [Pending]

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-17 | 0.1.0 | Initial amendment | Build System Team |
