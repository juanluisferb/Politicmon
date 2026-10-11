"""Front battle sprites (56x56) for the 25 politicians, drawn pixel by pixel.

Palette indices: 0 transparent, 1 light (skin/white), 2 type accent, 3 dark (outline/hair).
"""
from .canvas import Canvas, rows
from .chibi import (new, eyes, mouth, blush, head, hair, bun, torso, shirt_v, lapels, legs,
                    hand, arm, finish, L, A, D, CX)

TINY = {
    "C": "33\n3.\n3.\n3.\n33", "E": "33\n3.\n33\n3.\n33", "R": "33.\n3.3\n33.\n3.3\n3.3",
    "A": ".3.\n3.3\n333\n3.3\n3.3", "D": "33.\n3.3\n3.3\n3.3\n33.", "O": "333\n3.3\n3.3\n3.3\n333",
    "#": "3.3\n333\n3.3\n333\n3.3", "?": "33.\n..3\n.3.\n...\n.3.", "!": "3\n3\n3\n.\n3",
    "S": "333\n3..\n333\n..3\n333", "I": "3\n3\n3\n3\n3", ".": ".\n.\n.\n.\n3",
}


def tiny_text(c, text, x, y, color=D):
    for ch in text:
        g = rows(TINY[ch])
        remap = {"3": color}
        c.stamp(g, x, y, remap=remap)
        x += len(g[0]) + 1
    return x


PHONE = """
33333
31113
31113
31113
31113
33133
33333
"""

SWEAT = """
.3.
313
313
.3.
"""

DROP = """
.3.
313
3113
.33.
"""

SPARK = """
..2..
..2..
22.22
..2..
..2..
"""

STAR = """
.2.
232
.2.
"""

BUBBLE = """
.33333333.
3111111113
3131313113
3111111113
.33333333.
..33......
..3.......
"""

BUBBLE_Q = """
.3333333.
311333113
311113113
311131113
311111113
311131113
.3333333.
.33......
.3.......
"""

FOG = """
....3333.......33333....
..33111133...331111133..
.3111111113331111111113.
311111111111111111111113
.3333333333333333333333.
"""


def fog_band(c, y, x0=0, x1=55, rim=D):
    """Fog: puffy light clouds with a dotted rim (GBC style, no transparency)."""
    mask = set()
    for i, x in enumerate(range(x0 - 4, x1 + 8, 9)):
        mask |= c.ellipse_mask(x, y + (i % 2) * 2, 6, 4)
    for yy in range(y + 2, c.h):
        for xx in range(x0, x1 + 1):
            mask.add((xx, yy))
    mask = {(x, yy) for (x, yy) in mask if x0 <= x <= x1 and 0 <= yy < c.h}
    for (x, yy) in mask:
        c.set(x, yy, L)
    for (x, yy) in mask:
        if (x, yy - 1) not in mask and (x + yy) % 2 == 0:
            c.set(x, yy, rim)
    for i, x in enumerate(range(x0 + 3, x1, 11)):
        c.hline(x, x + 3, y + 6 + (i % 2) * 3, rim if (i % 3) else L)


def p01_becario():
    c = new()
    legs(c, 44, 50, gap=2, w=4)
    c.rect(20, 47, 35, 49, D, D)
    torso(c, 27, 47, 22, 27)
    shirt_v(c, 27, 5, 3, tie=A, tie_len=5)
    c.set(18, 31, L)
    c.set(37, 31, L)
    # long sleeves hugging a big folder against the chest
    arm(c, 16, 30, 20, 42, 6)
    arm(c, 39, 30, 35, 42, 6)
    c.rect(19, 30, 36, 45, A, D)
    c.rect(22, 33, 33, 37, L, D)
    c.hline(23, 32, 35, D)
    c.hline(22, 33, 40, D)
    c.hline(22, 29, 42, D)
    c.rect(36, 32, 37, 35, L, D)
    c.ellipse(20, 43, 3, 2, D, D)
    c.ellipse(35, 43, 3, 2, D, D)
    c.set(19, 43, L)
    c.set(36, 43, L)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "messy")
    eyes(c, 16, 5, "big", brow="worried", brow_dy=-2)
    mouth(c, 23, "wavy")
    c.stamp(SWEAT, 40, 9)
    c.stamp(SWEAT, 13, 13)
    return finish(c, 52)


def p02_asesor():
    c = new()
    legs(c, 43, 52, gap=2, w=4)
    torso(c, 27, 44, 17, 16)
    shirt_v(c, 27, 7, 3, tie=A, tie_len=8)
    lapels(c, 27, 7, 3)
    c.hline(20, 21, 29, L)
    c.hline(34, 35, 29, L)
    arm(c, 21, 30, 12, 37, 4)
    arm(c, 34, 30, 43, 37, 4)
    c.stamp(PHONE, 7, 29)
    c.stamp(PHONE, 44, 29)
    hand(c, 11, 37)
    hand(c, 44, 37)
    for (x, y, f) in ((5, 23, False), (47, 23, True)):
        c.stamp("2..\n.2.\n2.2", x, y, flip=f)
    head(c, 15, 12, 11)
    hair(c, 15, 12, 11, "slick")
    eyes(c, 15, 5, "half", brow="raised", brow_dy=-2)
    mouth(c, 21, "smirk")
    c.rect(38, 15, 40, 18, A, D)
    c.vline(41, 18, 27, D)
    c.set(40, 22, D)
    c.set(42, 25, D)
    return finish(c, 53)


def p03_jefe():
    c = new()
    legs(c, 45, 54, gap=3, w=5)
    torso(c, 28, 46, 30, 22)
    shirt_v(c, 28, 7, 4, tie=A, tie_len=5)
    lapels(c, 28, 7, 4)
    c.hline(15, 18, 30, L)
    c.hline(37, 40, 30, L)
    # crossed arms
    arm(c, 14, 33, 36, 39, 6)
    arm(c, 41, 33, 19, 39, 6)
    hand(c, 37, 38, 2)
    hand(c, 18, 38, 2)
    c.set(31, 42, A)
    c.set(32, 42, A)
    head(c, 16, 13, 11)
    hair(c, 16, 13, 11, "slick")
    c.rect(16, 13, 26, 18, D, D)
    c.rect(29, 13, 39, 18, D, D)
    c.hline(26, 29, 14, D)
    c.hline(18, 21, 14, L)
    c.hline(31, 34, 14, L)
    c.set(18, 15, L)
    c.set(31, 15, L)
    mouth(c, 22, "smirk")
    for (x, y) in ((1, 9), (47, 3), (48, 22)):
        c.stamp(PHONE, x, y)
        c.stamp("2.2\n.2.", x + 1, y - 3)
    return finish(c, 55)


def p04_tertulianete():
    c = new()
    legs(c, 43, 49, gap=2, w=4, color=L)
    c.rect(21, 42, 34, 45, D, D)
    torso(c, 31, 43, 15, 17, fill=D)
    shirt_v(c, 31, 6, 3, tie=None)
    c.set(27, 39, L)
    c.set(28, 41, L)
    arm(c, 21, 33, 17, 41, 4, fill=L)
    hand(c, 16, 42)
    # toy mic held up to the mouth
    arm(c, 34, 33, 38, 27, 4, fill=L)
    hand(c, 38, 26)
    c.vline(39, 20, 25, D)
    c.vline(40, 20, 25, L)
    c.ellipse(39.5, 17, 3, 3, D, D)
    c.set(38, 16, L)
    head(c, 17, 13, 12, cx=26.5)
    hair(c, 17, 13, 12, "bangs", cx=26.5)
    c.stamp("..3\n.33\n33.\n3..", 26, 1)
    eyes(c, 17, 5, "big", brow="up", brow_dy=-2, cx=26.5)
    mouth(c, 22, "talk", cx=26.5)
    # crooked bow tie
    c.stamp("33...3\n3233.3\n322223\n3.3323\n3...33", 23, 29, remap={"2": A})
    c.stamp(TINY["!"], 45, 6)
    c.stamp(TINY["!"], 48, 8)
    return finish(c, 51)


def p05_todologo():
    c = new()
    # speech bubbles
    c.stamp(BUBBLE, 1, 2)
    c.stamp(BUBBLE_Q, 45, 0)
    torso(c, 27, 44, 20, 20)
    shirt_v(c, 27, 6, 3, tie=A, tie_len=6)
    lapels(c, 27, 6, 3)
    # raised index finger
    arm(c, 36, 30, 42, 20, 5)
    hand(c, 42, 18, 3)
    c.rect(42, 11, 43, 16, L, D)
    head(c, 15, 12, 11)
    hair(c, 15, 12, 11, "balding")
    c.hline(23, 32, 4, D)
    eyes(c, 15, 5, "small", brow="raised", brow_dy=-2)
    mouth(c, 21, "open")
    # lectern with mic
    c.poly([(14, 33), (41, 33), (38, 54), (17, 54)], A, D)
    c.rect(12, 31, 43, 34, A, D)
    c.rect(22, 38, 33, 48, L, D)
    c.hline(24, 31, 41, D)
    c.hline(24, 29, 44, D)
    c.line(19, 31, 21, 25, D)
    c.line(21, 25, 23, 23, D)
    c.ellipse(23.5, 22, 2, 1, D, D)
    return finish(c, 54)


def p06_infografia():
    c = new()
    # floating bar chart
    c.rect(1, 14, 17, 34, L, D)
    c.vline(3, 16, 31, D)
    c.hline(3, 15, 31, D)
    for i, h in enumerate((4, 8, 6, 12)):
        c.rect(5 + i * 3, 30 - h, 6 + i * 3, 30, A, A)
    c.stamp("3\n33\n333", 13, 15)
    legs(c, 44, 52, gap=3, w=3, color=L)
    c.poly([(19, 39), (36, 39), (38, 47), (17, 47)], D, D)
    torso(c, 27, 40, 17, 18, fill=A)
    shirt_v(c, 27, 6, 3, tie=None)
    lapels(c, 27, 6, 3)
    # laser pointer pointing at the chart
    arm(c, 20, 30, 15, 37, 4, fill=A)
    hand(c, 15, 37)
    c.line(14, 36, 11, 33, D)
    for i in range(0, 8, 2):
        c.set(10 - i, 32 - i // 2, A)
    arm(c, 35, 30, 37, 40, 4, fill=A)
    hand(c, 37, 41)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "bob")
    bun(c, 16, 11, r=4)
    # thick-rimmed glasses
    c.rect(18, 14, 25, 19, L, D)
    c.rect(30, 14, 37, 19, L, D)
    c.hline(25, 30, 15, D)
    c.rect(21, 16, 22, 18, D, D)
    c.rect(33, 16, 34, 18, D, D)
    mouth(c, 23, "smile")
    return finish(c, 53)


def p07_puertagiratoria():
    c = new()
    # revolving door behind
    c.rect(4, 4, 51, 9, D, D)
    c.rect(6, 9, 49, 54, L, D)
    c.vline(27, 9, 54, D)
    c.vline(28, 9, 54, D)
    c.line(10, 12, 24, 52, A)
    c.line(45, 12, 31, 52, A)
    c.hline(7, 48, 6, L)
    c.rect(4, 52, 51, 54, D, D)
    # man stepping out
    legs(c, 43, 52, gap=4, w=4)
    torso(c, 28, 44, 18, 17)
    shirt_v(c, 28, 6, 3, tie=A, tie_len=6)
    lapels(c, 28, 6, 3)
    arm(c, 21, 30, 15, 40, 4)
    hand(c, 15, 41)
    # leather briefcase
    c.rect(8, 42, 21, 51, D, D)
    c.rect(12, 40, 17, 42, None, D)
    c.hline(9, 20, 45, A)
    c.rect(14, 44, 15, 46, L, D)
    arm(c, 34, 30, 39, 38, 4)
    hand(c, 39, 39)
    c.rect(37, 35, 40, 37, L, D)
    c.set(38, 36, A)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "side", color=A)
    c.line(19, 9, 24, 7, D)
    eyes(c, 16, 5, "half", brow="flat", brow_dy=-2)
    mouth(c, 22, "smile")
    return finish(c, 54)


def p08_trending():
    c = new()
    # giant phone
    c.rect(26, 2, 52, 52, D, D)
    c.rect(28, 6, 50, 46, L, D)
    c.rect(36, 3, 42, 4, L, None)
    c.ellipse(39, 49, 2, 1, L, L)
    for i, y in enumerate((9, 17, 25, 33)):
        c.ellipse(32, y + 2, 2, 2, A, D)
        c.hline(36, 47 - (i % 2) * 4, y + 1, D)
        c.hline(36, 44, y + 3, A)
    c.stamp(TINY["#"], 44, 39)
    # small body glued to it
    legs(c, 46, 52, gap=2, w=3)
    torso(c, 35, 47, 12, 13, fill=A)
    tiny_text(c, "#", 16, 38)
    arm(c, 21, 37, 27, 33, 3, fill=A)
    hand(c, 28, 33)
    arm(c, 13, 37, 9, 44, 3, fill=A)
    hand(c, 9, 45)
    head(c, 24, 11, 10, cx=18.5)
    hair(c, 24, 11, 10, "messy", cx=18.5)
    eyes(c, 24, 4, "big", cx=18.5)
    c.hline(13, 15, 28, D)
    c.hline(22, 24, 28, D)
    mouth(c, 30, "flat", cx=18.5)
    return finish(c, 54)


def p09_ventanilla():
    c = new()
    # wall with a little window
    c.rect(2, 12, 53, 54, L, D)
    c.rect(2, 10, 53, 13, D, D)
    c.rect(10, 18, 45, 42, D, D)
    c.rect(12, 20, 43, 41, A, None)
    # clerk inside
    torso(c, 33, 42, 20, 22, fill=L)
    c.vline(27, 34, 41, D)
    c.vline(28, 34, 41, D)
    head(c, 30, 9, 8)
    hair(c, 30, 9, 8, "balding")
    eyes(c, 30, 4, "half", brow="flat", brow_dy=-1)
    c.stamp("333333\n3....3", 25, 34)
    mouth(c, 36, "flat")
    # stamp raised
    arm(c, 36, 37, 39, 30, 4, fill=L)
    hand(c, 39, 29)
    c.rect(38, 22, 40, 27, D, D)
    c.rect(36, 27, 42, 29, A, D)
    # counter
    c.rect(4, 42, 51, 46, D, D)
    c.hline(5, 50, 43, L)
    # CERRADO sign
    c.rect(14, 46, 41, 54, L, D)
    c.line(14, 46, 20, 42, D)
    c.line(41, 46, 35, 42, D)
    tiny_text(c, "CERRADO", 16, 48)
    # coffee mug
    c.rect(4, 36, 10, 42, L, D)
    c.rect(10, 37, 12, 40, None, D)
    c.hline(5, 9, 38, A)
    c.stamp("3.3\n.3.\n3.3", 5, 31)
    return finish(c, 54)


def p10_pregonero():
    c = new()
    legs(c, 43, 50, gap=2, w=4)
    torso(c, 30, 44, 15, 17, fill=D)
    shirt_v(c, 30, 7, 3, tie=None)
    # sash
    c.rect(19, 38, 36, 41, A, D)
    c.rect(34, 40, 37, 46, A, D)
    c.set(35, 47, D)
    c.set(36, 47, D)
    # waving hand
    arm(c, 20, 32, 13, 26, 4, fill=L)
    hand(c, 12, 24, 3)
    c.stamp("3.3.3\n.....", 9, 19)
    # cone horn
    arm(c, 35, 32, 40, 30, 4, fill=L)
    c.poly([(38, 28), (52, 20), (52, 36), (38, 32)], A, D)
    c.vline(50, 22, 34, L)
    hand(c, 39, 30)
    head(c, 17, 13, 12)
    hair(c, 17, 13, 12, "short")
    eyes(c, 17, 5, "happy")
    blush(c, 21, 8)
    mouth(c, 22, "wide_smile")
    return finish(c, 51)


def p11_alcalde():
    c = new()
    legs(c, 44, 53, gap=3, w=4)
    c.rect(19, 38, 36, 45, D, D)
    torso(c, 28, 39, 18, 17, fill=A)
    shirt_v(c, 28, 9, 3, tie=None)
    c.vline(27, 30, 37, D)
    c.set(24, 37, D)
    c.set(31, 37, D)
    # carnation on the lapel
    c.ellipse(34, 31, 2, 2, A, D)
    c.set(34, 31, L)
    c.line(34, 34, 33, 36, D)
    # staff of office
    c.vline(10, 13, 54, D)
    c.vline(11, 13, 54, L)
    c.ellipse(10.5, 11, 3, 3, L, D)
    c.set(10, 10, A)
    c.stamp("3.3\n3.3\n.3.", 9, 15)
    arm(c, 20, 31, 12, 34, 4, fill=A)
    hand(c, 11, 34)
    arm(c, 35, 31, 39, 40, 4, fill=A)
    hand(c, 39, 41)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "side")
    c.rect(15, 16, 16, 20, D, D)
    c.rect(39, 16, 40, 20, D, D)
    eyes(c, 16, 5, "happy", brow="flat", brow_dy=-3)
    mouth(c, 21, "grin")
    blush(c, 20, 8)
    return finish(c, 54)


def p12_baron_sur():
    c = new()
    # wide cape
    c.poly([(16, 26), (2, 54), (53, 54), (39, 26)], A, D)
    c.line(9, 44, 12, 53, D)
    c.line(46, 44, 43, 53, D)
    legs(c, 46, 54, gap=4, w=5)
    torso(c, 26, 47, 26, 30)
    shirt_v(c, 26, 7, 4, tie=A, tie_len=6)
    c.ellipse(27.5, 41, 10, 6, D, D)
    c.hline(21, 34, 44, A)
    # golden staff
    c.vline(46, 6, 54, D)
    c.vline(47, 6, 54, L)
    c.ellipse(46.5, 5, 3, 3, L, D)
    c.stamp(STAR, 45, 4)
    arm(c, 38, 30, 45, 34, 5)
    hand(c, 46, 34, 3)
    arm(c, 17, 30, 15, 42, 5)
    hand(c, 15, 43, 3)
    head(c, 15, 13, 12)
    hair(c, 15, 13, 12, "balding", color=L, shine=False)
    c.hline(21, 34, 4, D)
    eyes(c, 15, 5, "small", brow="thick", brow_dy=-2)
    c.stamp("..3333..3333..\n.33333333333.\n33........33", 21, 20)
    mouth(c, 23, "smile")
    return finish(c, 55)


def p13_pico_oro():
    c = new()
    legs(c, 44, 53, gap=3, w=4)
    torso(c, 28, 45, 18, 17)
    shirt_v(c, 28, 7, 3, tie=A, tie_len=7)
    lapels(c, 28, 7, 3)
    # arms wide open
    arm(c, 20, 30, 8, 22, 4)
    hand(c, 7, 21, 3)
    arm(c, 35, 30, 46, 24, 4)
    hand(c, 47, 23, 3)
    # golden mic in the right hand
    c.rect(46, 13, 48, 20, L, D)
    c.ellipse(47, 10, 3, 3, L, D)
    c.hline(45, 49, 10, D)
    c.stamp(STAR, 51, 5)
    head(c, 16, 12, 11)
    # pompadour
    hair(c, 16, 12, 11, "short")
    c.ellipse(24, 5, 8, 4, D, D)
    c.hline(20, 25, 3, L)
    eyes(c, 16, 5, "big", brow="up", brow_dy=-2)
    mouth(c, 21, "open")
    # sparks from the mouth
    c.stamp(SPARK, 13, 25)
    c.stamp(SPARK, 37, 23)
    c.stamp(STAR, 9, 31)
    c.stamp(STAR, 44, 30)
    return finish(c, 54)


def p14_mitinera():
    c = new()
    # little stage
    c.rect(6, 44, 49, 54, A, D)
    c.rect(4, 42, 51, 45, L, D)
    for x in range(10, 48, 8):
        c.vline(x, 47, 52, L)
    legs(c, 35, 42, gap=3, w=3, color=L)
    c.poly([(20, 30), (35, 30), (37, 38), (18, 38)], A, D)
    torso(c, 22, 31, 16, 15, fill=A)
    shirt_v(c, 22, 5, 2, tie=None)
    # megaphone raised high
    arm(c, 34, 24, 39, 12, 4, fill=A)
    hand(c, 39, 11)
    c.poly([(37, 8), (46, 0), (50, 6), (41, 12)], L, D)
    c.line(47, 1, 50, 5, A)
    arm(c, 21, 24, 15, 31, 4, fill=A)
    hand(c, 15, 32)
    head(c, 11, 10, 9)
    hair(c, 11, 10, 9, "bob")
    c.ellipse(15, 7, 3, 4, D, D)
    c.ellipse(13, 13, 2, 3, D, D)
    eyes(c, 11, 4, "happy")
    mouth(c, 15, "talk")
    # confetti
    for (x, y, col) in ((3, 3, A), (8, 12, D), (52, 18, A), (48, 30, D), (3, 26, A), (12, 36, D),
                        (29, 0, A), (52, 40, A), (6, 33, A)):
        c.rect(x, y, x + 1, y + 1, col, None)
    return finish(c, 54)


def p15_jornalero():
    c = new()
    # hoe over the shoulder
    c.line(6, 8, 44, 40, D)
    c.line(7, 8, 45, 40, D)
    c.poly([(3, 3), (10, 2), (9, 8), (4, 11)], L, D)
    legs(c, 44, 53, gap=3, w=4)
    torso(c, 29, 45, 18, 19, fill=L)
    c.vline(27, 31, 44, D)
    c.set(26, 34, D)
    c.set(26, 38, D)
    c.hline(19, 36, 44, D)
    arm(c, 21, 31, 16, 37, 4, fill=L)
    hand(c, 16, 37)
    arm(c, 35, 31, 41, 37, 4, fill=L)
    hand(c, 41, 37)
    c.line(6, 8, 44, 40, D)
    c.set(17, 18, D)
    head(c, 19, 12, 10)
    eyes(c, 20, 5, "dot", brow="angry", brow_dy=-2)
    mouth(c, 25, "frown")
    # straw hat
    c.ellipse(27.5, 12, 18, 3, L, D)
    c.ellipse(27.5, 8, 9, 5, L, D)
    c.hline(19, 36, 10, A)
    c.hline(19, 36, 11, A)
    for x in range(14, 42, 4):
        c.set(x, 13, D)
    return finish(c, 54)


def p16_lider_marea():
    c = new()
    # crowd silhouettes
    for i, x in enumerate((4, 13, 42, 51)):
        y = 33 + (i % 2) * 2
        c.ellipse(x, y, 4, 4, A, D)
        c.ellipse(x, y + 10, 6, 6, A, D)
    c.rect(0, 46, 55, 54, A, D)
    legs(c, 44, 54, gap=3, w=4)
    torso(c, 28, 45, 20, 20, fill=A)
    shirt_v(c, 28, 4, 3, tie=None)
    # blank banner held high
    c.vline(36, 2, 34, D)
    c.vline(52, 2, 30, D)
    c.rect(34, 3, 54, 16, L, D)
    c.line(35, 17, 40, 15, D)
    arm(c, 35, 30, 37, 22, 4, fill=A)
    hand(c, 37, 21)
    # megaphone
    arm(c, 20, 30, 15, 26, 4, fill=A)
    c.poly([(16, 23), (5, 16), (5, 30), (16, 27)], L, D)
    c.vline(7, 18, 28, A)
    hand(c, 16, 26)
    head(c, 17, 12, 11)
    hair(c, 17, 12, 11, "messy")
    c.rect(16, 10, 39, 12, L, D)
    eyes(c, 17, 5, "dot", brow="angry", brow_dy=-2)
    mouth(c, 22, "open")
    return finish(c, 54)


def p17_don_subvencion():
    c = new()
    # giant envelope
    c.rect(3, 37, 52, 54, L, D)
    c.line(3, 37, 27, 48, D)
    c.line(52, 37, 28, 48, D)
    c.ellipse(27.5, 47, 3, 2, A, D)
    c.stamp("3333\n3..3\n3333", 41, 49)
    # man sitting on it
    c.rect(17, 34, 24, 44, D, D)
    c.rect(31, 34, 38, 44, D, D)
    c.ellipse(20.5, 44, 3, 1, D, D)
    c.ellipse(34.5, 44, 3, 1, D, D)
    c.ellipse(27.5, 30, 12, 9, L, D)
    c.poly([(16, 22), (39, 22), (40, 34), (15, 34)], A, D)
    c.ellipse(27.5, 31, 6, 5, L, D)
    for y in (24, 28, 32):
        c.set(22, y, D)
        c.set(33, y, D)
    arm(c, 16, 24, 10, 32, 4, fill=L)
    hand(c, 10, 33)
    arm(c, 39, 24, 45, 32, 4, fill=L)
    hand(c, 45, 33)
    head(c, 12, 12, 10)
    hair(c, 12, 12, 10, "balding")
    eyes(c, 12, 5, "happy")
    mouth(c, 17, "smile")
    blush(c, 16, 8)
    # unlit cigar
    c.rect(32, 17, 41, 19, D, D)
    c.hline(33, 39, 18, A)
    c.set(40, 18, L)
    return finish(c, 54)


def p18_concejal_aldea():
    c = new()
    legs(c, 43, 51, gap=3, w=4)
    torso(c, 29, 44, 18, 19, fill=A)
    for y in range(32, 44, 3):
        c.set(27, y, L)
    # cane
    c.vline(42, 31, 51, D)
    c.hline(39, 42, 30, D)
    c.set(39, 31, D)
    arm(c, 35, 31, 41, 36, 4, fill=A)
    hand(c, 42, 36)
    # closed umbrella under the arm
    c.line(8, 26, 22, 46, D)
    c.line(9, 26, 23, 46, D)
    c.poly([(7, 28), (12, 26), (21, 40), (18, 42)], A, D)
    c.stamp("33\n3.\n3.", 6, 23)
    arm(c, 20, 31, 15, 38, 4, fill=A)
    hand(c, 15, 38)
    head(c, 18, 11, 10)
    eyes(c, 18, 4, "happy", brow="flat", brow_dy=-2)
    c.set(19, 19, D)
    c.set(35, 19, D)
    c.stamp(".333333.\n33333333\n.3....3.", 23, 21)
    mouth(c, 25, "small", cx=27.5)
    # beret
    c.ellipse(26.5, 9, 12, 4, D, D)
    c.vline(27, 3, 5, D)
    c.hline(18, 22, 8, L)
    fog_band(c, 45)
    return finish(c, 54)


def p19_cacique():
    c = new()
    # mini horreo behind
    c.rect(38, 15, 54, 25, A, D)
    for x in range(40, 54, 3):
        c.vline(x, 16, 24, D)
    c.poly([(36, 15), (46, 8), (56, 15)], D, D)
    c.set(46, 6, D)
    c.set(46, 7, D)
    for x in (39, 52):
        c.vline(x, 26, 34, D)
        c.vline(x + 1, 26, 34, D)
        c.rect(x - 1, 26, x + 2, 27, L, D)
    # trench coat to the ground
    legs(c, 47, 54, gap=4, w=4)
    c.poly([(17, 27), (11, 50), (44, 50), (38, 27)], A, D)
    c.vline(27, 30, 50, D)
    c.line(27, 30, 22, 37, D)
    c.line(28, 30, 33, 37, D)
    shirt_v(c, 27, 5, 3, tie=D, tie_len=4)
    c.hline(13, 42, 40, D)
    for y in (33, 37, 44):
        c.set(25, y, L)
    # hand up showing the big ring
    arm(c, 36, 30, 40, 38, 5, fill=A)
    arm(c, 19, 30, 13, 22, 5, fill=A)
    hand(c, 12, 20, 3)
    c.rect(13, 21, 15, 23, L, D)
    c.set(14, 22, A)
    c.stamp("..2..\n.....\n2...2", 11, 14)
    head(c, 16, 12, 11)
    eyes(c, 16, 5, "half", brow="thick", brow_dy=-2)
    c.stamp("33333333\n3......3", 24, 21)
    mouth(c, 23, "smirk")
    c.ellipse(27.5, 7, 13, 4, D, D)
    c.vline(28, 1, 3, D)
    c.hline(19, 24, 6, L)
    return finish(c, 55)


def p20_retranqueiro():
    c = new()
    torso(c, 29, 50, 22, 24, fill=A)
    shirt_v(c, 29, 4, 3, tie=None)
    # shrug: shoulders up, palms out
    arm(c, 18, 31, 9, 28, 5, fill=A)
    arm(c, 37, 31, 46, 28, 5, fill=A)
    hand(c, 7, 25, 3)
    hand(c, 48, 25, 3)
    c.stamp("3.3\n...", 5, 21)
    c.stamp("3.3\n...", 47, 21)
    head(c, 18, 12, 11)
    hair(c, 18, 12, 11, "short")
    eyes(c, 18, 5, "half", brow="raised", brow_dy=-2)
    mouth(c, 24, "wavy")
    c.stamp(rows("33.\n..3\n.3.\n...\n.3."), 44, 6)
    # hidden in the fog
    fog_band(c, 36)
    fog_band(c, 44)
    c.rect(0, 50, 55, 54, L, None)
    c.hline(0, 55, 54, A)
    c.ellipse(9, 9, 6, 2, L, A)
    c.ellipse(13, 15, 4, 1, L, A)
    return finish(c, 54)


def p21_notario():
    c = new()
    # toga
    c.poly([(17, 27), (12, 54), (43, 54), (38, 27)], D, D)
    c.vline(27, 32, 54, L)
    c.poly([(21, 27), (27, 35), (28, 35), (34, 27)], L, D)
    c.stamp("3333\n3113\n.33.", 26, 28)
    # bundled files under the arm
    c.rect(4, 35, 19, 47, L, D)
    c.hline(5, 18, 38, D)
    c.hline(5, 18, 41, D)
    c.hline(5, 18, 44, D)
    c.vline(11, 35, 47, A)
    c.vline(12, 35, 47, A)
    c.stamp("2.2\n.2.", 10, 32)
    arm(c, 19, 30, 15, 38, 5)
    c.rect(13, 38, 18, 40, L, D)
    # quill
    arm(c, 36, 30, 41, 36, 5)
    c.rect(39, 36, 44, 38, L, D)
    hand(c, 42, 38)
    c.line(42, 37, 49, 22, D)
    c.poly([(47, 26), (52, 15), (53, 20), (49, 28)], L, D)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "balding", color=L, shine=False)
    c.stamp("3333\n3..3", 18, 9)
    eyes(c, 15, 5, "small", brow="flat", brow_dy=-2)
    # half-moon glasses
    c.stamp("3....3\n.3333.", 19, 17)
    c.stamp("3....3\n.3333.", 31, 17)
    c.hline(25, 30, 17, D)
    c.stamp("..3333..\n.311113.\n.311113.\n..3113..\n...33...", 24, 20)
    return finish(c, 54)


def p22_silenciosa():
    c = new()
    # mountain of files
    for i, (x, w, top) in enumerate(((1, 15, 6), (40, 15, 2), (12, 10, 20), (36, 9, 18))):
        for y in range(top, 54, 3):
            off = ((y // 3) % 2) * 2 - 1
            c.rect(x + off, y, x + w + off, y + 2, L if (y // 3) % 3 else A, D)
    legs(c, 44, 53, gap=3, w=3)
    torso(c, 28, 45, 17, 20, fill=A)
    for y in range(31, 44, 3):
        c.set(27, y, L)
        c.set(28, y, D)
    shirt_v(c, 28, 4, 2, tie=None)
    arm(c, 21, 30, 18, 40, 4, fill=A)
    hand(c, 18, 41)
    # finger on the lips
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "bob")
    bun(c, 16, 11, r=4)
    eyes(c, 16, 5, "line", brow="flat", brow_dy=-3)
    mouth(c, 22, "flat")
    arm(c, 36, 31, 31, 28, 5, fill=A)
    c.ellipse(28.5, 27, 3, 2, L, D)
    c.rect(27, 18, 29, 25, L, D)
    c.set(28, 18, L)
    return finish(c, 54)


def p23_mariscador():
    c = new()
    # water drops
    for (x, y) in ((3, 6), (48, 9), (6, 30), (50, 32), (40, 2), (14, 14)):
        c.stamp(DROP, x, y)
    # rubber boots
    c.rect(19, 42, 25, 53, A, D)
    c.rect(30, 42, 36, 53, A, D)
    c.hline(19, 25, 44, D)
    c.hline(30, 36, 44, D)
    torso(c, 28, 44, 19, 21, fill=L)
    c.rect(21, 32, 34, 44, A, D)
    c.vline(22, 28, 32, A)
    c.vline(33, 28, 32, A)
    # shellfish rake
    c.vline(45, 6, 52, D)
    c.vline(46, 6, 52, L)
    c.rect(40, 50, 51, 52, D, D)
    for x in range(40, 52, 2):
        c.set(x, 53, D)
        c.set(x, 54, D)
    arm(c, 35, 30, 44, 34, 4, fill=L)
    hand(c, 45, 34)
    # sign
    c.vline(9, 10, 40, D)
    c.rect(2, 8, 18, 20, L, D)
    c.rect(4, 10, 16, 18, A, None)
    c.hline(5, 15, 12, L)
    c.hline(5, 12, 15, L)
    arm(c, 20, 30, 11, 30, 4, fill=L)
    hand(c, 10, 30)
    head(c, 16, 12, 11)
    eyes(c, 16, 5, "dot", brow="angry", brow_dy=-2)
    mouth(c, 21, "talk")
    # fisherman cap
    c.ellipse(27.5, 8, 12, 5, A, D)
    c.rect(15, 8, 40, 10, A, D)
    c.hline(14, 41, 11, D)
    return finish(c, 54)


def p24_abogado():
    c = new()
    # endless roll of paper trailing on the floor
    c.poly([(38, 22), (45, 22), (44, 47), (55, 50), (55, 54), (22, 54), (24, 50), (37, 48)], L, D)
    for y in range(26, 47, 3):
        c.hline(39, 43, y, D)
    for x in range(26, 54, 4):
        c.vline(x, 51, 52, D)
    c.ellipse(41.5, 21, 4, 2, L, D)
    c.poly([(16, 27), (10, 53), (36, 53), (39, 27)], D, D)
    c.poly([(21, 27), (27, 34), (28, 34), (34, 27)], L, D)
    c.vline(27, 34, 53, L)
    arm(c, 36, 30, 40, 22, 5)
    c.rect(37, 19, 42, 22, L, D)
    hand(c, 39, 21)
    arm(c, 19, 30, 12, 38, 5)
    c.rect(9, 37, 14, 40, L, D)
    hand(c, 11, 40)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "side")
    c.rect(18, 14, 25, 18, None, D)
    c.rect(30, 14, 37, 18, None, D)
    c.hline(25, 30, 15, D)
    eyes(c, 16, 4, "small", brow="angry", brow_dy=-3)
    mouth(c, 22, "grin")
    return finish(c, 54)


def p25_eterno():
    c = new()
    # floating posters with his own face
    for (x, y) in ((0, 4), (44, 0), (46, 30)):
        c.rect(x, y, x + 10, y + 13, L, D)
        c.ellipse(x + 5, y + 5, 3, 3, L, D)
        c.rect(x + 3, y + 2, x + 7, y + 3, D, D)
        c.hline(x + 4, x + 6, y + 7, D)
        c.rect(x + 1, y + 10, x + 9, y + 12, A, D)
    legs(c, 46, 55, gap=4, w=5)
    torso(c, 26, 47, 30, 26, fill=A)
    shirt_v(c, 26, 7, 4, tie=D, tie_len=6)
    lapels(c, 26, 7, 4)
    # white sash across the chest
    c.poly([(15, 28), (19, 27), (40, 46), (35, 47)], L, D)
    c.ellipse(36, 42, 2, 2, A, D)
    arm(c, 15, 30, 9, 43, 6, fill=A)
    hand(c, 9, 44, 3)
    arm(c, 40, 30, 46, 22, 6, fill=A)
    hand(c, 47, 20, 3)
    c.set(47, 16, D)
    c.rect(46, 15, 48, 18, L, D)
    head(c, 14, 13, 12)
    hair(c, 14, 13, 12, "slick")
    c.ellipse(21, 3, 7, 2, D, D)
    eyes(c, 14, 5, "happy", brow="up", brow_dy=-2)
    mouth(c, 20, "poster")
    c.stamp("2\n.", 39, 6)
    return finish(c, 55)


ALL = {
    1: p01_becario, 2: p02_asesor, 3: p03_jefe, 4: p04_tertulianete, 5: p05_todologo,
    6: p06_infografia, 7: p07_puertagiratoria, 8: p08_trending, 9: p09_ventanilla,
    10: p10_pregonero, 11: p11_alcalde, 12: p12_baron_sur, 13: p13_pico_oro, 14: p14_mitinera,
    15: p15_jornalero, 16: p16_lider_marea, 17: p17_don_subvencion, 18: p18_concejal_aldea,
    19: p19_cacique, 20: p20_retranqueiro, 21: p21_notario, 22: p22_silenciosa,
    23: p23_mariscador, 24: p24_abogado, 25: p25_eterno,
}
