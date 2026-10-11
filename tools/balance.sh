#!/usr/bin/env bash
# Runs the abstract balance simulation and prints a report.
cd "$(dirname "$0")/.."
timeout -s INT 120 godot --headless --import >/dev/null 2>&1
timeout -s INT 600 godot --headless --audio-driver Dummy --path . res://tools/balance/balance_sim.tscn 2>&1 | grep -vE "^Godot|^$"
