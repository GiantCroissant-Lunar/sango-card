#!/usr/bin/env python3
"""
Pre-commit hook to enforce partial class interface separation pattern (R-CODE-090).

Validates that classes implementing multiple interfaces follow the pattern:
- Base file (ClassName.cs) contains only parent class inheritance
- Each interface in separate file (ClassName.IInterfaceName.cs)

This hook is part of the enforcement strategy for R-CODE-090.

Rule: R-CODE-090 - Partial Class Interface Separation
Documentation: docs/CODING-PATTERNS.md
Enforcement: Pre-commit hook + Roslyn analyzer (future)
"""

import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional

# Set UTF-8 encoding for Windows console
if sys.platform == "win32":
    import codecs
    sys.stdout = codecs.getwriter("utf-8")(sys.stdout.buffer, "replace")
    sys.stderr = codecs.getwriter("utf-8")(sys.stderr.buffer, "replace")


# ANSI color codes for better output
class Colors:
    RED = "\033[31m"
    GREEN = "\033[32m"
    YELLOW = "\033[33m"
    BLUE = "\033[34m"
    RESET = "\033[0m"


def print_colored(message: str, color: str = Colors.RESET) -> None:
    """Print colored output to terminal."""
    try:
        print(f"{color}{message}{Colors.RESET}")
    except UnicodeEncodeError:
        # Fallback to ASCII if Unicode fails
        print(f"{color}{message.encode('ascii', 'replace').decode('ascii')}{Colors.RESET}")


def get_staged_cs_files() -> List[str]:
    """Get list of staged C# files."""
    try:
        result = subprocess.run(
            ["git", "diff", "--cached", "--name-only", "--diff-filter=ACM"],
            capture_output=True,
            text=True,
            check=True,
        )
        files = [
            line.strip()
            for line in result.stdout.splitlines()
            if line.strip().endswith(".cs")
        ]
        return files
    except subprocess.CalledProcessError as e:
        print_colored(f"Error getting staged files: {e}", Colors.RED)
        return []


def check_partial_class_pattern(file_path: str) -> Optional[Dict]:
    """
    Check if a C# file follows the partial class interface separation pattern.

    Returns a violation dict if pattern is violated, None otherwise.
    """
    path = Path(file_path)
    if not path.exists():
        return None

    try:
        content = path.read_text(encoding="utf-8")
    except Exception as e:
        print_colored(f"Warning: Could not read {file_path}: {e}", Colors.YELLOW)
        return None

    # Regex to find class declarations with multiple interfaces
    # Matches: class ClassName : BaseClass, IInterface1, IInterface2
    # or: class ClassName : IInterface1, IInterface2
    pattern = r"(?m)^\s*(partial\s+)?class\s+(\w+)\s*:\s*([^{]+?)(?={)"

    match = re.search(pattern, content)
    if not match:
        return None

    class_declaration = match.group(3).strip()
    class_name = match.group(2)

    # Split by comma to count inheritance items
    inheritance_items = [item.strip() for item in class_declaration.split(",") if item.strip()]

    if len(inheritance_items) <= 1:
        return None

    # Check if this is a base file (ClassName.cs) or interface file (ClassName.IInterfaceName.cs)
    file_name = path.stem  # Filename without extension
    base_name = file_name.split(".")[0]

    # If file is ClassName.cs and has multiple items, it's likely a violation
    if file_name == class_name:
        # This is the base file - should only have one inheritance
        interfaces = inheritance_items[1:]  # Skip the first item (base class)

        if interfaces:
            expected_files = []
            for interface in interfaces:
                # Remove 'I' prefix from interface name for file naming
                interface_name = re.sub(r"^I", "", interface)
                expected_files.append(f"{class_name}.{interface_name}.cs")

            return {
                "file": file_path,
                "class": class_name,
                "message": f"Class '{class_name}' implements multiple interfaces in base file. Each interface should be in a separate partial class file.",
                "interfaces": interfaces,
                "expected_files": expected_files,
            }

    return None


def check_staged_files() -> bool:
    """
    Check all staged C# files for partial class pattern violations.

    Returns True if all files pass, False if violations found.
    """
    print_colored(
        "🔍 Checking partial class interface separation pattern (R-CODE-090)...",
        Colors.BLUE,
    )

    staged_files = get_staged_cs_files()

    if not staged_files:
        print_colored("✅ No C# files staged for commit.", Colors.GREEN)
        return True

    violations = []
    for file_path in staged_files:
        violation = check_partial_class_pattern(file_path)
        if violation:
            violations.append(violation)

    if violations:
        print_colored(
            "\n❌ Partial class pattern violations detected (R-CODE-090):\n",
            Colors.RED,
        )

        for v in violations:
            print_colored(f"  File: {v['file']}", Colors.YELLOW)
            print_colored(f"  Class: {v['class']}", Colors.YELLOW)
            print_colored(f"  Issue: {v['message']}", Colors.RED)
            print_colored(f"  Interfaces: {', '.join(v['interfaces'])}", Colors.YELLOW)
            print()
            print_colored("  Expected structure:", Colors.BLUE)
            print_colored(
                f"    - {v['class']}.cs → partial class {v['class']} : BaseClass",
                Colors.BLUE,
            )
            for expected_file in v["expected_files"]:
                print_colored(
                    f"    - {expected_file} → partial class {v['class']} : [Interface]",
                    Colors.BLUE,
                )
            print()

        print_colored(
            "See docs/CODING-PATTERNS.md for details on R-CODE-090 pattern.",
            Colors.YELLOW,
        )
        print_colored(
            "To bypass this check (not recommended): git commit --no-verify\n",
            Colors.YELLOW,
        )

        return False

    print_colored("✅ All staged C# files follow partial class pattern.", Colors.GREEN)
    return True


def main() -> int:
    """Main entry point."""
    try:
        result = check_staged_files()
        return 0 if result else 1
    except Exception as e:
        print_colored(f"❌ Pre-commit hook error: {e}", Colors.RED)
        import traceback

        print_colored(f"Stack trace:\n{traceback.format_exc()}", Colors.RED)
        return 1


if __name__ == "__main__":
    sys.exit(main())
