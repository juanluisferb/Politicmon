#!/usr/bin/env python3
"""Icono de la app (192x192): cara de Pepis con un calcetín robado, pixel art ampliado x8."""
import sys
from PIL import Image, ImageDraw

ART = [
    "........................",
    "...ooo............ooo...",
    "..offfo..........offfo..",
    "..ofmmfo........ofmmfo..",
    "..ofmmfooooooooooffmfo..",
    "...offffffffffffffffo...",
    "...offffffffffffffffo...",
    "..offmmmmffffffmmmmffo..",
    "..ofmmmmmmffffmmmmmmfo..",
    ".ofmmmmewmmffmmewmmmmfo.",
    ".ofmmmmeemmffmmeemmmmfo.",
    ".ofmmmmmmmffffmmmmmmmfo.",
    ".offmmmmmffffffmmmmmffo.",
    "..offffffffffffffffffo..",
    "..offffffffnnffffffffo..",
    "...offffffonnoffffffo...",
    "....offffffoofffffffo...",
    ".....offkkkkkkkkffo.....",
    "......okkskkkskkko......",
    "......okkkkkkkkkko......",
    ".......okskkkskko.......",
    "........okkkkkko........",
    ".........oooooo.........",
    "........................",
]
PAL = {
    "o": (58, 30, 18), "f": (246, 226, 190), "m": (120, 72, 40), "e": (16, 12, 16),
    "w": (255, 255, 255), "n": (236, 128, 150), "k": (214, 40, 56), "s": (252, 244, 232),
}
BG, BG2 = (255, 170, 60), (232, 110, 36)


def main(out):
    s = 8
    img = Image.new("RGBA", (192, 192), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, 191, 191], radius=36, fill=BG2)
    d.rounded_rectangle([8, 8, 183, 183], radius=30, fill=BG)
    for y, row in enumerate(ART):
        for x, c in enumerate(row.ljust(24, ".")[:24]):
            if c in PAL:
                d.rectangle([x * s, y * s + 4, x * s + s - 1, y * s + s + 3], fill=PAL[c])
    img.save(out)


if __name__ == "__main__":
    main(sys.argv[1])
