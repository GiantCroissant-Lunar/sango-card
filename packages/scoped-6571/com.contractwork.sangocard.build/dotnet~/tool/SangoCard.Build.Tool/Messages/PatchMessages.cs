using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when a patch is applied.
/// </summary>
/// <param name="FilePath">File that was patched.</param>
/// <param name="Patch">The patch that was applied.</param>
public record PatchAppliedMessage(string FilePath, CodePatch Patch);

/// <summary>
/// Message published when a patch fails.
/// </summary>
/// <param name="FilePath">File that failed to patch.</param>
/// <param name="Patch">The patch that failed.</param>
/// <param name="Error">Error message.</param>
public record PatchFailedMessage(string FilePath, CodePatch Patch, string Error);

/// <summary>
/// Message published when a patch is skipped (optional and target not found).
/// </summary>
/// <param name="FilePath">File that was skipped.</param>
/// <param name="Patch">The patch that was skipped.</param>
public record PatchSkippedMessage(string FilePath, CodePatch Patch);
