using System.Text.Json;
using MessagePipe;
using Microsoft.Extensions.Logging;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;

namespace SangoCard.Build.Tool.Core.Services;

/// <summary>
/// Service for managing preparation configuration files.
/// </summary>
public class ConfigService
{
    private readonly PathResolver _pathResolver;
    private readonly ILogger<ConfigService> _logger;
    private readonly IPublisher<ConfigLoadedMessage> _configLoadedPublisher;
    private readonly IPublisher<ConfigSavedMessage> _configSavedPublisher;
    private readonly IPublisher<ConfigModifiedMessage> _configModifiedPublisher;
    private readonly IPublisher<PackageAddedMessage> _packageAddedPublisher;
    private readonly IPublisher<AssemblyAddedMessage> _assemblyAddedPublisher;
    private readonly IPublisher<PatchAddedMessage> _patchAddedPublisher;
    private readonly IPublisher<DefineSymbolAddedMessage> _defineSymbolAddedPublisher;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigService"/> class.
    /// </summary>
    public ConfigService(
        PathResolver pathResolver,
        ILogger<ConfigService> logger,
        IPublisher<ConfigLoadedMessage> configLoadedPublisher,
        IPublisher<ConfigSavedMessage> configSavedPublisher,
        IPublisher<ConfigModifiedMessage> configModifiedPublisher,
        IPublisher<PackageAddedMessage> packageAddedPublisher,
        IPublisher<AssemblyAddedMessage> assemblyAddedPublisher,
        IPublisher<PatchAddedMessage> patchAddedPublisher,
        IPublisher<DefineSymbolAddedMessage> defineSymbolAddedPublisher)
    {
        _pathResolver = pathResolver;
        _logger = logger;
        _configLoadedPublisher = configLoadedPublisher;
        _configSavedPublisher = configSavedPublisher;
        _configModifiedPublisher = configModifiedPublisher;
        _packageAddedPublisher = packageAddedPublisher;
        _assemblyAddedPublisher = assemblyAddedPublisher;
        _patchAddedPublisher = patchAddedPublisher;
        _defineSymbolAddedPublisher = defineSymbolAddedPublisher;
    }

    /// <summary>
    /// Loads a configuration from a file.
    /// </summary>
    /// <param name="relativePath">Relative path to config file (from git root).</param>
    /// <returns>The loaded configuration.</returns>
    public async Task<PreparationConfig> LoadAsync(string relativePath)
    {
        var absolutePath = _pathResolver.Resolve(relativePath);

        _logger.LogInformation("Loading configuration from: {Path}", absolutePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {absolutePath}", absolutePath);
        }

        var json = await File.ReadAllTextAsync(absolutePath);
        var config = JsonSerializer.Deserialize<PreparationConfig>(json, _jsonOptions);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to deserialize configuration from: {absolutePath}");
        }

        _logger.LogInformation(
            "Configuration loaded: {PackageCount} packages, {AssemblyCount} assemblies, {PatchCount} patches",
            config.Packages.Count,
            config.Assemblies.Count,
            config.CodePatches.Count
        );

        // Publish message
        _configLoadedPublisher.Publish(new ConfigLoadedMessage(config, relativePath));

        return config;
    }

    /// <summary>
    /// Saves a configuration to a file.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    /// <param name="relativePath">Relative path to save to (from git root).</param>
    public async Task SaveAsync(PreparationConfig config, string relativePath)
    {
        var absolutePath = _pathResolver.Resolve(relativePath);

        _logger.LogInformation("Saving configuration to: {Path}", absolutePath);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(absolutePath, json);

        _logger.LogInformation("Configuration saved successfully");

        // Publish message
        _configSavedPublisher.Publish(new ConfigSavedMessage(config, relativePath));
    }

    /// <summary>
    /// Creates a new empty configuration.
    /// </summary>
    /// <param name="description">Optional description.</param>
    /// <returns>A new configuration.</returns>
    public PreparationConfig CreateNew(string? description = null)
    {
        _logger.LogDebug("Creating new configuration");

        return new PreparationConfig
        {
            Version = "1.0",
            Description = description
        };
    }

    /// <summary>
    /// Adds a Unity package to the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="package">The package to add.</param>
    public void AddPackage(PreparationConfig config, UnityPackageReference package)
    {
        _logger.LogDebug("Adding package: {Name}@{Version}", package.Name, package.Version);

        // Check for duplicates
        var existing = config.Packages.FirstOrDefault(p =>
            p.Name == package.Name && p.Version == package.Version);

        if (existing != null)
        {
            _logger.LogWarning("Package {Name}@{Version} already exists, replacing", package.Name, package.Version);
            config.Packages.Remove(existing);
        }

        config.Packages.Add(package);

        // Publish messages
        _packageAddedPublisher.Publish(new PackageAddedMessage(package));
        _configModifiedPublisher.Publish(new ConfigModifiedMessage(
            config,
            $"Added package: {package.Name}@{package.Version}"
        ));
    }

    /// <summary>
    /// Adds an assembly to the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="assembly">The assembly to add.</param>
    public void AddAssembly(PreparationConfig config, AssemblyReference assembly)
    {
        _logger.LogDebug("Adding assembly: {Name}", assembly.Name);

        // Check for duplicates
        var existing = config.Assemblies.FirstOrDefault(a => a.Name == assembly.Name);

        if (existing != null)
        {
            _logger.LogWarning("Assembly {Name} already exists, replacing", assembly.Name);
            config.Assemblies.Remove(existing);
        }

        config.Assemblies.Add(assembly);

        // Publish messages
        _assemblyAddedPublisher.Publish(new AssemblyAddedMessage(assembly));
        _configModifiedPublisher.Publish(new ConfigModifiedMessage(
            config,
            $"Added assembly: {assembly.Name}"
        ));
    }

    /// <summary>
    /// Adds a code patch to the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="patch">The patch to add.</param>
    public void AddPatch(PreparationConfig config, CodePatch patch)
    {
        _logger.LogDebug("Adding patch for file: {File}", patch.File);

        config.CodePatches.Add(patch);

        // Publish messages
        _patchAddedPublisher.Publish(new PatchAddedMessage(patch));
        _configModifiedPublisher.Publish(new ConfigModifiedMessage(
            config,
            $"Added patch for: {patch.File}"
        ));
    }

    /// <summary>
    /// Adds a scripting define symbol to the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="symbol">The symbol to add.</param>
    /// <param name="platform">Optional platform (null for all platforms).</param>
    public void AddDefineSymbol(PreparationConfig config, string symbol, string? platform = null)
    {
        _logger.LogDebug("Adding define symbol: {Symbol}", symbol);

        // Ensure ScriptingDefineSymbols exists
        config.ScriptingDefineSymbols ??= new ScriptingDefineSymbols
        {
            Platform = platform
        };

        // Check if symbol already exists
        if (!config.ScriptingDefineSymbols.Add.Contains(symbol))
        {
            config.ScriptingDefineSymbols.Add.Add(symbol);

            // Publish messages
            _defineSymbolAddedPublisher.Publish(new DefineSymbolAddedMessage(symbol));
            _configModifiedPublisher.Publish(new ConfigModifiedMessage(
                config,
                $"Added define symbol: {symbol}"
            ));
        }
        else
        {
            _logger.LogWarning("Define symbol {Symbol} already exists", symbol);
        }
    }

    /// <summary>
    /// Removes a package from the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="name">Package name.</param>
    /// <param name="version">Package version.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemovePackage(PreparationConfig config, string name, string version)
    {
        _logger.LogDebug("Removing package: {Name}@{Version}", name, version);

        var package = config.Packages.FirstOrDefault(p => p.Name == name && p.Version == version);
        if (package != null)
        {
            config.Packages.Remove(package);
            _configModifiedPublisher.Publish(new ConfigModifiedMessage(
                config,
                $"Removed package: {name}@{version}"
            ));
            return true;
        }

        _logger.LogWarning("Package {Name}@{Version} not found", name, version);
        return false;
    }

    /// <summary>
    /// Removes an assembly from the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="name">Assembly name.</param>
    /// <returns>True if removed, false if not found.</returns>
    public bool RemoveAssembly(PreparationConfig config, string name)
    {
        _logger.LogDebug("Removing assembly: {Name}", name);

        var assembly = config.Assemblies.FirstOrDefault(a => a.Name == name);
        if (assembly != null)
        {
            config.Assemblies.Remove(assembly);
            _configModifiedPublisher.Publish(new ConfigModifiedMessage(
                config,
                $"Removed assembly: {name}"
            ));
            return true;
        }

        _logger.LogWarning("Assembly {Name} not found", name);
        return false;
    }
}
