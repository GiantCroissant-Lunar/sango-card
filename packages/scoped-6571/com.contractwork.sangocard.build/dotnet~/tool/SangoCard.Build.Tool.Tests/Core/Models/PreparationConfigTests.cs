using System.Text.Json;
using FluentAssertions;
using SangoCard.Build.Tool.Core.Models;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Models;

/// <summary>
/// Tests for PreparationConfig and related models.
/// </summary>
public class PreparationConfigTests
{
    [Fact]
    public void PreparationConfig_ShouldSerializeToJson()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            Description = "Test configuration",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "com.unity.addressables",
                    Version = "1.21.2",
                    Source = "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
                    Target = "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
                }
            },
            Assemblies = new List<AssemblyReference>
            {
                new()
                {
                    Name = "Newtonsoft.Json",
                    Version = "13.0.1",
                    Source = "build/preparation/cache/Newtonsoft.Json.dll",
                    Target = "projects/client/Assets/Plugins/Newtonsoft.Json.dll"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        // Assert
        json.Should().Contain("\"version\": \"1.0\"");
        json.Should().Contain("\"description\": \"Test configuration\"");
        json.Should().Contain("\"packages\"");
        json.Should().Contain("\"assemblies\"");
        json.Should().Contain("com.unity.addressables");
        json.Should().Contain("Newtonsoft.Json");
    }

    [Fact]
    public void PreparationConfig_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = """
        {
            "version": "1.0",
            "description": "Test configuration",
            "packages": [
                {
                    "name": "com.unity.addressables",
                    "version": "1.21.2",
                    "source": "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
                    "target": "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
                }
            ],
            "assemblies": [
                {
                    "name": "Newtonsoft.Json",
                    "version": "13.0.1",
                    "source": "build/preparation/cache/Newtonsoft.Json.dll",
                    "target": "projects/client/Assets/Plugins/Newtonsoft.Json.dll"
                }
            ]
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<PreparationConfig>(json);

        // Assert
        config.Should().NotBeNull();
        config!.Version.Should().Be("1.0");
        config.Description.Should().Be("Test configuration");
        config.Packages.Should().HaveCount(1);
        config.Packages[0].Name.Should().Be("com.unity.addressables");
        config.Assemblies.Should().HaveCount(1);
        config.Assemblies[0].Name.Should().Be("Newtonsoft.Json");
    }

    [Fact]
    public void PreparationConfig_ShouldHaveDefaultValues()
    {
        // Act
        var config = new PreparationConfig();

        // Assert
        config.Version.Should().Be("1.0");
        config.Packages.Should().NotBeNull().And.BeEmpty();
        config.Assemblies.Should().NotBeNull().And.BeEmpty();
        config.AssetManipulations.Should().NotBeNull().And.BeEmpty();
        config.CodePatches.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UnityPackageReference_ShouldSerializeCorrectly()
    {
        // Arrange
        var package = new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Source = "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
            Target = "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
        };

        // Act
        var json = JsonSerializer.Serialize(package);

        // Assert
        json.Should().Contain("\"name\":\"com.unity.addressables\"");
        json.Should().Contain("\"version\":\"1.21.2\"");
        json.Should().Contain("\"source\":");
        json.Should().Contain("\"target\":");
    }

    [Fact]
    public void AssemblyReference_ShouldSerializeCorrectly()
    {
        // Arrange
        var assembly = new AssemblyReference
        {
            Name = "Newtonsoft.Json",
            Version = "13.0.1",
            Source = "build/preparation/cache/Newtonsoft.Json.dll",
            Target = "projects/client/Assets/Plugins/Newtonsoft.Json.dll"
        };

        // Act
        var json = JsonSerializer.Serialize(assembly);

        // Assert
        json.Should().Contain("\"name\":\"Newtonsoft.Json\"");
        json.Should().Contain("\"version\":\"13.0.1\"");
    }
}
