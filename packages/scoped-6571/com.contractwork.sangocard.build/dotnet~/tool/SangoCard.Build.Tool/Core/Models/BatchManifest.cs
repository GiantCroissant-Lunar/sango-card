using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Batch manifest for adding multiple items at once.
/// Supports both source collection (Phase 1) and injection mapping (Phase 2).
/// </summary>
public class BatchManifest
{
    /// <summary>
    /// Gets or sets the manifest version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the description of this batch manifest.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of packages to add.
    /// </summary>
    [JsonPropertyName("packages")]
    public List<BatchPackageItem> Packages { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of assemblies to add.
    /// </summary>
    [JsonPropertyName("assemblies")]
    public List<BatchAssemblyItem> Assemblies { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of assets to add.
    /// </summary>
    [JsonPropertyName("assets")]
    public List<BatchAssetItem> Assets { get; set; } = new();
}

/// <summary>
/// Batch item for a Unity package.
/// </summary>
public class BatchPackageItem
{
    /// <summary>
    /// Gets or sets the source path (absolute or relative to git root).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target path in client project (optional).
    /// If not specified, defaults to projects/client/Packages/{name}
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }
}

/// <summary>
/// Batch item for an assembly (DLL).
/// </summary>
public class BatchAssemblyItem
{
    /// <summary>
    /// Gets or sets the source path (absolute or relative to git root).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the assembly version (optional).
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the target path in client project (optional).
    /// If not specified, defaults to projects/client/Assets/Plugins/{name}
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }
}

/// <summary>
/// Batch item for an asset (arbitrary files/folders).
/// </summary>
public class BatchAssetItem
{
    /// <summary>
    /// Gets or sets the source path (absolute or relative to git root).
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the asset name/identifier.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target path in client project.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
