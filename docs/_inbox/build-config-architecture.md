---
title: Build Configuration Architecture
description: Critical design patterns for preparation and injection configs
status: active
date: 2025-10-20
author: AI Assistant
tags: [build, configuration, architecture, preparation, injection]
---

# Build Configuration Architecture

## Critical Design Pattern: TWO Separate Configs

### ⚠️ IMPORTANT: Always Maintain Separation

The build system uses **TWO separate configs** for preparation and injection.
This separation is NOT optional - it's a core architectural principle.

## Why Two Configs?

### Different Concerns, Different Data

1. **Preparation Config** (Cache Population)
   - **Concern:** What to copy FROM source TO cache
   - **Source Paths:** `projects/code-quality/Library/PackageCache/pkg@hash`
   - **Target Paths:** `build/preparation/cache/pkg`
   - **Purpose:** Define sources for cache population

2. **Injection Config** (Build Operations)
   - **Concern:** What to inject FROM cache TO client + operations
   - **Source Paths:** `build/preparation/cache/pkg`
   - **Target Paths:** `projects/client/Packages/pkg`
   - **Purpose:** Define targets and build operations

### This is NOT Duplication

The package lists in both configs are **semantically different**:

```json
// multi-stage-preparation.json - SOURCE to CACHE
{
  "name": "com.cysharp.unitask",
  "source": "projects/code-quality/Library/PackageCache/com.cysharp.unitask@15a4a7657f99",
  "target": "build/preparation/cache/com.cysharp.unitask"
}

// multi-stage-injection.json - CACHE to CLIENT
{
  "name": "com.cysharp.unitask",
  "source": "build/preparation/cache/com.cysharp.unitask",
  "target": "projects/client/Packages/com.cysharp.unitask"
}
```

**These are DIFFERENT transformations, not duplication!**

## Common Mistakes to Avoid

### ❌ MISTAKE 1: Creating One Unified Config

**Wrong Thinking:**
> "I can create one config with `cacheSource` that references the preparation config,
> so I don't need to duplicate the package list in injection config."

**Why This is Wrong:**

- Preparation and injection have DIFFERENT data (source vs target paths)
- `cacheSource` should reference the v2.0 preparation config, not replace it
- The package list is NOT duplicated - the paths are DIFFERENT

### ❌ MISTAKE 2: Removing "Duplicated" Package Lists

**Wrong Thinking:**
> "The injection config has a `cacheSource` property, so I can remove all the packages
> from it and let it auto-inject from the preparation config."

**Why This is Wrong:**

- Injection config needs to specify TARGET paths (where to inject in client)
- Injection config needs to specify which packages to inject (may be subset)
- Injection config needs to add operations (assetManipulations, codePatches, etc.)

### ❌ MISTAKE 3: Conflating "Multi-Stage" with "Unified"

**Wrong Thinking:**
> "Multi-stage means combining both preparation and injection into one config
> with multiple stages."

**Why This is Wrong:**

- "Multi-stage" means multiple stages WITHIN each concern
- Preparation can have stages: initial, incremental, final
- Injection can have stages: preTest, preBuild, postBuild, preNativeBuild, postNativeBuild
- Each config has its own stages for its own concern

## Correct Architecture

### V2.0 Multi-Stage (Current)

```
multi-stage-preparation.json (v2.0)
├── Stage: initial
│   ├── packages (source → cache)
│   └── assemblies (source → cache)
└── Stage: incremental (future)
    └── packages (additional sources)

multi-stage-injection.json (v2.0)
├── Stage: preTest
│   └── packages (cache → client, test-only)
├── Stage: preBuild
│   ├── packages (cache → client, all packages)
│   ├── assemblies (cache → client, all assemblies)
│   └── assetManipulations (delete generated folders)
└── Stage: postBuild (future)
    └── codePatches (runtime modifications)
```

## The `cacheSource` Property

### Purpose

Reference the preparation config to understand what's available in cache.
Used for validation and dependency resolution.

### NOT a Replacement

`cacheSource` does NOT mean "use this instead of defining packages."
It means "this is where the cache was populated from."

## Verification Checklist

When working with build configs, verify:

- [ ] ✅ TWO separate v2.0 configs exist (preparation + injection)
- [ ] ✅ Preparation has SOURCE paths (code-quality → cache)
- [ ] ✅ Injection has TARGET paths (cache → client)
- [ ] ✅ Both configs have complete package/assembly lists
- [ ] ✅ Injection config has operations (assetManipulations, codePatches, etc.)
- [ ] ✅ `cacheSource` in injection config references preparation config

## File Structure

```
build/configs/preparation/
├── multi-stage-preparation.json (v2.0) - Cache population ONLY
└── multi-stage-injection.json (v2.0)   - Injection + operations ONLY

build/nuke/build/Schemas/
├── multi-stage-preparation.schema.json - Preparation schema
└── multi-stage-injection.schema.json   - Injection schema
```

## Remember

**The separation exists because preparation and injection are DIFFERENT operations
with DIFFERENT data and DIFFERENT purposes. Combining them would violate
separation of concerns and make the system harder to understand and maintain.**

When in doubt, ask: "Does this operation populate the cache (preparation) or
inject into the client (injection)?" The answer determines which config to modify.

## See Also

- [README.md](../../build/configs/preparation/README.md) - Quick start guide
- [v1-config-deprecation.md](v1-config-deprecation.md) - V1.0 deprecation timeline
