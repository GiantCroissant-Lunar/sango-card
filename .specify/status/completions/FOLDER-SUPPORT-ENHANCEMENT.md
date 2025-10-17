# Enhancement: Folder Support for Packages and Assemblies

**Date:** 2025-10-17  
**Type:** Enhancement  
**Status:** Implemented

## Problem

The original implementation only supported single files for packages (`.tgz`) and assemblies (`.dll`). This was too restrictive for real-world scenarios where:

1. Unity packages might be in folder format (not archived)
2. Assemblies might come with dependencies in a folder structure
3. Plugin folders need to be copied as-is

## Solution

Enhanced `PreparationService.cs` to support **both files and folders** for:
- **Packages** (`config.Packages`)
- **Assemblies** (`config.Assemblies`)

### Implementation Changes

**File:** `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/SangoCard.Build.Tool/Core/Services/PreparationService.cs`

#### 1. Updated Package Copying (Lines 183-216)

**Before:**
```csharp
// Only supported single files
File.Copy(src, dst, overwrite: true);
```

**After:**
```csharp
// Support both files and directories
if (Directory.Exists(src))
{
    // Copy directory recursively
    CopyDirectory(src, dst, overwrite: true);
}
else if (File.Exists(src))
{
    // Copy single file
    EnsureDirectoryOf(dst);
    File.Copy(src, dst, overwrite: true);
}
else
{
    throw new FileNotFoundException($"Package source not found: {src}");
}
```

#### 2. Updated Assembly Copying (Lines 218-251)

Same pattern as packages - supports both files and folders.

#### 3. Added CopyDirectory Helper Method (Lines 491-511)

```csharp
private void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
{
    // Create target directory
    Directory.CreateDirectory(targetDir);

    // Copy all files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
        var fileName = Path.GetFileName(file);
        var targetFile = Path.Combine(targetDir, fileName);
        File.Copy(file, targetFile, overwrite);
    }

    // Recursively copy subdirectories
    foreach (var subDir in Directory.GetDirectories(sourceDir))
    {
        var dirName = Path.GetFileName(subDir);
        var targetSubDir = Path.Combine(targetDir, dirName);
        CopyDirectory(subDir, targetSubDir, overwrite);
    }
}
```

## Configuration Examples

### Single File Package (Original)

```json
{
  "packages": [
    {
      "name": "com.unity.addressables",
      "version": "1.21.2",
      "source": "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
      "target": "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
    }
  ]
}
```

### Folder Package (New)

```json
{
  "packages": [
    {
      "name": "com.example.custompkg",
      "version": "2.0.0",
      "source": "build/preparation/cache/com.example.custompkg",
      "target": "projects/client/Packages/com.example.custompkg"
    }
  ]
}
```

### Single File Assembly (Original)

```json
{
  "assemblies": [
    {
      "name": "Microsoft.Extensions.DependencyInjection",
      "version": "8.0.0",
      "source": "build/preparation/cache/Microsoft.Extensions.DependencyInjection.dll",
      "target": "projects/client/Assets/Plugins/Microsoft.Extensions.DependencyInjection.dll"
    }
  ]
}
```

### Folder Assembly (New)

```json
{
  "assemblies": [
    {
      "name": "MyPluginWithDependencies",
      "version": "1.0.0",
      "source": "build/preparation/cache/MyPluginWithDependencies",
      "target": "projects/client/Assets/Plugins/MyPluginWithDependencies"
    }
  ]
}
```

## Use Cases

### 1. Unity Package Folders

Some Unity packages are distributed as folders rather than `.tgz` archives:

```
build/preparation/cache/
└── com.example.package/
    ├── package.json
    ├── Runtime/
    │   └── Scripts/
    └── Editor/
        └── Scripts/
```

### 2. Plugin Folders with Dependencies

Native plugins or assemblies with dependencies:

```
build/preparation/cache/
└── NativePlugin/
    ├── NativePlugin.dll
    ├── NativePlugin.xml
    └── Dependencies/
        ├── Dependency1.dll
        └── Dependency2.dll
```

### 3. Asset Bundles

Pre-built asset bundles with metadata:

```
build/preparation/cache/
└── AssetBundles/
    ├── bundle1.unity3d
    ├── bundle2.unity3d
    └── manifest.json
```

## Logging

The tool now logs whether it's copying a file or folder:

```
[INFO] Copied package (file): com.unity.addressables -> projects/client/Packages/...
[INFO] Copied package (folder): com.example.custompkg -> projects/client/Packages/...
[INFO] Copied assembly (file): MyLib.dll -> projects/client/Assets/Plugins/...
[INFO] Copied assembly (folder): MyPluginFolder -> projects/client/Assets/Plugins/...
```

## Backward Compatibility

✅ **Fully backward compatible**

- Existing configurations with single files continue to work
- No breaking changes to configuration schema
- Tool automatically detects whether source is file or folder

## Testing

### Manual Testing

1. **File-based package:**
   ```bash
   # Create config with .tgz file
   # Run: task build:prepare:client
   # Verify: Single file copied
   ```

2. **Folder-based package:**
   ```bash
   # Create config with folder source
   # Run: task build:prepare:client
   # Verify: Entire folder copied recursively
   ```

3. **Mixed configuration:**
   ```bash
   # Config with both files and folders
   # Run: task build:prepare:client
   # Verify: All items copied correctly
   ```

### Dry-Run Testing

```bash
task build:prepare:dry-run
```

Should show:
```
[DRY-RUN] Copied package (file): ...
[DRY-RUN] Copied package (folder): ...
```

## Documentation Updates

Updated files:
- ✅ `docs/guides/build-preparation-workflow.md`
  - Added folder support documentation
  - Updated configuration examples
  - Added note about file/folder support

## Benefits

1. **Flexibility:** Supports both archived and unarchived packages
2. **Simplicity:** No need to archive folders before caching
3. **Performance:** Direct folder copy can be faster than extract
4. **Compatibility:** Works with Unity's package folder format
5. **Debugging:** Easier to inspect folder contents vs archives

## Migration

**No migration needed!** Existing configurations work as-is.

To use folder support, simply:
1. Place folder in cache instead of `.tgz` file
2. Update config `source` to point to folder
3. Update config `target` to folder destination

## Related

- **Original Issue:** "This is not reasonable, we have to adjust, file(s) and folder(s) have to be supported"
- **Rule Compliance:** R-BLD-060 (still enforced - client read-only outside builds)
- **Spec:** SPEC-BPT-001 (enhancement, not breaking change)

---

**Status:** ✅ Implemented and documented  
**Breaking Changes:** None  
**Backward Compatible:** Yes
