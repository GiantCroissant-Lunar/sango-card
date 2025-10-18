---
title: Report Usage Examples
type: guide
status: active
created: 2025-10-18
tags:
  - dotnet
  - reporting
---
# SangoCard Reporting - Usage Examples

## Basic Usage

### 1. Define Your Data Model

```csharp
public class CardStatistics
{
    public string CardId { get; set; } = "";
    public string CardName { get; set; } = "";
    public int TotalUses { get; set; }
    public int TotalWins { get; set; }
    public decimal WinRate { get; set; }
}
```

### 2. Create a Data Provider

```csharp
using Reporting.Abstractions;
using Reporting.Abstractions.Attributes;

[ReportProvider("card-stats", Name = "Card Statistics", Category = "Card")]
public class CardStatsDataProvider : IReportDataProvider<CardStatistics>
{
    private readonly ICardRepository _repository;

    public CardStatsDataProvider(ICardRepository repository)
    {
        _repository = repository;
    }

    public async Task<PreparedModel<CardStatistics>> GetDataAsync(CancellationToken ct = default)
    {
        var stats = await _repository.GetCardStatisticsAsync(ct);

        return new PreparedModel<CardStatistics>
        {
            Data = stats,
            Metadata = GetMetadata(),
            PreparedAt = DateTimeOffset.UtcNow,
            Context = new Dictionary<string, object>
            {
                ["TotalCards"] = stats.Count(),
                ["GeneratedBy"] = "CardStatsDataProvider"
            }
        };
    }

    public ReportMetadata GetMetadata()
    {
        return new ReportMetadata
        {
            Id = "card-stats",
            Title = "Card Statistics Report",
            SchemaVersion = "1.0",
            Description = "Comprehensive statistics for all cards in the game",
            Columns = new[]
            {
                new ReportColumnInfo
                {
                    PropertyName = "CardId",
                    DisplayName = "Card ID",
                    DataType = "string",
                    SortOrder = 0
                },
                new ReportColumnInfo
                {
                    PropertyName = "CardName",
                    DisplayName = "Card Name",
                    DataType = "string",
                    SortOrder = 1
                },
                new ReportColumnInfo
                {
                    PropertyName = "TotalUses",
                    DisplayName = "Uses",
                    DataType = "int",
                    SortOrder = 2,
                    IncludeInSummary = true
                },
                new ReportColumnInfo
                {
                    PropertyName = "TotalWins",
                    DisplayName = "Wins",
                    DataType = "int",
                    SortOrder = 3,
                    IncludeInSummary = true
                },
                new ReportColumnInfo
                {
                    PropertyName = "WinRate",
                    DisplayName = "Win Rate",
                    DataType = "decimal",
                    Format = "P2",
                    SortOrder = 4
                }
            }
        };
    }
}
```

### 3. Generate Reports

```csharp
using Reporting.Core;
using Reporting.Abstractions;

// Create the engine
var engine = new SimpleReportEngine();

// Create the data provider
var dataProvider = new CardStatsDataProvider(cardRepository);

// Generate Markdown report
var markdownResult = await engine.GenerateAsync(
    dataProvider,
    new ReportOptions
    {
        Formats = ReportFormat.Markdown
    }
);

Console.WriteLine(markdownResult.Markdown);

// Generate multiple formats
var multiFormatResult = await engine.GenerateAsync(
    dataProvider,
    new ReportOptions
    {
        Formats = ReportFormat.Markdown | ReportFormat.Json | ReportFormat.Html,
        IncludeMetadata = true,
        Title = "Custom Report Title"
    }
);

// Save to files
await File.WriteAllTextAsync("report.md", multiFormatResult.Markdown);
await File.WriteAllTextAsync("report.json", multiFormatResult.Json);
await File.WriteAllTextAsync("report.html", multiFormatResult.Html);
```

## Advanced Scenarios

### Custom Formatting

```csharp
public ReportMetadata GetMetadata()
{
    return new ReportMetadata
    {
        Id = "player-activity",
        Title = "Player Activity Report",
        Columns = new[]
        {
            new ReportColumnInfo
            {
                PropertyName = "PlayerId",
                DisplayName = "Player ID"
            },
            new ReportColumnInfo
            {
                PropertyName = "LastActive",
                DisplayName = "Last Active",
                DataType = "date",
                Format = "yyyy-MM-dd HH:mm:ss"
            },
            new ReportColumnInfo
            {
                PropertyName = "TotalSpent",
                DisplayName = "Total Spent",
                DataType = "decimal",
                Format = "C2"  // Currency format
            }
        }
    };
}
```

### With Cancellation Token

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await engine.GenerateAsync(
        dataProvider,
        new ReportOptions { Formats = ReportFormat.Json },
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Report generation timed out");
}
```

### Error Handling

```csharp
var result = await engine.GenerateAsync(dataProvider);

// The engine never throws - it returns error results
if (result.Json?.Contains("\"error\"") == true)
{
    Console.WriteLine("Report generation encountered an error");
    Console.WriteLine(result.Json);
}
```

## Output Examples

### Markdown Output

```markdown
# Card Statistics Report

Comprehensive statistics for all cards in the game

## Report Information

- **Report ID:** card-stats
- **Generated At:** 2025-10-18 00:30:00 UTC

## Data

| Card ID | Card Name | Uses | Wins | Win Rate |
| --- | --- | --- | --- | --- |
| CARD001 | Fireball | 150 | 90 | 60.00% |
| CARD002 | Ice Shield | 120 | 72 | 60.00% |
| CARD003 | Lightning Bolt | 200 | 140 | 70.00% |
```

### JSON Output

```json
{
  "data": [
    {
      "cardId": "CARD001",
      "cardName": "Fireball",
      "totalUses": 150,
      "totalWins": 90,
      "winRate": 0.6
    }
  ],
  "metadata": {
    "id": "card-stats",
    "title": "Card Statistics Report",
    "schemaVersion": "1.0"
  },
  "preparedAt": "2025-10-18T00:30:00.000Z"
}
```

## Integration with Unity

Unity code should reference only the **Abstractions** package:

```csharp
// In Unity MonoBehaviour or ScriptableObject
using Reporting.Abstractions;
using Reporting.Abstractions.Attributes;

[ReportProvider("game-state", Category = "Debug")]
public class GameStateReportProvider : IReportDataProvider<GameStateData>
{
    // Implementation
}
```

The Core engine runs outside Unity (editor tools, build pipeline, or separate service).

## License

MIT
