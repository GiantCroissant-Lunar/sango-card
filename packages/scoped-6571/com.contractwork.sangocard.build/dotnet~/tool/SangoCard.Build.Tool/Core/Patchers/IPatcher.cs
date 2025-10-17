using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Interface for code patchers.
/// </summary>
public interface IPatcher
{
    /// <summary>
    /// Gets the patch type this patcher handles.
    /// </summary>
    PatchType PatchType { get; }

    /// <summary>
    /// Applies a patch to a file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to patch.</param>
    /// <param name="patch">The patch to apply.</param>
    /// <param name="dryRun">If true, validates but does not modify the file.</param>
    /// <returns>Result of the patch operation.</returns>
    Task<PatchResult> ApplyPatchAsync(string filePath, CodePatch patch, bool dryRun = false);

    /// <summary>
    /// Validates if a patch can be applied to a file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="patch">The patch to validate.</param>
    /// <returns>Validation result with details.</returns>
    Task<PatchValidationResult> ValidatePatchAsync(string filePath, CodePatch patch);

    /// <summary>
    /// Creates a rollback point for a file before patching.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <returns>Rollback identifier.</returns>
    Task<string> CreateRollbackPointAsync(string filePath);

    /// <summary>
    /// Rolls back a file to a previous state.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="rollbackId">Rollback identifier returned from CreateRollbackPointAsync.</param>
    /// <returns>True if rollback was successful.</returns>
    Task<bool> RollbackAsync(string filePath, string rollbackId);

    /// <summary>
    /// Cleans up rollback data for a file.
    /// </summary>
    /// <param name="rollbackId">Rollback identifier to clean up.</param>
    Task CleanupRollbackAsync(string rollbackId);
}

/// <summary>
/// Result of a patch operation.
/// </summary>
public record PatchResult
{
    /// <summary>
    /// Gets a value indicating whether the patch was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the result message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the rollback identifier if created.
    /// </summary>
    public string? RollbackId { get; init; }

    /// <summary>
    /// Gets a value indicating whether content was modified.
    /// </summary>
    public bool Modified { get; init; }

    /// <summary>
    /// Gets the preview of changes in dry-run mode.
    /// </summary>
    public string? Preview { get; init; }
}

/// <summary>
/// Result of patch validation.
/// </summary>
public record PatchValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the patch is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation warning messages.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets a value indicating whether the target pattern was found.
    /// </summary>
    public bool TargetFound { get; init; }
}
