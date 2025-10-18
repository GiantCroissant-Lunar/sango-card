# Reporting.Abstractions

Lightweight abstractions for the SangoCard reporting system. This package provides:

- **Marker Attributes**: `[ReportProvider]`, `[ReportDataProvider]`, `[ReportAction]`, `[ReportTemplate]`
- **Core Interfaces**: `IReportEngine`, `IReportDataProvider<T>`
- **Data Models**: `ReportResult`, `ReportMetadata`, `PreparedModel<T>`, `ReportOptions`
- **Enums**: `ReportFormat`

## Features

- **Zero Unity Dependencies**: Pure .NET Standard 2.1 / .NET 6.0 library
- **Attribute-Driven**: Mark classes with attributes for auto-discovery by source generators
- **Async-First**: All data operations are async with cancellation support
- **Multi-Format Output**: Support for Markdown, JSON, XML, YAML, and HTML
- **Metadata-Rich**: Comprehensive column and report metadata

## Usage Example

```csharp
using Reporting.Abstractions;
using Reporting.Abstractions.Attributes;

[ReportProvider("card-stats", Name = "Card Statistics", Category = "Card")]
public class CardStatsProvider : IReportDataProvider<CardStatsData>
{
    public async Task<PreparedModel<CardStatsData>> GetDataAsync(CancellationToken ct = default)
    {
        var data = await FetchCardStatsAsync(ct);
        return new PreparedModel<CardStatsData>
        {
            Data = data,
            Metadata = GetMetadata(),
            PreparedAt = DateTimeOffset.UtcNow
        };
    }

    public ReportMetadata GetMetadata()
    {
        return new ReportMetadata
        {
            Id = "card-stats",
            Title = "Card Statistics Report",
            Columns = new[]
            {
                new ReportColumnInfo { PropertyName = "CardId", DisplayName = "Card ID" },
                new ReportColumnInfo { PropertyName = "Name", DisplayName = "Card Name" },
                new ReportColumnInfo { PropertyName = "Uses", DisplayName = "Total Uses", DataType = "int" }
            }
        };
    }
}
```

## Integration

This library is designed to work with:

- Source generators for automatic registration
- Unity integration layer (separate package)
- Report engines that transform data to various formats

## License

MIT
