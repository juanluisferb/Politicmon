#!/usr/bin/env bash
# Regenerates every generated asset (graphics, font, data tables, audio) from tools/.
set -e
cd "$(dirname "$0")"
python3 data_politicians.py
python3 gen_font.py
python3 gen_ui.py
python3 art/gen_politician_sprites.py
python3 art/gen_trainer_sprites.py
python3 art/gen_overworld_sprites.py
python3 gen_items.py
python3 gen_tiles.py
if [ -f gen_audio.py ]; then python3 gen_audio.py; fi
if [ -f gen_maps.py ]; then python3 gen_maps.py; fi
echo "assets regenerated"
python3 check_assets.py
