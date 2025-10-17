using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Utilities;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Utilities;

/// <summary>
/// Tests for PathResolver.
/// </summary>
public class PathResolverTests
{
    private readonly Mock<ILogger<GitHelper>> _gitLoggerMock;
    private readonly Mock<ILogger<PathResolver>> _pathLoggerMock;
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
    private readonly string _gitRoot;

    public PathResolverTests()
    {
        _gitLoggerMock = new Mock<ILogger<GitHelper>>();
        _pathLoggerMock = new Mock<ILogger<PathResolver>>();
        _gitHelper = new GitHelper(_gitLoggerMock.Object);
        _pathResolver = new PathResolver(_gitHelper, _pathLoggerMock.Object);
        _gitRoot = _gitHelper.DetectGitRoot();
    }

    [Fact]
    public void GitRoot_ShouldReturnGitRepositoryRoot()
    {
        // Act
        var gitRoot = _pathResolver.GitRoot;

        // Assert
        gitRoot.Should().NotBeNullOrEmpty();
        gitRoot.Should().Be(_gitRoot);
        Path.IsPathRooted(gitRoot).Should().BeTrue();
    }

    [Fact]
    public void Resolve_ShouldResolveRelativePath()
    {
        // Arrange
        var relativePath = "build/preparation/configs/config.json";

        // Act
        var absolutePath = _pathResolver.Resolve(relativePath);

        // Assert
        absolutePath.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(absolutePath).Should().BeTrue();
        absolutePath.Should().StartWith(_gitRoot);
        absolutePath.Should().Contain("build");
        absolutePath.Should().Contain("preparation");
        absolutePath.Should().Contain("configs");
        absolutePath.Should().EndWith("config.json");
    }

    [Fact]
    public void Resolve_ShouldHandleForwardSlashes()
    {
        // Arrange
        var relativePath = "build/preparation/config.json";

        // Act
        var absolutePath = _pathResolver.Resolve(relativePath);

        // Assert
        absolutePath.Should().Contain(Path.DirectorySeparatorChar.ToString());
    }

    [Fact]
    public void Resolve_ShouldHandleBackslashes()
    {
        // Arrange
        var relativePath = "build\\preparation\\config.json";

        // Act
        var absolutePath = _pathResolver.Resolve(relativePath);

        // Assert
        absolutePath.Should().Contain(Path.DirectorySeparatorChar.ToString());
    }

    [Fact]
    public void Resolve_ShouldReturnAbsolutePath_WhenGivenAbsolutePath()
    {
        // Arrange
        var absolutePath = Path.Combine(_gitRoot, "build", "config.json");

        // Act
        var result = _pathResolver.Resolve(absolutePath);

        // Assert
        result.Should().Be(Path.GetFullPath(absolutePath));
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenPathIsNull()
    {
        // Act & Assert
        var act = () => _pathResolver.Resolve(null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null*");
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenPathIsEmpty()
    {
        // Act & Assert
        var act = () => _pathResolver.Resolve("");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null*");
    }

    [Fact]
    public void MakeRelative_ShouldConvertAbsolutePathToRelative()
    {
        // Arrange
        var absolutePath = Path.Combine(_gitRoot, "build", "preparation", "config.json");

        // Act
        var relativePath = _pathResolver.MakeRelative(absolutePath);

        // Assert
        relativePath.Should().NotBeNullOrEmpty();
        Path.IsPathRooted(relativePath).Should().BeFalse();
        relativePath.Should().Contain("build");
        relativePath.Should().Contain("preparation");
        relativePath.Should().Contain("config.json");
    }

    [Fact]
    public void MakeRelative_ShouldUseForwardSlashes()
    {
        // Arrange
        var absolutePath = Path.Combine(_gitRoot, "build", "preparation", "config.json");

        // Act
        var relativePath = _pathResolver.MakeRelative(absolutePath);

        // Assert
        relativePath.Should().Contain("/");
        relativePath.Should().NotContain("\\");
    }

    [Fact]
    public void MakeRelative_ShouldThrow_WhenPathOutsideGitRoot()
    {
        // Arrange
        var outsidePath = Path.GetTempPath();

        // Act & Assert
        var act = () => _pathResolver.MakeRelative(outsidePath);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*outside git repository root*");
    }

    [Fact]
    public void MakeRelative_ShouldThrow_WhenPathIsNull()
    {
        // Act & Assert
        var act = () => _pathResolver.MakeRelative(null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null*");
    }

    [Fact]
    public void Resolve_And_MakeRelative_ShouldBeReversible()
    {
        // Arrange
        var originalRelativePath = "build/preparation/config.json";

        // Act
        var absolutePath = _pathResolver.Resolve(originalRelativePath);
        var backToRelative = _pathResolver.MakeRelative(absolutePath);

        // Assert
        // Normalize for comparison (forward slashes)
        var normalizedOriginal = originalRelativePath.Replace('\\', '/');
        backToRelative.Should().Be(normalizedOriginal);
    }

    [Fact]
    public void FileExists_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange - use .gitignore or README.md which should exist in git root
        var testFilePath = ".gitignore";

        // Act
        var exists = _pathResolver.FileExists(testFilePath);

        // Assert
        exists.Should().BeTrue("a .gitignore file should exist in the git repository root");
    }

    [Fact]
    public void FileExists_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = "build/nonexistent/file.json";

        // Act
        var exists = _pathResolver.FileExists(nonExistentPath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void DirectoryExists_ShouldReturnTrue_WhenDirectoryExists()
    {
        // Arrange - use git root itself
        var rootPath = ".";

        // Act
        var exists = _pathResolver.DirectoryExists(rootPath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void DirectoryExists_ShouldReturnFalse_WhenDirectoryDoesNotExist()
    {
        // Arrange
        var nonExistentPath = "build/nonexistent/directory";

        // Act
        var exists = _pathResolver.DirectoryExists(nonExistentPath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void Exists_ShouldReturnTrue_ForExistingFile()
    {
        // Arrange - use .gitignore which should exist in git root
        var testFilePath = ".gitignore";

        // Act
        var exists = _pathResolver.Exists(testFilePath);

        // Assert
        exists.Should().BeTrue("a .gitignore file should exist in the git repository root");
    }

    [Fact]
    public void Exists_ShouldReturnTrue_ForExistingDirectory()
    {
        // Arrange
        var rootPath = ".";

        // Act
        var exists = _pathResolver.Exists(rootPath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void EnsureDirectory_ShouldCreateDirectory_WhenDoesNotExist()
    {
        // Arrange
        var testDirPath = $"build/test-{Guid.NewGuid()}";
        var absolutePath = _pathResolver.Resolve(testDirPath);

        try
        {
            // Act
            var result = _pathResolver.EnsureDirectory(testDirPath);

            // Assert
            result.Should().Be(absolutePath);
            Directory.Exists(result).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(absolutePath))
            {
                Directory.Delete(absolutePath, recursive: true);
            }
        }
    }

    [Fact]
    public void EnsureDirectory_ShouldNotThrow_WhenDirectoryAlreadyExists()
    {
        // Arrange
        var rootPath = ".";

        // Act
        var act = () => _pathResolver.EnsureDirectory(rootPath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateWithinGitRoot_ShouldNotThrow_WhenPathWithinGitRoot()
    {
        // Arrange
        var validPath = "build/preparation/config.json";

        // Act
        var act = () => _pathResolver.ValidateWithinGitRoot(validPath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateWithinGitRoot_ShouldThrow_WhenPathOutsideGitRoot()
    {
        // Arrange
        var outsidePath = Path.GetTempPath();

        // Act & Assert
        var act = () => _pathResolver.ValidateWithinGitRoot(outsidePath);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*outside git repository root*");
    }
}
