using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when validation starts.
/// </summary>
/// <param name="Level">Validation level being performed.</param>
public record ValidationStartedMessage(ValidationLevel Level);

/// <summary>
/// Message published when validation completes.
/// </summary>
/// <param name="Result">Validation result.</param>
public record ValidationCompletedMessage(ValidationResult Result);

/// <summary>
/// Message published when a validation error is found.
/// </summary>
/// <param name="Error">The validation error.</param>
public record ValidationErrorFoundMessage(ValidationError Error);

/// <summary>
/// Message published when a validation warning is found.
/// </summary>
/// <param name="Warning">The validation warning.</param>
public record ValidationWarningFoundMessage(ValidationWarning Warning);
