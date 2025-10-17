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
    /// <returns>True if patch was applied successfully.</returns>
    Task<bool> ApplyPatchAsync(string filePath, CodePatch patch);

    /// <summary>
    /// Validates if a patch can be applied to a file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="patch">The patch to validate.</param>
    /// <returns>True if patch can be applied.</returns>
    Task<bool> CanApplyPatchAsync(string filePath, CodePatch patch);
}
