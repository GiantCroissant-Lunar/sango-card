using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Preparation manifest that defines what to collect into cache (Phase 1).
/// Maps: external sources → cache.
/// </summary>
public class PreparationManifest
{
    /// <summary>
    /// Gets or sets the manifest version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets the unique identifier for this manifest.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the cache directory where items will be stored.
    /// Defaults to "build/preparation/cache" if not specified.
    /// </summary>
    [JsonPropertyName("cacheDirectory")]
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the list of items to collect into cache.
    /// </summary>
    [JsonPropertyName("items")]
    public List<PreparationItem> Items { get; set; } = new();
}

/// <summary>
/// Represents an item to be collected into cache.
/// </summary>
public class PreparationItem
{
    /// <summary>
    /// Gets or sets the source path (absolute or relative to git root).
    /// Can be from any location: external drive, network share, etc.
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name to use in cache directory.
    /// </summary>
    [JsonPropertyName("cacheAs")]
    public string CacheAs { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of item.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "package", "assembly", "asset"
}

/// <summary>
/// Item types for preparation.
/// </summary>
public static class PreparationItemType
{
    public const string Package = "package";
    public const string Assembly = "assembly";
    public const string Asset = "asset";
}
