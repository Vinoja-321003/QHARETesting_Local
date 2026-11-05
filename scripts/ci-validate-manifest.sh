#!/usr/bin/env bash
# Shared CI entry for FRAMEWORK_MANIFEST.json governance.
# Does not restore, build, or run tests.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

PYTHON=""
for candidate in python3 python; do
  if command -v "$candidate" >/dev/null 2>&1; then
    PYTHON="$candidate"
    break
  fi
done

if [[ -z "$PYTHON" ]]; then
  echo "python3 (or python) is required to validate FRAMEWORK_MANIFEST.json" >&2
  exit 1
fi

VENV="${TMPDIR:-/tmp}/qf-manifest-venv"
if "$PYTHON" -m venv "$VENV"; then
  if [[ -x "$VENV/bin/pip" ]]; then
    PIP="$VENV/bin/pip"
    VENV_PYTHON="$VENV/bin/python"
  else
    PIP="$VENV/Scripts/pip"
    VENV_PYTHON="$VENV/Scripts/python"
  fi
  "$PIP" install --quiet --upgrade pip
  "$PIP" install --quiet jsonschema
  "$VENV_PYTHON" scripts/validate-framework-manifest.py
else
  echo "venv unavailable; installing jsonschema with the system interpreter" >&2
  "$PYTHON" -m pip install --quiet jsonschema
  "$PYTHON" scripts/validate-framework-manifest.py
fi
