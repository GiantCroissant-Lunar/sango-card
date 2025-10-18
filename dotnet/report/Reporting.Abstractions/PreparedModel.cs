using System;
using System.Collections.Generic;

namespace Reporting.Abstractions
{
    /// <summary>
    /// Container for prepared data model with metadata.
    /// </summary>
    public sealed class PreparedModel<T>
    {
        /// <summary>The prepared data payload.</summary>
        public T Data { get; init; } = default!;

        /// <summary>Metadata about the report.</summary>
        public ReportMetadata Metadata { get; init; } = new();

        /// <summary>Optional timestamp when the data was prepared.</summary>
        public DateTimeOffset? PreparedAt { get; init; }

        /// <summary>Optional additional context data.</summary>
        public IReadOnlyDictionary<string, object>? Context { get; init; }
    }
}
