using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif
using UnityEngine;

namespace SangoCard.Build.Editor.Report;

internal partial class PostprocessBuildWithReport : IPostprocessBuildWithReport
{
    private const string FRAMEWORK_NAME = "FirebaseAppDistribution.framework";

    private const string FIREBASE_PATH = "Assets/Plugins/iOS/Firebase";

    private const string GOOGLE_SERVICE_INFO = "GoogleService-Info.plist";

    private void HandleiOS(BuildReport report)
    {
#if UNITY_IOS
        HandleFirebasePackage(report);
        HandleFacebookPlugin(report);
#endif
    }

#if UNITY_IOS
    private void HandleFirebasePackage(BuildReport report)
    {
        string pathToBuildProject = report.summary.outputPath;
        string projPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string targetGuid = proj.GetUnityMainTargetGuid();
        string frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

        string frameworkSourcePath = Path.Combine(FIREBASE_PATH, FRAMEWORK_NAME);
        string frameworkDestinationPath = Path.Combine(pathToBuildProject, "Frameworks", FRAMEWORK_NAME);
        string googleServicePath = Path.Combine(FIREBASE_PATH, GOOGLE_SERVICE_INFO);
        string googleServiceDestPath = Path.Combine(pathToBuildProject, GOOGLE_SERVICE_INFO);

        // Copy FirebaseAppDistribution.framework
        FileUtil.CopyFileOrDirectoryFollowSymlinks(frameworkSourcePath, frameworkDestinationPath);

        // Add framework to Xcode project
        proj.AddFileToBuild(
            targetGuid,
            proj.AddFile("Frameworks/" + FRAMEWORK_NAME, "Frameworks/" + FRAMEWORK_NAME, PBXSourceTree.Source));

        proj.AddFrameworkToProject(targetGuid, "Security.framework", false);
        proj.AddFrameworkToProject(targetGuid, "SafariServices.framework", false);

        // Embed the framework
        PBXProjectExtensions.AddFileToEmbedFrameworks(proj, frameworkTargetGuid, proj.FindFileGuidByProjectPath("Frameworks/" + FRAMEWORK_NAME));

        // Add -ObjC linker flag
        proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");

        // Add framework search path
        proj.AddBuildProperty(targetGuid, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");

        // Copy GoogleService-Info.plist
        if (File.Exists(googleServicePath))
        {
            FileUtil.CopyFileOrDirectoryFollowSymlinks(googleServicePath, googleServiceDestPath);
            proj.AddFileToBuild(targetGuid, proj.AddFile(GOOGLE_SERVICE_INFO, GOOGLE_SERVICE_INFO));
        }

        proj.WriteToFile(projPath);
    }

    private void HandleFacebookPlugin(BuildReport report)
    {
        // Implement your post-build logic here.
        // For example, you could log the build report or perform additional checks.
        string projPath = PBXProject.GetPBXProjectPath(report.summary.outputPath);
        Debug.Log($"PostprocessBuildWithReport: projPath = {projPath}");

        var project = new PBXProject();
        project.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
        string unityMainTarget = project.GetUnityMainTargetGuid();
        string unityFrameworkTarget = project.GetUnityFrameworkTargetGuid();
#else
        string unityFrameworkTarget = project.TargetGuidByName("UnityFramework");
#endif

        string swiftFolder = "Libraries/FacebookSDK/SDK/Editor/iOS/Swift";
        string[] swiftFiles = new[]
        {
            "FBSDKTournamentUpdater.swift",
            "FBSDKTournament.swift",
            "FBSDKShareTournamentDialog.swift",
            "FBSDKTournamentFetcher.swift"
        };

        foreach (string file in swiftFiles)
        {
            string relativePath = Path.Combine(swiftFolder, file);
            string fullPath = Path.Combine(report.summary.outputPath, relativePath);

            // Delete file from disk if it exists
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Debug.Log($"Deleted file: {relativePath}");
            }

            // Remove from Xcode project even if the file was already deleted
            string fileGuid = project.FindFileGuidByProjectPath(relativePath.Replace("\\", "/"));
            if (!string.IsNullOrEmpty(fileGuid))
            {
                project.RemoveFile(fileGuid);
                project.RemoveFileFromBuild(unityFrameworkTarget, fileGuid);
                Debug.Log($"Removed reference to: {relativePath}");
            }
        }

        project.WriteToFile(projPath);
    }
#endif
}
