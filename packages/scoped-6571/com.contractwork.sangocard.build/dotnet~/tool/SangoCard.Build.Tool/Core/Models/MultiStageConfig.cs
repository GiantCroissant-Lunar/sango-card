using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Multi-stage preparation configuration (v2.0).
/// Supports sequential injection stages throughout the build lifecycle.
/// </summary>
public class MultiStageConfig
{
    /// <summary>
    /// Gets or sets the configuration version (2.x).
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the description of this configuration.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the path to v1.0 preparation.json for cache population.
    /// </summary>
    [JsonPropertyName("cacheSource")]
    public string? CacheSource { get; set; }

    /// <summary>
    /// Gets or sets the array of sequential injection stages.
    /// </summary>
    [JsonPropertyName("injectionStages")]
    public List<InjectionStage> InjectionStages { get; set; } = new();
}

/// <summary>
/// Represents a single injection stage in the multi-stage build workflow.
/// </summary>
public class InjectionStage
{
    /// <summary>
    /// Gets or sets the stage name (e.g., preTest, preBuild, postBuild).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this stage is active.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the description of this stage.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically cleanup after this stage completes.
    /// </summary>
    [JsonPropertyName("cleanupAfter")]
    public bool CleanupAfter { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of Unity packages to inject.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<UnityPackageReference> Packages { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of assemblies to inject.
    /// </summary>
    [JsonPropertyName("assemblies")]
    public List<AssemblyReference> Assemblies { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of asset manipulations to perform.
    /// </summary>
    [JsonPropertyName("assetManipulations")]
    public List<AssetManipulation> AssetManipulations { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of code patches to apply.
    /// </summary>
    [JsonPropertyName("codePatches")]
    public List<CodePatch> CodePatches { get; set; } = new();

    /// <summary>
    /// Gets or sets the scripting define symbols to set.
    /// </summary>
    [JsonPropertyName("scriptingDefineSymbols")]
    public ScriptingDefineSymbols? ScriptingDefineSymbols { get; set; }

    /// <summary>
    /// Gets or sets platform-specific configurations (for preNativeBuild stage).
    /// </summary>
    [JsonPropertyName("platforms")]
    public Dictionary<string, PlatformConfig>? Platforms { get; set; }
}

/// <summary>
/// Platform-specific configuration for native build stages.
/// </summary>
public class PlatformConfig
{
    /// <summary>
    /// Gets or sets whether this platform configuration is active.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of files to copy.
    /// </summary>
    [JsonPropertyName("files")]
    public List<FileOperation> Files { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of code patches to apply.
    /// </summary>
    [JsonPropertyName("codePatches")]
    public List<CodePatch> CodePatches { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of commands to execute (e.g., pod install).
    /// </summary>
    [JsonPropertyName("commands")]
    public List<string> Commands { get; set; } = new();
}

/// <summary>
/// Simple file copy operation.
/// </summary>
public class FileOperation
{
    /// <summary>
    /// Gets or sets the source file path (relative to git root).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target file path (relative to git root).
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
