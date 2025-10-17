using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when preparation starts.
/// </summary>
/// <param name="ConfigPath">The config file used, if any.</param>
/// <param name="ClientPath">The client project path.</param>
/// <param name="IsDryRun">Whether this is a dry run.</param>
public record PreparationStartedMessage(string? ConfigPath, string ClientPath, bool IsDryRun);

/// <summary>
/// Message published during preparation progress.
/// </summary>
/// <param name="CurrentStep">Current step number.</param>
/// <param name="TotalSteps">Total steps.</param>
/// <param name="Message">Progress message.</param>
public record PreparationProgressMessage(int CurrentStep, int TotalSteps, string Message);

/// <summary>
/// Message published when preparation completes.
/// </summary>
/// <param name="Copied">Files copied.</param>
/// <param name="Moved">Files moved.</param>
/// <param name="Deleted">Files deleted.</param>
/// <param name="Patched">Files patched.</param>
/// <param name="Duration">Execution duration.</param>
/// <param name="BackupPath">Backup path if created.</param>
public record PreparationCompletedMessage(int Copied, int Moved, int Deleted, int Patched, TimeSpan Duration, string? BackupPath);

/// <summary>
/// Message published when preparation fails.
/// </summary>
/// <param name="Error">Error message.</param>
/// <param name="WasRolledBack">Whether changes were rolled back.</param>
public record PreparationFailedMessage(string Error, bool WasRolledBack);

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
