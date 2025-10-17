---
doc_id: DOC-2025-00098
title: IMPLEMENTATION PLAN
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [implementation-plan]
summary: >
  (Add summary here)
source:
  author: system
---
# Sango Card Build Implementation Plan

## Overview

**Goal:** Fix custom Unity packages and establish automated build flow for client project

**Strategy:**

1. Fix packages in `code-quality` (Unity 6000.2.x) first
2. Use existing preparation config to augment `client` before build
3. Build `client` with Unity 6000.2.x despite being Unity 2022.3.7f1 in development

## Current State

### Projects

- **code-quality:** `D:\lunar-snake\constract-work\card-projects\sango-card\projects\code-quality` (Unity 6000.2.x)
  - Used to test custom packages
  - Currently has packages with compilation errors

- **client:** `D:\lunar-snake\constract-work\card-projects\sango-card\projects\client` (Unity 2022.3.7f1)
  - Target for production builds
  - Separate git repository (read-only except during build)
  - Will be augmented before build via preparation config

### Custom Packages

- **Build Package:** `packages/scoped-6571/com.contractwork.sangocard.build`
  - Provides `BuildEntry.PerformBuild` method for Unity CLI
  - Missing DLLs and has compilation errors

- **Cross Package:** `packages/scoped-2151/com.contractwork.sangocard.cross`
  - Dependency of build package
  - Missing DLLs

### Available Resources

- **DLLs in code-quality:**
  - `System.CommandLine.dll` ✅
  - `Splat.dll` ✅
  - Many other NuGet packages ✅

- **Preparation Config:** Existing JSON config defining:
  - Manifest modifications
  - Code patches
  - Asset manipulations (copy/remove/symlink)
  - Scripting define symbols

## Phase 1: Fix Custom Packages in code-quality

### Step 1.1: Copy Required DLLs to Cross Package

**Target:** `packages/scoped-2151/com.contractwork.sangocard.cross`

**Actions:**

```powershell
# Create Plugins directory structure
New-Item -Path "packages/scoped-2151/com.contractwork.sangocard.cross/Editor/Plugins" -ItemType Directory -Force

# Copy Splat (for dependency injection)
Copy-Item `
  "projects/code-quality/Assets/Packages/Splat.15.3.1/lib/netstandard2.0/Splat.dll" `
  "packages/scoped-2151/com.contractwork.sangocard.cross/Editor/Plugins/"
```

**Update asmdef:**

- Add `Splat.dll` to `precompiledReferences` in `SangoCard.Cross.Editor.asmdef`

### Step 1.2: Copy Required DLLs to Build Package

**Target:** `packages/scoped-6571/com.contractwork.sangocard.build`

**Actions:**

```powershell
# Create Plugins directory
New-Item -Path "packages/scoped-6571/com.contractwork.sangocard.build/Editor/Plugins" -ItemType Directory -Force

# Copy System.CommandLine
Copy-Item `
  "projects/code-quality/Assets/Packages/System.CommandLine.2.0.0-rc.2.25502.107/lib/netstandard2.0/System.CommandLine.dll" `
  "packages/scoped-6571/com.contractwork.sangocard.build/Editor/Plugins/"

# Copy Splat
Copy-Item `
  "projects/code-quality/Assets/Packages/Splat.15.3.1/lib/netstandard2.0/Splat.dll" `
  "packages/scoped-6571/com.contractwork.sangocard.build/Editor/Plugins/"

# Copy Microsoft.Extensions.Logging (need to source)
# Copy Microsoft.Extensions.DependencyInjection (need to source)
```

**Update asmdef:**

- `System.CommandLine.dll` already in `precompiledReferences` ✅
- Add other DLLs as needed

### Step 1.3: Source Microsoft.Extensions DLLs

**Option A: Download from NuGet**

```powershell
# Use NuGet CLI or download manually from nuget.org
# Need .NET Standard 2.0 compatible versions:
# - Microsoft.Extensions.Logging (8.0.0)
# - Microsoft.Extensions.Logging.Abstractions (8.0.0)
# - Microsoft.Extensions.DependencyInjection (8.0.0)
# - Microsoft.Extensions.DependencyInjection.Abstractions (8.0.0)
```

**Option B: Check if already in code-quality**

```powershell
# Search for existing Microsoft.Extensions packages
Get-ChildItem "projects/code-quality/Assets/Packages" -Recurse -Filter "Microsoft.Extensions*.dll"
```

### Step 1.4: Add Packages to code-quality manifest.json

**File:** `projects/code-quality/Packages/manifest.json`

**Add:**

```json
{
  "dependencies": {
    "com.contractwork.sangocard.cross": "file:../../../packages/scoped-2151/com.contractwork.sangocard.cross",
    "com.contractwork.sangocard.build": "file:../../../packages/scoped-6571/com.contractwork.sangocard.build",
    // ... existing packages
  }
}
```

### Step 1.5: Fix Unity 6-Specific Code

**Issue:** `UnityEditor.Build.Profile` only exists in Unity 6

**File:** `packages/scoped-6571/com.contractwork.sangocard.build/Editor/Core/BuildEntry.cs`

**Solution:** Already has fallback logic for when BuildProfile is not available ✅

### Step 1.6: Test Compilation in code-quality

**Actions:**

1. Open `code-quality` project in Unity 6000.2.x
2. Wait for package import
3. Check Console for compilation errors
4. Fix any remaining errors

## Phase 2: Implement Preparation System

### Step 2.1: Understand Existing Config

**Config Location:** (Need to determine where to place it)

- Suggested: `build/nuke/configs/build-preparation.json`

**Key Operations:**

- **manifestModification:** Remove packages from client manifest
- **assetManipulations:** Copy/remove/symlink files
- **codePatches:** Modify source files
- **scriptingDefineSymbols:** Add/remove defines

### Step 2.2: Create Preparation Executor

**Option A:** Implement in Nuke (C#)

- Create `Build.Preparation.cs` partial class
- Parse JSON config
- Execute operations before Unity build

**Option B:** PowerShell Script

- Create `build/nuke/scripts/prepare-client.ps1`
- Parse JSON and execute operations
- Call from Nuke before Unity build

**Recommended:** Option A (C# in Nuke) for better integration

### Step 2.3: Key Preparation Operations

**Before Unity Build:**

1. **Backup client state** (git status check)
2. **Modify manifest.json:**
   - Remove: `com.github-glitchenzo.nugetforunity`, `com.unity.visualscripting`, etc.
   - Add: Custom packages via file: references
3. **Copy packages:**
   - `com.contractwork.sangocard.build` → `client/Packages/`
   - `com.contractwork.sangocard.cross` → `client/Packages/`
   - Other custom packages as per config
4. **Copy DLLs:**
   - NuGet assemblies → `client/Assets/Plugins/assemblies/`
5. **Apply code patches:**
   - Fix `CreateFormationEntityController.cs`
   - Fix `RankMatchModel.cs`
   - Fix `TooltipExtensions.cs`
6. **Remove conflicting assets:**
   - Old MessagePack versions
   - Test folders
   - etc.

**After Unity Build:**

1. **Restore client state** (`git reset --hard`)
2. **Collect build artifacts**

## Phase 3: Update Nuke Build System

### Step 3.1: Create Preparation Component

**File:** `build/nuke/build/Components/IBuildPreparation.cs`

```csharp
interface IBuildPreparation : INukeBuild
{
    [Parameter("Build preparation config path")]
    AbsolutePath PreparationConfigPath => TryGetValue(() => PreparationConfigPath)
        ?? RootDirectory / "build" / "nuke" / "configs" / "build-preparation.json";

    Target PrepareClient => _ => _
        .Description("Prepare client project for build")
        .Executes(() =>
        {
            // 1. Load config
            // 2. Backup client state
            // 3. Execute preparation operations
            // 4. Verify preparation succeeded
        });

    Target RestoreClient => _ => _
        .Description("Restore client project to original state")
        .Executes(() =>
        {
            // git reset --hard in client directory
        });
}
```

### Step 3.2: Update Build.UnityBuild.cs

**Add dependency:**

```csharp
partial class Build : IUnityBuild, IBuildPreparation
{
    // Existing implementation
}
```

**Update BuildUnity target:**

```csharp
Target BuildUnity => _ => _
    .DependsOn(PrepareClient)  // Add this
    .Description("Build Unity project")
    .Executes(() => { /* existing code */ })
    .Finally(() => RestoreClient());  // Add this
```

### Step 3.3: Handle Unity Version

**Update IUnityBuild.cs:**

```csharp
[Parameter("Unity executable path")]
string UnityPath => TryGetValue(() => UnityPath) ?? GetUnity6Path();

private string GetUnity6Path()
{
    // Find Unity 6000.2.x installation
    var editorPath = @"C:\Program Files\Unity\Hub\Editor";
    var unity6Dirs = Directory.GetDirectories(editorPath, "6000.2.*");

    if (unity6Dirs.Length == 0)
        throw new Exception("Unity 6000.2.x not found");

    return Path.Combine(unity6Dirs[0], "Editor", "Unity.exe");
}
```

## Phase 4: Directory Structure After Setup

```
sango-card/
├── build/
│   └── nuke/
│       ├── build/
│       │   ├── Build.cs
│       │   ├── Build.UnityBuild.cs
│       │   ├── Build.Preparation.cs (NEW)
│       │   └── Components/
│       │       ├── IUnityBuild.cs (UPDATED)
│       │       └── IBuildPreparation.cs (NEW)
│       ├── configs/
│       │   └── build-preparation.json (MOVED FROM YOUR CONFIG)
│       └── scripts/
│           └── (existing scripts)
├── packages/
│   ├── scoped-2151/
│   │   └── com.contractwork.sangocard.cross/
│   │       └── Editor/
│   │           ├── Plugins/
│   │           │   └── Splat.dll (NEW)
│   │           └── (existing files)
│   └── scoped-6571/
│       └── com.contractwork.sangocard.build/
│           └── Editor/
│               ├── Plugins/
│               │   ├── System.CommandLine.dll (NEW)
│               │   ├── Splat.dll (NEW)
│               │   ├── Microsoft.Extensions.Logging.dll (NEW)
│               │   └── Microsoft.Extensions.DependencyInjection.dll (NEW)
│               └── (existing files)
└── projects/
    ├── code-quality/ (Unity 6000.2.x - for testing packages)
    │   └── Packages/
    │       ├── manifest.json (UPDATED with custom packages)
    │       └── (Unity packages)
    └── client/ (Unity 2022.3.7f1 - build target, read-only)
        └── (augmented during build only)
```

## Phase 5: Execution Steps

### Step 1: Fix Packages (Manual)

```powershell
# Copy DLLs
.\copy-dlls-to-packages.ps1

# Update package.json files
# Update asmdef files

# Test in code-quality
# Open Unity 6000.2.x with code-quality project
# Verify no compilation errors
```

### Step 2: Implement Preparation System

```powershell
# Create IBuildPreparation.cs
# Create Build.Preparation.cs
# Move config to build/nuke/configs/
# Update Build.UnityBuild.cs
```

### Step 3: Test Build Flow

```powershell
cd build/nuke

# Test preparation only
.\build.ps1 PrepareClient --UnityProjectPath 'D:\lunar-snake\constract-work\card-projects\sango-card\projects\client'

# Test full build
.\build.ps1 BuildUnity `
  --UnityProjectPath 'D:\lunar-snake\constract-work\card-projects\sango-card\projects\client' `
  --UnityBuildProfileName 'Windows' `
  --UnityBuildPurpose 'UnityPlayer' `
  --UnityBuildVersion '1.0.0'
```

## Success Criteria

- [ ] Custom packages compile without errors in code-quality
- [ ] Preparation system successfully augments client project
- [ ] Unity 6000.2.x can open augmented client project
- [ ] Build completes successfully with artifacts in output folder
- [ ] Client project restored to clean state after build
- [ ] All operations logged and traceable

## Next Immediate Actions

1. **Copy DLLs to packages** (I can help with this)
2. **Update asmdef files** to reference new DLLs
3. **Test packages in code-quality** project
4. **Source Microsoft.Extensions DLLs** if not already available
5. **Implement IBuildPreparation** component in Nuke

## Notes

- Client project is separate git repo - must use `git reset --hard` before build
- Unity 6000.2.x will be used to build despite client being 2022.3.7f1
- Preparation config already exists - just need to implement executor
- DLL dependencies: Unity packages go to Packages/, plain DLLs go to Assets/Plugins/assemblies/
