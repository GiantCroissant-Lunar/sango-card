using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when preparation starts.
/// </summary>
/// <param name="ConfigPath">The config file used, if any.</param>
public record PreparationStartedMessage(string? ConfigPath);

/// <summary>
/// Message published when preparation completes.
/// </summary>
/// <param name="Copied">Files copied.</param>
/// <param name="Moved">Files moved.</param>
/// <param name="Deleted">Files deleted.</param>
/// <param name="Patched">Files patched.</param>
public record PreparationCompletedMessage(int Copied, int Moved, int Deleted, int Patched);

/// <summary>
/// Message for a file copy operation.
/// </summary>
/// <param name="Source">Source file (relative).</param>
/// <param name="Target">Target file (relative).</param>
public record FileCopiedMessage(string Source, string Target);

/// <summary>
/// Message for a file move operation.
/// </summary>
/// <param name="Source">Source file (relative).</param>
/// <param name="Target">Target file (relative).</param>
public record FileMovedMessage(string Source, string Target);

/// <summary>
/// Message for a file delete operation.
/// </summary>
/// <param name="Path">Deleted file (relative).</param>
public record FileDeletedMessage(string Path);
