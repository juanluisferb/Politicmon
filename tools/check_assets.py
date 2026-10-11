"""Validates generated art: sizes, 4-color palettes per sprite and per 8x8 tile, file naming."""
import glob
import json
import os
import sys
from PIL import Image
import pixlib

errors = []


def colors(img, box=None):
    im = img.crop(box) if box else img
    return set(im.get_flattened_data()) if hasattr(im, "get_flattened_data") else set(im.getdata())


def opaque_colors(img, box=None):
    return {c for c in colors(img, box) if c[3] > 0}


pol = json.load(open(os.path.join(pixlib.ROOT, "data", "politicians.json"), encoding="utf-8"))
for k, v in pol.items():
    if k.startswith("_"):
        continue
    base = os.path.join(pixlib.ROOT, "assets", "sprites", "politicians", "%02d_%s" % (int(k), v["key"]))
    for kind, size in (("front", 56), ("back", 32), ("icon", 16)):
        path = "%s_%s.png" % (base, kind)
        if not os.path.exists(path):
            errors.append("missing " + path)
            continue
        im = Image.open(path).convert("RGBA")
        if im.size != (size, size):
            errors.append("%s size %s" % (path, im.size))
        if len(opaque_colors(im)) > 3:
            errors.append("%s uses %d opaque colors" % (path, len(opaque_colors(im))))
        if any(0 < c[3] < 255 for c in colors(im)):
            errors.append("%s has semi-transparent pixels" % path)

for path in glob.glob(os.path.join(pixlib.ROOT, "assets", "sprites", "*", "*.png")):
    im = Image.open(path).convert("RGBA")
    if "overworld" in path:
        for y in range(0, im.height, 16):
            for x in range(0, im.width, 16):
                if len(opaque_colors(im, (x, y, x + 16, y + 16))) > 3:
                    errors.append("%s frame %d,%d > 3 colors" % (path, x, y))
    elif len(opaque_colors(im)) > 3:
        errors.append("%s uses %d opaque colors" % (path, len(opaque_colors(im))))

tiles = Image.open(os.path.join(pixlib.ROOT, "assets", "tiles", "metatiles.png")).convert("RGBA")
for y in range(0, tiles.height, 8):
    for x in range(0, tiles.width, 8):
        if len(colors(tiles, (x, y, x + 8, y + 8))) > 4:
            errors.append("metatile 8x8 at %d,%d > 4 colors" % (x, y))

if errors:
    print("\n".join(errors))
    sys.exit(1)
print("assets ok")
