using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Represents an item in the preparation cache.
/// </summary>
public class CacheItem
{
    /// <summary>
    /// Gets or sets the item type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CacheItemType Type { get; set; }

    /// <summary>
    /// Gets or sets the item name (e.g., package name or assembly name).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the file path in cache (relative to git root).
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the SHA256 hash of the file.
    /// </summary>
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    /// <summary>
    /// Gets or sets the date when this item was added to cache.
    /// </summary>
    [JsonPropertyName("addedDate")]
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the source location where this item was obtained from.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>
/// Cache item types.
/// </summary>
public enum CacheItemType
{
    /// <summary>
    /// Unity package (.tgz file).
    /// </summary>
    UnityPackage,

    /// <summary>
    /// Plain assembly (.dll file).
    /// </summary>
    Assembly,

    /// <summary>
    /// Other file type.
    /// </summary>
    Other
}
