using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Reporting.Abstractions;
using Reporting.Core.Renderers;
using Xunit;

namespace Reporting.Core.Tests
{
    public class JsonRendererTests
    {
        private readonly JsonRenderer _renderer;

        public JsonRendererTests()
        {
            _renderer = new JsonRenderer();
        }

        [Fact]
        public void Render_WithValidData_GeneratesValidJson()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().NotBeNullOrEmpty();

            // Verify it's valid JSON
            var parsed = JsonDocument.Parse(result);
            parsed.Should().NotBeNull();
        }

        [Fact]
        public void Render_UsesIndentation()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().Contain("\n");
            result.Should().Contain("  ");
        }

        [Fact]
        public void Render_UsesCamelCase()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().Contain("\"data\":");
            result.Should().Contain("\"metadata\":");
            result.Should().NotContain("\"Data\":");
            result.Should().NotContain("\"Metadata\":");
        }

        [Fact]
        public void Render_WithEmptyData_GeneratesValidJson()
        {
            var model = new PreparedModel<List<TestItem>>
            {
                Data = new List<TestItem>(),
                Metadata = new ReportMetadata { Id = "empty", Title = "Empty" }
            };

            var result = _renderer.Render(model);

            var parsed = JsonDocument.Parse(result);
            parsed.Should().NotBeNull();
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
                    Title = "Test Report"
                },
                PreparedAt = new DateTimeOffset(2025, 10, 18, 0, 30, 0, TimeSpan.Zero)
            };
        }
    }

    public class XmlRendererTests
    {
        private readonly XmlRenderer _renderer;

        public XmlRendererTests()
        {
            _renderer = new XmlRenderer();
        }

        [Fact]
        public void Render_WithValidData_GeneratesValidXml()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().NotBeNullOrEmpty();
            result.Should().StartWith("<");
            result.Should().Contain("<Report");
            result.Should().Contain("</Report>");
        }

        [Fact]
        public void Render_EscapesXmlCharacters()
        {
            var data = new List<TestItem>
            {
                new TestItem { Name = "Test <tag>", Value = 100 }
            };

            var model = new PreparedModel<List<TestItem>>
            {
                Data = data,
                Metadata = new ReportMetadata
                {
                    Id = "escape-test",
                    Title = "Test & Escape"
                }
            };

            var result = _renderer.Render(model);

            result.Should().NotContain("Test <tag>");
            result.Should().NotContain("Test & Escape");
        }

        private PreparedModel<List<TestItem>> CreateTestModel()
        {
            var data = new List<TestItem>
            {
                new TestItem { Name = "Test 1", Value = 100 }
            };

            return new PreparedModel<List<TestItem>>
            {
                Data = data,
                Metadata = new ReportMetadata
                {
                    Id = "test",
                    Title = "Test Report"
                },
                PreparedAt = new DateTimeOffset(2025, 10, 18, 0, 30, 0, TimeSpan.Zero)
            };
        }
    }

    public class YamlRendererTests
    {
        private readonly YamlRenderer _renderer;

        public YamlRendererTests()
        {
            _renderer = new YamlRenderer();
        }

        [Fact]
        public void Render_WithValidData_GeneratesValidYaml()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("data:");
            result.Should().Contain("metadata:");
        }

        [Fact]
        public void Render_UsesCamelCase()
        {
            var model = CreateTestModel();

            var result = _renderer.Render(model);

            result.Should().Contain("data:");
            result.Should().Contain("metadata:");
            result.Should().NotContain("Data:");
            result.Should().NotContain("Metadata:");
        }

        private PreparedModel<List<TestItem>> CreateTestModel()
        {
            var data = new List<TestItem>
            {
                new TestItem { Name = "Test 1", Value = 100 }
            };

            return new PreparedModel<List<TestItem>>
            {
                Data = data,
                Metadata = new ReportMetadata
                {
                    Id = "test",
                    Title = "Test Report"
                }
            };
        }
    }
}
