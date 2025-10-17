using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SangoCard.Build.Editor.Report;

internal partial class PostprocessBuildWithReport : IPostprocessBuildWithReport
{
#pragma warning disable IDE1006
    public int callbackOrder => 0;
#pragma warning restore IDE1006

    // Example of a method that could be used for post-build processing:
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            HandleiOS(report);
        }
    }
}
