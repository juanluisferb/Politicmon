"""Back sprites (32x32) and menu icons (16x16) built from per-politician traits."""
from .canvas import Canvas
from .chibi import L, A, D

# hair: style, hair color; hat; body fill; extra accessory key
TRAITS = {
    1: dict(hair="messy", hc=D, body=D, extra="folder"),
    2: dict(hair="slick", hc=D, body=D, extra="phones"),
    3: dict(hair="slick", hc=D, body=D, extra="phones3", big=True),
    4: dict(hair="messy", hc=D, body=D, extra="mic"),
    5: dict(hair="balding", hc=D, body=D, extra="bubble"),
    6: dict(hair="bun", hc=D, body=A, extra="chart"),
    7: dict(hair="side", hc=A, body=D, extra="briefcase"),
    8: dict(hair="messy", hc=D, body=A, extra="bigphone"),
    9: dict(hair="balding", hc=D, body=L, extra="mug"),
    10: dict(hair="short", hc=D, body=D, extra="horn"),
    11: dict(hair="side", hc=D, body=A, extra="staff"),
    12: dict(hair="balding", hc=L, body=D, extra="cape", big=True),
    13: dict(hair="pompadour", hc=D, body=D, extra="spark"),
    14: dict(hair="ponytail", hc=D, body=A, extra="megaphone"),
    15: dict(hair="none", hc=D, body=L, hat="straw", extra="hoe"),
    16: dict(hair="messy", hc=D, body=A, extra="banner"),
    17: dict(hair="balding", hc=D, body=A, extra="envelope"),
    18: dict(hair="none", hc=D, body=A, hat="beret", extra="umbrella"),
    19: dict(hair="none", hc=D, body=A, hat="beret", extra="coat", big=True),
    20: dict(hair="short", hc=D, body=A, extra="fog"),
    21: dict(hair="balding", hc=L, body=D, extra="toga"),
    22: dict(hair="bun", hc=D, body=A, extra="files"),
    23: dict(hair="none", hc=D, body=L, hat="cap", extra="rake"),
    24: dict(hair="side", hc=D, body=D, extra="scroll"),
    25: dict(hair="slick", hc=D, body=A, extra="sash", big=True),
}


def back_sprite(pid):
    t = TRAITS[pid]
    c = Canvas(32, 32)
    big = t.get("big", False)
    hx, hy = 15.5, 11
    rx, ry = (10, 9) if not big else (10, 9)
    body = t["body"]
    extra = t.get("extra")
    # body seen from behind (cut at the bottom edge)
    wt, wb = (18, 22) if not big else (22, 26)
    if extra == "cape" or extra == "coat":
        c.poly([(int(hx - wt / 2) - 1, 19), (int(hx - wb / 2) - 3, 31), (int(hx + wb / 2) + 3, 31), (int(hx + wt / 2) + 1, 19)], A, D)
    c.poly([(int(hx - wt / 2 + 0.5), 19), (int(hx - wb / 2 + 0.5), 31), (int(hx + wb / 2 - 0.5), 31), (int(hx + wt / 2 - 0.5), 19)], body, D)
    if extra == "toga":
        c.vline(15, 21, 31, D)
    if extra == "sash":
        c.poly([(8, 21), (11, 20), (24, 31), (20, 31)], L, D)
    # arms
    c.ellipse(hx - wt / 2 - 1, 25, 3, 5, body, D)
    c.ellipse(hx + wt / 2 + 1, 25, 3, 5, body, D)
    # ears
    c.ellipse(hx - rx, hy + 1, 2, 2, L, D)
    c.ellipse(hx + rx, hy + 1, 2, 2, L, D)
    c.ellipse(hx, hy, rx, ry, L, D)
    hair = t["hair"]
    hc = t["hc"]
    m = c.ellipse_mask(hx, hy, rx, ry)
    if hair in ("short", "messy", "slick", "side", "pompadour", "bun", "ponytail"):
        cover = {(x, y) for (x, y) in m if y <= hy + ry * 0.55}
        c.paint_mask(cover, hc, D)
        c.hline(int(hx - 4), int(hx + 1), int(hy - ry + 3), L)
    elif hair == "balding":
        cover = {(x, y) for (x, y) in m if y >= hy + 1 and y <= hy + ry * 0.7}
        c.paint_mask(cover, hc, D)
        if hc == L:
            c.hline(int(hx - 6), int(hx + 6), int(hy + 1), D)
        c.hline(int(hx - 3), int(hx), int(hy - ry + 3), D)
    if hair == "messy":
        for x in range(int(hx - 7), int(hx + 8), 4):
            c.poly([(x - 1, int(hy - ry + 2)), (x + 1, int(hy - ry - 2)), (x + 2, int(hy - ry + 2))], hc, D)
    if hair == "pompadour":
        c.ellipse(hx - 2, hy - ry, 6, 3, hc, D)
    if hair == "bun":
        c.ellipse(hx, hy - ry, 4, 3, hc, D)
    if hair == "ponytail":
        c.ellipse(hx, hy + 4, 3, 6, hc, D)
    hat = t.get("hat")
    if hat == "straw":
        c.ellipse(hx, hy - 3, 15, 3, L, D)
        c.ellipse(hx, hy - 6, 8, 4, L, D)
        c.hline(int(hx - 7), int(hx + 7), int(hy - 4), A)
    elif hat == "beret":
        c.ellipse(hx, hy - 5, 11, 4, D, D)
        c.vline(int(hx), int(hy - 11), int(hy - 9), D)
    elif hat == "cap":
        c.ellipse(hx, hy - 4, 10, 5, A, D)
        c.hline(int(hx - 10), int(hx + 10), int(hy - 1), D)
    # accessories
    if extra == "phones" or extra == "phones3":
        c.rect(1, 20, 4, 25, D, D)
        c.rect(27, 20, 30, 25, D, D)
        c.set(2, 22, L)
        c.set(28, 22, L)
        if extra == "phones3":
            c.rect(26, 1, 29, 6, D, D)
            c.set(27, 3, L)
    elif extra == "folder":
        c.rect(23, 21, 30, 30, A, D)
    elif extra == "mic":
        c.vline(27, 14, 20, D)
        c.ellipse(27, 12, 2, 2, D, D)
    elif extra == "bubble":
        c.rect(23, 0, 31, 5, L, D)
        c.set(25, 2, D)
        c.set(27, 2, D)
        c.set(29, 2, D)
    elif extra == "chart":
        c.rect(0, 2, 8, 12, L, D)
        c.rect(2, 7, 3, 10, A, A)
        c.rect(5, 4, 6, 10, A, A)
    elif extra == "briefcase":
        c.rect(0, 24, 7, 31, D, D)
        c.hline(1, 6, 26, A)
    elif extra == "bigphone":
        c.rect(22, 4, 31, 31, D, D)
        c.rect(24, 6, 29, 27, A, None)
    elif extra == "mug":
        c.rect(26, 22, 30, 27, L, D)
    elif extra == "horn":
        c.poly([(24, 16), (31, 12), (31, 22), (24, 19)], A, D)
    elif extra == "staff" or extra == "cape":
        c.vline(29, 3, 31, D)
        c.vline(30, 3, 31, L)
        c.ellipse(29.5, 3, 2, 2, L, D)
    elif extra == "spark":
        c.stamp("..2..\n2.2.2\n..2..", 26, 2)
        c.stamp("2.2\n.2.", 2, 6)
    elif extra == "megaphone":
        c.poly([(24, 8), (30, 2), (31, 9), (26, 11)], L, D)
    elif extra == "hoe":
        c.line(2, 3, 26, 26, D)
        c.poly([(0, 0), (5, 0), (4, 4), (1, 5)], L, D)
    elif extra == "banner":
        c.vline(28, 0, 31, D)
        c.rect(20, 0, 31, 8, L, D)
    elif extra == "envelope":
        c.rect(0, 26, 31, 31, L, D)
        c.line(0, 26, 15, 31, D)
        c.line(31, 26, 16, 31, D)
    elif extra == "umbrella":
        c.line(1, 14, 7, 31, D)
        c.poly([(1, 16), (4, 15), (8, 28), (5, 29)], A, D)
    elif extra == "coat":
        c.vline(15, 22, 31, D)
        c.vline(16, 22, 31, D)
    elif extra == "fog":
        for i, x in enumerate(range(-2, 34, 7)):
            c.ellipse(x, 26 + (i % 2), 5, 3, L, None)
        c.rect(0, 28, 31, 31, L, None)
        for x in range(0, 32, 2):
            if c.get(x, 23) == L or c.get(x, 24) == L:
                pass
        for i, x in enumerate(range(0, 32, 2)):
            c.set(x, 24 + (i % 3 == 0), A)
    elif extra == "files":
        for y in range(4, 31, 3):
            c.rect(0, y, 6, y + 2, L if (y // 3) % 3 else A, D)
            c.rect(25, y + 1, 31, y + 3, L if (y // 3) % 2 else A, D)
    elif extra == "rake":
        c.vline(28, 2, 31, D)
        c.rect(24, 1, 31, 2, D, D)
    elif extra == "scroll":
        c.rect(25, 12, 29, 31, L, D)
        for y in range(15, 30, 3):
            c.hline(26, 28, y, D)
    return c


def icon(pid):
    t = TRAITS[pid]
    c = Canvas(16, 16)
    hx, hy = 7.5, 6
    body = t["body"]
    c.poly([(4, 11), (3, 15), (12, 15), (11, 11)], body, D)
    if t.get("extra") == "cape":
        c.poly([(3, 11), (1, 15), (14, 15), (12, 11)], A, D)
        c.poly([(5, 11), (4, 15), (11, 15), (10, 11)], body, D)
    if t.get("extra") == "toga":
        c.set(7, 12, L)
        c.set(8, 12, L)
    if t.get("extra") == "sash":
        c.line(4, 11, 10, 15, L)
    c.ellipse(hx, hy, 5, 5, L, D)
    hair = t["hair"]
    hc = t["hc"]
    m = c.ellipse_mask(hx, hy - 1, 6, 5)
    if hair in ("short", "messy", "slick", "side", "pompadour", "bun", "ponytail"):
        c.paint_mask({(x, y) for (x, y) in m if y <= hy - 2 or (y <= hy and (x <= 2 or x >= 13))}, hc, D)
    elif hair == "balding":
        c.paint_mask({(x, y) for (x, y) in m if (x <= 3 or x >= 12) and hy - 2 <= y <= hy + 1}, hc, D)
    if hair == "messy":
        c.set(5, 0, hc)
        c.set(9, 0, hc)
    if hair == "pompadour":
        c.ellipse(6, 1, 4, 1, hc, D)
    if hair == "bun":
        c.ellipse(7.5, 0, 2, 1, hc, D)
    hat = t.get("hat")
    if hat == "straw":
        c.hline(0, 15, 3, D)
        c.rect(3, 0, 12, 2, L, D)
        c.hline(4, 11, 2, A)
    elif hat == "beret":
        c.ellipse(7.5, 2, 6, 2, D, D)
    elif hat == "cap":
        c.rect(3, 0, 12, 2, A, D)
        c.hline(2, 13, 3, D)
    # face
    c.set(5, 6, D)
    c.set(10, 6, D)
    c.set(5, 7, D)
    c.set(10, 7, D)
    extra = t.get("extra")
    if pid == 3:
        c.hline(3, 12, 6, D)
        c.hline(3, 12, 5, D)
    if pid in (6,):
        c.rect(3, 5, 6, 7, None, D)
        c.rect(9, 5, 12, 7, None, D)
    c.hline(6, 9, 9, D)
    if extra in ("phones", "phones3"):
        c.rect(13, 9, 15, 13, D, D)
    elif extra == "folder":
        c.rect(5, 11, 10, 15, A, D)
    elif extra in ("mic", "spark"):
        c.set(14, 4, A)
        c.set(13, 5, A)
        c.set(15, 5, A)
    elif extra in ("horn", "megaphone"):
        c.poly([(12, 9), (15, 7), (15, 12), (12, 11)], A, D)
    elif extra in ("staff", "cape", "rake"):
        c.vline(14, 2, 15, D)
    elif extra in ("briefcase", "envelope"):
        c.rect(11, 12, 15, 15, D if extra == "briefcase" else L, D)
    elif extra in ("bigphone",):
        c.rect(12, 4, 15, 15, D, D)
        c.vline(13, 6, 13, A)
    elif extra == "fog":
        c.rect(0, 13, 15, 15, L, None)
        for x in range(0, 16, 2):
            c.set(x, 13, A)
    elif extra in ("banner", "chart"):
        c.rect(11, 0, 15, 4, L, D)
    elif extra in ("files", "scroll"):
        c.rect(12, 8, 15, 15, L, D)
    elif extra == "mug":
        c.rect(12, 11, 14, 14, L, D)
    elif extra == "hoe":
        c.line(0, 1, 5, 11, D)
    elif extra == "umbrella":
        c.line(14, 6, 12, 15, A)
    elif extra == "bubble":
        c.rect(11, 0, 15, 3, L, D)
    return c
