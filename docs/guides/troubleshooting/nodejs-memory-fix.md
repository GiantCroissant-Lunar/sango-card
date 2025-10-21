# Node.js Memory Crash Fix

## Problem

The Copilot CLI/Windsurf IDE was crashing during Unity builds with:

```
FATAL ERROR: CALL_AND_RETRY_LAST Allocation failed - JavaScript heap out of memory
[4112:0000020E2E0D5000]  3023124 ms: Mark-Compact (reduce) 4093.1 (4100.3) -> 4093.0 (4099.3) MB
```

The crash occurred at around 4093 MB, which is Node.js's default 4GB heap limit. **The crash happened in Windsurf, not Unity.**

## Root Cause - The Chain Reaction

When running Unity builds, a **cascade of events** exhausts Windsurf's memory:

### 1. Build Preparation Triggers Mass File Changes
- Build preparation injects **23,935 files** (1.82 GB) from cache into Unity project
- These include packages, DLLs, and NuGet assemblies
- **25,123 C# files** (217 MB total) exist in the project

### 2. Windsurf File Watcher Detects Everything
- File system watchers detect all injected files
- Even with `files.exclude`, watchers can still trigger on changes
- Windsurf tries to process thousands of file change events

### 3. Language Servers Attempt Full Re-indexing
- **C# Language Server**: Re-indexes 25K+ C# files (~1.1 GB RAM usage observed)
- **Copilot Language Server**: Loads files for AI context (~112 MB)
- **TypeScript/JSON Language Servers**: Process 544+ files

### 4. Multiple Node.js Processes Compete for Memory
- Windsurf main process
- Multiple extension host processes
- Language server processes
- File watcher processes
- Each hits memory limits independently

### 5. Weakest Process Crashes First
- One Node.js process (in this case PID 4112) hits the 4GB limit
- Crash occurs in Windsurf, making it look like a build failure
- Unity build itself may be fine, but the IDE crashes

## Solution - Two-Pronged Approach

### Primary Fix: Exclude Unity/Build Folders from Windsurf

**This is the most important fix** - preventing Windsurf from watching/indexing build artifacts:

**.vscode/settings.json** now excludes:
```json
{
  "files.watcherExclude": {
    "**/build/preparation/cache/**": true,
    "**/projects/client/Library/**": true,
    "**/projects/client/Temp/**": true,
    "**/projects/client/Logs/**": true
  },
  "search.exclude": {
    "**/build/preparation/cache/**": true,
    "**/projects/client/Library/**": true
  }
}
```

This prevents the chain reaction by stopping file watchers from detecting build artifact changes.

### Secondary Fix: Increase Node.js Memory Limits

For processes that still need more memory (Unity's internal Node.js, remaining language servers):

#### For Build Scripts (Unity-spawned Node.js)
Added NODE_OPTIONS to the global environment variables:
```yaml
env:
  DOTNET_CLI_TELEMETRY_OPTOUT: "1"
  NODE_OPTIONS: "--max-old-space-size=8192"
```

### 2. build.ps1 (Build Script Level)
Added NODE_OPTIONS to PowerShell build script:
```powershell
$env:NODE_OPTIONS = "--max-old-space-size=8192"
```

### 3. IUnityBuild.cs (Unity Build Target Level)
Added NODE_OPTIONS before each Unity build invocation:
```csharp
// Set NODE_OPTIONS for Unity's internal Node.js processes (ShaderGraph, Addressables, etc.)
Environment.SetEnvironmentVariable("NODE_OPTIONS", "--max-old-space-size=8192");
```

This was added to all Unity build targets:
- `BuildUnity` (generic build)
- `BuildWindows`
- `BuildMacOS`
- `BuildLinux`
- `BuildAndroid`
- `BuildiOS`
- `BuildWebGL`
- `TestUnity` (test execution)

## Why This Solution Works

### Primary Protection: File Watcher Exclusions
- **Prevents the trigger**: Windsurf no longer watches build cache (23K files)
- **No re-indexing**: Language servers don't try to process injected files
- **Faster IDE**: Reduced file system events improve responsiveness
- **Lower memory**: No mass file analysis during builds

### Secondary Protection: Memory Limits
- Unity's Node.js processes (ShaderGraph, Addressables) get 8GB
- Remaining language server processes have headroom if needed
- System-wide setting protects IDE processes if set

## Why It Crashed This Time But Not Before

**Previous sessions likely:**
- Didn't trigger build preparation (or used smaller cache)
- Had fewer packages/files to inject
- Had Windsurf closed during builds
- Used Unity Editor directly (not from IDE terminal)

**This time:**
- Build preparation injected 1.82 GB of files
- Windsurf was open and watching
- Multiple language servers active
- Perfect storm of file events + memory pressure

## Verification

To verify the fix is working:

1. Check environment variable is set:
   ```powershell
   $env:NODE_OPTIONS
   ```
   Should output: `--max-old-space-size=8192`

2. Run a build and monitor Node.js processes:
   ```powershell
   task build
   ```

3. If crashes still occur, you can increase the limit further by changing `8192` to `12288` (12GB) or higher.

## Related Files

- `Taskfile.yml` - Global task environment
- `build/nuke/build.ps1` - Build script environment
- `build/nuke/build/Components/IUnityBuild.cs` - Unity build targets
- `.node-options` - Informational only (not used by Unity-spawned processes)

## Prevention

To prevent this issue in the future:
- Monitor memory usage during builds
- Consider adding memory usage logging to build reports
- Document memory requirements for CI/CD environments
- Ensure NODE_OPTIONS is set in CI/CD pipeline configurations
