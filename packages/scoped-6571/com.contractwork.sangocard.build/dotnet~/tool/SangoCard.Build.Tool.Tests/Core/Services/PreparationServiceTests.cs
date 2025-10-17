using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using System.IO.Compression;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

public class PreparationServiceTests : IDisposable
{
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
    private readonly Mock<ValidationService> _validationService;
    private readonly PreparationService _prepService;
    private readonly string _testDir;
    private readonly string _srcDir;
    private readonly string _dstDir;

    private readonly Mock<IPublisher<PreparationStartedMessage>> _prepStarted = new();
    private readonly Mock<IPublisher<PreparationCompletedMessage>> _prepCompleted = new();
    private readonly Mock<IPublisher<FileCopiedMessage>> _fileCopied = new();
    private readonly Mock<IPublisher<FileMovedMessage>> _fileMoved = new();
    private readonly Mock<IPublisher<FileDeletedMessage>> _fileDeleted = new();

    public PreparationServiceTests()
    {
        var gitLogger = new Mock<ILogger<GitHelper>>();
        var pathLogger = new Mock<ILogger<PathResolver>>();
        var prepLogger = new Mock<ILogger<PreparationService>>();

        _gitHelper = new GitHelper(gitLogger.Object);
        _pathResolver = new PathResolver(_gitHelper, pathLogger.Object);

        // Mock ValidationService
        _validationService = new Mock<ValidationService>(
            MockBehavior.Loose,
            _pathResolver,
            Mock.Of<ILogger<ValidationService>>(),
            Mock.Of<IPublisher<ValidationStartedMessage>>(),
            Mock.Of<IPublisher<ValidationCompletedMessage>>(),
            Mock.Of<IPublisher<ValidationErrorFoundMessage>>(),
            Mock.Of<IPublisher<ValidationWarningFoundMessage>>());

        // Prepare patchers
        var textPatcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var jsonPatcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var csPatcher = new CSharpPatcher(new Mock<ILogger<CSharpPatcher>>().Object);
        var unityPatcher = new UnityAssetPatcher(new Mock<ILogger<UnityAssetPatcher>>().Object);

        _prepService = new PreparationService(
            _pathResolver,
            _validationService.Object,
            new[] { (IPatcher)textPatcher, jsonPatcher, csPatcher, unityPatcher },
            prepLogger.Object,
            _prepStarted.Object,
            _prepCompleted.Object,
            _fileCopied.Object,
            _fileMoved.Object,
            _fileDeleted.Object
        );

        var gitRoot = _gitHelper.DetectGitRoot();
        _testDir = Path.Combine(gitRoot, "build", "test-temp", $"prep-test-{Guid.NewGuid()}");
        _srcDir = Path.Combine(_testDir, "src");
        _dstDir = Path.Combine(_testDir, "dst");
        Directory.CreateDirectory(_srcDir);
        Directory.CreateDirectory(_dstDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCopyPackagesAndAssemblies()
    {
        // Arrange
        var pkgSrc = Path.Combine(_srcDir, "com.example.pkg-1.0.0.tgz");
        var asmSrc = Path.Combine(_srcDir, "Example.dll");
        await File.WriteAllTextAsync(pkgSrc, "pkg");
        await File.WriteAllTextAsync(asmSrc, "dll");

        var pkgTarget = Path.Combine(_dstDir, "Packages", Path.GetFileName(pkgSrc));
        var asmTarget = Path.Combine(_dstDir, "Plugins", Path.GetFileName(asmSrc));

        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages =
            {
                new UnityPackageReference
                {
                    Name = "com.example.pkg",
                    Version = "1.0.0",
                    Source = _pathResolver.MakeRelative(pkgSrc),
                    Target = _pathResolver.MakeRelative(pkgTarget)
                }
            },
            Assemblies =
            {
                new AssemblyReference
                {
                    Name = "Example",
                    Version = "1.0.0",
                    Source = _pathResolver.MakeRelative(asmSrc),
                    Target = _pathResolver.MakeRelative(asmTarget)
                }
            }
        };

        // Act
        var summary = await _prepService.ExecuteAsync(config, validate: false);

        // Assert
        File.Exists(pkgTarget).Should().BeTrue();
        File.Exists(asmTarget).Should().BeTrue();
        summary.Copied.Should().BeGreaterOrEqualTo(2);
        _prepStarted.Verify(p => p.Publish(It.IsAny<PreparationStartedMessage>()), Times.Once);
        _prepCompleted.Verify(p => p.Publish(It.IsAny<PreparationCompletedMessage>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPerformAssetManipulations()
    {
        // Arrange
        var sourceFile = Path.Combine(_srcDir, "a.txt");
        await File.WriteAllTextAsync(sourceFile, "a");
        var copyTarget = Path.Combine(_dstDir, "b.txt");
        var moveTarget = Path.Combine(_dstDir, "c.txt");
        var deleteTarget = Path.Combine(_dstDir, "d.txt");
        await File.WriteAllTextAsync(deleteTarget, "del");

        var config = new PreparationConfig
        {
            Version = "1.0",
            AssetManipulations =
            {
                new AssetManipulation { Operation = AssetOperation.Copy, Source = _pathResolver.MakeRelative(sourceFile), Target = _pathResolver.MakeRelative(copyTarget), Overwrite = true },
                new AssetManipulation { Operation = AssetOperation.Move, Source = _pathResolver.MakeRelative(copyTarget), Target = _pathResolver.MakeRelative(moveTarget), Overwrite = true },
                new AssetManipulation { Operation = AssetOperation.Delete, Target = _pathResolver.MakeRelative(deleteTarget) }
            }
        };

        // Act
        var summary = await _prepService.ExecuteAsync(config, validate: false);

        // Assert
        File.Exists(copyTarget).Should().BeFalse();
        File.Exists(moveTarget).Should().BeTrue();
        File.Exists(deleteTarget).Should().BeFalse();
        summary.Copied.Should().BeGreaterOrEqualTo(1);
        summary.Moved.Should().BeGreaterOrEqualTo(1);
        summary.Deleted.Should().BeGreaterOrEqualTo(1);
        _fileCopied.Verify(p => p.Publish(It.IsAny<FileCopiedMessage>()), Times.AtLeastOnce);
        _fileMoved.Verify(p => p.Publish(It.IsAny<FileMovedMessage>()), Times.AtLeastOnce);
        _fileDeleted.Verify(p => p.Publish(It.IsAny<FileDeletedMessage>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApplyTextPatch()
    {
        // Arrange
        var targetFile = Path.Combine(_dstDir, "x.txt");
        await File.WriteAllTextAsync(targetFile, "Hello World");

        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches =
            {
                new CodePatch
                {
                    File = _pathResolver.MakeRelative(targetFile),
                    Type = PatchType.Text,
                    Search = "World",
                    Replace = "Universe",
                    Mode = PatchMode.Replace
                }
            }
        };

        // Act
        var summary = await _prepService.ExecuteAsync(config, validate: false);

        // Assert
        var content = await File.ReadAllTextAsync(targetFile);
        content.Should().Be("Hello Universe");
        summary.Patched.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipOptionalPatchWhenFileMissing()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches =
            {
                new CodePatch
                {
                    File = _pathResolver.MakeRelative(Path.Combine(_dstDir, "missing.txt")),
                    Type = PatchType.Text,
                    Search = "abc",
                    Replace = "xyz",
                    Mode = PatchMode.Replace,
                    Optional = true
                }
            }
        };

        // Act
        var summary = await _prepService.ExecuteAsync(config, validate: false);

        // Assert
        summary.Patched.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidation_ShouldValidateConfig()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0"
        };

        var validationResult = new ValidationResult
        {
            IsValid = true,
            Summary = "All validations passed"
        };

        _validationService
            .Setup(v => v.Validate(config, ValidationLevel.Full))
            .Returns(validationResult);

        // Act
        await _prepService.ExecuteAsync(config, validate: true);

        // Assert
        _validationService.Verify(v => v.Validate(config, ValidationLevel.Full), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedValidation_ShouldThrow()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0"
        };

        var validationResult = new ValidationResult
        {
            IsValid = false,
            Summary = "Validation failed with 5 errors"
        };

        _validationService
            .Setup(v => v.Validate(config, ValidationLevel.Full))
            .Returns(validationResult);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _prepService.ExecuteAsync(config, validate: true));
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRun_ShouldNotModifyFiles()
    {
        // Arrange
        var sourceFile = Path.Combine(_srcDir, "test.txt");
        var targetFile = Path.Combine(_dstDir, "test.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            AssetManipulations =
            {
                new AssetManipulation
                {
                    Operation = AssetOperation.Copy,
                    Source = _pathResolver.MakeRelative(sourceFile),
                    Target = _pathResolver.MakeRelative(targetFile),
                    Overwrite = true
                }
            }
        };

        // Act
        await _prepService.ExecuteAsync(config, validate: false, dryRun: true);

        // Assert
        File.Exists(targetFile).Should().BeFalse("File should not be created in dry-run mode");
        _fileCopied.Verify(p => p.Publish(It.IsAny<FileCopiedMessage>()), Times.Once,
            "Messages should still be published in dry-run mode");
    }

    [Fact]
    public async Task ExecuteAsync_OnError_ShouldRollback()
    {
        // Arrange
        var sourceFile = Path.Combine(_srcDir, "source.txt");
        var targetFile = Path.Combine(_dstDir, "target.txt");
        await File.WriteAllTextAsync(sourceFile, "new content");
        await File.WriteAllTextAsync(targetFile, "original content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            AssetManipulations =
            {
                new AssetManipulation
                {
                    Operation = AssetOperation.Copy,
                    Source = _pathResolver.MakeRelative(sourceFile),
                    Target = _pathResolver.MakeRelative(targetFile),
                    Overwrite = true
                },
                // This will cause an error
                new AssetManipulation
                {
                    Operation = AssetOperation.Copy,
                    Source = _pathResolver.MakeRelative(Path.Combine(_srcDir, "nonexistent.txt")),
                    Target = _pathResolver.MakeRelative(Path.Combine(_dstDir, "other.txt")),
                    Overwrite = true
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await _prepService.ExecuteAsync(config, validate: false));

        // Verify rollback - original content should be restored
        var content = await File.ReadAllTextAsync(targetFile);
        content.Should().Be("original content", "File should be rolled back to original state");
    }

    [Fact]
    public async Task RestoreAsync_ShouldRestoreFromBackup()
    {
        // Arrange
        var testFile = Path.Combine(_dstDir, "important.txt");
        await File.WriteAllTextAsync(testFile, "original data");

        // Manually create a backup to test restore functionality
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(Path.GetTempPath(), $"build_prep_backup_test_{timestamp}");
        Directory.CreateDirectory(backupPath);

        var archivePath = Path.Combine(backupPath, "backup.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            var relativePath = _pathResolver.MakeRelative(testFile);
            archive.CreateEntryFromFile(testFile, relativePath);
        }

        // Delete the original file
        File.Delete(testFile);
        File.Exists(testFile).Should().BeFalse("File should be deleted");

        // Act - Restore
        await _prepService.RestoreAsync(backupPath);

        // Assert
        File.Exists(testFile).Should().BeTrue("File should be restored");
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("original data", "Content should match original");

        // Cleanup
        if (Directory.Exists(backupPath))
        {
            Directory.Delete(backupPath, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteInReasonableTime()
    {
        // Arrange - Create 20 small files
        for (int i = 0; i < 20; i++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(_srcDir, $"file{i}.txt"),
                $"content {i}");
        }

        var config = new PreparationConfig
        {
            Version = "1.0",
            AssetManipulations = Enumerable.Range(0, 20).Select(i => new AssetManipulation
            {
                Operation = AssetOperation.Copy,
                Source = _pathResolver.MakeRelative(Path.Combine(_srcDir, $"file{i}.txt")),
                Target = _pathResolver.MakeRelative(Path.Combine(_dstDir, $"file{i}.txt")),
                Overwrite = true
            }).ToList()
        };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _prepService.ExecuteAsync(config, validate: false);
        sw.Stop();

        // Assert - Should complete well under 30 seconds
        sw.Elapsed.TotalSeconds.Should().BeLessThan(10,
            "20 file operations should complete in under 10 seconds");
    }
}
