using System.Text.Json;
using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Service for validating preparation configurations.
/// Implements 4-level validation: Schema, FileExistence, UnityPackages, Full.
/// </summary>
public class ValidationService
{
    private readonly PathResolver _pathResolver;
    private readonly ILogger<ValidationService> _logger;
    private readonly IPublisher<ValidationStartedMessage> _validationStartedPublisher;
    private readonly IPublisher<ValidationCompletedMessage> _validationCompletedPublisher;
    private readonly IPublisher<ValidationErrorFoundMessage> _validationErrorFoundPublisher;
    private readonly IPublisher<ValidationWarningFoundMessage> _validationWarningFoundPublisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationService"/> class.
    /// </summary>
    public ValidationService(
        PathResolver pathResolver,
        ILogger<ValidationService> logger,
        IPublisher<ValidationStartedMessage> validationStartedPublisher,
        IPublisher<ValidationCompletedMessage> validationCompletedPublisher,
        IPublisher<ValidationErrorFoundMessage> validationErrorFoundPublisher,
        IPublisher<ValidationWarningFoundMessage> validationWarningFoundPublisher)
    {
        _pathResolver = pathResolver;
        _logger = logger;
        _validationStartedPublisher = validationStartedPublisher;
        _validationCompletedPublisher = validationCompletedPublisher;
        _validationErrorFoundPublisher = validationErrorFoundPublisher;
        _validationWarningFoundPublisher = validationWarningFoundPublisher;
    }

    /// <summary>
    /// Validates a configuration at the specified level.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <param name="level">Validation level.</param>
    /// <returns>Validation result.</returns>
    public virtual ValidationResult Validate(PreparationConfig config, ValidationLevel level)
    {
        _logger.LogInformation("Starting validation at level: {Level}", level);
        _validationStartedPublisher.Publish(new ValidationStartedMessage(level));

        var result = new ValidationResult
        {
            Level = level,
            IsValid = true
        };

        // Level 1: Schema validation
        ValidateSchema(config, result);

        if (level == ValidationLevel.Schema)
        {
            return FinalizeResult(result);
        }

        // Level 2: File existence validation
        if (level >= ValidationLevel.FileExistence)
        {
            ValidateFileExistence(config, result);
        }

        if (level == ValidationLevel.FileExistence)
        {
            return FinalizeResult(result);
        }

        // Level 3: Unity package validity
        if (level >= ValidationLevel.UnityPackages)
        {
            ValidateUnityPackages(config, result);
        }

        if (level == ValidationLevel.UnityPackages)
        {
            return FinalizeResult(result);
        }

        // Level 4: Full validation (includes code patch applicability)
        if (level == ValidationLevel.Full)
        {
            ValidateCodePatches(config, result);
        }

        return FinalizeResult(result);
    }

    /// <summary>
    /// Level 1: Validates configuration schema and required fields.
    /// </summary>
    private void ValidateSchema(PreparationConfig config, ValidationResult result)
    {
        _logger.LogDebug("Validating schema...");

        // Validate version
        if (string.IsNullOrWhiteSpace(config.Version))
        {
            AddError(result, "SCHEMA001", "Configuration version is required");
        }

        // Validate packages
        foreach (var package in config.Packages)
        {
            if (string.IsNullOrWhiteSpace(package.Name))
            {
                AddError(result, "SCHEMA002", "Package name is required");
            }

            if (string.IsNullOrWhiteSpace(package.Version))
            {
                AddError(result, "SCHEMA003", $"Package version is required for: {package.Name}");
            }

            if (string.IsNullOrWhiteSpace(package.Source))
            {
                AddError(result, "SCHEMA004", $"Package source path is required for: {package.Name}");
            }

            if (string.IsNullOrWhiteSpace(package.Target))
            {
                AddError(result, "SCHEMA005", $"Package target path is required for: {package.Name}");
            }
        }

        // Validate assemblies
        foreach (var assembly in config.Assemblies)
        {
            if (string.IsNullOrWhiteSpace(assembly.Name))
            {
                AddError(result, "SCHEMA006", "Assembly name is required");
            }

            if (string.IsNullOrWhiteSpace(assembly.Source))
            {
                AddError(result, "SCHEMA007", $"Assembly source path is required for: {assembly.Name}");
            }

            if (string.IsNullOrWhiteSpace(assembly.Target))
            {
                AddError(result, "SCHEMA008", $"Assembly target path is required for: {assembly.Name}");
            }
        }

        // Validate code patches
        foreach (var patch in config.CodePatches)
        {
            if (string.IsNullOrWhiteSpace(patch.File))
            {
                AddError(result, "SCHEMA009", "Code patch file path is required");
            }

            if (string.IsNullOrWhiteSpace(patch.Search))
            {
                AddError(result, "SCHEMA010", $"Code patch search pattern is required for: {patch.File}");
            }
        }

        _logger.LogDebug("Schema validation complete: {ErrorCount} errors", result.Errors.Count);
    }

    /// <summary>
    /// Level 2: Validates that all referenced files exist.
    /// </summary>
    private void ValidateFileExistence(PreparationConfig config, ValidationResult result)
    {
        _logger.LogDebug("Validating file existence...");

        // Validate package source files
        foreach (var package in config.Packages)
        {
            if (!string.IsNullOrWhiteSpace(package.Source))
            {
                if (!_pathResolver.FileExists(package.Source))
                {
                    AddError(
                        result,
                        "FILE001",
                        $"Package source file not found: {package.Source}",
                        package.Source
                    );
                }
            }
        }

        // Validate assembly source files
        foreach (var assembly in config.Assemblies)
        {
            if (!string.IsNullOrWhiteSpace(assembly.Source))
            {
                if (!_pathResolver.FileExists(assembly.Source))
                {
                    AddError(
                        result,
                        "FILE002",
                        $"Assembly source file not found: {assembly.Source}",
                        assembly.Source
                    );
                }
            }
        }

        // Validate code patch target files
        foreach (var patch in config.CodePatches)
        {
            if (!string.IsNullOrWhiteSpace(patch.File))
            {
                if (!_pathResolver.FileExists(patch.File))
                {
                    if (patch.Optional)
                    {
                        AddWarning(
                            result,
                            "FILE003",
                            $"Optional patch target file not found: {patch.File}",
                            patch.File
                        );
                    }
                    else
                    {
                        AddError(
                            result,
                            "FILE004",
                            $"Patch target file not found: {patch.File}",
                            patch.File
                        );
                    }
                }
            }
        }

        _logger.LogDebug("File existence validation complete: {ErrorCount} errors, {WarningCount} warnings",
            result.Errors.Count, result.Warnings.Count);
    }

    /// <summary>
    /// Level 3: Validates Unity package files (.tgz format and structure).
    /// </summary>
    private void ValidateUnityPackages(PreparationConfig config, ValidationResult result)
    {
        _logger.LogDebug("Validating Unity packages...");

        foreach (var package in config.Packages)
        {
            if (string.IsNullOrWhiteSpace(package.Source))
            {
                continue;
            }

            if (!_pathResolver.FileExists(package.Source))
            {
                continue; // Already reported in file existence validation
            }

            var absolutePath = _pathResolver.Resolve(package.Source);

            // Validate file extension
            if (!absolutePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
            {
                AddError(
                    result,
                    "PKG001",
                    $"Unity package must be a .tgz file: {package.Source}",
                    package.Source
                );
                continue;
            }

            // Validate file size (should be > 0)
            var fileInfo = new FileInfo(absolutePath);
            if (fileInfo.Length == 0)
            {
                AddError(
                    result,
                    "PKG002",
                    $"Unity package file is empty: {package.Source}",
                    package.Source
                );
            }

            // Validate package naming convention
            var fileName = Path.GetFileNameWithoutExtension(absolutePath);
            if (!fileName.Contains('-'))
            {
                AddWarning(
                    result,
                    "PKG003",
                    $"Unity package filename should follow 'name-version' convention: {package.Source}",
                    package.Source
                );
            }
        }

        _logger.LogDebug("Unity package validation complete: {ErrorCount} errors, {WarningCount} warnings",
            result.Errors.Count, result.Warnings.Count);
    }

    /// <summary>
    /// Level 4: Validates code patch applicability.
    /// </summary>
    private void ValidateCodePatches(PreparationConfig config, ValidationResult result)
    {
        _logger.LogDebug("Validating code patches...");

        foreach (var patch in config.CodePatches)
        {
            if (string.IsNullOrWhiteSpace(patch.File))
            {
                continue;
            }

            if (!_pathResolver.FileExists(patch.File))
            {
                continue; // Already reported in file existence validation
            }

            var absolutePath = _pathResolver.Resolve(patch.File);

            // Validate patch type matches file extension
            var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
            var expectedType = patch.Type;

            var isValid = expectedType switch
            {
                PatchType.CSharp => extension == ".cs",
                PatchType.Json => extension == ".json",
                PatchType.UnityAsset => extension == ".asset" || extension == ".prefab" || extension == ".unity",
                PatchType.Text => true, // Text can be any file
                _ => false
            };

            if (!isValid)
            {
                AddWarning(
                    result,
                    "PATCH001",
                    $"Patch type '{expectedType}' may not match file extension '{extension}': {patch.File}",
                    patch.File
                );
            }

            // Validate search pattern is not empty
            if (string.IsNullOrWhiteSpace(patch.Search))
            {
                continue; // Already reported in schema validation
            }

            // For text patches, validate regex if it looks like regex
            if (patch.Type == PatchType.Text && (patch.Search.Contains(".*") || patch.Search.Contains("\\")))
            {
                try
                {
                    _ = new System.Text.RegularExpressions.Regex(patch.Search);
                }
                catch (Exception ex)
                {
                    AddError(
                        result,
                        "PATCH002",
                        $"Invalid regex pattern in patch search: {ex.Message}",
                        patch.File,
                        context: patch.Search
                    );
                }
            }
        }

        _logger.LogDebug("Code patch validation complete: {ErrorCount} errors, {WarningCount} warnings",
            result.Errors.Count, result.Warnings.Count);
    }

    private ValidationResult FinalizeResult(ValidationResult result)
    {
        result.IsValid = result.Errors.Count == 0;
        result.Summary = GenerateSummary(result);
        result.Timestamp = DateTime.UtcNow;

        _logger.LogInformation(
            "Validation complete: {IsValid}, {ErrorCount} errors, {WarningCount} warnings",
            result.IsValid ? "PASSED" : "FAILED",
            result.Errors.Count,
            result.Warnings.Count
        );

        _validationCompletedPublisher.Publish(new ValidationCompletedMessage(result));

        return result;
    }

    private string GenerateSummary(ValidationResult result)
    {
        if (result.IsValid && result.Warnings.Count == 0)
        {
            return $"Validation passed at level {result.Level} with no issues.";
        }

        if (result.IsValid)
        {
            return $"Validation passed at level {result.Level} with {result.Warnings.Count} warning(s).";
        }

        return $"Validation failed at level {result.Level} with {result.Errors.Count} error(s) and {result.Warnings.Count} warning(s).";
    }

    private void AddError(
        ValidationResult result,
        string code,
        string message,
        string? file = null,
        int? line = null,
        string? context = null)
    {
        var error = new ValidationError
        {
            Code = code,
            Message = message,
            File = file,
            Line = line,
            Context = context
        };

        result.Errors.Add(error);
        _validationErrorFoundPublisher.Publish(new ValidationErrorFoundMessage(error));

        _logger.LogWarning("Validation error [{Code}]: {Message}", code, message);
    }

    private void AddWarning(
        ValidationResult result,
        string code,
        string message,
        string? file = null,
        string? context = null)
    {
        var warning = new ValidationWarning
        {
            Code = code,
            Message = message,
            File = file,
            Context = context
        };

        result.Warnings.Add(warning);
        _validationWarningFoundPublisher.Publish(new ValidationWarningFoundMessage(warning));

        _logger.LogDebug("Validation warning [{Code}]: {Message}", code, message);
    }
}
