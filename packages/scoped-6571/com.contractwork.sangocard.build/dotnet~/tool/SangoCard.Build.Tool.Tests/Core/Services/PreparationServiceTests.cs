using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

public class PreparationServiceTests : IDisposable
{
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
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

        // Prepare patchers
        var textPatcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var jsonPatcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var csPatcher = new CSharpPatcher(new Mock<ILogger<CSharpPatcher>>().Object);
        var unityPatcher = new UnityAssetPatcher(new Mock<ILogger<UnityAssetPatcher>>().Object);

        _prepService = new PreparationService(
            _pathResolver,
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
    public async Task RunAsync_ShouldCopyPackagesAndAssemblies()
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
        var summary = await _prepService.RunAsync(config);

        // Assert
        File.Exists(pkgTarget).Should().BeTrue();
        File.Exists(asmTarget).Should().BeTrue();
        summary.Copied.Should().BeGreaterOrEqualTo(2);
        _prepStarted.Verify(p => p.Publish(It.IsAny<PreparationStartedMessage>()), Times.Once);
        _prepCompleted.Verify(p => p.Publish(It.IsAny<PreparationCompletedMessage>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPerformAssetManipulations()
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
        var summary = await _prepService.RunAsync(config);

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
    public async Task RunAsync_ShouldApplyTextPatch()
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
        var summary = await _prepService.RunAsync(config);

        // Assert
        var content = await File.ReadAllTextAsync(targetFile);
        content.Should().Be("Hello Universe");
        summary.Patched.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task RunAsync_ShouldSkipOptionalPatchWhenFileMissing()
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
        var summary = await _prepService.RunAsync(config);

        // Assert
        summary.Patched.Should().Be(0);
    }
}
