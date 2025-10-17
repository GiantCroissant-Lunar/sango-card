# Build Preparation Tool Design v2

## Overview

A .NET CLI/TUI tool for managing Unity build preparation configurations with robust path resolution, advanced code patching, and interactive management.

## Key Design Decisions

### 1. Config Storage Location

**Decision:** `build/preparation/configs/`

- Co-located with cache for logical grouping
- Clear separation from Nuke build scripts
- Easy to find and manage

### 2. Path Resolution Strategy

**Decision:** All paths are **absolute** or resolved from **git repository root**

**Problem with relative paths:**

- Different agents interpret base paths differently
- Config file location, build folder, or git root?
- Causes confusion and errors

**Solution:**

```json
{
  "pathResolution": {
    "strategy": "git-root",
    "gitRoot": "auto-detect",  // or explicit path
    "markers": [".git", ".gitignore"]
  },
  "paths": {
    "// All paths relative to git root": "",
    "source": "build/preparation/unity-packages/...",
    "destination": "projects/client/Packages/..."
  }
}
```

**Path Resolution Algorithm:**

1. Detect git root (walk up from config file until `.git` found)
2. Store git root as base path
3. All paths in config are relative to git root
4. Tool validates git root on load
5. If git root not found, fail with clear error

**Benefits:**

- ✅ Unambiguous base path (git root)
- ✅ All agents agree on same base
- ✅ Portable within repository
- ✅ Clear error messages if git root not found

### 3. Code Patch Implementation

**Decision:** Multi-strategy approach based on file type

#### C# Files (.cs)

**Use Roslyn** for robust syntax-aware patching:

```csharp
public class CSharpPatcher
{
    public void RemoveUsing(string filePath, string usingNamespace);
    public void ReplaceExpression(string filePath, string oldExpr, string newExpr);
    public void RemoveMethod(string filePath, string methodName);
    public void AddAttribute(string filePath, string className, string attribute);
}
```

**Benefits:**

- Syntax-aware (won't break code)
- Handles formatting automatically
- Can validate changes before applying

#### JSON Files (.json)

**Use System.Text.Json** for structured patching:

```csharp
public class JsonPatcher
{
    public void SetValue(string filePath, string jsonPath, object value);
    public void RemoveProperty(string filePath, string jsonPath);
    public void AddToArray(string filePath, string jsonPath, object item);
}
```

**JSON Path examples:**

- `$.dependencies["com.unity.visualscripting"]` - Remove package
- `$.scopedRegistries[0].scopes` - Add scope

#### Unity Asset Files (.asset, .prefab, .unity)

**Use Unity YAML parser** with special handling:

Unity uses a **custom YAML format** with:

- `%YAML 1.1` header
- `%TAG !u! tag:unity3d.com,2011:` directive
- Object references like `{fileID: 11500000, guid: xxx}`
- Anchors and aliases

**Strategy:**

```csharp
public class UnityAssetPatcher
{
    // Parse Unity YAML (custom parser)
    public UnityAsset Parse(string filePath);

    // Modify specific properties
    public void SetProperty(UnityAsset asset, string objectPath, string property, object value);

    // Add/remove scripting defines
    public void ModifyScriptingDefines(string projectSettingsPath, List<string> add, List<string> remove);

    // Serialize back to Unity YAML format
    public void Save(UnityAsset asset, string filePath);
}
```

**For now, focus on:**

- `ProjectSettings.asset` - Scripting defines
- Simple property modifications
- **Avoid** complex scene/prefab patching initially

#### Plain Text Files

**Use regex/string replacement** for simple cases:

```csharp
public class TextPatcher
{
    public void Replace(string filePath, string pattern, string replacement, bool useRegex);
    public void RemoveLine(string filePath, string pattern);
    public void InsertAfter(string filePath, string marker, string content);
}
```

### 4. Scripting Define Symbols

**Decision:** Modify `ProjectSettings/ProjectSettings.asset` only

**Implementation:**

```csharp
public class ScriptingDefineService
{
    public void AddDefines(string projectSettingsPath, List<string> defines, BuildTargetGroup targetGroup);
    public void RemoveDefines(string projectSettingsPath, List<string> defines, BuildTargetGroup targetGroup);
    public List<string> GetDefines(string projectSettingsPath, BuildTargetGroup targetGroup);
}
```

**Unity ProjectSettings.asset structure:**

```yaml
PlayerSettings:
  scriptingDefineSymbols:
    1: UNITY_POST_PROCESSING_STACK_V2;LOCAL_BUILD  # Standalone
    4: UNITY_POST_PROCESSING_STACK_V2;USE_APK      # Android
    9: UNITY_POST_PROCESSING_STACK_V2;iOS_BUILD    # iOS
```

### 5. Cache Management

**Decision:** Tool manages cache and updates config bidirectionally

**Workflow:**

1. **Add to cache** → Automatically updates config
2. **Remove from cache** → Automatically updates config
3. **Config changes** → Validates cache consistency

```bash
# Add package to cache (also updates config)
tool cache add package \
  --source "projects/code-quality/Library/PackageCache/org.nuget.microsoft.extensions.logging@xxx" \
  --category "Microsoft.Extensions packages"

# Tool does:
# 1. Copy package to build/preparation/unity-packages/
# 2. Add entry to config JSON
# 3. Save config
```

### 6. Validation Levels

**Decision:** Implement all validation levels

```csharp
public class ValidationService
{
    // Level 1: JSON structure
    public ValidationResult ValidateSchema(PreparationConfig config);

    // Level 2: File/folder existence
    public ValidationResult ValidateFileExistence(PreparationConfig config);

    // Level 3: Unity package validity
    public ValidationResult ValidateUnityPackages(PreparationConfig config);

    // Level 4: Code patch applicability
    public ValidationResult ValidateCodePatches(PreparationConfig config);

    // Run all levels
    public ValidationResult ValidateAll(PreparationConfig config);
}
```

**Validation output:**

```
✓ Config schema valid
✓ All source files exist (24/24)
✓ All Unity packages valid (6/6)
✗ Code patch failed: File not found
  - projects/client/Assets/Scripts/Startup.cs
✗ Code patch failed: Pattern not found
  - CreateFormationEntityController.cs: "using Unity.VisualScripting;"

Summary: 2 errors, 0 warnings
```

### 7. Integration with Nuke

**Decision:** Call tool as external process

```csharp
// In Build.Preparation.cs
Target PrepareClient => _ => _
    .Executes(() =>
    {
        var toolExe = RootDirectory / "packages" / "scoped-6571" /
                      "com.contractwork.sangocard.build" / "dotnet~" / "tool" /
                      "publish" / "SangoCard.Build.Tool.exe";

        ProcessTasks.StartProcess(
            toolExe,
            $"prepare run " +
            $"--config \"{BuildPreparationConfig}\" " +
            $"--client \"{UnityProjectPath}\" " +
            $"--build-target {UnityBuildTarget} " +
            $"--mode cli",
            workingDirectory: RootDirectory
        ).AssertZeroExitCode();
    });
```

## Tool Modes

### CLI Mode (Command Line Interface)

**For automation and Nuke integration**

```bash
# Non-interactive, scriptable
tool prepare run --config "..." --client "..." --mode cli

# Exit codes:
# 0 = Success
# 1 = Validation error
# 2 = Execution error
# 3 = Configuration error
```

**Features:**

- No user input required
- Structured output (JSON optional)
- Clear exit codes
- Progress indicators
- Error messages to stderr

### TUI Mode (Terminal User Interface)

**For interactive configuration management**

```bash
# Interactive mode
tool config edit --config "..." --mode tui

# Or just:
tool tui
```

**Features using Spectre.Console:**

- Interactive menus
- Visual config editor
- Real-time validation
- File browser for path selection
- Syntax highlighting
- Undo/redo support

**TUI Screens:**

1. **Main Menu:**

   ```
   ╔═══════════════════════════════════════════╗
   ║  Sango Card Build Preparation Tool       ║
   ╠═══════════════════════════════════════════╣
   ║  1. Manage Cache                         ║
   ║  2. Edit Configuration                   ║
   ║  3. Run Preparation (Dry Run)            ║
   ║  4. Validate Configuration               ║
   ║  5. View Logs                            ║
   ║  6. Exit                                 ║
   ╚═══════════════════════════════════════════╝
   ```

2. **Cache Management:**

   ```
   Cache Contents (build/preparation/)

   Unity Packages (4):
   ✓ org.nuget.microsoft.extensions.logging@c95ee2e3b656
   ✓ org.nuget.microsoft.extensions.logging.abstractions@daf01cbf05d5
   ✓ org.nuget.microsoft.extensions.dependencyinjection@50cfd603eb43
   ✓ org.nuget.microsoft.extensions.dependencyinjection.abstractions@011670b187e9

   Assemblies (2):
   ✓ System.CommandLine.dll (155 KB)
   ✓ Splat.dll (141 KB)

   [A]dd  [R]emove  [P]opulate from code-quality  [B]ack
   ```

3. **Config Editor:**

   ```
   Preparation: "Prepare for build - Sango Card"

   Scripting Defines (3):
   ✓ LOCAL_BUILD (add)
   ✗ ODIN_INSPECTOR (remove)
   ✗ UNITY_INCLUDE_TESTS (remove)

   Asset Manipulations (12 categories):
   ▸ Add Microsoft.Extensions packages (4 items)
   ▸ Add custom packages (2 items)
   ▸ Remove assemblies (7 items)
   ▼ Code Patches (3 files)
     ✓ CreateFormationEntityController.cs (2 patches)
     ✓ RankMatchModel.cs (1 patch)
     ✓ TooltipExtensions.cs (2 patches)

   [E]dit  [A]dd  [D]elete  [V]alidate  [S]ave  [Q]uit
   ```

## Project Structure

```
packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/
├── SangoCard.Build.Tool.sln
└── SangoCard.Build.Tool/
    ├── SangoCard.Build.Tool.csproj
    ├── Program.cs                      # Entry point, mode selection
    │
    ├── Cli/                            # CLI mode
    │   ├── Commands/
    │   │   ├── ConfigCommands.cs       # config create/add/remove/list
    │   │   ├── CacheCommands.cs        # cache populate/add/list/clean
    │   │   ├── PrepareCommands.cs      # prepare run/validate
    │   │   └── RestoreCommands.cs      # restore run
    │   └── CliHost.cs                  # CLI command routing
    │
    ├── Tui/                            # TUI mode
    │   ├── Screens/
    │   │   ├── MainMenuScreen.cs
    │   │   ├── CacheManagementScreen.cs
    │   │   ├── ConfigEditorScreen.cs
    │   │   └── ValidationScreen.cs
    │   ├── Components/
    │   │   ├── FileSelector.cs
    │   │   ├── ListEditor.cs
    │   │   └── PropertyEditor.cs
    │   └── TuiHost.cs                  # TUI screen routing
    │
    ├── Core/                           # Shared core logic
    │   ├── Models/
    │   │   ├── PreparationConfig.cs
    │   │   ├── AssetManipulation.cs
    │   │   ├── CodePatch.cs
    │   │   ├── ScriptingDefineSymbol.cs
    │   │   └── PathResolution.cs       # Git root resolution
    │   │
    │   ├── Services/
    │   │   ├── ConfigService.cs
    │   │   ├── CacheService.cs
    │   │   ├── PreparationService.cs
    │   │   ├── ValidationService.cs
    │   │   ├── PathResolver.cs         # Git root-based paths
    │   │   └── ManifestService.cs
    │   │
    │   ├── Patchers/                   # Code patching strategies
    │   │   ├── IPatcher.cs
    │   │   ├── CSharpPatcher.cs        # Roslyn-based
    │   │   ├── JsonPatcher.cs          # System.Text.Json
    │   │   ├── UnityAssetPatcher.cs    # Unity YAML
    │   │   └── TextPatcher.cs          # Regex/string
    │   │
    │   └── Utilities/
    │       ├── GitHelper.cs            # Git root detection
    │       ├── FileSystemHelper.cs
    │       └── UnityYamlParser.cs      # Custom Unity YAML parser
    │
    └── publish/                        # Published executable
        └── SangoCard.Build.Tool.exe    # Self-contained or framework-dependent
```

## Path Resolution Implementation

### Config File Structure

```json
{
  "version": "3.0",
  "pathResolution": {
    "strategy": "git-root",
    "gitRoot": null,  // Auto-detected on load
    "validated": false
  },
  "metadata": {
    "gitRoot": "D:\\lunar-snake\\constract-work\\card-projects\\sango-card",
    "configPath": "build/preparation/configs/build-preparation.json",
    "lastModified": "2025-10-17T09:20:00Z"
  },
  "preparations": [
    {
      "assetManipulations": [
        {
          "assets": [
            {
              "source": {
                "path": "build/preparation/unity-packages/org.nuget.microsoft.extensions.logging@xxx"
              },
              "destination": {
                "path": "projects/client/Packages/org.nuget.microsoft.extensions.logging@xxx"
              }
            }
          ]
        }
      ]
    }
  ]
}
```

### PathResolver Service

```csharp
public class PathResolver
{
    private readonly string _gitRoot;

    public PathResolver(string configPath)
    {
        _gitRoot = DetectGitRoot(configPath);
        if (_gitRoot == null)
            throw new InvalidOperationException(
                $"Git root not found. Config must be inside a git repository. " +
                $"Config path: {configPath}");
    }

    private string DetectGitRoot(string startPath)
    {
        var current = Path.GetDirectoryName(Path.GetFullPath(startPath));

        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
                return current;

            current = Directory.GetParent(current)?.FullName;
        }

        return null;
    }

    public string Resolve(string relativePath)
    {
        // All paths relative to git root
        return Path.GetFullPath(Path.Combine(_gitRoot, relativePath));
    }

    public string MakeRelative(string absolutePath)
    {
        // Convert absolute path to relative from git root
        return Path.GetRelativePath(_gitRoot, absolutePath);
    }

    public string GitRoot => _gitRoot;
}
```

### Usage in Config

```csharp
var resolver = new PathResolver(configPath);

// Load config
var config = ConfigService.Load(configPath);

// Resolve paths
foreach (var asset in config.GetAllAssets())
{
    var sourcePath = resolver.Resolve(asset.Source.Path);
    var destPath = resolver.Resolve(asset.Destination.Path);

    // Now sourcePath and destPath are absolute and unambiguous
    File.Copy(sourcePath, destPath);
}
```

## Code Patcher Implementation

### Patcher Interface

```csharp
public interface IPatcher
{
    bool CanHandle(string filePath);
    void ApplyPatch(string filePath, CodePatch patch);
    ValidationResult ValidatePatch(string filePath, CodePatch patch);
}
```

### C# Patcher (Roslyn)

```csharp
public class CSharpPatcher : IPatcher
{
    public bool CanHandle(string filePath) =>
        Path.GetExtension(filePath).Equals(".cs", StringComparison.OrdinalIgnoreCase);

    public void ApplyPatch(string filePath, CodePatch patch)
    {
        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        switch (patch.Type)
        {
            case "remove_using":
                root = RemoveUsing(root, patch.Pattern);
                break;
            case "replace":
                root = ReplaceExpression(root, patch.Pattern, patch.Replacement);
                break;
            // ... other operations
        }

        File.WriteAllText(filePath, root.ToFullString());
    }

    private SyntaxNode RemoveUsing(SyntaxNode root, string usingNamespace)
    {
        var usings = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name.ToString() == usingNamespace);

        return root.RemoveNodes(usings, SyntaxRemoveOptions.KeepNoTrivia);
    }
}
```

### Unity Asset Patcher

```csharp
public class UnityAssetPatcher : IPatcher
{
    public bool CanHandle(string filePath) =>
        Path.GetExtension(filePath) == ".asset";

    public void ModifyScriptingDefines(
        string projectSettingsPath,
        BuildTargetGroup targetGroup,
        List<string> add,
        List<string> remove)
    {
        var asset = UnityYamlParser.Parse(projectSettingsPath);

        // Find PlayerSettings object
        var playerSettings = asset.FindObject("PlayerSettings");

        // Get current defines for target group
        var defines = playerSettings
            .GetProperty("scriptingDefineSymbols")
            .GetValue((int)targetGroup)
            .Split(';')
            .ToList();

        // Apply changes
        defines.AddRange(add);
        defines.RemoveAll(d => remove.Contains(d));

        // Update
        playerSettings
            .GetProperty("scriptingDefineSymbols")
            .SetValue((int)targetGroup, string.Join(";", defines.Distinct()));

        // Save
        UnityYamlParser.Save(asset, projectSettingsPath);
    }
}
```

## Technology Stack

- **.NET 8.0** - Latest LTS
- **System.CommandLine** - CLI framework
- **Spectre.Console** - TUI framework
- **System.Text.Json** - JSON handling
- **Microsoft.CodeAnalysis.CSharp** (Roslyn) - C# patching
- **YamlDotNet** - Base for Unity YAML (with custom extensions)

## Next Steps

1. Create project structure
2. Implement PathResolver with git root detection
3. Implement core models
4. Implement ConfigService with path resolution
5. Implement CacheService
6. Implement patcher interfaces and C# patcher
7. Implement CLI commands
8. Implement TUI screens
9. Test with existing config
10. Integrate with Nuke

Should I start implementing this?
