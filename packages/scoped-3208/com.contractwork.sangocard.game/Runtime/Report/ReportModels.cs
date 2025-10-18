using System;
using System.Collections.Generic;

namespace SangoCard.Game.Report
{
    [Flags]
    public enum ReportFormat
    {
        None = 0,
        Json = 1 << 0,
        Markdown = 1 << 1
    }

    public sealed class ReportColumnInfo
    {
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Format { get; set; }
        public int? SortOrder { get; set; }
    }

    public sealed class ReportMetadata
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IReadOnlyList<ReportColumnInfo> Columns { get; set; } = Array.Empty<ReportColumnInfo>();
    }

    public sealed class PreparedModel<T>
    {
        public T? Data { get; set; }
        public ReportMetadata Metadata { get; set; } = new ReportMetadata();
        public DateTimeOffset? PreparedAt { get; set; }
    }
}
