using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for JSON files using JSON path.
/// </summary>
public class JsonPatcher : PatcherBase
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatcher"/> class.
    /// </summary>
    public JsonPatcher(ILogger<JsonPatcher> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override PatchType PatchType => PatchType.Json;

    /// <inheritdoc/>
    protected override Task<string> ApplyPatchInternalAsync(string content, CodePatch patch)
    {
        var jsonNode = JsonNode.Parse(content);

        if (jsonNode == null)
        {
            throw new InvalidOperationException("Failed to parse JSON content");
        }

        // Check if patch specifies JSON operation
        bool modified;
        if (!string.IsNullOrEmpty(patch.Operation))
        {
            modified = patch.Operation.ToLowerInvariant() switch
            {
                "addproperty" => ApplyAddProperty(jsonNode, patch),
                "removeproperty" => ApplyRemoveProperty(jsonNode, patch),
                "replacevalue" => ApplyReplaceValue(jsonNode, patch),
                _ => throw new NotSupportedException($"JSON operation '{patch.Operation}' not supported")
            };
        }
        else
        {
            // Fall back to mode-based operations for backward compatibility
            modified = patch.Mode switch
            {
                PatchMode.Replace => ApplyReplace(jsonNode, patch),
                PatchMode.Delete => ApplyDelete(jsonNode, patch),
                PatchMode.InsertAfter => ApplyAddProperty(jsonNode, patch),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported for JSON")
            };
        }

        if (!modified)
        {
            return Task.FromResult(content); // Return original if not modified
        }

        // Convert back to string, preserving formatting
        var updatedJson = jsonNode.ToJsonString(_jsonOptions);
        return Task.FromResult(updatedJson);
    }

    /// <inheritdoc/>
    protected override Task<PatchValidationResult> ValidateInternalAsync(string filePath, string content, CodePatch patch)
    {
        var errors = new List<string>();

        try
        {
            var jsonNode = JsonNode.Parse(content);
            if (jsonNode == null)
            {
                errors.Add("Invalid JSON format");
            }

            return Task.FromResult(new PatchValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                TargetFound = true
            });
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parse error: {ex.Message}");
            return Task.FromResult(new PatchValidationResult
            {
                IsValid = false,
                Errors = errors,
                TargetFound = false
            });
        }
    }

    /// <inheritdoc/>
    protected override Task<PatchValidationResult> ValidatePatchedContentAsync(string filePath, string patchedContent, CodePatch patch)
    {
        var errors = new List<string>();

        try
        {
            var jsonNode = JsonNode.Parse(patchedContent);
            if (jsonNode == null)
            {
                errors.Add("Patched content is not valid JSON");
            }

            return Task.FromResult(new PatchValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                TargetFound = true
            });
        }
        catch (JsonException ex)
        {
            errors.Add($"Patched JSON is invalid: {ex.Message}");
            return Task.FromResult(new PatchValidationResult
            {
                IsValid = false,
                Errors = errors,
                TargetFound = true
            });
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> IsTargetPresentAsync(string content, CodePatch patch)
    {
        try
        {
            var jsonNode = JsonNode.Parse(content);
            if (jsonNode == null)
            {
                return Task.FromResult(false);
            }

            var node = GetNodeByPath(jsonNode, patch.Search);
            return Task.FromResult(node != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private bool ApplyReplace(JsonNode root, CodePatch patch)
    {
        var pathParts = ParseJsonPath(patch.Search);
        if (pathParts.Length == 0)
        {
            return false;
        }

        var parent = NavigateToParent(root, pathParts, out var lastKey);
        if (parent == null || lastKey == null)
        {
            return false;
        }

        // Parse replacement value
        JsonNode? newValue;
        try
        {
            newValue = JsonNode.Parse(patch.Replace);
        }
        catch
        {
            // If not valid JSON, treat as string
            newValue = JsonValue.Create(patch.Replace);
        }

        if (parent is JsonObject obj)
        {
            obj[lastKey] = newValue;
            return true;
        }
        else if (parent is JsonArray arr && int.TryParse(lastKey, out var index))
        {
            if (index >= 0 && index < arr.Count)
            {
                arr[index] = newValue;
                return true;
            }
        }

        return false;
    }

    private bool ApplyDelete(JsonNode root, CodePatch patch)
    {
        var pathParts = ParseJsonPath(patch.Search);
        if (pathParts.Length == 0)
        {
            return false;
        }

        var parent = NavigateToParent(root, pathParts, out var lastKey);
        if (parent == null || lastKey == null)
        {
            return false;
        }

        if (parent is JsonObject obj)
        {
            return obj.Remove(lastKey);
        }
        else if (parent is JsonArray arr && int.TryParse(lastKey, out var index))
        {
            if (index >= 0 && index < arr.Count)
            {
                arr.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    private bool ApplyAddProperty(JsonNode root, CodePatch patch)
    {
        // Parse the path - Search contains the parent path, Replace contains the new property name and value
        // Format: Search="path.to.parent", Replace="propertyName:value" or just the JSON value
        var pathParts = ParseJsonPath(patch.Search);
        
        // Navigate to the parent object
        JsonNode? parent;
        if (pathParts.Length == 0)
        {
            parent = root;
        }
        else
        {
            parent = GetNodeByPath(root, patch.Search);
        }

        if (parent is not JsonObject obj)
        {
            Logger.LogWarning("AddProperty: Target path is not a JSON object: {Path}", patch.Search);
            return false;
        }

        // Parse the replacement to get property name and value
        // Support format: "propertyName:value" or just JSON object
        string propertyName;
        JsonNode? propertyValue;

        if (patch.Replace.Contains(':') && !patch.Replace.TrimStart().StartsWith('{'))
        {
            var parts = patch.Replace.Split(':', 2);
            propertyName = parts[0].Trim();
            var valueStr = parts[1].Trim();

            try
            {
                propertyValue = JsonNode.Parse(valueStr);
            }
            catch
            {
                // If not valid JSON, treat as string
                propertyValue = JsonValue.Create(valueStr);
            }
        }
        else
        {
            // Assume Replace is a JSON object with single property
            try
            {
                var tempObj = JsonNode.Parse(patch.Replace) as JsonObject;
                if (tempObj == null || tempObj.Count != 1)
                {
                    Logger.LogWarning("AddProperty: Replace value must be 'name:value' or single-property JSON object");
                    return false;
                }

                var prop = tempObj.First();
                propertyName = prop.Key;
                propertyValue = prop.Value?.DeepClone();
            }
            catch
            {
                Logger.LogWarning("AddProperty: Invalid replace format: {Replace}", patch.Replace);
                return false;
            }
        }

        obj[propertyName] = propertyValue;
        Logger.LogDebug("Added property '{Property}' to {Path}", propertyName, patch.Search);
        return true;
    }

    private bool ApplyRemoveProperty(JsonNode root, CodePatch patch)
    {
        // Search contains the full path to the property to remove
        var pathParts = ParseJsonPath(patch.Search);
        if (pathParts.Length == 0)
        {
            return false;
        }

        var parent = NavigateToParent(root, pathParts, out var lastKey);
        if (parent is not JsonObject obj || lastKey == null)
        {
            Logger.LogWarning("RemoveProperty: Target path is not a JSON object property: {Path}", patch.Search);
            return false;
        }

        var removed = obj.Remove(lastKey);
        if (removed)
        {
            Logger.LogDebug("Removed property '{Property}' from path", lastKey);
        }
        return removed;
    }

    private bool ApplyReplaceValue(JsonNode root, CodePatch patch)
    {
        // Search contains the path to the property, Replace contains the new value
        var pathParts = ParseJsonPath(patch.Search);
        if (pathParts.Length == 0)
        {
            return false;
        }

        var parent = NavigateToParent(root, pathParts, out var lastKey);
        if (parent == null || lastKey == null)
        {
            return false;
        }

        // Parse replacement value
        JsonNode? newValue;
        try
        {
            newValue = JsonNode.Parse(patch.Replace);
        }
        catch
        {
            // If not valid JSON, treat as string
            newValue = JsonValue.Create(patch.Replace);
        }

        if (parent is JsonObject obj)
        {
            obj[lastKey] = newValue;
            Logger.LogDebug("Replaced value at {Path}", patch.Search);
            return true;
        }
        else if (parent is JsonArray arr && int.TryParse(lastKey, out var index))
        {
            if (index >= 0 && index < arr.Count)
            {
                arr[index] = newValue;
                Logger.LogDebug("Replaced array value at {Path}[{Index}]", patch.Search, index);
                return true;
            }
        }

        return false;
    }

    private JsonNode? GetNodeByPath(JsonNode root, string path)
    {
        var pathParts = ParseJsonPath(path);
        var current = root;

        foreach (var part in pathParts)
        {
            if (current is JsonObject obj)
            {
                current = obj[part];
            }
            else if (current is JsonArray arr && int.TryParse(part, out var index))
            {
                if (index >= 0 && index < arr.Count)
                {
                    current = arr[index];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private JsonNode? NavigateToParent(JsonNode root, string[] pathParts, out string? lastKey)
    {
        lastKey = null;

        if (pathParts.Length == 0)
        {
            return null;
        }

        if (pathParts.Length == 1)
        {
            lastKey = pathParts[0];
            return root;
        }

        var parentPath = pathParts.Take(pathParts.Length - 1).ToArray();
        lastKey = pathParts[^1];

        return GetNodeByPath(root, string.Join(".", parentPath));
    }

    private string[] ParseJsonPath(string path)
    {
        // Simple JSON path parser: "a.b.c" or "a[0].b"
        return path.Split('.', StringSplitOptions.RemoveEmptyEntries)
                   .Select(p => p.Replace("[", "").Replace("]", ""))
                   .ToArray();
    }
}
