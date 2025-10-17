---
doc_id: DOC-2025-00169
title: Build Tool Migration Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [build-tool-migration-guide]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00119
title: Build Tool Migration Guide
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [build-tool-migration-guide]
summary: >
  (Add summary here)
source:
  author: system
---
# Migration Guide: Two-Phase Workflow

**Version:** 1.1.0  
**Date:** 2025-10-17  
**Status:** Active

## Overview

This guide helps you migrate from the old single-phase `prepare run` command to the new two-phase workflow (`cache populate` + `prepare inject`).

## Why Migrate?

### Old Approach (Deprecated)

```bash
tool prepare run --config <path>
```

**Problems:**

- ❌ Modifies `projects/client/` outside build operations (violates R-BLD-060)
- ❌ No separation between cache preparation and client injection
- ❌ Cannot run cache population independently
- ❌ Unsafe for development workflows

### New Approach (Recommended)

```bash
# Phase 1: Populate cache (safe anytime)
tool cache populate --source projects/code-quality

# Phase 2: Inject to client (build-time only)
tool prepare inject --config <path> --target projects/client/
```

**Benefits:**

- ✅ Complies with R-BLD-060 (client never modified outside builds)
- ✅ Clear separation of concerns
- ✅ Cache can be prepared independently
- ✅ Git reset before injection ensures clean state
- ✅ Safer for development workflows

## Migration Steps

### Step 1: Update Scripts

**Before:**

```bash
#!/bin/bash
# Old script
tool prepare run --config build/preparation/configs/default.json
```

**After:**

```bash
#!/bin/bash
# New script - two-phase workflow

# Phase 1: Populate cache (safe anytime)
tool cache populate --source projects/code-quality

# Phase 2: Inject to client (build-time only)
tool prepare inject \
  --config build/preparation/configs/default.json \
  --target projects/client/
```

### Step 2: Update Nuke Builds

**Before:**

```csharp
Target PrepareBuild => _ => _
    .Executes(() =>
    {
        DotNetRun(s => s
            .SetProjectFile(ToolProject)
            .SetApplicationArguments("prepare run --config ...")
        );
    });
```

**After:**

```csharp
Target PrepareCache => _ => _
    .Description("Phase 1: Populate cache")
    .Executes(() =>
    {
        DotNetRun(s => s
            .SetProjectFile(ToolProject)
            .SetApplicationArguments("cache populate --source projects/code-quality")
        );
    });

Target PrepareClient => _ => _
    .Description("Phase 2: Inject to client")
    .DependsOn(PrepareCache)
    .Executes(() =>
    {
        // R-BLD-060: Reset client before injection
        Git("reset --hard", workingDirectory: ClientProject);

        DotNetRun(s => s
            .SetProjectFile(ToolProject)
            .SetApplicationArguments(
                "prepare inject " +
                "--config build/preparation/configs/default.json " +
                "--target projects/client/")
        );
    });
```

### Step 3: Update CI/CD Pipelines

**Before (GitHub Actions):**

```yaml
- name: Prepare Build
  run: |
    dotnet run --project $TOOL_PROJECT \
      -- prepare run --config build/preparation/configs/default.json
```

**After (GitHub Actions):**

```yaml
- name: Phase 1 - Populate Cache
  run: |
    dotnet run --project $TOOL_PROJECT \
      -- cache populate --source projects/code-quality

- name: Phase 2 - Inject to Client
  run: |
    cd projects/client
    git reset --hard
    cd ../..
    dotnet run --project $TOOL_PROJECT \
      -- prepare inject \
         --config build/preparation/configs/default.json \
         --target projects/client/
```

**Or use Nuke (Recommended):**

```yaml
- name: Build with Preparation
  run: nuke BuildUnityWithPreparation
```

### Step 4: Update Documentation

Update any internal documentation, wikis, or runbooks that reference the old command.

## Backward Compatibility

### Deprecation Period

The old `prepare run` command is **deprecated but still works** in v1.1.0:

```bash
tool prepare run --config <path>

# Output:
# ⚠️  WARNING: 'prepare run' is deprecated.
#    Use 'prepare inject --target projects/client/' instead.
#    This command will be removed in the next major version.
```

**Timeline:**

- **v1.1.0:** Deprecated, shows warning, still works
- **v2.0.0:** Removed (breaking change)

### Gradual Migration

You can migrate gradually:

1. **Week 1:** Update development scripts
2. **Week 2:** Update CI/CD pipelines
3. **Week 3:** Update Nuke builds
4. **Week 4:** Verify all old commands removed

## Command Mapping

| Old Command | New Commands | Notes |
|-------------|--------------|-------|
| `prepare run --config <path>` | `cache populate --source <src>`<br>`prepare inject --config <path> --target projects/client/` | Two-phase workflow |
| N/A | `prepare inject --dry-run` | New: Preview changes |
| N/A | `nuke PrepareCache` | New: Nuke target |
| N/A | `nuke PrepareClient` | New: Nuke target with git reset |
| N/A | `nuke RestoreClient` | New: Cleanup target |

## Common Migration Scenarios

### Scenario 1: Local Development

**Before:**

```bash
# Developer workflow
tool prepare run --config my-config.json
# Build Unity manually
```

**After:**

```bash
# Developer workflow
tool cache populate --source projects/code-quality  # Once
nuke BuildUnityWithPreparation  # Handles everything
```

### Scenario 2: CI/CD Build

**Before:**

```yaml
steps:
  - name: Prepare
    run: tool prepare run --config default.json
  - name: Build
    run: unity-build.sh
```

**After:**

```yaml
steps:
  - name: Build with Preparation
    run: nuke BuildUnityWithPreparation
```

### Scenario 3: Manual Testing

**Before:**

```bash
# Test preparation
tool prepare run --config test-config.json
```

**After:**

```bash
# Test preparation
tool cache populate --source projects/code-quality
tool prepare inject --config test-config.json --target projects/client/ --dry-run  # Preview
tool prepare inject --config test-config.json --target projects/client/  # Execute
```

## Troubleshooting

### Error: "Target must be 'projects/client/'"

**Cause:** You're trying to inject to an invalid target.

**Solution:**

```bash
# ❌ Wrong
tool prepare inject --config <path> --target projects/wrong/

# ✅ Correct
tool prepare inject --config <path> --target projects/client/
```

### Error: "Cache files not found"

**Cause:** Phase 1 (cache populate) not run before Phase 2 (inject).

**Solution:**

```bash
# Run Phase 1 first
tool cache populate --source projects/code-quality

# Then Phase 2
tool prepare inject --config <path> --target projects/client/
```

### Warning: "prepare run is deprecated"

**Cause:** Using old command.

**Solution:** Migrate to two-phase workflow as shown in this guide.

## Validation

After migration, verify:

### 1. Cache Population Works

```bash
tool cache populate --source projects/code-quality
# Should succeed, no errors
```

### 2. Injection Works

```bash
tool prepare inject --config <path> --target projects/client/ --verbose
# Should succeed, show file operations
```

### 3. Nuke Integration Works

```bash
nuke PrepareCache
nuke PrepareClient
nuke RestoreClient
# All should succeed
```

### 4. Full Workflow Works

```bash
nuke BuildUnityWithPreparation
# Should complete: PrepareCache → PrepareClient → BuildUnity → RestoreClient
```

### 5. Client Stays Clean

```bash
cd projects/client
git status
# Should be clean (no modifications outside build)
```

## Best Practices

### DO ✅

- **DO** use Nuke targets for automated workflows
- **DO** run `PrepareCache` independently when dependencies change
- **DO** use `--dry-run` to preview changes before injection
- **DO** validate configs before injection
- **DO** ensure git reset before injection in builds

### DON'T ❌

- **DON'T** run `prepare inject` outside build operations
- **DON'T** inject to targets other than `projects/client/`
- **DON'T** skip cache population before injection
- **DON'T** use deprecated `prepare run` in new scripts
- **DON'T** modify `projects/client/` manually during builds

## Getting Help

### Documentation

- **Tool README:** `tool/README.md`
- **Spec Amendment:** `.specify/specs/build-preparation-tool-amendment-001.md`
- **Nuke Integration:** `build/nuke/build/Build.Preparation.cs`

### Support

- Check logs with `--verbose` flag
- Use `--dry-run` to preview changes
- Validate configs with `config validate`
- Review error messages carefully

## FAQ

### Q: Can I still use `prepare run`?

**A:** Yes, but it's deprecated. It will show a warning and redirect to `prepare inject --target projects/client/`. Migrate to the two-phase workflow as soon as possible.

### Q: When will `prepare run` be removed?

**A:** In v2.0.0 (next major version). You have until then to migrate.

### Q: Do I need to update my configs?

**A:** No, configuration files remain unchanged. Only the commands change.

### Q: Can I run Phase 2 without Phase 1?

**A:** No, Phase 2 requires cache to be populated by Phase 1. You'll get an error if cache is missing.

### Q: Can I run Phase 1 multiple times?

**A:** Yes, Phase 1 is safe to run anytime and can be run multiple times.

### Q: What if I forget to run Phase 1?

**A:** Phase 2 will fail with a clear error message listing missing cache files.

### Q: Can I inject to a different target?

**A:** No, only `projects/client/` is allowed per R-BLD-060 security rule.

## Migration Checklist

Use this checklist to track your migration:

- [ ] Read this migration guide
- [ ] Understand two-phase workflow
- [ ] Update development scripts
- [ ] Update Nuke build files
- [ ] Update CI/CD pipelines
- [ ] Update documentation
- [ ] Test Phase 1 (cache populate)
- [ ] Test Phase 2 (inject)
- [ ] Test full workflow (Nuke)
- [ ] Verify client stays clean
- [ ] Remove old `prepare run` commands
- [ ] Train team on new workflow
- [ ] Update runbooks/wikis
- [ ] Monitor for issues

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.1.0 | 2025-10-17 | Two-phase workflow introduced, `prepare run` deprecated |
| 1.0.0 | 2025-10-15 | Initial release with `prepare run` |

---

**Need Help?** Check the tool README or consult the spec amendment documents.
