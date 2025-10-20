#!/usr/bin/env python3
"""
Find C# files containing multiple type declarations.

This script scans C# source files to identify those containing multiple
top-level type declarations (classes, interfaces, structs, enums, delegates, records).
It can optionally attempt to automatically split them into separate files.
"""

import re
import argparse
import json
import sys
from pathlib import Path
from typing import List, Dict, Tuple
from dataclasses import dataclass, asdict


@dataclass
class TypeDeclaration:
    """Represents a type declaration in a C# file."""
    type_kind: str  # class, interface, struct, enum, delegate, record
    name: str
    line: int
    start_line: int
    end_line: int
    full_text: str
    modifiers: str  # public, internal, etc.


@dataclass
class FileAnalysis:
    """Analysis result for a C# file."""
    file_path: str
    relative_path: str
    type_count: int
    types: List[TypeDeclaration]


class CSharpTypeScanner:
    """Scanner for finding type declarations in C# files."""

    # Patterns for top-level type declarations
    TYPE_PATTERNS = [
        # With explicit access modifiers
        r'^\s*(public|internal|private|protected)\s+(static\s+)?(sealed\s+)?(abstract\s+)?(partial\s+)?(class|interface|struct|enum|delegate|record)\s+(\w+)',
        # Without access modifiers (implicit internal)
        r'^\s*(static\s+)?(sealed\s+)?(abstract\s+)?(partial\s+)?(class|interface|struct|enum|delegate|record)\s+(\w+)',
    ]

    def __init__(self, root_path: Path):
        self.root_path = root_path.resolve()

    def find_csharp_files(self) -> List[Path]:
        """Find all C# files, excluding build artifacts."""
        exclude_patterns = {'obj', 'bin', 'Library', 'Temp', '.git', 'node_modules'}

        files = []
        for cs_file in self.root_path.rglob('*.cs'):
            # Skip if any parent directory matches exclude patterns
            if any(part in exclude_patterns for part in cs_file.parts):
                continue
            # Skip temp files
            if cs_file.name.startswith('.#'):
                continue
            files.append(cs_file)

        return files

    def count_brace_depth(self, lines: List[str], up_to_line: int) -> int:
        """Calculate brace nesting depth up to a specific line."""
        depth = 0
        for i in range(up_to_line):
            line = lines[i]
            # Simple brace counting (doesn't handle strings/comments perfectly)
            depth += line.count('{')
            depth -= line.count('}')
        return depth

    def is_nested_type(self, lines: List[str], line_index: int) -> bool:
        """Check if a type declaration is nested inside another type."""
        return self.count_brace_depth(lines, line_index) > 0

    def find_type_end(self, lines: List[str], start_line: int) -> int:
        """Find the closing brace of a type declaration."""
        depth = 0
        in_type = False

        for i in range(start_line, len(lines)):
            line = lines[i]

            # Track when we enter the type body
            if '{' in line:
                in_type = True

            if in_type:
                depth += line.count('{')
                depth -= line.count('}')

                if depth == 0:
                    return i

        return len(lines) - 1  # Fallback to end of file

    def extract_type_text(self, lines: List[str], start: int, end: int) -> str:
        """Extract the full text of a type declaration including attributes."""
        # Look backwards for attributes
        attr_start = start
        for i in range(start - 1, -1, -1):
            line = lines[i].strip()
            if line.startswith('[') or line.startswith('//') or line == '':
                attr_start = i
            else:
                break

        return '\n'.join(lines[attr_start:end + 1])

    def parse_modifiers(self, line: str) -> str:
        """Extract access modifiers from a type declaration line."""
        modifiers = []
        tokens = ['public', 'internal', 'protected', 'private', 'static', 'sealed', 'abstract', 'partial']
        for token in tokens:
            if re.search(rf'\b{token}\b', line):
                modifiers.append(token)
        return ' '.join(modifiers) if modifiers else 'internal'

    def analyze_file(self, file_path: Path) -> FileAnalysis:
        """Analyze a C# file for type declarations."""
        try:
            content = file_path.read_text(encoding='utf-8')
            lines = content.splitlines()
        except Exception as e:
            print(f"Error reading {file_path}: {e}", file=sys.stderr)
            return FileAnalysis(
                file_path=str(file_path),
                relative_path=str(file_path.relative_to(self.root_path)),
                type_count=0,
                types=[]
            )

        types = []
        in_multiline_comment = False

        for i, line in enumerate(lines):
            # Skip multiline comments
            if '/*' in line:
                in_multiline_comment = True
            if '*/' in line:
                in_multiline_comment = False
                continue
            if in_multiline_comment or line.strip().startswith('//'):
                continue

            # Try to match type declaration patterns
            for pattern in self.TYPE_PATTERNS:
                match = re.match(pattern, line)
                if match:
                    # Check if it's a top-level type (not nested)
                    if not self.is_nested_type(lines, i):
                        # Extract type kind and name
                        groups = match.groups()
                        type_kind = groups[-2]  # Second to last group is always the type kind
                        type_name = groups[-1]  # Last group is always the name

                        # Find where the type ends
                        end_line = self.find_type_end(lines, i)

                        # Extract full text
                        full_text = self.extract_type_text(lines, i, end_line)

                        # Parse modifiers
                        modifiers = self.parse_modifiers(line)

                        types.append(TypeDeclaration(
                            type_kind=type_kind,
                            name=type_name,
                            line=i + 1,  # 1-indexed for display
                            start_line=i,
                            end_line=end_line,
                            full_text=full_text,
                            modifiers=modifiers
                        ))
                    break

        relative_path = file_path.relative_to(self.root_path)

        return FileAnalysis(
            file_path=str(file_path),
            relative_path=str(relative_path),
            type_count=len(types),
            types=types
        )


class MultiTypeReporter:
    """Generate reports for files with multiple types."""

    @staticmethod
    def console_report(results: List[FileAnalysis]) -> str:
        """Generate console-friendly report."""
        if not results:
            return "[OK] No files with multiple types found!"

        lines = [
            "Files with Multiple Type Declarations:",
            "=" * 80,
            ""
        ]

        for result in results:
            lines.append(f"\n{result.relative_path}")
            lines.append(f"  Type Count: {result.type_count}")
            for type_decl in result.types:
                lines.append(f"    - {type_decl.type_kind} {type_decl.name} (line {type_decl.line})")

        lines.extend([
            "",
            "=" * 80,
            f"Total violations: {len(results)} files",
            "",
            "To fix: Open files in Rider and use Alt+Enter on each type -> 'Move to separate file'"
        ])

        return '\n'.join(lines)

    @staticmethod
    def markdown_report(results: List[FileAnalysis]) -> str:
        """Generate markdown report."""
        if not results:
            return "# Files with Multiple Type Declarations\n\n✓ No violations found!"

        lines = [
            "# Files with Multiple Type Declarations",
            "",
            f"**Total files with violations:** {len(results)}",
            "",
            "## Summary",
            "",
            "| File | Type Count | Types |",
            "|------|------------|-------|",
        ]

        for result in results:
            type_list = ", ".join([f"{t.type_kind} {t.name}" for t in result.types])
            lines.append(f"| `{result.relative_path}` | {result.type_count} | {type_list} |")

        lines.extend([
            "",
            "## Details",
            ""
        ])

        for result in results:
            lines.append(f"### `{result.relative_path}`\n")
            for type_decl in result.types:
                lines.append(f"- **{type_decl.type_kind}** `{type_decl.name}` (line {type_decl.line}, modifiers: `{type_decl.modifiers}`)")
            lines.append("")

        lines.extend([
            "## How to Fix",
            "",
            "Open each file in JetBrains Rider:",
            "1. Place cursor on the type name",
            "2. Press **Alt+Enter** (or **Option+Enter** on macOS)",
            "3. Select **'Move to separate file'**",
            "4. Repeat for each type until only one remains",
        ])

        return '\n'.join(lines)

    @staticmethod
    def json_report(results: List[FileAnalysis]) -> str:
        """Generate JSON report."""
        data = {
            "total_violations": len(results),
            "files": [
                {
                    "file": r.file_path,
                    "relative_path": r.relative_path,
                    "type_count": r.type_count,
                    "types": [
                        {
                            "kind": t.type_kind,
                            "name": t.name,
                            "line": t.line,
                            "modifiers": t.modifiers
                        }
                        for t in r.types
                    ]
                }
                for r in results
            ]
        }
        return json.dumps(data, indent=2)


def main():
    parser = argparse.ArgumentParser(
        description="Find C# files with multiple type declarations"
    )
    parser.add_argument(
        'path',
        type=Path,
        nargs='?',
        default=Path('.'),
        help='Root path to scan (default: current directory)'
    )
    parser.add_argument(
        '--format',
        choices=['console', 'json', 'markdown'],
        default='console',
        help='Output format (default: console)'
    )
    parser.add_argument(
        '--output',
        type=Path,
        help='Save output to file'
    )
    parser.add_argument(
        '--errors-only',
        action='store_true',
        help='Exit with error code if violations found'
    )

    args = parser.parse_args()

    # Scan files
    scanner = CSharpTypeScanner(args.path)
    print(f"Scanning for C# files in: {args.path}", file=sys.stderr)

    files = scanner.find_csharp_files()
    print(f"Found {len(files)} C# files to analyze\n", file=sys.stderr)

    # Analyze each file
    results = []
    for file_path in files:
        analysis = scanner.analyze_file(file_path)
        if analysis.type_count > 1:
            results.append(analysis)

    # Generate report
    reporter = MultiTypeReporter()

    if args.format == 'json':
        output = reporter.json_report(results)
    elif args.format == 'markdown':
        output = reporter.markdown_report(results)
    else:
        output = reporter.console_report(results)

    # Write output
    if args.output:
        args.output.write_text(output, encoding='utf-8')
        print(f"[OK] Report saved to: {args.output}", file=sys.stderr)
    else:
        # Handle Windows console encoding issues
        try:
            print(output)
        except UnicodeEncodeError:
            # Fallback to ASCII-safe output
            print(output.encode('ascii', 'replace').decode('ascii'))

    # Exit with appropriate code
    if args.errors_only and results:
        sys.exit(1)
    else:
        sys.exit(0)


if __name__ == '__main__':
    main()
