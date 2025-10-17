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
/// Tests for CacheService.
/// </summary>
public class CacheServiceTests : IDisposable
{
    private readonly GitHelper _gitHelper;
    private readonly PathResolver _pathResolver;
    private readonly ConfigService _configService;
    private readonly CacheService _cacheService;
    private readonly Mock<IPublisher<CachePopulatedMessage>> _cachePopulatedPublisherMock;
    private readonly Mock<IPublisher<CacheItemAddedMessage>> _cacheItemAddedPublisherMock;
    private readonly Mock<IPublisher<CacheItemRemovedMessage>> _cacheItemRemovedPublisherMock;
    private readonly Mock<IPublisher<CacheCleanedMessage>> _cacheCleanedPublisherMock;
    private readonly string _testDir;
    private readonly string _cacheDir;
    private readonly string _sourceDir;

    public CacheServiceTests()
    {
        var gitLoggerMock = new Mock<ILogger<GitHelper>>();
        var pathLoggerMock = new Mock<ILogger<PathResolver>>();
        var configLoggerMock = new Mock<ILogger<ConfigService>>();
        var cacheLoggerMock = new Mock<ILogger<CacheService>>();

        _gitHelper = new GitHelper(gitLoggerMock.Object);
        _pathResolver = new PathResolver(_gitHelper, pathLoggerMock.Object);

        _configService = new ConfigService(
            _pathResolver,
            configLoggerMock.Object,
            new Mock<IPublisher<ConfigLoadedMessage>>().Object,
            new Mock<IPublisher<ConfigSavedMessage>>().Object,
            new Mock<IPublisher<ConfigModifiedMessage>>().Object,
            new Mock<IPublisher<PackageAddedMessage>>().Object,
            new Mock<IPublisher<AssemblyAddedMessage>>().Object,
            new Mock<IPublisher<PatchAddedMessage>>().Object,
            new Mock<IPublisher<DefineSymbolAddedMessage>>().Object
        );

        _cachePopulatedPublisherMock = new Mock<IPublisher<CachePopulatedMessage>>();
        _cacheItemAddedPublisherMock = new Mock<IPublisher<CacheItemAddedMessage>>();
        _cacheItemRemovedPublisherMock = new Mock<IPublisher<CacheItemRemovedMessage>>();
        _cacheCleanedPublisherMock = new Mock<IPublisher<CacheCleanedMessage>>();

        _cacheService = new CacheService(
            _pathResolver,
            _configService,
            cacheLoggerMock.Object,
            _cachePopulatedPublisherMock.Object,
            _cacheItemAddedPublisherMock.Object,
            _cacheItemRemovedPublisherMock.Object,
            _cacheCleanedPublisherMock.Object
        );

        // Create test directories within git root
        var gitRoot = _gitHelper.DetectGitRoot();
        _testDir = Path.Combine(gitRoot, "build", "test-temp", $"cache-test-{Guid.NewGuid()}");
        _cacheDir = Path.Combine(_testDir, "cache");
        _sourceDir = Path.Combine(_testDir, "source");

        Directory.CreateDirectory(_cacheDir);
        Directory.CreateDirectory(_sourceDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task AddPackageAsync_ShouldAddPackageToCache()
    {
        // Arrange
        var config = _configService.CreateNew();
        var sourceFile = Path.Combine(_sourceDir, "test-package.tgz");
        await File.WriteAllTextAsync(sourceFile, "test package content");

        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        // Act
        var cacheItem = await _cacheService.AddPackageAsync(
            config,
            "com.test.package",
            "1.0.0",
            sourceFile,
            cacheRelativePath
        );

        // Assert
        cacheItem.Should().NotBeNull();
        cacheItem.Type.Should().Be(CacheItemType.UnityPackage);
        cacheItem.Name.Should().Be("com.test.package");
        cacheItem.Version.Should().Be("1.0.0");
        cacheItem.Size.Should().BeGreaterThan(0);
        cacheItem.Hash.Should().NotBeNullOrEmpty();

        // Verify file was copied
        var expectedCacheFile = Path.Combine(_cacheDir, "com.test.package-1.0.0.tgz");
        File.Exists(expectedCacheFile).Should().BeTrue();

        // Verify config was updated
        config.Packages.Should().HaveCount(1);
        config.Packages[0].Name.Should().Be("com.test.package");

        // Verify message published
        _cacheItemAddedPublisherMock.Verify(
            p => p.Publish(It.Is<CacheItemAddedMessage>(m => m.Item.Name == "com.test.package")),
            Times.Once
        );
    }

    [Fact]
    public async Task AddAssemblyAsync_ShouldAddAssemblyToCache()
    {
        // Arrange
        var config = _configService.CreateNew();
        var sourceFile = Path.Combine(_sourceDir, "TestAssembly.dll");
        await File.WriteAllTextAsync(sourceFile, "test assembly content");

        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        // Act
        var cacheItem = await _cacheService.AddAssemblyAsync(
            config,
            "TestAssembly",
            "1.0.0",
            sourceFile,
            cacheRelativePath
        );

        // Assert
        cacheItem.Should().NotBeNull();
        cacheItem.Type.Should().Be(CacheItemType.Assembly);
        cacheItem.Name.Should().Be("TestAssembly");
        cacheItem.Version.Should().Be("1.0.0");

        // Verify file was copied
        var expectedCacheFile = Path.Combine(_cacheDir, "TestAssembly.dll");
        File.Exists(expectedCacheFile).Should().BeTrue();

        // Verify config was updated
        config.Assemblies.Should().HaveCount(1);
        config.Assemblies[0].Name.Should().Be("TestAssembly");
    }

    [Fact]
    public async Task PopulateFromDirectoryAsync_ShouldPopulateCacheFromSource()
    {
        // Arrange
        var config = _configService.CreateNew();

        // Create test files in source
        await File.WriteAllTextAsync(Path.Combine(_sourceDir, "com.unity.addressables-1.21.2.tgz"), "package1");
        await File.WriteAllTextAsync(Path.Combine(_sourceDir, "com.unity.textmeshpro-3.0.6.tgz"), "package2");
        await File.WriteAllTextAsync(Path.Combine(_sourceDir, "Newtonsoft.Json.dll"), "assembly1");

        var sourceRelativePath = _pathResolver.MakeRelative(_sourceDir);
        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        // Act
        var items = await _cacheService.PopulateFromDirectoryAsync(
            sourceRelativePath,
            cacheRelativePath,
            config
        );

        // Assert
        items.Should().HaveCount(3);
        items.Should().Contain(i => i.Name == "com.unity.addressables" && i.Version == "1.21.2");
        items.Should().Contain(i => i.Name == "com.unity.textmeshpro" && i.Version == "3.0.6");
        items.Should().Contain(i => i.Name == "Newtonsoft.Json");

        // Verify files were copied
        Directory.GetFiles(_cacheDir).Should().HaveCount(3);

        // Verify config was updated
        config.Packages.Should().HaveCount(2);
        config.Assemblies.Should().HaveCount(1);

        // Verify message published
        _cachePopulatedPublisherMock.Verify(
            p => p.Publish(It.Is<CachePopulatedMessage>(m => m.ItemCount == 3)),
            Times.Once
        );
    }

    [Fact]
    public async Task PopulateFromDirectoryAsync_ShouldThrow_WhenSourceNotFound()
    {
        // Arrange
        var nonExistentPath = "build/nonexistent/source";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _cacheService.PopulateFromDirectoryAsync(nonExistentPath)
        );
    }

    [Fact]
    public async Task ListCacheAsync_ShouldReturnAllCacheItems()
    {
        // Arrange
        var config = _configService.CreateNew();
        var sourceFile1 = Path.Combine(_sourceDir, "test1.tgz");
        var sourceFile2 = Path.Combine(_sourceDir, "test2.dll");
        await File.WriteAllTextAsync(sourceFile1, "content1");
        await File.WriteAllTextAsync(sourceFile2, "content2");

        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        await _cacheService.AddPackageAsync(config, "test.package", "1.0.0", sourceFile1, cacheRelativePath);
        await _cacheService.AddAssemblyAsync(config, "TestAssembly", null, sourceFile2, cacheRelativePath);

        // Act
        var items = await _cacheService.ListCacheAsync(cacheRelativePath);

        // Assert
        items.Should().HaveCount(2);
        items.Should().Contain(i => i.Type == CacheItemType.UnityPackage);
        items.Should().Contain(i => i.Type == CacheItemType.Assembly);
    }

    [Fact]
    public async Task ListCacheAsync_ShouldReturnEmpty_WhenCacheDoesNotExist()
    {
        // Arrange
        var nonExistentCache = "build/nonexistent/cache";

        // Act
        var items = await _cacheService.ListCacheAsync(nonExistentCache);

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task CleanCache_ShouldRemoveAllItems()
    {
        // Arrange
        var config = _configService.CreateNew();
        var sourceFile = Path.Combine(_sourceDir, "test.tgz");
        await File.WriteAllTextAsync(sourceFile, "content");

        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);
        await _cacheService.AddPackageAsync(config, "test.package", "1.0.0", sourceFile, cacheRelativePath);

        // Act
        var removedCount = _cacheService.CleanCache(cacheRelativePath);

        // Assert
        removedCount.Should().Be(1);
        Directory.GetFiles(_cacheDir).Should().BeEmpty();

        // Verify message published
        _cacheCleanedPublisherMock.Verify(
            p => p.Publish(It.Is<CacheCleanedMessage>(m => m.RemovedCount == 1)),
            Times.Once
        );
    }

    [Fact]
    public void CleanCache_ShouldReturnZero_WhenCacheDoesNotExist()
    {
        // Arrange
        var nonExistentCache = "build/nonexistent/cache";

        // Act
        var removedCount = _cacheService.CleanCache(nonExistentCache);

        // Assert
        removedCount.Should().Be(0);
    }

    [Fact]
    public async Task CacheItem_ShouldHaveValidHash()
    {
        // Arrange
        var config = _configService.CreateNew();
        var sourceFile = Path.Combine(_sourceDir, "test.tgz");
        var content = "test content for hashing";
        await File.WriteAllTextAsync(sourceFile, content);

        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        // Act
        var cacheItem = await _cacheService.AddPackageAsync(
            config,
            "test.package",
            "1.0.0",
            sourceFile,
            cacheRelativePath
        );

        // Assert
        cacheItem.Hash.Should().NotBeNullOrEmpty();
        cacheItem.Hash.Should().HaveLength(64); // SHA256 hex string length
    }

    [Fact]
    public async Task PopulateFromDirectoryAsync_ShouldHandleNestedDirectories()
    {
        // Arrange
        var config = _configService.CreateNew();
        var nestedDir = Path.Combine(_sourceDir, "nested", "packages");
        Directory.CreateDirectory(nestedDir);

        await File.WriteAllTextAsync(Path.Combine(nestedDir, "com.test.nested-1.0.0.tgz"), "nested package");

        var sourceRelativePath = _pathResolver.MakeRelative(_sourceDir);
        var cacheRelativePath = _pathResolver.MakeRelative(_cacheDir);

        // Act
        var items = await _cacheService.PopulateFromDirectoryAsync(
            sourceRelativePath,
            cacheRelativePath,
            config
        );

        // Assert
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("com.test.nested");
    }
}
