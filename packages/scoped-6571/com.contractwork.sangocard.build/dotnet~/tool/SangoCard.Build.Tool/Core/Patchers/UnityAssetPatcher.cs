using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for Unity asset files (YAML format).
/// Handles Unity's custom YAML format including file IDs, tags, and structure.
/// Supports ModifyProperty, AddComponent, and RemoveComponent operations.
/// </summary>
public class UnityAssetPatcher : PatcherBase
{
    private readonly IDeserializer _deserializer;
    private readonly ISerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnityAssetPatcher"/> class.
    /// </summary>
    public UnityAssetPatcher(ILogger<UnityAssetPatcher> logger) : base(logger)
    {
        // Unity YAML uses camelCase for property names
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();
    }

    /// <inheritdoc/>
    public override PatchType PatchType => PatchType.UnityAsset;

    /// <inheritdoc/>
    protected override async Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        // Handle different operations based on patch.Operation
        if (!string.IsNullOrEmpty(patch.Operation))
        {
            return patch.Operation.ToLowerInvariant() switch
            {
                "modifyproperty" => await ModifyPropertyAsync(content, patch),
                "addcomponent" => await AddComponentAsync(content, patch),
                "removecomponent" => await RemoveComponentAsync(content, patch),
                _ => throw new NotSupportedException($"Unity asset operation '{patch.Operation}' is not supported")
            };
        }

        // Fallback to simple text replacement for backward compatibility
        var result = patch.Mode switch
        {
            PatchMode.Replace => content.Replace(patch.Search, patch.Replace, StringComparison.Ordinal),
            PatchMode.InsertBefore => content.Replace(patch.Search, patch.Replace + patch.Search, StringComparison.Ordinal),
            PatchMode.InsertAfter => content.Replace(patch.Search, patch.Search + patch.Replace, StringComparison.Ordinal),
            PatchMode.Delete => content.Replace(patch.Search, string.Empty, StringComparison.Ordinal),
            _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported for UnityAssetPatcher")
        };

        return result;
    }

    /// <summary>
    /// Modifies a property value in the Unity YAML asset.
    /// Search format: "propertyPath" (e.g., "m_Volume", "serializedVersion")
    /// Replace: new value as string
    /// </summary>
    private Task<string> ModifyPropertyAsync(string content, CodePatch patch)
    {
        try
        {
            var lines = content.Split('\n');
            var modified = false;
            var result = new List<string>();

            foreach (var line in lines)
            {
                // Look for property: value pattern
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith(patch.Search + ":", StringComparison.Ordinal))
                {
                    // Preserve indentation
                    var indent = line.Substring(0, line.Length - trimmed.Length);
                    result.Add($"{indent}{patch.Search}: {patch.Replace}");
                    modified = true;
                    Logger.LogDebug("Modified property '{Property}' to '{Value}'", patch.Search, patch.Replace);
                }
                else
                {
                    result.Add(line);
                }
            }

            if (!modified)
            {
                Logger.LogWarning("Property '{Property}' not found in Unity asset", patch.Search);
            }

            return Task.FromResult(string.Join("\n", result));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to modify property '{Property}' in Unity asset", patch.Search);
            throw;
        }
    }

    /// <summary>
    /// Adds a component/section to the Unity YAML asset.
    /// Search: location marker (e.g., "MonoBehaviour:" to add after)
    /// Replace: YAML content to add (properly indented)
    /// </summary>
    private Task<string> AddComponentAsync(string content, CodePatch patch)
    {
        try
        {
            var searchIndex = content.IndexOf(patch.Search, StringComparison.Ordinal);
            if (searchIndex == -1)
            {
                throw new InvalidOperationException($"Location marker '{patch.Search}' not found in Unity asset");
            }

            // Find the end of the line
            var endOfLine = content.IndexOf('\n', searchIndex);
            if (endOfLine == -1)
            {
                endOfLine = content.Length;
            }

            // Insert the new component after the marker line
            var result = content.Insert(endOfLine + 1, patch.Replace + "\n");
            Logger.LogDebug("Added component after '{Marker}'", patch.Search);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to add component to Unity asset");
            throw;
        }
    }

    /// <summary>
    /// Removes a component/section from the Unity YAML asset.
    /// Search: component identifier or section to remove
    /// </summary>
    private Task<string> RemoveComponentAsync(string content, CodePatch patch)
    {
        try
        {
            // For simplicity, use text replacement to remove the component
            // A more sophisticated implementation would parse the YAML structure
            var result = content.Replace(patch.Search, string.Empty, StringComparison.Ordinal);
            Logger.LogDebug("Removed component matching '{Search}'", patch.Search);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove component from Unity asset");
            throw;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> IsTargetPresentAsync(string content, CodePatch patch)
    {
        // Check if the search pattern exists in the content
        return Task.FromResult(content.Contains(patch.Search, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    protected override Task<PatchValidationResult> ValidatePatchedContentAsync(string filePath, string patchedContent, CodePatch patch)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate Unity asset structure
        if (!ValidateUnityAssetStructure(patchedContent, out var structureErrors))
        {
            errors.AddRange(structureErrors);
        }

        // Validate YAML syntax
        if (!ValidateYamlSyntax(patchedContent, out var yamlErrors))
        {
            errors.AddRange(yamlErrors);
        }

        return Task.FromResult(new PatchValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            TargetFound = true
        });
    }

    /// <summary>
    /// Validates the basic Unity asset structure.
    /// </summary>
    private bool ValidateUnityAssetStructure(string content, out List<string> errors)
    {
        errors = new List<string>();

        try
        {
            // Unity assets should start with %YAML header
            if (!content.StartsWith("%YAML", StringComparison.Ordinal))
            {
                errors.Add("Unity asset must start with %YAML header");
                return false;
            }

            // Check for Unity tag
            if (!content.Contains("%TAG !u! tag:unity3d.com", StringComparison.Ordinal))
            {
                errors.Add("Unity asset must contain %TAG declaration");
                return false;
            }

            // Check for document separator
            if (!content.Contains("---", StringComparison.Ordinal))
            {
                errors.Add("Unity asset must contain document separator (---)");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating Unity asset structure");
            errors.Add($"Structure validation error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Validates YAML syntax using YamlDotNet parser.
    /// </summary>
    private bool ValidateYamlSyntax(string content, out List<string> errors)
    {
        errors = new List<string>();

        try
        {
            using var reader = new StringReader(content);
            var yaml = new YamlStream();
            yaml.Load(reader);

            return true;
        }
        catch (YamlException ex)
        {
            Logger.LogError(ex, "YAML syntax validation failed");
            errors.Add($"YAML syntax error at line {ex.Start.Line}, column {ex.Start.Column}: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error during YAML validation");
            errors.Add($"Validation error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts a property value from Unity YAML content.
    /// </summary>
    public static string? ExtractPropertyValue(string content, string propertyName)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith(propertyName + ":", StringComparison.Ordinal))
            {
                var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
                if (colonIndex >= 0 && colonIndex + 1 < trimmed.Length)
                {
                    return trimmed.Substring(colonIndex + 1).Trim();
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a Unity asset has a specific component type.
    /// </summary>
    public static bool HasComponent(string content, string componentType)
    {
        return content.Contains($"--- !u!{componentType}", StringComparison.Ordinal) ||
               content.Contains($"m_Script: {{fileID: 11500000, guid:", StringComparison.Ordinal);
    }
}
