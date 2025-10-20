---
title: Build Cache Cleanup Summary
category: finding
status: completed
date: 2025-10-20
tags: [build, cache, cleanup]
---

# Build Cache Optimization Summary

## Changes Made

### 1. Removed Large Unused Unity Packages

Removed from both `multi-stage-preparation.json` and `multi-stage-injection.json`:

- `com.unity.shadergraph@2c9221ffedf4` - 4,191 files (190 MB)
- `com.unity.render-pipelines.core@60896ea171dc` - 2,524 files (28 MB)
- `com.unity.render-pipelines.universal@cc5029882822` - 2,634 files (47 MB)
- `com.unity.render-pipelines.universal-config@8dc1aab4af1d`

**Total Removed**: ~9,349 files, ~265 MB

### 2. Updated Task Scripts for v2.0 Config Support

Updated `scripts/cleanup-preparation-cache.ps1` and `scripts/verify-preparation-cache.ps1` to support both:

- v1.0 config format (packages/assemblies at root)
- v2.0 config format (nested under `stages` or `injectionStages`)

### 3. Fixed Default Config in Taskfile.yml

Updated default CONFIG parameter from `preparation` to `multi-stage-preparation` for:

- `task build:prepare:cleanup`
- `task build:prepare:cleanup-preview`  
- `task build:prepare:cleanup-force`

## Cleanup Preview Results

After running `task build:prepare:cleanup-preview`:

```
Cache Status:
  📁 Total folders:    118
  ✅ Keep (in config): 30
  🗑️  Orphaned:         88
  💾 Total size to reclaim: 1,313 MB
```

**Top Orphaned Folders** (will be deleted when you run cleanup):

1. `com.unity.burst@1df634d836b8` - 842 MB
2. `com.unity.shadergraph@2c9221ffedf4` - 190 MB (removed from config)
3. `com.unity.render-pipelines.universal@cc5029882822` - 47 MB (removed)
4. `org.nuget.grpc.core@30a46b906ed0` - 41 MB
5. `com.unity.timeline@6b9e48457ddb` - 36 MB
6. And 83 more...

## Next Steps

### Immediate - Clean Up Cache

Run this to actually delete the orphaned folders:

```bash
task build:prepare:cleanup
```

Or force without confirmation:

```bash
task build:prepare:cleanup-force
```

### Future - Optimize dotnet~ Folder (Optional)

The `com.contractwork.sangocard.build` package contains `dotnet~/tool/` with 1,348 files (293 MB) of .NET build tool source code.

**Option A**: Add assetManipulations to delete dotnet~ after injection
**Option B**: Build the tool once and copy only the DLL

See `build/configs/preparation/OPTIMIZATION-NOTES.md` for detailed implementation options.

## Impact

**Before Optimization**:

- 118 cache folders
- 24,008 files
- 1,627 MB

**After Cleanup** (estimated):

- 30 cache folders (74% reduction)
- ~14,659 files (39% reduction)
- ~314 MB (81% reduction)

## Files Modified

- `build/configs/preparation/multi-stage-preparation.json` - Removed 4 Unity packages
- `build/configs/preparation/multi-stage-injection.json` - Removed 4 Unity packages  
- `scripts/cleanup-preparation-cache.ps1` - Added v2.0 config support
- `scripts/verify-preparation-cache.ps1` - Added v2.0 config support
- `Taskfile.yml` - Updated default CONFIG to multi-stage-preparation
- `build/configs/preparation/OPTIMIZATION-NOTES.md` - Detailed optimization notes (new)
- `build/configs/preparation/SUMMARY.md` - This file (new)
