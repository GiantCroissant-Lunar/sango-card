using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UnityEditor;
using UnityEditor.Build.Reporting;
#if HAS_UNITY_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;

namespace SangoCard.Build.Editor;

[JetBrains.Annotations.PublicAPI]
public partial class BuildEntry
{
    private static bool _buildStarted = false;

    private static ILogger<BuildEntry>? _logger;

    private static ILogger<BuildEntry> Log =>
        _logger ??= new NullLogger<BuildEntry>();

    [InitializeOnLoadMethod]
    public static void SafePerformBuild()
    {
        Debug.Log("🚀 SafePerformBuild called - BUILD ENTRY POINT REACHED!");
        Debug.Log($"🚀 Command Line Args: {string.Join(" ", Environment.GetCommandLineArgs())}");

        // Only run in batchmode
        if (!UnityEngine.Application.isBatchMode)
        {
            Debug.LogWarning("SafePerformBuild called outside of batchmode, skipping build.");
            return;
        }

        int retryCount = 0;
        const int maxRetries = 100; // ~10 seconds if delayCall is ~0.1s

        UnityEditor.EditorApplication.delayCall += TryRunBuild;

        // Call immediately in case delayCall does not fire in batchmode
        TryRunBuild();

        void TryRunBuild()
        {
            var loggerFactory = Splat.Locator.Current.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
            Debug.Log(
                loggerFactory == null
                ? $"[{retryCount}] Logger factory not ready, deferring..."
                : "Logger factory is ready, proceeding with build.");

            if (loggerFactory == null)
            {
                retryCount++;
                if (retryCount > maxRetries)
                {
                    Debug.LogError("Logger factory was never registered. Aborting build.");
                    return;
                }
                UnityEditor.EditorApplication.delayCall += TryRunBuild;
                return;
            }
            else
            {
                UnityEditor.EditorApplication.delayCall -= TryRunBuild;
            }

            if (_buildStarted)
            {
                Debug.LogWarning("Build already started, skipping duplicate execution.");
                return;
            }
            _buildStarted = true;

            PerformBuild();
        }
    }

    public static void PerformBuild()
    {
        Debug.Log("?? PerformBuild method reached - starting actual build logic!");

        var loggerFactory = Splat.Locator.Current.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
        _logger = loggerFactory?.CreateLogger<BuildEntry>() ?? new NullLogger<BuildEntry>();

        try
        {
            CheckIssues();

            var args = Environment.GetCommandLineArgs();
            var adjustedArgs = args.Length > 1 ? args.Skip(1).ToArray() : Array.Empty<string>();

            if (!TryParseArguments(adjustedArgs, out var parsed, out var error))
            {
                Log.LogError("Failed to parse build arguments: {Error}", error);
                throw new Exceptions.BuildFailedException(error);
            }

            ProceedToBuild(
                parsed.OutputPath,
                parsed.BuildVersion,
                parsed.BuildProfileName,
                parsed.BuildPurpose,
                parsed.BuildTarget);

            EditorApplication.Exit(0);
        }
        catch (Exceptions.BuildFailedException e)
        {
            Log.LogError(e, nameof(BuildEntry));
            EditorApplication.Exit(1);
        }
    }

    internal readonly struct CliArguments
    {
        internal CliArguments(string outputPath, string buildVersion, string buildProfileName, string buildPurpose, string? buildTarget)
        {
            OutputPath = outputPath;
            BuildVersion = buildVersion;
            BuildProfileName = buildProfileName;
            BuildPurpose = buildPurpose;
            BuildTarget = buildTarget;
        }

        internal string OutputPath { get; }
        internal string BuildVersion { get; }
        internal string BuildProfileName { get; }
        internal string BuildPurpose { get; }
        internal string? BuildTarget { get; }
    }

    internal static bool TryParseArguments(string[] args, out CliArguments parsed, out string error)
    {
        parsed = default;
        error = string.Empty;

        var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            var trimmed = token.TrimStart('-');
            string value;

            var equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex >= 0)
            {
                value = trimmed[(equalsIndex + 1)..];
                trimmed = trimmed[..equalsIndex];
            }
            else
            {
                if (index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    error = $"Option '{token}' is missing a value.";
                    return false;
                }

                value = args[++index];
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            map[trimmed] = value;
        }

        if (!map.TryGetValue("outputPath", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            error = "Missing required option 'outputPath'.";
            return false;
        }

        if (!map.TryGetValue("buildVersion", out var buildVersion) || string.IsNullOrWhiteSpace(buildVersion))
        {
            error = "Missing required option 'buildVersion'.";
            return false;
        }

        if (!map.TryGetValue("buildProfileName", out var buildProfileName) || string.IsNullOrWhiteSpace(buildProfileName))
        {
            error = "Missing required option 'buildProfileName'.";
            return false;
        }

        if (!map.TryGetValue("buildPurpose", out var buildPurpose) || string.IsNullOrWhiteSpace(buildPurpose))
        {
            error = "Missing required option 'buildPurpose'.";
            return false;
        }

        map.TryGetValue("buildTarget", out var buildTarget);

        parsed = new CliArguments(
            outputPath,
            buildVersion,
            buildProfileName,
            buildPurpose,
            string.IsNullOrWhiteSpace(buildTarget) ? null : buildTarget);
        return true;
    }
    private static void ProceedToBuild(
        string outputPath,
        string buildVersion,
        string buildProfileName,
        string buildPurpose,
        string buildTargetString = null)
    {
        if (string.IsNullOrWhiteSpace(buildVersion))
        {
            Log.LogError("Missing or empty argument: buildVersion");
            throw new Exceptions.BuildFailedException("Missing or empty argument: buildVersion");
        }

        if (string.IsNullOrWhiteSpace(buildProfileName))
        {
            Log.LogError("Missing or empty argument: buildProfileName");
            throw new Exceptions.BuildFailedException("Missing or empty argument: buildProfileName");
        }

        Log.LogDebug("Checking buildPurpose: {BuildPurpose}", buildPurpose);
        if (buildPurpose == null)
        {
            Log.LogError("buildPurpose is null!");
            throw new Exceptions.BuildFailedException("buildPurpose is null!");
        }

        var buildPreparationSettings = Utility.LoadAssetOfType<BuildPreparationSettings>();
        ScriptableObject buildProfile = null;
        BuildTarget target = BuildTarget.NoTarget;

        // First try to use the buildTarget parameter if provided
        if (!string.IsNullOrEmpty(buildTargetString) && TryParseBuildTarget(buildTargetString, out target))
        {
            Log.LogInformation("Using BuildTarget from command line parameter: {BuildTarget}", target);
            buildProfile = CreateMockBuildProfile(target);
        }
        else if (!buildPreparationSettings)
        {
            Log.LogWarning("BuildPreparationSettings asset not found. Using fallback configuration for build profile: " + buildProfileName);

            // Fallback: Create a simple build profile based on the profile name
            if (buildProfileName.Contains("windows", System.StringComparison.OrdinalIgnoreCase))
            {
                target = BuildTarget.StandaloneWindows64;
                Log.LogInformation("Using fallback Windows build target: StandaloneWindows64");
            }
            else if (buildProfileName.Contains("android", System.StringComparison.OrdinalIgnoreCase))
            {
                target = BuildTarget.Android;
                Log.LogInformation("Using fallback Android build target: Android");
            }
            else if (buildProfileName.Contains("ios", System.StringComparison.OrdinalIgnoreCase))
            {
                target = BuildTarget.iOS;
                Log.LogInformation("Using fallback iOS build target: iOS");
            }
            else
            {
                var exception = new Exceptions.BuildFailedException($"Cannot determine build target from profile name '{buildProfileName}' and no BuildPreparationSettings found.");
                Log.LogError(exception.Message);
                throw exception;
            }

            // Create a mock build profile that satisfies the GetBuildTarget method
            buildProfile = CreateMockBuildProfile(target);
        }
        else
        {
            // var crossSettings = buildPreparationSettings.CrossProfileSettings;
            var profile = buildPreparationSettings.ProfileSettings.Profiles
                .FirstOrDefault(x => x.Name.Equals(buildProfileName));
            if (profile == null)
            {
                var exception = new Exceptions.BuildFailedException($"Build profile '{buildProfileName}' not found.");
                Log.LogError(exception.Message);
                throw exception;
            }

            buildProfile = profile.BuildProfile;
            target = GetBuildTarget(buildProfile);
        }

        // For fallback builds, buildProfile might not be a Unity BuildProfile, so handle it gracefully
        UnityEditor.Build.Profile.BuildProfile unityBuildProfile = null;
        if (buildPreparationSettings != null && buildProfile is UnityEditor.Build.Profile.BuildProfile actualBuildProfile)
        {
            unityBuildProfile = actualBuildProfile;
        }
        else
        {
            Log.LogDebug("Using fallback build configuration (no Unity BuildProfile available)");
        }

        Log.LogDebug("Build profile name: {BuildProfileName}", buildProfileName);
        Log.LogDebug("Build profile target: {BuildTarget}", target);
        Log.LogDebug("Build version: {BuildVersion}", buildVersion);
        Log.LogDebug("Build profile: {BuildProfile}", buildProfile);

        var targetAssetBundlePath = Path.GetFullPath( Path.Combine("Assets", "StreamingAssets", "AssetBundles"));
        var sourceAssetBundlePath = Path.GetFullPath(
            Path.Combine("Assets", "..", "Build", target.ToString(), "AssetBundles"));

        Debug.Log($"📋 ProceedToBuild: Build purpose is '{buildPurpose}'");
        Debug.Log($"📋 ProceedToBuild: Final BuildTarget is '{target}'");

        Log.LogDebug("Build accordng to build purpose: {BuildPurpose}", buildPurpose);
        if (buildPurpose.Equals("UnityAddressables", StringComparison.Ordinal))
        {
            Debug.Log("🎮 Calling ProcessToBuildUnityAddressables");
            ProcessToBuildUnityAddressables();
        }
        else if (buildPurpose.Equals("UnityAssetBundles", StringComparison.Ordinal))
        {
            Debug.Log("📦 Calling ProcessToBuildUnityAssetBundles");
            ProcessToBuildUnityAssetBundles(
                target,
                sourceAssetBundlePath);
        }
        else if (buildPurpose.Equals("UnityPlayer", StringComparison.Ordinal))
        {
            Debug.Log("🎮 Calling ProcessToBuildPlayer - This should build the Windows executable!");
            ProcessToBuildPlayer(
                outputPath,
                buildVersion,
                buildProfileName,
                target,
                unityBuildProfile,
                targetAssetBundlePath,
                sourceAssetBundlePath);
        }
        else
        {
            Log.LogWarning("Unknown build purpose: {BuildPurpose}", buildPurpose);
        }

        Log.LogDebug("Build completed");
    }

    /// <summary>
    /// Creates a mock build profile for fallback scenarios when BuildPreparationSettings is not available
    /// </summary>
    private static ScriptableObject CreateMockBuildProfile(BuildTarget target)
    {
        // Create a simple ScriptableObject that can be used with GetBuildTarget
        var mockProfile = ScriptableObject.CreateInstance<MockBuildProfileForFallback>();
        mockProfile.name = $"Mock {target} Profile";
        mockProfile.m_BuildTarget = target;
        return mockProfile;
    }

    /// <summary>
    /// Mock build profile class for fallback scenarios
    /// </summary>
    private class MockBuildProfileForFallback : ScriptableObject
    {
        [SerializeField]
        public BuildTarget m_BuildTarget;
    }

    [Serializable]
    private class BuildPreparationSettings : ScriptableObject
    {
        [SerializeField]
        public ProfileCollection ProfileSettings = new();

        [Serializable]
        public class ProfileCollection
        {
            public ProfileEntry[] Profiles = Array.Empty<ProfileEntry>();
        }

        [Serializable]
        public class ProfileEntry
        {
            public string Name = string.Empty;
            public UnityEditor.Build.Profile.BuildProfile BuildProfile;
        }
    }

    private static bool TryParseBuildTarget(string value, out BuildTarget target)
    {
        return Enum.TryParse(value, ignoreCase: true, out target);
    }

    private static BuildTarget GetBuildTarget(ScriptableObject buildProfile)
    {
        if (!buildProfile)
        {
            return BuildTarget.NoTarget;
        }

        var method = buildProfile.GetType().GetMethod(
            "GetBuildTarget",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);
        if (method != null && method.ReturnType == typeof(BuildTarget))
        {
            return (BuildTarget)method.Invoke(buildProfile, null);
        }

        var field = buildProfile.GetType().GetField(
            "m_BuildTarget",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(BuildTarget))
        {
            return (BuildTarget)field.GetValue(buildProfile);
        }

        return BuildTarget.NoTarget;
    }

    private static class BuildAssetBundleProcessor
    {
        public static void BuildAssetBundle(BuildTarget target)
        {
            Log.LogInformation("BuildAssetBundleProcessor placeholder invoked for {Target}", target);
        }
    }

    private static string GetBuildExtensionForTarget(BuildTarget target, UnityEditor.Build.Profile.BuildProfile buildProfile)
    {
        if (buildProfile != null)
        {
            var method = buildProfile.GetType().GetMethod(
                "GetExecutableExtension",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (method != null && method.ReturnType == typeof(string))
            {
                var result = method.Invoke(buildProfile, null) as string;
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
        }

        return target switch
        {
            BuildTarget.Android => ".apk",
            BuildTarget.StandaloneWindows => ".exe",
            BuildTarget.StandaloneWindows64 => ".exe",
            BuildTarget.iOS => ".ipa",
            _ => ".build"
        };
    }
    private static void ProcessToBuildUnityAddressables()
    {
        Log.LogDebug("ProcessToBuildUnityAddressables");

#if HAS_UNITY_ADDRESSABLES
        AddressableAssetSettings.BuildPlayerContent();
#endif
    }

    private static void ProcessToBuildUnityAssetBundles(
        BuildTarget target,
        string sourceAssetBundlePath)
    {
        Log.LogDebug("ProcessToBuildUnityAssetBundles");
        Log.LogDebug("Going to build asset bundles for target: {Target} to path: {OutputPath}", target, sourceAssetBundlePath);
        BuildAssetBundleProcessor.BuildAssetBundle(target);
    }

    private static void ProcessToBuildPlayer(
        string outputPath,
        string buildVersion,
        string buildProfileName,
        BuildTarget target,
        UnityEditor.Build.Profile.BuildProfile buildProfile,
        string targetAssetBundlePath,
        string sourceAssetBundlePath)
    {
        // Enhanced debug logging to verify method execution and target
        Debug.Log("🎯 ProcessToBuildPlayer method REACHED!");
        Debug.Log($"🎯 Build Target Parameter: {target}");
        Debug.Log($"🎯 Build Profile Name: {buildProfileName}");
        Debug.Log($"🎯 Output Path: {outputPath}");
        Debug.Log($"🎯 Build Version: {buildVersion}");

        Log.LogDebug("ProcessToBuildPlayer");

        // Defensive argument checks
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            Log.LogError("Missing or empty argument: outputPath");
            throw new Exceptions.BuildFailedException("Missing or empty argument: outputPath");
        }

        if (Directory.Exists(targetAssetBundlePath))
        {
            Directory.Delete(targetAssetBundlePath, true);
        }

        if (Directory.Exists(sourceAssetBundlePath))
        {
            FileUtil.CopyFileOrDirectory(sourceAssetBundlePath, targetAssetBundlePath);
        }

        var filePath = string.Empty;
        var extension = GetBuildExtensionForTarget(target, buildProfile);
        if (!string.IsNullOrEmpty(extension))
        {
            filePath = $"{PlayerSettings.productName}{extension}";
        }

        var adjustedOutputPath = Path.Combine(outputPath, filePath);
        Log.LogDebug("Output path: {AdjustedOutputPath}", adjustedOutputPath);
        Log.LogDebug("Build profile name: {BuildProfileName}", buildProfileName);
        Log.LogDebug("Build profile target: {BuildTarget}", target);
        Log.LogDebug("Build version: {BuildVersion}", buildVersion);
        Log.LogDebug("Build profile: {BuildProfile}", buildProfile);

        // Use Unity Build Profile if available, otherwise fall back to standard build
        if (buildProfile != null)
        {
            Log.LogDebug("Using Unity Build Profile for build");

            var buildOptions = new BuildPlayerWithProfileOptions
            {
                buildProfile = buildProfile,
                locationPathName = adjustedOutputPath,
            };

            Log.LogInformation("Starting Unity Build Profile build with options:");
            Log.LogInformation("  - Build Profile: {BuildProfile}", buildProfile.name);
            Log.LogInformation("  - Location Path: {LocationPath}", adjustedOutputPath);

            var result = BuildPipeline.BuildPlayer(buildOptions);

            if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.LogInformation("Build succeeded! Output files: {FileCount}", result.summary.outputPath);
                Log.LogInformation("Build size: {BuildSize} bytes", result.summary.totalSize);
                Log.LogInformation("Build time: {BuildTime}", result.summary.totalTime);
            }
            else
            {
                Log.LogError("Build failed with result: {BuildResult}", result.summary.result);
                Log.LogError("Total errors: {ErrorCount}", result.summary.totalErrors);
                Log.LogError("Total warnings: {WarningCount}", result.summary.totalWarnings);

                foreach (var step in result.steps)
                {
                    if (step.messages.Length > 0)
                    {
                        Log.LogError("Step '{StepName}' messages:", step.name);
                        foreach (var message in step.messages)
                        {
                            Log.LogError("  {MessageType}: {MessageContent}", message.type, message.content);
                        }
                    }
                }

                throw new Exceptions.BuildFailedException($"Unity build failed: {result.summary.result}");
            }
        }
        else
        {
            Log.LogDebug("Using standard build pipeline (no Build Profile available)");

            // Get current scenes to build
            Log.LogInformation("Reading EditorBuildSettings for scene configuration...");
            var editorScenes = EditorBuildSettings.scenes;

            Log.LogInformation("Found {SceneCount} scenes in EditorBuildSettings:", editorScenes.Length);
            foreach (var scene in editorScenes)
            {
                Log.LogInformation("  - Scene: {Path} (Enabled: {Enabled}, GUID: {GUID})", scene.path, scene.enabled, scene.guid);
            }

            var scenes = editorScenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Log.LogWarning("No enabled scenes found in EditorBuildSettings. Searching for scenes in Assets directory...");
                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                scenes = sceneGuids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                Log.LogInformation("Found {SceneCount} scenes via AssetDatabase search:", scenes.Length);
                foreach (var scene in scenes)
                {
                    Log.LogInformation("  - Found scene: {Path}", scene);
                }

                if (scenes.Length == 0)
                {
                    Log.LogError("No scenes found in the project! Cannot proceed with build.");
                    throw new Exceptions.BuildFailedException("No scenes found in the project. Please add at least one scene to Assets/Scenes or configure EditorBuildSettings.");
                }
            }

            // Even if EditorBuildSettings has scenes configured, also search for additional scenes
            // in case EditorBuildSettings is being reset by Unity during compilation
            if (scenes.Length > 0)
            {
                Log.LogInformation("EditorBuildSettings scenes found, but also searching for additional scenes as fallback...");
                var allSceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
                var allScenes = allSceneGuids
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .ToArray();

                Log.LogInformation("Total available scenes in project: {SceneCount}", allScenes.Length);
                foreach (var scene in allScenes)
                {
                    Log.LogInformation("  - Available scene: {Path}", scene);
                }

                // Use the more comprehensive scene list if EditorBuildSettings seems incomplete
                if (allScenes.Length > scenes.Length)
                {
                    Log.LogInformation("Using comprehensive scene list ({AllCount} scenes) instead of EditorBuildSettings ({EditorCount} scenes)", allScenes.Length, scenes.Length);
                    scenes = allScenes;
                }
            }

            Log.LogInformation("Found {SceneCount} scenes to build:", scenes.Length);
            foreach (var scene in scenes)
            {
                Log.LogInformation("  - {ScenePath}", scene);
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = adjustedOutputPath,
                target = target,
                options = BuildOptions.None // Can be modified based on buildProfileName if needed
            };

            Log.LogInformation("Starting standard build pipeline with options:");
            Log.LogInformation("  - Target: {BuildTarget}", target);
            Log.LogInformation("  - Location Path: {LocationPath}", adjustedOutputPath);
            Log.LogInformation("  - Scene Count: {SceneCount}", scenes.Length);
            Log.LogInformation("  - Build Options: {BuildOptions}", buildPlayerOptions.options);

            var result = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Log.LogInformation("Build succeeded!");
                Log.LogInformation("  - Output path: {OutputPath}", result.summary.outputPath);
                Log.LogInformation("  - Build size: {BuildSize} bytes", result.summary.totalSize);
                Log.LogInformation("  - Build time: {BuildTime}", result.summary.totalTime);
                Log.LogInformation("  - Total errors: {ErrorCount}", result.summary.totalErrors);
                Log.LogInformation("  - Total warnings: {WarningCount}", result.summary.totalWarnings);

                // Verify the output file actually exists
                if (File.Exists(adjustedOutputPath))
                {
                    var fileInfo = new FileInfo(adjustedOutputPath);
                    Log.LogInformation("  - Output file confirmed: {FileName} ({FileSize} bytes)", fileInfo.Name, fileInfo.Length);
                }
                else if (Directory.Exists(adjustedOutputPath))
                {
                    var dirInfo = new DirectoryInfo(adjustedOutputPath);
                    var fileCount = dirInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                    Log.LogInformation("  - Output directory confirmed: {DirectoryName} ({FileCount} files)", dirInfo.Name, fileCount);
                }
                else
                {
                    Log.LogWarning("Output path does not exist after successful build: {OutputPath}", adjustedOutputPath);
                }
            }
            else
            {
                Log.LogError("Build failed with result: {BuildResult}", result.summary.result);
                Log.LogError("Total errors: {ErrorCount}", result.summary.totalErrors);
                Log.LogError("Total warnings: {WarningCount}", result.summary.totalWarnings);

                foreach (var step in result.steps)
                {
                    if (step.messages.Length > 0)
                    {
                        Log.LogError("Step '{StepName}' messages:", step.name);
                        foreach (var message in step.messages)
                        {
                            Log.LogError("  {MessageType}: {MessageContent}", message.type, message.content);
                        }
                    }
                }

                throw new Exceptions.BuildFailedException($"Unity build failed: {result.summary.result}");
            }
        }
    }
}
