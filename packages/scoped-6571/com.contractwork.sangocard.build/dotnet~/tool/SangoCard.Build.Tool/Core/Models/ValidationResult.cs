using System.Text.Json.Serialization;

namespace SangoCard.Build.Tool.Core.Models;

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation level that was performed.
    /// </summary>
    [JsonPropertyName("level")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ValidationLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of validation warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation summary message.
    /// </summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// Gets or sets the validation timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the total number of issues (errors + warnings).
    /// </summary>
    [JsonIgnore]
    public int TotalIssues => Errors.Count + Warnings.Count;
}

/// <summary>
/// Validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path related to this error (if applicable).
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }

    /// <summary>
    /// Gets or sets the line number (if applicable).
    /// </summary>
    [JsonPropertyName("line")]
    public int? Line { get; set; }

    /// <summary>
    /// Gets or sets additional context for this error.
    /// </summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; }
}

/// <summary>
/// Validation warning.
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Gets or sets the warning code.
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path related to this warning (if applicable).
    /// </summary>
    [JsonPropertyName("file")]
    public string? File { get; set; }

    /// <summary>
    /// Gets or sets additional context for this warning.
    /// </summary>
    [JsonPropertyName("context")]
    public string? Context { get; set; }
}

/// <summary>
/// Validation levels.
/// </summary>
public enum ValidationLevel
{
    /// <summary>
    /// Level 1: Schema validation only.
    /// </summary>
    Schema,

    /// <summary>
    /// Level 2: Schema + file existence.
    /// </summary>
    FileExistence,

    /// <summary>
    /// Level 3: Schema + files + Unity package validity.
    /// </summary>
    UnityPackages,

    /// <summary>
    /// Level 4: Full validation including code patch applicability.
    /// </summary>
    Full
}
