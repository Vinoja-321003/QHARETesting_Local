#!/usr/bin/env python3
"""Validate FRAMEWORK_MANIFEST.json against FRAMEWORK_MANIFEST.schema.json.

Checks JSON syntax, JSON Schema, unique IDs, internal references,
repository-relative path existence, documentation paths, and obvious secrets.
Compatible with the repository Python toolchain (no extra runtime required
beyond the optional jsonschema package).
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
MANIFEST_PATH = REPO_ROOT / "FRAMEWORK_MANIFEST.json"
SCHEMA_PATH = REPO_ROOT / "FRAMEWORK_MANIFEST.schema.json"

SECRET_KEY_HINTS = re.compile(
    r"(password|passwd|secret|accesskey|apikey|token|connectionstring|privatekey)",
    re.IGNORECASE,
)
PLACEHOLDER_VALUES = {
    "",
    "changeme",
    "your-key",
    "yourencryptionkeyhere",
    "yourjwtsecrethere",
}
SECRET_VALUE_HINTS = re.compile(
    r"^(?=.{8,})(?=.*[A-Z])(?=.*[a-z])(?=.*\d).*(@|!|#|\$).*$"
)

ID_FIELDS = ("id",)
REF_FIELDS = (
    "moduleReferences",
    "configurationReferences",
    "reusableComponentReferences",
    "documentationReferences",
    "approachReferences",
    "standardReferences",
    "supportedApproaches",
    "readers",
    "reporters",
    "capabilities",
    "moduleReference",
)
PATH_FIELDS = (
    "implementationReferences",
    "exampleReferences",
    "sourceReferences",
    "examples",
    "featureFiles",
    "stepDefinitions",
    "hooks",
    "runners",
    "keywordDefinitions",
    "locations",
    "factories",
    "sources",
    "licenseReferences",
)
# Declared output folders (often gitignored). Missing paths are warnings, not errors.
OUTPUT_PATH_FIELDS = (
    "artifactDirectories",
)
SINGLE_PATH_FIELDS = (
    "path",
    "pipelineFile",
    "source",
    "sdkPinFile",
)


def load_json(path: Path) -> object:
    try:
        with path.open(encoding="utf-8") as handle:
            return json.load(handle)
    except json.JSONDecodeError as exc:
        raise SystemExit(f"INVALID JSON: {path.relative_to(REPO_ROOT)}: {exc}") from exc


def collect_ids(node: object, found: dict[str, str], errors: list[str], trail: str = "$") -> None:
    if isinstance(node, dict):
        if "id" in node and isinstance(node["id"], str):
            item_id = node["id"]
            if item_id in found and found[item_id] != trail:
                errors.append(f"Duplicate ID '{item_id}' at {trail} (also {found[item_id]})")
            else:
                found[item_id] = trail
        for key, value in node.items():
            collect_ids(value, found, errors, f"{trail}.{key}")
    elif isinstance(node, list):
        for index, value in enumerate(node):
            collect_ids(value, found, errors, f"{trail}[{index}]")


def walk(node: object):
    if isinstance(node, dict):
        yield node
        for value in node.values():
            yield from walk(value)
    elif isinstance(node, list):
        for value in node:
            yield from walk(value)


def path_exists(rel: str) -> bool:
    candidate = REPO_ROOT / rel
    return candidate.exists()


DESCRIPTIVE_FIELDS = {
    "secretHandling",
    "notes",
    "purpose",
    "summary",
    "rule",
    "guidance",
    "description",
    "usageGuidance",
    "whenToUpdateManifest",
    "schemaGovernance",
    "coreBehavior",
    "reuseBeforeCreate",
    "command",
    "signature",
    "returns",
    "name",
    "title",
    "when",
    "qualityGates",
    "environmentHandling",
    "html",
    "xml",
    "screenshots",
    "video",
    "traces",
    "threadSafety",
    "preferredApi",
    "logging",
    "telemetry",
    "dashboards",
    "failureDiagnostics",
    "analytics",
    "parameterization",
    "masking",
    "cleanup",
    "precedence",
    "headedHeadless",
    "retries",
    "parallelization",
    "containers",
    "remoteCloud",
    "tags",
    "executionFlow",
    "style",
    "layout",
    "license",
    "displayName",
    "role",
    "type",
    "reason",
    "replacement",
    "extensionModel",
}


def looks_like_secret_value(key: str, value: str) -> bool:
    if not isinstance(value, str):
        return False
    if key in DESCRIPTIVE_FIELDS:
        return False
    lowered = value.strip().lower()
    if not value or lowered in PLACEHOLDER_VALUES:
        return False
    parent_sensitive = SECRET_KEY_HINTS.search(key)
    if not parent_sensitive:
        return False
    if lowered.startswith("http") or lowered.endswith(".json") or "/" in value:
        return False
    return bool(SECRET_VALUE_HINTS.search(value)) or len(value) >= 12


def validate_references(manifest: dict, ids: dict[str, str], errors: list[str], warnings: list[str]) -> None:
    doc_ids = {
        item["id"]
        for item in manifest.get("documentation", {}).get("items", [])
        if isinstance(item, dict) and "id" in item
    }
    module_ids = {
        item["id"]
        for item in manifest.get("modules", {}).get("items", [])
        if isinstance(item, dict) and "id" in item
    }
    component_ids = {
        item["id"]
        for item in manifest.get("reusableComponents", {}).get("items", [])
        if isinstance(item, dict) and "id" in item
    }
    config_ids = {
        item["id"]
        for item in manifest.get("configuration", {}).get("settings", [])
        if isinstance(item, dict) and "id" in item
    }

    for node in walk(manifest):
        for field in REF_FIELDS:
            if field not in node:
                continue
            values = node[field] if isinstance(node[field], list) else [node[field]]
            for ref in values:
                if not isinstance(ref, str):
                    continue
                if ref not in ids:
                    errors.append(f"Unresolved reference '{ref}' in {field} of {node.get('id', '<anonymous>')}")
        if node.get("moduleReference") and node["moduleReference"] not in module_ids:
            errors.append(f"Invalid moduleReference '{node['moduleReference']}' on {node.get('id')}")
        for field in PATH_FIELDS:
            for rel in node.get(field, []) if isinstance(node.get(field), list) else []:
                if isinstance(rel, dict) and isinstance(rel.get("path"), str):
                    rel = rel["path"]
                if not isinstance(rel, str):
                    continue
                if not looks_like_repo_path(rel):
                    continue
                if not path_exists(rel):
                    errors.append(f"Missing path '{rel}' referenced from {node.get('id', '<anonymous>')}.{field}")
        for field in OUTPUT_PATH_FIELDS:
            for rel in node.get(field, []) if isinstance(node.get(field), list) else []:
                if not isinstance(rel, str) or not looks_like_repo_path(rel):
                    continue
                if not path_exists(rel):
                    warnings.append(
                        f"Output path '{rel}' from {node.get('id', '<anonymous>')}.{field} is not present (often gitignored until a run)."
                    )
        for field in SINGLE_PATH_FIELDS:
            rel = node.get(field)
            if isinstance(rel, str) and field in node:
                if field == "path" and not looks_like_repo_path(rel):
                    continue
                if looks_like_repo_path(rel) and not path_exists(rel):
                    errors.append(f"Missing path '{rel}' referenced from {node.get('id', '<anonymous>')}.{field}")
        for key, value in node.items():
            if isinstance(value, str) and looks_like_secret_value(key, value):
                errors.append(f"Possible secret value in field '{key}' on {node.get('id', '<anonymous>')}")

    for item in manifest.get("traceability", {}).get("items", []):
        cap = item.get("capability")
        if cap and cap not in ids:
            errors.append(f"Traceability capability '{cap}' does not resolve")

    unused_docs = doc_ids - collect_used_ids(manifest, "documentationReferences")
    if unused_docs:
        warnings.append(f"Documentation IDs not referenced elsewhere: {sorted(unused_docs)}")

    orphan_components = component_ids - collect_used_ids(manifest, "reusableComponentReferences")
    if len(orphan_components) > max(3, len(component_ids) // 2):
        warnings.append("Many reusable components are not referenced from capabilities/traceability.")


def looks_like_repo_path(value: str) -> bool:
    return any(sep in value for sep in ("/", ".cs", ".md", ".json", ".yml", ".yaml", ".sh", ".csproj", ".sln", ".slnf", ".feature", ".props")) and not value.startswith("capability.")


def collect_used_ids(manifest: dict, field: str) -> set[str]:
    used: set[str] = set()
    for node in walk(manifest):
        value = node.get(field)
        if isinstance(value, list):
            used.update(x for x in value if isinstance(x, str))
        elif isinstance(value, str):
            used.add(value)
    return used


def validate_schema(manifest: object, schema: dict, errors: list[str]) -> None:
    try:
        import jsonschema
        from jsonschema import Draft202012Validator
    except ImportError:
        errors.append(
            "jsonschema is not installed. Create a venv and install it, for example: "
            "python3 -m venv /tmp/qf-manifest-venv && "
            "/tmp/qf-manifest-venv/bin/pip install jsonschema && "
            "/tmp/qf-manifest-venv/bin/python scripts/validate-framework-manifest.py"
        )
        return

    validator = Draft202012Validator(schema)
    for error in sorted(validator.iter_errors(manifest), key=lambda e: list(e.path)):
        location = ".".join(str(part) for part in error.path) or "$"
        errors.append(f"Schema: {location}: {error.message}")


def main() -> int:
    errors: list[str] = []
    warnings: list[str] = []

    if not MANIFEST_PATH.exists():
        print("MISSING: FRAMEWORK_MANIFEST.json")
        return 1
    if not SCHEMA_PATH.exists():
        print("MISSING: FRAMEWORK_MANIFEST.schema.json")
        return 1

    manifest = load_json(MANIFEST_PATH)
    schema = load_json(SCHEMA_PATH)
    print("JSON: valid")

    if not isinstance(manifest, dict) or not isinstance(schema, dict):
        print("Schema or manifest is not an object")
        return 1

    if manifest.get("$schema") != "./FRAMEWORK_MANIFEST.schema.json":
        errors.append('manifest.$schema must be "./FRAMEWORK_MANIFEST.schema.json"')

    validate_schema(manifest, schema, errors)

    ids: dict[str, str] = {}
    collect_ids(manifest, ids, errors)
    validate_references(manifest, ids, errors, warnings)

    print(f"IDs: {len(ids)} unique" if not any("Duplicate ID" in e for e in errors) else "IDs: duplicates found")

    if errors:
        print("VALIDATION FAILED")
        for item in errors:
            print(f"  ERROR: {item}")
        for item in warnings:
            print(f"  WARN: {item}")
        return 1

    print("Schema: valid")
    print("Paths: valid")
    print("References: valid")
    print("Secrets scan: no obvious secret values")
    for item in warnings:
        print(f"  WARN: {item}")
    print("OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
