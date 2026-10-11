#!/usr/bin/env bash
# Parses every GDScript file and prints compile errors with locations.
cd "$(dirname "$0")/.."
timeout -s INT 120 godot --headless --import >/dev/null 2>&1
status=0
for f in $(find src tests -name "*.gd"); do
  out=$(timeout -s INT 60 godot --headless --check-only --script "res://$f" 2>&1 | grep -A1 -E "SCRIPT ERROR|Parse Error" | grep -vE "Failed to compile depended|^--$" | grep -vE "Identifier not found: (Ui|GameData|GameState|ScreenScaler|Audio|Router|SaveSystem|InputSetup|TouchControls)" | grep -B0 -A1 -E "SCRIPT ERROR" | grep -v "^--$")
  if [ -n "$out" ]; then echo "== $f"; echo "$out"; status=1; fi
done
exit $status
