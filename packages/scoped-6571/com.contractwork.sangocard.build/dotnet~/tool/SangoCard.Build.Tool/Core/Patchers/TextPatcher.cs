using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for plain text files using regex patterns and literal string matching.
/// Supports Replace, InsertBefore, InsertAfter, Delete, and RemoveLine operations.
/// </summary>
public class TextPatcher : PatcherBase
{
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="TextPatcher"/> class.
    /// </summary>
    public TextPatcher(ILogger<TextPatcher> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override PatchType PatchType => PatchType.Text;

    /// <inheritdoc/>
    protected override Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        try
        {
            var result = patch.Mode switch
            {
                PatchMode.Replace => ApplyReplace(content, patch),
                PatchMode.InsertBefore => ApplyInsertBefore(content, patch),
                PatchMode.InsertAfter => ApplyInsertAfter(content, patch),
                PatchMode.Delete => ApplyDelete(content, patch),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported for TextPatcher")
            };

            return Task.FromResult(result);
        }
        catch (RegexMatchTimeoutException ex)
        {
            Logger.LogError(ex, "Regex pattern timed out during patch application: {Pattern}", patch.Search);
            throw new InvalidOperationException($"Regex pattern timed out: {patch.Search}", ex);
        }
        catch (ArgumentException ex)
        {
            Logger.LogError(ex, "Invalid regex pattern: {Pattern}", patch.Search);
            throw new InvalidOperationException($"Invalid regex pattern: {patch.Search}", ex);
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> IsTargetPresentAsync(string content, CodePatch patch)
    {
        try
        {
            if (IsRegexPattern(patch.Search))
            {
                var regex = CreateRegex(patch.Search);
                return Task.FromResult(regex.IsMatch(content));
            }
            else
            {
                return Task.FromResult(content.Contains(patch.Search, StringComparison.Ordinal));
            }
        }
        catch (RegexMatchTimeoutException ex)
        {
            Logger.LogError(ex, "Regex pattern timed out during target check: {Pattern}", patch.Search);
            return Task.FromResult(false);
        }
        catch (ArgumentException ex)
        {
            Logger.LogError(ex, "Invalid regex pattern: {Pattern}", patch.Search);
            return Task.FromResult(false);
        }
    }

    private string ApplyReplace(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = CreateRegex(patch.Search);
            return regex.Replace(content, patch.Replace ?? string.Empty);
        }
        else
        {
            return content.Replace(patch.Search, patch.Replace ?? string.Empty, StringComparison.Ordinal);
        }
    }

    private string ApplyInsertBefore(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = CreateRegex(patch.Search);
            return regex.Replace(content, (patch.Replace ?? string.Empty) + "$0");
        }
        else
        {
            return content.Replace(patch.Search, (patch.Replace ?? string.Empty) + patch.Search, StringComparison.Ordinal);
        }
    }

    private string ApplyInsertAfter(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = CreateRegex(patch.Search);
            return regex.Replace(content, "$0" + (patch.Replace ?? string.Empty));
        }
        else
        {
            return content.Replace(patch.Search, patch.Search + (patch.Replace ?? string.Empty), StringComparison.Ordinal);
        }
    }

    private string ApplyDelete(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = CreateRegex(patch.Search);
            return regex.Replace(content, string.Empty);
        }
        else
        {
            return content.Replace(patch.Search, string.Empty, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Creates a compiled regex with timeout for performance and safety.
    /// </summary>
    private Regex CreateRegex(string pattern)
    {
        return new Regex(
            pattern,
            RegexOptions.Compiled | RegexOptions.Multiline,
            _regexTimeout);
    }

    /// <summary>
    /// Determines if a pattern should be treated as a regex pattern.
    /// Uses a heuristic based on common regex metacharacters.
    /// </summary>
    private bool IsRegexPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        // Check for common regex metacharacters
        return pattern.Contains(".*", StringComparison.Ordinal) ||
               pattern.Contains(".+", StringComparison.Ordinal) ||
               pattern.Contains("\\d", StringComparison.Ordinal) ||
               pattern.Contains("\\w", StringComparison.Ordinal) ||
               pattern.Contains("\\s", StringComparison.Ordinal) ||
               pattern.Contains("\\D", StringComparison.Ordinal) ||
               pattern.Contains("\\W", StringComparison.Ordinal) ||
               pattern.Contains("\\S", StringComparison.Ordinal) ||
               pattern.StartsWith("^", StringComparison.Ordinal) ||
               pattern.EndsWith("$", StringComparison.Ordinal) ||
               pattern.Contains("[\\", StringComparison.Ordinal) ||  // Character class with escape
               pattern.Contains("(", StringComparison.Ordinal) ||
               pattern.Contains("{", StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates a regex pattern without executing it.
    /// </summary>
    public static bool ValidateRegexPattern(string pattern, out string? errorMessage)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            errorMessage = "Pattern cannot be null or empty";
            return false;
        }

        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            errorMessage = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            errorMessage = $"Invalid regex pattern: {ex.Message}";
            return false;
        }
    }
}
