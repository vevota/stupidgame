#!/usr/bin/env bash
# Run the built player on a virtual display and save a frame.
# Usage: ./tools/shot.sh [delaySeconds] [outPath]
set -uo pipefail

DELAY="${1:-4}"
PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${OUT:-/tmp/opencode}"
SHOT="${2:-$OUT/frame.png}"
LOG="$OUT/player.log"
mkdir -p "$OUT"

if [ ! -x "$PROJ/build/stupidgame.x86_64" ]; then
  echo "No player at build/stupidgame.x86_64 — run ./tools/build.sh first."
  exit 2
fi

rm -f "$SHOT"

xvfb-run -a -s "-screen 0 1280x720x24" \
  "$PROJ/build/stupidgame.x86_64" \
  -force-glcore -screen-width 1280 -screen-height 720 -screen-fullscreen 0 \
  -screenshot "$SHOT" -screenshotDelay "$DELAY" -quitAfter 20 \
  > "$LOG" 2>&1

echo "log=$LOG"
if [ -f "$SHOT" ]; then
  echo "saved $SHOT ($(stat -c%s "$SHOT") bytes)"
else
  echo "no screenshot produced; tail of log:"
  tail -25 "$LOG"
  exit 1
fi
