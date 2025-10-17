function Show-BuildHelp {
    param(
        [string]$ScriptName = "build-unity.ps1"
    )

    Write-Host "Usage: .\$ScriptName [UnityProjectPath] [UnityEditorVersion] [UnityExecuteMethod]"
    Write-Host "  UnityProjectPath: Relative path to Unity project (default: projects/client)"
    Write-Host "  UnityEditorVersion: Unity Editor version (default: 6000.0.50f1)"
    Write-Host "  UnityExecuteMethod: Unity execute method (default: SangoCard.Build.MEditor.BuildScript.PerformBuild)"
    Write-Host "  (BuildProfile is set by the script: Windows, Android, iOS, etc.)"

    exit 0
}
