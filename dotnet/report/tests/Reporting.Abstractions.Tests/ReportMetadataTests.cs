using System;
using Reporting.Abstractions;
using Xunit;

namespace Reporting.Abstractions.Tests
{
    public class ReportMetadataTests
    {
        [Fact]
        public void ReportMetadata_DefaultValues_AreCorrect()
        {
            var metadata = new ReportMetadata();

            Assert.Equal(string.Empty, metadata.Id);
            Assert.Equal(string.Empty, metadata.Title);
            Assert.Equal("1.0", metadata.SchemaVersion);
            Assert.Null(metadata.Description);
            Assert.Empty(metadata.Columns);
            Assert.Null(metadata.Template);
        }

        [Fact]
        public void ReportMetadata_WithInitializer_SetsValues()
        {
            var metadata = new ReportMetadata
            {
                Id = "test-report",
                Title = "Test Report",
                SchemaVersion = "2.0",
                Description = "Test Description",
                Columns = new[]
                {
                    new ReportColumnInfo { PropertyName = "Id", DisplayName = "ID" }
                }
            };

            Assert.Equal("test-report", metadata.Id);
            Assert.Equal("Test Report", metadata.Title);
            Assert.Equal("2.0", metadata.SchemaVersion);
            Assert.Equal("Test Description", metadata.Description);
            Assert.Single(metadata.Columns);
        }
    }

    public class ReportColumnInfoTests
    {
        [Fact]
        public void ReportColumnInfo_DefaultValues_AreCorrect()
        {
            var column = new ReportColumnInfo();

            Assert.Equal(string.Empty, column.PropertyName);
            Assert.Equal(string.Empty, column.DisplayName);
            Assert.Equal("string", column.DataType);
            Assert.Null(column.Format);
            Assert.Null(column.Width);
            Assert.Null(column.SortOrder);
            Assert.False(column.IncludeInSummary);
        }

        [Fact]
        public void ReportColumnInfo_WithInitializer_SetsValues()
        {
            var column = new ReportColumnInfo
            {
                PropertyName = "TotalCount",
                DisplayName = "Total Count",
                DataType = "int",
                Format = "N0",
                Width = 100,
                SortOrder = 1,
                IncludeInSummary = true
            };

            Assert.Equal("TotalCount", column.PropertyName);
            Assert.Equal("Total Count", column.DisplayName);
            Assert.Equal("int", column.DataType);
            Assert.Equal("N0", column.Format);
            Assert.Equal(100, column.Width);
            Assert.Equal(1, column.SortOrder);
            Assert.True(column.IncludeInSummary);
        }
    }

    public class ReportTemplateTests
    {
        [Fact]
        public void ReportTemplate_DefaultValues_AreCorrect()
        {
            var template = new ReportTemplate();

            Assert.Equal("Markdown", template.Kind);
            Assert.Equal(string.Empty, template.Path);
        }

        [Fact]
        public void ReportTemplate_WithInitializer_SetsValues()
        {
            var template = new ReportTemplate
            {
                Kind = "FastReportFrx",
                Path = "/templates/report.frx"
            };

            Assert.Equal("FastReportFrx", template.Kind);
            Assert.Equal("/templates/report.frx", template.Path);
        }
    }
}
