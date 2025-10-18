using Reporting.Abstractions.Attributes;
using Xunit;

namespace Reporting.Abstractions.Tests
{
    public class ReportAttributesTests
    {
        [Fact]
        public void ReportProviderAttribute_Constructor_SetsId()
        {
            var attribute = new ReportProviderAttribute("test-id");

            Assert.Equal("test-id", attribute.Id);
            Assert.Null(attribute.Name);
            Assert.Null(attribute.Version);
            Assert.Null(attribute.Category);
        }

        [Fact]
        public void ReportProviderAttribute_Properties_CanBeSet()
        {
            var attribute = new ReportProviderAttribute("test-id")
            {
                Name = "Test Report",
                Version = "1.0",
                Category = "Analytics"
            };

            Assert.Equal("test-id", attribute.Id);
            Assert.Equal("Test Report", attribute.Name);
            Assert.Equal("1.0", attribute.Version);
            Assert.Equal("Analytics", attribute.Category);
        }

        [Fact]
        public void ReportDataProviderAttribute_Constructor_SetsId()
        {
            var attribute = new ReportDataProviderAttribute("data-provider");

            Assert.Equal("data-provider", attribute.Id);
            Assert.Null(attribute.Version);
        }

        [Fact]
        public void ReportDataProviderAttribute_Version_CanBeSet()
        {
            var attribute = new ReportDataProviderAttribute("data-provider")
            {
                Version = "2.0"
            };

            Assert.Equal("data-provider", attribute.Id);
            Assert.Equal("2.0", attribute.Version);
        }

        [Fact]
        public void ReportActionAttribute_Constructor_SetsId()
        {
            var attribute = new ReportActionAttribute("action-id");

            Assert.Equal("action-id", attribute.Id);
            Assert.Null(attribute.DisplayName);
        }

        [Fact]
        public void ReportActionAttribute_DisplayName_CanBeSet()
        {
            var attribute = new ReportActionAttribute("action-id")
            {
                DisplayName = "Execute Action"
            };

            Assert.Equal("action-id", attribute.Id);
            Assert.Equal("Execute Action", attribute.DisplayName);
        }

        [Fact]
        public void ReportTemplateAttribute_Constructor_SetsProperties()
        {
            var attribute = new ReportTemplateAttribute("template-id", "text/markdown");

            Assert.Equal("template-id", attribute.Id);
            Assert.Equal("text/markdown", attribute.ContentType);
        }
    }

    [ReportProvider("test-report", Name = "Test Report", Category = "Test")]
    public class TestReportProvider
    {
    }

    [ReportDataProvider("test-data")]
    public class TestDataProvider
    {
    }

    public class AttributeUsageTests
    {
        [Fact]
        public void ReportProviderAttribute_CanBeAppliedToClass()
        {
            var type = typeof(TestReportProvider);
            var attributes = type.GetCustomAttributes(typeof(ReportProviderAttribute), false);

            Assert.Single(attributes);
            var attribute = (ReportProviderAttribute)attributes[0];
            Assert.Equal("test-report", attribute.Id);
            Assert.Equal("Test Report", attribute.Name);
            Assert.Equal("Test", attribute.Category);
        }

        [Fact]
        public void ReportDataProviderAttribute_CanBeAppliedToClass()
        {
            var type = typeof(TestDataProvider);
            var attributes = type.GetCustomAttributes(typeof(ReportDataProviderAttribute), false);

            Assert.Single(attributes);
            var attribute = (ReportDataProviderAttribute)attributes[0];
            Assert.Equal("test-data", attribute.Id);
        }
    }
}
