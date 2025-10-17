using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for C# files using Roslyn syntax tree manipulation.
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
    protected override async Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        // Check if patch specifies Roslyn operation or fall back to text-based
        if (!string.IsNullOrEmpty(patch.Operation))
        {
            return await ApplyRoslynOperationAsync(content, patch);
        }

        // Fall back to simple text-based replacement for backward compatibility
        var modifiedCode = patch.Mode switch
        {
            PatchMode.Replace => content.Replace(patch.Search, patch.Replace),
            PatchMode.InsertBefore => content.Replace(patch.Search, patch.Replace + patch.Search),
            PatchMode.InsertAfter => content.Replace(patch.Search, patch.Search + patch.Replace),
            PatchMode.Delete => content.Replace(patch.Search, string.Empty),
            _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
        };

        return modifiedCode;
    }

    private async Task<string> ApplyRoslynOperationAsync(string content, CodePatch patch)
    {
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = await tree.GetRootAsync();

        var newRoot = patch.Operation?.ToLowerInvariant() switch
        {
            "removeusing" => RemoveUsing(root, patch.Search),
            "replaceexpression" => ReplaceExpression(root, patch.Search, patch.Replace),
            "replaceblock" => ReplaceBlock(root, patch.Search, patch.Replace),
            "removeblock" => RemoveBlock(root, patch.Search),
            _ => throw new NotSupportedException($"Roslyn operation '{patch.Operation}' not supported")
        };

        // Preserve formatting by using original trivia where possible
        return newRoot.ToFullString();
    }

    private SyntaxNode RemoveUsing(SyntaxNode root, string usingName)
    {
        var usingDirectives = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(u => u.Name?.ToString() == usingName)
            .ToList();

        if (!usingDirectives.Any())
        {
            Logger.LogWarning("Using directive not found: {Using}", usingName);
            return root;
        }

        var rewriter = new UsingRemovalRewriter(usingDirectives);
        return rewriter.Visit(root)!;
    }

    private SyntaxNode ReplaceExpression(SyntaxNode root, string searchPattern, string replacement)
    {
        // Find expression nodes that match the search pattern
        var expressions = root.DescendantNodes()
            .OfType<ExpressionSyntax>()
            .Where(e => e.ToString().Trim() == searchPattern.Trim())
            .ToList();

        if (!expressions.Any())
        {
            Logger.LogWarning("Expression not found: {Expression}", searchPattern);
            return root;
        }

        // Parse replacement as expression
        var replacementExpr = SyntaxFactory.ParseExpression(replacement);

        var rewriter = new ExpressionReplacementRewriter(expressions, replacementExpr);
        return rewriter.Visit(root)!;
    }

    private SyntaxNode ReplaceBlock(SyntaxNode root, string searchPattern, string replacement)
    {
        // Find block statements that contain the search pattern
        var blocks = root.DescendantNodes()
            .OfType<BlockSyntax>()
            .Where(b => b.ToString().Contains(searchPattern))
            .ToList();

        if (!blocks.Any())
        {
            Logger.LogWarning("Block not found containing: {Pattern}", searchPattern);
            return root;
        }

        // Parse replacement as block
        var replacementBlock = SyntaxFactory.ParseStatement(replacement) as BlockSyntax;
        if (replacementBlock == null)
        {
            // If not a block, wrap in block
            var stmt = SyntaxFactory.ParseStatement(replacement);
            replacementBlock = SyntaxFactory.Block(stmt);
        }

        var rewriter = new BlockReplacementRewriter(blocks, replacementBlock);
        return rewriter.Visit(root)!;
    }

    private SyntaxNode RemoveBlock(SyntaxNode root, string searchPattern)
    {
        // Find blocks that match the search pattern
        var blocks = root.DescendantNodes()
            .OfType<BlockSyntax>()
            .Where(b => b.ToString().Contains(searchPattern))
            .ToList();

        if (!blocks.Any())
        {
            Logger.LogWarning("Block not found containing: {Pattern}", searchPattern);
            return root;
        }

        var rewriter = new BlockRemovalRewriter(blocks);
        return rewriter.Visit(root)!;
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

/// <summary>
/// Syntax rewriter for removing using directives.
/// </summary>
internal class UsingRemovalRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<UsingDirectiveSyntax> _toRemove;

    public UsingRemovalRewriter(IEnumerable<UsingDirectiveSyntax> toRemove)
    {
        _toRemove = toRemove.ToHashSet();
    }

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (_toRemove.Contains(node))
        {
            // Remove the using directive but preserve trailing trivia
            return null;
        }

        return base.VisitUsingDirective(node);
    }
}

/// <summary>
/// Syntax rewriter for replacing expressions.
/// </summary>
internal class ExpressionReplacementRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<ExpressionSyntax> _toReplace;
    private readonly ExpressionSyntax _replacement;

    public ExpressionReplacementRewriter(IEnumerable<ExpressionSyntax> toReplace, ExpressionSyntax replacement)
    {
        _toReplace = toReplace.ToHashSet();
        _replacement = replacement;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (_toReplace.Contains(node))
        {
            // Preserve leading and trailing trivia
            return _replacement.WithTriviaFrom(node);
        }

        return base.VisitIdentifierName(node);
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (_toReplace.Contains(node))
        {
            return _replacement.WithTriviaFrom(node);
        }

        return base.VisitMemberAccessExpression(node);
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (_toReplace.Contains(node))
        {
            return _replacement.WithTriviaFrom(node);
        }

        return base.VisitInvocationExpression(node);
    }

    public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        if (_toReplace.Contains(node))
        {
            return _replacement.WithTriviaFrom(node);
        }

        return base.VisitLiteralExpression(node);
    }
}

/// <summary>
/// Syntax rewriter for replacing blocks.
/// </summary>
internal class BlockReplacementRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<BlockSyntax> _toReplace;
    private readonly BlockSyntax _replacement;

    public BlockReplacementRewriter(IEnumerable<BlockSyntax> toReplace, BlockSyntax replacement)
    {
        _toReplace = toReplace.ToHashSet();
        _replacement = replacement;
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        if (_toReplace.Contains(node))
        {
            // Preserve leading and trailing trivia
            return _replacement.WithTriviaFrom(node);
        }

        return base.VisitBlock(node);
    }
}

/// <summary>
/// Syntax rewriter for removing blocks.
/// </summary>
internal class BlockRemovalRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<BlockSyntax> _toRemove;

    public BlockRemovalRewriter(IEnumerable<BlockSyntax> toRemove)
    {
        _toRemove = toRemove.ToHashSet();
    }

    public override SyntaxNode? VisitBlock(BlockSyntax node)
    {
        if (_toRemove.Contains(node))
        {
            // Remove the block entirely
            return null;
        }

        return base.VisitBlock(node);
    }
}

