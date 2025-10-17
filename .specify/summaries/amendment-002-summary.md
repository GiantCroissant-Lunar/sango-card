---
title: "Amendment 002 Summary - Two-Config Architecture"
created: 2025-10-17
status: draft
---

# Amendment 002 Summary: Two-Config Architecture & Manual Source Control

## Quick Overview

**Problem:** Current build preparation tool mixes preparation and build concerns in one config, can't handle scattered sources, and has inflexible path assumptions.

**Solution:** Split into two configs - one for collecting sources, one for build injection. Add manual control for scattered dependencies.

## The Two Configs

### Config 1: Preparation Manifest (Phase 1)
**Location:** `build/preparation/sources/<name>.json`  
**Purpose:** Define what to collect into cache

```json
{
  "items": [
    {
      "source": "D:/any/path/to/package",
      "cacheAs": "package-name",
      "type": "package"
    }
  ]
}
```

**Command:**
```bash
cache populate --manifest sources/my-sources.json
```

### Config 2: Build Injection Config (Phase 2)
**Location:** `build/preparation/configs/<name>.json`  
**Purpose:** Define what to inject and build operations

```json
{
  "packages": [...],
  "assemblies": [...],
  "assetManipulations": [...],
  "codePatches": [...],
  "scriptingDefineSymbols": {...}
}
```

**Command:**
```bash
prepare inject --config configs/production.json
```

## Key Benefits

1. **Clear Separation:** Preparation vs build concerns are distinct
2. **No Path Restrictions:** Source and target can be anywhere
3. **Scattered Sources:** Collect from multiple locations
4. **Reusable Cache:** Same cache, different build configs
5. **Manual Control:** Add items one-by-one or in batches

## New CLI Commands

```bash
# Add source to preparation manifest
config add-source --source <path> --cache-as <name> --type <type> --manifest <file>

# Add injection to build config
config add-injection --source <cache-path> --target <client-path> --type <type> --config <file>

# Batch operations
config add-batch --manifest <batch-file> --output <config> --config-type <source|injection>
```

## Real-World Example

Your scattered dependencies:
```yaml
# my-dependencies.yml
items:
  - source: projects/code-quality/Library/PackageCache/org.nuget.microsoft.bcl.asyncinterfaces@328a307bd65b
    cacheAs: org.nuget.microsoft.bcl.asyncinterfaces
    type: package

  - source: C:/external-libs/Polly.8.6.2
    cacheAs: Polly.8.6.2
    type: assembly
```

**Step 1:** Collect into cache
```bash
config add-batch --manifest my-dependencies.yml --output sources/all.json --config-type source
cache populate --manifest sources/all.json
```

**Step 2:** Define injections
```bash
config add-injection \
  --source build/preparation/cache/Polly.8.6.2 \
  --target projects/client/Assets/MyLibs/Polly \
  --type assembly \
  --config configs/production.json
```

**Step 3:** Inject and build
```bash
prepare inject --config configs/production.json
```

## Migration Path

Existing configs still work (with deprecation warning). Migration tool available:

```bash
config migrate --old-config configs/default.json
```

## Implementation Timeline

- **Week 1:** Core architecture (schemas, cache/inject logic, migration)
- **Week 2:** CLI commands (add-source, add-injection, add-batch)
- **Week 3:** TUI updates, testing, documentation

**Total:** ~40 hours

## Files

- **Spec:** `.specify/specs/build-preparation-tool-amendment-002.md`
- **Task:** `.specify/tasks/TASK-BLD-PREP-002.md`
- **This Summary:** `.specify/AMENDMENT-002-SUMMARY.md`

## Next Steps

1. Review and approve spec
2. Assign task
3. Begin implementation (Week 1: Core architecture)
