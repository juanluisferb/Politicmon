#!/usr/bin/env bash
# Runs the automated test-suite headless. Extra args are forwarded (e.g. --only=types).
cd "$(dirname "$0")/.."
timeout -s INT 180 godot --headless --import >/dev/null 2>&1
timeout -s INT 300 godot --headless --audio-driver Dummy --path . res://tests/test_runner.tscn -- "$@" 2>&1 | grep -vE "^Godot Engine|^$"
exit ${PIPESTATUS[0]}
