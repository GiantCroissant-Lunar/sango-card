using System;
using System.Collections.Generic;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Metadata describing the structure and properties of a report.
    /// </summary>
    public sealed class ReportMetadata
    {
        /// <summary>Unique identifier for the report.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Human-readable title.</summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>Schema version for forward-compatible evolution.</summary>
        public string SchemaVersion { get; init; } = "1.0";

        /// <summary>Optional description of the report.</summary>
        public string? Description { get; init; }

        /// <summary>Column definitions for tabular reports.</summary>
        public IReadOnlyList<ReportColumnInfo> Columns { get; init; } = Array.Empty<ReportColumnInfo>();

        /// <summary>Optional template reference.</summary>
        public ReportTemplate? Template { get; init; }
    }

    /// <summary>
    /// Describes a single column in a tabular report.
    /// </summary>
    public sealed class ReportColumnInfo
    {
        /// <summary>Property name in the data model.</summary>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>Display name for the column.</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>Data type (e.g., "string", "int", "decimal", "date").</summary>
        public string DataType { get; init; } = "string";

        /// <summary>Format string for rendering (e.g., "N2", "yyyy-MM-dd").</summary>
        public string? Format { get; init; }

        /// <summary>Suggested display width.</summary>
        public int? Width { get; init; }

        /// <summary>Sort order for display.</summary>
        public int? SortOrder { get; init; }

        /// <summary>Whether to include in summary views.</summary>
        public bool IncludeInSummary { get; init; }
    }

    /// <summary>
    /// Reference to a report template.
    /// </summary>
    public sealed class ReportTemplate
    {
        /// <summary>Template kind (e.g., "Markdown", "RazorTemplate", "FastReportFrx").</summary>
        public string Kind { get; init; } = "Markdown";

        /// <summary>Path to the template (absolute or repo-relative).</summary>
        public string Path { get; init; } = string.Empty;
    }
}
