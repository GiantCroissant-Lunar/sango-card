#!/usr/bin/env python3
"""
Pre-commit hook to detect scattered documentation files.

Checks for markdown files in non-canonical locations and blocks commit if found.
Part of the documentation management system (R-DOC-xxx).
"""

import re
import subprocess
import sys
from typing import List


# ANSI color codes
class Colors:
    RED = "\033[31m"
    GREEN = "\033[32m"
    YELLOW = "\033[33m"
    CYAN = "\033[36m"
    WHITE = "\033[97m"
    RESET = "\033[0m"


# Define allowed documentation locations
ALLOWED_PATTERNS = [
    r"^README\.md$",
    r"^AGENTS\.md$",
    r"^CLAUDE\.md$",
    r"^CODE_OF_CONDUCT\.md$",
    r"^CONTRIBUTING\.md$",
    r"^LICENSE\.md$",
    r"^CHANGELOG\.md$",
    r"^docs/",
    r"^\.agent/",
    r"^\.github/",
    r"^\.specify/",
    r"^\.windsurf/",
    r"^\.gemini/",
    r".*/README\.md$",
    r".*/LICENSE\.md$",
    r".*/CHANGELOG\.md$",
    r"^build/nuke/build/.*\.md$",  # Nuke build system docs
    r"^build/preparation/.*\.md$",  # Build preparation docs
    r"^projects/client/.*\.md$",  # Unity project docs
    r"^projects/code-quality/.*\.md$",  # Code quality project docs
    r"^packages/.*/dotnet~/.*\.md$",  # .NET tool docs
]


def get_staged_markdown_files() -> List[str]:
    """Get list of staged markdown files."""
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
            if line.strip().endswith(".md")
        ]
        return files
    except subprocess.CalledProcessError as e:
        print(f"{Colors.RED}Error getting staged files: {e}{Colors.RESET}")
        return []


def is_allowed_location(file_path: str) -> bool:
    """Check if a markdown file is in an allowed location."""
    for pattern in ALLOWED_PATTERNS:
        if re.search(pattern, file_path):
            return True
    return False


def check_scattered_docs() -> int:
    """
    Check for scattered documentation files.

    Returns 0 if all files are in allowed locations, 1 if scattered docs found.
    """
    staged_files = get_staged_markdown_files()

    if not staged_files:
        return 0

    scattered_docs = [f for f in staged_files if not is_allowed_location(f)]

    if scattered_docs:
        print()
        print(f"{Colors.RED}ERROR: Scattered documentation detected!{Colors.RESET}")
        print()
        print(
            f"{Colors.YELLOW}The following markdown files are in non-canonical locations:{Colors.RESET}"
        )
        print()

        for doc in scattered_docs:
            print(f"{Colors.RED}  - {doc}{Colors.RESET}")

        print()
        print(
            f"{Colors.CYAN}Documentation must be in canonical locations:{Colors.RESET}"
        )
        print(f"{Colors.GREEN}  - New docs        -> docs/_inbox/{Colors.RESET}")
        print(f"{Colors.GREEN}  - Guides          -> docs/guides/{Colors.RESET}")
        print(f"{Colors.GREEN}  - Specifications  -> docs/specs/{Colors.RESET}")
        print(f"{Colors.GREEN}  - RFCs            -> docs/rfcs/{Colors.RESET}")
        print(f"{Colors.GREEN}  - ADRs            -> docs/adrs/{Colors.RESET}")
        print(f"{Colors.GREEN}  - Plans           -> docs/plans/{Colors.RESET}")
        print(f"{Colors.GREEN}  - Findings        -> docs/findings/{Colors.RESET}")
        print(f"{Colors.GREEN}  - Obsolete docs   -> docs/archive/{Colors.RESET}")
        print()
        print(f"{Colors.CYAN}See: docs/DOCUMENTATION-SCHEMA.md{Colors.RESET}")
        print(
            f"{Colors.CYAN}See: .agent/base/40-documentation.md (R-DOC-001){Colors.RESET}"
        )
        print()
        print(f"{Colors.YELLOW}To fix:{Colors.RESET}")
        print(
            f"{Colors.WHITE}  1. Move files to proper location (usually docs/_inbox/){Colors.RESET}"
        )
        print(
            f"{Colors.WHITE}  2. Add YAML front-matter (see docs/DOCUMENTATION-SCHEMA.md){Colors.RESET}"
        )
        print(
            f"{Colors.WHITE}  3. Or remove from staging: git reset HEAD <file>{Colors.RESET}"
        )
        print()

        return 1

    return 0


def main() -> int:
    """Main entry point."""
    try:
        return check_scattered_docs()
    except Exception as e:
        print(f"{Colors.RED}❌ Pre-commit hook error: {e}{Colors.RESET}")
        import traceback

        print(f"{Colors.RED}Stack trace:\n{traceback.format_exc()}{Colors.RESET}")
        return 1


if __name__ == "__main__":
    sys.exit(main())
