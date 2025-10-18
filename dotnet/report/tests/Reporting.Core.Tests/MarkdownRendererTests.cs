using System;
using System.Collections.Generic;
using FluentAssertions;
using Reporting.Abstractions;
using Reporting.Core.Renderers;
using Xunit;

namespace Reporting.Core.Tests
{
    public class MarkdownRendererTests
    {
        private readonly MarkdownRenderer _renderer;

        public MarkdownRendererTests()
        {
            _renderer = new MarkdownRenderer();
        }

        [Fact]
        public void Render_WithValidData_GeneratesMarkdown()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("# Test Report");
            result.Should().Contain("## Report Information");
            result.Should().Contain("## Data");
        }

        [Fact]
        public void Render_WithTableData_GeneratesTable()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().Contain("| Name | Value |");
            result.Should().Contain("| --- | --- |");
            result.Should().Contain("| Test 1 | 100 |");
        }

        [Fact]
        public void Render_EscapesMarkdownCharacters()
        {
            var data = new List<TestItem>
            {
                new TestItem { Name = "Test | Pipe", Value = 100 },
                new TestItem { Name = "Test * Star", Value = 200 }
            };

            var model = new PreparedModel<List<TestItem>>
            {
                Data = data,
                Metadata = new ReportMetadata
                {
                    Id = "escape-test",
                    Title = "Escape # Test",
                    Columns = new[]
                    {
                        new ReportColumnInfo { PropertyName = "Name", DisplayName = "Name" },
                        new ReportColumnInfo { PropertyName = "Value", DisplayName = "Value" }
                    }
                }
            };

            var result = _renderer.Render(model);

            result.Should().Contain("\\|");
            result.Should().Contain("\\*");
            result.Should().Contain("\\#");
        }

        [Fact]
        public void Render_WithEmptyData_HandlesGracefully()
        {
            var model = new PreparedModel<List<TestItem>>
            {
                Data = new List<TestItem>(),
                Metadata = new ReportMetadata
                {
                    Id = "empty",
                    Title = "Empty Report"
                }
            };

            var result = _renderer.Render(model);

            result.Should().Contain("*No data rows found.*");
        }

        private PreparedModel<List<TestItem>> CreateTestModel()
        {
            var data = new List<TestItem>
            {
                new TestItem { Name = "Test 1", Value = 100 },
                new TestItem { Name = "Test 2", Value = 200 }
            };

            return new PreparedModel<List<TestItem>>
            {
                Data = data,
                Metadata = new ReportMetadata
                {
                    Id = "test",
                    Title = "Test Report",
                    Description = "Test Description",
                    Columns = new[]
                    {
                        new ReportColumnInfo { PropertyName = "Name", DisplayName = "Name" },
                        new ReportColumnInfo { PropertyName = "Value", DisplayName = "Value" }
                    }
                },
                PreparedAt = new DateTimeOffset(2025, 10, 18, 0, 30, 0, TimeSpan.Zero)
            };
        }
    }

    public class TestItem
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}
