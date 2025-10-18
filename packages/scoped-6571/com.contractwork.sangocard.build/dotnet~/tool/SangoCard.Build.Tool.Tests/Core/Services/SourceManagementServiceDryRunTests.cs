using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

/// <summary>
/// Unit tests for SourceManagementService dry-run functionality.
/// Tests that dry-run mode previews operations without making changes.
/// </summary>
[UsesVerify]
public class SourceManagementServiceDryRunTests : IDisposable
{
    private readonly Mock<PathResolver> _mockPathResolver;
    private readonly Mock<ILogger<SourceManagementService>> _mockLogger;
    private readonly SourceManagementService _service;
    private readonly string _testDirectory;
    private readonly string _sourceDirectory;
    private readonly string _cacheDirectory;

    public SourceManagementServiceDryRunTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BuildPrepDryRunTests", Guid.NewGuid().ToString());
        _sourceDirectory = Path.Combine(_testDirectory, "sources");
        _cacheDirectory = Path.Combine(_testDirectory, "cache");

        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_sourceDirectory);
        Directory.CreateDirectory(_cacheDirectory);

        _mockPathResolver = new Mock<PathResolver>(string.Empty);
        _mockPathResolver.Setup(x => x.Resolve(It.IsAny<string>()))
            .Returns<string>(path => Path.IsPathRooted(path) ? path : Path.Combine(_testDirectory, path));
        _mockPathResolver.Setup(x => x.GitRoot).Returns(_testDirectory);

        _mockLogger = new Mock<ILogger<SourceManagementService>>();
        _service = new SourceManagementService(_mockPathResolver.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithSingleFile_CalculatesCorrectMetrics()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        // Create a test file with known size
        var sourceFile = Path.Combine(_sourceDirectory, "test-assembly.dll");
        var testContent = new byte[1024 * 50]; // 50 KB
        await File.WriteAllBytesAsync(sourceFile, testContent);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourceFile,
            "test-assembly",
            PreparationItemType.Assembly,
            dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.FileCount.Should().Be(1);
        result.DirectoryCount.Should().Be(0);
        result.TotalSize.Should().Be(1024 * 50);
        result.SourcePath.Should().Be(sourceFile);
        result.CachePath.Should().NotBeNull();

        // Verify no files were copied
        var cacheDir = Path.Combine(_testDirectory, "cache");
        Directory.Exists(cacheDir).Should().BeFalse("dry-run should not create cache directory");

        // Verify manifest was not modified
        manifest.Items.Should().BeEmpty("dry-run should not modify manifest");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithDirectory_CalculatesCorrectMetrics()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        // Create a test directory structure with multiple files
        var sourcePackage = Path.Combine(_sourceDirectory, "com.example.package");
        Directory.CreateDirectory(sourcePackage);
        Directory.CreateDirectory(Path.Combine(sourcePackage, "Runtime"));
        Directory.CreateDirectory(Path.Combine(sourcePackage, "Editor"));

        // Create test files with known sizes
        await File.WriteAllBytesAsync(Path.Combine(sourcePackage, "package.json"), new byte[512]);
        await File.WriteAllBytesAsync(Path.Combine(sourcePackage, "Runtime", "Script1.cs"), new byte[2048]);
        await File.WriteAllBytesAsync(Path.Combine(sourcePackage, "Runtime", "Script2.cs"), new byte[3072]);
        await File.WriteAllBytesAsync(Path.Combine(sourcePackage, "Editor", "EditorScript.cs"), new byte[1024]);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourcePackage,
            "com.example.package",
            PreparationItemType.Package,
            dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.FileCount.Should().Be(4, "should count all files in directory tree");
        result.DirectoryCount.Should().Be(2, "should count Runtime and Editor subdirectories");
        result.TotalSize.Should().Be(512 + 2048 + 3072 + 1024, "should sum all file sizes");

        // Verify no files were copied
        var cachePath = Path.Combine(_testDirectory, "cache", "com.example.package");
        Directory.Exists(cachePath).Should().BeFalse("dry-run should not copy directory");

        // Verify manifest was not modified
        manifest.Items.Should().BeEmpty("dry-run should not modify manifest");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithLargeDirectory_CalculatesCorrectMetrics()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        // Create a larger directory structure (simulating a real Unity package)
        var sourcePackage = Path.Combine(_sourceDirectory, "com.cysharp.unitask");
        Directory.CreateDirectory(sourcePackage);

        var subdirs = new[] { "Runtime", "Editor", "Documentation~", "Tests" };
        foreach (var subdir in subdirs)
        {
            Directory.CreateDirectory(Path.Combine(sourcePackage, subdir));
        }

        // Create multiple files with varying sizes
        var totalSize = 0L;
        var fileCount = 0;

        // Runtime files
        for (int i = 0; i < 20; i++)
        {
            var size = 1024 * (i + 1); // 1KB, 2KB, 3KB, etc.
            await File.WriteAllBytesAsync(
                Path.Combine(sourcePackage, "Runtime", $"Script{i}.cs"),
                new byte[size]);
            totalSize += size;
            fileCount++;
        }

        // Editor files
        for (int i = 0; i < 5; i++)
        {
            var size = 2048 * (i + 1);
            await File.WriteAllBytesAsync(
                Path.Combine(sourcePackage, "Editor", $"EditorScript{i}.cs"),
                new byte[size]);
            totalSize += size;
            fileCount++;
        }

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourcePackage,
            "com.cysharp.unitask",
            PreparationItemType.Package,
            dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.FileCount.Should().Be(fileCount);
        result.DirectoryCount.Should().Be(4);
        result.TotalSize.Should().Be(totalSize);

        // Verify no files were copied
        var cachePath = Path.Combine(_testDirectory, "cache", "com.cysharp.unitask");
        Directory.Exists(cachePath).Should().BeFalse("dry-run should not copy directory");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithNonExistentSource_ReturnsError()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        var nonExistentPath = Path.Combine(_sourceDirectory, "does-not-exist.dll");

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            nonExistentPath,
            "test-assembly",
            PreparationItemType.Assembly,
            dryRun: true);

        // Assert
        result.Success.Should().BeFalse();
        result.DryRun.Should().BeFalse("should not set DryRun flag on error");
        result.ErrorMessage.Should().Contain("does not exist");

        // Verify manifest was not modified
        manifest.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithDuplicateCacheAs_ReturnsError()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache",
            Items = new List<PreparationItem>
            {
                new() { Source = "existing/source", CacheAs = "duplicate-name", Type = PreparationItemType.Package }
            }
        };

        var sourceFile = Path.Combine(_sourceDirectory, "test.dll");
        await File.WriteAllBytesAsync(sourceFile, new byte[1024]);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourceFile,
            "duplicate-name", // Same as existing item
            PreparationItemType.Assembly,
            dryRun: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");

        // Verify manifest was not modified (still has only 1 item)
        manifest.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddSourceAsync_DryRunFalse_ActuallyCopiesFiles()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        var sourceFile = Path.Combine(_sourceDirectory, "real-copy.dll");
        await File.WriteAllBytesAsync(sourceFile, new byte[2048]);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourceFile,
            "real-copy",
            PreparationItemType.Assembly,
            dryRun: false); // NOT a dry run

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeFalse();

        // Verify file WAS copied
        var cachePath = Path.Combine(_testDirectory, "cache", "real-copy");
        File.Exists(cachePath).Should().BeTrue("file should be copied when not dry-run");

        // Verify manifest WAS modified
        manifest.Items.Should().HaveCount(1);
        manifest.Items[0].CacheAs.Should().Be("real-copy");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_ReturnsCorrectPaths()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "build/preparation/cache"
        };

        var sourceFile = Path.Combine(_sourceDirectory, "test.dll");
        await File.WriteAllBytesAsync(sourceFile, new byte[1024]);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourceFile,
            "test-assembly",
            PreparationItemType.Assembly,
            dryRun: true);

        // Assert
        result.SourcePath.Should().Be(sourceFile);
        result.CacheRelativePath.Should().Be("build/preparation/cache/test-assembly");
        result.CachePath.Should().Contain("build").And.Contain("preparation").And.Contain("cache").And.Contain("test-assembly");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_WithEmptyDirectory_ReturnsZeroMetrics()
    {
        // Arrange
        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache"
        };

        var emptyDir = Path.Combine(_sourceDirectory, "empty-package");
        Directory.CreateDirectory(emptyDir);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            emptyDir,
            "empty-package",
            PreparationItemType.Package,
            dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.FileCount.Should().Be(0, "empty directory has no files");
        result.DirectoryCount.Should().Be(0, "empty directory has no subdirectories");
        result.TotalSize.Should().Be(0, "empty directory has zero size");
    }

    [Fact]
    public async Task AddSourceAsync_DryRun_PreservesManifestState()
    {
        // Arrange
        var originalItems = new List<PreparationItem>
        {
            new() { Source = "existing1", CacheAs = "pkg1", Type = PreparationItemType.Package },
            new() { Source = "existing2", CacheAs = "pkg2", Type = PreparationItemType.Assembly }
        };

        var manifest = new PreparationManifest
        {
            Id = "test",
            Title = "Test Manifest",
            CacheDirectory = "cache",
            Items = new List<PreparationItem>(originalItems) // Copy to detect changes
        };

        var sourceFile = Path.Combine(_sourceDirectory, "new-item.dll");
        await File.WriteAllBytesAsync(sourceFile, new byte[1024]);

        // Act
        var result = await _service.AddSourceAsync(
            manifest,
            sourceFile,
            "new-item",
            PreparationItemType.Assembly,
            dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();

        // Verify manifest state is unchanged
        manifest.Items.Should().HaveCount(2, "dry-run should not add new items");
        manifest.Items[0].CacheAs.Should().Be("pkg1");
        manifest.Items[1].CacheAs.Should().Be("pkg2");
    }
}
