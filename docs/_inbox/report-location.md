---
title: Report Location
type: guide
status: active
created: 2025-10-18
tags:
  - dotnet
  - reporting
---
# SangoCard Reporting Library - Location

This directory contains the complete SangoCard reporting library.

## Location

**Path**: `D:\lunar-snake\constract-work\card-projects\sango-card\dotnet\report\`

## Structure

```
report/
├── SangoCard.sln                       # Solution file
├── Directory.Build.props                         # Build configuration
├── .gitignore                                    # Git ignore rules
├── README.md                                     # Main documentation
├── PROJECT-SUMMARY.md                            # Creation summary
├── USAGE-EXAMPLES.md                             # Usage examples
├── Reporting.Abstractions/             # Abstractions library
│   ├── Attributes/
│   │   └── ReportAttributes.cs
│   ├── IReportEngine.cs
│   ├── IReportDataProvider.cs
│   ├── ReportResult.cs
│   ├── ReportMetadata.cs
│   ├── ReportOptions.cs
│   ├── ReportFormat.cs
│   └── PreparedModel.cs
├── Reporting.Core/                     # Core implementation
│   ├── SimpleReportEngine.cs
│   └── Renderers/
│       ├── JsonRenderer.cs
│       ├── XmlRenderer.cs
│       ├── YamlRenderer.cs
│       └── MarkdownRenderer.cs
└── tests/                                        # Test projects
    ├── Reporting.Abstractions.Tests/
    │   ├── ReportAttributesTests.cs
    │   ├── ReportMetadataTests.cs
    │   └── ReportModelsTests.cs
    └── Reporting.Core.Tests/
        ├── SimpleReportEngineTests.cs
        ├── MarkdownRendererTests.cs
        └── RenderersTests.cs
```

## Quick Commands

```bash
# Navigate to the report library
cd D:\lunar-snake\constract-work\card-projects\sango-card\dotnet\report

# Build
dotnet build

# Run tests
dotnet test

# Pack for distribution
dotnet pack -c Release
```

## Status

✅ Successfully moved from `packages/scoped-2151/com.contractwork.sangocard.cross/dotnet/report`  
✅ All 44 tests passing  
✅ Clean build with no warnings  
✅ Ready for development

## Date Moved

2025-10-18
