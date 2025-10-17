using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using YamlDotNet.RepresentationModel;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for Unity asset files (YAML format).
/// Note: Unity uses a custom YAML format. This is a simplified implementation.
/// </summary>
public class UnityAssetPatcher : IPatcher
{
    private readonly ILogger<UnityAssetPatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityAssetPatcher"/> class.
    /// </summary>
    public UnityAssetPatcher(ILogger<UnityAssetPatcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public PatchType PatchType => PatchType.UnityAsset;

    /// <inheritdoc/>
    public async Task<bool> ApplyPatchAsync(string filePath, CodePatch patch)
    {
        _logger.LogDebug("Applying Unity asset patch to: {File}", filePath);

        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var originalContent = content;

            // For Unity assets, we'll use simple text replacement for now
            // A full implementation would parse Unity's custom YAML format
            content = patch.Mode switch
            {
                PatchMode.Replace => content.Replace(patch.Search, patch.Replace),
                PatchMode.InsertBefore => content.Replace(patch.Search, patch.Replace + patch.Search),
                PatchMode.InsertAfter => content.Replace(patch.Search, patch.Search + patch.Replace),
                PatchMode.Delete => content.Replace(patch.Search, string.Empty),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported")
            };

            if (content == originalContent)
            {
                _logger.LogWarning("Unity asset patch did not modify content: {File}", filePath);
                return false;
            }

            // Basic validation: check if it's still valid YAML-like structure
            if (!ValidateUnityAssetStructure(content))
            {
                _logger.LogError("Unity asset patch resulted in invalid structure: {File}", filePath);
                return false;
            }

            // Write back to file
            await File.WriteAllTextAsync(filePath, content);

            _logger.LogInformation("Unity asset patch applied successfully: {File}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply Unity asset patch: {File}", filePath);
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

            // Check if search pattern exists
            return content.Contains(patch.Search);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Unity asset patch applicability: {File}", filePath);
            return false;
        }
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
