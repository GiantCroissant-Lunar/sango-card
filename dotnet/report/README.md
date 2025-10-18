# SangoCard Reporting - .NET Libraries

This directory contains .NET libraries for the SangoCard reporting system.

## Projects

### Reporting.Abstractions

Lightweight abstractions package with no Unity dependencies. Provides:

- Marker attributes for report providers, data providers, actions, and templates
- Core interfaces (`IReportEngine`, `IReportDataProvider<T>`)
- Data models and metadata types
- Multi-format support (Markdown, JSON, XML, YAML, HTML)

Target frameworks: .NET Standard 2.1, .NET 6.0

### Reporting.Core

Report engine implementation with multi-format rendering. Provides:

- `SimpleReportEngine` - Main report generation engine
- Format-specific renderers (JSON, XML, YAML, Markdown)
- HTML generation via Markdig
- FastReport.OpenSource integration (foundation for advanced features)

Target framework: .NET 6.0

Dependencies: FastReport.OpenSource, System.Text.Json, YamlDotNet, Markdig

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test
```

### Test Results

- **Total Tests**: 44
- **Passed**: 44 ✅
- **Failed**: 0
- **Coverage**: Abstractions (22 tests), Core (22 tests)

## Packaging

```bash
dotnet pack -c Release
```

## Structure

```text
dotnet/report/
├── Reporting.Abstractions/    # Core abstractions
│   ├── Attributes/                      # Marker attributes
│   │   └── ReportAttributes.cs
│   ├── IReportEngine.cs                 # Engine interface
│   ├── IReportDataProvider.cs           # Provider interface
│   ├── ReportResult.cs                  # Output container
│   ├── ReportMetadata.cs                # Metadata types
│   ├── ReportOptions.cs                 # Configuration
│   ├── ReportFormat.cs                  # Format flags
│   └── PreparedModel.cs                 # Data container
├── Reporting.Core/            # Engine implementation
│   ├── SimpleReportEngine.cs            # Main engine
│   └── Renderers/                       # Format renderers
│       ├── JsonRenderer.cs
│       ├── XmlRenderer.cs
│       ├── YamlRenderer.cs
│       └── MarkdownRenderer.cs
├── SangoCard.sln              # Solution file
└── Directory.Build.props                # Build settings
```

## Design Principles

1. **No Unity Dependencies**: Pure .NET for cross-platform compatibility
2. **Async-First**: All I/O operations are asynchronous with cancellation support
3. **Attribute-Driven**: Mark classes for auto-discovery by source generators
4. **Multi-Format**: Support multiple output formats from single data source
5. **Metadata-Rich**: Comprehensive metadata for tooling and UI generation

## Integration

These libraries integrate with:

- Unity packages via IL2CPP compatible APIs
- Source generators for automatic provider registration
- Report engines (Markdown, FastReport, etc.)
- Editor tools and runtime reporting systems

## References

This implementation is inspired by:

- `nfun-report` (Plate.Reporting.Abstractions)
- `giantcroissant-lunar-report` (Lunar.NfunReport.Abstractions)

## License

See LICENSE file in repository root.
