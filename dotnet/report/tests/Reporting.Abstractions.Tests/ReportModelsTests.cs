// ReSharper disable RedundantUsingDirective
using System;
using Reporting.Abstractions;
using Xunit;
// ReSharper restore RedundantUsingDirective

namespace Reporting.Abstractions.Tests
{
    public class ReportResultTests
    {
        [Fact]
        public void ReportResult_DefaultValues_AreNull()
        {
            var result = new ReportResult();

            Assert.Null(result.Markdown);
            Assert.Null(result.Json);
            Assert.Null(result.Xml);
            Assert.Null(result.Yaml);
            Assert.Null(result.Html);
        }

        [Fact]
        public void ReportResult_WithInitializer_SetsValues()
        {
            var result = new ReportResult
            {
                Markdown = "# Test",
                Json = "{}",
                Xml = "<root />",
                Yaml = "test: value",
                Html = "<html />"
            };

            Assert.Equal("# Test", result.Markdown);
            Assert.Equal("{}", result.Json);
            Assert.Equal("<root />", result.Xml);
            Assert.Equal("test: value", result.Yaml);
            Assert.Equal("<html />", result.Html);
        }
    }

    public class ReportOptionsTests
    {
        [Fact]
        public void ReportOptions_DefaultValues_AreCorrect()
        {
            var options = new ReportOptions();

            Assert.Equal(ReportFormat.Markdown, options.Formats);
            Assert.Null(options.Title);
            Assert.Null(options.Description);
            Assert.Null(options.Parameters);
            Assert.True(options.IncludeMetadata);
            Assert.Null(options.TemplatePath);
        }

        [Fact]
        public void ReportOptions_WithInitializer_SetsValues()
        {
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                ["key1"] = "value1"
            };

            var options = new ReportOptions
            {
                Formats = ReportFormat.Json | ReportFormat.Xml,
                Title = "Custom Title",
                Description = "Custom Description",
                Parameters = parameters,
                IncludeMetadata = false,
                TemplatePath = "/custom/template.frx"
            };

            Assert.Equal(ReportFormat.Json | ReportFormat.Xml, options.Formats);
            Assert.Equal("Custom Title", options.Title);
            Assert.Equal("Custom Description", options.Description);
            Assert.NotNull(options.Parameters);
            Assert.False(options.IncludeMetadata);
            Assert.Equal("/custom/template.frx", options.TemplatePath);
        }
    }

    public class ReportFormatTests
    {
        [Fact]
        public void ReportFormat_FlagsWork()
        {
            var format = ReportFormat.Markdown | ReportFormat.Json;

            Assert.True(format.HasFlag(ReportFormat.Markdown));
            Assert.True(format.HasFlag(ReportFormat.Json));
            Assert.False(format.HasFlag(ReportFormat.Xml));
        }

        [Fact]
        public void ReportFormat_AllIncludesAllFormats()
        {
            var format = ReportFormat.All;

            Assert.True(format.HasFlag(ReportFormat.Markdown));
            Assert.True(format.HasFlag(ReportFormat.Json));
            Assert.True(format.HasFlag(ReportFormat.Xml));
            Assert.True(format.HasFlag(ReportFormat.Yaml));
            Assert.True(format.HasFlag(ReportFormat.Html));
        }
    }

    public class PreparedModelTests
    {
        [Fact]
        public void PreparedModel_DefaultValues_AreCorrect()
        {
            var model = new PreparedModel<string>();

            Assert.Null(model.Data);
            Assert.NotNull(model.Metadata);
            Assert.Null(model.PreparedAt);
            Assert.Null(model.Context);
        }

        [Fact]
        public void PreparedModel_WithInitializer_SetsValues()
        {
            var metadata = new ReportMetadata { Id = "test" };
            var context = new System.Collections.Generic.Dictionary<string, object>
            {
                ["key"] = "value"
            };
            var timestamp = DateTimeOffset.UtcNow;

            var model = new PreparedModel<string>
            {
                Data = "test data",
                Metadata = metadata,
                PreparedAt = timestamp,
                Context = context
            };

            Assert.Equal("test data", model.Data);
            Assert.Equal("test", model.Metadata.Id);
            Assert.Equal(timestamp, model.PreparedAt);
            Assert.NotNull(model.Context);
        }
    }
}
