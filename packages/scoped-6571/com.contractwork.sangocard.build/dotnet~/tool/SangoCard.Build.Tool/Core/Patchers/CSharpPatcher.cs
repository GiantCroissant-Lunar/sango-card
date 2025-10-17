using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for C# files using Roslyn.
/// </summary>
public class CSharpPatcher : PatcherBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpPatcher"/> class.
    /// </summary>
    public CSharpPatcher(ILogger<CSharpPatcher> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override PatchType PatchType => PatchType.CSharp;

    /// <inheritdoc/>
    protected override Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        // For simplicity, use text-based replacement for now
        // In a full implementation, we'd use Roslyn's syntax rewriting
        var modifiedCode = patch.Mode switch
        {
            PatchMode.Replace => content.Replace(patch.Search, patch.Replace),
            PatchMode.InsertBefore => content.Replace(patch.Search, patch.Replace + patch.Search),
            PatchMode.InsertAfter => content.Replace(patch.Search, patch.Search + patch.Replace),
            PatchMode.Delete => content.Replace(patch.Search, string.Empty),
            _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
        };

        return Task.FromResult(modifiedCode);
    }

    /// <inheritdoc/>
    protected override async Task<ValidationResult> ValidatePatchedContentAsync(string filePath, string patchedContent, CodePatch patch)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Validate syntax after patching
            var tree = CSharpSyntaxTree.ParseText(patchedContent);
            var diagnostics = tree.GetDiagnostics();
            var syntaxErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (syntaxErrors.Any())
            {
                Logger.LogError("C# patch resulted in syntax errors: {File}", filePath);
                foreach (var error in syntaxErrors)
                {
                    var errorMsg = error.GetMessage();
                    Logger.LogError("  {Error}", errorMsg);
                    errors.Add(errorMsg);
                }
            }

            var syntaxWarnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
            foreach (var warning in syntaxWarnings)
            {
                warnings.Add(warning.GetMessage());
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                TargetFound = true
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to validate patched C# content: {File}", filePath);
            errors.Add($"Validation error: {ex.Message}");
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors,
                TargetFound = true
            };
        }
    }
}
