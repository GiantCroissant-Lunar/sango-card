using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace SangoCard.Build.Editor.Processor;

internal static class BuildPostProcessor
{
    // This class is intentionally left empty.
    // It serves as a placeholder for build post-processing logic.
    // You can implement your post-build logic here if needed.

    [PostProcessBuildAttribute(1)]
    public static void OnPostProcessBuild(
        BuildTarget buildTarget,
        string pathToBuiltProject)
    {
        Debug.Log(pathToBuiltProject);

        var pbxProject = new PBXProject();
        var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

        if (buildTarget == BuildTarget.iOS)
        {
            // Read the contents of the Info.plist file that was generated during the build
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
        }
    }
}
