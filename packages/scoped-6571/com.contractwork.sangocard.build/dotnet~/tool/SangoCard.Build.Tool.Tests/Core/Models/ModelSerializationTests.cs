using System.Text.Json;
using FluentAssertions;
using SangoCard.Build.Tool.Core.Models;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Models;

/// <summary>
/// Tests for model serialization and deserialization.
/// </summary>
public class ModelSerializationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    [Fact]
    public void AssetManipulation_ShouldSerializeWithEnumAsString()
    {
        // Arrange
        var manipulation = new AssetManipulation
        {
            Operation = AssetOperation.Copy,
            Source = "build/source.txt",
            Target = "projects/client/target.txt",
            Overwrite = true,
            Description = "Copy test file"
        };

        // Act
        var json = JsonSerializer.Serialize(manipulation, _jsonOptions);

        // Assert
        json.Should().Contain("\"operation\": \"Copy\""); // Enum as string
        json.Should().Contain("\"source\": \"build/source.txt\"");
        json.Should().Contain("\"overwrite\": true");
    }

    [Fact]
    public void CodePatch_ShouldSerializeWithEnums()
    {
        // Arrange
        var patch = new CodePatch
        {
            File = "projects/client/Assets/Scripts/Test.cs",
            Type = PatchType.CSharp,
            Search = "public class Test",
            Replace = "public class TestModified",
            Mode = PatchMode.Replace,
            Optional = false,
            Description = "Rename class"
        };

        // Act
        var json = JsonSerializer.Serialize(patch, _jsonOptions);

        // Assert
        json.Should().Contain("\"type\": \"CSharp\"");
        json.Should().Contain("\"mode\": \"Replace\"");
        json.Should().Contain("\"optional\": false");
    }

    [Fact]
    public void ScriptingDefineSymbols_ShouldSerializeCorrectly()
    {
        // Arrange
        var symbols = new ScriptingDefineSymbols
        {
            Add = new List<string> { "DEBUG_MODE", "FEATURE_X" },
            Remove = new List<string> { "OLD_FEATURE" },
            Platform = "StandaloneWindows64",
            ClearExisting = false
        };

        // Act
        var json = JsonSerializer.Serialize(symbols, _jsonOptions);

        // Assert
        json.Should().Contain("\"add\"");
        json.Should().Contain("DEBUG_MODE");
        json.Should().Contain("\"platform\": \"StandaloneWindows64\"");
    }

    [Fact]
    public void CacheItem_ShouldSerializeWithDateTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var item = new CacheItem
        {
            Type = CacheItemType.UnityPackage,
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Path = "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
            Size = 1024000,
            Hash = "abc123",
            AddedDate = now,
            Source = "code-quality"
        };

        // Act
        var json = JsonSerializer.Serialize(item, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<CacheItem>(json);

        // Assert
        json.Should().Contain("\"type\": \"UnityPackage\"");
        deserialized.Should().NotBeNull();
        deserialized!.AddedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ValidationResult_ShouldSerializeWithNestedObjects()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Level = ValidationLevel.Full,
            Errors = new List<ValidationError>
            {
                new()
                {
                    Code = "E001",
                    Message = "File not found",
                    File = "test.txt",
                    Line = 10
                }
            },
            Warnings = new List<ValidationWarning>
            {
                new()
                {
                    Code = "W001",
                    Message = "Deprecated API",
                    File = "old.cs"
                }
            },
            Summary = "Validation failed with 1 error and 1 warning"
        };

        // Act
        var json = JsonSerializer.Serialize(result, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<ValidationResult>(json);

        // Assert
        json.Should().Contain("\"isValid\": false");
        json.Should().Contain("\"level\": \"Full\"");
        deserialized.Should().NotBeNull();
        deserialized!.Errors.Should().HaveCount(1);
        deserialized.Warnings.Should().HaveCount(1);
        deserialized.TotalIssues.Should().Be(2);
    }

    [Fact]
    public void ValidationResult_TotalIssues_ShouldCalculateCorrectly()
    {
        // Arrange
        var result = new ValidationResult
        {
            Errors = new List<ValidationError>
            {
                new() { Code = "E001", Message = "Error 1" },
                new() { Code = "E002", Message = "Error 2" }
            },
            Warnings = new List<ValidationWarning>
            {
                new() { Code = "W001", Message = "Warning 1" }
            }
        };

        // Act & Assert
        result.TotalIssues.Should().Be(3);
    }

    [Fact]
    public void AllEnums_ShouldSerializeAsStrings()
    {
        // Arrange & Act
        var assetOp = JsonSerializer.Serialize(AssetOperation.Copy);
        var patchType = JsonSerializer.Serialize(PatchType.CSharp);
        var patchMode = JsonSerializer.Serialize(PatchMode.Replace);
        var cacheType = JsonSerializer.Serialize(CacheItemType.UnityPackage);
        var validLevel = JsonSerializer.Serialize(ValidationLevel.Full);

        // Assert - All should be numeric (default) unless in a model with JsonStringEnumConverter
        assetOp.Should().Be("0"); // Default is numeric
        patchType.Should().Be("0");
        patchMode.Should().Be("0");
        cacheType.Should().Be("0");
        validLevel.Should().Be("3");
    }
}
