"""Shared helpers for pixel-art generation: char matrices -> paletted PNG."""
import json
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def load_palettes():
    with open(os.path.join(ROOT, "data", "palettes.json"), encoding="utf-8") as f:
        return json.load(f)


def parse_rows(text):
    rows = [r for r in text.strip("\n").split("\n")]
    rows = [r.rstrip() for r in rows]
    width = max(len(r) for r in rows)
    return [r.ljust(width, ".") for r in rows]


def grid_from_rows(rows, mapping=None):
    """Convert char rows to a 2D list of palette indices (0..3). '.' or ' ' = 0."""
    mapping = mapping or {}
    grid = []
    for r in rows:
        line = []
        for ch in r:
            if ch in mapping:
                line.append(mapping[ch])
            elif ch in ".  ":
                line.append(0)
            elif ch in "0123":
                line.append(int(ch))
            else:
                raise ValueError("bad pixel char %r" % ch)
        grid.append(line)
    return grid


def new_grid(w, h, v=0):
    return [[v] * w for _ in range(h)]


def grid_to_image(grid, colors, transparent0=True):
    h = len(grid)
    w = len(grid[0])
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    px = img.load()
    for y in range(h):
        for x in range(w):
            v = grid[y][x]
            if v == 0 and transparent0:
                continue
            r, g, b = hex_rgb(colors[v])
            px[x, y] = (r, g, b, 255)
    return img


def mirror(grid):
    return [list(reversed(r)) for r in grid]


def stamp(dst, src, ox, oy, transparent=True):
    for y, row in enumerate(src):
        for x, v in enumerate(row):
            if transparent and v == 0:
                continue
            yy, xx = oy + y, ox + x
            if 0 <= yy < len(dst) and 0 <= xx < len(dst[0]):
                dst[yy][xx] = v


def save(img, *parts):
    path = os.path.join(ROOT, *parts)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    return path


def upscale(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def contact_sheet(images, cols, cell, bg=(255, 255, 255, 255), scale=2, labels=None):
    from PIL import ImageDraw
    rows = (len(images) + cols - 1) // cols
    lab_h = 10 if labels else 0
    sheet = Image.new("RGBA", (cols * (cell[0] + 4) + 4, rows * (cell[1] + 4 + lab_h) + 4), bg)
    d = ImageDraw.Draw(sheet)
    for i, im in enumerate(images):
        cx = 4 + (i % cols) * (cell[0] + 4)
        cy = 4 + (i // cols) * (cell[1] + 4 + lab_h)
        sheet.alpha_composite(im, (cx + (cell[0] - im.width) // 2, cy + (cell[1] - im.height) // 2))
        if labels:
            d.text((cx, cy + cell[1]), labels[i][:14], fill=(0, 0, 0, 255))
    return upscale(sheet, scale)
