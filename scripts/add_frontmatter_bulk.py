#!/usr/bin/env python3
"""
Bulk front-matter migration script for remaining documentation.
"""

import pathlib
import yaml
import re
from datetime import date

ROOT = pathlib.Path(__file__).resolve().parents[1]

# Mapping of files to their metadata
MIGRATIONS = [
    # Task documentation - keep as guides in docs/guides/
    {
        "file": "docs/task/README.md",
        "target": "docs/guides/task-runner.md",
        "doc_id": "DOC-2025-00070",
        "title": "Task Runner Guide",
        "tags": ["task", "build", "automation"],
        "summary": "Complete guide to using Task for building and development."
    },
    {
        "file": "docs/task/GETTING_STARTED.md",
        "target": "docs/guides/task-getting-started.md",
        "doc_id": "DOC-2025-00071",
        "title": "Task Runner Getting Started",
        "tags": ["task", "quickstart", "setup"],
        "summary": "Quick start guide for Task runner setup and basic usage."
    },
    {
        "file": "docs/task/QUICK_REFERENCE.md",
        "target": "docs/guides/task-quick-reference.md",
        "doc_id": "DOC-2025-00072",
        "title": "Task Runner Quick Reference",
        "tags": ["task", "reference", "cheatsheet"],
        "summary": "Quick reference card for Task runner commands."
    },
    {
        "file": "docs/task/INTEGRATION.md",
        "target": "docs/guides/task-integration.md",
        "doc_id": "DOC-2025-00073",
        "title": "Task Runner Integration",
        "tags": ["task", "integration", "ci-cd"],
        "summary": "Technical overview of Task runner integration with build system."
    },
    {
        "file": "docs/task/ALTERNATIVES.md",
        "target": "docs/guides/task-alternatives.md",
        "doc_id": "DOC-2025-00074",
        "title": "Task Runner Alternatives Comparison",
        "tags": ["task", "comparison", "alternatives"],
        "summary": "Comparison of Task runner with alternative build tools."
    },
    {
        "file": "docs/task/INDEX.md",
        "target": "docs/guides/task-index.md",
        "doc_id": "DOC-2025-00075",
        "title": "Task Documentation Index",
        "tags": ["task", "index"],
        "summary": "Index of all Task runner documentation."
    },
    {
        "file": "docs/task/TODO.md",
        "action": "archive",
        "target": "docs/archive/task-TODO.md"
    },

    # Remaining guides that need front-matter
    {
        "file": "docs/guides/coding-patterns.md",
        "doc_id": "DOC-2025-00066",
        "title": "Coding Patterns Guide",
        "tags": ["code", "patterns", "best-practices"],
        "summary": "Coding patterns and best practices for the project."
    },
    {
        "file": "docs/guides/multi-agent-quickstart.md",
        "doc_id": "DOC-2025-00062",
        "title": "Multi-Agent Quick Start",
        "tags": ["agents", "quickstart", "multi-agent"],
        "summary": "Quick start guide for setting up multi-agent development environment."
    },
    {
        "file": "docs/guides/multi-agent-workflow.md",
        "doc_id": "DOC-2025-00061",
        "title": "Multi-Agent Workflow Summary",
        "tags": ["agents", "workflow", "multi-agent"],
        "summary": "Summary of multi-agent workflow patterns and best practices."
    },
    {
        "file": "docs/guides/pre-commit.md",
        "doc_id": "DOC-2025-00067",
        "title": "Pre-Commit Hooks Guide",
        "tags": ["pre-commit", "git", "hooks"],
        "summary": "Guide for setting up and using pre-commit hooks."
    },
    {
        "file": "docs/guides/spec-kit.md",
        "doc_id": "DOC-2025-00064",
        "title": "Spec Kit Guide",
        "tags": ["spec-kit", "workflow"],
        "summary": "Complete guide to using Spec Kit for feature development."
    },
    {
        "file": "docs/guides/spec-kit-quickstart.md",
        "doc_id": "DOC-2025-00065",
        "title": "Spec Kit Quick Start",
        "tags": ["spec-kit", "quickstart"],
        "summary": "Quick start guide for Spec Kit workflow."
    },
    {
        "file": "docs/guides/spec-kit-to-github-issues.md",
        "doc_id": "DOC-2025-00063",
        "title": "Spec Kit to GitHub Issues",
        "tags": ["spec-kit", "github", "issues"],
        "summary": "Guide for converting Spec Kit artifacts to GitHub issues."
    },
]

def has_frontmatter(content: str) -> bool:
    """Check if content already has front-matter."""
    return content.strip().startswith("---")

def add_frontmatter(file_path: pathlib.Path, meta: dict) -> bool:
    """Add front-matter to a file. Returns True if modified."""
    if not file_path.exists():
        print(f"  [SKIP] File not found: {file_path}")
        return False

    content = file_path.read_text(encoding="utf-8")

    if has_frontmatter(content):
        print(f"  [SKIP] Already has front-matter: {file_path}")
        return False

    # Build front-matter
    frontmatter_dict = {
        "doc_id": meta["doc_id"],
        "title": meta["title"],
        "doc_type": "guide",
        "status": "active",
        "canonical": True,
        "created": str(date.today()),
        "tags": meta["tags"],
        "summary": meta["summary"]
    }

    frontmatter_yaml = yaml.dump(frontmatter_dict, default_flow_style=False, sort_keys=False)
    new_content = f"---\n{frontmatter_yaml}---\n\n{content}"

    file_path.write_text(new_content, encoding="utf-8")
    print(f"  [OK] Added front-matter: {file_path}")
    return True

def move_and_migrate(source: pathlib.Path, target: pathlib.Path, meta: dict) -> bool:
    """Move file and add front-matter. Returns True if successful."""
    if not source.exists():
        print(f"  [SKIP] Source not found: {source}")
        return False

    if target.exists():
        print(f"  [SKIP] Target already exists: {target}")
        return False

    # Read source
    content = source.read_text(encoding="utf-8")

    # Check if already has front-matter
    if has_frontmatter(content):
        # Just move it
        target.parent.mkdir(parents=True, exist_ok=True)
        source.rename(target)
        print(f"  [MOVE] {source} -> {target}")
        return True

    # Add front-matter
    frontmatter_dict = {
        "doc_id": meta["doc_id"],
        "title": meta["title"],
        "doc_type": "guide",
        "status": "active",
        "canonical": True,
        "created": str(date.today()),
        "tags": meta["tags"],
        "summary": meta["summary"]
    }

    frontmatter_yaml = yaml.dump(frontmatter_dict, default_flow_style=False, sort_keys=False)
    new_content = f"---\n{frontmatter_yaml}---\n\n{content}"

    # Write to target
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(new_content, encoding="utf-8")

    # Remove source
    source.unlink()

    print(f"  [MIGRATE] {source} -> {target}")
    return True

def archive_file(source: pathlib.Path, target: pathlib.Path) -> bool:
    """Move file to archive."""
    if not source.exists():
        print(f"  [SKIP] Source not found: {source}")
        return False

    target.parent.mkdir(parents=True, exist_ok=True)
    source.rename(target)
    print(f"  [ARCHIVE] {source} -> {target}")
    return True

def main():
    print("Bulk front-matter migration starting...\n")

    stats = {"added": 0, "moved": 0, "archived": 0, "skipped": 0}

    for migration in MIGRATIONS:
        source_path = ROOT / migration["file"]

        # Handle archive action
        if migration.get("action") == "archive":
            target_path = ROOT / migration["target"]
            if archive_file(source_path, target_path):
                stats["archived"] += 1
            else:
                stats["skipped"] += 1
            continue

        # Handle move with migration
        if "target" in migration:
            target_path = ROOT / migration["target"]
            if move_and_migrate(source_path, target_path, migration):
                stats["moved"] += 1
            else:
                stats["skipped"] += 1
        # Handle in-place front-matter addition
        else:
            if add_frontmatter(source_path, migration):
                stats["added"] += 1
            else:
                stats["skipped"] += 1

    print(f"\nMigration complete!")
    print(f"  Added front-matter: {stats['added']}")
    print(f"  Moved and migrated: {stats['moved']}")
    print(f"  Archived: {stats['archived']}")
    print(f"  Skipped: {stats['skipped']}")
    print(f"  Total processed: {sum(stats.values())}")

if __name__ == "__main__":
    main()
