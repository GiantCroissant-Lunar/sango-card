---
doc_id: DOC-2025-00158
title: REVISED APPROACH
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [revised-approach]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00109
title: REVISED APPROACH
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [revised-approach]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00104
title: REVISED APPROACH
doc_type: guide
status: archived
canonical: false
created: 2025-10-17
tags: [revised-approach]
summary: >
  (Add summary here)
source:
  author: system
---
# Revised Approach: Use Unity Packages Directly

## Key Insight

Instead of extracting DLLs, treat Microsoft.Extensions packages as normal Unity packages and copy them directly. This is cleaner and follows Unity's package management conventions.

## New Strategy

### 1. Build Preparation Cache Structure

Create a centralized cache folder for all build dependencies:

```
build/preparation/
├── unity-packages/          # Unity packages to copy to client/Packages/
│   ├── microsoft.extensions.logging/
│   ├── microsoft.extensions.logging.abstractions/
│   ├── microsoft.extensions.dependencyinjection/
│   ├── microsoft.extensions.dependencyinjection.abstractions/
│   ├── com.contractwork.sangocard.build/
│   └── com.contractwork.sangocard.cross/
├── assemblies/              # Plain DLLs to copy to client/Assets/Plugins/assemblies/
│   ├── System.CommandLine.dll
│   └── Splat.dll
└── all/                     # Platform-agnostic assets (from your config)
    ├── Sentry/
    ├── TextMesh Pro/
    ├── Directory.Build.props
    └── csc.rsp
```

### 2. Package Preparation Steps

#### Step 1: Copy Microsoft.Extensions Packages to Cache

```powershell
# Source: code-quality PackageCache
# Destination: build/preparation/unity-packages/

Copy-Item `
  "projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.logging@c95ee2e3b656" `
  "build/preparation/unity-packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656" `
  -Recurse -Force

Copy-Item `
  "projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5" `
  "build/preparation/unity-packages/org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5" `
  -Recurse -Force

Copy-Item `
  "projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43" `
  "build/preparation/unity-packages/org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43" `
  -Recurse -Force

Copy-Item `
  "projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9" `
  "build/preparation/unity-packages/org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9" `
  -Recurse -Force
```

#### Step 2: Copy Custom Packages to Cache

```powershell
# These are already in packages/ but we reference them from cache for consistency
# Or we can reference them directly from packages/ in the preparation config
```

#### Step 3: Copy Plain DLLs to Cache

```powershell
# For DLLs that aren't in Unity package format
Copy-Item `
  "projects/code-quality/Assets/Packages/System.CommandLine.2.0.0-rc.2.25502.107/lib/netstandard2.0/System.CommandLine.dll" `
  "build/preparation/assemblies/" -Force

Copy-Item `
  "projects/code-quality/Assets/Packages/Splat.15.3.1/lib/netstandard2.0/Splat.dll" `
  "build/preparation/assemblies/" -Force
```

### 3. Updated Preparation Config

The config should reference the cache folder:

```json
{
  "preparations": [
    {
      "name": "Prepare for build - Sango Card",
      "assetManipulations": [
        {
          "category": "Add Unity packages",
          "assets": [
            {
              "source": {
                "rootPath": "../preparation",
                "path": "unity-packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../preparation",
                "path": "unity-packages/org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../preparation",
                "path": "unity-packages/org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../preparation",
                "path": "unity-packages/org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../../packages",
                "path": "scoped-6571/com.contractwork.sangocard.build",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/com.contractwork.sangocard.build",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../../packages",
                "path": "scoped-2151/com.contractwork.sangocard.cross",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Packages/com.contractwork.sangocard.cross",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            }
          ]
        },
        {
          "category": "Add plain DLL assemblies",
          "assets": [
            {
              "source": {
                "rootPath": "../preparation",
                "path": "assemblies/System.CommandLine.dll",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Assets/Plugins/assemblies/System.CommandLine.dll",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            },
            {
              "source": {
                "rootPath": "../preparation",
                "path": "assemblies/Splat.dll",
                "useRegex": false
              },
              "destination": {
                "rootPath": "../../projects/client",
                "path": "Assets/Plugins/assemblies/Splat.dll",
                "useRegex": false
              },
              "status": {
                "presence": "present",
                "operation": "copy"
              },
              "enable": true
            }
          ]
        }
      ]
    }
  ]
}
```

### 4. Benefits of This Approach

✅ **Cleaner:**

- No manual DLL extraction
- Follows Unity package conventions
- Preserves package metadata and structure

✅ **Maintainable:**

- Centralized cache in `build/preparation/`
- Easy to update packages
- Clear separation of Unity packages vs plain DLLs

✅ **Consistent:**

- Same approach for all Unity packages
- Matches your existing config pattern
- Works with Unity's package resolution

✅ **Flexible:**

- Can version packages in cache
- Easy to add/remove dependencies
- Cache can be committed to git or generated

### 5. Custom Package Changes Needed

Since we're copying packages as-is, we need to **remove the DLLs from custom packages**:

#### For `com.contractwork.sangocard.build`

- Remove `Editor/Plugins/` folder (DLLs will come from Unity packages or plain assemblies)
- Update `SangoCard.Build.Editor.asmdef` to reference Unity packages instead

#### For `com.contractwork.sangocard.cross`

- Remove `Editor/Plugins/` folder
- Update `SangoCard.Cross.Editor.asmdef` to reference Unity packages instead

### 6. Assembly Definition Changes

**Build Package (`SangoCard.Build.Editor.asmdef`):**

```json
{
  "name": "SangoCard.Build.Editor",
  "references": [
    "SangoCard.Cross.Editor",
    "Unity.Addressables",
    "Unity.Addressables.Editor",
    "Unity.ResourceManager",
    "Microsoft.Extensions.Logging",
    "Microsoft.Extensions.Logging.Abstractions",
    "Microsoft.Extensions.DependencyInjection",
    "Microsoft.Extensions.DependencyInjection.Abstractions"
  ],
  "precompiledReferences": [
    "System.CommandLine.dll",
    "Splat.dll"
  ]
}
```

**Cross Package (`SangoCard.Cross.Editor.asmdef`):**

```json
{
  "name": "SangoCard.Cross.Editor",
  "references": [],
  "precompiledReferences": [
    "Splat.dll"
  ]
}
```

### 7. Implementation Steps

1. **Create cache structure:**

   ```powershell
   New-Item -Path "build/preparation/unity-packages" -ItemType Directory -Force
   New-Item -Path "build/preparation/assemblies" -ItemType Directory -Force
   ```

2. **Populate cache:**
   - Copy Microsoft.Extensions packages from PackageCache
   - Copy plain DLLs (System.CommandLine, Splat)

3. **Clean custom packages:**
   - Remove `Editor/Plugins/` folders
   - Update asmdef files to use package references

4. **Update preparation config:**
   - Add Microsoft.Extensions packages to asset manipulations
   - Reference cache folder paths

5. **Test in code-quality:**
   - Verify packages work with references instead of embedded DLLs

6. **Implement preparation executor in Nuke:**
   - Parse config
   - Copy packages and DLLs to client
   - Modify manifest.json

## Next Actions

1. Create `build/preparation/` cache structure
2. Copy packages and DLLs to cache
3. Clean up custom packages (remove embedded DLLs)
4. Update asmdef files to use references
5. Test in code-quality project
6. Update preparation config JSON
7. Implement Nuke preparation component

This approach is much cleaner and aligns with Unity's package management!
