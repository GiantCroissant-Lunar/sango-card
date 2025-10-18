# Reporting.Core

Report engine implementation for SangoCard with multi-format rendering.

## Features

- **Simple Report Engine**: Transforms data models to multiple output formats
- **Multi-Format Renderers**:
  - **JSON** - Structured data with pretty-printing
  - **XML** - Standard XML format with proper escaping
  - **YAML** - Human-readable YAML format
  - **Markdown** - Tables and formatted text
  - **HTML** - Converted from Markdown using Markdig

## Dependencies

- **FastReport.OpenSource** (2024.2.0) - Report generation foundation
- **System.Text.Json** - JSON serialization
- **YamlDotNet** - YAML serialization
- **Markdig** - Markdown to HTML conversion

## Usage

```csharp
using Reporting.Core;
using Reporting.Abstractions;

var engine = new SimpleReportEngine();

var result = await engine.GenerateAsync(
    dataProvider,
    new ReportOptions
    {
        Formats = ReportFormat.Markdown | ReportFormat.Json
    }
);

// Access generated formats
var markdown = result.Markdown;
var json = result.Json;
```

## Architecture

### SimpleReportEngine

Main engine that orchestrates rendering:

1. Gets prepared data from provider
2. Invokes appropriate renderers based on requested formats
3. Returns `ReportResult` with generated outputs

### Renderers

Each renderer implements format-specific serialization:

- **JsonRenderer** - Uses System.Text.Json with camelCase naming
- **XmlRenderer** - Creates structured XML with proper elements
- **YamlRenderer** - Uses YamlDotNet with camelCase naming
- **MarkdownRenderer** - Creates tables with escaped special characters

## Error Handling

All renderers include error handling:

- Exceptions are caught and returned as error format
- Error results include report ID and error message
- No exceptions bubble up to callers

## Thread Safety

All renderers are thread-safe and can be reused across requests.

## Future Enhancements

- FastReport integration for advanced templates
- PDF export support
- Excel export support
- Custom template loading
- Aggregate calculations
- Grouping support

## License

MIT
