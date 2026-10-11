#!/usr/bin/env bash
# Usage: tools/screenshot.sh <scenario> <WxH> <out.png> [extra user args...]
set -e
SCEN="$1"; RES="$2"; OUT="$3"; shift 3
W=${RES%x*}; H=${RES#*x}
cd "$(dirname "$0")/.."
timeout 120 xvfb-run -a -s "-screen 0 $((W+64))x$((H+64))x24" \
  godot --rendering-driver opengl3 --audio-driver Dummy --resolution "$RES" --path . -- \
  --scenario="$SCEN" --shot="$(realpath -m "$OUT")" "$@" 2>&1 | grep -vE "^ALSA|audio_driver|All audio" || true
