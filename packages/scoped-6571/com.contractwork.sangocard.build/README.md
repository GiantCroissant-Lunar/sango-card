# SangoCard.Build

Build preparation package for Unity projects.

## Documentation

Full specification: [Build Preparation Tool](../../../docs/specs/build-preparation-tool.md)

## Quick Start

This package provides a .NET CLI/TUI tool for managing Unity build preparation configurations.

### Installation

Install via Unity Package Manager scoped registry:

```json
{
  "scopedRegistries": [
    {
      "name": "Contract Work",
      "url": "https://npm.pkg.github.com/@contractwork",
      "scopes": ["com.contractwork"]
    }
  ],
  "dependencies": {
    "com.contractwork.sangocard.build": "0.1.0"
  }
}
```

### Usage

The tool is located at `packages/scoped-6571/com.contractwork.sangocard.build/dotnet~/tool/`.

**CLI Mode**:

```bash
dotnet run -- prepare run --config path/to/config.json --client path/to/client
```

**TUI Mode**:

```bash
dotnet run -- tui
```

For detailed usage, commands, architecture, and technical details, see the [specification](../../../docs/specs/build-preparation-tool.md).

## Features

- Git root-based path resolution
- Reactive architecture with MessagePipe
- Roslyn-based C# code patching
- Unity YAML manipulation
- Terminal.Gui v2 TUI interface
- System.CommandLine CLI

## License

See LICENSE file in project root.
