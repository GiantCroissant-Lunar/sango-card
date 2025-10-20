# Build Tool Optimization - Complete Guide

## Overview

The SangoCard.Build.Tool is now configured to build as a **single-file executable** and automatically copy to the package Tools folder, eliminating the need to distribute 1,348 source files during cache population and injection.

## What Was Changed

### 1. Updated .csproj Configuration

**File**: `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/SangoCard.Build.Tool.csproj`

**Changes**:
- Added `IncludeNativeLibrariesForSelfExtract=true` for better single-file bundling
- Added MSBuild property: `PackageToolsDir` pointing to `../../../Tools`
- Added MSBuild target: `CopyToolToPackage` (runs after Publish)
  - Copies published single-file exe to Tools folder
  - Creates Tools directory if it doesn't exist
- Added MSBuild target: `CopyToolToPackageAfterBuild` (runs after Build)
  - Copies debug builds with `-debug` suffix for development

### 2. Added Nuke Build Target

**File**: `build/nuke/build/Build.Preparation.cs`

**Target**: `BuildPreparationTool`

**Features**:
- Integrated with existing NUKE build system
- Publishes the tool as self-contained single-file executable
- Supports multiple platforms (win-x64, linux-x64, osx-x64, osx-arm64) via environment variables
- Configurable build configuration (Debug/Release) via environment variables
- Verifies the tool was copied successfully
- Shows tool size and location

### 3. Added Task Command

**File**: `Taskfile.yml`

**Task**: `setup:build-tool`

**Usage**:
```bash
# Default: Release build for Windows x64
task setup:build-tool

# Specify configuration
task setup:build-tool CONFIG=Debug

# Specify runtime
task setup:build-tool RUNTIME=linux-x64

# Both
task setup:build-tool CONFIG=Release RUNTIME=osx-arm64
```

## Build Process Flow

```
1. Developer runs: task setup:build-tool
   ↓
2. Taskfile sets environment variables (PREP_TOOL_CONFIG, PREP_TOOL_RUNTIME)
   ↓
3. Calls Nuke target: BuildPreparationTool
   ↓
4. NUKE calls: dotnet publish
   ↓
5. .NET SDK compiles and publishes single-file exe
   ↓
6. MSBuild CopyToolToPackage target runs
   ↓
7. Single-file exe copied to: packages/.../Tools/SangoCard.Build.Tool.exe
   ↓
8. ✅ Tool ready for use (103 MB self-contained exe)
```

## File Locations

**Source Project**:
```
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/
├── SangoCard.Build.Tool.csproj  (updated with copy targets)
├── Program.cs
├── Core/
├── Tui/
└── ... (1,348 files total)
```

**Published Output**:
```
packages/scoped-6571/com.contractwork.sangocard.build/Tools/
└── SangoCard.Build.Tool.exe  (103 MB single-file, self-contained)
```

**Build Artifacts** (not distributed):
```
packages/.../dotnet~/tool/SangoCard.Build.Tool/bin/Release/net8.0/win-x64/
├── publish/
│   ├── SangoCard.Build.Tool.exe  (single-file bundle)
│   └── SangoCard.Build.Tool.pdb  (debug symbols)
└── ... (intermediate files)
```

## Impact on Cache & Injection

### Before Optimization

**com.contractwork.sangocard.build** package in cache:
- Total: 1,445 files (293 MB)
  - `dotnet~/tool/` - 1,348 files (293 MB source code)
  - `Editor/` - 83 files
  - `Tests/` - 7 files
  - Other - 7 files

### After Building Tool (Current State)

**com.contractwork.sangocard.build** package:
- Total: 1,446 files (396 MB)
  - `dotnet~/tool/` - 1,348 files (293 MB source code) - **still in cache**
  - `Tools/` - 1 file (103 MB exe) - **NEW**
  - `Editor/` - 83 files
  - Other - 14 files

**Size increased temporarily** because both source and exe are present.

### After Cache Exclusion (Future Optimization)

When you exclude `dotnet~/` from cache population:
- Total: 98 files (103 MB)
  - `Tools/` - 1 file (103 MB exe)
  - `Editor/` - 83 files
  - Other - 14 files

**Reduction**: 1,348 fewer files, 190 MB smaller (compared to before, excluding the runtime overhead)

## Next Steps

### Option 1: Keep Source in Cache (Current)

**Pros**:
- Developers can rebuild the tool if needed
- Full source available for debugging

**Cons**:
- Wastes 1,348 files (293 MB) in cache
- Slower cache population

### Option 2: Exclude dotnet~ from Cache (Recommended)

Modify cache population to skip the `dotnet~/` folder:

**Benefits**:
- Reduces cache by 1,348 files (293 MB)
- Faster cache population and injection
- Only the pre-built tool is distributed

**Drawbacks**:
- Need to rebuild tool manually if changes are made
- Source not available in cached packages

### Option 3: Add Post-Injection Cleanup

Add `assetManipulations` to delete `dotnet~/` after injecting to client:

```json
{
  "name": "preBuild",
  "packages": [...],
  "assetManipulations": [
    {
      "type": "delete",
      "target": "projects/client/Packages/com.contractwork.sangocard.build/dotnet~",
      "description": "Remove build tool source code (only exe needed)"
    }
  ]
}
```

**Benefits**:
- Source available in original package
- Client project doesn't get the source files
- Smaller client project footprint

## Development Workflow

### Making Changes to the Tool

1. Edit source files in `packages/.../dotnet~/tool/SangoCard.Build.Tool/`
2. Test with: `dotnet run --project <path-to-csproj> -- <args>`
3. When ready, rebuild: `task setup:build-tool`
4. The new exe is automatically copied to Tools/
5. Commit both source changes and the updated exe

### Building for Multiple Platforms

```bash
# Windows (via Task)
task setup:build-tool RUNTIME=win-x64

# Linux
task setup:build-tool RUNTIME=linux-x64

# macOS Intel
task setup:build-tool RUNTIME=osx-x64

# macOS Apple Silicon
task setup:build-tool RUNTIME=osx-arm64

# Or set environment variable and call Nuke directly
$env:PREP_TOOL_RUNTIME="linux-x64"
./build/nuke/build.ps1 BuildPreparationTool
```

### Testing Different Configurations

```bash
# Release (optimized, smaller)
task setup:build-tool CONFIG=Release

# Debug (with symbols, easier debugging)
task setup:build-tool CONFIG=Debug
```

Debug builds are copied with `-debug` suffix: `SangoCard.Build.Tool-debug.exe`

## Technical Details

### Single-File Publishing

The tool uses .NET's single-file publishing feature:
- All dependencies bundled into one executable
- Runtime included (self-contained)
- Native libraries extracted to temp on first run
- Approximately 103 MB for win-x64 (includes .NET runtime)

### MSBuild Targets

**CopyToolToPackage** (after Publish):
- Triggers: After `dotnet publish`
- Source: `$(PublishDir)/SangoCard.Build.Tool.exe`
- Destination: `$(PackageToolsDir)/SangoCard.Build.Tool.exe`
- Creates Tools directory if missing

**CopyToolToPackageAfterBuild** (after Build):
- Triggers: After `dotnet build` (Debug mode)
- Source: `$(OutputPath)/SangoCard.Build.Tool.exe`
- Destination: `$(PackageToolsDir)/SangoCard.Build.Tool-debug.exe`
- For development/testing purposes

### Path Resolution

```
PackageToolsDir = $(MSBuildThisFileDirectory)../../../Tools
                = dotnet~/tool/SangoCard.Build.Tool/../../../Tools
                = dotnet~/Tools  (wrong! - but MSBuild resolves it)
                = packages/.../com.contractwork.sangocard.build/Tools ✓
```

## Troubleshooting

### Tool not copied after publish

**Check**: Look for MSBuild messages during publish
**Fix**: Run with higher verbosity: `dotnet publish --verbosity detailed`

### Tool copied to wrong location

**Check**: Verify `$(PackageToolsDir)` path resolution
**Fix**: Adjust the relative path `../../../Tools` in .csproj

### Exe is too large

**Current**: 103 MB (self-contained with runtime)
**Reduce**: Use framework-dependent publishing (but requires .NET 8 on target)
```bash
dotnet publish --self-contained false
```

### Missing native libraries

**Error**: "Unable to find library"
**Fix**: Ensure `IncludeNativeLibrariesForSelfExtract=true` is set

## References

- .NET Single-File Publishing: https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file
- MSBuild Targets: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets
- Build preparation system: `build/nuke/build/Build.Preparation.cs`
- Optimization notes: `build/configs/preparation/OPTIMIZATION-NOTES.md`
