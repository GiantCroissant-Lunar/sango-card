using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Reporting.Abstractions;
using Reporting.Core;
using Xunit;

namespace Reporting.Core.Tests
{
    public class SimpleReportEngineTests
    {
        private readonly SimpleReportEngine _engine;

        public SimpleReportEngineTests()
        {
            _engine = new SimpleReportEngine();
        }

        [Fact]
        public async Task GenerateAsync_WithValidProvider_GeneratesMarkdown()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Markdown };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Markdown.Should().NotBeNullOrEmpty();
            result.Markdown.Should().Contain("# Test Report");
            // Column order is alphabetical by default when no sort order specified
            result.Markdown.Should().Contain("| Count | Id | Name |");
        }

        [Fact]
        public async Task GenerateAsync_WithJsonFormat_GeneratesJson()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Json };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Json.Should().NotBeNullOrEmpty();
            result.Json.Should().Contain("\"data\"");
            result.Json.Should().Contain("\"metadata\"");
        }

        [Fact]
        public async Task GenerateAsync_WithXmlFormat_GeneratesXml()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Xml };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Xml.Should().NotBeNullOrEmpty();
            result.Xml.Should().Contain("<Report");
            result.Xml.Should().Contain("</Report>");
        }

        [Fact]
        public async Task GenerateAsync_WithYamlFormat_GeneratesYaml()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Yaml };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Yaml.Should().NotBeNullOrEmpty();
            result.Yaml.Should().Contain("data:");
            result.Yaml.Should().Contain("metadata:");
        }

        [Fact]
        public async Task GenerateAsync_WithHtmlFormat_GeneratesHtml()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Html };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Html.Should().NotBeNullOrEmpty();
            result.Html.Should().Contain("<h1>");
        }

        [Fact]
        public async Task GenerateAsync_WithMultipleFormats_GeneratesAll()
        {
            var provider = new TestDataProvider();
            var options = new ReportOptions
            {
                Formats = ReportFormat.Markdown | ReportFormat.Json | ReportFormat.Xml
            };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Markdown.Should().NotBeNullOrEmpty();
            result.Json.Should().NotBeNullOrEmpty();
            result.Xml.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GenerateAsync_WithNullOptions_UsesDefaults()
        {
            var provider = new TestDataProvider();

            var result = await _engine.GenerateAsync(provider, null);

            result.Should().NotBeNull();
            result.Markdown.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GenerateAsync_WithEmptyData_HandlesGracefully()
        {
            var provider = new EmptyDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Markdown };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Markdown.Should().NotBeNullOrEmpty();
            result.Markdown.Should().Contain("No data");
        }

        [Fact]
        public async Task GenerateAsync_WithException_ReturnsErrorResult()
        {
            var provider = new ErrorDataProvider();
            var options = new ReportOptions { Formats = ReportFormat.Json };

            var result = await _engine.GenerateAsync(provider, options);

            result.Should().NotBeNull();
            result.Json.Should().Contain("error");
        }
    }

    // Test helper classes
    public class TestDataProvider : IReportDataProvider<List<TestData>>
    {
        public async Task<PreparedModel<List<TestData>>> GetDataAsync(System.Threading.CancellationToken ct = default)
        {
            await Task.Delay(1, ct);

            var data = new List<TestData>
            {
                new TestData { Id = 1, Name = "Item 1", Count = 10 },
                new TestData { Id = 2, Name = "Item 2", Count = 20 },
                new TestData { Id = 3, Name = "Item 3", Count = 30 }
            };

            return new PreparedModel<List<TestData>>
            {
                Data = data,
                Metadata = GetMetadata(),
                PreparedAt = DateTimeOffset.UtcNow
            };
        }

        public ReportMetadata GetMetadata()
        {
            return new ReportMetadata
            {
                Id = "test-report",
                Title = "Test Report",
                Description = "Test report for unit tests",
                Columns = new[]
                {
                    new ReportColumnInfo { PropertyName = "Id", DisplayName = "Id", DataType = "int" },
                    new ReportColumnInfo { PropertyName = "Name", DisplayName = "Name", DataType = "string" },
                    new ReportColumnInfo { PropertyName = "Count", DisplayName = "Count", DataType = "int" }
                }
            };
        }
    }

    public class EmptyDataProvider : IReportDataProvider<List<TestData>>
    {
        public async Task<PreparedModel<List<TestData>>> GetDataAsync(System.Threading.CancellationToken ct = default)
        {
            await Task.Delay(1, ct);

            return new PreparedModel<List<TestData>>
            {
                Data = new List<TestData>(),
                Metadata = new ReportMetadata { Id = "empty", Title = "Empty Report" },
                PreparedAt = DateTimeOffset.UtcNow
            };
        }

        public ReportMetadata GetMetadata()
        {
            return new ReportMetadata { Id = "empty", Title = "Empty Report" };
        }
    }

    public class ErrorDataProvider : IReportDataProvider<List<TestData>>
    {
        public Task<PreparedModel<List<TestData>>> GetDataAsync(System.Threading.CancellationToken ct = default)
        {
            throw new InvalidOperationException("Test error");
        }

        public ReportMetadata GetMetadata()
        {
            return new ReportMetadata { Id = "error", Title = "Error Report" };
        }
    }

    public class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
