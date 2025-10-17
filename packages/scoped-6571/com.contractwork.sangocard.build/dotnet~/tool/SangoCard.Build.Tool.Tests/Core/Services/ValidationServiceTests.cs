using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Services;
using SangoCard.Build.Tool.Core.Utilities;
using SangoCard.Build.Tool.Messages;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Services;

/// <summary>
/// Tests for ValidationService.
/// </summary>
public class ValidationServiceTests : IDisposable
{
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
    private readonly ValidationService _validationService;
    private readonly Mock<IPublisher<ValidationStartedMessage>> _validationStartedPublisherMock;
    private readonly Mock<IPublisher<ValidationCompletedMessage>> _validationCompletedPublisherMock;
    private readonly Mock<IPublisher<ValidationErrorFoundMessage>> _validationErrorFoundPublisherMock;
    private readonly Mock<IPublisher<ValidationWarningFoundMessage>> _validationWarningFoundPublisherMock;
    private readonly string _testDir;

    public ValidationServiceTests()
    {
        var gitLoggerMock = new Mock<ILogger<GitHelper>>();
        var pathLoggerMock = new Mock<ILogger<PathResolver>>();
        var validationLoggerMock = new Mock<ILogger<ValidationService>>();

        _gitHelper = new GitHelper(gitLoggerMock.Object);
        _pathResolver = new PathResolver(_gitHelper, pathLoggerMock.Object);

        _validationStartedPublisherMock = new Mock<IPublisher<ValidationStartedMessage>>();
        _validationCompletedPublisherMock = new Mock<IPublisher<ValidationCompletedMessage>>();
        _validationErrorFoundPublisherMock = new Mock<IPublisher<ValidationErrorFoundMessage>>();
        _validationWarningFoundPublisherMock = new Mock<IPublisher<ValidationWarningFoundMessage>>();

        _validationService = new ValidationService(
            _pathResolver,
            validationLoggerMock.Object,
            _validationStartedPublisherMock.Object,
            _validationCompletedPublisherMock.Object,
            _validationErrorFoundPublisherMock.Object,
            _validationWarningFoundPublisherMock.Object
        );

        // Create test directory within git root
        var gitRoot = _gitHelper.DetectGitRoot();
        _testDir = Path.Combine(gitRoot, "build", "test-temp", $"validation-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Validate_Schema_ShouldPassForValidConfig()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "com.unity.addressables",
                    Version = "1.21.2",
                    Source = "build/cache/package.tgz",
                    Target = "projects/client/Packages/package.tgz"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Schema);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Level.Should().Be(ValidationLevel.Schema);
        result.Errors.Should().BeEmpty();
        result.Summary.Should().Contain("passed");

        // Verify messages published
        _validationStartedPublisherMock.Verify(
            p => p.Publish(It.Is<ValidationStartedMessage>(m => m.Level == ValidationLevel.Schema)),
            Times.Once
        );
        _validationCompletedPublisherMock.Verify(
            p => p.Publish(It.IsAny<ValidationCompletedMessage>()),
            Times.Once
        );
    }

    [Fact]
    public void Validate_Schema_ShouldFailForMissingVersion()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "" // Missing version
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "SCHEMA001");
    }

    [Fact]
    public void Validate_Schema_ShouldFailForMissingPackageFields()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new() { Name = "", Version = "", Source = "", Target = "" }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(0);
        result.Errors.Should().Contain(e => e.Code == "SCHEMA002"); // Missing name
    }

    [Fact]
    public void Validate_FileExistence_ShouldPassWhenFilesExist()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.tgz");
        File.WriteAllText(testFile, "test content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "test.package",
                    Version = "1.0.0",
                    Source = _pathResolver.MakeRelative(testFile),
                    Target = "projects/client/Packages/test.tgz"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.FileExistence);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Level.Should().Be(ValidationLevel.FileExistence);
    }

    [Fact]
    public void Validate_FileExistence_ShouldFailWhenFilesDoNotExist()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "test.package",
                    Version = "1.0.0",
                    Source = "build/nonexistent/package.tgz",
                    Target = "projects/client/Packages/package.tgz"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.FileExistence);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "FILE001");
    }

    [Fact]
    public void Validate_FileExistence_ShouldWarnForOptionalPatchFileNotFound()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches = new List<CodePatch>
            {
                new()
                {
                    File = "build/nonexistent/file.cs",
                    Type = PatchType.CSharp,
                    Search = "test",
                    Replace = "modified",
                    Optional = true
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.FileExistence);

        // Assert
        result.IsValid.Should().BeTrue(); // Should pass with warnings
        result.Warnings.Should().ContainSingle(w => w.Code == "FILE003");
    }

    [Fact]
    public void Validate_UnityPackages_ShouldPassForValidPackages()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "com.unity.addressables-1.21.2.tgz");
        File.WriteAllText(testFile, "package content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "com.unity.addressables",
                    Version = "1.21.2",
                    Source = _pathResolver.MakeRelative(testFile),
                    Target = "projects/client/Packages/package.tgz"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.UnityPackages);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnityPackages_ShouldFailForNonTgzFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "package.zip");
        File.WriteAllText(testFile, "package content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "test.package",
                    Version = "1.0.0",
                    Source = _pathResolver.MakeRelative(testFile),
                    Target = "projects/client/Packages/package.zip"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.UnityPackages);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PKG001");
    }

    [Fact]
    public void Validate_UnityPackages_ShouldFailForEmptyFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "empty.tgz");
        File.WriteAllText(testFile, ""); // Empty file

        var config = new PreparationConfig
        {
            Version = "1.0",
            Packages = new List<UnityPackageReference>
            {
                new()
                {
                    Name = "test.package",
                    Version = "1.0.0",
                    Source = _pathResolver.MakeRelative(testFile),
                    Target = "projects/client/Packages/empty.tgz"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.UnityPackages);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PKG002");
    }

    [Fact]
    public void Validate_Full_ShouldValidateCodePatches()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "Test.cs");
        File.WriteAllText(testFile, "public class Test { }");

        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches = new List<CodePatch>
            {
                new()
                {
                    File = _pathResolver.MakeRelative(testFile),
                    Type = PatchType.CSharp,
                    Search = "public class Test",
                    Replace = "public class TestModified"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Full);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Full_ShouldWarnForTypeMismatch()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "Test.json");
        File.WriteAllText(testFile, "{}");

        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches = new List<CodePatch>
            {
                new()
                {
                    File = _pathResolver.MakeRelative(testFile),
                    Type = PatchType.CSharp, // Mismatch: file is .json but type is CSharp
                    Search = "test",
                    Replace = "modified"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Full);

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't fail validation
        result.Warnings.Should().ContainSingle(w => w.Code == "PATCH001");
    }

    [Fact]
    public void Validate_Full_ShouldFailForInvalidRegex()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "Test.txt");
        File.WriteAllText(testFile, "test content");

        var config = new PreparationConfig
        {
            Version = "1.0",
            CodePatches = new List<CodePatch>
            {
                new()
                {
                    File = _pathResolver.MakeRelative(testFile),
                    Type = PatchType.Text,
                    Search = ".*[invalid(regex", // Invalid regex with .*
                    Replace = "modified"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Full);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PATCH002");
    }

    [Fact]
    public void Validate_ShouldPublishErrorMessages()
    {
        // Arrange
        var config = new PreparationConfig
        {
            Version = "" // Will cause error
        };

        // Act
        _validationService.Validate(config, ValidationLevel.Schema);

        // Assert
        _validationErrorFoundPublisherMock.Verify(
            p => p.Publish(It.IsAny<ValidationErrorFoundMessage>()),
            Times.AtLeastOnce
        );
    }

    [Fact]
    public void Validate_ShouldIncludeSummary()
    {
        // Arrange
        var config = new PreparationConfig { Version = "1.0" };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Schema);

        // Assert
        result.Summary.Should().NotBeNullOrEmpty();
        result.Summary.Should().Contain("passed");
    }

    [Fact]
    public void Validate_TotalIssues_ShouldIncludeErrorsAndWarnings()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "Test.json");
        File.WriteAllText(testFile, "{}");

        var config = new PreparationConfig
        {
            Version = "", // Error
            CodePatches = new List<CodePatch>
            {
                new()
                {
                    File = _pathResolver.MakeRelative(testFile),
                    Type = PatchType.CSharp, // Warning: type mismatch
                    Search = "test",
                    Replace = "modified"
                }
            }
        };

        // Act
        var result = _validationService.Validate(config, ValidationLevel.Full);

        // Assert
        result.TotalIssues.Should().BeGreaterThan(0);
        result.TotalIssues.Should().Be(result.Errors.Count + result.Warnings.Count);
    }
}
