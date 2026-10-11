"""UI art: dialog frame (9-patch), cursor, touch controls, HP bar frame, platform."""
import pixlib
from pixlib import parse_rows, grid_from_rows, grid_to_image, save

PAL = pixlib.load_palettes()
BOX = ["#00000000"] + PAL["ui"]["box"][1:]
BOX_OPAQUE = PAL["ui"]["box"]

FRAME = """
.3333333333333333333333.
331111111111111111111133
313333333333333333333313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313000000000000000000313
313333333333333333333313
331111111111111111111133
.3333333333333333333333.
"""

CURSOR = """
3.....
33....
323...
3223..
323...
33....
3.....
"""

ARROW_DOWN = """
33333
.323.
..3..
"""

DPAD_UP = """
....33333333....
...3111111113...
..311111111113..
..311113311113..
..311133331113..
..311333333113..
..313333333313..
..311111111113..
..311111111113..
..311111111113..
..311111111113..
..311111111113..
..311111111113..
..311111111113..
..322222222223..
...3333333333...
"""

BTN_A = """
.......333333.......
.....3311111133.....
....311111111113....
...31111111111113...
..3111111331111113..
.311111133331111113.
.311111331133111113.
31111113311331111113
31111113333331111113
31111113311331111113
31111113311331111113
31111113311331111113
31111111111111111113
.311111111111111113.
.312111111111111213.
..3122111111112213..
...31222222222213...
....322222222223....
.....3322222233.....
.......333333.......
"""

BTN_B = """
.......333333.......
.....3311111133.....
....311111111113....
...31111111111113...
..3111111111111113..
.311111333331111113.
.311111331133111113.
31111113311331111113
31111113333311111113
31111113311331111113
31111113311331111113
31111113333311111113
31111111111111111113
.311111111111111113.
.312111111111111213.
..3122111111112213..
...31222222222213...
....322222222223....
.....3322222233.....
.......333333.......
"""

BTN_START = """
..33333333333333333333..
.3111111111111111111113.
311111111111111111111113
313331333133313331333113
313111131313131313131113
313331131333133111311113
311131131313131311311113
313331131313131311311113
311111111111111111111113
322222222222222222222223
.3222222222222222222223.
..33333333333333333333..
"""

PLATFORM = """
..........2222222222222222222222222222..........
.....2222222222222222222222222222222222222222...
..22222111111111111111111111111111111111112222..
.2222111111111111111111111111111111111111112222.
22221111111111111111111111111111111111111111222.
.2222111111111111111111111111111111111111112222.
..22222111111111111111111111111111111111112222..
.....2222222222222222222222222222222222222222...
..........2222222222222222222222222222..........
"""


def rot90(grid):
    h, w = len(grid), len(grid[0])
    return [[grid[h - 1 - y][x] for y in range(h)] for x in range(w)]


def main():
    touch = PAL["ui"]["touch"]
    save(grid_to_image(grid_from_rows(parse_rows(FRAME)), BOX_OPAQUE, transparent0=False).convert("RGBA"),
         "assets", "ui", "frame.png")
    fr = grid_from_rows(parse_rows(FRAME))
    img = grid_to_image(fr, BOX_OPAQUE, transparent0=False)
    px = img.load()
    for y, row in enumerate(parse_rows(FRAME)):
        for x, c in enumerate(row):
            if c == ".":
                px[x, y] = (0, 0, 0, 0)
    save(img, "assets", "ui", "frame.png")
    save(grid_to_image(grid_from_rows(parse_rows(CURSOR)), BOX), "assets", "ui", "cursor.png")
    save(grid_to_image(grid_from_rows(parse_rows(ARROW_DOWN)), BOX), "assets", "ui", "arrow_down.png")
    up = grid_from_rows(parse_rows(DPAD_UP))
    right = rot90(up)
    down = rot90(right)
    left = rot90(down)
    for name, g in (("up", up), ("right", right), ("down", down), ("left", left)):
        save(grid_to_image(g, touch), "assets", "ui", "touch_%s.png" % name)
    save(grid_to_image(grid_from_rows(parse_rows(BTN_A)), touch), "assets", "ui", "touch_a.png")
    save(grid_to_image(grid_from_rows(parse_rows(BTN_B)), touch), "assets", "ui", "touch_b.png")
    save(grid_to_image(grid_from_rows(parse_rows(BTN_START)), touch), "assets", "ui", "touch_start.png")
    bg = PAL["ui"]["battle_bg"]
    save(grid_to_image(grid_from_rows(parse_rows(PLATFORM)), ["#00000000"] + bg[1:]), "assets", "ui", "platform.png")
    print("ui ok")


if __name__ == "__main__":
    main()


def make_icon():
    from PIL import Image
    import os
    src = Image.open(os.path.join(pixlib.ROOT, "assets", "sprites", "politicians", "25_eterno_front.png"))
    for size, scale in ((192, 3), (432, 7)):
        bg = Image.new("RGBA", (size, size), pixlib.hex_rgb("#f8e8a0") + (255,))
        big = src.resize((56 * scale, 56 * scale), Image.NEAREST)
        bg.alpha_composite(big, ((size - big.width) // 2, (size - big.height) // 2 + scale))
        name = "icon.png" if size == 192 else "icon_foreground.png"
        bg.save(os.path.join(pixlib.ROOT, "assets", "ui", name))
    print("icon ok")


if __name__ == "__main__":
    make_icon()
