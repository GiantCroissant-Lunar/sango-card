---
doc_id: DOC-2025-00103
title: PHASE1 REVISED COMPLETE
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [phase1-revised-complete]
summary: >
  (Add summary here)
source:
  author: system
---
# Phase 1 Complete: Revised Approach Using Unity Packages

## ✅ What Was Done

### 1. Created Build Preparation Cache

**Structure:**

```
build/preparation/
├── unity-packages/          # Unity packages (copied as-is)
│   ├── org.nuget.microsoft.extensions.logging@c95ee2e3b656/
│   ├── org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5/
│   ├── org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43/
│   └── org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9/
└── assemblies/              # Plain DLLs
    ├── System.CommandLine.dll
    └── Splat.dll
```

### 2. Copied Microsoft.Extensions Packages

**Source:** `projects/code-quality/Library/PackageCache/`
**Destination:** `build/preparation/unity-packages/`

All 4 Microsoft.Extensions packages copied as complete Unity packages with:

- package.json
- .asmdef files
- DLL files
- Documentation
- All metadata

### 3. Copied Plain DLLs

**To:** `build/preparation/assemblies/`

- ✅ System.CommandLine.dll (155,680 bytes)
- ✅ Splat.dll (141,752 bytes)

### 4. Cleaned Custom Packages

**Removed embedded DLLs:**

- ❌ `packages/scoped-6571/com.contractwork.sangocard.build/Editor/Plugins/` (deleted)
- ❌ `packages/scoped-2151/com.contractwork.sangocard.cross/Editor/Plugins/` (deleted)

### 5. Updated Assembly Definitions

**Build Package (`SangoCard.Build.Editor.asmdef`):**

```json
{
  "references": [
    "CodeStage.Maintainer.Editor",
    "SangoCard.Cross.Editor",
    "Unity.Addressables",
    "Unity.Addressables.Editor",
    "Unity.ResourceManager",
    "Microsoft.Extensions.Logging",                          // ← Package reference
    "Microsoft.Extensions.Logging.Abstractions",             // ← Package reference
    "Microsoft.Extensions.DependencyInjection",              // ← Package reference
    "Microsoft.Extensions.DependencyInjection.Abstractions"  // ← Package reference
  ],
  "precompiledReferences": [
    "System.CommandLine.dll",  // ← Plain DLL
    "Splat.dll"                // ← Plain DLL
  ]
}
```

**Cross Package (`SangoCard.Cross.Editor.asmdef`):**

```json
{
  "references": [],
  "precompiledReferences": [
    "Splat.dll"  // ← Plain DLL
  ]
}
```

## Benefits of This Approach

### ✅ Cleaner Architecture

- Unity packages stay as Unity packages
- No manual DLL extraction
- Preserves all package metadata

### ✅ Easier Maintenance

- Centralized cache in `build/preparation/`
- Easy to update packages (just replace in cache)
- Clear separation: Unity packages vs plain DLLs

### ✅ Follows Unity Conventions

- Packages copied to `client/Packages/` during build
- Plain DLLs copied to `client/Assets/Plugins/assemblies/`
- Unity's package resolution handles dependencies

### ✅ Flexible

- Cache can be version controlled
- Can be regenerated from code-quality
- Easy to add/remove dependencies

## How It Works During Build

### Pre-Build Augmentation (via preparation config)

1. **Copy Unity Packages:**

   ```
   build/preparation/unity-packages/org.nuget.microsoft.extensions.logging@xxx/
     → projects/client/Packages/org.nuget.microsoft.extensions.logging@xxx/
   ```

2. **Copy Plain DLLs:**

   ```
   build/preparation/assemblies/System.CommandLine.dll
     → projects/client/Assets/Plugins/assemblies/System.CommandLine.dll
   ```

3. **Copy Custom Packages:**

   ```
   packages/scoped-6571/com.contractwork.sangocard.build/
     → projects/client/Packages/com.contractwork.sangocard.build/
   ```

4. **Unity Resolves References:**
   - Build package references Microsoft.Extensions packages
   - Unity finds them in `Packages/`
   - Plain DLLs loaded from `Assets/Plugins/assemblies/`

## Next Steps

### 1. Test in code-quality Project

The code-quality project already has Microsoft.Extensions packages via manifest.json.
We need to verify our custom packages work with package references.

**Action:**

- Open code-quality in Unity 6000.2.x
- Check Console for compilation errors
- Verify custom packages compile successfully

### 2. Update Preparation Config JSON

Add Microsoft.Extensions packages and plain DLLs to the preparation config:

```json
{
  "assetManipulations": [
    {
      "category": "Add Microsoft.Extensions Unity packages",
      "assets": [
        {
          "source": {
            "rootPath": "../preparation",
            "path": "unity-packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656"
          },
          "destination": {
            "rootPath": "../../projects/client",
            "path": "Packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656"
          },
          "status": { "presence": "present", "operation": "copy" },
          "enable": true
        }
        // ... other Microsoft.Extensions packages
      ]
    },
    {
      "category": "Add plain DLL assemblies",
      "assets": [
        {
          "source": {
            "rootPath": "../preparation",
            "path": "assemblies/System.CommandLine.dll"
          },
          "destination": {
            "rootPath": "../../projects/client",
            "path": "Assets/Plugins/assemblies/System.CommandLine.dll"
          },
          "status": { "presence": "present", "operation": "copy" },
          "enable": true
        }
        // ... other plain DLLs
      ]
    }
  ]
}
```

### 3. Implement Preparation Executor in Nuke

Create `IBuildPreparation` component that:

- Parses preparation config JSON
- Executes asset manipulations
- Copies packages and DLLs to client
- Modifies manifest.json
- Applies code patches

### 4. Test Full Build Flow

1. Run preparation
2. Build client with Unity 6000.2.x
3. Verify build succeeds
4. Restore client to clean state

## File Changes Summary

### Created

```
build/preparation/
├── unity-packages/
│   ├── org.nuget.microsoft.extensions.logging@c95ee2e3b656/ (NEW)
│   ├── org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5/ (NEW)
│   ├── org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43/ (NEW)
│   └── org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9/ (NEW)
└── assemblies/
    ├── System.CommandLine.dll (NEW)
    └── Splat.dll (NEW)
```

### Modified

```
packages/scoped-6571/com.contractwork.sangocard.build/
├── Editor/
│   ├── Plugins/ (DELETED)
│   └── SangoCard.Build.Editor.asmdef (MODIFIED - uses package references)
└── package.json (unity: 6000.2)

packages/scoped-2151/com.contractwork.sangocard.cross/
├── Editor/
│   ├── Plugins/ (DELETED)
│   └── SangoCard.Cross.Editor.asmdef (unchanged - already correct)
└── package.json (unity: 6000.2)
```

## Success Criteria

- [ ] code-quality project compiles without errors
- [ ] Custom packages work with package references
- [ ] No embedded DLLs in custom packages
- [ ] Cache structure complete and organized
- [ ] Ready to implement preparation executor

## Current Status

✅ **Phase 1 Complete** - Cache created, packages cleaned, ready for testing

**Next:** Test in code-quality project, then implement Phase 3 (Preparation System)
