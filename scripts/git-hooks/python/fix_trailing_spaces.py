#!/usr/bin/env python3
"""
Fix trailing spaces in markdown files.

This script removes trailing spaces from specified files while preserving
line endings.
"""

import sys
from pathlib import Path
from typing import List


# Files to process
TARGET_FILES = [
    ".specify/COORDINATION-STATUS.md",
    "scripts/docs_validate.py",
]


def fix_trailing_spaces(file_path: Path) -> bool:
    """
    Remove trailing spaces from a file.

    Args:
        file_path: Path to the file to fix

    Returns:
        True if file was modified, False otherwise
    """
    if not file_path.exists():
        return False

    try:
        # Read the file content
        content = file_path.read_text(encoding="utf-8")

        # Remove trailing spaces from each line while preserving line endings
        lines = content.splitlines(keepends=True)
        fixed_lines = []

        for line in lines:
            # Strip trailing spaces but keep the line ending
            if line.endswith("\r\n"):
                fixed_lines.append(line.rstrip(" ") + "\r\n" if line.strip() else "\r\n")
            elif line.endswith("\n"):
                fixed_lines.append(line.rstrip(" ") + "\n" if line.strip() else "\n")
            else:
                # Last line without newline
                fixed_lines.append(line.rstrip(" "))

        fixed_content = "".join(fixed_lines)

        # Only write if content changed
        if fixed_content != content:
            file_path.write_text(fixed_content, encoding="utf-8")
            print(f"Fixed trailing spaces in: {file_path}")
            return True
        return False

    except Exception as e:
        print(f"Error processing {file_path}: {e}")
        return False


def main() -> int:
    """Main entry point."""
    modified = False

    for file_path_str in TARGET_FILES:
        file_path = Path(file_path_str)
        if fix_trailing_spaces(file_path):
            modified = True

    if modified:
        print("Done!")
    else:
        print("No files needed fixing.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
