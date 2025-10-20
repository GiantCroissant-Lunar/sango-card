---
title: Build System Optimization - Complete Summary
category: finding
status: completed
date: 2025-10-20
tags: [build, optimization, cache, nuke, preparation]
---

# Build System Optimization - Complete Summary

## What Was Done

This document summarizes all optimizations made to the build preparation cache system on 2025-10-20.

## Part 1: Cache Cleanup - Removed Unused Packages

### Changes

- Removed 4 large Unity packages from both `multi-stage-preparation.json` and `multi-stage-injection.json`
- Updated cleanup/verify scripts to support v2.0 config format
- Fixed Taskfile.yml default config paths

### Packages Removed

1. `com.unity.shadergraph` - 4,191 files (190 MB)
2. `com.unity.render-pipelines.core` - 2,524 files (28 MB)
3. `com.unity.render-pipelines.universal` - 2,634 files (47 MB)
4. `com.unity.render-pipelines.universal-config`

**Total Removed**: ~9,349 files, ~265 MB

### Impact

Run `task build:prepare:cleanup` to actually delete the orphaned cache folders:

- 88 orphaned folders will be removed
- 1,313 MB will be reclaimed
- Cache reduced from 118 folders to 30 folders (74% reduction)

## Part 2: Build Tool Optimization - Single-File Executable

### Problem

The `com.contractwork.sangocard.build` package contained:

- `dotnet~/tool/` folder with 1,348 source files (293 MB)
- These source files were being cached and potentially injected into client

### Solution Implemented

✅ Build tool now publishes as **single-file self-contained executable**

### Changes Made

**1. Updated .csproj** (`SangoCard.Build.Tool.csproj`):

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <PackageToolsDir>$(MSBuildThisFileDirectory)..\..\..\Tools</PackageToolsDir>
</PropertyGroup>

<!-- MSBuild target to auto-copy after publish -->
<Target Name="CopyToolToPackage" AfterTargets="Publish">
  <!-- Copies exe to Tools/ folder -->
</Target>
```

**2. Created Nuke build target** (`Build.Preparation.cs`):

- Target: `BuildPreparationTool`
- Publishes as single-file exe
- Supports win-x64, linux-x64, osx-x64, osx-arm64 via environment variables
- Integrated with existing NUKE build system
- Verifies tool was copied successfully

**3. Added task command**:

```bash
task setup:build-tool              # Release build for Windows
task setup:build-tool CONFIG=Debug # Debug build
task setup:build-tool RUNTIME=linux-x64  # Linux build
```

### Result

**Published Tool**:

- Location: `packages/scoped-6571/com.contractwork.sangocard.build/Tools/SangoCard.Build.Tool.exe`
- Size: 103 MB (self-contained, includes .NET runtime)
- Single file: No dependencies needed

**Current Package State**:

- Source code: 1,348 files in `dotnet~/tool/` (still present)
- Tool exe: 1 file in `Tools/` (newly built)
- Total: 1,446 files (temporarily larger due to both being present)

## Part 3: Future Optimization Opportunities

### Option A: Exclude dotnet~ from Cache (Recommended)

Modify cache population to skip the `dotnet~/tool/` folder entirely.

**Benefits**:

- Reduces cache by 1,348 files (293 MB)
- Faster cache population
- Only pre-built tool exe distributed

**Implementation**:
Modify cache population logic in the build tool to exclude folders matching `dotnet~`.

### Option B: Post-Injection Cleanup

Add `assetManipulations` to delete `dotnet~/` after injecting to client:

```json
{
  "name": "preBuild",
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
      "description": "Remove build tool source (only exe needed)"
    }
  ]
}
```

**Benefits**:

- Source remains in original package (for development)
- Client project doesn't get source files
- Smaller client project

## Overall Impact

### Cache Before All Optimizations

- 118 folders
- 24,008 files
- 1,627 MB

### Cache After Cleanup (Part 1)

- 30 folders (74% ↓)
- ~14,659 files (39% ↓)
- ~314 MB (81% ↓)

### Additional Savings if dotnet~ Excluded (Part 3A)

- Would remove 1,348 more files
- Would save 293 MB source + 103 MB exe overhead = 190 MB net savings
- Final: 30 folders, ~13,311 files, ~124 MB (95% reduction from original!)

## Files Modified/Created

### Configuration Files

- ✏️ `build/configs/preparation/multi-stage-preparation.json` - Removed 4 packages
- ✏️ `build/configs/preparation/multi-stage-injection.json` - Removed 4 packages
- ✏️ `Taskfile.yml` - Updated default config paths, added setup:build-tool task

### Scripts

- ✏️ `scripts/cleanup-preparation-cache.ps1` - Added v2.0 config support
- ✏️ `scripts/verify-preparation-cache.ps1` - Added v2.0 config support
- ~~➕ `scripts/build-tool.ps1` - New build script for tool~~ (Replaced with Nuke target)

### Build Configuration

- ✏️ `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/SangoCard.Build.Tool.csproj` - Added publish targets
- ➕ `build/nuke/build/Build.Preparation.cs` - Added BuildPreparationTool target

### Documentation

- ➕ `build/configs/preparation/SUMMARY.md` - Cache cleanup summary
- ✏️ `build/configs/preparation/OPTIMIZATION-NOTES.md` - Updated with implementation status
- ➕ `docs/build-tool-optimization.md` - Complete build tool guide
- ➕ `README-OPTIMIZATIONS.md` - This file

### Build Artifacts (New)

- ➕ `packages/scoped-6571/com.contractwork.sangocard.build/Tools/SangoCard.Build.Tool.exe` - Published tool (103 MB)

## How to Use

### Step 1: Clean Up Cache

```bash
# Preview what will be deleted
task build:prepare:cleanup-preview

# Actually delete orphaned folders
task build:prepare:cleanup
```

### Step 2: Rebuild Tool (Already Done)

```bash
# Only needed if you modify the tool source
task setup:build-tool
```

### Step 3: Repopulate Cache

```bash
# Repopulate with the updated configuration
task build:prepare:cache CONFIG=multi-stage-preparation
```

### Step 4: Verify

```bash
# Verify all items are present
task build:prepare:verify CONFIG=multi-stage-preparation
```

## Recommendations

### Immediate Actions

1. ✅ **Done**: Build tool as single-file exe
2. 🔲 **Run**: `task build:prepare:cleanup` to remove orphaned packages
3. 🔲 **Verify**: Check that builds still work after cleanup

### Short-Term (Optional)

4. 🔲 **Consider**: Implement Option B (post-injection cleanup of dotnet~)
5. 🔲 **Test**: Verify Unity builds work with only the tool exe (no source)

### Long-Term (Optional)

6. 🔲 **Implement**: Modify cache population to exclude dotnet~ folder (Option A)
7. 🔲 **Monitor**: Track cache size and file counts after changes

## Additional Notes

### About the Node.js Memory Issue

The original crash (JavaScript heap out of memory) was **not caused by an infinite loop**. It was due to:

1. Claude Code CLI processing large file operations
2. 24,008 files in the cache (though only 118 folders)
3. Multiple large Unity packages with thousands of shader files

The `.node-options` file was created to increase Node.js memory limit, but the real solution was to optimize the cache itself.

### About Unity Packages with Many Files

Some Unity packages naturally have many files:

- `com.unity.shadergraph` - 4,191 files (shader variations)
- `com.unity.render-pipelines.*` - 2,500+ files each (rendering code)
- `com.tuyoogame.yooasset` - 1,908 files (asset management)

These are normal for Unity packages, but if you don't use them, they should be excluded from the cache.

## See Also

- **Cache optimization details**: `build/configs/preparation/OPTIMIZATION-NOTES.md`
- **Build tool guide**: `docs/build-tool-optimization.md`
- **Cleanup summary**: `build/configs/preparation/SUMMARY.md`
- **Build system code**: `build/nuke/build/Build.Preparation.cs`
- **Injection schema**: `build/nuke/build/Schemas/multi-stage-injection.schema.json`
