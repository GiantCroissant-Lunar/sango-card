using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

/// <summary>
/// Unit tests for SourceManagementService.
/// Tests the two-config architecture and manual source control features.
/// </summary>
public class SourceManagementServiceTests : IDisposable
{
    private readonly Mock<PathResolver> _mockPathResolver;
    private readonly Mock<ILogger<SourceManagementService>> _mockLogger;
    private readonly SourceManagementService _service;
    private readonly string _testDirectory;

    public SourceManagementServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BuildPrepTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockPathResolver = new Mock<PathResolver>(string.Empty);
        _mockPathResolver.Setup(x => x.Resolve(It.IsAny<string>()))
            .Returns<string>(path => Path.Combine(_testDirectory, path));
        _mockPathResolver.Setup(x => x.GitRoot).Returns(_testDirectory);

        _mockLogger = new Mock<ILogger<SourceManagementService>>();
        _service = new SourceManagementService(_mockPathResolver.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task LoadOrCreateManifestAsync_WhenFileDoesNotExist_CreatesNewManifest()
    {
        // Arrange
        var manifestPath = "build/preparation/sources/test.json";

        // Act
        var manifest = await _service.LoadOrCreateManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Id.Should().Be("test");
        manifest.Title.Should().Be("Preparation Manifest");
        manifest.CacheDirectory.Should().Be("build/preparation/cache");
        manifest.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadOrCreateManifestAsync_WhenFileExists_LoadsManifest()
    {
        // Arrange
        var manifestPath = "build/preparation/sources/existing.json";
        var fullPath = _mockPathResolver.Object.Resolve(manifestPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        var existingManifest = new PreparationManifest
        {
            Id = "existing",
            Title = "Existing Manifest",
            CacheDirectory = "custom/cache",
            Items = new List<PreparationItem>
            {
                new() { Source = "test/source", CacheAs = "test-pkg", Type = PreparationItemType.Package }
            }
        };

        await File.WriteAllTextAsync(fullPath, System.Text.Json.JsonSerializer.Serialize(existingManifest));

        // Act
        var manifest = await _service.LoadOrCreateManifestAsync(manifestPath);

        // Assert
        manifest.Should().NotBeNull();
        manifest.Id.Should().Be("existing");
        manifest.Title.Should().Be("Existing Manifest");
        manifest.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveManifestAsync_CreatesDirectoryAndSavesFile()
    {
        // Arrange
        var manifestPath = "build/preparation/sources/new.json";
        var manifest = new PreparationManifest
        {
            Id = "new",
            Title = "New Manifest",
            CacheDirectory = "build/preparation/cache"
        };

        // Act
        await _service.SaveManifestAsync(manifest, manifestPath);

        // Assert
        var fullPath = _mockPathResolver.Object.Resolve(manifestPath);
        File.Exists(fullPath).Should().BeTrue();

        var savedContent = await File.ReadAllTextAsync(fullPath);
        savedContent.Should().Contain("\"id\": \"new\"");
        savedContent.Should().Contain("\"title\": \"New Manifest\"");
    }

    [Fact]
    public async Task AddSourceAsync_WithValidPackage_AddsToManifestAndCopiesFile()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            CacheDirectory = "build/preparation/cache"
        };

        // Create source package directory
        var sourceDir = Path.Combine(_testDirectory, "source", "test-package");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            "source/test-package",
            "test-package",
            PreparationItemType.Package,
            dryRun: false
        );

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
        result.CacheRelativePath.Should().Be("build/preparation/cache/test-package");

        manifest.Items.Should().HaveCount(1);
        manifest.Items[0].Source.Should().Contain("source/test-package");
        manifest.Items[0].CacheAs.Should().Be("test-package");
        manifest.Items[0].Type.Should().Be(PreparationItemType.Package);
    }

    [Fact]
    public async Task AddSourceAsync_WithDryRun_DoesNotCopyFiles()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            CacheDirectory = "build/preparation/cache"
        };

        var sourceDir = Path.Combine(_testDirectory, "source", "dry-run-pkg");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "package.json"), "{}");

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            "source/dry-run-pkg",
            "dry-run-pkg",
            PreparationItemType.Package,
            dryRun: true
        );

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();

        // Manifest should still be updated in dry run mode
        manifest.Items.Should().HaveCount(1);

        // But cache directory should not exist
        var cacheDir = Path.Combine(_testDirectory, "build/preparation/cache/dry-run-pkg");
        Directory.Exists(cacheDir).Should().BeFalse();
    }

    [Fact]
    public async Task AddSourceAsync_WithNonExistentSource_ReturnsError()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            CacheDirectory = "build/preparation/cache"
        };

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            "non-existent/source",
            "missing-pkg",
            PreparationItemType.Package,
            dryRun: false
        );

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        manifest.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddInjection_AddsPackageToConfig()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Description = "test",
            Packages = new List<UnityPackageReference>()
        };

        // Act
        _service.AddInjection(
            config,
            "build/preparation/cache/test-package",
            "projects/client/Packages/test-package",
            PreparationItemType.Package,
            "test-package",
            "1.0.0"
        );

        // Assert
        config.Packages.Should().HaveCount(1);
        config.Packages[0].Name.Should().Be("test-package");
        config.Packages[0].Version.Should().Be("1.0.0");
        config.Packages[0].Source.Should().Be("build/preparation/cache/test-package");
        config.Packages[0].Target.Should().Be("projects/client/Packages/test-package");
    }

    [Fact]
    public void AddInjection_AddsAssemblyToConfig()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Description = "test",
            Assemblies = new List<AssemblyReference>()
        };

        // Act
        _service.AddInjection(
            config,
            "build/preparation/cache/TestLib.dll",
            "projects/client/Assets/Plugins/TestLib.dll",
            PreparationItemType.Assembly,
            "TestLib",
            "2.0.0"
        );

        // Assert
        config.Assemblies.Should().HaveCount(1);
        config.Assemblies[0].Name.Should().Be("TestLib");
        config.Assemblies[0].Version.Should().Be("2.0.0");
        config.Assemblies[0].Source.Should().Be("build/preparation/cache/TestLib.dll");
        config.Assemblies[0].Target.Should().Be("projects/client/Assets/Plugins/TestLib.dll");
    }

    [Fact]
    public async Task ProcessBatchSourcesAsync_WithMultipleItems_ProcessesAll()
    {
        // Arrange
        var batchManifest = new BatchManifest
        {
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source/pkg1", Name = "pkg1" },
                new() { Source = "source/pkg2", Name = "pkg2" }
            },
            Assemblies = new List<BatchAssemblyItem>
            {
                new() { Source = "source/asm1.dll", Name = "asm1" }
            }
        };

        var targetManifest = new PreparationManifest
        {
            Id = "batch-test",
            CacheDirectory = "build/preparation/cache"
        };

        // Create source directories/files
        foreach (var pkg in batchManifest.Packages)
        {
            var dir = Path.Combine(_testDirectory, pkg.Source);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
        }

        foreach (var asm in batchManifest.Assemblies)
        {
            var file = Path.Combine(_testDirectory, asm.Source);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "dummy");
        }

        // Act
        var result = await _service.ProcessBatchSourcesAsync(batchManifest, targetManifest, continueOnError: false);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(3);
        result.FailureCount.Should().Be(0);
        targetManifest.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task ProcessBatchSourcesAsync_WithErrors_StopsWithoutContinueOnError()
    {
        // Arrange
        var batchManifest = new BatchManifest
        {
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source/good-pkg", Name = "good-pkg" },
                new() { Source = "source/missing-pkg", Name = "missing-pkg" }, // This will fail
                new() { Source = "source/another-pkg", Name = "another-pkg" }
            }
        };

        var targetManifest = new PreparationManifest
        {
            Id = "batch-test",
            CacheDirectory = "build/preparation/cache"
        };

        // Create only the first package
        var dir = Path.Combine(_testDirectory, "source/good-pkg");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "package.json"), "{}");

        // Act
        var result = await _service.ProcessBatchSourcesAsync(batchManifest, targetManifest, continueOnError: false);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(1); // Only first one succeeded
        result.FailureCount.Should().BeGreaterThan(0);
        result.FailedItems.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessBatchSourcesAsync_WithErrors_ContinuesWithContinueOnError()
    {
        // Arrange
        var batchManifest = new BatchManifest
        {
            Packages = new List<BatchPackageItem>
            {
                new() { Source = "source/good-pkg1", Name = "good-pkg1" },
                new() { Source = "source/missing-pkg", Name = "missing-pkg" }, // This will fail
                new() { Source = "source/good-pkg2", Name = "good-pkg2" }
            }
        };

        var targetManifest = new PreparationManifest
        {
            Id = "batch-test",
            CacheDirectory = "build/preparation/cache"
        };

        // Create good packages
        foreach (var pkgName in new[] { "good-pkg1", "good-pkg2" })
        {
            var dir = Path.Combine(_testDirectory, $"source/{pkgName}");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "package.json"), "{}");
        }

        // Act
        var result = await _service.ProcessBatchSourcesAsync(batchManifest, targetManifest, continueOnError: true);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(2); // Two succeeded
        result.FailureCount.Should().Be(1); // One failed
        result.SuccessfulItems.Should().HaveCount(2);
        result.FailedItems.Should().HaveCount(1);
    }

    [Fact]
    public void ProcessBatchInjections_AddsAllItemsToConfig()
    {
        // Arrange
        var batchManifest = new BatchManifest
        {
            Packages = new List<BatchPackageItem>
            {
                new() { Name = "pkg1", Version = "1.0.0", Target = "projects/client/Packages/pkg1" }
            },
            Assemblies = new List<BatchAssemblyItem>
            {
                new() { Name = "asm1", Version = "2.0.0", Target = "projects/client/Assets/Plugins/asm1.dll" }
            }
        };

        var targetConfig = new PreparationConfig
        {
            Description = "test",
            Packages = new List<UnityPackageReference>(),
            Assemblies = new List<AssemblyReference>()
        };

        // Act
        var result = _service.ProcessBatchInjections(batchManifest, targetConfig, dryRun: false, continueOnError: false);

        // Assert
        result.Should().NotBeNull();
        result.SuccessCount.Should().Be(2);
        result.FailureCount.Should().Be(0);
        targetConfig.Packages.Should().HaveCount(1);
        targetConfig.Assemblies.Should().HaveCount(1);
    }
}
