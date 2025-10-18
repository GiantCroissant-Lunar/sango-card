#!/usr/bin/env python3
"""
Helper script to add front-matter to migrated documentation files.
"""

import pathlib
import yaml
from datetime import date

MIGRATIONS = [
    {
        "file": "docs/guides/agent-orchestration.md",
        "doc_id": "DOC-2025-00060",
        "title": "Agent Orchestration Guide",
        "tags": ["agents", "orchestration", "multi-agent"],
        "summary": "Guide for orchestrating multiple AI agents in development workflow."
    },
    {
        "file": "docs/guides/multi-agent-workflow.md",
        "doc_id": "DOC-2025-00061",
        "title": "Multi-Agent Workflow Summary",
        "tags": ["agents", "workflow", "multi-agent"],
        "summary": "Summary of multi-agent workflow patterns and best practices."
    },
    {
        "file": "docs/guides/multi-agent-quickstart.md",
        "doc_id": "DOC-2025-00062",
        "title": "Multi-Agent Quick Start",
        "tags": ["agents", "quickstart", "multi-agent"],
        "summary": "Quick start guide for setting up multi-agent development environment."
    },
    {
        "file": "docs/guides/spec-kit-to-github-issues.md",
        "doc_id": "DOC-2025-00063",
        "title": "Spec Kit to GitHub Issues",
        "tags": ["spec-kit", "github", "issues"],
        "summary": "Guide for converting Spec Kit artifacts to GitHub issues."
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
        "file": "docs/guides/coding-patterns.md",
        "doc_id": "DOC-2025-00066",
        "title": "Coding Patterns Guide",
        "tags": ["code", "patterns", "best-practices"],
        "summary": "Coding patterns and best practices for the project."
    },
    {
        "file": "docs/guides/pre-commit.md",
        "doc_id": "DOC-2025-00067",
        "title": "Pre-Commit Hooks Guide",
        "tags": ["pre-commit", "git", "hooks"],
        "summary": "Guide for setting up and using pre-commit hooks."
    },
]

def add_frontmatter(file_path: str, meta: dict):
    """Add front-matter to a file."""
    path = pathlib.Path(file_path)

    if not path.exists():
        print(f"⚠️  File not found: {file_path}")
        return

    content = path.read_text(encoding="utf-8")

    # Skip if already has front-matter
    if content.startswith("---"):
        print(f"⏭️  Skipping (has front-matter): {file_path}")
        return

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

    # Write back
    path.write_text(new_content, encoding="utf-8")
    print(f"✅ Added front-matter: {file_path}")

if __name__ == "__main__":
    for migration in MIGRATIONS:
        add_frontmatter(migration["file"], migration)
    print("\n✅ Front-matter migration complete!")
