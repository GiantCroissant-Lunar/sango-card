namespace Reporting.Abstractions
{
    /// <summary>
    /// Container for emitted report artifacts. Not all formats must be populated for every run.
    /// </summary>
    public sealed class ReportResult
    {
        /// <summary>Markdown representation of the report.</summary>
        public string? Markdown { get; init; }

        /// <summary>JSON representation of the report.</summary>
        public string? Json { get; init; }

        /// <summary>XML representation of the report.</summary>
        public string? Xml { get; init; }

        /// <summary>YAML representation of the report.</summary>
        public string? Yaml { get; init; }

        /// <summary>HTML representation of the report.</summary>
        public string? Html { get; init; }
    }
}
