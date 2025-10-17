using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Represents a code patch operation.
/// </summary>
public class CodePatch
{
    /// <summary>
    /// Gets or sets the target file path (relative to git root).
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the patch type.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PatchType Type { get; set; }

    /// <summary>
    /// Gets or sets the search pattern or selector.
    /// - For C#: Method/class name or Roslyn selector
    /// - For JSON: JSON path
    /// - For Unity: YAML path
    /// - For Text: Regex pattern
    /// </summary>
    [JsonPropertyName("search")]
    public string Search { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replacement content.
    /// </summary>
    [JsonPropertyName("replace")]
    public string Replace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the patch mode.
    /// </summary>
    [JsonPropertyName("mode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PatchMode Mode { get; set; } = PatchMode.Replace;

    /// <summary>
    /// Gets or sets whether this patch is optional (won't fail if target not found).
    /// </summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;

    /// <summary>
    /// Gets or sets the description of this patch.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Code patch types based on file type.
/// </summary>
public enum PatchType
{
    /// <summary>
    /// C# code patch using Roslyn.
    /// </summary>
    CSharp,

    /// <summary>
    /// JSON patch using System.Text.Json.
    /// </summary>
    Json,

    /// <summary>
    /// Unity asset patch (YAML).
    /// </summary>
    UnityAsset,

    /// <summary>
    /// Plain text patch using regex.
    /// </summary>
    Text
}

/// <summary>
/// Patch operation modes.
/// </summary>
public enum PatchMode
{
    /// <summary>
    /// Replace the matched content.
    /// </summary>
    Replace,

    /// <summary>
    /// Insert before the matched content.
    /// </summary>
    InsertBefore,

    /// <summary>
    /// Insert after the matched content.
    /// </summary>
    InsertAfter,

    /// <summary>
    /// Delete the matched content.
    /// </summary>
    Delete
}
