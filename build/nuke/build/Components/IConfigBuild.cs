using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;

/// <summary>
/// Reusable component for build configuration management and validation
/// </summary>
interface IConfigBuild : INukeBuild
{
    // Resolve repository root robustly (same pattern as IReportBuild)
    AbsolutePath RepoRoot =>
        Directory.Exists((RootDirectory / ".git").ToString())
            ? RootDirectory
            : (AbsolutePath)System.IO.Path.GetFullPath(((BuildProjectDirectory / ".." / ".." / "..").ToString()));

    [Parameter("Path to preparation configs directory")]
    AbsolutePath ConfigsDirectory => TryGetValue(() => ConfigsDirectory) ?? RepoRoot / "build" / "preparation" / "configs";

    [Parameter("Path to schemas directory")]
    AbsolutePath SchemasDirectory => TryGetValue(() => SchemasDirectory) ?? BuildProjectDirectory / "Schemas";

    /// <summary>
    /// Validate all preparation and injection configs against their schemas
    /// </summary>
    Target ValidateConfigs => _ => _
        .Description("Validate all build preparation configs against JSON schemas")
        .Executes(() =>
        {
            Serilog.Log.Information("Validating build configs in: {Path}", ConfigsDirectory);

            var preparationSchema = SchemasDirectory / "preparation.schema.json";
            var injectionSchema = SchemasDirectory / "injection.schema.json";

            if (!File.Exists(preparationSchema))
            {
                Serilog.Log.Error("Preparation schema not found: {Path}", preparationSchema);
                throw new FileNotFoundException("Preparation schema not found", preparationSchema);
            }

            if (!File.Exists(injectionSchema))
            {
                Serilog.Log.Error("Injection schema not found: {Path}", injectionSchema);
                throw new FileNotFoundException("Injection schema not found", injectionSchema);
            }

            var configs = Directory.GetFiles(ConfigsDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".schema.json"))
                .ToArray();

            Serilog.Log.Information("Found {Count} config files to validate", configs.Length);

            var errors = 0;
            foreach (var configPath in configs)
            {
                var configName = Path.GetFileName(configPath);
                Serilog.Log.Information("Validating: {Config}", configName);

                try
                {
                    var configJson = File.ReadAllText(configPath);
                    var config = JsonDocument.Parse(configJson);
                    var root = config.RootElement;

                    // Basic validation: check required fields
                    JsonElement versionElem;
                    if (!root.TryGetProperty("version", out versionElem))
                    {
                        Serilog.Log.Error("{Config}: Missing required field 'version'", configName);
                        errors++;
                        continue;
                    }

                    JsonElement idElem;
                    if (!root.TryGetProperty("id", out idElem))
                    {
                        Serilog.Log.Error("{Config}: Missing required field 'id'", configName);
                        errors++;
                        continue;
                    }

                    JsonElement titleElem;
                    if (!root.TryGetProperty("title", out titleElem))
                    {
                        Serilog.Log.Error("{Config}: Missing required field 'title'", configName);
                        errors++;
                        continue;
                    }

                    JsonElement packagesElem;
                    if (!root.TryGetProperty("packages", out packagesElem))
                    {
                        Serilog.Log.Error("{Config}: Missing required field 'packages'", configName);
                        errors++;
                        continue;
                    }

                    JsonElement assembliesElem;
                    if (!root.TryGetProperty("assemblies", out assembliesElem))
                    {
                        Serilog.Log.Error("{Config}: Missing required field 'assemblies'", configName);
                        errors++;
                        continue;
                    }

                    // Determine config type by checking for injection-specific fields
                    JsonElement assetManipElem, codePatchElem, scriptingSymElem;
                    var isInjectionConfig = root.TryGetProperty("assetManipulations", out assetManipElem) ||
                                            root.TryGetProperty("codePatches", out codePatchElem) ||
                                            root.TryGetProperty("scriptingDefineSymbols", out scriptingSymElem);

                    var schemaType = isInjectionConfig ? "injection" : "preparation";
                    Serilog.Log.Information("{Config}: Detected as {Type} config", configName, schemaType);

                    // Validate packages structure
                    JsonElement packages;
                    if (root.TryGetProperty("packages", out packages))
                    {
                        var packageCount = 0;
                        foreach (var pkg in packages.EnumerateArray())
                        {
                            packageCount++;
                            JsonElement pkgName, pkgVersion, pkgSource, pkgTarget;
                            if (!pkg.TryGetProperty("name", out pkgName) ||
                                !pkg.TryGetProperty("version", out pkgVersion) ||
                                !pkg.TryGetProperty("source", out pkgSource) ||
                                !pkg.TryGetProperty("target", out pkgTarget))
                            {
                                Serilog.Log.Error("{Config}: Package #{Index} missing required fields", configName, packageCount);
                                errors++;
                            }
                        }
                        Serilog.Log.Information("{Config}: Validated {Count} packages", configName, packageCount);
                    }

                    // Validate assemblies structure
                    JsonElement assemblies;
                    if (root.TryGetProperty("assemblies", out assemblies))
                    {
                        var assemblyCount = 0;
                        foreach (var asm in assemblies.EnumerateArray())
                        {
                            assemblyCount++;
                            JsonElement asmName, asmVersion, asmSource, asmTarget;
                            if (!asm.TryGetProperty("name", out asmName) ||
                                !asm.TryGetProperty("version", out asmVersion) ||
                                !asm.TryGetProperty("source", out asmSource) ||
                                !asm.TryGetProperty("target", out asmTarget))
                            {
                                Serilog.Log.Error("{Config}: Assembly #{Index} missing required fields", configName, assemblyCount);
                                errors++;
                            }
                        }
                        Serilog.Log.Information("{Config}: Validated {Count} assemblies", configName, assemblyCount);
                    }

                    Serilog.Log.Information("{Config}: ✅ Validation passed", configName);
                }
                catch (JsonException ex)
                {
                    Serilog.Log.Error("{Config}: Invalid JSON - {Error}", configName, ex.Message);
                    errors++;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error("{Config}: Validation failed - {Error}", configName, ex.Message);
                    errors++;
                }
            }

            if (errors > 0)
            {
                Serilog.Log.Error("Config validation failed with {Count} error(s)", errors);
                throw new Exception($"Config validation failed with {errors} error(s)");
            }

            Serilog.Log.Information("✅ All configs validated successfully");
        });

    /// <summary>
    /// List all available configs
    /// </summary>
    Target ListConfigs => _ => _
        .Description("List all available build preparation configs")
        .Executes(() =>
        {
            Serilog.Log.Information("Configs directory: {Path}", ConfigsDirectory);

            var configs = Directory.GetFiles(ConfigsDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".schema.json"))
                .ToArray();

            Serilog.Log.Information("Found {Count} config file(s):", configs.Length);

            foreach (var configPath in configs)
            {
                var configName = Path.GetFileName(configPath);
                try
                {
                    var configJson = File.ReadAllText(configPath);
                    var config = JsonDocument.Parse(configJson);
                    var root = config.RootElement;

                    var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown";
                    var title = root.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "No title";
                    var description = root.TryGetProperty("description", out var descProp) ? descProp.GetString() : "";

                    Serilog.Log.Information("  • {Name}", configName);
                    Serilog.Log.Information("    ID: {Id}", id);
                    Serilog.Log.Information("    Title: {Title}", title);
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        Serilog.Log.Information("    Description: {Description}", description);
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning("  • {Name} (Error reading: {Error})", configName, ex.Message);
                }
            }
        });
}
