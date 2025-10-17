using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

/// <summary>
/// Unit tests for TextPatcher.
/// </summary>
public class TextPatcherTests : IDisposable
{
    private readonly TextPatcher _patcher;
    private readonly string _testDir;

    public TextPatcherTests()
    {
        _patcher = new TextPatcher(NullLogger<TextPatcher>.Instance);
        _testDir = Path.Combine(Path.GetTempPath(), $"textpatcher-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region Replace Operation Tests

    [Fact]
    public async Task Replace_WithLiteralString_ShouldReplaceText()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "test.txt");
        var originalContent = "Hello World\nGoodbye World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = "World",
            Replace = "Universe",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello Universe\nGoodbye Universe");
    }

    [Fact]
    public async Task Replace_WithRegexPattern_ShouldReplaceMatches()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "version.txt");
        var originalContent = "Version: 1.0.0\nBuild: 12345";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = @"Version: \d+\.\d+\.\d+",
            Replace = "Version: 2.0.0",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("Version: 2.0.0");
        content.Should().Contain("Build: 12345");
    }

    [Fact]
    public async Task Replace_WithMultilineRegex_ShouldWork()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "multiline.txt");
        var originalContent = "Line 1\nLine 2\nLine 3\nLine 4";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = @"^Line \d+$",
            Replace = "Modified Line",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("Modified Line");
    }

    #endregion

    #region InsertBefore Operation Tests

    [Fact]
    public async Task InsertBefore_WithLiteralString_ShouldInsertText()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "insert.txt");
        var originalContent = "World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.InsertBefore,
            Search = "World",
            Replace = "Hello ",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello World");
    }

    [Fact]
    public async Task InsertBefore_WithRegex_ShouldInsertBeforeMatch()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "regex-insert.txt");
        var originalContent = "function test() { }";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.InsertBefore,
            Search = @"function \w+\(",
            Replace = "async ",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("async function test()");
    }

    #endregion

    #region InsertAfter Operation Tests

    [Fact]
    public async Task InsertAfter_WithLiteralString_ShouldInsertText()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "insert-after.txt");
        var originalContent = "Hello";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.InsertAfter,
            Search = "Hello",
            Replace = " World",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello World");
    }

    [Fact]
    public async Task InsertAfter_WithRegex_ShouldInsertAfterMatch()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "regex-insert-after.txt");
        var originalContent = "using System;";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.InsertAfter,
            Search = @"using System;$",
            Replace = "\nusing System.Linq;",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("using System;\nusing System.Linq;");
    }

    #endregion

    #region Delete Operation Tests

    [Fact]
    public async Task Delete_WithLiteralString_ShouldRemoveText()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "delete.txt");
        var originalContent = "Hello World\nGoodbye World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Delete,
            Search = " World",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello\nGoodbye");
    }

    [Fact]
    public async Task Delete_WithRegex_ShouldRemoveMatches()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "regex-delete.txt");
        var originalContent = "Line 1\nDEBUG: test\nLine 2\nDEBUG: another\nLine 3";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Delete,
            Search = @"DEBUG:.*\n",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().NotContain("DEBUG");
        content.Should().Contain("Line 1");
        content.Should().Contain("Line 2");
        content.Should().Contain("Line 3");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidatePatch_WhenTargetExists_ShouldReturnValid()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "validate.txt");
        var originalContent = "Hello World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Search = "World",
            File = testFile
        };

        // Act
        var result = await _patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        result.IsValid.Should().BeTrue();
        result.TargetFound.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePatch_WhenTargetMissing_ShouldReturnWarning()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "validate-missing.txt");
        var originalContent = "Hello World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Search = "NotFound",
            File = testFile
        };

        // Act
        var result = await _patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        result.TargetFound.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("not found"));
        // Note: IsValid is still true, it's just a warning
    }

    [Fact]
    public async Task ValidatePatch_WithRegexPattern_ShouldWork()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "validate-regex.txt");
        var originalContent = "Version: 1.2.3";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Search = @"Version: \d+\.\d+\.\d+",
            File = testFile
        };

        // Act
        var result = await _patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        result.IsValid.Should().BeTrue();
        result.TargetFound.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ApplyPatch_WithInvalidRegex_ShouldReturnFailure()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "invalid-regex.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = @"[\w+",  // Invalid regex - unclosed bracket
            Replace = "replacement",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        // The base class catches exceptions and returns failure result
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Error");
    }

    [Fact]
    public void ValidateRegexPattern_WithValidPattern_ShouldReturnTrue()
    {
        // Arrange & Act
        var result = TextPatcher.ValidateRegexPattern(@"\d+\.\d+\.\d+", out var errorMessage);

        // Assert
        result.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRegexPattern_WithInvalidPattern_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = TextPatcher.ValidateRegexPattern("[invalid", out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().NotBeNull();
        errorMessage.Should().Contain("Invalid regex pattern");
    }

    [Fact]
    public void ValidateRegexPattern_WithNullPattern_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = TextPatcher.ValidateRegexPattern(string.Empty, out var errorMessage);

        // Assert
        result.Should().BeFalse();
        errorMessage.Should().Contain("cannot be null or empty");
    }

    #endregion

    #region DryRun Tests

    [Fact]
    public async Task ApplyPatch_InDryRunMode_ShouldNotModifyFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "dryrun.txt");
        var originalContent = "Hello World";
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = "World",
            Replace = "Universe",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch, dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeTrue();
        result.Preview.Should().NotBeNullOrEmpty();
        result.Preview.Should().Contain("Patch Preview");
        
        var actualContent = await File.ReadAllTextAsync(testFile);
        actualContent.Should().Be(originalContent); // File should not be modified
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ApplyPatch_WithEmptyFile_ShouldHandleGracefully()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "empty.txt");
        await File.WriteAllTextAsync(testFile, string.Empty);

        var patch = new CodePatch
        {
            Type = PatchType.Text,
            Mode = PatchMode.Replace,
            Search = "anything",
            Replace = "something",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeFalse(); // No changes made
    }

    #endregion
}
