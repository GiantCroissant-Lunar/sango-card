using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for plain text files using regex.
/// </summary>
public class TextPatcher : PatcherBase
{
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
        var result = patch.Mode switch
        {
            PatchMode.Replace => ApplyReplace(content, patch),
            PatchMode.InsertBefore => ApplyInsertBefore(content, patch),
            PatchMode.InsertAfter => ApplyInsertAfter(content, patch),
            PatchMode.Delete => ApplyDelete(content, patch),
            _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    protected override Task<bool> IsTargetPresentAsync(string content, CodePatch patch)
    {
        if (IsRegexPattern(patch.Search))
        {
            var regex = new Regex(patch.Search);
            return Task.FromResult(regex.IsMatch(content));
        }
        else
        {
            return Task.FromResult(content.Contains(patch.Search));
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
