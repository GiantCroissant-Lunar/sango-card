# Example Versioned Artifact Directory Structure

This file demonstrates the expected structure for a versioned build artifact.
It serves as a reference for build scripts and CI/CD pipelines.

## Directory Tree Example

```
build/_artifacts/1.0.0/
├── version.json
├── unity-output/
│   ├── Android/
│   │   ├── SangoCard.apk
│   │   ├── SangoCard.aab
│   │   └── symbols/
│   ├── iOS/
│   │   ├── SangoCard.ipa
│   │   └── dSYMs/
│   ├── StandaloneWindows64/
│   │   ├── SangoCard.exe
│   │   ├── SangoCard_Data/
│   │   ├── UnityCrashHandler64.exe
│   │   └── UnityPlayer.dll
│   ├── StandaloneLinux64/
│   │   ├── SangoCard.x86_64
│   │   └── SangoCard_Data/
│   └── WebGL/
│       ├── index.html
│       ├── Build/
│       └── TemplateData/
├── logs/
│   ├── unity-build.log
│   ├── unity-android.log
│   ├── unity-ios.log
│   ├── unity-windows.log
│   ├── gradle-build.log
│   └── xcode-build.log
└── intermediate/
    ├── il2cpp/
    ├── gradle/
    │   ├── build/
    │   └── src/
    └── xcode/
        └── Unity-iPhone.xcodeproj/
```

## Platform-Specific Notes

### Android
- **Final Output**: `unity-output/Android/SangoCard.apk` or `unity-output/Android/SangoCard.aab`
- **Intermediate**: `intermediate/gradle/` (Gradle project)
- **Logs**: `logs/unity-android.log`, `logs/gradle-build.log`

### iOS
- **Final Output**: `unity-output/iOS/SangoCard.ipa`
- **Intermediate**: `intermediate/xcode/` (Xcode project)
- **Logs**: `logs/unity-ios.log`, `logs/xcode-build.log`

### Windows Standalone
- **Final Output**: `unity-output/StandaloneWindows64/SangoCard.exe` + data folder
- **Logs**: `logs/unity-windows.log`

### WebGL
- **Final Output**: `unity-output/WebGL/` directory (served as-is)
- **Logs**: `logs/unity-webgl.log`

## Version Manifest (version.json)

Located at: `build/_artifacts/{version}/version.json`

```json
{
  \"version\": \"1.0.0\",
  \"buildTime\": \"2025-10-16T16:50:00Z\",
  \"commit\": \"2c37600\",
  \"branch\": \"main\",
  \"gitVersion\": {
    \"semVer\": \"1.0.0\",
    \"fullSemVer\": \"1.0.0\",
    \"majorMinorPatch\": \"1.0.0\",
    \"branchName\": \"main\",
    \"sha\": \"2c37600\"
  },
  \"platforms\": {
    \"Android\": {
      \"apk\": \"Android/SangoCard.apk\",
      \"aab\": \"Android/SangoCard.aab\",
      \"buildLog\": \"logs/gradle-build.log\",
      \"intermediate\": \"intermediate/gradle/\"
    },
    \"iOS\": {
      \"ipa\": \"iOS/SangoCard.ipa\",
      \"buildLog\": \"logs/xcode-build.log\",
      \"intermediate\": \"intermediate/xcode/\"
    },
    \"StandaloneWindows64\": {
      \"executable\": \"StandaloneWindows64/SangoCard.exe\",
      \"dataPath\": \"StandaloneWindows64/SangoCard_Data/\",
      \"buildLog\": \"logs/unity-windows.log\"
    }
  },
  \"unity\": {
    \"version\": \"2022.3.50f1\",
    \"buildTarget\": \"Multiple\"
  }
}
```
