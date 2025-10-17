# Unity Build Component

A reusable Nuke.build component for building Unity projects following the [Build Components pattern](https://nuke.build/docs/sharing/build-components/).

## Usage

To use this component in your build, simply implement the `IUnityBuild` interface in your `Build` class:

```csharp
class Build : NukeBuild, IUnityBuild
{
    // Your existing build configuration
}
```

## Available Targets

The component provides the following targets:

### CleanUnity

Cleans Unity build artifacts including the output directory and temporary folders.

```bash
nuke CleanUnity
```

### BuildUnity

Builds the Unity project for the specified target platform.

```bash
nuke BuildUnity
nuke BuildUnity --unity-build-target Android
nuke BuildUnity --unity-path "C:\Program Files\Unity\Hub\Editor\2021.3.0f1\Editor\Unity.exe"
```

### ExportUnityPackage

Exports the Unity project as a .unitypackage file.

```bash
nuke ExportUnityPackage
```

### TestUnity

Runs Unity EditMode tests and generates test results.

```bash
nuke TestUnity
```

## Parameters

The component exposes the following parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--unity-path` | Path to Unity executable | Auto-detected from Unity Hub installations |
| `--unity-project-path` | Path to Unity project | Root directory |
| `--unity-build-target` | Target platform (StandaloneWindows64, Android, iOS, etc.) | StandaloneWindows64 |
| `--unity-build-output` | Output path for builds | `{RootDirectory}/output` |

## Examples

### Build for Windows

```bash
nuke BuildUnity --unity-build-target StandaloneWindows64
```

### Build for Android

```bash
nuke BuildUnity --unity-build-target Android
```

### Build for iOS

```bash
nuke BuildUnity --unity-build-target iOS
```

### Custom Unity Path

```bash
nuke BuildUnity --unity-path "C:\Program Files\Unity\Hub\Editor\2023.1.0f1\Editor\Unity.exe"
```

### Clean and Build

```bash
nuke CleanUnity BuildUnity
```

## Requirements

- Unity installed via Unity Hub or custom location
- Unity project with a build script (for BuildUnity target)
- Nuke.Build package

## Customization

You can override any of the parameters in your `Build` class:

```csharp
class Build : NukeBuild, IUnityBuild
{
    // Override default Unity project path
    AbsolutePath IUnityBuild.UnityProjectPath => RootDirectory / "MyUnityProject";

    // Override default build target
    string IUnityBuild.UnityBuildTarget => "Android";

    // Add custom targets that depend on Unity targets
    Target BuildAll => _ => _
        .DependsOn(((IUnityBuild)this).BuildUnity)
        .Executes(() =>
        {
            // Additional build steps
        });
}
```

## Notes

- The `BuildUnity` target expects a `BuildScript.Build` method in your Unity project
- Build logs are saved to the output directory
- Unity is run in batch mode with no graphics
- Default timeout for builds is 30 minutes

---

# Build Preparation Component

A Nuke.build component for managing Unity build preparation with two-phase workflow following R-BLD-060 compliance.

## Overview

The Build Preparation component implements a **two-phase workflow** to safely prepare Unity projects for builds:

- **Phase 1 (PrepareCache):** Populate cache from source - safe to run anytime
- **Phase 2 (PrepareClient):** Inject from cache to client - build-time only with git reset

This ensures `projects/client/` is never modified outside build operations (R-BLD-060).

## Usage

The component is implemented as a partial class in `Build.Preparation.cs`:

```csharp
partial class Build : IUnityBuild
{
    // Preparation targets are automatically available
}
```

## Available Targets

### PrepareCache (Phase 1)

Populates the preparation cache from the code-quality project.

```bash
nuke PrepareCache
```

**What it does:**

- Calls `tool cache populate --source projects/code-quality`
- Gathers packages, assemblies, and assets into cache
- Safe to run anytime, no client modification
- Can be run independently

**When to use:**

- When dependencies change
- Before starting a build
- To refresh the cache
- Anytime (safe operation)

### PrepareClient (Phase 2)

Injects preparation from cache into the Unity client project.

```bash
nuke PrepareClient
```

**What it does:**

1. Performs `git reset --hard` on `projects/client/` (R-BLD-060)
2. Calls `tool prepare inject --config <path> --target projects/client/`
3. Injects packages, assemblies, patches from cache
4. Depends on `PrepareCache` (runs it first if needed)

**When to use:**

- During build operations ONLY
- Automatically called by `BuildUnityWithPreparation`
- Never run manually outside builds

**⚠️ Warning:** This modifies `projects/client/`. Only use during builds!

### RestoreClient

Restores the Unity client project to clean state.

```bash
nuke RestoreClient
```

**What it does:**

- Performs `git reset --hard` on `projects/client/`
- Removes all injected files
- Cleans up after builds

**When to use:**

- After builds to clean up
- To reset client to clean state
- Automatically called by `BuildUnityWithPreparation`

### BuildUnityWithPreparation

Full Unity build workflow with preparation.

```bash
nuke BuildUnityWithPreparation
```

**What it does:**

1. `PrepareCache` - Populate cache
2. `PrepareClient` - Inject to client (with git reset)
3. `BuildUnity` - Build Unity project
4. `RestoreClient` - Clean up (triggered after build)

**When to use:**

- Primary build command
- CI/CD pipelines
- Automated builds
- Full workflow execution

### ValidatePreparation

Validates preparation configuration without executing.

```bash
nuke ValidatePreparation
```

**What it does:**

- Calls `tool config validate --file <config> --level Full`
- Validates configuration schema
- Checks file existence
- Validates Unity package references

**When to use:**

- Before builds to catch config errors
- In CI/CD validation steps
- When editing configs

### DryRunPreparation

Shows what would be injected without modifying files.

```bash
nuke DryRunPreparation
```

**What it does:**

- Depends on `PrepareCache`
- Calls `tool prepare inject --dry-run`
- Shows file operations without executing
- No actual modifications

**When to use:**

- Testing new configurations
- Previewing changes
- Debugging preparation issues

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `--preparation-config` | Path to preparation config | `build/preparation/configs/default.json` |

## Examples

### Basic Build with Preparation

```bash
nuke BuildUnityWithPreparation
```

### Refresh Cache Only

```bash
nuke PrepareCache
```

### Validate Before Building

```bash
nuke ValidatePreparation
nuke BuildUnityWithPreparation
```

### Preview Changes

```bash
nuke DryRunPreparation
```

### Manual Workflow

```bash
# Step 1: Populate cache
nuke PrepareCache

# Step 2: Inject to client
nuke PrepareClient

# Step 3: Build Unity
nuke BuildUnity

# Step 4: Clean up
nuke RestoreClient
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Build Unity

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Validate Preparation
        run: nuke ValidatePreparation

      - name: Build Unity with Preparation
        run: nuke BuildUnityWithPreparation
```

### GitLab CI

```yaml
build:
  script:
    - nuke ValidatePreparation
    - nuke BuildUnityWithPreparation
```

### Azure Pipelines

```yaml
steps:
  - task: PowerShell@2
    displayName: 'Validate Preparation'
    inputs:
      targetType: 'inline'
      script: 'nuke ValidatePreparation'

  - task: PowerShell@2
    displayName: 'Build Unity with Preparation'
    inputs:
      targetType: 'inline'
      script: 'nuke BuildUnityWithPreparation'
```

## Two-Phase Workflow Details

### Why Two Phases?

**R-BLD-060 Compliance:**

- `projects/client` is a standalone Git repository
- Must remain clean except during builds
- Git reset ensures clean state before injection

**Benefits:**

- ✅ Cache can be prepared independently
- ✅ Client never modified outside builds
- ✅ Clear separation of concerns
- ✅ Safer development workflow
- ✅ Faster builds (cache reuse)

### Phase 1: PrepareCache

**Safe Operations:**

- Reads from `projects/code-quality`
- Writes to `build/preparation/cache/`
- No client modification
- Can run anytime

**Output:**

- Cached packages (`.tgz` files)
- Cached assemblies (`.dll` files)
- Cached assets
- Cache manifest

### Phase 2: PrepareClient

**Build-Time Operations:**

1. Git reset `projects/client/` to clean state
2. Copy packages from cache to client
3. Copy assemblies from cache to client
4. Apply code patches
5. Modify Unity manifest
6. Set scripting defines

**Safety:**

- Only runs during builds
- Git reset ensures clean start
- Automatic cleanup after build
- Target validation (only `projects/client/`)

## Customization

### Override Config Path

```csharp
partial class Build
{
    [Parameter("Path to preparation config")]
    AbsolutePath PreparationConfig => RootDirectory / "my-configs" / "custom.json";
}
```

### Custom Source Path

```csharp
partial class Build
{
    AbsolutePath CodeQualityProject => RootDirectory / "my-source";
}
```

### Add Pre/Post Steps

```csharp
partial class Build
{
    Target CustomPrepare => _ => _
        .DependsOn(PrepareClient)
        .Before(((IUnityBuild)this).BuildUnity)
        .Executes(() =>
        {
            // Custom preparation steps
        });
}
```

## Troubleshooting

### Error: "Cache files not found"

**Cause:** Phase 2 run before Phase 1.

**Solution:**

```bash
nuke PrepareCache  # Run Phase 1 first
nuke PrepareClient # Then Phase 2
```

### Error: "Target must be 'projects/client/'"

**Cause:** Invalid target path.

**Solution:** Only `projects/client/` is allowed per R-BLD-060.

### Client Not Clean After Build

**Cause:** `RestoreClient` not run.

**Solution:**

```bash
nuke RestoreClient  # Manual cleanup
# Or use BuildUnityWithPreparation (auto cleanup)
```

### Cache Out of Date

**Cause:** Dependencies changed but cache not refreshed.

**Solution:**

```bash
nuke PrepareCache  # Refresh cache
```

## Best Practices

### DO ✅

- **DO** use `BuildUnityWithPreparation` for full builds
- **DO** run `PrepareCache` when dependencies change
- **DO** validate configs before building
- **DO** use dry-run to preview changes
- **DO** let Nuke handle git reset automatically

### DON'T ❌

- **DON'T** run `PrepareClient` outside builds
- **DON'T** modify `projects/client/` manually during builds
- **DON'T** skip cache population
- **DON'T** forget to restore client after builds
- **DON'T** bypass git reset

## Requirements

- .NET 8.0 SDK
- Build Preparation Tool installed
- Git (for reset operations)
- Unity project at `projects/client/`
- Code quality project at `projects/code-quality/`
- Nuke.Build package

## Related Documentation

- **Tool README:** `packages/.../tool/README.md`
- **Migration Guide:** `packages/.../tool/MIGRATION-GUIDE.md`
- **Spec Amendment:** `.specify/specs/build-preparation-tool-amendment-001.md`
- **Build.Preparation.cs:** `build/nuke/build/Build.Preparation.cs`
