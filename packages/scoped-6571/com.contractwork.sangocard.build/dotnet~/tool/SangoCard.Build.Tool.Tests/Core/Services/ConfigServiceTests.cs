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
/// Tests for ConfigService.
/// </summary>
public class ConfigServiceTests : IDisposable
{
    private readonly Mock<ILogger<GitHelper>> _gitLoggerMock;
    private readonly Mock<ILogger<PathResolver>> _pathLoggerMock;
    private readonly Mock<ILogger<ConfigService>> _configLoggerMock;
    private readonly Mock<IPublisher<ConfigLoadedMessage>> _configLoadedPublisherMock;
    private readonly Mock<IPublisher<ConfigSavedMessage>> _configSavedPublisherMock;
    private readonly Mock<IPublisher<ConfigModifiedMessage>> _configModifiedPublisherMock;
    private readonly Mock<IPublisher<PackageAddedMessage>> _packageAddedPublisherMock;
    private readonly Mock<IPublisher<AssemblyAddedMessage>> _assemblyAddedPublisherMock;
    private readonly Mock<IPublisher<PatchAddedMessage>> _patchAddedPublisherMock;
    private readonly Mock<IPublisher<DefineSymbolAddedMessage>> _defineSymbolAddedPublisherMock;
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
    private readonly ConfigService _configService;
    private readonly string _testDir;

    public ConfigServiceTests()
    {
        _gitLoggerMock = new Mock<ILogger<GitHelper>>();
        _pathLoggerMock = new Mock<ILogger<PathResolver>>();
        _configLoggerMock = new Mock<ILogger<ConfigService>>();
        _configLoadedPublisherMock = new Mock<IPublisher<ConfigLoadedMessage>>();
        _configSavedPublisherMock = new Mock<IPublisher<ConfigSavedMessage>>();
        _configModifiedPublisherMock = new Mock<IPublisher<ConfigModifiedMessage>>();
        _packageAddedPublisherMock = new Mock<IPublisher<PackageAddedMessage>>();
        _assemblyAddedPublisherMock = new Mock<IPublisher<AssemblyAddedMessage>>();
        _patchAddedPublisherMock = new Mock<IPublisher<PatchAddedMessage>>();
        _defineSymbolAddedPublisherMock = new Mock<IPublisher<DefineSymbolAddedMessage>>();

        _gitHelper = new GitHelper(_gitLoggerMock.Object);
        _pathResolver = new PathResolver(_gitHelper, _pathLoggerMock.Object);

        _configService = new ConfigService(
            _pathResolver,
            _configLoggerMock.Object,
            _configLoadedPublisherMock.Object,
            _configSavedPublisherMock.Object,
            _configModifiedPublisherMock.Object,
            _packageAddedPublisherMock.Object,
            _assemblyAddedPublisherMock.Object,
            _patchAddedPublisherMock.Object,
            _defineSymbolAddedPublisherMock.Object
        );

        // Create temp directory for tests within git root
        var gitRoot = _gitHelper.DetectGitRoot();
        _testDir = Path.Combine(gitRoot, "build", "test-temp", $"config-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void CreateNew_ShouldCreateEmptyConfig()
    {
        // Act
        var config = _configService.CreateNew("Test config");

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1.0");
        config.Description.Should().Be("Test config");
        config.Packages.Should().BeEmpty();
        config.Assemblies.Should().BeEmpty();
        config.CodePatches.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveConfigToFile()
    {
        // Arrange
        var config = _configService.CreateNew("Test config");
        var testFile = Path.Combine(_testDir, "test-config.json");
        var relativePath = _pathResolver.MakeRelative(testFile);

        // Act
        await _configService.SaveAsync(config, relativePath);

        // Assert
        File.Exists(testFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Contain("\"version\"");
        content.Should().Contain("\"description\"");

        // Verify message published
        _configSavedPublisherMock.Verify(
            p => p.Publish(It.Is<ConfigSavedMessage>(m => m.FilePath == relativePath)),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadConfigFromFile()
    {
        // Arrange
        var config = _configService.CreateNew("Test config");
        var testFile = Path.Combine(_testDir, "test-config.json");
        var relativePath = _pathResolver.MakeRelative(testFile);
        await _configService.SaveAsync(config, relativePath);

        // Act
        var loadedConfig = await _configService.LoadAsync(relativePath);

        // Assert
        loadedConfig.Should().NotBeNull();
        loadedConfig.Version.Should().Be("1.0");
        loadedConfig.Description.Should().Be("Test config");

        // Verify message published
        _configLoadedPublisherMock.Verify(
            p => p.Publish(It.Is<ConfigLoadedMessage>(m => m.FilePath == relativePath)),
            Times.Once
        );
    }

    [Fact]
    public async Task LoadAsync_ShouldThrow_WhenFileNotFound()
    {
        // Arrange
        var nonExistentPath = "build/nonexistent/config.json";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _configService.LoadAsync(nonExistentPath)
        );
    }

    [Fact]
    public void AddPackage_ShouldAddPackageToConfig()
    {
        // Arrange
        var config = _configService.CreateNew();
        var package = new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Source = "build/preparation/cache/com.unity.addressables-1.21.2.tgz",
            Target = "projects/client/Packages/com.unity.addressables-1.21.2.tgz"
        };

        // Act
        _configService.AddPackage(config, package);

        // Assert
        config.Packages.Should().HaveCount(1);
        config.Packages[0].Should().Be(package);

        // Verify messages published
        _packageAddedPublisherMock.Verify(
            p => p.Publish(It.Is<PackageAddedMessage>(m => m.Package == package)),
            Times.Once
        );
        _configModifiedPublisherMock.Verify(
            p => p.Publish(It.IsAny<ConfigModifiedMessage>()),
            Times.Once
        );
    }

    [Fact]
    public void AddPackage_ShouldReplaceDuplicate()
    {
        // Arrange
        var config = _configService.CreateNew();
        var package1 = new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Source = "old-source.tgz",
            Target = "old-target.tgz"
        };
        var package2 = new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Source = "new-source.tgz",
            Target = "new-target.tgz"
        };

        // Act
        _configService.AddPackage(config, package1);
        _configService.AddPackage(config, package2);

        // Assert
        config.Packages.Should().HaveCount(1);
        config.Packages[0].Source.Should().Be("new-source.tgz");
    }

    [Fact]
    public void AddAssembly_ShouldAddAssemblyToConfig()
    {
        // Arrange
        var config = _configService.CreateNew();
        var assembly = new AssemblyReference
        {
            Name = "Newtonsoft.Json",
            Version = "13.0.1",
            Source = "build/preparation/cache/Newtonsoft.Json.dll",
            Target = "projects/client/Assets/Plugins/Newtonsoft.Json.dll"
        };

        // Act
        _configService.AddAssembly(config, assembly);

        // Assert
        config.Assemblies.Should().HaveCount(1);
        config.Assemblies[0].Should().Be(assembly);

        // Verify messages published
        _assemblyAddedPublisherMock.Verify(
            p => p.Publish(It.Is<AssemblyAddedMessage>(m => m.Assembly == assembly)),
            Times.Once
        );
    }

    [Fact]
    public void AddPatch_ShouldAddPatchToConfig()
    {
        // Arrange
        var config = _configService.CreateNew();
        var patch = new CodePatch
        {
            File = "projects/client/Assets/Scripts/Test.cs",
            Type = PatchType.CSharp,
            Search = "public class Test",
            Replace = "public class TestModified",
            Mode = PatchMode.Replace
        };

        // Act
        _configService.AddPatch(config, patch);

        // Assert
        config.CodePatches.Should().HaveCount(1);
        config.CodePatches[0].Should().Be(patch);

        // Verify messages published
        _patchAddedPublisherMock.Verify(
            p => p.Publish(It.Is<PatchAddedMessage>(m => m.Patch == patch)),
            Times.Once
        );
    }

    [Fact]
    public void AddDefineSymbol_ShouldAddSymbolToConfig()
    {
        // Arrange
        var config = _configService.CreateNew();

        // Act
        _configService.AddDefineSymbol(config, "DEBUG_MODE");

        // Assert
        config.ScriptingDefineSymbols.Should().NotBeNull();
        config.ScriptingDefineSymbols!.Add.Should().Contain("DEBUG_MODE");

        // Verify messages published
        _defineSymbolAddedPublisherMock.Verify(
            p => p.Publish(It.Is<DefineSymbolAddedMessage>(m => m.Symbol == "DEBUG_MODE")),
            Times.Once
        );
    }

    [Fact]
    public void AddDefineSymbol_ShouldNotAddDuplicate()
    {
        // Arrange
        var config = _configService.CreateNew();

        // Act
        _configService.AddDefineSymbol(config, "DEBUG_MODE");
        _configService.AddDefineSymbol(config, "DEBUG_MODE");

        // Assert
        config.ScriptingDefineSymbols!.Add.Should().HaveCount(1);
    }

    [Fact]
    public void RemovePackage_ShouldRemovePackage()
    {
        // Arrange
        var config = _configService.CreateNew();
        var package = new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2"
        };
        _configService.AddPackage(config, package);

        // Act
        var result = _configService.RemovePackage(config, "com.unity.addressables", "1.21.2");

        // Assert
        result.Should().BeTrue();
        config.Packages.Should().BeEmpty();
    }

    [Fact]
    public void RemovePackage_ShouldReturnFalse_WhenNotFound()
    {
        // Arrange
        var config = _configService.CreateNew();

        // Act
        var result = _configService.RemovePackage(config, "nonexistent", "1.0.0");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveAssembly_ShouldRemoveAssembly()
    {
        // Arrange
        var config = _configService.CreateNew();
        var assembly = new AssemblyReference { Name = "Newtonsoft.Json" };
        _configService.AddAssembly(config, assembly);

        // Act
        var result = _configService.RemoveAssembly(config, "Newtonsoft.Json");

        // Assert
        result.Should().BeTrue();
        config.Assemblies.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRoundTrip()
    {
        // Arrange
        var config = _configService.CreateNew("Round trip test");
        _configService.AddPackage(config, new UnityPackageReference
        {
            Name = "com.unity.addressables",
            Version = "1.21.2",
            Source = "source.tgz",
            Target = "target.tgz"
        });
        _configService.AddDefineSymbol(config, "TEST_SYMBOL");

        var testFile = Path.Combine(_testDir, "roundtrip-config.json");
        var relativePath = _pathResolver.MakeRelative(testFile);

        // Act
        await _configService.SaveAsync(config, relativePath);
        var loadedConfig = await _configService.LoadAsync(relativePath);

        // Assert
        loadedConfig.Description.Should().Be("Round trip test");
        loadedConfig.Packages.Should().HaveCount(1);
        loadedConfig.Packages[0].Name.Should().Be("com.unity.addressables");
        loadedConfig.ScriptingDefineSymbols!.Add.Should().Contain("TEST_SYMBOL");
    }
}
