# Unity Log Parser

Automated tool to extract, deduplicate, and analyze errors and warnings from Unity build logs.

**Available in two implementations:**
- **Python** (recommended) - Enhanced features, better performance, more output formats
- **PowerShell** - Legacy version, cross-platform PowerShell Core support

## Features

### Core Capabilities
- **Error Detection**: Extracts compiler errors, exceptions, and build failures
- **Smart Deduplication**: Groups identical errors and counts occurrences
- **Multiple Formats**: Summary (console), JSON, Markdown, CSV (Python only)
- **Severity Classification**: CRITICAL, HIGH, MEDIUM, LOW
- **Category Grouping**: Organizes by error category (Async/Await, Obsolete API, etc.)

### Python Version Enhancements
- ✅ Dataclass-based architecture for type safety
- ✅ CSV export for spreadsheet analysis
- ✅ Severity levels (CRITICAL/HIGH/MEDIUM/LOW)
- ✅ Category-based grouping (Async/Await, Obsolete API, Unused Code, etc.)
- ✅ Better performance (~0.2s vs 0.3s for 3300 lines)
- ✅ Verbose mode with statistics
- ✅ Extensible pattern system

## Quick Start

```bash
# Parse latest build log (summary to console)
task logs:parse:build

# Parse with errors only
task logs:parse:build ERRORS_ONLY=true

# Generate reports
task logs:report OUTPUT=output/build-errors.md      # Markdown
task logs:report:json OUTPUT=output/build-errors.json  # JSON
task logs:report:csv OUTPUT=output/build-errors.csv   # CSV

# Direct Python usage
python build/nuke/scripts/parse_unity_log.py output/unity-build.log
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --format json
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --errors-only --verbose
```

## Output Formats

### Summary (Console)
Color-coded console output with:
- Error count summary with severity levels
- Grouped errors by severity (CRITICAL → LOW)
- Category-based warning organization
- Top 3 examples per warning group
- Parse statistics

Example:
```
===================================================================
  Unity Log Analysis
===================================================================

Summary:
  Unique Errors:      2
  Unique Warnings:    12
  Unique Exceptions:  0
  Total Lines Parsed: 3309
  Parse Time:         0.218s

-------------------------------------------------------------------
  ERRORS (2 unique)
-------------------------------------------------------------------

  [MsgPack009] (×6) [CRITICAL]
  📄 MessagePack_Formatters_Quest_Component_QuestComponentFormatter.cs:19:25
  💬 Multiple formatters for type Quest.Component.QuestDataComponent found
  🏷️  Code Generation
```

### Markdown
Documentation-ready markdown with:
- Summary table with parse statistics
- Errors grouped by severity level
- Warnings organized by category
- File and line references with occurrence counts

### JSON
Machine-readable format for CI/CD:
```json
{
  "timestamp": "2025-10-19T18:07:34Z",
  "summary": {
    "errors": 2,
    "warnings": 12,
    "exceptions": 0,
    "total_lines": 3309,
    "parse_time": 0.224
  },
  "entries": [
    {
      "type": "Error",
      "code": "MsgPack009",
      "severity": "critical",
      "category": "Code Generation",
      "file": "...",
      "line": 19,
      "column": 25,
      "message": "...",
      "count": 6,
      "location": "file:19:25"
    }
  ]
}
```

### CSV
Spreadsheet-compatible format:
```csv
type,code,severity,category,file,line,column,message,count
Error,MsgPack009,critical,Code Generation,Assets\Scripts\...,19,25,Multiple formatters...,6
Warning,CS0618,low,Obsolete API,Assets\FacebookSDK\...,13,40,'Object.FindObjectOfType...',20
```

## Current Unity Build Errors

Based on latest analysis (`output/unity-build.log`):

### Critical Errors (2 unique, 12 occurrences)

Both are **MsgPack009** - Duplicate MessagePack formatters:

1. **Quest.Component.QuestDataComponent** (×6)
   - File: `MessagePack_Formatters_Quest_Component_QuestComponentFormatter.cs:19:25`
   - Category: Code Generation
   - Severity: CRITICAL

2. **Shop.Component.ProductDataComponent** (×6)
   - File: `MessagePack_Formatters_Shop_Component_ProductDataComponentFormatter.cs:19:25`
   - Category: Code Generation
   - Severity: CRITICAL

### Warning Categories (12 unique, 284 total)

1. **Async/Await Pattern** (128 occurrences)
   - CS1998: Async without await (96×)
   - CS4014: Unawaited async calls (32×)

2. **Nullable Reference** (84 occurrences)
   - CS8632: Annotations outside nullable context

3. **Obsolete API** (44 occurrences)
   - CS0618: Deprecated Unity APIs

4. **Unused Code** (24 occurrences)
   - CS0168: Unused variables (12×)
   - CS0414: Unused fields (8×)
   - CS0067: Unused events (8×)

### Resolution Steps

**For MsgPack009 Errors:**

1. Check for duplicate MessagePack attributes:
   ```bash
   git grep -n "MessagePackObject.*QuestDataComponent"
   git grep -n "MessagePackObject.*ProductDataComponent"
   ```

2. Look for multiple partial class definitions:
   ```bash
   git grep -n "partial class QuestDataComponent"
   git grep -n "partial class ProductDataComponent"
   ```

3. Clean and regenerate formatters:
   ```bash
   Remove-Item "projects/client/Assets/Scripts/MessagePackGenerated/*" -Recurse
   task build:unity:prepared
   ```

## Task Reference

| Task | Description |
|------|-------------|
| `logs:parse` | Parse Unity log with customizable options |
| `logs:parse:build` | Quick parse of latest build log |
| `logs:parse:test` | Quick parse of latest test log |
| `logs:report` | Generate markdown report |
| `logs:report:json` | Generate JSON report |
| `logs:report:csv` | Generate CSV report |

### Parameters

- `LOG`: Path to log file (default: `output/unity-build.log`)
- `FORMAT`: Output format - `summary`, `json`, `markdown`, `csv` (Python only)
- `ERRORS_ONLY`: Set to `true` to hide warnings
- `OUTPUT`: Output file path for reports

## Integration Examples

### CI/CD Pipeline (GitHub Actions)

```yaml
- name: Parse Unity Build Logs
  run: |
    task logs:report:json OUTPUT=build-errors.json
    task logs:report:csv OUTPUT=build-errors.csv

- name: Upload Error Reports
  uses: actions/upload-artifact@v3
  with:
    name: unity-build-analysis
    path: |
      output/build-errors.json
      output/build-errors.csv

- name: Check for Critical Errors
  run: |
    python -c "
    import json
    with open('output/build-errors.json') as f:
        data = json.load(f)
        critical = [e for e in data['entries'] if e['severity'] == 'critical']
        if critical:
            print(f'❌ {len(critical)} CRITICAL errors found!')
            exit(1)
    "
```

### Pre-Commit Hook

```bash
#!/bin/bash
# .git/hooks/pre-push

task build:unity:prepared || {
  echo "Build failed. Analyzing errors..."
  task logs:parse:build ERRORS_ONLY=true
  exit 1
}
```

### Local Development

```bash
# Quick error check after build
task build:unity:prepared && task logs:parse:build

# Generate detailed report for issue tracking
task logs:report OUTPUT=docs/build-status.md
```

### Excel/Spreadsheet Analysis

```bash
# Export to CSV for Excel analysis
task logs:report:csv OUTPUT=build-errors.csv

# Import into Excel, Google Sheets, etc.
# Create pivot tables, charts, trend analysis
```

## Advanced Usage

### Direct Python Usage

```bash
# Basic parsing
python build/nuke/scripts/parse_unity_log.py output/unity-build.log

# Verbose mode with statistics
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --verbose

# Multiple formats
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --format json
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --format csv
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --format markdown

# Errors only
python build/nuke/scripts/parse_unity_log.py output/unity-build.log --errors-only

# Save to file
python build/nuke/scripts/parse_unity_log.py output/unity-build.log \
  --format markdown \
  --output docs/build-errors.md
```

### Programmatic Usage (Python)

```python
from build.nuke.scripts.parse_unity_log import UnityLogParser, LogFormatter
from pathlib import Path

# Parse log
parser = UnityLogParser()
result = parser.parse_file(Path('output/unity-build.log'))

# Get critical errors
critical = result.get_by_severity('critical')
print(f"Found {len(critical)} critical errors")

# Generate custom report
for error in result.get_by_type('Error'):
    print(f"{error.code}: {error.message} (×{error.count})")

# Export to JSON
formatter = LogFormatter()
json_output = formatter.format_json(result)
```

## Script Locations

- **Python Parser**: `build/nuke/scripts/parse_unity_log.py` (primary)
- **Tasks**: Defined in root `Taskfile.yml`
- **Output**: `output/` directory (gitignored)

## Requirements

- Python 3.10+ (uses match-case, dataclasses)
- No external dependencies (stdlib only)

## Performance Comparison

Parsing 3309 lines from unity-build.log:
- **Parse Time**: ~0.2-0.3 seconds
- **Memory Usage**: Low (streaming parser)
- **CPU Usage**: Minimal (single-threaded regex matching)

## Architecture Benefits

1. **Type Safety**: Dataclasses for structured data
2. **Performance**: Fast regex-based parsing
3. **Extensibility**: Easy to add new patterns, analyzers
4. **CSV Export**: Spreadsheet integration
5. **Severity Levels**: Automatic classification
6. **Categories**: Smart grouping by error type
7. **Better Testing**: Unit test friendly
8. **Future ML**: Ready for ML-based error classification

---

**See Also**:
- [Build System Documentation](../build/README.md)
- [Unity Build Tasks](../../Taskfile.yml)
- [Implementation Summary](./unity-log-parser-summary.md)
