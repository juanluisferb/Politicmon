"""Render helper: draw a set of canvases into a big preview PNG."""
import os
import sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import pixlib
from PIL import Image


def to_image(c, pal):
    return pixlib.grid_to_image(c.px, pal)


def sheet(canvases, pals, path, cols=5, scale=3, labels=None, bg=(255, 255, 255, 255)):
    imgs = [to_image(c, p) for c, p in zip(canvases, pals)]
    cell = (max(i.width for i in imgs), max(i.height for i in imgs))
    img = pixlib.contact_sheet(imgs, cols, cell, bg=bg, scale=scale, labels=labels)
    img.save(path)
    return path
