# Unity Log Parser - Quick Reference

Location: `build/nuke/scripts/parse_unity_log.py`

## Quick Commands

```bash
# Analyze latest build
task logs:parse:build

# Show only critical/high errors
task logs:parse:build ERRORS_ONLY=true

# Generate reports
task logs:report                    # Markdown
task logs:report:json               # JSON for CI/CD
task logs:report:csv                # CSV for Excel

# Custom analysis
task logs:parse LOG=custom.log FORMAT=json
```

## Output Formats

- **summary** - Color-coded console with severity badges
- **json** - Machine-readable for CI/CD
- **markdown** - Documentation-ready with grouping
- **csv** - Excel/spreadsheet compatible

## Severity Levels

- **CRITICAL** - Build blockers (e.g., MsgPack009)
- **HIGH** - Missing types/methods
- **MEDIUM** - Async issues, nullable context
- **LOW** - Code quality warnings

## Categories

- Code Generation (MessagePack, Roslyn, etc.)
- Async/Await Pattern
- Obsolete API
- Unused Code
- Nullable Reference
- Member Hiding

## CI/CD Integration

```yaml
# Fail on critical errors
- name: Check Build Errors
  run: |
    task logs:report:json OUTPUT=errors.json
    python -c "
      import json
      data = json.load(open('errors.json'))
      critical = [e for e in data['entries'] if e['severity'] == 'critical']
      if critical:
        print(f'❌ {len(critical)} CRITICAL errors!')
        exit(1)
    "
```

## Direct Usage

```bash
# Basic
python build/nuke/scripts/parse_unity_log.py output/unity-build.log

# With options
python build/nuke/scripts/parse_unity_log.py output/unity-build.log \
  --format json \
  --output errors.json \
  --verbose
```

## Programmatic Usage

```python
from build.nuke.scripts.parse_unity_log import UnityLogParser

parser = UnityLogParser()
result = parser.parse_file(Path('output/unity-build.log'))

# Get critical issues
critical = result.get_by_severity('critical')
for error in critical:
    print(f"{error.code}: {error.message}")
```

## Help

```bash
python build/nuke/scripts/parse_unity_log.py --help
```

Full documentation: [docs/tools/unity-log-parser.md](../../docs/tools/unity-log-parser.md)
