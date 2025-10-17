using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for C# files using Roslyn.
/// </summary>
public class CSharpPatcher : IPatcher
{
    private readonly ILogger<CSharpPatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpPatcher"/> class.
    /// </summary>
    public CSharpPatcher(ILogger<CSharpPatcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public PatchType PatchType => PatchType.CSharp;

    /// <inheritdoc/>
    public async Task<bool> ApplyPatchAsync(string filePath, CodePatch patch)
    {
        _logger.LogDebug("Applying C# patch to: {File}", filePath);

        try
        {
            var code = await File.ReadAllTextAsync(filePath);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            // For simplicity, use text-based replacement for now
            // In a full implementation, we'd use Roslyn's syntax rewriting
            var modifiedCode = patch.Mode switch
            {
                PatchMode.Replace => code.Replace(patch.Search, patch.Replace),
                PatchMode.InsertBefore => code.Replace(patch.Search, patch.Replace + patch.Search),
                PatchMode.InsertAfter => code.Replace(patch.Search, patch.Search + patch.Replace),
                PatchMode.Delete => code.Replace(patch.Search, string.Empty),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
            };

            if (modifiedCode == code)
            {
                _logger.LogWarning("C# patch did not modify content: {File}", filePath);
                return false;
            }

            // Validate syntax after patching
            var modifiedTree = CSharpSyntaxTree.ParseText(modifiedCode);
            var diagnostics = modifiedTree.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (errors.Any())
            {
                _logger.LogError("C# patch resulted in syntax errors: {File}", filePath);
                foreach (var error in errors)
                {
                    _logger.LogError("  {Error}", error.GetMessage());
                }
                return false;
            }

            // Write back to file
            await File.WriteAllTextAsync(filePath, modifiedCode);

            _logger.LogInformation("C# patch applied successfully: {File}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply C# patch: {File}", filePath);
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

            var code = await File.ReadAllTextAsync(filePath);

            // Check if search pattern exists
            return code.Contains(patch.Search);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate C# patch applicability: {File}", filePath);
            return false;
        }
    }
}
