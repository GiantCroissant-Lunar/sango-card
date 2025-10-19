#!/usr/bin/env python3
"""
Unity Log Parser - Python Implementation

Extracts, deduplicates, and analyzes errors and warnings from Unity build logs.
Provides multiple output formats and advanced analysis capabilities.

Usage:
    python parse_unity_log.py <log_file> [options]
    python parse_unity_log.py output/unity-build.log --format summary
    python parse_unity_log.py output/unity-build.log --format json > errors.json
    python parse_unity_log.py output/unity-build.log --errors-only
"""

import re
import sys
import json
import argparse
from dataclasses import dataclass, field, asdict
from typing import List, Dict, Optional, Set
from pathlib import Path
from datetime import datetime
from collections import defaultdict
from enum import Enum


class ErrorType(Enum):
    """Error classification types"""
    COMPILER_ERROR = "Error"
    COMPILER_WARNING = "Warning"
    EXCEPTION = "Exception"
    BUILD_ERROR = "BuildError"
    UNITY_ERROR = "UnityError"
    RUNTIME_ERROR = "RuntimeError"


class Severity(Enum):
    """Error severity levels"""
    CRITICAL = "critical"
    HIGH = "high"
    MEDIUM = "medium"
    LOW = "low"
    INFO = "info"


@dataclass
class LogEntry:
    """Represents a single log entry (error/warning/exception)"""
    type: str
    code: str
    message: str
    file: Optional[str] = None
    line: Optional[int] = None
    column: Optional[int] = None
    full_text: str = ""
    count: int = 1
    severity: str = "medium"
    category: Optional[str] = None

    def get_key(self) -> str:
        """Generate unique key for deduplication"""
        return f"{self.type}|{self.code}|{self.message}"

    def get_location(self) -> str:
        """Get file location string"""
        if self.file:
            return f"{self.file}:{self.line}:{self.column}"
        return ""

    def to_dict(self) -> dict:
        """Convert to dictionary"""
        return {
            'type': self.type,
            'code': self.code,
            'message': self.message,
            'file': self.file,
            'line': self.line,
            'column': self.column,
            'count': self.count,
            'severity': self.severity,
            'category': self.category,
            'location': self.get_location()
        }


@dataclass
class ParseResult:
    """Results from parsing a log file"""
    entries: List[LogEntry] = field(default_factory=list)
    total_lines: int = 0
    parse_time: float = 0.0
    timestamp: str = field(default_factory=lambda: datetime.now().isoformat())

    def get_summary(self) -> Dict[str, int]:
        """Get count summary by type"""
        summary = defaultdict(int)
        for entry in self.entries:
            summary[entry.type] += 1
        return dict(summary)

    def get_by_type(self, entry_type: str) -> List[LogEntry]:
        """Get all entries of a specific type"""
        return [e for e in self.entries if e.type == entry_type]

    def get_by_severity(self, severity: str) -> List[LogEntry]:
        """Get all entries of a specific severity"""
        return [e for e in self.entries if e.severity == severity]


class UnityLogParser:
    """Parser for Unity build logs"""

    # Regex patterns for different error types
    PATTERNS = {
        'compiler_error': re.compile(
            r'(?P<file>[^(]+)\((?P<line>\d+),(?P<column>\d+)\):\s*error\s+(?P<code>\w+):\s+(?P<message>.+)'
        ),
        'compiler_warning': re.compile(
            r'(?P<file>[^(]+)\((?P<line>\d+),(?P<column>\d+)\):\s*warning\s+(?P<code>\w+):\s+(?P<message>.+)'
        ),
        'exception': re.compile(
            r'(?P<type>\w*Exception):\s+(?P<message>.+)'
        ),
        'stack_trace': re.compile(
            r'at\s+(?P<method>.+)\s+in\s+(?P<file>.+):line\s+(?P<line>\d+)'
        ),
        'build_error': re.compile(
            r'^Error:\s+(?P<message>.+)'
        ),
        'unity_error': re.compile(
            r'\[Error\]\s+(?P<message>.+)'
        ),
        'assertion_failed': re.compile(
            r'Assertion failed:\s+(?P<message>.+)'
        ),
        'null_reference': re.compile(
            r'NullReferenceException:\s+(?P<message>.+)'
        ),
    }

    # Error code to severity mapping
    SEVERITY_MAP = {
        'CS0103': Severity.HIGH,      # Name does not exist
        'CS0246': Severity.HIGH,      # Type or namespace not found
        'CS1061': Severity.HIGH,      # Does not contain definition
        'CS0029': Severity.HIGH,      # Cannot convert type
        'MsgPack009': Severity.CRITICAL,  # Duplicate formatters
        'CS0618': Severity.LOW,       # Obsolete API
        'CS1998': Severity.LOW,       # Async without await
        'CS4014': Severity.MEDIUM,    # Unawaited async
        'CS0168': Severity.LOW,       # Variable declared but never used
        'CS0414': Severity.LOW,       # Field assigned but never used
        'CS0067': Severity.LOW,       # Event never used
    }

    # Error code to category mapping
    CATEGORY_MAP = {
        'CS0618': 'Obsolete API',
        'CS1998': 'Async/Await Pattern',
        'CS4014': 'Async/Await Pattern',
        'CS8632': 'Nullable Reference',
        'CS0168': 'Unused Code',
        'CS0414': 'Unused Code',
        'CS0067': 'Unused Code',
        'CS0108': 'Member Hiding',
        'MsgPack009': 'Code Generation',
    }

    def __init__(self):
        self.entries: Dict[str, LogEntry] = {}
        self.total_lines = 0

    def parse_file(self, log_path: Path) -> ParseResult:
        """Parse a Unity log file"""
        start_time = datetime.now()

        if not log_path.exists():
            raise FileNotFoundError(f"Log file not found: {log_path}")

        with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                self.total_lines += 1
                self._parse_line(line.strip())

        parse_time = (datetime.now() - start_time).total_seconds()

        return ParseResult(
            entries=list(self.entries.values()),
            total_lines=self.total_lines,
            parse_time=parse_time
        )

    def _parse_line(self, line: str) -> None:
        """Parse a single log line"""
        # Try compiler error
        if match := self.PATTERNS['compiler_error'].search(line):
            self._add_entry(
                ErrorType.COMPILER_ERROR.value,
                match.group('code'),
                match.group('message').strip(),
                match.group('file'),
                int(match.group('line')),
                int(match.group('column')),
                line
            )
            return

        # Try compiler warning
        if match := self.PATTERNS['compiler_warning'].search(line):
            self._add_entry(
                ErrorType.COMPILER_WARNING.value,
                match.group('code'),
                match.group('message').strip(),
                match.group('file'),
                int(match.group('line')),
                int(match.group('column')),
                line
            )
            return

        # Try exception patterns
        if match := self.PATTERNS['exception'].search(line):
            self._add_entry(
                ErrorType.EXCEPTION.value,
                match.group('type'),
                match.group('message').strip(),
                full_text=line
            )
            return

        # Try build error
        if match := self.PATTERNS['build_error'].search(line):
            self._add_entry(
                ErrorType.BUILD_ERROR.value,
                'BUILD',
                match.group('message').strip(),
                full_text=line
            )
            return

        # Try Unity error
        if match := self.PATTERNS['unity_error'].search(line):
            self._add_entry(
                ErrorType.UNITY_ERROR.value,
                'UNITY',
                match.group('message').strip(),
                full_text=line
            )
            return

    def _add_entry(
        self,
        entry_type: str,
        code: str,
        message: str,
        file: Optional[str] = None,
        line: Optional[int] = None,
        column: Optional[int] = None,
        full_text: str = ""
    ) -> None:
        """Add or update a log entry"""
        # Determine severity
        severity = self.SEVERITY_MAP.get(code, Severity.MEDIUM).value

        # Determine category
        category = self.CATEGORY_MAP.get(code, 'General')

        entry = LogEntry(
            type=entry_type,
            code=code,
            message=message,
            file=file,
            line=line,
            column=column,
            full_text=full_text,
            severity=severity,
            category=category
        )

        key = entry.get_key()

        if key in self.entries:
            self.entries[key].count += 1
        else:
            self.entries[key] = entry


class LogFormatter:
    """Formats parse results for different outputs"""

    @staticmethod
    def format_summary(result: ParseResult, errors_only: bool = False) -> str:
        """Format as colored console summary"""
        lines = []

        # Header
        lines.append("\n" + "=" * 67)
        lines.append("  Unity Log Analysis")
        lines.append("=" * 67)

        # Summary
        summary = result.get_summary()
        errors = result.get_by_type('Error')
        warnings = result.get_by_type('Warning')
        exceptions = [e for e in result.entries if e.type in ['Exception', 'BuildError', 'UnityError']]

        lines.append("\nSummary:")
        lines.append(f"  Unique Errors:      {len(errors)}")
        lines.append(f"  Unique Warnings:    {len(warnings)}")
        lines.append(f"  Unique Exceptions:  {len(exceptions)}")
        lines.append(f"  Total Lines Parsed: {result.total_lines}")
        lines.append(f"  Parse Time:         {result.parse_time:.3f}s")

        # Errors section
        if errors:
            lines.append("\n" + "-" * 67)
            lines.append(f"  ERRORS ({len(errors)} unique)")
            lines.append("-" * 67)

            for error in sorted(errors, key=lambda e: (e.code, e.file or '', e.line or 0)):
                count_suffix = f" (×{error.count})" if error.count > 1 else ""
                lines.append(f"\n  [{error.code}]{count_suffix} [{error.severity.upper()}]")
                if error.file:
                    lines.append(f"  📄 {error.get_location()}")
                lines.append(f"  💬 {error.message}")
                if error.category:
                    lines.append(f"  🏷️  {error.category}")

        # Warnings section
        if not errors_only and warnings:
            lines.append("\n" + "-" * 67)
            lines.append(f"  WARNINGS ({len(warnings)} unique)")
            lines.append("-" * 67)

            # Group by code
            by_code = defaultdict(list)
            for warning in warnings:
                by_code[warning.code].append(warning)

            for code in sorted(by_code.keys()):
                group = by_code[code]
                total_occurrences = sum(w.count for w in group)
                category = group[0].category

                lines.append(f"\n  [{code}] - {len(group)} unique, {total_occurrences} total")
                if category:
                    lines.append(f"  Category: {category}")

                # Show first 3 examples
                for warning in sorted(group, key=lambda w: w.count, reverse=True)[:3]:
                    count_suffix = f" (×{warning.count})" if warning.count > 1 else ""
                    if warning.file:
                        lines.append(f"    • {warning.get_location()}{count_suffix}")
                    lines.append(f"      {warning.message}")

                if len(group) > 3:
                    lines.append(f"    ... and {len(group) - 3} more")

        # Exceptions section
        if exceptions:
            lines.append("\n" + "-" * 67)
            lines.append(f"  EXCEPTIONS ({len(exceptions)} unique)")
            lines.append("-" * 67)

            for exc in exceptions:
                count_suffix = f" (×{exc.count})" if exc.count > 1 else ""
                lines.append(f"\n  [{exc.code}]{count_suffix}")
                lines.append(f"  Msg: {exc.message}")

        lines.append("\n" + "=" * 67 + "\n")

        return "\n".join(lines)

    @staticmethod
    def format_json(result: ParseResult, errors_only: bool = False) -> str:
        """Format as JSON"""
        entries = result.entries
        if errors_only:
            entries = result.get_by_type('Error')

        output = {
            'timestamp': result.timestamp,
            'summary': {
                'errors': len(result.get_by_type('Error')),
                'warnings': len(result.get_by_type('Warning')),
                'exceptions': len([e for e in result.entries if e.type in ['Exception', 'BuildError', 'UnityError']]),
                'total_lines': result.total_lines,
                'parse_time': result.parse_time
            },
            'entries': [entry.to_dict() for entry in entries]
        }

        return json.dumps(output, indent=2)

    @staticmethod
    def format_markdown(result: ParseResult, errors_only: bool = False) -> str:
        """Format as Markdown"""
        lines = []

        errors = result.get_by_type('Error')
        warnings = result.get_by_type('Warning')
        exceptions = [e for e in result.entries if e.type in ['Exception', 'BuildError', 'UnityError']]

        # Header
        lines.append("# Unity Log Analysis")
        lines.append("")
        lines.append(f"**Generated:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        lines.append(f"**Parse Time:** {result.parse_time:.3f}s")
        lines.append("")

        # Summary table
        lines.append("## Summary")
        lines.append("")
        lines.append("| Category | Count |")
        lines.append("|----------|-------|")
        lines.append(f"| Errors | {len(errors)} |")
        lines.append(f"| Warnings | {len(warnings)} |")
        lines.append(f"| Exceptions | {len(exceptions)} |")
        lines.append(f"| Total Lines | {result.total_lines} |")
        lines.append("")

        # Errors
        if errors:
            lines.append("## Errors")
            lines.append("")

            # Group by severity
            by_severity = defaultdict(list)
            for error in errors:
                by_severity[error.severity].append(error)

            for severity in ['critical', 'high', 'medium', 'low']:
                if severity not in by_severity:
                    continue

                lines.append(f"### {severity.upper()} Severity")
                lines.append("")

                for error in sorted(by_severity[severity], key=lambda e: (e.code, e.file or '', e.line or 0)):
                    count_suffix = f" (×{error.count})" if error.count > 1 else ""
                    lines.append(f"#### [{error.code}]{count_suffix}")
                    lines.append("")
                    if error.file:
                        lines.append(f"**Location:** `{error.get_location()}`  ")
                    if error.category:
                        lines.append(f"**Category:** {error.category}  ")
                    lines.append(f"**Message:** {error.message}")
                    lines.append("")

        # Warnings
        if not errors_only and warnings:
            lines.append("## Warnings")
            lines.append("")

            # Group by category
            by_category = defaultdict(list)
            for warning in warnings:
                by_category[warning.category or 'General'].append(warning)

            for category in sorted(by_category.keys()):
                group = by_category[category]
                total = sum(w.count for w in group)

                lines.append(f"### {category} ({len(group)} unique, {total} total)")
                lines.append("")

                # Group by code within category
                by_code = defaultdict(list)
                for w in group:
                    by_code[w.code].append(w)

                for code in sorted(by_code.keys()):
                    code_group = by_code[code]
                    code_total = sum(w.count for w in code_group)

                    lines.append(f"#### [{code}] - {len(code_group)} unique, {code_total} total")
                    lines.append("")

                    for warning in sorted(code_group, key=lambda w: w.count, reverse=True)[:5]:
                        count_suffix = f" (×{warning.count})" if warning.count > 1 else ""
                        if warning.file:
                            lines.append(f"- `{warning.get_location()}`{count_suffix}")
                            lines.append(f"  - {warning.message}")
                        else:
                            lines.append(f"- {warning.message}{count_suffix}")

                    if len(code_group) > 5:
                        lines.append(f"- ... and {len(code_group) - 5} more")
                    lines.append("")

        # Exceptions
        if exceptions:
            lines.append("## Exceptions")
            lines.append("")

            for exc in exceptions:
                count_suffix = f" (×{exc.count})" if exc.count > 1 else ""
                lines.append(f"### [{exc.code}]{count_suffix}")
                lines.append("")
                lines.append(f"**Message:** {exc.message}")
                lines.append("")

        return "\n".join(lines)

    @staticmethod
    def format_csv(result: ParseResult, errors_only: bool = False) -> str:
        """Format as CSV"""
        import csv
        from io import StringIO

        entries = result.entries
        if errors_only:
            entries = result.get_by_type('Error')

        output = StringIO()
        writer = csv.DictWriter(
            output,
            fieldnames=['type', 'code', 'severity', 'category', 'file', 'line', 'column', 'message', 'count']
        )
        writer.writeheader()

        for entry in entries:
            writer.writerow({
                'type': entry.type,
                'code': entry.code,
                'severity': entry.severity,
                'category': entry.category,
                'file': entry.file or '',
                'line': entry.line or '',
                'column': entry.column or '',
                'message': entry.message,
                'count': entry.count
            })

        return output.getvalue()


def main():
    """Main entry point"""
    parser = argparse.ArgumentParser(
        description='Parse Unity build logs and extract errors/warnings',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    %(prog)s output/unity-build.log
    %(prog)s output/unity-build.log --format json > errors.json
    %(prog)s output/unity-build.log --errors-only --format markdown
    %(prog)s output/unity-build.log --format csv > errors.csv
        """
    )

    parser.add_argument('log_file', type=Path, help='Path to Unity log file')
    parser.add_argument(
        '--format', '-f',
        choices=['summary', 'json', 'markdown', 'csv'],
        default='summary',
        help='Output format (default: summary)'
    )
    parser.add_argument(
        '--errors-only', '-e',
        action='store_true',
        help='Show only errors, exclude warnings'
    )
    parser.add_argument(
        '--output', '-o',
        type=Path,
        help='Output file (default: stdout)'
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Verbose output'
    )

    args = parser.parse_args()

    # Parse log file
    try:
        log_parser = UnityLogParser()
        result = log_parser.parse_file(args.log_file)

        if args.verbose:
            print(f"Parsed {result.total_lines} lines in {result.parse_time:.3f}s", file=sys.stderr)
            print(f"Found {len(result.entries)} unique issues", file=sys.stderr)

        # Format output
        formatter = LogFormatter()

        if args.format == 'summary':
            output = formatter.format_summary(result, args.errors_only)
        elif args.format == 'json':
            output = formatter.format_json(result, args.errors_only)
        elif args.format == 'markdown':
            output = formatter.format_markdown(result, args.errors_only)
        elif args.format == 'csv':
            output = formatter.format_csv(result, args.errors_only)

        # Write output
        if args.output:
            args.output.write_text(output, encoding='utf-8')
            if args.verbose:
                print(f"Output written to {args.output}", file=sys.stderr)
        else:
            print(output)

        # Exit with error code if errors found
        errors = result.get_by_type('Error')
        if errors:
            sys.exit(1)

    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(2)
    except Exception as e:
        print(f"Unexpected error: {e}", file=sys.stderr)
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(2)


if __name__ == '__main__':
    main()
