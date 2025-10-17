param(
    # Relative path to the Unity project under sango-card
    [Parameter(Position=0, HelpMessage="Relative path to Unity project (e.g., projects/client)")]
    [ValidateNotNullOrEmpty()]
    [string]$UnityProjectPath = "projects/client",
    # Build profile name (e.g., Windows, Mac, Android, iOS, etc.)
    [Parameter(Position=1, HelpMessage="Build profile name (e.g., Windows, Android, iOS)")]
    [ValidateNotNullOrEmpty()]
    [string]$BuildProfile = "Windows",
    # Unity Editor version
    [Parameter(Position=2, HelpMessage="Unity Editor version (e.g., 6000.0.50f1)")]
    [ValidateNotNullOrEmpty()]
    [string]$UnityEditorVersion = "6000.0.50f1",
    # Unity execute method
    [Parameter(Position=3, HelpMessage="Unity execute method (e.g., SangoCard.Build.MEditor.BuildScript.PerformBuild)")]
    [ValidateNotNullOrEmpty()]
    [string]$UnityExecuteMethod = "SangoCard.Build.Editor.BuildEntry.PerformBuild"
)
