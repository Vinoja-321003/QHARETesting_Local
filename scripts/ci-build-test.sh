#!/usr/bin/env bash
# Shared CI entry point for GitHub Actions, Azure DevOps, and local parity runs.
# Builds and tests each supported TFM (net8.0, net9.0, net10.0) unless QUALIFRAME_TFM is set.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

BUILD_CONFIGURATION="${BUILD_CONFIGURATION:-Release}"
TEST_PROJECT="${TEST_PROJECT:-src/QualiFrame.Tests/QualiFrame.Tests.csproj}"
TEST_RESULTS_TRX="${TEST_RESULTS_TRX:-test-results.trx}"
TFMS="${QUALIFRAME_TFM:-net8.0 net9.0 net10.0}"

echo "QualiFrame CI: configuration=${BUILD_CONFIGURATION} project=${TEST_PROJECT}"
echo "QualiFrame CI: frameworks=${TFMS}"

dotnet restore "$TEST_PROJECT"

for tfm in $TFMS; do
  echo "==== Building ${tfm} ===="
  dotnet build "$TEST_PROJECT" --no-restore -c "$BUILD_CONFIGURATION" -f "$tfm"

  echo "==== Testing ${tfm} ===="
  trx_name="${TEST_RESULTS_TRX%.trx}-${tfm}.trx"
  # Keep a stable primary artifact name for the first/default TFM as well
  if [[ "$tfm" == "net9.0" ]] || [[ "$TFMS" == "$tfm" ]]; then
    trx_name="${TEST_RESULTS_TRX}"
  fi

  dotnet test "$TEST_PROJECT" \
    --no-build \
    -c "$BUILD_CONFIGURATION" \
    -f "$tfm" \
    --verbosity normal \
    --logger "trx;LogFileName=${trx_name}" \
    --collect:"XPlat Code Coverage"
done
