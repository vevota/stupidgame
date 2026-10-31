#!/usr/bin/env bash
# Build a Linux player. Close the editor first.
set -uo pipefail

UNITY="${UNITY:-$HOME/Unity/Hub/Editor/6000.6.7f1/Editor/Unity}"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${OUT:-/tmp/opencode}"
LOG="$OUT/build.log"
mkdir -p "$OUT" "$PROJ/build"

if [ -e "$PROJ/Temp/UnityLockfile" ]; then
  echo "FAIL: the Unity editor has this project open. Close it and retry."
  exit 2
fi

"$UNITY" -batchmode -nographics -quit -projectPath "$PROJ" \
  -buildLinux64Player "$PROJ/build/stupidgame.x86_64" \
  -logFile "$LOG"
code=$?

grep -E "error CS[0-9]+|Build Finished|Build succeeded|Build failed" "$LOG" | tail -10
echo "build_exit=$code  player=$PROJ/build/stupidgame.x86_64"
exit "$code"
