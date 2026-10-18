#!/usr/bin/env bash
# Tjekker at den installerede Unity-editor faktisk kan starte (en halvt installeret
# editor fra en ødelagt cache fejler fx med "Unity.dll failed to load", exit 126).
#
#   --delete-cache-if-broken  slet Unity-installations-cachen, så næste install henter forfra
#   --fail-if-broken          lad steppet fejle (ellers skrives kun healthy=false som output)
set -uo pipefail

delete_cache=false
fail=false
for arg in "$@"; do
  case "$arg" in
    --delete-cache-if-broken) delete_cache=true ;;
    --fail-if-broken) fail=true ;;
  esac
done

healthy=true
if [ "${SETUP_OUTCOME:-success}" != "success" ]; then
  echo "::warning::Unity-installationen fejlede (${SETUP_OUTCOME})"
  healthy=false
elif [ -z "${UNITY_EDITOR_PATH:-}" ]; then
  echo "::warning::UNITY_EDITOR_PATH er ikke sat"
  healthy=false
else
  editor_path="$(cygpath -u "$UNITY_EDITOR_PATH")"
  version="$("$editor_path" -batchmode -nographics -version 2>&1)"
  code=$?
  echo "$version"
  if [ "$code" -ne 0 ] || ! grep -q '6000\.' <<<"$version"; then
    echo "::warning::Unity-editoren kunne ikke starte (exit $code)"
    healthy=false
  fi
fi

echo "healthy=$healthy" >> "${GITHUB_OUTPUT:-/dev/null}"
[ "$healthy" = true ] && { echo "Unity-editoren virker."; exit 0; }

if [ "$delete_cache" = true ]; then
  unity_version="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
  echo "Sletter installations-cache for Unity $unity_version ..."
  gh cache list --key "unity-setup-" --limit 100 --json key --jq '.[].key' \
    | grep -- "-${unity_version}-" \
    | while read -r key; do
        echo "  gh cache delete $key"
        gh cache delete "$key" || echo "::warning::Kunne ikke slette cachen $key"
      done
fi

if [ "$fail" = true ]; then
  if [ "$delete_cache" = true ]; then
    echo "::error::Unity-editoren virker ikke. Cachen er slettet - kør workflowet igen."
  else
    echo "::error::Unity-editoren virker ikke (installations-cachen er sandsynligvis ødelagt)."
  fi
  exit 1
fi
exit 0
