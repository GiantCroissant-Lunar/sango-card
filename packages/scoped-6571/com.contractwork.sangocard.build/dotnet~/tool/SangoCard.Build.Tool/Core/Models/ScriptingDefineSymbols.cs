using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Scripting define symbols configuration.
/// </summary>
public class ScriptingDefineSymbols
{
    /// <summary>
    /// Gets or sets the list of symbols to add.
    /// </summary>
    [JsonPropertyName("add")]
    public List<string> Add { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of symbols to remove.
    /// </summary>
    [JsonPropertyName("remove")]
    public List<string> Remove { get; set; } = new();

    /// <summary>
    /// Gets or sets the target build platform.
    /// If null, applies to all platforms.
    /// </summary>
    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    /// <summary>
    /// Gets or sets whether to clear all existing symbols before adding new ones.
    /// </summary>
    [JsonPropertyName("clearExisting")]
    public bool ClearExisting { get; set; } = false;
}
