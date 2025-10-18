#!/usr/bin/env python3

"""

Documentation Validation and Registry Generator


Validates all markdown files in docs/ for:

- Required front-matter fields

- Canonical uniqueness

- Near-duplicate detection

- Status consistency


Generates docs/index/registry.json for agent consumption.

"""


import pathlib

import sys

import re

import json

import hashlib

from typing import Dict, List, Tuple, Optional

from datetime import datetime, timezone


try:

    import yaml

except ImportError:

    print("ERROR: PyYAML not installed. Run: pip install pyyaml")

    sys.exit(1)


try:

    from simhash import Simhash

except ImportError:

    print("WARNING: simhash not installed. Duplicate detection disabled.")

    print("To enable: pip install simhash")

    Simhash = None


try:

    from rapidfuzz import fuzz

except ImportError:

    print("WARNING: rapidfuzz not installed. Fuzzy matching disabled.")

    print("To enable: pip install rapidfuzz")

    fuzz = None



ROOT = pathlib.Path(__file__).resolve().parents[1]

DOCS = ROOT / "docs"

INBOX = DOCS / "_inbox"

ARCHIVE = DOCS / "archive"

REGISTRY = DOCS / "index" / "registry.json"


# Paths to exclude from validation

EXCLUDE_PATTERNS = [

    "/archive/",

    "/index/",

    "/_inbox/",

    "/node_modules/",

    "/.git/",

]


# Required front-matter fields

REQUIRED_FIELDS = ["doc_id", "title", "doc_type", "status", "canonical", "created", "tags", "summary"]


# Valid values

VALID_DOC_TYPES = ["spec", "rfc", "adr", "plan", "finding", "guide", "glossary", "reference"]

VALID_STATUSES = ["draft", "active", "superseded", "rejected", "archived"]



class ValidationError:

    def __init__(self, path: pathlib.Path, message: str, severity: str = "error"):

        self.path = path

        self.message = message

        self.severity = severity  # error, warning, info


    def __str__(self):

        rel_path = self.path.relative_to(ROOT)

        return f"[{self.severity.upper()}] {rel_path}: {self.message}"



def extract_frontmatter(text: str) -> Tuple[Optional[Dict], str]:

    """Extract YAML front-matter and body from markdown."""

    match = re.match(r"^---\r?\n(.*?)\r?\n---\r?\n", text, re.S)

    if not match:

        return None, text


    try:

        meta = yaml.safe_load(match.group(1))

        body = text[match.end():]

        return meta or {}, body

    except yaml.YAMLError as e:

        return {"_parse_error": str(e)}, text



def normalize_text(text: str) -> str:

    """Normalize text for similarity comparison."""

    # Remove code blocks

    text = re.sub(r"```.*?```", "", text, flags=re.S)

    # Remove inline code

    text = re.sub(r"`[^`]+`", "", text)

    # Remove URLs

    text = re.sub(r"https?://\S+", "", text)

    # Lowercase and remove non-word chars

    text = re.sub(r"\W+", " ", text.lower())

    return text.strip()



def compute_simhash(text: str) -> Optional[str]:

    """Compute SimHash for duplicate detection."""

    if Simhash is None:

        return None

    normalized = normalize_text(text)

    if not normalized:

        return None

    return str(Simhash(normalized).value)



def compute_sha256(text: str) -> str:

    """Compute SHA256 hash of text."""

    return hashlib.sha256(text.encode("utf-8")).hexdigest()



def should_exclude(path: pathlib.Path) -> bool:

    """Check if path should be excluded from validation."""

    path_str = str(path).replace("\\", "/")

    return any(pattern in path_str for pattern in EXCLUDE_PATTERNS)



def validate_frontmatter(path: pathlib.Path, meta: Dict) -> List[ValidationError]:

    """Validate front-matter fields."""

    errors = []


    # Check for parse errors

    if "_parse_error" in meta:

        errors.append(ValidationError(path, f"YAML parse error: {meta['_parse_error']}"))

        return errors


    # Check required fields

    missing = [field for field in REQUIRED_FIELDS if field not in meta]

    if missing:

        errors.append(ValidationError(path, f"Missing required fields: {', '.join(missing)}"))


    # Validate doc_type

    if "doc_type" in meta and meta["doc_type"] not in VALID_DOC_TYPES:

        errors.append(ValidationError(

            path,

            f"Invalid doc_type '{meta['doc_type']}'. Must be one of: {', '.join(VALID_DOC_TYPES)}"

        ))


    # Validate status

    if "status" in meta and meta["status"] not in VALID_STATUSES:

        errors.append(ValidationError(

            path,

            f"Invalid status '{meta['status']}'. Must be one of: {', '.join(VALID_STATUSES)}"

        ))


    # Validate canonical is boolean

    if "canonical" in meta and not isinstance(meta["canonical"], bool):

        errors.append(ValidationError(path, f"Field 'canonical' must be boolean, got: {type(meta['canonical']).__name__}"))


    # Validate created date format

    if "created" in meta:

        try:

            datetime.fromisoformat(str(meta["created"]))

        except (ValueError, TypeError):

            errors.append(ValidationError(path, f"Field 'created' must be ISO date (YYYY-MM-DD), got: {meta['created']}"))


    # Validate tags is list

    if "tags" in meta and not isinstance(meta["tags"], list):

        errors.append(ValidationError(path, f"Field 'tags' must be a list, got: {type(meta['tags']).__name__}"))


    # Check doc_id format

    if "doc_id" in meta:

        doc_id = str(meta["doc_id"])

        if not re.match(r"^[A-Z]+-\d{4}-\d{5}$", doc_id):

            errors.append(ValidationError(

                path,

                f"Invalid doc_id format '{doc_id}'. Expected: PREFIX-YYYY-NNNNN (e.g., DOC-2025-00042)",

                severity="warning"

            ))


    return errors



def validate_canonical_uniqueness(entries: List[Dict]) -> List[ValidationError]:

    """Ensure only one canonical doc per concept (normalized title)."""

    errors = []

    by_concept = {}


    for entry in entries:

        # Normalize title to concept key

        title = entry.get("title", "")

        concept = re.sub(r"\W+", " ", title.lower()).strip()


        if not concept:

            continue


        by_concept.setdefault(concept, []).append(entry)


    for concept, group in by_concept.items():

        canonical = [e for e in group if e.get("canonical")]


        if len(canonical) > 1:

            paths = [e["path"] for e in canonical]

            errors.append(ValidationError(

                ROOT,

                f"Multiple canonical docs for concept '{concept}':\n  " + "\n  ".join(paths)

            ))


    return errors



def detect_near_duplicates(entries: List[Dict]) -> List[ValidationError]:

    """Detect near-duplicate documents between inbox and corpus."""

    if Simhash is None or fuzz is None:

        return []


    errors = []

    inbox_entries = [e for e in entries if "/_inbox/" in e["path"]]

    corpus_entries = [e for e in entries if "/_inbox/" not in e["path"]]


    for inbox_entry in inbox_entries:

        inbox_hash = inbox_entry.get("simhash")

        if not inbox_hash:

            continue


        for corpus_entry in corpus_entries:

            corpus_hash = corpus_entry.get("simhash")

            if not corpus_hash:

                continue


            # Fast SimHash comparison (Hamming distance)

            hamming = bin(int(inbox_hash) ^ int(corpus_hash)).count("1")


            if hamming <= 8:  # Threshold for similarity

                # Double-check with title similarity

                inbox_title = inbox_entry.get("title", "")

                corpus_title = corpus_entry.get("title", "")

                title_score = fuzz.token_set_ratio(inbox_title, corpus_title)


                if title_score >= 80:

                    errors.append(ValidationError(

                        ROOT,

                        f"Near-duplicate detected:\n"

                        f"  Inbox:  {inbox_entry['path']}\n"

                        f"  Corpus: {corpus_entry['path']}\n"

                        f"  Title similarity: {title_score}%, Content similarity: {100 - hamming * 2}%",

                        severity="warning"

                    ))

                    break  # Only report first match per inbox doc


    return errors



def process_documents() -> Tuple[List[Dict], List[ValidationError]]:

    """Process all markdown documents and collect errors."""

    entries = []

    errors = []


    # Find all markdown files in docs/

    for md_path in DOCS.rglob("*.md"):

        if should_exclude(md_path):

            continue


        try:

            content = md_path.read_text(encoding="utf-8")

        except Exception as e:

            errors.append(ValidationError(md_path, f"Failed to read file: {e}"))

            continue


        meta, body = extract_frontmatter(content)


        # Files without front-matter

        if meta is None:

            # Only warn for non-inbox files

            if "/_inbox/" not in str(md_path):

                errors.append(ValidationError(

                    md_path,

                    "Missing YAML front-matter. See docs/DOCUMENTATION-SCHEMA.md",

                    severity="warning"

                ))

            continue


        # Validate front-matter

        errors.extend(validate_frontmatter(md_path, meta))


        # Build registry entry

        entry = {

            "path": str(md_path.relative_to(ROOT)).replace("\\", "/"),

            "doc_id": meta.get("doc_id", ""),

            "title": meta.get("title", ""),

            "doc_type": meta.get("doc_type", ""),

            "status": meta.get("status", ""),

            "canonical": bool(meta.get("canonical", False)),

            "tags": meta.get("tags", []),

            "created": str(meta.get("created", "")),

            "summary": meta.get("summary", ""),

            "supersedes": meta.get("supersedes", []),

            "related": meta.get("related", []),

            "sha256": compute_sha256(content),

            "simhash": compute_simhash(body),

        }

        entries.append(entry)


    return entries, errors



def generate_registry(entries: List[Dict]):

    """Generate machine-readable registry JSON."""

    REGISTRY.parent.mkdir(parents=True, exist_ok=True)


    registry = {

        "generated_at": datetime.now(timezone.utc).isoformat().replace('+00:00', 'Z'),

        "total_docs": len(entries),

        "by_type": {},

        "by_status": {},

        "docs": sorted(entries, key=lambda e: e.get("created", ""), reverse=True),

    }


    # Compute stats

    for entry in entries:

        doc_type = entry.get("doc_type", "unknown")

        status = entry.get("status", "unknown")

        registry["by_type"][doc_type] = registry["by_type"].get(doc_type, 0) + 1

        registry["by_status"][status] = registry["by_status"].get(status, 0) + 1


    with open(REGISTRY, "w", encoding="utf-8", newline='\n') as f:

        json.dump(registry, f, indent=2, ensure_ascii=False)

        f.write('\n')  # Ensure final newline


    print(f"Registry generated: {REGISTRY.relative_to(ROOT)}")

    print(f"   Total docs: {len(entries)}")

    print(f"   By type: {registry['by_type']}")

    print(f"   By status: {registry['by_status']}")



def main():

    import argparse


    parser = argparse.ArgumentParser(description="Validate documentation and generate registry")

    parser.add_argument("--pre-commit", action="store_true",

                        help="Pre-commit mode: validate only, don't regenerate registry")

    args = parser.parse_args()


    print("Validating documentation...")

    print()


    # Process all documents

    entries, errors = process_documents()


    # Additional validations

    errors.extend(validate_canonical_uniqueness(entries))

    errors.extend(detect_near_duplicates(entries))


    # Generate registry (skip in pre-commit mode to avoid infinite loop)

    if not args.pre_commit:

        generate_registry(entries)

        print()

    else:

        print("(Pre-commit mode: skipping registry regeneration)")

        print()


    # Report errors

    if errors:

        print("Validation failed:")

        print()


        # Group by severity

        by_severity = {"error": [], "warning": [], "info": []}

        for error in errors:

            by_severity[error.severity].append(error)


        for severity in ["error", "warning", "info"]:

            if by_severity[severity]:

                print(f"{severity.upper()}S ({len(by_severity[severity])}):")

                for error in by_severity[severity]:

                    print(f"  {error}")

                print()


        # Exit with failure if errors exist

        if by_severity["error"]:

            sys.exit(1)

        else:

            print("Warnings found but validation passed.")

            sys.exit(0)

    else:

        print("All documentation validated successfully!")

        sys.exit(0)



if __name__ == "__main__":

    main()
