---
doc_id: DOC-2025-00137
title: TOOL DESIGN
doc_type: guide
status: draft
canonical: false
created: 2025-10-17
tags: [tool-design]
summary: >
  (Add summary here)
source:
  author: system
---
# Build Preparation Tool Design

## Overview

A .NET CLI tool for managing Unity build preparation configurations. This tool will:

1. Create/update preparation config JSON
2. Populate build/preparation cache
3. Execute preparation operations
4. Apply code patches and scripting defines

## Tool Location

```
packages/scoped-6571/com.contractwork.sangocard.build/
└── dotnet~/
    └── tool/
        ├── SangoCard.Build.Tool.sln
        └── SangoCard.Build.Tool/
            ├── SangoCard.Build.Tool.csproj
            ├── Program.cs
            ├── Commands/
            │   ├── ConfigCommand.cs          # Create/update config
            │   ├── CacheCommand.cs            # Populate preparation cache
            │   ├── PrepareCommand.cs          # Execute preparation
            │   └── RestoreCommand.cs          # Restore client state
            ├── Models/
            │   ├── PreparationConfig.cs       # Config JSON model
            │   ├── AssetManipulation.cs
            │   ├── CodePatch.cs
            │   └── ScriptingDefineSymbol.cs
            ├── Services/
            │   ├── ConfigService.cs           # Config CRUD operations
            │   ├── CacheService.cs            # Manage preparation cache
            │   ├── PreparationService.cs      # Execute preparations
            │   ├── CodePatchService.cs        # Apply code patches
            │   └── ManifestService.cs         # Modify manifest.json
            └── Utilities/
                ├── FileSystemHelper.cs
                └── PathResolver.cs
```

## Tool Commands

### 1. `config` - Manage Configuration

#### `config create`

Create a new preparation config from scratch or template.

```bash
# Create new config interactively
dotnet run -- config create --output "../../configs/build-preparation.json"

# Create from template
dotnet run -- config create --template base --output "../../configs/build-preparation.json"

# Create with initial values
dotnet run -- config create \
  --name "Prepare for build - Sango Card" \
  --platform any \
  --build-target all \
  --output "../../configs/build-preparation.json"
```

#### `config add-package`

Add Unity package to preparation config.

```bash
# Add Microsoft.Extensions package
dotnet run -- config add-package \
  --config "../../configs/build-preparation.json" \
  --source "../preparation/unity-packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656" \
  --destination "Packages/org.nuget.microsoft.extensions.logging@c95ee2e3b656" \
  --category "Add Microsoft.Extensions packages"

# Add custom package
dotnet run -- config add-package \
  --config "../../configs/build-preparation.json" \
  --source "../../packages/scoped-6571/com.contractwork.sangocard.build" \
  --destination "Packages/com.contractwork.sangocard.build" \
  --category "Add custom packages"
```

#### `config add-assembly`

Add plain DLL to preparation config.

```bash
# Add System.CommandLine.dll
dotnet run -- config add-assembly \
  --config "../../configs/build-preparation.json" \
  --source "../preparation/assemblies/System.CommandLine.dll" \
  --destination "Assets/Plugins/assemblies/System.CommandLine.dll" \
  --category "Add plain DLL assemblies"
```

#### `config add-define`

Add scripting define symbol.

```bash
# Add LOCAL_BUILD define
dotnet run -- config add-define \
  --config "../../configs/build-preparation.json" \
  --name "LOCAL_BUILD" \
  --operation add \
  --enable

# Remove ODIN_INSPECTOR define
dotnet run -- config add-define \
  --config "../../configs/build-preparation.json" \
  --name "ODIN_INSPECTOR" \
  --operation remove \
  --enable
```

#### `config add-patch`

Add code patch to preparation config.

```bash
# Add using statement removal
dotnet run -- config add-patch \
  --config "../../configs/build-preparation.json" \
  --file "Assets/Scripts/Battle/Formation/Controller/CreateFormationEntityController.cs" \
  --type remove_using \
  --pattern "using Unity.VisualScripting;"

# Add code replacement
dotnet run -- config add-patch \
  --config "../../configs/build-preparation.json" \
  --file "Assets/Scripts/Battle/Formation/Controller/CreateFormationEntityController.cs" \
  --type replace \
  --pattern "var toucch = toucchOb.AddComponent<TouchColliderController>();" \
  --replacement "var toucch = toucchOb.gameObject.AddComponent<TouchColliderController>();"
```

#### `config remove`

Remove items from config.

```bash
# Remove package by path
dotnet run -- config remove package \
  --config "../../configs/build-preparation.json" \
  --path "Packages/com.unity.visualscripting"

# Remove define by name
dotnet run -- config remove define \
  --config "../../configs/build-preparation.json" \
  --name "ODIN_INSPECTOR"

# Remove patch by file
dotnet run -- config remove patch \
  --config "../../configs/build-preparation.json" \
  --file "Assets/Scripts/Startup.cs"
```

#### `config list`

List items in config.

```bash
# List all packages
dotnet run -- config list packages --config "../../configs/build-preparation.json"

# List all defines
dotnet run -- config list defines --config "../../configs/build-preparation.json"

# List all patches
dotnet run -- config list patches --config "../../configs/build-preparation.json"
```

### 2. `cache` - Manage Preparation Cache

#### `cache populate`

Populate preparation cache from code-quality project.

```bash
# Populate all
dotnet run -- cache populate \
  --source "../../projects/code-quality" \
  --destination "../../build/preparation"

# Populate only packages
dotnet run -- cache populate \
  --source "../../projects/code-quality" \
  --destination "../../build/preparation" \
  --packages-only

# Populate specific packages
dotnet run -- cache populate \
  --source "../../projects/code-quality" \
  --destination "../../build/preparation" \
  --package "org.nuget.microsoft.extensions.logging@*" \
  --package "org.nuget.microsoft.extensions.dependencyinjection@*"
```

#### `cache add`

Manually add items to cache.

```bash
# Add Unity package
dotnet run -- cache add package \
  --source "../../projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.logging@c95ee2e3b656" \
  --destination "../../build/preparation/unity-packages"

# Add DLL
dotnet run -- cache add assembly \
  --source "../../projects/code-quality/Assets/Packages/Splat.15.3.1/lib/netstandard2.0/Splat.dll" \
  --destination "../../build/preparation/assemblies"
```

#### `cache list`

List items in cache.

```bash
# List all
dotnet run -- cache list --cache "../../build/preparation"

# List only packages
dotnet run -- cache list packages --cache "../../build/preparation"

# List only assemblies
dotnet run -- cache list assemblies --cache "../../build/preparation"
```

#### `cache clean`

Clean cache.

```bash
# Clean all
dotnet run -- cache clean --cache "../../build/preparation"

# Clean only packages
dotnet run -- cache clean packages --cache "../../build/preparation"
```

### 3. `prepare` - Execute Preparation

#### `prepare run`

Execute preparation on client project.

```bash
# Run full preparation
dotnet run -- prepare run \
  --config "../../configs/build-preparation.json" \
  --client "../../projects/client" \
  --build-target StandaloneWindows64

# Dry run (show what would be done)
dotnet run -- prepare run \
  --config "../../configs/build-preparation.json" \
  --client "../../projects/client" \
  --build-target StandaloneWindows64 \
  --dry-run

# Run specific phase
dotnet run -- prepare run \
  --config "../../configs/build-preparation.json" \
  --client "../../projects/client" \
  --phase preUnity
```

#### `prepare validate`

Validate preparation config.

```bash
# Validate config
dotnet run -- prepare validate \
  --config "../../configs/build-preparation.json"

# Validate and check file existence
dotnet run -- prepare validate \
  --config "../../configs/build-preparation.json" \
  --check-files
```

### 4. `restore` - Restore Client State

#### `restore run`

Restore client project to original state.

```bash
# Restore using git
dotnet run -- restore run \
  --client "../../projects/client" \
  --method git

# Restore from backup
dotnet run -- restore run \
  --client "../../projects/client" \
  --method backup \
  --backup-path "../../build/backup"
```

## Configuration Model

### JSON Structure

```json
{
  "version": "3.0",
  "metadata": {
    "author": "string",
    "lastModified": "ISO8601 datetime",
    "description": "string",
    "tags": ["string"]
  },
  "preparations": [
    {
      "name": "string",
      "description": "string",
      "platform": "any|windows|macos|linux",
      "buildTarget": "all|StandaloneWindows64|Android|iOS|...",
      "mode": "legacy|multi-phase",
      "scriptingDefineSymbols": [
        {
          "name": "string",
          "status": {
            "presence": "present|absent",
            "operation": "add|remove"
          },
          "useRegex": false,
          "enable": true
        }
      ],
      "codePatches": [
        {
          "targetFile": {
            "RootPath": "string",
            "Path": "string"
          },
          "patches": [
            {
              "Type": "remove_using|replace|replace_block|remove_block",
              "Pattern": "string",
              "Replacement": "string (optional)",
              "PatternStart": "string (for blocks)",
              "PatternEnd": "string (for blocks)"
            }
          ],
          "enable": true
        }
      ],
      "manifestModification": [
        {
          "name": "string (package name)",
          "status": {
            "presence": "present|absent",
            "operation": "add|remove"
          },
          "version": "string (optional)"
        }
      ],
      "assetManipulations": [
        {
          "category": "string",
          "assets": [
            {
              "source": {
                "rootPath": "string",
                "path": "string",
                "useRegex": false
              },
              "destination": {
                "rootPath": "string",
                "path": "string",
                "useRegex": false
              },
              "status": {
                "presence": "present|absent",
                "operation": "copy|symlink|remove"
              },
              "enable": true,
              "operateOnMetaFile": true,
              "symlinkChildren": false
            }
          ]
        }
      ]
    }
  ]
}
```

## Implementation Details

### Technology Stack

- **.NET 8.0** - Latest LTS
- **System.CommandLine** - CLI framework (already used in build package)
- **System.Text.Json** - JSON serialization
- **Spectre.Console** - Rich terminal UI
- **Nuke.Common** - Path utilities (optional, for consistency)

### Key Services

#### ConfigService

```csharp
public class ConfigService
{
    public PreparationConfig Load(string path);
    public void Save(PreparationConfig config, string path);
    public void AddPackage(PreparationConfig config, PackageInfo package);
    public void AddAssembly(PreparationConfig config, AssemblyInfo assembly);
    public void AddDefine(PreparationConfig config, ScriptingDefineSymbol define);
    public void AddPatch(PreparationConfig config, CodePatch patch);
    public void Remove(PreparationConfig config, string type, string identifier);
}
```

#### CacheService

```csharp
public class CacheService
{
    public void PopulateFromCodeQuality(string codeQualityPath, string cachePath);
    public void AddPackage(string sourcePath, string cachePath);
    public void AddAssembly(string sourcePath, string cachePath);
    public List<CacheItem> ListCache(string cachePath);
    public void Clean(string cachePath, string type = null);
}
```

#### PreparationService

```csharp
public class PreparationService
{
    public void Execute(PreparationConfig config, string clientPath, string buildTarget, bool dryRun = false);
    public void CopyAssets(List<AssetManipulation> manipulations, string clientPath, bool dryRun);
    public void ModifyManifest(List<ManifestModification> modifications, string clientPath, bool dryRun);
    public void ApplyDefines(List<ScriptingDefineSymbol> defines, string clientPath, bool dryRun);
    public ValidationResult Validate(PreparationConfig config, bool checkFiles = false);
}
```

#### CodePatchService

```csharp
public class CodePatchService
{
    public void ApplyPatches(List<CodePatch> patches, string clientPath, bool dryRun = false);
    public void RemoveUsing(string filePath, string pattern);
    public void Replace(string filePath, string pattern, string replacement);
    public void ReplaceBlock(string filePath, string startPattern, string endPattern, string replacement);
    public void RemoveBlock(string filePath, string startPattern, string endPattern);
}
```

#### ManifestService

```csharp
public class ManifestService
{
    public void ModifyManifest(string manifestPath, List<ManifestModification> modifications);
    public void AddPackage(string manifestPath, string packageName, string version = null);
    public void RemovePackage(string manifestPath, string packageName);
    public ManifestInfo ReadManifest(string manifestPath);
}
```

### Path Resolution

All paths in config are relative to the config file location. The tool resolves them:

```csharp
public class PathResolver
{
    private readonly string _configDirectory;

    public PathResolver(string configPath)
    {
        _configDirectory = Path.GetDirectoryName(configPath);
    }

    public string Resolve(string rootPath, string relativePath)
    {
        // Resolve rootPath relative to config directory
        var root = Path.GetFullPath(Path.Combine(_configDirectory, rootPath));
        // Combine with relative path
        return Path.GetFullPath(Path.Combine(root, relativePath));
    }
}
```

## Usage Workflow

### Initial Setup

1. **Create preparation cache:**

   ```bash
   dotnet run -- cache populate \
     --source "../../projects/code-quality" \
     --destination "../../build/preparation"
   ```

2. **Create base config:**

   ```bash
   dotnet run -- config create \
     --template base \
     --output "../../configs/build-preparation.json"
   ```

3. **Add Microsoft.Extensions packages:**

   ```bash
   for pkg in logging logging.abstractions dependencyinjection dependencyinjection.abstractions; do
     dotnet run -- config add-package \
       --config "../../configs/build-preparation.json" \
       --source "../preparation/unity-packages/org.nuget.microsoft.extensions.$pkg@*" \
       --destination "Packages/org.nuget.microsoft.extensions.$pkg@*" \
       --category "Add Microsoft.Extensions packages"
   done
   ```

4. **Add plain DLLs:**

   ```bash
   dotnet run -- config add-assembly \
     --config "../../configs/build-preparation.json" \
     --source "../preparation/assemblies/System.CommandLine.dll" \
     --destination "Assets/Plugins/assemblies/System.CommandLine.dll"
   ```

5. **Add custom packages:**

   ```bash
   dotnet run -- config add-package \
     --config "../../configs/build-preparation.json" \
     --source "../../packages/scoped-6571/com.contractwork.sangocard.build" \
     --destination "Packages/com.contractwork.sangocard.build"
   ```

### Build Time

1. **Validate config:**

   ```bash
   dotnet run -- prepare validate \
     --config "../../configs/build-preparation.json" \
     --check-files
   ```

2. **Execute preparation:**

   ```bash
   dotnet run -- prepare run \
     --config "../../configs/build-preparation.json" \
     --client "../../projects/client" \
     --build-target StandaloneWindows64
   ```

3. **Build with Unity** (via Nuke)

4. **Restore client:**

   ```bash
   dotnet run -- restore run \
     --client "../../projects/client" \
     --method git
   ```

## Integration with Nuke

The Nuke build system will call this tool:

```csharp
// In Build.Preparation.cs
Target PrepareClient => _ => _
    .Description("Prepare client project for build")
    .Executes(() =>
    {
        var toolPath = RootDirectory / "packages" / "scoped-6571" /
                       "com.contractwork.sangocard.build" / "dotnet~" / "tool";

        DotNetRun(s => s
            .SetProjectFile(toolPath / "SangoCard.Build.Tool" / "SangoCard.Build.Tool.csproj")
            .SetApplicationArguments($"prepare run " +
                $"--config \"{BuildPreparationConfig}\" " +
                $"--client \"{UnityProjectPath}\" " +
                $"--build-target {UnityBuildTarget}"));
    });
```

## Benefits

✅ **Maintainable** - Centralized config management
✅ **Flexible** - Easy to add/remove/modify preparations
✅ **Testable** - Can dry-run and validate
✅ **Reusable** - Tool can be used standalone or via Nuke
✅ **Type-safe** - Strong typing with C# models
✅ **Interactive** - Rich CLI with Spectre.Console
✅ **Documented** - Self-documenting via CLI help

## Next Steps

1. Create .NET project structure
2. Implement core models (PreparationConfig, etc.)
3. Implement ConfigService
4. Implement CacheService
5. Implement PreparationService
6. Implement CodePatchService
7. Implement CLI commands
8. Test with existing config
9. Integrate with Nuke

Would you like me to start implementing this tool?
