using System;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Flags representing available report output formats.
    /// </summary>
    [Flags]
    public enum ReportFormat
    {
        /// <summary>No format specified.</summary>
        None = 0,

        /// <summary>Markdown format.</summary>
        Markdown = 1 << 0,

        /// <summary>JSON format.</summary>
        Json = 1 << 1,

        /// <summary>XML format.</summary>
        Xml = 1 << 2,

        /// <summary>YAML format.</summary>
        Yaml = 1 << 3,

        /// <summary>HTML format.</summary>
        Html = 1 << 4,

        /// <summary>All available formats.</summary>
        All = Markdown | Json | Xml | Yaml | Html
    }
}
