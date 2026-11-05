#!/usr/bin/env bash
# Remove local build outputs and generated test reports (safe to run anytime).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "Cleaning QualiFrame build artifacts and report outputs..."

find . -type d \( -name bin -o -name obj \) -not -path './.git/*' -prune -exec rm -rf {} + 2>/dev/null || true
rm -rf Results
find . -type d -name TestResults -not -path './.git/*' -prune -exec rm -rf {} + 2>/dev/null || true
rm -rf src/QualiFrame.Tests/src

dotnet clean Qualiframe-CSharp-NUnit_v3.0.sln -v q 2>/dev/null || true

echo "Done. Repository source-only footprint restored."
