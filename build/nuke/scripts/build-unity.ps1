# Usage: .\build-unified.ps1 [UnityProjectPath] [BuildProfile] [UnityEditorVersion] [UnityExecuteMethod]
# Example: .\build-unified.ps1 projects/client Windows 6000.0.50f1 SangoCard.Build.MEditor.BuildScript.PerformBuild

. $PSScriptRoot/build-params.ps1
. $PSScriptRoot/build-help.ps1

if ($args -contains '-?' -or $args -contains '-Help') {
    Show-BuildHelp -ScriptName "build-unified.ps1"
}

..\build.ps1 `
    --UnityEditorVersion $UnityEditorVersion `
    --UnityExecuteMethod $UnityExecuteMethod `
    --BuildPreparationName $BuildProfile `
    --UnityProjectPath $UnityProjectPath
