using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SangoCard.Build.Editor.Report;

internal class ProcessSceneWithReport : IProcessSceneWithReport
{
#pragma warning disable IDE1006
    public int callbackOrder => 100;
#pragma warning restore IDE1006

    public void OnProcessScene(
        Scene scene,
        BuildReport report)
    {
        Debug.Log("ProcessSceneWithReport.OnProcessScene: " + scene.name);
    }
}
