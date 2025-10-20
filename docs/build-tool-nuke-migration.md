# PowerShell Script → Nuke Target Migration

## Summary

Successfully migrated the build tool compilation from a standalone PowerShell script to an integrated NUKE build target.

## Changes Made

### ✅ Added NUKE Build Target

**File**: `build/nuke/build/Build.Preparation.cs`

**New Target**: `BuildPreparationTool`

```csharp
Target BuildPreparationTool => _ => _
    .Description("Build SangoCard.Build.Tool as single-file executable")
    .Executes(() =>
    {
        var runtime = Environment.GetEnvironmentVariable("PREP_TOOL_RUNTIME") ?? "win-x64";
        var configuration = Environment.GetEnvironmentVariable("PREP_TOOL_CONFIG") ?? "Release";

        DotNetPublish(s => s
            .SetProject(PreparationToolProject)
            .SetConfiguration(configuration)
            .SetRuntime(runtime)
            .EnableSelfContained()
            .EnablePublishSingleFile()
            .SetProperty("IncludeNativeLibrariesForSelfExtract", "true")
            .SetProperty("PublishTrimmed", "false")
        );

        // Verification and logging...
    });
```

### ✅ Updated Taskfile

**File**: `Taskfile.yml`

**Changed from**:
```yaml
setup:build-tool:
  cmds:
    - pwsh -NoProfile -ExecutionPolicy Bypass -File ./scripts/build-tool.ps1 -Configuration {{.CONFIG}} -RuntimeIdentifier {{.RUNTIME}}
```

**Changed to**:
```yaml
setup:build-tool:
  env:
    PREP_TOOL_CONFIG: '{{.CONFIG}}'
    PREP_TOOL_RUNTIME: '{{.RUNTIME}}'
  cmds:
    - '{{.NUKE_BUILD_SCRIPT}} BuildPreparationTool'
```

### ✅ Removed Standalone Script

**Deleted**: `scripts/build-tool.ps1` (104 lines)

**Reason**: Functionality now integrated into NUKE build system

### ✅ Updated Documentation

Updated all references in:
- `build/configs/preparation/OPTIMIZATION-NOTES.md`
- `docs/build-tool-optimization.md`
- `README-OPTIMIZATIONS.md`

## Benefits

### 1. **Consistency**
- Uses the same build system as the rest of the project
- No separate PowerShell script to maintain
- Follows established patterns in Build.Preparation.cs

### 2. **Integration**
- Can depend on other NUKE targets if needed
- Uses NUKE's built-in logging (Serilog)
- Inherits project structure and conventions

### 3. **Simplicity**
- One less file to maintain (104 lines removed)
- Environment variable pattern matches existing code
- Direct integration with DotNetPublish API

### 4. **Visibility**
- Shows up in `nuke --help` and IDE tooling
- Part of the documented build pipeline
- Better integration with CI/CD

## Usage

### Via Task (Recommended)

```bash
# Default: Release build for Windows x64
task setup:build-tool

# Specify configuration
task setup:build-tool CONFIG=Debug

# Specify runtime
task setup:build-tool RUNTIME=linux-x64

# Both
task setup:build-tool CONFIG=Debug RUNTIME=osx-arm64
```

### Direct NUKE Call

```bash
# Windows
./build/nuke/build.ps1 BuildPreparationTool

# With environment variables
$env:PREP_TOOL_RUNTIME="linux-x64"
$env:PREP_TOOL_CONFIG="Release"
./build/nuke/build.ps1 BuildPreparationTool

# Linux/macOS
export PREP_TOOL_RUNTIME=linux-x64
export PREP_TOOL_CONFIG=Release
./build/nuke/build.sh BuildPreparationTool
```

## Technical Details

### Environment Variables

The target reads configuration from environment variables:

- **`PREP_TOOL_RUNTIME`** (default: `win-x64`)
  - Valid values: `win-x64`, `linux-x64`, `osx-x64`, `osx-arm64`
  - Determines target platform for the executable

- **`PREP_TOOL_CONFIG`** (default: `Release`)
  - Valid values: `Debug`, `Release`
  - Determines build optimization level

### Build Flow

```
1. Task sets environment variables
   ↓
2. Calls: ./build/nuke/build.ps1 BuildPreparationTool
   ↓
3. NUKE target reads environment variables
   ↓
4. Calls DotNetPublish with appropriate settings
   ↓
5. .NET SDK publishes single-file exe
   ↓
6. MSBuild post-publish target copies to Tools/
   ↓
7. NUKE verifies and logs result
```

### Output

```
═══════════════════════════
║ BuildPreparationTool
╬════════════════

[INF] === Building Preparation Tool ===
[INF] Project: .../SangoCard.Build.Tool.csproj
[INF] Output: .../Tools
[INF] Configuration: Release
[INF] Runtime: win-x64
[INF] > dotnet publish ...
[DBG]   Copying published tool to Unity package Tools folder...
[DBG]   ✅ Tool copied successfully to .../Tools/SangoCard.Build.Tool.exe
[INF] ✅ Tool built successfully!
[INF]    Location: .../Tools/SangoCard.Build.Tool.exe
[INF]    Size: 103.24 MB
[INF]    Modified: 10/20/2025 13:12:51

Target                  Status      Duration
────────────────────────────────────────────
BuildPreparationTool    Succeeded       0:01
```

## Comparison: Before vs After

| Aspect | PowerShell Script | NUKE Target |
|--------|------------------|-------------|
| **Location** | `scripts/build-tool.ps1` | `build/nuke/build/Build.Preparation.cs` |
| **Lines of Code** | 104 lines (dedicated file) | 55 lines (integrated) |
| **Dependencies** | PowerShell, dotnet CLI | NUKE (already required) |
| **Configuration** | Command-line parameters | Environment variables |
| **Logging** | Write-Host | Serilog (structured) |
| **Integration** | Standalone | Part of build system |
| **Discoverability** | Task list only | `nuke --help`, IDE, docs |
| **Error Handling** | try/catch + exit codes | NUKE exception handling |
| **Validation** | Manual file existence check | NUKE + MSBuild integration |

## Migration Notes

### What Changed for Users

**Before**:
```bash
# Task called PowerShell script with parameters
task setup:build-tool CONFIG=Release RUNTIME=win-x64
```

**After**:
```bash
# Task calls NUKE target with environment variables
task setup:build-tool CONFIG=Release RUNTIME=win-x64
# (Same command, different implementation)
```

**Result**: No change in user experience! The task command syntax remains identical.

### What Changed for Developers

1. **Build logic location**: Now in `Build.Preparation.cs` alongside other build targets
2. **Parameter passing**: Uses environment variables instead of CLI args
3. **Logging**: Uses Serilog instead of Write-Host
4. **Integration**: Can reference other NUKE targets/properties if needed

### Why Environment Variables?

NUKE targets don't easily support dynamic parameters like `--runtime` or `--config`, but they can read environment variables. The Taskfile sets these variables before calling NUKE, providing the same flexibility with better integration.

## Future Enhancements

Now that the build tool is integrated with NUKE, we could:

1. **Add dependencies**: Make other targets depend on `BuildPreparationTool`
2. **Cache awareness**: Rebuild tool only if source changed
3. **Multi-runtime builds**: Build for all platforms in one command
4. **Integration tests**: Add a target to test the built tool
5. **Version stamping**: Use GitVersion to version the tool exe

## Conclusion

This migration consolidates the build tool compilation into the existing NUKE build system, reducing maintenance burden while improving integration and consistency. The user experience remains unchanged, but the implementation is now more maintainable and better integrated with the project's build infrastructure.
