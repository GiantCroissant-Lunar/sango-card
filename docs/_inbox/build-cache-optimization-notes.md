---
title: Preparation Cache Optimization Notes
category: guide
status: active
date: 2025-10-20
tags: [build, cache, optimization, preparation]
---

# Preparation Cache Optimization Notes

## Overview

This document tracks optimizations made to reduce the preparation cache size and improve build performance.

## Changes Made (2025-10-20)

### 1. Removed Large Unity Packages from Cache

Removed the following packages from both `multi-stage-preparation.json` and `multi-stage-injection.json`:

- `com.unity.shadergraph` (4,191 files, 190 MB)
- `com.unity.render-pipelines.core` (2,524 files, 28 MB)
- `com.unity.render-pipelines.universal` (2,634 files, 47 MB)
- `com.unity.render-pipelines.universal-config` (minimal files)

**Reason**: These packages are not needed for the current build and were consuming significant cache space.

**Impact**:

- Reduced cache by ~9,349 files and ~265 MB
- Faster cache population and injection

### 2. Identified dotnet~ Folder Optimization Opportunity

**Issue**: The `com.contractwork.sangocard.build` package contains a `dotnet~/tool/` folder with 1,348 files (293 MB) of .NET build tooling source code.

**Current Behavior**: The entire folder is copied to the cache and potentially injected into the client project.

**Proposed Solution**:

1. **Build the tool once**: Compile `SangoCard.Build.Tool.csproj` to produce the final DLL
2. **Copy only the built DLL**: Copy from `dotnet~/tool/SangoCard.Build.Tool/bin/Release/net8.0/` to `Tools/` folder
3. **Exclude source during injection**: Add `assetManipulations` to delete the `dotnet~/` folder after injection

This would reduce the package footprint from 1,445 files to ~100 files (excluding build artifacts).

## Implementation Plan for dotnet~ Optimization

### ✅ IMPLEMENTED: Pre-Built Tool (Recommended)

**Status**: Completed 2025-10-20

The build tool is now configured to automatically publish as a single-file executable and copy to the Tools folder.

**What was done**:

1. **Updated .csproj** with:
   - `PublishSingleFile=true` with `IncludeNativeLibrariesForSelfExtract=true`
   - Post-publish MSBuild target that copies the exe to `Tools/` folder
   - Support for both debug builds (with `-debug` suffix) and release builds

2. **Added Nuke build target**: `BuildPreparationTool` in `Build.Preparation.cs`
   - Publishes as self-contained single-file executable (win-x64 by default)
   - Automatically copies to Tools folder via MSBuild target
   - Supports multiple runtimes via environment variables
   - Integrated with existing NUKE build system

3. **Updated Taskfile command**: `task setup:build-tool`
   - Calls the Nuke target instead of a standalone script
   - Can specify Configuration and RuntimeIdentifier via environment variables

**Usage**:

```bash
# Build release version for Windows
task setup:build-tool

# Build for Linux
task setup:build-tool RUNTIME=linux-x64

# Build debug version
task setup:build-tool CONFIG=Debug

# Or call Nuke directly
./build/nuke/build.ps1 BuildPreparationTool
```

**Environment Variables**:

- `PREP_TOOL_RUNTIME` - Target runtime (default: win-x64)
- `PREP_TOOL_CONFIG` - Build configuration (default: Release)

**Result**:

- Single-file exe: `packages/scoped-6571/com.contractwork.sangocard.build/Tools/SangoCard.Build.Tool.exe`
- Size: ~103 MB (self-contained runtime included)
- The `dotnet~/tool/` folder (1,348 files, 293 MB) is **no longer needed at runtime**

### Future: Exclude dotnet~ During Cache Population (Option A)

Add to the cache population logic to exclude the `dotnet~/` folder entirely:

**Benefits**:

- Reduces cache by 1,348 files (293 MB)
- Only the pre-built tool exe (~103 MB) is needed
- Cache population is faster

### Alternative: Post-Copy Cleanup (Option B)

Add to `multi-stage-injection.json` in the `preBuild` stage:

### Alternative: Post-Copy Cleanup (Option B)

Add to `multi-stage-injection.json` in the `preBuild` stage to delete dotnet~ after injection:

```json
{
  "name": "preBuild",
  "enabled": true,
  "packages": [
    {
      "name": "com.contractwork.sangocard.build",
      "source": "build/preparation/cache/com.contractwork.sangocard.build",
      "target": "projects/client/Packages/com.contractwork.sangocard.build"
    }
  ],
  "assetManipulations": [
    {
      "type": "delete",
      "target": "projects/client/Packages/com.contractwork.sangocard.build/dotnet~",
      "description": "Remove .NET source code after injection (only built tools needed)"
    },
    {
      "type": "copy",
      "source": "packages/scoped-6571/com.contractwork.sangocard.build/Tools/SangoCard.Build.Tool.dll",
      "target": "projects/client/Packages/com.contractwork.sangocard.build/Tools/SangoCard.Build.Tool.dll",
      "description": "Copy pre-built tool DLL to Tools folder"
    }
  ]
}
```

**Note**: The above assumes the tool DLL already exists in the Tools folder. With the implemented solution, you would change it to just delete dotnet~ since the tool exe is already built.

### Option C: Selective Copy During Cache Population (Deprecated)

~~Modify the cache population logic to exclude dotnet~ folder.~~

This is superseded by the implemented pre-built tool approach.

## Current Cache Statistics

**Before Optimization**:

- Total cache folders: 118
- Total files: 24,008
- Total size: 1,627 MB

**Top 5 File Offenders**:

1. `com.unity.shadergraph` - 4,191 files (190 MB) - **REMOVED**
2. `com.unity.render-pipelines.universal` - 2,634 files (47 MB) - **REMOVED**
3. `com.unity.render-pipelines.core` - 2,524 files (28 MB) - **REMOVED**
4. `com.tuyoogame.yooasset` - 1,908 files (32 MB)
5. `com.unity.inputsystem` - 1,682 files (24 MB)

**Build Package Breakdown**:

- `com.contractwork.sangocard.build` - 1,445 files (293 MB)
  - `dotnet~/tool/` - 1,348 files (293 MB) - **OPTIMIZATION TARGET**
  - `Editor/` - 83 files
  - `Tests/` - 7 files

**After Current Optimizations** (estimated):

- Total cache folders: 114
- Total files: ~14,659 (39% reduction)
- Total size: ~1,362 MB (16% reduction)

**After dotnet~ Optimization** (if implemented):

- Total files: ~13,311 (45% reduction from original)
- Total size: ~1,069 MB (34% reduction from original)

## Recommendations

1. **Immediate**: Run `task build:prepare:cleanup` to remove orphaned cache folders for the removed packages
2. **Short-term**: Implement Option A (post-copy cleanup with assetManipulations) - easiest to implement
3. **Long-term**: Consider Option B (pre-build compilation) for cleaner separation of source and built artifacts
4. **Monitor**: Track cache size after each optimization to measure impact

## Related Files

- `build/configs/preparation/multi-stage-preparation.json` - Cache population config
- `build/configs/preparation/multi-stage-injection.json` - Injection config with operations
- `build/nuke/build/Schemas/multi-stage-injection.schema.json` - Schema supporting assetManipulations
- `scripts/cleanup-preparation-cache.ps1` - Cache cleanup utility

## See Also

- Build preparation system: `build/nuke/build/Build.Preparation.cs`
- Preparation tool: `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/`
