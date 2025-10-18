using System.Collections.Generic;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Options for configuring report generation.
    /// </summary>
    public sealed class ReportOptions
    {
        /// <summary>Requested output formats.</summary>
        public ReportFormat Formats { get; init; } = ReportFormat.Markdown;

        /// <summary>Optional title override.</summary>
        public string? Title { get; init; }

        /// <summary>Optional description override.</summary>
        public string? Description { get; init; }

        /// <summary>Custom parameters for the report engine.</summary>
        public IReadOnlyDictionary<string, object>? Parameters { get; init; }

        /// <summary>Whether to include detailed metadata in output.</summary>
        public bool IncludeMetadata { get; init; } = true;

        /// <summary>Optional template path override.</summary>
        public string? TemplatePath { get; init; }
    }
}
