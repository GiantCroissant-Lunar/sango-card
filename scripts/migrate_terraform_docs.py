#!/usr/bin/env python3
"""
Migrate terraform documentation to canonical docs structure.
"""

import pathlib
import yaml
import shutil
from datetime import date

ROOT = pathlib.Path(__file__).resolve().parents[1]

MIGRATIONS = [
    {
        "source": "infra/terraform/QUICKSTART.md",
        "target": "docs/guides/terraform-quickstart.md",
        "doc_id": "DOC-2025-00080",
        "title": "Terraform SOPS Quick Start",
        "tags": ["terraform", "sops", "quickstart", "infrastructure"],
        "summary": "5-minute setup guide for SOPS encrypted secrets with Terraform."
    },
    {
        "source": "infra/terraform/SECURITY.md",
        "target": "docs/guides/terraform-security.md",
        "doc_id": "DOC-2025-00081",
        "title": "Terraform Security Guide",
        "tags": ["terraform", "security", "sops", "encryption"],
        "summary": "Security best practices for managing encrypted Terraform secrets."
    },
    {
        "source": "infra/terraform/TERRAFORM_CLOUD_SETUP.md",
        "target": "docs/guides/terraform-cloud-setup.md",
        "doc_id": "DOC-2025-00082",
        "title": "Terraform Cloud Setup Guide",
        "tags": ["terraform", "terraform-cloud", "setup"],
        "summary": "Complete guide for setting up Terraform Cloud backend."
    },
    {
        "source": "infra/terraform/IMPLEMENTATION_SUMMARY.md",
        "target": "docs/archive/terraform-IMPLEMENTATION_SUMMARY.md",
        "action": "archive"
    },
    {
        "source": "infra/terraform/.sops-setup-complete.md",
        "target": "docs/archive/terraform-sops-setup-complete.md",
        "action": "archive"
    },
    {
        "source": "infra/QUICKSTART.md",
        "target": "docs/archive/infra-QUICKSTART.md",
        "action": "archive"
    },
    {
        "source": "infra/COMPLETION-SUMMARY.md",
        "target": "docs/archive/infra-COMPLETION-SUMMARY.md",
        "action": "archive"
    }
]

def add_frontmatter(content: str, meta: dict) -> str:
    """Add front-matter to content."""
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
    return f"---\n{frontmatter_yaml}---\n\n{content}"

def migrate_file(source_path: pathlib.Path, target_path: pathlib.Path, meta: dict = None):
    """Migrate a file with optional front-matter."""
    if not source_path.exists():
        print(f"  [SKIP] Source not found: {source_path}")
        return False

    content = source_path.read_text(encoding="utf-8")

    # Add front-matter if metadata provided
    if meta:
        content = add_frontmatter(content, meta)

    # Create target directory
    target_path.parent.mkdir(parents=True, exist_ok=True)

    # Write target file
    target_path.write_text(content, encoding="utf-8")

    # Remove source
    source_path.unlink()

    print(f"  [MIGRATE] {source_path} -> {target_path}")
    return True

def main():
    print("Migrating terraform documentation...\n")

    stats = {"migrated": 0, "archived": 0, "skipped": 0}

    for migration in MIGRATIONS:
        source = ROOT / migration["source"]
        target = ROOT / migration["target"]

        if migration.get("action") == "archive":
            # Archive without front-matter
            if migrate_file(source, target):
                stats["archived"] += 1
            else:
                stats["skipped"] += 1
        else:
            # Migrate with front-matter
            if migrate_file(source, target, migration):
                stats["migrated"] += 1
            else:
                stats["skipped"] += 1

    print(f"\nMigration complete!")
    print(f"  Migrated with front-matter: {stats['migrated']}")
    print(f"  Archived: {stats['archived']}")
    print(f"  Skipped: {stats['skipped']}")
    print(f"  Total: {sum(stats.values())}")

if __name__ == "__main__":
    main()
