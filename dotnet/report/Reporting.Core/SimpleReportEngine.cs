using System;
using System.Threading;
using System.Threading.Tasks;
using Reporting.Abstractions;
using Reporting.Core.Renderers;

namespace Reporting.Core
{
    /// <summary>
    /// Simple report engine implementation that transforms data to multiple output formats.
    /// This is a simplified version without FastReport dependency for initial implementation.
    /// </summary>
    public class SimpleReportEngine : IReportEngine
    {
        private readonly JsonRenderer _jsonRenderer = new();
        private readonly XmlRenderer _xmlRenderer = new();
        private readonly YamlRenderer _yamlRenderer = new();
        private readonly MarkdownRenderer _markdownRenderer = new();

        /// <summary>
        /// Generates report artifacts for the given provider.
        /// </summary>
        public async Task<ReportResult> GenerateAsync<T>(IReportDataProvider<T> dataProvider, ReportOptions? options = null, CancellationToken ct = default)
        {
            options ??= new ReportOptions();

            try
            {
                ct.ThrowIfCancellationRequested();

                var preparedModel = await dataProvider.GetDataAsync(ct);
                ct.ThrowIfCancellationRequested();

                return await RenderToFormatsAsync(preparedModel, options, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return CreateErrorResult(dataProvider.GetMetadata(), ex);
            }
        }

        private async Task<ReportResult> RenderToFormatsAsync<T>(PreparedModel<T> preparedModel, ReportOptions options, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                string? json = null;
                string? xml = null;
                string? yaml = null;
                string? markdown = null;
                string? html = null;

                if (options.Formats.HasFlag(ReportFormat.Json))
                {
                    ct.ThrowIfCancellationRequested();
                    json = _jsonRenderer.Render(preparedModel);
                }

                if (options.Formats.HasFlag(ReportFormat.Xml))
                {
                    ct.ThrowIfCancellationRequested();
                    xml = _xmlRenderer.Render(preparedModel);
                }

                if (options.Formats.HasFlag(ReportFormat.Yaml))
                {
                    ct.ThrowIfCancellationRequested();
                    yaml = _yamlRenderer.Render(preparedModel);
                }

                if (options.Formats.HasFlag(ReportFormat.Markdown))
                {
                    ct.ThrowIfCancellationRequested();
                    markdown = _markdownRenderer.Render(preparedModel);
                }

                if (options.Formats.HasFlag(ReportFormat.Html))
                {
                    ct.ThrowIfCancellationRequested();
                    // HTML needs markdown first
                    if (markdown == null)
                    {
                        markdown = _markdownRenderer.Render(preparedModel);
                    }
#if NET6_0
                    html = Markdig.Markdown.ToHtml(markdown);
#else
                    // HTML rendering not available on netstandard2.1 build
                    html = null;
#endif
                }

                return new ReportResult
                {
                    Json = json,
                    Xml = xml,
                    Yaml = yaml,
                    Markdown = markdown,
                    Html = html
                };
            }, ct);
        }

        private static ReportResult CreateErrorResult(ReportMetadata metadata, Exception ex)
        {
            var errorMessage = $"Report generation failed for {metadata.Id}: {ex.Message}";

            return new ReportResult
            {
                Markdown = $"# Error\n\n{errorMessage}",
                Json = System.Text.Json.JsonSerializer.Serialize(new { error = errorMessage, reportId = metadata.Id }),
                Xml = $"<error reportId=\"{System.Security.SecurityElement.Escape(metadata.Id)}\">{System.Security.SecurityElement.Escape(errorMessage)}</error>",
                Yaml = $"error: \"{errorMessage.Replace("\"", "\\\"")}\"\nreportId: \"{metadata.Id}\""
            };
        }
    }
}
