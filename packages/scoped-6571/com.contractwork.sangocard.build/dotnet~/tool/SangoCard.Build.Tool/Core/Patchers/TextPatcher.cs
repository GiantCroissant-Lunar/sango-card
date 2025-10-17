using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for plain text files using regex.
/// </summary>
public class TextPatcher : IPatcher
{
    private readonly ILogger<TextPatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPatcher"/> class.
    /// </summary>
    public TextPatcher(ILogger<TextPatcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public PatchType PatchType => PatchType.Text;

    /// <inheritdoc/>
    public async Task<bool> ApplyPatchAsync(string filePath, CodePatch patch)
    {
        _logger.LogDebug("Applying text patch to: {File}", filePath);

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var originalContent = content;

            // Apply patch based on mode
            content = patch.Mode switch
            {
                PatchMode.Replace => ApplyReplace(content, patch),
                PatchMode.InsertBefore => ApplyInsertBefore(content, patch),
                PatchMode.InsertAfter => ApplyInsertAfter(content, patch),
                PatchMode.Delete => ApplyDelete(content, patch),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
            };

            // Check if content changed
            if (content == originalContent)
            {
                _logger.LogWarning("Patch did not modify content: {File}", filePath);
                return false;
            }

            // Write back to file
            await File.WriteAllTextAsync(filePath, content);

            _logger.LogInformation("Text patch applied successfully: {File}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply text patch: {File}", filePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CanApplyPatchAsync(string filePath, CodePatch patch)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var content = await File.ReadAllTextAsync(filePath);

            // Check if search pattern exists in content
            if (IsRegexPattern(patch.Search))
            {
                var regex = new Regex(patch.Search);
                return regex.IsMatch(content);
            }
            else
            {
                return content.Contains(patch.Search);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate patch applicability: {File}", filePath);
            return false;
        }
    }

    private string ApplyReplace(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = new Regex(patch.Search);
            return regex.Replace(content, patch.Replace);
        }
        else
        {
            return content.Replace(patch.Search, patch.Replace);
        }
    }

    private string ApplyInsertBefore(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = new Regex(patch.Search);
            return regex.Replace(content, patch.Replace + "$0");
        }
        else
        {
            return content.Replace(patch.Search, patch.Replace + patch.Search);
        }
    }

    private string ApplyInsertAfter(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = new Regex(patch.Search);
            return regex.Replace(content, "$0" + patch.Replace);
        }
        else
        {
            return content.Replace(patch.Search, patch.Search + patch.Replace);
        }
    }

    private string ApplyDelete(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = new Regex(patch.Search);
            return regex.Replace(content, string.Empty);
        }
        else
        {
            return content.Replace(patch.Search, string.Empty);
        }
    }

    private bool IsRegexPattern(string pattern)
    {
        // Simple heuristic: if it contains regex special chars, treat as regex
        return pattern.Contains(".*") || 
               pattern.Contains("\\") || 
               pattern.Contains("^") || 
               pattern.Contains("$") ||
               pattern.Contains("[") ||
               pattern.Contains("(");
    }
}
