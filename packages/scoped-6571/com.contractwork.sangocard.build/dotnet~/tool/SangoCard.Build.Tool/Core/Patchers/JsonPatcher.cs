using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Core.Patchers;

/// <summary>
/// Patcher for JSON files using JSON path.
/// </summary>
public class JsonPatcher : IPatcher
{
    private readonly ILogger<JsonPatcher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatcher"/> class.
    /// </summary>
    public JsonPatcher(ILogger<JsonPatcher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public PatchType PatchType => PatchType.Json;

    /// <inheritdoc/>
    public async Task<bool> ApplyPatchAsync(string filePath, CodePatch patch)
    {
        _logger.LogDebug("Applying JSON patch to: {File}", filePath);

        try
        {
            var jsonText = await File.ReadAllTextAsync(filePath);
            var jsonNode = JsonNode.Parse(jsonText);

            if (jsonNode == null)
            {
                _logger.LogError("Failed to parse JSON: {File}", filePath);
                return false;
            }

            // Apply patch based on mode
            var modified = patch.Mode switch
            {
                PatchMode.Replace => ApplyReplace(jsonNode, patch),
                PatchMode.Delete => ApplyDelete(jsonNode, patch),
                _ => throw new NotSupportedException($"Patch mode {patch.Mode} not supported for JSON")
            };

            if (!modified)
            {
                _logger.LogWarning("JSON patch did not modify content: {File}", filePath);
                return false;
            }

            // Write back to file
            var updatedJson = jsonNode.ToJsonString(_jsonOptions);
            await File.WriteAllTextAsync(filePath, updatedJson);

            _logger.LogInformation("JSON patch applied successfully: {File}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply JSON patch: {File}", filePath);
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

            var jsonText = await File.ReadAllTextAsync(filePath);
            var jsonNode = JsonNode.Parse(jsonText);

            if (jsonNode == null)
            {
                return false;
            }

            // Check if the path exists
            var node = GetNodeByPath(jsonNode, patch.Search);
            return node != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate JSON patch applicability: {File}", filePath);
            return false;
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
