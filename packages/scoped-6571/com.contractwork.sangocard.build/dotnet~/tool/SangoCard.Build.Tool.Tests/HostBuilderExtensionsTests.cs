using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace SangoCard.Build.Tool.Tests;

/// <summary>
/// Tests for HostBuilderExtensions.
/// </summary>
public class HostBuilderExtensionsTests
{
    [Fact]
    public void AddBuildToolServices_ShouldRegisterMessagePipe()
    {
        // Arrange
        var services = new ServiceCollection();
        var args = Array.Empty<string>();

        // Act
        services.AddBuildToolServices(args);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var publisher = serviceProvider.GetService<IPublisher<TestMessage>>();
        publisher.Should().NotBeNull("MessagePipe should be registered");
    }

    [Fact]
    public void ConfigureBuildToolLogging_ShouldSetDefaultLogLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ConfigureBuildToolLogging(Array.Empty<string>());
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<HostBuilderExtensionsTests>();

        // Assert
        logger.Should().NotBeNull();
        logger.IsEnabled(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public void ConfigureBuildToolLogging_WithVerboseFlag_ShouldSetDebugLogLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ConfigureBuildToolLogging(new[] { "--verbose" });
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<HostBuilderExtensionsTests>();

        // Assert
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue();
    }

    [Fact]
    public void ConfigureBuildToolLogging_WithQuietFlag_ShouldSetWarningLogLevel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ConfigureBuildToolLogging(new[] { "--quiet" });
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<HostBuilderExtensionsTests>();

        // Assert
        logger.IsEnabled(LogLevel.Information).Should().BeFalse();
        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();
    }

    [Fact]
    public async Task MessagePipe_ShouldPublishAndSubscribe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBuildToolServices(Array.Empty<string>());
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher<TestMessage>>();
        var subscriber = serviceProvider.GetRequiredService<ISubscriber<TestMessage>>();

        TestMessage? receivedMessage = null;
        var disposable = subscriber.Subscribe(msg => receivedMessage = msg);

        // Act
        var testMessage = new TestMessage("Hello, MessagePipe!");
        publisher.Publish(testMessage);

        // Allow async processing
        await Task.Delay(100);

        // Assert
        receivedMessage.Should().NotBeNull();
        receivedMessage!.Content.Should().Be("Hello, MessagePipe!");

        // Cleanup
        disposable.Dispose();
    }

    /// <summary>
    /// Test message for MessagePipe testing.
    /// </summary>
    private record TestMessage(string Content);
}
