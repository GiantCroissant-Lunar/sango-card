using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Base class for patchers with common functionality.
/// </summary>
public abstract class PatcherBase : IPatcher
{
    private readonly ILogger _logger;
    private readonly string _rollbackDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatcherBase"/> class.
    /// </summary>
    protected PatcherBase(ILogger logger)
    {
        _logger = logger;
        _rollbackDirectory = Path.Combine(Path.GetTempPath(), "SangoCard.Build.Rollback");
        Directory.CreateDirectory(_rollbackDirectory);
    }

    /// <inheritdoc/>
    public abstract PatchType PatchType { get; }

    /// <inheritdoc/>
    public async Task<PatchResult> ApplyPatchAsync(string filePath, CodePatch patch, bool dryRun = false)
    {
        _logger.LogDebug("Applying {Type} patch to: {File} (DryRun: {DryRun})", PatchType, filePath, dryRun);

        // Pre-validation hook
        var validation = await ValidatePatchAsync(filePath, patch);
        if (!validation.IsValid)
        {
            return new PatchResult
            {
                Success = false,
                Message = $"Validation failed: {string.Join(", ", validation.Errors)}",
                Modified = false
            };
        }

        try
        {
            // Read current content
            var originalContent = await File.ReadAllTextAsync(filePath);

            // Apply the patch (implementation-specific)
            var patchedContent = await ApplyPatchInternalAsync(originalContent, patch);

            // Check if content changed
            if (patchedContent == originalContent)
            {
                _logger.LogWarning("Patch did not modify content: {File}", filePath);
                return new PatchResult
                {
                    Success = true,
                    Message = "No changes made - target pattern not found or already applied",
                    Modified = false
                };
            }

            // Post-validation hook
            var postValidation = await ValidatePatchedContentAsync(filePath, patchedContent, patch);
            if (!postValidation.IsValid)
            {
                return new PatchResult
                {
                    Success = false,
                    Message = $"Post-validation failed: {string.Join(", ", postValidation.Errors)}",
                    Modified = false
                };
            }

            // Dry-run mode: return preview without modifying file
            if (dryRun)
            {
                var preview = GeneratePreview(originalContent, patchedContent);
                return new PatchResult
                {
                    Success = true,
                    Message = "Dry-run: Patch would be applied successfully",
                    Modified = true,
                    Preview = preview
                };
            }

            // Create rollback point before writing
            var rollbackId = await CreateRollbackPointAsync(filePath);

            // Write patched content
            await File.WriteAllTextAsync(filePath, patchedContent);

            _logger.LogInformation("{Type} patch applied successfully: {File}", PatchType, filePath);
            return new PatchResult
            {
                Success = true,
                Message = "Patch applied successfully",
                RollbackId = rollbackId,
                Modified = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply {Type} patch: {File}", PatchType, filePath);
            return new PatchResult
            {
                Success = false,
                Message = $"Error: {ex.Message}",
                Modified = false
            };
        }
    }

    /// <inheritdoc/>
    public async Task<PatchValidationResult> ValidatePatchAsync(string filePath, CodePatch patch)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // File existence check
        if (!File.Exists(filePath))
        {
            errors.Add($"File not found: {filePath}");
            return new PatchValidationResult
            {
                IsValid = false,
                Errors = errors,
                TargetFound = false
            };
        }

        try
        {
            // Read file content
            var content = await File.ReadAllTextAsync(filePath);

            // Check if target pattern exists
            var targetFound = await IsTargetPresentAsync(content, patch);
            if (!targetFound)
            {
                warnings.Add("Target pattern not found in file");
            }

            // Perform implementation-specific validation
            var customValidation = await ValidateInternalAsync(filePath, content, patch);
            errors.AddRange(customValidation.Errors);
            warnings.AddRange(customValidation.Warnings);

            return new PatchValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                TargetFound = targetFound
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate patch for: {File}", filePath);
            errors.Add($"Validation error: {ex.Message}");
            return new PatchValidationResult
            {
                IsValid = false,
                Errors = errors,
                TargetFound = false
            };
        }
    }

    /// <inheritdoc/>
    public async Task<string> CreateRollbackPointAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Cannot create rollback for non-existent file: {filePath}");
            }

            // Generate unique rollback ID
            var rollbackId = GenerateRollbackId(filePath);
            var rollbackPath = GetRollbackPath(rollbackId);

            // Copy current file to rollback location
            File.Copy(filePath, rollbackPath, overwrite: true);
            await Task.CompletedTask;

            _logger.LogDebug("Created rollback point: {RollbackId} for {File}", rollbackId, filePath);
            return rollbackId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create rollback point for: {File}", filePath);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RollbackAsync(string filePath, string rollbackId)
    {
        try
        {
            var rollbackPath = GetRollbackPath(rollbackId);
            if (!File.Exists(rollbackPath))
            {
                _logger.LogError("Rollback file not found: {RollbackId}", rollbackId);
                return false;
            }

            // Restore file from rollback
            File.Copy(rollbackPath, filePath, overwrite: true);
            await Task.CompletedTask;

            _logger.LogInformation("Rolled back file: {File} using {RollbackId}", filePath, rollbackId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback file: {File} using {RollbackId}", filePath, rollbackId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task CleanupRollbackAsync(string rollbackId)
    {
        try
        {
            var rollbackPath = GetRollbackPath(rollbackId);
            if (File.Exists(rollbackPath))
            {
                await Task.Run(() => File.Delete(rollbackPath));
                _logger.LogDebug("Cleaned up rollback: {RollbackId}", rollbackId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup rollback: {RollbackId}", rollbackId);
        }
    }

    /// <summary>
    /// Applies the patch to the content. Implementation-specific.
    /// </summary>
    /// <param name="content">Original file content.</param>
    /// <param name="patch">The patch to apply.</param>
    /// <returns>Patched content.</returns>
    protected abstract Task<string> ApplyPatchInternalAsync(string content, CodePatch patch);

    /// <summary>
    /// Validates the patch before applying. Implementation-specific.
    /// </summary>
    /// <param name="filePath">File path.</param>
    /// <param name="content">File content.</param>
    /// <param name="patch">The patch to validate.</param>
    /// <returns>Validation result.</returns>
    protected virtual Task<PatchValidationResult> ValidateInternalAsync(string filePath, string content, CodePatch patch)
    {
        // Default: no additional validation
        return Task.FromResult(new PatchValidationResult
        {
            IsValid = true,
            TargetFound = true
        });
    }

    /// <summary>
    /// Validates the patched content. Implementation-specific.
    /// </summary>
    /// <param name="filePath">File path.</param>
    /// <param name="patchedContent">Patched content.</param>
    /// <param name="patch">The applied patch.</param>
    /// <returns>Validation result.</returns>
    protected virtual Task<PatchValidationResult> ValidatePatchedContentAsync(string filePath, string patchedContent, CodePatch patch)
    {
        // Default: no additional validation
        return Task.FromResult(new PatchValidationResult
        {
            IsValid = true,
            TargetFound = true
        });
    }

    /// <summary>
    /// Checks if the target pattern is present in the content.
    /// </summary>
    /// <param name="content">File content.</param>
    /// <param name="patch">The patch.</param>
    /// <returns>True if target is present.</returns>
    protected virtual Task<bool> IsTargetPresentAsync(string content, CodePatch patch)
    {
        return Task.FromResult(content.Contains(patch.Search));
    }

    /// <summary>
    /// Generates a preview of changes.
    /// </summary>
    /// <param name="originalContent">Original content.</param>
    /// <param name="patchedContent">Patched content.</param>
    /// <returns>Preview string.</returns>
    protected virtual string GeneratePreview(string originalContent, string patchedContent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Patch Preview ===");
        sb.AppendLine($"Original length: {originalContent.Length} chars");
        sb.AppendLine($"Patched length: {patchedContent.Length} chars");
        sb.AppendLine($"Difference: {patchedContent.Length - originalContent.Length:+#;-#;0} chars");

        // Simple diff indication
        var originalLines = originalContent.Split('\n');
        var patchedLines = patchedContent.Split('\n');

        if (originalLines.Length != patchedLines.Length)
        {
            sb.AppendLine($"Line count changed: {originalLines.Length} -> {patchedLines.Length}");
        }

        return sb.ToString();
    }

    private string GenerateRollbackId(string filePath)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var fileHash = ComputeHash(filePath);
        return $"{fileHash}_{timestamp}";
    }

    private string GetRollbackPath(string rollbackId)
    {
        return Path.Combine(_rollbackDirectory, $"{rollbackId}.rollback");
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash)[..16]; // Use first 16 chars
    }

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger => _logger;
}
