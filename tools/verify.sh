#!/usr/bin/env bash
# Compile check without opening the editor.
set -uo pipefail

UNITY="${UNITY:-$HOME/Unity/Hub/Editor/6000.6.7f1/Editor/Unity}"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${OUT:-/tmp/opencode}"
LOG="$OUT/compile.log"
mkdir -p "$OUT"

if [ -e "$PROJ/Temp/UnityLockfile" ]; then
  echo "FAIL: the Unity editor has this project open. Close it and retry."
  exit 2
fi

"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" -logFile "$LOG"
code=$?

if grep -q "No valid Unity Editor license" "$LOG"; then
  echo "FAIL: no Unity license. Open the editor once and sign in."
  exit 3
fi

errs=$(grep -cE "error CS[0-9]+" "$LOG")
warns=$(grep -cE "warning CS[0-9]+" "$LOG")

echo "unity_exit=$code  compile_errors=$errs  warnings=$warns  log=$LOG"

if [ "$errs" -gt 0 ]; then
  grep -E "error CS[0-9]+" "$LOG" | sed 's/^ *//' | sort -u | head -40
  exit 1
fi

echo "OK: no compile errors."
exit "$code"
