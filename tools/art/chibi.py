"""Chibi building blocks for 56x56 battle sprites (big head, small body)."""
from .canvas import Canvas, rows

L, A, D = 1, 2, 3
CX = 27.5

EYES = {
    "dot": "33\n33\n33",
    "big": ".33.\n3133\n3333\n.33.",
    "tall": "33\n13\n33\n33",
    "half": "3333\n.33.",
    "happy": ".33.\n3..3",
    "ring": ".33.\n3113\n3133\n.33.",
    "small": "33\n33",
    "line": "333",
    "wink": "333\n...",
    "tired": ".33.\n3133\n3333\n.33.\n....\n.22.",
}

BROWS = {
    "flat": "333",
    "worried": "..33\n33..",
    "angry": "33..\n..33",
    "up": ".33.\n3..3",
    "thick": "3333\n3333",
    "raised": ".333\n3...",
}

MOUTHS = {
    "smile": "3....3\n.3333.",
    "grin": "333333\n311113\n.3333.",
    "open": ".3333.\n322223\n.3333.",
    "talk": ".333.\n32223\n32223\n.333.",
    "o": ".33.\n3223\n.33.",
    "flat": "3333",
    "frown": ".3333.\n3....3",
    "smirk": ".....3\n33333.",
    "small": "33",
    "wavy": "3.33.3\n.3..3.",
    "poster": "3333333333\n3111111113\n3131313113\n.33333333.",
    "wide_smile": "33......33\n.33....33.\n..333333..",
    "shh": "..3..\n.333.\n..3..",
    "worried": ".3333.\n3....3",
}


def new():
    return Canvas(56, 56)


def eyes(c, y, dx, style="big", brow=None, brow_dy=-3, cx=CX):
    st = rows(EYES[style])
    w = len(st[0])
    xl = int(cx - dx - w / 2 + 0.5)
    c.stamp(st, xl, y)
    c.stamp(st, int(2 * cx) - xl - w + 1, y, flip=True)
    if brow:
        b = rows(BROWS[brow])
        bw = len(b[0])
        bx = int(cx - dx - bw / 2 + 0.5)
        by = y + brow_dy - (len(b) - 1)
        c.stamp(b, bx, by)
        c.stamp(b, int(2 * cx) - bx - bw + 1, by, flip=True)


def mouth(c, y, style="smile", cx=CX, dx=0, flip=False):
    st = rows(MOUTHS[style])
    w = len(st[0])
    x = int(cx - w / 2 + 0.5) + dx
    c.stamp(st, x, y, flip=flip)


def blush(c, y, dx, cx=CX, color=A):
    xl = int(cx - dx - 1)
    c.hline(xl, xl + 1, y, color)
    xr = int(2 * cx) - xl - 1
    c.hline(xr - 1, xr, y, color)


def head(c, cy, rx, ry, cx=CX, ears=True, fill=L):
    if ears:
        c.ellipse(cx - rx, cy + 2, 2, 3, fill, D)
        c.ellipse(cx + rx, cy + 2, 2, 3, fill, D)
    c.ellipse(cx, cy, rx, ry, fill, D)


def hair(c, cy, rx, ry, style="short", cx=CX, color=D, shine=True):
    """Hair drawn over the head. Styles define the fringe line."""
    top = cy - ry
    big = c.ellipse_mask(cx, cy - 1, rx + 1, ry + 1)
    if style == "short":
        lim = lambda x, y: y <= cy - ry * 0.35 + (abs(x - cx) > rx * 0.55) * 3
    elif style == "side":
        lim = lambda x, y: y <= cy - ry * 0.3 - (x - cx) * 0.25 + (abs(x - cx) > rx * 0.7) * 4
    elif style == "slick":
        lim = lambda x, y: y <= cy - ry * 0.55 + (abs(x - cx) > rx * 0.6) * 6
    elif style == "bob":
        lim = lambda x, y: y <= cy - ry * 0.35 or (abs(x - cx) > rx * 0.62 and y <= cy + ry * 0.55)
    elif style == "long":
        lim = lambda x, y: y <= cy - ry * 0.4 or (abs(x - cx) > rx * 0.66 and y <= cy + ry * 0.9)
    elif style == "balding":
        lim = lambda x, y: (abs(x - cx) > rx * 0.62 and cy - ry * 0.45 <= y <= cy + ry * 0.15)
    elif style == "bangs":
        lim = lambda x, y: y <= cy - ry * 0.2 - ((int(x) % 6) < 3) * 2 or (abs(x - cx) > rx * 0.7 and y <= cy + ry * 0.3)
    elif style == "messy":
        lim = lambda x, y: y <= cy - ry * 0.3 + ((int(x) % 5) == 0) * 3
    else:
        lim = lambda x, y: False
    mask = {(x, y) for (x, y) in big if lim(x, y)}
    if style == "messy":
        for i, sx in enumerate(range(int(cx - rx * 0.8), int(cx + rx * 0.8), 5)):
            h = 4 + (i % 2) * 2
            mask |= c.poly_mask([(sx - 2, top + 3), (sx + 1, top - h + 2), (sx + 3, top + 3)])
    c.paint_mask(mask, color, D)
    if shine and color == D:
        sx = int(cx - rx * 0.45)
        sy = int(top + 3)
        c.hline(sx, sx + 3, sy, L)
        c.set(sx - 1, sy + 1, L)
    return mask


def bun(c, cy, ry, cx=CX, r=5):
    c.ellipse(cx, cy - ry - r + 2, r, r - 1, D, D)
    c.hline(int(cx - 2), int(cx), int(cy - ry - r), L)


def torso(c, y0, y1, wt, wb, fill=D, cx=CX):
    """Trapezoid body: top width wt at y0, bottom width wb at y1."""
    pts = [(int(cx - wt / 2 + 0.5), y0), (int(cx - wb / 2 + 0.5), y1),
           (int(cx + wb / 2 - 0.5), y1), (int(cx + wt / 2 - 0.5), y0)]
    c.poly(pts, fill, D)


def shirt_v(c, y0, depth, half, cx=CX, tie=A, tie_len=None):
    """White V of shirt at collar with an optional tie."""
    for i in range(depth):
        w = max(0, half - int(i * half / depth))
        c.hline(int(cx - w), int(cx + w), y0 + i, L)
    if tie is not None:
        tl = tie_len or depth + 4
        c.set(int(cx), y0, tie)
        c.set(int(cx) + 1, y0, tie)
        for i in range(1, tl):
            ww = 1 if i < tl - 2 else 0
            c.hline(int(cx) - ww, int(cx) + 1 + ww, y0 + i, tie)
        c.hline(int(cx), int(cx) + 1, y0 + tl, D)


def lapels(c, y0, depth, half, cx=CX):
    for i in range(depth):
        w = max(0, half - int(i * half / depth))
        c.set(int(cx - w) - 1, y0 + i, D)
        c.set(int(cx + w) + 2, y0 + i, D)


def legs(c, y0, y1, gap=2, w=4, cx=CX, color=D, shoes=True):
    xl = int(cx) - gap // 2 - w + 1
    xr = int(cx) + 1 + gap // 2
    c.rect(xl, y0, xl + w - 1, y1, color, D)
    c.rect(xr, y0, xr + w - 1, y1, color, D)
    if shoes:
        c.ellipse(xl + w / 2 - 1.5, y1, w / 2 + 0.5, 1, D, D)
        c.ellipse(xr + w / 2 - 0.5 + 1, y1, w / 2 + 0.5, 1, D, D)


def hand(c, x, y, r=2):
    c.ellipse(x, y, r, r, L, D)


def arm(c, x0, y0, x1, y1, width=4, fill=D):
    """Thick arm as a polygon between shoulder (x0,y0) and wrist (x1,y1)."""
    import math
    dx, dy = x1 - x0, y1 - y0
    ln = max(1.0, math.hypot(dx, dy))
    nx, ny = -dy / ln * width / 2, dx / ln * width / 2
    pts = [(round(x0 + nx), round(y0 + ny)), (round(x1 + nx), round(y1 + ny)),
           (round(x1 - nx), round(y1 - ny)), (round(x0 - nx), round(y0 - ny))]
    c.poly(pts, fill, D)


def finish(c, bottom=54):
    c.ground(bottom)
    return c
