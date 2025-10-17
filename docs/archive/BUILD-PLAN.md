# Unity Build Plan for Sango Card Client

## Current Situation

**Target Project:** `D:\lunar-snake\constract-work\card-projects\sango-card\projects\client`
**Current Unity Version:** 2022.3.7f1 (incorrect - needs upgrade)
**Target Unity Version:** 6000.2.x (Unity 6)
**Build System:** Nuke + Custom Unity Build Package

## Problem Analysis

### 1. Unity Version Mismatch

- Client project is on Unity 2022.3.7f1
- Build packages require Unity 6000.2.x (Unity 6)
- Build package uses Unity 6-specific APIs (UnityEditor.Build.Profile)

### 2. Missing Dependencies

Build package (com.contractwork.sangocard.build) requires:

- System.CommandLine.dll - Available in code-quality project
- Splat.dll - Available in code-quality project  
- Microsoft.Extensions.Logging - NOT found, needs to be sourced
- Microsoft.Extensions.DependencyInjection - NOT found, needs to be sourced
- com.contractwork.sangocard.cross - Exists but incomplete

### 3. Package Issues

- Build package (scoped-6571/com.contractwork.sangocard.build):
  - Has compilation errors due to missing dependencies
  - References cross package
  - Needs DLL files copied

- Cross package (scoped-2151/com.contractwork.sangocard.cross):
  - Exists with source code
  - Missing DLL dependencies
  - Needs to be functional before build package works

## Build Flow (Correct Process)

### Phase 1: Pre-Build Preparation (Augmentation)

1. Upgrade client project from Unity 2022.3.7f1 to Unity 6000.2.x
2. Copy required packages to client/Packages:
   - com.contractwork.sangocard.build
   - com.contractwork.sangocard.cross
3. Copy required DLLs to packages
4. Modify client manifest.json (remove conflicting packages, add build packages)

### Phase 2: Build Execution

1. Nuke calls Unity CLI with correct version
2. Unity loads augmented project with build packages
3. Build package provides BuildEntry.PerformBuild method
4. Unity executes build based on command-line parameters

### Phase 3: Post-Build Cleanup

1. Output artifacts collected
2. Optional: Restore original client state

## Action Plan

### Step 1: Upgrade Unity Project

**Priority: CRITICAL**

- Upgrade projects/client from Unity 2022.3.7f1 to Unity 6000.2.x
- Test project opens successfully
- Resolve any upgrade issues

### Step 2: Source Missing DLLs

**Priority: HIGH**

- Find or download Microsoft.Extensions.Logging NuGet package
- Find or download Microsoft.Extensions.DependencyInjection NuGet package
- Extract DLLs compatible with .NET Standard 2.0/2.1

### Step 3: Fix Cross Package

**Priority: HIGH**
Location: packages/scoped-2151/com.contractwork.sangocard.cross

Actions:

- Update package.json unity version from 6000.0 to 2022.3 (or remove if upgrading)
- Copy required DLLs to package
- Update asmdef if needed to reference DLLs
- Verify package compiles

### Step 4: Fix Build Package  

**Priority: HIGH**
Location: packages/scoped-6571/com.contractwork.sangocard.build

Actions:

- Copy System.CommandLine.dll from code-quality project
- Copy Splat.dll from code-quality project
- Copy Microsoft.Extensions.* DLLs
- Update package.json unity version
- Create Plugins folder structure for DLLs
- Update asmdef precompiledReferences
- Verify package compiles

### Step 5: Create Build Augmentation Script

**Priority: MEDIUM**
Create: build/nuke/scripts/augment-client.ps1

Purpose:

- Backup original client manifest.json
- Copy build packages to client/Packages
- Modify manifest.json to include build packages
- Remove conflicting packages if needed

### Step 6: Update Nuke Build Component

**Priority: MEDIUM**
File: build/nuke/build/Components/IUnityBuild.cs

Actions:

- Add pre-build augmentation step
- Ensure correct Unity 6 path
- Add post-build cleanup
- Handle git reset --hard for client repo

### Step 7: Test Build Flow

**Priority: HIGH**

- Run augmentation script manually
- Test Unity opens augmented project
- Test build package loads
- Run full Nuke build
- Verify output artifacts

## Required Files to Copy

### From code-quality/Assets/Packages to build package

- System.CommandLine.2.0.0-rc.2.25502.107/lib/netstandard2.0/System.CommandLine.dll
- Splat.15.3.1/lib/netstandard2.0/Splat.dll

### To be sourced externally

- Microsoft.Extensions.Logging.dll
- Microsoft.Extensions.Logging.Abstractions.dll  
- Microsoft.Extensions.DependencyInjection.dll
- Microsoft.Extensions.DependencyInjection.Abstractions.dll

## Package Structure After Fix

```
packages/scoped-6571/com.contractwork.sangocard.build/
  Editor/
    Plugins/
      System.CommandLine.dll
      Splat.dll
      Microsoft.Extensions.Logging.dll
      Microsoft.Extensions.Logging.Abstractions.dll
      Microsoft.Extensions.DependencyInjection.dll
      Microsoft.Extensions.DependencyInjection.Abstractions.dll
    Core/
      BuildEntry.cs
      ...
    Module/
    ...
  package.json (unity: 2022.3 or remove for Unity 6)

packages/scoped-2151/com.contractwork.sangocard.cross/
  Editor/
    Module/
    Utility/
    ...
  package.json (unity: 2022.3 or remove for Unity 6)
```

## Next Immediate Actions

1. **DECISION REQUIRED:** Upgrade client to Unity 6 or downgrade packages to Unity 2022?
   - Recommended: Upgrade to Unity 6 (matches build package design)

2. **Source Microsoft.Extensions DLLs:**
   - Download from NuGet.org
   - Extract .NET Standard 2.0 compatible versions

3. **Copy DLLs to build package**

4. **Test package compilation in isolation**

5. **Implement augmentation script**

6. **Test full build flow**

## Risk Mitigation

- Client project is a separate git repo - use git reset --hard before build
- Backup manifest.json before modification
- Test augmentation reversibility
- Verify Unity 6 compatibility before full upgrade
- Keep Unity 2022.3.7f1 available for rollback

## Success Criteria

- [ ] Client project upgraded to Unity 6000.2.x
- [ ] All packages compile without errors
- [ ] Nuke build successfully augments client project
- [ ] Unity CLI build executes without errors
- [ ] Build artifacts generated in output folder
- [ ] Client project restored to clean state after build
