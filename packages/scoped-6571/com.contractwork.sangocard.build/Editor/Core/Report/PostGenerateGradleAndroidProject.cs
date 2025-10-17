using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnityEditor;
using UnityEditor.Android;

namespace Plate.BuildAssistant.Editor.Report;

internal class PostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject
{
    private static ILogger<PostGenerateGradleAndroidProject>? _logger;

    private static ILogger<PostGenerateGradleAndroidProject> Log =>
        _logger ??= new NullLogger<PostGenerateGradleAndroidProject>();

#pragma warning disable IDE1006
    public int callbackOrder => 0;
#pragma warning restore IDE1006

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var loggerFactory = Splat.Locator.Current.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        _logger = loggerFactory?.CreateLogger<PostGenerateGradleAndroidProject>() ?? new NullLogger<PostGenerateGradleAndroidProject>();

        Log.LogDebug("[PostProcess] Gradle project generated at: {Path}", path);

        // Create missing Gradle build files that Unity doesn't generate
        CreateMissingGradleFiles(path);

        // Patch existing files
        PatchGradleRepositories(path);
        PatchAllManifests(path);
        EnsureNamespaceInGradle(path);

        Log.LogDebug("[PostProcess] Gradle project completion finished successfully");
    }

    private static void PatchAllManifests(string rootPath)
    {
        var manifestPaths = Directory.GetFiles(rootPath, "AndroidManifest.xml", SearchOption.AllDirectories);
        foreach (var manifestPath in manifestPaths)
        {
            PatchAndroidManifest(manifestPath);
        }
    }

    private static void PatchAndroidManifest(string manifestPath)
    {
        try
        {
            var doc = XDocument.Load(manifestPath);
            XNamespace androidNs = "http://schemas.android.com/apk/res/android";
            XNamespace toolsNs = "http://schemas.android.com/tools";

            var manifestElement = doc.Root;
            var applicationElement = manifestElement?.Element("application");

            if (manifestElement == null || applicationElement == null)
                return;

            var isLauncher = manifestPath.ToLowerInvariant().Contains($"{Path.DirectorySeparatorChar}launcher{Path.DirectorySeparatorChar}");
            var isLibrary = manifestPath.ToLowerInvariant().Contains($"{Path.DirectorySeparatorChar}unitylibrary{Path.DirectorySeparatorChar}");

            // Ensure 'package' attribute only appears in launcher
            var packageAttr = manifestElement.Attribute("package");
            if (isLibrary && packageAttr != null)
            {
                packageAttr.Remove();
                Log.LogDebug("Removed 'package' from library manifest: {Path}", manifestPath);
            }

            // Patch only the launcher manifest to avoid conflict
            if (isLauncher)
            {
                applicationElement.SetAttributeValue(androidNs + "icon", "@mipmap/ic_launcher");
                applicationElement.SetAttributeValue(androidNs + "label", "My Game");

                // Ensure tools:replace includes necessary attributes
                var replaceAttr = applicationElement.Attribute(toolsNs + "replace");
                var values = (replaceAttr?.Value.Split(',') ?? Array.Empty<string>())
                    .Concat(new[] { "android:icon", "android:label", "android:theme" })
                    .Distinct();
                applicationElement.SetAttributeValue(toolsNs + "replace", string.Join(",", values));
            }

            // UnityPlayerActivity handling
            var unityActivity = applicationElement.Elements("activity")
                .FirstOrDefault(x => x.Attribute(androidNs + "name")?.Value == "com.unity3d.player.UnityPlayerActivity");

            if (unityActivity != null)
            {
                unityActivity.SetAttributeValue(toolsNs + "replace", "android:configChanges");
                unityActivity.SetAttributeValue(androidNs + "exported", "true");
            }

            if (isLibrary)
            {
                // Remove icon and label from library manifest if present
                var iconAttr = applicationElement.Attribute(androidNs + "icon");
                var labelAttr = applicationElement.Attribute(androidNs + "label");

                iconAttr?.Remove();
                labelAttr?.Remove();

                Log.LogDebug("Removed 'icon' and 'label' from library manifest: {Path}", manifestPath);

                // Remove FacebookContentProvider if any
                var providerElements = applicationElement.Elements("provider")
                    .Where(x => x.Attribute(androidNs + "authorities")?.Value?.Contains("FacebookContentProvider") == true)
                    .ToList();

                foreach (var provider in providerElements)
                {
                    provider.Remove();
                    Log.LogDebug("Removed FacebookContentProvider from library manifest: {Path}", manifestPath);
                }

                // Inject UnityPlayerGameActivity if missing
                bool hasGameActivity = applicationElement.Elements("activity")
                    .Any(x => x.Attribute(androidNs + "name")?.Value == "com.unity3d.player.UnityPlayerGameActivity");

                if (!hasGameActivity)
                {
                    var gameActivity = new XElement("activity",
                        new XAttribute(androidNs + "name", "com.unity3d.player.UnityPlayerGameActivity"),
                        new XAttribute(androidNs + "exported", "true"),
                        new XAttribute(androidNs + "theme", "@style/BaseUnityGameActivityTheme"),
                        new XElement("intent-filter",
                            new XElement("action", new XAttribute(androidNs + "name", "android.intent.action.MAIN")),
                            new XElement("category", new XAttribute(androidNs + "name", "android.intent.category.LAUNCHER"))
                        ),
                        new XElement("meta-data",
                            new XAttribute(androidNs + "name", "unityplayer.UnityActivity"),
                            new XAttribute(androidNs + "value", "true")
                        ),
                        new XElement("meta-data",
                            new XAttribute(androidNs + "name", "android.app.lib_name"),
                            new XAttribute(androidNs + "value", "game")
                        )
                    );

                    applicationElement.Add(gameActivity);
                    applicationElement.SetAttributeValue(androidNs + "debuggable", "true");

                    Log.LogDebug("Injected UnityPlayerGameActivity into library manifest: {Path}", manifestPath);
                }
            }

            doc.Save(manifestPath);
            Log.LogDebug("Patched AndroidManifest.xml at: {ManifestPath}", manifestPath);
        }
        catch (Exception ex)
        {
            Log.LogError("Failed to patch AndroidManifest.xml at {ManifestPath}: {ExMessage}", manifestPath, ex.Message);
        }
    }
    private static void EnsureNamespaceInGradle(string rootPath)
    {
        var gradleFiles = Directory.GetFiles(rootPath, "build.gradle", SearchOption.AllDirectories)
            .Where(p => p.ToLowerInvariant().Contains("androidlib"));

        foreach (var gradleFile in gradleFiles)
        {
            var lines = File.ReadAllLines(gradleFile).ToList();
            var androidBlockStart = lines.FindIndex(line => line.TrimStart().StartsWith("android {"));

            if (androidBlockStart >= 0 && !lines.Any(l => l.Contains("namespace")))
            {
                lines.Insert(androidBlockStart + 1, "    namespace 'com.firebase.app'");
                File.WriteAllLines(gradleFile, lines);
                Log.LogDebug("Inserted namespace into: {File}", gradleFile);
            }
        }
    }

    private static void PatchGradleRepositories(string rootPath)
    {
        var gradleFiles = Directory.GetFiles(rootPath, "build.gradle", SearchOption.AllDirectories);
        foreach (var gradleFile in gradleFiles)
        {
            PatchGradleFile(gradleFile);
        }
    }

    private static void PatchGradleFile(string gradlePath)
    {
        var lines = File.ReadAllLines(gradlePath).ToList();
        var index = lines.FindIndex(line => line.Trim().StartsWith("repositories {"));
        if (index >= 0 && !lines.Any(l => l.Contains("GeneratedLocalRepo")))
        {
            lines.Insert(index + 1, "        maven { url \"file://$rootDir/../../Assets/GeneratedLocalRepo/Firebase/m2repository\" }");
            File.WriteAllLines(gradlePath, lines);
            Log.LogDebug("Patched Gradle file with Firebase local repo: {Path}", gradlePath);
        }
    }

    /// <summary>
    /// Creates missing Gradle build files that Unity doesn't generate automatically
    /// </summary>
    private static void CreateMissingGradleFiles(string rootPath)
    {
        Log.LogDebug("Creating missing Gradle files in: {Path}", rootPath);

        CreateRootBuildGradle(rootPath);
        CreateSettingsGradle(rootPath);
        CreateGradleProperties(rootPath);
        CreateLocalProperties(rootPath);
        EnsureLauncherBuildGradle(rootPath);
        EnsureUnityLibraryBuildGradle(rootPath);
        EnsureGradleWrapper(rootPath);
        FixUnityJavaCompatibilityIssues(rootPath);
        FixKotlinStdlibConflicts(rootPath);
    }

    private static void CreateRootBuildGradle(string rootPath)
    {
        var buildGradlePath = Path.Combine(rootPath, "build.gradle");

        if (!File.Exists(buildGradlePath))
        {
            var content = @"plugins {
    // Modern Android Gradle Plugin for Unity build-assistant
    // Compatible with Gradle version preinstalled with Unity 6000.0.51f1
    id 'com.android.application' version '8.7.2' apply false
    id 'com.android.library' version '8.7.2' apply false
}

tasks.register('clean', Delete) {
    delete rootProject.layout.buildDirectory
}";

            File.WriteAllText(buildGradlePath, content);
            Log.LogDebug("Created modern root build.gradle (AGP 8.7.2): {Path}", buildGradlePath);
        }
        else
        {
            // Update existing build.gradle to modern format
            ModernizeExistingBuildGradle(buildGradlePath);
        }
    }

    private static void ModernizeExistingBuildGradle(string buildGradlePath)
    {
        var lines = File.ReadAllLines(buildGradlePath).ToList();
        bool modified = false;

        // Replace buildscript with modern plugins block if needed
        var buildscriptIndex = lines.FindIndex(l => l.TrimStart().StartsWith("buildscript {"));
        if (buildscriptIndex >= 0)
        {
            // Find the end of buildscript block
            var braceCount = 0;
            var endIndex = buildscriptIndex;
            for (int i = buildscriptIndex; i < lines.Count; i++)
            {
                if (lines[i].Contains("{")) braceCount += lines[i].Count(c => c == '{');
                if (lines[i].Contains("}")) braceCount -= lines[i].Count(c => c == '}');
                if (braceCount == 0 && i > buildscriptIndex)
                {
                    endIndex = i;
                    break;
                }
            }

            // Replace with modern plugins block
            var modernBlock = new List<string>
            {
                "plugins {",
                "    // Modern Android Gradle Plugin for Unity build-assistant",
                "    // Compatible with Gradle version preinstalled with Unity 6000.0.51f1",
                "    id 'com.android.application' version '8.7.2' apply false",
                "    id 'com.android.library' version '8.7.2' apply false",
                "}"
            };

            // Remove old buildscript block and insert modern plugins
            lines.RemoveRange(buildscriptIndex, endIndex - buildscriptIndex + 1);
            lines.InsertRange(buildscriptIndex, modernBlock);
            modified = true;

            Log.LogInformation("Upgraded to modern plugins block (AGP 8.7.2) in: {Path}", buildGradlePath);
        }

        // Update clean task to modern syntax
        var cleanTaskIndex = lines.FindIndex(l => l.Contains("task clean(type: Delete)"));
        if (cleanTaskIndex >= 0)
        {
            lines[cleanTaskIndex] = "tasks.register('clean', Delete) {";
            var deleteIndex = lines.FindIndex(cleanTaskIndex, l => l.Contains("delete rootProject.buildDir"));
            if (deleteIndex >= 0)
            {
                lines[deleteIndex] = "    delete rootProject.layout.buildDirectory";
            }
            modified = true;
            Log.LogDebug("Modernized clean task syntax in: {Path}", buildGradlePath);
        }

        if (modified)
        {
            File.WriteAllLines(buildGradlePath, lines);
        }
    }

    private static void CreateSettingsGradle(string rootPath)
    {
        var settingsGradlePath = Path.Combine(rootPath, "settings.gradle");

        if (!File.Exists(settingsGradlePath))
        {
            var content = @"pluginManagement {
    repositories {
        gradlePluginPortal()
        google()
        mavenCentral()
    }
}

include ':launcher', ':unityLibrary'

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.PREFER_SETTINGS)
    repositories {
        google()
        mavenCentral()
        mavenLocal()
        flatDir {
            dirs ""${project(':unityLibrary').projectDir}/libs""
        }
    }
}";

            File.WriteAllText(settingsGradlePath, content);
            Log.LogDebug("Created settings.gradle: {Path}", settingsGradlePath);
        }
    }

    private static void CreateGradleProperties(string rootPath)
    {
        var gradlePropertiesPath = Path.Combine(rootPath, "gradle.properties");

        // Always create/update gradle.properties with current configuration
        var content = @"org.gradle.jvmargs=-Xmx4096M
org.gradle.parallel=true
unityStreamingAssets=.bin, .hash, .xml, .png, .mp4
android.useAndroidX=true
android.enableJetifier=true
unityTemplateVersion=19
unity.debugSymbolLevel=full
unity.buildToolsVersion=34.0.0
unity.minSdkVersion=23
unity.targetSdkVersion=36
unity.compileSdkVersion=36
unity.applicationId=com.giantcroissant.buildassistant.test
unity.abiFilters=armeabi-v7a,arm64-v8a
unity.versionCode=1
unity.versionName=1.0.0
unity.namespace=com.giantcroissant.buildassistant.test
unity.androidSdkPath=C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK
unity.androidNdkPath=C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK
unity.androidNdkVersion=27.2.12479018
unity.jdkPath=C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK
unity.javaCompatabilityVersion=VERSION_17
android.bundle.includeNativeDebugMetadata=false
# android.enableR8=true - deprecated in AGP 8.x, R8 is enabled by default
org.gradle.welcome=never";

        File.WriteAllText(gradlePropertiesPath, content);
        Log.LogDebug("Created/updated gradle.properties: {Path}", gradlePropertiesPath);
    }

    private static void CreateLocalProperties(string rootPath)
    {
        var localPropertiesPath = Path.Combine(rootPath, "local.properties");

        if (!File.Exists(localPropertiesPath))
        {
            var content = @"# This file was automatically generated by Unity
sdk.dir=C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK
ndk.dir=C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK";

            File.WriteAllText(localPropertiesPath, content);
            Log.LogDebug("Created local.properties: {Path}", localPropertiesPath);
        }
    }

    private static void EnsureLauncherBuildGradle(string rootPath)
    {
        var launcherBuildPath = Path.Combine(rootPath, "launcher", "build.gradle");

        if (!File.Exists(launcherBuildPath))
        {
            var content = @"apply plugin: 'com.android.application'

dependencies {
    implementation project(':unityLibrary')
}

android {
    namespace ""com.giantcroissant.buildassistant.test""
    ndkPath ""C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK""
    ndkVersion ""27.2.12479018""
    compileSdk 36
    buildToolsVersion = ""34.0.0""

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_17
        targetCompatibility JavaVersion.VERSION_17
    }

    defaultConfig {
        applicationId ""com.giantcroissant.buildassistant.test""
        versionName ""1.0.0""
        minSdk 23
        targetSdk 36
        versionCode 1

        ndk {
            abiFilters ""armeabi-v7a"", ""arm64-v8a""
            debugSymbolLevel ""full""
        }
    }

    lint {
        abortOnError false
    }

    androidResources {
        ignoreAssetsPattern = ""!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~""
        noCompress = ['.unity3d', '.ress', '.resource', '.obb', '.bundle', '.unityexp']
    }

    packaging {
        jniLibs {
            useLegacyPackaging true
        }
    }

    buildTypes {
        debug {
            minifyEnabled = false
            proguardFiles getDefaultProguardFile('proguard-android.txt')
            jniDebuggable = true
            signingConfig signingConfigs.debug
        }

        release {
            minifyEnabled = false
            proguardFiles getDefaultProguardFile('proguard-android.txt')
            signingConfig signingConfigs.debug
        }
    }
}";

            File.WriteAllText(launcherBuildPath, content);
            Log.LogDebug("Created launcher/build.gradle: {Path}", launcherBuildPath);
        }
    }

    private static void EnsureUnityLibraryBuildGradle(string rootPath)
    {
        var unityLibraryBuildPath = Path.Combine(rootPath, "unityLibrary", "build.gradle");

        if (!File.Exists(unityLibraryBuildPath))
        {
            var content = @"apply plugin: 'com.android.library'

dependencies {
    implementation fileTree(dir: 'libs', include: ['*.jar'])
    implementation 'androidx.appcompat:appcompat:1.6.1'
    implementation 'androidx.core:core:1.9.0'
    implementation 'androidx.games:games-activity:3.0.5'
    implementation 'androidx.constraintlayout:constraintlayout:2.1.4'
}

android {
    namespace ""com.unity3d.player""
    ndkPath ""C:/Program Files/Unity/Hub/Editor/6000.0.51f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK""
    ndkVersion ""27.2.12479018""

    compileSdk 36
    buildToolsVersion = ""34.0.0""

    compileOptions {
        sourceCompatibility JavaVersion.VERSION_17
        targetCompatibility JavaVersion.VERSION_17
    }

    defaultConfig {
        minSdk 23
        targetSdk 36
        ndk {
            abiFilters 'armeabi-v7a', 'arm64-v8a'
            debugSymbolLevel 'full'
        }
        versionCode 1
        versionName '1.0.0'
        consumerProguardFiles 'proguard-unity.txt'
        externalNativeBuild {
            cmake {
                arguments ""-DANDROID_STL=c++_shared"", ""-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON""
            }
        }
    }

    lint {
        abortOnError false
    }

    androidResources {
        noCompress = ['.unity3d', '.ress', '.resource', '.obb', '.bundle', '.unityexp']
        ignoreAssetsPattern = ""!.svn:!.git:!.ds_store:!*.scc:!CVS:!thumbs.db:!picasa.ini:!*~""
    }

    packaging {
        jniLibs {
            useLegacyPackaging true
        }
    }
}

android.externalNativeBuild {
    cmake {
        version ""3.22.1""
        path ""src/main/cpp/CMakeLists.txt""
    }
}

android.buildFeatures {
    prefab true
}";

            File.WriteAllText(unityLibraryBuildPath, content);
            Log.LogDebug("Created unityLibrary/build.gradle: {Path}", unityLibraryBuildPath);
        }
    }

    private static void EnsureGradleWrapper(string rootPath)
    {
        var gradleWrapperDir = Path.Combine(rootPath, "gradle", "wrapper");
        var gradleWrapperJarPath = Path.Combine(gradleWrapperDir, "gradle-wrapper.jar");

        if (!File.Exists(gradleWrapperJarPath))
        {
            // Copy gradle-wrapper.jar from our gradle-8.11 installation
            var sourceGradleDir = Path.Combine(rootPath, "..", "..", "fastlane", "android", "gradle-8.11");
            var sourceWrapperJar = Path.Combine(sourceGradleDir, "lib", "gradle-wrapper-shared-8.11.jar");

            if (File.Exists(sourceWrapperJar))
            {
                Directory.CreateDirectory(gradleWrapperDir);
                File.Copy(sourceWrapperJar, gradleWrapperJarPath);
                Log.LogDebug("Copied gradle-wrapper.jar: {Path}", gradleWrapperJarPath);
            }
            else
            {
                // Create a minimal gradle-wrapper.jar placeholder
                CreateMinimalGradleWrapperJar(gradleWrapperJarPath);
                Log.LogDebug("Created minimal gradle-wrapper.jar: {Path}", gradleWrapperJarPath);
            }
        }
    }

    private static void CreateMinimalGradleWrapperJar(string jarPath)
    {
        // For now, let's create an empty JAR file that at least exists
        // In a real scenario, we'd need the actual gradle-wrapper.jar content
        Directory.CreateDirectory(Path.GetDirectoryName(jarPath));
        File.WriteAllBytes(jarPath, new byte[0]); // Empty file placeholder
        Log.LogWarning("Created placeholder gradle-wrapper.jar - manual gradle-wrapper.jar copy needed for build");
    }

    private static void FixUnityJavaCompatibilityIssues(string rootPath)
    {
        // Fix the UnityPlayerGameActivity.java compilation error
        var javaFiles = Directory.GetFiles(rootPath, "*.java", SearchOption.AllDirectories)
            .Where(f => f.Contains("UnityPlayerGameActivity"));

        foreach (var javaFile in javaFiles)
        {
            try
            {
                var content = File.ReadAllText(javaFile);

                // Fix the @Override method signature issue
                if (content.Contains("@Override protected InputEnabledSurfaceView createSurfaceView()"))
                {
                    // Remove the @Override annotation for methods that don't actually override
                    content = content.Replace("@Override protected InputEnabledSurfaceView createSurfaceView()",
                                            "protected InputEnabledSurfaceView createSurfaceView()");

                    File.WriteAllText(javaFile, content);
                    Log.LogDebug("Fixed Java @Override issue in: {File}", javaFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("Failed to fix Java compatibility in {File}: {Error}", javaFile, ex.Message);
            }
        }
    }

    private static void FixKotlinStdlibConflicts(string rootPath)
    {
        // Fix Kotlin stdlib version conflicts in all build.gradle files
        var gradleFiles = Directory.GetFiles(rootPath, "build.gradle", SearchOption.AllDirectories);

        foreach (var gradleFile in gradleFiles)
        {
            try
            {
                var content = File.ReadAllText(gradleFile);

                // Add Kotlin stdlib conflict resolution if not already present
                if (!content.Contains("resolutionStrategy") && content.Contains("android {"))
                {
                    var conflictResolution = @"
// Fix Kotlin stdlib version conflicts
configurations.all {
    resolutionStrategy {
        force 'org.jetbrains.kotlin:kotlin-stdlib:1.8.22'
        force 'org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.8.22'
        force 'org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.8.22'
    }
    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'
    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'
}

";

                    // Insert the conflict resolution after the first line (apply plugin line)
                    var lines = content.Split('\n').ToList();
                    if (lines.Count > 1)
                    {
                        lines.Insert(2, conflictResolution);
                        content = string.Join("\n", lines);

                        File.WriteAllText(gradleFile, content);
                        Log.LogDebug("Added Kotlin stdlib conflict resolution to: {File}", gradleFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("Failed to fix Kotlin conflicts in {File}: {Error}", gradleFile, ex.Message);
            }
        }
    }
}
