using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Root configuration for build preparation.
/// All paths are relative to git repository root.
/// </summary>
public class PreparationConfig
{
    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the description of this configuration.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of Unity packages to install.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<UnityPackageReference> Packages { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of plain assemblies (DLLs) to copy.
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
}

/// <summary>
/// Reference to a Unity package (.tgz file).
/// </summary>
public class UnityPackageReference
{
    /// <summary>
    /// Gets or sets the package name (e.g., "com.unity.addressables").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version (e.g., "1.21.2").
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source path in preparation cache (relative to git root).
    /// Example: "build/preparation/cache/com.unity.addressables-1.21.2.tgz"
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target path in Unity project (relative to git root).
    /// Example: "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}

/// <summary>
/// Reference to a plain assembly (DLL).
/// </summary>
public class AssemblyReference
{
    /// <summary>
    /// Gets or sets the assembly name (e.g., "Newtonsoft.Json").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assembly version (e.g., "13.0.1").
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the source path (relative to git root).
    /// Example: "build/preparation/cache/Newtonsoft.Json.dll"
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target path (relative to git root).
    /// Example: "projects/client/Assets/Plugins/Newtonsoft.Json.dll"
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
