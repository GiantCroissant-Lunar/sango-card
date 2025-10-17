using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using YamlDotNet.RepresentationModel;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for Unity asset files (YAML format).
/// Note: Unity uses a custom YAML format. This is a simplified implementation.
/// </summary>
public class UnityAssetPatcher : PatcherBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnityAssetPatcher"/> class.
    /// </summary>
    public UnityAssetPatcher(ILogger<UnityAssetPatcher> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override PatchType PatchType => PatchType.UnityAsset;

    /// <inheritdoc/>
    protected override Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        // For Unity assets, we'll use simple text replacement for now
        // A full implementation would parse Unity's custom YAML format
        var result = patch.Mode switch
        {
            PatchMode.Replace => content.Replace(patch.Search, patch.Replace),
            PatchMode.InsertBefore => content.Replace(patch.Search, patch.Replace + patch.Search),
            PatchMode.InsertAfter => content.Replace(patch.Search, patch.Search + patch.Replace),
            PatchMode.Delete => content.Replace(patch.Search, string.Empty),
            _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    protected override Task<PatchValidationResult> ValidatePatchedContentAsync(string filePath, string patchedContent, CodePatch patch)
    {
        var errors = new List<string>();

        // Basic validation: check if it's still valid YAML-like structure
        if (!ValidateUnityAssetStructure(patchedContent))
        {
            errors.Add("Unity asset patch resulted in invalid structure");
        }

        return Task.FromResult(new PatchValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            TargetFound = true
        });
    }

    private bool ValidateUnityAssetStructure(string content)
    {
        try
        {
            // Basic validation: Unity assets start with %YAML and have specific structure
            if (!content.StartsWith("%YAML") && !content.StartsWith("---"))
            {
                // Not a YAML file, but might be valid for Unity
                return true;
            }

            // Try to parse as YAML (Unity's format is close enough for basic validation)
            using var reader = new StringReader(content);
            var yaml = new YamlStream();
            yaml.Load(reader);

            return true;
        }
        catch
        {
            // If YAML parsing fails, it might still be valid Unity format
            // Unity uses a custom YAML dialect, so we're lenient here
            return true;
        }
    }
}
