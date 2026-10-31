#!/usr/bin/env bash
# Fast C# compile check that works even while the Unity editor is open.
# Compiles Assets/Scripts against Unity's own reference assemblies + the project's
# compiled package assemblies. Catches syntax/API errors in ~2 seconds.
set -uo pipefail

PROJ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
E="${UNITY_DATA:-$HOME/Unity/Hub/Editor/6000.6.7f1/Editor/Data}"
MONO="$E/MonoBleedingEdge/bin/mono"
CSC="$E/MonoBleedingEdge/lib/mono/msbuild/Current/bin/Roslyn/csc.exe"
OUT="${OUT:-/tmp/opencode}"
RSP="$OUT/refs.rsp"
mkdir -p "$OUT"

if [ ! -x "$MONO" ] || [ ! -f "$CSC" ]; then
  echo "FAIL: Unity reference compiler not found under $E"
  exit 2
fi

: > "$RSP"
for d in "$E"/NetStandard/ref/2.1.0/*.dll; do echo "-r:$d" >> "$RSP"; done
for d in "$E"/Managed/UnityEngine/UnityEngine*.dll; do echo "-r:$d" >> "$RSP"; done
for d in "$PROJ"/Library/ScriptAssemblies/*.dll; do
  case "$(basename "$d")" in
    Assembly-CSharp*.dll) continue ;;   # never reference our own old build
  esac
  echo "-r:$d" >> "$RSP"
done

shopt -s nullglob
files=("$PROJ"/Assets/Scripts/*.cs)
if [ ${#files[@]} -eq 0 ]; then echo "no scripts found"; exit 2; fi

"$MONO" "$CSC" -nologo -noconfig -nostdlib+ -target:library -langversion:9.0 \
  -out:"$OUT/check.dll" "@$RSP" "${files[@]}" 2>&1 | tee "$OUT/cscheck.log"
code=${PIPESTATUS[0]}

errs=$(grep -cE "error CS[0-9]+" "$OUT/cscheck.log")
if [ "$errs" -gt 0 ]; then
  echo "FAIL: $errs compile error(s)."
  exit 1
fi

echo "OK: scripts compile."
exit "$code"
