using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

/// <summary>
/// Unit tests for BatchManifestService.
/// Tests batch manifest loading and processing.
/// </summary>
public class BatchManifestServiceTests : IDisposable
{
    private readonly Mock<PathResolver> _mockPathResolver;
    private readonly Mock<ILogger<BatchManifestService>> _mockLogger;
    private readonly BatchManifestService _service;
    private readonly string _testDirectory;

    public BatchManifestServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BuildPrepTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockPathResolver = new Mock<PathResolver>(string.Empty);
        _mockPathResolver.Setup(x => x.Resolve(It.IsAny<string>()))
            .Returns<string>(path => Path.Combine(_testDirectory, path));

        _mockLogger = new Mock<ILogger<BatchManifestService>>();
        _service = new BatchManifestService(_mockPathResolver.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithJsonFile_LoadsSuccessfully()
    {
        // Arrange
        var manifestPath = "test-manifest.json";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var jsonContent = @"{
  ""version"": ""1.0"",
  ""description"": ""Test batch manifest"",
  ""packages"": [
    {
      ""source"": ""D:/downloads/com.example.package"",
      ""name"": ""com.example.package"",
      ""version"": ""1.0.0"",
      ""target"": ""projects/client/Packages/com.example.package""
    }
  ],
  ""assemblies"": [
    {
      ""source"": ""C:/libs/TestLib.dll"",
      ""name"": ""TestLib"",
      ""version"": ""2.0.0"",
      ""target"": ""projects/client/Assets/Plugins/TestLib.dll""
    }
  ],
  ""assets"": [
    {
      ""source"": ""D:/assets/Textures"",
      ""name"": ""Textures"",
      ""target"": ""projects/client/Assets/Resources/Textures""
    }
  ]
}";

        await File.WriteAllTextAsync(fullPath, jsonContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Version.Should().Be("1.0");
        manifest.Description.Should().Be("Test batch manifest");
        manifest.Packages.Should().HaveCount(1);
        manifest.Assemblies.Should().HaveCount(1);
        manifest.Assets.Should().HaveCount(1);

        manifest.Packages[0].Name.Should().Be("com.example.package");
        manifest.Assemblies[0].Name.Should().Be("TestLib");
        manifest.Assets[0].Name.Should().Be("Textures");
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithYamlFile_LoadsSuccessfully()
    {
        // Arrange
        var manifestPath = "test-manifest.yaml";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var yamlContent = @"version: '1.0'
description: Test YAML batch manifest
packages:
  - source: D:/downloads/com.example.package
    name: com.example.package
    version: 1.0.0
    target: projects/client/Packages/com.example.package
assemblies:
  - source: C:/libs/TestLib.dll
    name: TestLib
    version: 2.0.0
    target: projects/client/Assets/Plugins/TestLib.dll
assets:
  - source: D:/assets/Textures
    name: Textures
    target: projects/client/Assets/Resources/Textures
";

        await File.WriteAllTextAsync(fullPath, yamlContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Version.Should().Be("1.0");
        manifest.Description.Should().Be("Test YAML batch manifest");
        manifest.Packages.Should().HaveCount(1);
        manifest.Assemblies.Should().HaveCount(1);
        manifest.Assets.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithYmlExtension_LoadsSuccessfully()
    {
        // Arrange
        var manifestPath = "test-manifest.yml";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var yamlContent = @"version: '1.0'
packages:
  - source: test/source
    name: test-package
    version: 1.0.0
";

        await File.WriteAllTextAsync(fullPath, yamlContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Packages.Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithNonExistentFile_ThrowsException()
    {
        // Arrange
        var manifestPath = "non-existent.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.LoadBatchManifestAsync(manifestPath)
        );
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var manifestPath = "invalid.json";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        await File.WriteAllTextAsync(fullPath, "{ invalid json }");

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            () => _service.LoadBatchManifestAsync(manifestPath)
        );
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithEmptyManifest_LoadsSuccessfully()
    {
        // Arrange
        var manifestPath = "empty.json";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var jsonContent = @"{
  ""version"": ""1.0""
}";

        await File.WriteAllTextAsync(fullPath, jsonContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Version.Should().Be("1.0");
        manifest.Packages.Should().BeEmpty();
        manifest.Assemblies.Should().BeEmpty();
        manifest.Assets.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithPackagesOnly_LoadsCorrectly()
    {
        // Arrange
        var manifestPath = "packages-only.json";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var jsonContent = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""source"": ""source1"",
      ""name"": ""package1"",
      ""version"": ""1.0.0""
    },
    {
      ""source"": ""source2"",
      ""name"": ""package2"",
      ""version"": ""2.0.0""
    }
  ]
}";

        await File.WriteAllTextAsync(fullPath, jsonContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Packages.Should().HaveCount(2);
        manifest.Assemblies.Should().BeEmpty();
        manifest.Assets.Should().BeEmpty();

        manifest.Packages[0].Name.Should().Be("package1");
        manifest.Packages[1].Name.Should().Be("package2");
    }

    [Fact]
    public async Task LoadBatchManifestAsync_WithOptionalFields_LoadsCorrectly()
    {
        // Arrange
        var manifestPath = "optional-fields.json";
        var fullPath = Path.Combine(_testDirectory, manifestPath);

        var jsonContent = @"{
  ""version"": ""1.0"",
  ""packages"": [
    {
      ""source"": ""source1"",
      ""name"": ""package1""
    }
  ]
}";

        await File.WriteAllTextAsync(fullPath, jsonContent);

        // Act
        var manifest = await _service.LoadBatchManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Packages.Should().HaveCount(1);
        manifest.Packages[0].Name.Should().Be("package1");
        manifest.Packages[0].Version.Should().BeNullOrEmpty();
        manifest.Packages[0].Target.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ValidateBatchManifest_WithValidManifest_ReturnsNoErrors()
    {
        // Arrange
        var manifest = new BatchManifest
        {
            Version = "1.0",
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source1", Name = "package1", Version = "1.0.0" }
            }
        };

        // Act
        var result = _service.ValidateBatchManifest(manifest);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateBatchManifest_WithMissingPackageName_ReturnsError()
    {
        // Arrange
        var manifest = new BatchManifest
        {
            Version = "1.0",
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source1", Name = "", Version = "1.0.0" }
            }
        };

        // Act
        var result = _service.ValidateBatchManifest(manifest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Package name is required");
    }

    [Fact]
    public void ValidateBatchManifest_WithMissingSource_ReturnsError()
    {
        // Arrange
        var manifest = new BatchManifest
        {
            Version = "1.0",
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "", Name = "package1", Version = "1.0.0" }
            }
        };

        // Act
        var result = _service.ValidateBatchManifest(manifest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Source is required");
    }

    [Fact]
    public void ValidateBatchManifest_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var manifest = new BatchManifest
        {
            Version = "1.0",
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "", Name = "", Version = "1.0.0" },
                new() { Source = "source2", Name = "package2" }
            },
            Assemblies = new List<BatchAssemblyItem>
            {
                new() { Source = "", Name = "assembly1" }
            }
        };

        // Act
        var result = _service.ValidateBatchManifest(manifest);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void ValidateBatchManifest_WithWarnings_ReturnsWarnings()
    {
        // Arrange
        var manifest = new BatchManifest
        {
            Version = "1.0",
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source1", Name = "package1" } // Missing version
            }
        };

        // Act
        var result = _service.ValidateBatchManifest(manifest);

        // Assert
        result.IsValid.Should().BeTrue(); // Still valid
        result.Warnings.Should().Contain("version");
    }
}
