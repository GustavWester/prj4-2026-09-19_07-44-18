#!/usr/bin/env bash
# Kører én Unity test-platform (EditMode eller PlayMode) i batchmode og fejler hvis
# Unity ikke laver en resultatfil, ingen tests blev kørt, eller en test fejlede.
# Smoke (booter spillet) og ShouldFail (pipeline-check) springes over.
set -uo pipefail

platform="$1"
mkdir -p results
editor_path="$(cygpath -u "$UNITY_EDITOR_PATH")"
results_file="results/${platform}-results.xml"

"$editor_path" \
  -batchmode \
  -nographics \
  -projectPath "$(cygpath -w "$PWD")" \
  -runTests \
  -testPlatform "$platform" \
  -testCategory "!Smoke;!ShouldFail" \
  -testResults "$(cygpath -w "$PWD/$results_file")" \
  -logFile -
code=$?

echo "Unity exited with code $code"
if [ ! -f "$results_file" ]; then
  echo "::error::Unity did not produce $results_file"
  exit 1
fi
if ! grep -Eq 'total="[1-9][0-9]*"' "$results_file"; then
  echo "::error::No $platform tests were executed"
  exit 1
fi
exit "$code"
