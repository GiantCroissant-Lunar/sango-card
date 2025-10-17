using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Utilities;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Utilities;

/// <summary>
/// Tests for GitHelper.
/// </summary>
public class GitHelperTests
{
    private readonly Mock<ILogger<GitHelper>> _loggerMock;
    private readonly GitHelper _gitHelper;

    public GitHelperTests()
    {
        _loggerMock = new Mock<ILogger<GitHelper>>();
        _gitHelper = new GitHelper(_loggerMock.Object);
    }

    [Fact]
    public void DetectGitRoot_ShouldFindGitRoot_WhenInGitRepository()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var gitRoot = _gitHelper.DetectGitRoot(currentDir);

        // Assert
        gitRoot.Should().NotBeNullOrEmpty();
        Directory.Exists(gitRoot).Should().BeTrue();

        // Should have .git directory or file
        var gitPath = Path.Combine(gitRoot, ".git");
        (Directory.Exists(gitPath) || File.Exists(gitPath)).Should().BeTrue();
    }

    [Fact]
    public void DetectGitRoot_ShouldCacheResult()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var gitRoot1 = _gitHelper.DetectGitRoot(currentDir);
        var gitRoot2 = _gitHelper.DetectGitRoot(currentDir);

        // Assert
        gitRoot1.Should().Be(gitRoot2);

        // Should log cache usage on second call
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cached")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void DetectGitRoot_ShouldThrow_WhenNotInGitRepository()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _gitHelper.ClearCache();

        // Act & Assert
        var act = () => _gitHelper.DetectGitRoot(tempDir);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Git repository root not found*");
    }

    [Fact]
    public void IsInGitRepository_ShouldReturnTrue_WhenInGitRepository()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var result = _gitHelper.IsInGitRepository(currentDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInGitRepository_ShouldReturnFalse_WhenNotInGitRepository()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _gitHelper.ClearCache();

        // Act
        var result = _gitHelper.IsInGitRepository(tempDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ClearCache_ShouldClearCachedGitRoot()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var gitRoot1 = _gitHelper.DetectGitRoot(currentDir);

        // Act
        _gitHelper.ClearCache();
        var gitRoot2 = _gitHelper.DetectGitRoot(currentDir);

        // Assert
        gitRoot1.Should().Be(gitRoot2); // Same result

        // Should not log cache usage after clear
        _loggerMock.Invocations.Clear();
        _gitHelper.DetectGitRoot(currentDir);

        // Should use cached value now
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cached")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void DetectGitRoot_ShouldReturnAbsolutePath()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var gitRoot = _gitHelper.DetectGitRoot(currentDir);

        // Assert
        Path.IsPathRooted(gitRoot).Should().BeTrue();
        gitRoot.Should().Be(Path.GetFullPath(gitRoot));
    }
}
