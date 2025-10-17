using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

/// <summary>
/// Tests for all patcher implementations.
/// </summary>
public class PatcherTests : IDisposable
{
    private readonly string _testDir;

    public PatcherTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"patcher-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region TextPatcher Tests

    [Fact]
    public async Task TextPatcher_Replace_ShouldReplaceText()
    {
        // Arrange
        var patcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        var patch = new CodePatch
        {
            Search = "World",
            Replace = "Universe",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello Universe");
    }

    [Fact]
    public async Task TextPatcher_InsertBefore_ShouldInsertText()
    {
        // Arrange
        var patcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "World");

        var patch = new CodePatch
        {
            Search = "World",
            Replace = "Hello ",
            Mode = PatchMode.InsertBefore
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Hello World");
    }

    [Fact]
    public async Task TextPatcher_Regex_ShouldWork()
    {
        // Arrange
        var patcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Version: 1.0.0");

        var patch = new CodePatch
        {
            Search = @"Version: \d+\.\d+\.\d+",
            Replace = "Version: 2.0.0",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("Version: 2.0.0");
    }

    [Fact]
    public async Task TextPatcher_CanApplyPatch_ShouldReturnTrue_WhenPatternExists()
    {
        // Arrange
        var patcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "Hello World");

        var patch = new CodePatch { Search = "World" };

        // Act
        var canApply = await patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        canApply.IsValid.Should().Be(true);
    }

    #endregion

    #region JsonPatcher Tests

    [Fact]
    public async Task JsonPatcher_Replace_ShouldReplaceValue()
    {
        // Arrange
        var patcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.json");
        await File.WriteAllTextAsync(testFile, @"{""name"": ""old"", ""version"": ""1.0.0""}");

        var patch = new CodePatch
        {
            Search = "name",
            Replace = @"""new""",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain(@"""name"": ""new""");
    }

    [Fact]
    public async Task JsonPatcher_Delete_ShouldRemoveProperty()
    {
        // Arrange
        var patcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.json");
        await File.WriteAllTextAsync(testFile, @"{""name"": ""test"", ""version"": ""1.0.0""}");

        var patch = new CodePatch
        {
            Search = "version",
            Mode = PatchMode.Delete
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().NotContain("version");
    }

    [Fact]
    public async Task JsonPatcher_CanApplyPatch_ShouldReturnTrue_WhenPathExists()
    {
        // Arrange
        var patcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "test.json");
        await File.WriteAllTextAsync(testFile, @"{""name"": ""test""}");

        var patch = new CodePatch { Search = "name" };

        // Act
        var canApply = await patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        canApply.IsValid.Should().Be(true);
    }

    #endregion

    #region CSharpPatcher Tests

    [Fact]
    public async Task CSharpPatcher_Replace_ShouldReplaceCode()
    {
        // Arrange
        var patcher = new CSharpPatcher(new Mock<ILogger<CSharpPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "Test.cs");
        await File.WriteAllTextAsync(testFile, "public class Test { }");

        var patch = new CodePatch
        {
            Search = "public class Test",
            Replace = "public class TestModified",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("public class TestModified");
    }

    [Fact]
    public async Task CSharpPatcher_ShouldFailForInvalidSyntax()
    {
        // Arrange
        var patcher = new CSharpPatcher(new Mock<ILogger<CSharpPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "Test.cs");
        await File.WriteAllTextAsync(testFile, "public class Test { }");

        var patch = new CodePatch
        {
            Search = "public class Test",
            Replace = "invalid syntax {{{",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(false); // Should fail due to syntax errors
    }

    #endregion

    #region UnityAssetPatcher Tests

    [Fact]
    public async Task UnityAssetPatcher_Replace_ShouldReplaceValue()
    {
        // Arrange
        var patcher = new UnityAssetPatcher(new Mock<ILogger<UnityAssetPatcher>>().Object);
        var testFile = Path.Combine(_testDir, "Test.asset");
        await File.WriteAllTextAsync(testFile, "%YAML 1.1\nscriptingDefineSymbols: DEBUG");

        var patch = new CodePatch
        {
            Search = "DEBUG",
            Replace = "RELEASE",
            Mode = PatchMode.Replace
        };

        // Act
        var result = await patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().Be(true);
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("RELEASE");
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void AllPatchers_ShouldHaveCorrectPatchType()
    {
        // Arrange & Act
        var textPatcher = new TextPatcher(new Mock<ILogger<TextPatcher>>().Object);
        var jsonPatcher = new JsonPatcher(new Mock<ILogger<JsonPatcher>>().Object);
        var csharpPatcher = new CSharpPatcher(new Mock<ILogger<CSharpPatcher>>().Object);
        var unityPatcher = new UnityAssetPatcher(new Mock<ILogger<UnityAssetPatcher>>().Object);

        // Assert
        textPatcher.PatchType.Should().Be(PatchType.Text);
        jsonPatcher.PatchType.Should().Be(PatchType.Json);
        csharpPatcher.PatchType.Should().Be(PatchType.CSharp);
        unityPatcher.PatchType.Should().Be(PatchType.UnityAsset);
    }

    #endregion
}
