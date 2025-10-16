# Build Artifacts Directory

This directory contains versioned build artifacts following **R-BLD-070**.

## Structure

```
build/_artifacts/
├── .gitkeep
├── README.md
└── {version}/                           # e.g., 1.0.0, 1.0.0-beta.1, 0.1.0-alpha.5
    ├── version.json                     # Version manifest
    ├── unity-output/                    # Unity build outputs
    │   ├── Android/                     # Android builds
    │   │   ├── SangoCard.apk            # APK build
    │   │   └── SangoCard.aab            # Android App Bundle
    │   ├── iOS/                         # iOS builds
    │   │   ├── SangoCard.ipa            # iOS App Archive
    │   │   └── dSYMs/                   # Debug symbols
    │   ├── StandaloneWindows64/         # Windows 64-bit builds
    │   │   ├── SangoCard.exe            # Windows executable
    │   │   └── SangoCard_Data/          # Unity data folder
    │   ├── StandaloneLinux64/           # Linux 64-bit builds
    │   │   └── SangoCard.x86_64         # Linux executable
    │   └── WebGL/                       # WebGL builds
    │       ├── index.html
    │       └── Build/
    ├── logs/                            # Build logs
    │   ├── unity-build.log              # Unity build log
    │   ├── gradle-build.log             # Gradle build log (Android)
    │   └── xcode-build.log              # Xcode build log (iOS)
    └── intermediate/                    # Intermediate build artifacts
        ├── il2cpp/                      # IL2CPP output
        ├── gradle/                      # Android Gradle intermediate files
        └── xcode/                       # iOS Xcode intermediate files
```

## Versioning with GitVersion

Versions are determined by [GitVersion](https://gitversion.net/) based on Git history and commit messages.

### Branch Versioning

- **main**: Release versions (e.g., `1.0.0`, `1.2.3`)
- **develop**: Alpha versions (e.g., `1.0.0-alpha.1`)
- **feature/xyz**: Feature versions (e.g., `1.0.0-xyz.1`)
- **release/1.x**: Beta versions (e.g., `1.0.0-beta.1`)
- **hotfix/xyz**: Hotfix betas (e.g., `1.0.1-beta.1`)

### Semantic Versioning via Commits

Control version bumps with commit message tags:

```bash
# Major version bump (breaking changes)
git commit -m "feat: new API +semver:major"
# 1.0.0 → 2.0.0

# Minor version bump (new features)
git commit -m "feat: add card system +semver:minor"
# 1.0.0 → 1.1.0

# Patch version bump (bug fixes)
git commit -m "fix: resolve crash +semver:patch"
# 1.0.0 → 1.0.1

# No version bump
git commit -m "docs: update README +semver:none"
# 1.0.0 → 1.0.0
```

## Build Process

When building, scripts **MUST**:

1. Query GitVersion for current version:
   ```bash
   dotnet gitversion /showvariable SemVer
   # Output: 1.0.0-beta.1
   ```

2. Create versioned directory with subfolders:
   ```bash
   VERSION="1.0.0-beta.1"
   mkdir -p build/_artifacts/${VERSION}/unity-output/{Android,iOS,StandaloneWindows64,logs,intermediate}
   ```

3. Build to platform-specific path:
   ```bash
   # Unity Android build outputs to:
   build/_artifacts/1.0.0-beta.1/unity-output/Android/SangoCard.apk
   
   # Unity Windows build outputs to:
   build/_artifacts/1.0.0-beta.1/unity-output/StandaloneWindows64/SangoCard.exe
   build/_artifacts/1.0.0-beta.1/unity-output/StandaloneWindows64/SangoCard_Data/
   
   # Build logs go to:
   build/_artifacts/1.0.0-beta.1/logs/unity-build.log
   
   # Intermediate Gradle project (Android):
   build/_artifacts/1.0.0-beta.1/intermediate/gradle/
   ```

4. Generate version manifest at root of version directory:
   ```json
   {
     "version": "1.0.0-beta.1",
     "buildTime": "2025-10-16T16:40:00Z",
     "commit": "1ec20ff",
     "branch": "release/1.0",
     "platforms": {
       "Android": {
         "apk": "unity-output/Android/SangoCard.apk",
         "buildLog": "logs/gradle-build.log"
       },
       "StandaloneWindows64": {
         "executable": "unity-output/StandaloneWindows64/SangoCard.exe",
         "buildLog": "logs/unity-build.log"
       }
     }
   }
   ```

## Running Built Executables

**Always** use the full versioned path with unity-output and platform folder:

```bash
# ✅ Correct - with unity-output and platform subfolder
./build/_artifacts/1.0.0/unity-output/StandaloneWindows64/SangoCard.exe

# ❌ Wrong - no unity-output folder
./build/_artifacts/1.0.0/StandaloneWindows64/SangoCard.exe

# ❌ Wrong - no platform subfolder
./build/_artifacts/1.0.0/unity-output/SangoCard.exe

# ❌ Wrong - no version
./build/_artifacts/SangoCard.exe
```

## Platform-Specific Paths

```bash
# Android
build/_artifacts/{version}/unity-output/Android/SangoCard.apk
adb install build/_artifacts/1.0.0/unity-output/Android/SangoCard.apk

# iOS
build/_artifacts/{version}/unity-output/iOS/SangoCard.ipa
xcrun simctl install booted build/_artifacts/1.0.0/unity-output/iOS/SangoCard.ipa

# Windows
build/_artifacts/{version}/unity-output/StandaloneWindows64/SangoCard.exe

# Linux
build/_artifacts/{version}/unity-output/StandaloneLinux64/SangoCard.x86_64

# WebGL (serve directory)
cd build/_artifacts/{version}/unity-output/WebGL && python -m http.server
```

## Example Usage

```bash
# Get current version
VERSION=$(dotnet gitversion /showvariable SemVer)

# Run Windows build
./build/_artifacts/${VERSION}/unity-output/StandaloneWindows64/SangoCard.exe

# Install Android build to device
adb install build/_artifacts/${VERSION}/unity-output/Android/SangoCard.apk

# Check build logs
cat build/_artifacts/${VERSION}/logs/unity-build.log

# Compare versions (Windows)
./build/_artifacts/1.0.0/unity-output/StandaloneWindows64/SangoCard.exe &     # Old
./build/_artifacts/1.0.1/unity-output/StandaloneWindows64/SangoCard.exe &     # New

# Archive specific platform
tar -czf archives/1.0.0-android.tar.gz build/_artifacts/1.0.0/unity-output/Android/

# Archive all platforms for version
tar -czf archives/1.0.0-all.tar.gz build/_artifacts/1.0.0/

# Clean intermediate files but keep final builds
rm -rf build/_artifacts/1.0.0/intermediate/
rm -rf build/_artifacts/1.0.0/logs/
```

## Benefits

- **Traceability**: Each build is tied to a specific version
- **Coexistence**: Multiple versions can exist simultaneously
- **Rollback**: Easy to test or deploy previous versions
- **CI/CD**: Automated versioning based on Git history
- **Testing**: Compare behavior across versions

## References

- **Rule**: R-BLD-070 in `.agent/base/20-rules.md`
- **Config**: `GitVersion.yml` in repository root
- **Docs**: https://gitversion.net/docs/
