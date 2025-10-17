using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Represents an asset manipulation operation (copy, move, delete).
/// </summary>
public class AssetManipulation
{
    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    [JsonPropertyName("operation")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AssetOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the source path (relative to git root).
    /// Required for Copy and Move operations.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the target path (relative to git root).
    /// Required for Copy, Move, and Delete operations.
    /// </summary>
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to overwrite existing files.
    /// </summary>
    [JsonPropertyName("overwrite")]
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the description of this manipulation.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Asset manipulation operation types.
/// </summary>
public enum AssetOperation
{
    /// <summary>
    /// Copy file or directory from source to target.
    /// </summary>
    Copy,

    /// <summary>
    /// Move file or directory from source to target.
    /// </summary>
    Move,

    /// <summary>
    /// Delete file or directory at target.
    /// </summary>
    Delete
}
