using System.Text.Json;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Utilities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Service for processing batch manifests.
/// Implements SPEC-BLD-PREP-002 requirement R-BLD-PREP-025.
/// </summary>
public class BatchManifestService
{
    private readonly PathResolver _paths;
    private readonly ILogger<BatchManifestService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public BatchManifestService(PathResolver paths, ILogger<BatchManifestService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>
    /// Loads a batch manifest from file (supports both JSON and YAML).
    /// </summary>
    public async Task<BatchManifest> LoadBatchManifestAsync(string manifestRelativePath)
    {
        var fullPath = _paths.Resolve(manifestRelativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Batch manifest not found: {fullPath}");
        }

        var content = await File.ReadAllTextAsync(fullPath);
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();

        BatchManifest? manifest;

        if (extension == ".yaml" || extension == ".yml")
        {
            _logger.LogInformation("Parsing YAML manifest: {Path}", fullPath);
            manifest = _yamlDeserializer.Deserialize<BatchManifest>(content);
        }
        else if (extension == ".json")
        {
            _logger.LogInformation("Parsing JSON manifest: {Path}", fullPath);
            manifest = JsonSerializer.Deserialize<BatchManifest>(content, _jsonOptions);
        }
        else
        {
            throw new NotSupportedException($"Unsupported manifest format: {extension}. Use .json, .yaml, or .yml");
        }

        if (manifest == null)
        {
            throw new InvalidOperationException($"Failed to deserialize batch manifest: {fullPath}");
        }

        _logger.LogInformation(
            "Loaded batch manifest: {Packages} packages, {Assemblies} assemblies, {Assets} assets",
            manifest.Packages.Count,
            manifest.Assemblies.Count,
            manifest.Assets.Count);

        return manifest;
    }

    /// <summary>
    /// Validates a batch manifest.
    /// </summary>
    public BatchManifestValidationResult ValidateBatchManifest(BatchManifest manifest)
    {
        var result = new BatchManifestValidationResult { IsValid = true };

        // Validate packages
        foreach (var package in manifest.Packages)
        {
            if (string.IsNullOrWhiteSpace(package.Source))
            {
                result.Errors.Add($"Package '{package.Name}': Source path is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(package.Name))
            {
                result.Errors.Add("Package: Name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(package.Version))
            {
                result.Warnings.Add($"Package '{package.Name}': Version not specified");
            }
        }

        // Validate assemblies
        foreach (var assembly in manifest.Assemblies)
        {
            if (string.IsNullOrWhiteSpace(assembly.Source))
            {
                result.Errors.Add($"Assembly '{assembly.Name}': Source path is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(assembly.Name))
            {
                result.Errors.Add("Assembly: Name is required");
                result.IsValid = false;
            }
        }

        // Validate assets
        foreach (var asset in manifest.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Source))
            {
                result.Errors.Add($"Asset '{asset.Name}': Source path is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(asset.Name))
            {
                result.Errors.Add("Asset: Name is required");
                result.IsValid = false;
            }

            if (string.IsNullOrWhiteSpace(asset.Target))
            {
                result.Errors.Add($"Asset '{asset.Name}': Target path is required");
                result.IsValid = false;
            }
        }

        if (result.IsValid)
        {
            _logger.LogInformation("Batch manifest validation passed");
        }
        else
        {
            _logger.LogWarning("Batch manifest validation failed: {ErrorCount} errors", result.Errors.Count);
        }

        return result;
    }
}

/// <summary>
/// Result of batch manifest validation.
/// </summary>
public class BatchManifestValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
