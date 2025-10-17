# Amendment 001 Summary: Two-Phase Workflow

**Status:** Proposed  
**Created:** 2025-10-17  
**Ready for Implementation:** Yes

## Quick Summary

We're modifying the build preparation tool to use a **two-phase workflow** to comply with R-BLD-060 (projects/client is read-only outside build operations).

## The Change

### Before (Current - Violates R-BLD-060)

```bash
tool prepare run --config build/preparation/configs/default.json
# ❌ Directly modifies projects/client/ outside build context
```

### After (Proposed - Complies with R-BLD-060)

```bash
# Phase 1: Populate cache (safe anytime, no client modification)
tool cache populate --source projects/code-quality

# Phase 2: Inject to client (ONLY during Nuke build)
tool prepare inject --config <path> --target projects/client/
# ✅ Only modifies projects/client/ during build
# ✅ Nuke performs git reset --hard before injection
```

## Why This Change?

**Problem:** `projects/client` is a standalone Git repository that must remain clean except during builds.

**Current Issue:** The tool directly modifies `projects/client/` when running `prepare run`, which can happen outside build context.

**Solution:** Separate cache preparation (safe) from client injection (build-time only).

## Documents Created

1. **Spec Amendment:** `.specify/specs/build-preparation-tool-amendment-001.md`
   - Updates US-2 (Build System CLI)
   - Adds US-7 (Safe Two-Phase Preparation)
   - Details technical changes

2. **Tasks Amendment:** `.specify/tasks/build-preparation-tool-tasks-amendment-001.md`
   - Modifies Task 4.4 (Prepare Commands)
   - Modifies Task 6.1 (Nuke Integration)
   - Adds Task 4.4.1 (Deprecation Support)

3. **This Summary:** Quick reference for implementation

## Implementation Checklist

### Phase 1: Update Tool Commands (Task 4.4 Modified)

- [ ] Rename `prepare run` → `prepare inject`
- [ ] Add `--target` parameter (required)
- [ ] Add target path validation (must be `projects/client/`)
- [ ] Add cache existence validation
- [ ] Keep `prepare run` with deprecation warning
- [ ] Update help text
- [ ] Update tests

**Files to Modify:**

- `PrepareCommandHandler.cs`
- `CliHost.cs`
- `PreparationService.cs` (rename `ExecuteAsync` → `InjectAsync`)

### Phase 2: Update Nuke Integration (Task 6.1 Modified)

- [ ] Create `Build.Preparation.cs` partial class
- [ ] Implement `PrepareCache` target
- [ ] Implement `PrepareClient` target (with git reset)
- [ ] Implement `RestoreClient` target
- [ ] Add dependency chain
- [ ] Add logging
- [ ] Test full workflow

**Files to Create:**

- `build/nuke/build/Build.Preparation.cs`

### Phase 3: Documentation

- [ ] Update spec (main document)
- [ ] Update tasks (main document)
- [ ] Update RFC-001
- [ ] Update tool README
- [ ] Create migration guide
- [ ] Update Nuke component README

## Usage Examples

### Developer Workflow

```bash
# Populate cache (can run anytime)
cd D:\lunar-snake\constract-work\card-projects\sango-card
dotnet run --project packages/scoped-6571/.../tool/SangoCard.Build.Tool \
  -- cache populate --source projects/code-quality

# Build with Nuke (handles injection automatically)
nuke BuildWithPreparation
```

### Nuke Build Workflow

```bash
# Full build with preparation
nuke BuildWithPreparation
  ├─ PrepareCache (Phase 1)
  ├─ PrepareClient (Phase 2 with git reset)
  ├─ BuildUnity
  └─ RestoreClient (git reset)

# Or individual targets
nuke PrepareCache      # Safe anytime
nuke PrepareClient     # Build-time only
nuke RestoreClient     # Cleanup
```

### CI/CD Workflow

```yaml
# GitHub Actions example
- name: Populate Cache
  run: |
    dotnet run --project $TOOL_PROJECT \
      -- cache populate --source projects/code-quality

- name: Build Unity
  run: nuke BuildWithPreparation
```

## Key Constraints

### Phase 1: Cache Populate

✅ **CAN:**

- Run anytime
- Run multiple times
- Write to `build/preparation/cache/`

❌ **CANNOT:**

- Modify `projects/client/`
- Modify any Unity project files

### Phase 2: Inject

✅ **CAN:**

- Run during Nuke build only
- Write to `projects/client/`
- Assume cache is populated

❌ **CANNOT:**

- Run outside build context
- Target any path except `projects/client/`
- Run without cache being populated

## Validation Rules

### Target Path Validation

```csharp
// Must normalize and validate
var normalized = targetPath.Replace('\\', '/').TrimEnd('/');
if (normalized != "projects/client")
{
    throw new ArgumentException("Target must be 'projects/client/' per R-BLD-060");
}
```

### Cache Validation

```csharp
// Before injection, verify cache exists
foreach (var pkg in config.Packages)
{
    if (!File.Exists(Path.Combine(gitRoot, pkg.Source)))
    {
        throw new FileNotFoundException($"Cache not found: {pkg.Source}. Run 'cache populate' first.");
    }
}
```

## Testing Requirements

### Unit Tests

```csharp
[Fact] void InjectAsync_RequiresTargetParameter()
[Fact] void InjectAsync_ValidatesTargetIsClient()
[Fact] void InjectAsync_RejectsInvalidTarget()
[Fact] void InjectAsync_ValidatesCacheExists()
[Fact] void CachePopulate_NeverTouchesClient()
```

### Integration Tests

```csharp
[Fact] void TwoPhaseWorkflow_PopulateThenInject_Succeeds()
[Fact] void InjectWithoutCache_FailsGracefully()
[Fact] void InjectWithInvalidTarget_Throws()
```

### E2E Tests

```csharp
[Fact] void NukeBuild_WithPreparation_PerformsGitReset()
[Fact] void NukeBuild_FullWorkflow_Succeeds()
[Fact] void RestoreClient_ResetsToClean()
```

## Migration Path

### For Existing Scripts

**Old:**

```bash
tool prepare run --config build/preparation/configs/default.json
```

**New:**

```bash
# Option 1: Use deprecated command (shows warning)
tool prepare run --config build/preparation/configs/default.json

# Option 2: Migrate to new commands (recommended)
tool cache populate --source projects/code-quality
tool prepare inject --config build/preparation/configs/default.json --target projects/client/
```

### Deprecation Timeline

- **v1.1.0:** Add `prepare inject`, deprecate `prepare run`
- **v1.2.0:** Remove `prepare run` (breaking change)

## Success Metrics

- [ ] Zero modifications to `projects/client/` outside builds
- [ ] Cache preparation < 10 seconds
- [ ] Injection < 20 seconds
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Migration guide available

## Next Steps

1. **Review amendments** with team
2. **Approve amendments** (update status to "Approved")
3. **Implement Task 4.4 modifications** (PrepareCommandHandler)
4. **Implement Task 6.1 modifications** (Nuke integration)
5. **Test two-phase workflow**
6. **Update documentation**
7. **Deploy and monitor**

## Questions?

- **Q: Can I still use `prepare run`?**
  - A: Yes, but it's deprecated. It will show a warning and redirect to `prepare inject`.

- **Q: What if I forget to populate cache?**
  - A: `prepare inject` will fail with clear error: "Cache not found. Run 'cache populate' first."

- **Q: Can I inject to a different target?**
  - A: No. Only `projects/client/` is allowed per R-BLD-060.

- **Q: When should I run `PrepareCache`?**
  - A: Anytime! It's safe and doesn't modify the client. Run it when dependencies change.

- **Q: When should I run `PrepareClient`?**
  - A: Only during Nuke builds. Never manually. Let Nuke handle it.

## Related Rules

- **R-BLD-060:** Never modify `projects/client` outside build operations
- **R-SPEC-010:** Follow spec-kit workflow for features
- **R-BLD-010:** Use Task runner for builds

## Approval Status

- [ ] Spec Amendment Approved
- [ ] Tasks Amendment Approved
- [ ] Ready for Implementation

---

**Created:** 2025-10-17  
**Author:** Build System Team  
**Next Review:** After team approval
