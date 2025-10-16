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
