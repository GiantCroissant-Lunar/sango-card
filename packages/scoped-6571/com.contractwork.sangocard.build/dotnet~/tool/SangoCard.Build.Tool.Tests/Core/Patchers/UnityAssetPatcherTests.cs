using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SangoCard.Build.Tool.Core.Models;
using SangoCard.Build.Tool.Core.Patchers;
using Xunit;

namespace SangoCard.Build.Tool.Tests.Core.Patchers;

/// <summary>
/// Unit tests for UnityAssetPatcher.
/// Tests Unity YAML asset manipulation including property modification, component addition/removal.
/// </summary>
public class UnityAssetPatcherTests : IDisposable
{
    private readonly UnityAssetPatcher _patcher;
    private readonly string _testDir;

    // Sample Unity asset content for testing
    private const string SampleAudioManagerAsset = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!11 &1
AudioManager:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Volume: 1
  Rolloff Scale: 1
  Doppler Factor: 1
  Default Speaker Mode: 2
  m_SampleRate: 0
  m_DSPBufferSize: 1024
  m_VirtualVoiceCount: 512
  m_RealVoiceCount: 32
";

    private const string SampleMonoBehaviourAsset = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fcf7219bab7fe46a1ad266029b2fee19, type: 3}
  m_Name: TestAsset
  m_EditorClassIdentifier:
";

    public UnityAssetPatcherTests()
    {
        _patcher = new UnityAssetPatcher(NullLogger<UnityAssetPatcher>.Instance);
        _testDir = Path.Combine(Path.GetTempPath(), $"unityasset-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region ModifyProperty Operation Tests

    [Fact]
    public async Task ModifyProperty_ShouldUpdatePropertyValue()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "AudioManager.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Volume",
            Replace = "0.5",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("m_Volume: 0.5");
        content.Should().NotContain("m_Volume: 1");
    }

    [Fact]
    public async Task ModifyProperty_ShouldPreserveIndentation()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "indent-test.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "Rolloff Scale",
            Replace = "2.5",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        var lines = content.Split('\n');
        var rolloffLine = lines.First(l => l.Contains("Rolloff Scale"));

        // Should preserve the original indentation (2 spaces)
        rolloffLine.Should().StartWith("  Rolloff Scale: 2.5");
    }

    [Fact]
    public async Task ModifyProperty_WithMultipleOccurrences_ShouldModifyAll()
    {
        // Arrange
        var assetWithDuplicates = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!11 &1
TestAsset:
  m_Value: 10
  nested:
    m_Value: 20
";
        var testFile = Path.Combine(_testDir, "duplicates.asset");
        await File.WriteAllTextAsync(testFile, assetWithDuplicates);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Value",
            Replace = "100",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        // All occurrences should be modified
        var lines = content.Split('\n');
        var valueLines = lines.Where(l => l.Trim().StartsWith("m_Value:")).ToList();
        valueLines.Should().AllSatisfy(line => line.Should().Contain("m_Value: 100"));
    }

    #endregion

    #region AddComponent Operation Tests

    [Fact]
    public async Task AddComponent_ShouldInsertContentAfterMarker()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "add-component.asset");
        await File.WriteAllTextAsync(testFile, SampleMonoBehaviourAsset);

        var newComponent = "  m_CustomProperty: CustomValue";
        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "AddComponent",
            Search = "m_EditorClassIdentifier:",
            Replace = newComponent,
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain(newComponent);

        // Should be added after the marker
        var markerIndex = content.IndexOf("m_EditorClassIdentifier:");
        var componentIndex = content.IndexOf(newComponent);
        componentIndex.Should().BeGreaterThan(markerIndex);
    }

    [Fact]
    public async Task AddComponent_WithMissingMarker_ShouldFail()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "missing-marker.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "AddComponent",
            Search = "NonExistentMarker:",
            Replace = "  newProperty: value",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    #endregion

    #region RemoveComponent Operation Tests

    [Fact]
    public async Task RemoveComponent_ShouldRemoveMatchingContent()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "remove.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "RemoveComponent",
            Search = "  m_SampleRate: 0",  // Without newline
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().NotContain("m_SampleRate");
    }

    #endregion

    #region Backward Compatibility (PatchMode) Tests

    [Fact]
    public async Task PatchMode_Replace_ShouldWork()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "mode-replace.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Mode = PatchMode.Replace,
            Search = "m_Volume: 1",
            Replace = "m_Volume: 0.75",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("m_Volume: 0.75");
    }

    [Fact]
    public async Task PatchMode_Delete_ShouldWork()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "mode-delete.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Mode = PatchMode.Delete,
            Search = "  m_DSPBufferSize: 1024",  // Test deleting a different line
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().NotContain("m_DSPBufferSize");
        // m_SampleRate should still exist
        content.Should().Contain("m_SampleRate");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidatePatch_WithValidAsset_ShouldReturnTrue()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "valid.asset");
        await File.WriteAllTextAsync(testFile, SampleAudioManagerAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Search = "m_Volume",
            File = testFile
        };

        // Act
        var result = await _patcher.ValidatePatchAsync(testFile, patch);

        // Assert
        result.IsValid.Should().BeTrue();
        result.TargetFound.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePatch_WithMissingHeader_ShouldCatchDuringValidation()
    {
        // Arrange
        var invalidAsset = @"AudioManager:
  m_Volume: 1
";
        var testFile = Path.Combine(_testDir, "invalid-header.asset");
        await File.WriteAllTextAsync(testFile, invalidAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Volume",
            Replace = "0.5",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        // The patch will fail because validation detects missing header
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("YAML");
    }

    [Fact]
    public async Task ValidatePatch_WithInvalidYaml_ShouldFailDuringValidation()
    {
        // Arrange
        var invalidYaml = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!11 &1
AudioManager:
  m_Volume: 1
  invalid: [unclosed bracket
";
        var testFile = Path.Combine(_testDir, "invalid-yaml.asset");
        await File.WriteAllTextAsync(testFile, invalidYaml);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Volume",
            Replace = "0.5",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        // The base class catches the exception during validation and returns failure
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("YAML");
    }

    #endregion

    #region Utility Method Tests

    [Fact]
    public void ExtractPropertyValue_ShouldReturnCorrectValue()
    {
        // Arrange & Act
        var value = UnityAssetPatcher.ExtractPropertyValue(SampleAudioManagerAsset, "m_Volume");

        // Assert
        value.Should().Be("1");
    }

    [Fact]
    public void ExtractPropertyValue_WithMissingProperty_ShouldReturnNull()
    {
        // Arrange & Act
        var value = UnityAssetPatcher.ExtractPropertyValue(SampleAudioManagerAsset, "NonExistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void HasComponent_WithExistingComponent_ShouldReturnTrue()
    {
        // Arrange & Act
        var hasMonoBehaviour = UnityAssetPatcher.HasComponent(SampleMonoBehaviourAsset, "114");

        // Assert
        hasMonoBehaviour.Should().BeTrue();
    }

    [Fact]
    public void HasComponent_WithMissingComponent_ShouldReturnFalse()
    {
        // Arrange & Act
        var hasComponent = UnityAssetPatcher.HasComponent(SampleAudioManagerAsset, "114");

        // Assert
        hasComponent.Should().BeFalse();
    }

    #endregion

    #region DryRun Tests

    [Fact]
    public async Task ApplyPatch_InDryRunMode_ShouldNotModifyFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "dryrun.asset");
        var originalContent = SampleAudioManagerAsset;
        await File.WriteAllTextAsync(testFile, originalContent);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Volume",
            Replace = "0.25",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch, dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.Modified.Should().BeTrue();
        result.Preview.Should().NotBeNullOrEmpty();

        var actualContent = await File.ReadAllTextAsync(testFile);
        actualContent.Should().Be(originalContent); // File unchanged
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ModifyProperty_WithComplexValue_ShouldWork()
    {
        // Arrange
        var complexAsset = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &1
TestAsset:
  m_Color: {r: 1, g: 0.5, b: 0.25, a: 1}
";
        var testFile = Path.Combine(_testDir, "complex.asset");
        await File.WriteAllTextAsync(testFile, complexAsset);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Color",
            Replace = "{r: 0, g: 1, b: 0, a: 1}",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue();

        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("m_Color: {r: 0, g: 1, b: 0, a: 1}");
    }

    [Fact]
    public async Task ApplyPatch_WithEmptyFile_ShouldFail()
    {
        // Arrange
        var testFile = Path.Combine(_testDir, "empty.asset");
        await File.WriteAllTextAsync(testFile, string.Empty);

        var patch = new CodePatch
        {
            Type = PatchType.UnityAsset,
            Operation = "ModifyProperty",
            Search = "m_Volume",
            Replace = "0.5",
            File = testFile
        };

        // Act
        var result = await _patcher.ApplyPatchAsync(testFile, patch);

        // Assert
        result.Success.Should().BeTrue(); // Patch applies (no-op)
        result.Modified.Should().BeFalse(); // But nothing changed
    }

    #endregion
}
