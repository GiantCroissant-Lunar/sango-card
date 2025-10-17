using SangoCard.Build.Tool.Core.Models;

namespace SangoCard.Build.Tool.Messages;

/// <summary>
/// Message published when configuration is loaded.
/// </summary>
/// <param name="Config">The loaded configuration.</param>
/// <param name="FilePath">The file path where config was loaded from.</param>
public record ConfigLoadedMessage(PreparationConfig Config, string FilePath);

/// <summary>
/// Message published when configuration is saved.
/// </summary>
/// <param name="Config">The saved configuration.</param>
/// <param name="FilePath">The file path where config was saved to.</param>
public record ConfigSavedMessage(PreparationConfig Config, string FilePath);

/// <summary>
/// Message published when configuration is modified.
/// </summary>
/// <param name="Config">The modified configuration.</param>
/// <param name="ChangeDescription">Description of what changed.</param>
public record ConfigModifiedMessage(PreparationConfig Config, string ChangeDescription);

/// <summary>
/// Message published when a package is added to configuration.
/// </summary>
/// <param name="Package">The package that was added.</param>
public record PackageAddedMessage(UnityPackageReference Package);

/// <summary>
/// Message published when an assembly is added to configuration.
/// </summary>
/// <param name="Assembly">The assembly that was added.</param>
public record AssemblyAddedMessage(AssemblyReference Assembly);

/// <summary>
/// Message published when a code patch is added to configuration.
/// </summary>
/// <param name="Patch">The patch that was added.</param>
public record PatchAddedMessage(CodePatch Patch);

/// <summary>
/// Message published when a define symbol is added to configuration.
/// </summary>
/// <param name="Symbol">The symbol that was added.</param>
public record DefineSymbolAddedMessage(string Symbol);
