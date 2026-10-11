"""Battle sprites for rivals, gym leaders, player and mentor (56x56) + player back (32x32)."""
from .canvas import Canvas
from .chibi import (new, eyes, mouth, blush, head, hair, bun, torso, shirt_v, lapels, legs,
                    hand, arm, finish, L, A, D, CX)

PIN = "333\n313\n333"


def player():
    c = new()
    legs(c, 44, 53, gap=3, w=4)
    torso(c, 28, 45, 18, 19, fill=L)
    # neutral party vest
    c.poly([(19, 28), (17, 45), (25, 45), (25, 33)], A, D)
    c.poly([(36, 28), (38, 45), (30, 45), (30, 33)], A, D)
    c.stamp(PIN, 20, 36)
    arm(c, 20, 31, 15, 40, 4, fill=L)
    hand(c, 15, 41)
    arm(c, 35, 31, 42, 25, 4, fill=L)
    hand(c, 43, 23, 3)
    c.rect(42, 15, 45, 21, L, D)
    c.hline(42, 45, 17, A)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "bangs")
    eyes(c, 16, 5, "big", brow="up", brow_dy=-2)
    mouth(c, 22, "smile")
    return finish(c, 54)


def player_back():
    c = Canvas(32, 32)
    c.poly([(7, 19), (5, 31), (26, 31), (24, 19)], L, D)
    c.poly([(7, 19), (5, 31), (15, 31), (15, 19)], A, D)
    c.poly([(16, 19), (16, 31), (26, 31), (24, 19)], A, D)
    c.ellipse(4, 24, 3, 5, L, D)
    c.ellipse(27, 18, 3, 5, L, D)
    c.ellipse(28, 12, 2, 2, L, D)
    c.ellipse(5.5, 12, 2, 2, L, D)
    c.ellipse(25.5, 12, 2, 2, L, D)
    c.ellipse(15.5, 11, 10, 9, L, D)
    m = c.ellipse_mask(15.5, 11, 10, 9)
    c.paint_mask({(x, y) for (x, y) in m if y <= 16}, D, D)
    c.hline(11, 15, 4, L)
    return c


def mentor():
    c = new()
    legs(c, 45, 53, gap=3, w=3, color=L)
    c.poly([(16, 28), (13, 46), (42, 46), (39, 28)], L, D)
    c.vline(27, 34, 46, D)
    shirt_v(c, 28, 6, 3, tie=A, tie_len=4)
    # ballot box under the arm
    c.rect(33, 32, 50, 46, L, D)
    c.rect(36, 34, 47, 35, D, D)
    c.rect(35, 38, 48, 44, A, D)
    c.rect(39, 30, 43, 34, L, D)
    c.hline(40, 42, 31, D)
    arm(c, 36, 30, 38, 40, 4, fill=L)
    hand(c, 37, 42)
    arm(c, 19, 30, 14, 39, 4, fill=L)
    hand(c, 14, 40)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "bob", color=A, shine=False)
    bun(c, 16, 11, r=4)
    c.rect(18, 14, 25, 18, None, D)
    c.rect(30, 14, 37, 18, None, D)
    c.hline(25, 30, 15, D)
    eyes(c, 16, 4, "small")
    mouth(c, 22, "smile")
    return finish(c, 54)


def rival(style):
    c = new()
    legs(c, 44, 53, gap=3, w=4)
    if style == "ppi":
        torso(c, 28, 45, 18, 19, fill=A)
        c.rect(25, 28, 30, 31, L, D)
        c.stamp(PIN, 31, 34)
        arm(c, 20, 31, 15, 40, 4, fill=A)
        hand(c, 15, 41)
        arm(c, 35, 30, 41, 20, 4, fill=A)
        c.ellipse(42, 17, 3, 3, L, D)
        c.hline(40, 44, 16, D)
        head(c, 16, 12, 11)
        hair(c, 16, 12, 11, "side")
        eyes(c, 16, 5, "big", brow="angry", brow_dy=-2)
        mouth(c, 22, "open")
    elif style == "asc":
        c.poly([(20, 38), (35, 38), (37, 46), (18, 46)], A, D)
        torso(c, 28, 39, 17, 18, fill=A)
        shirt_v(c, 28, 6, 3, tie=None)
        lapels(c, 28, 6, 3)
        # clipboard
        c.rect(32, 30, 43, 44, L, D)
        c.rect(35, 28, 40, 30, D, D)
        for y in (33, 36, 39):
            c.hline(34, 41, y, D)
        arm(c, 35, 31, 38, 40, 4, fill=A)
        hand(c, 37, 41)
        arm(c, 20, 31, 16, 40, 4, fill=A)
        hand(c, 16, 41)
        head(c, 16, 12, 11)
        hair(c, 16, 12, 11, "long")
        eyes(c, 16, 5, "half", brow="flat", brow_dy=-2)
        mouth(c, 22, "smirk")
    elif style == "myt":
        torso(c, 28, 45, 20, 21, fill=A)
        c.rect(22, 38, 33, 42, A, D)
        c.vline(25, 28, 34, L)
        c.vline(30, 28, 34, L)
        arm(c, 20, 31, 14, 26, 4, fill=A)
        c.poly([(15, 24), (5, 17), (5, 31), (15, 27)], L, D)
        hand(c, 15, 26)
        arm(c, 35, 31, 40, 40, 4, fill=A)
        hand(c, 40, 41)
        head(c, 16, 12, 11)
        eyes(c, 17, 5, "big", brow="up", brow_dy=-2)
        mouth(c, 23, "grin")
        c.ellipse(27.5, 9, 12, 5, A, D)
        c.rect(15, 9, 40, 11, A, D)
        c.rect(30, 11, 46, 13, D, D)
    elif style == "pas":
        torso(c, 28, 45, 19, 20, fill=A)
        shirt_v(c, 28, 5, 3, tie=None)
        c.vline(27, 33, 45, D)
        # tablet
        c.rect(30, 33, 42, 42, D, D)
        c.rect(32, 35, 40, 40, L, None)
        c.hline(33, 38, 37, A)
        arm(c, 35, 31, 36, 40, 4, fill=A)
        hand(c, 36, 41)
        arm(c, 20, 31, 15, 40, 4, fill=A)
        hand(c, 15, 41)
        head(c, 16, 12, 11)
        hair(c, 16, 12, 11, "short")
        c.rect(18, 14, 25, 18, None, D)
        c.rect(30, 14, 37, 18, None, D)
        c.hline(25, 30, 15, D)
        eyes(c, 16, 4, "small")
        mouth(c, 22, "smile")
    return finish(c, 54)


def leader_madrid():
    c = new()
    # studio spotlight rays
    for x0 in (2, 50):
        c.line(x0, 0, 27, 40, A)
    c.stamp("..2..\n.222.\n22322\n.222.\n..2..", 4, 26)
    c.stamp("..2..\n.222.\n22322\n.222.\n..2..", 46, 34)
    legs(c, 45, 54, gap=3, w=3, color=L)
    c.poly([(19, 36), (36, 36), (39, 47), (16, 47)], A, D)
    torso(c, 27, 37, 18, 16, fill=A)
    shirt_v(c, 27, 5, 3, tie=None)
    lapels(c, 27, 5, 3)
    # hand mic with cube
    arm(c, 35, 30, 39, 24, 4, fill=A)
    hand(c, 39, 23)
    c.rect(38, 15, 40, 21, D, D)
    c.ellipse(39, 13, 2, 2, D, D)
    c.rect(37, 17, 41, 19, L, D)
    arm(c, 20, 30, 14, 36, 4, fill=A)
    hand(c, 13, 37)
    # voluminous hair
    c.ellipse(27.5, 15, 16, 13, D, D)
    head(c, 17, 12, 11, ears=False)
    hair(c, 17, 12, 11, "bangs")
    c.hline(15, 20, 6, L)
    eyes(c, 17, 5, "big", brow="up", brow_dy=-2)
    mouth(c, 23, "grin")
    blush(c, 21, 8)
    return finish(c, 54)


def leader_andalucia():
    c = new()
    c.poly([(16, 26), (4, 54), (51, 54), (39, 26)], A, D)
    legs(c, 46, 54, gap=4, w=5)
    torso(c, 26, 47, 26, 28)
    shirt_v(c, 26, 7, 4, tie=A, tie_len=5)
    lapels(c, 26, 7, 4)
    c.ellipse(34, 30, 2, 2, A, D)
    c.vline(9, 4, 54, D)
    c.vline(10, 4, 54, L)
    c.ellipse(9.5, 4, 3, 3, L, D)
    c.stamp("3.3\n3.3\n.3.", 8, 8)
    arm(c, 17, 30, 11, 34, 5)
    hand(c, 10, 34, 3)
    arm(c, 38, 30, 42, 42, 5)
    hand(c, 42, 43, 3)
    head(c, 16, 13, 12)
    hair(c, 16, 13, 12, "slick", color=L, shine=False)
    c.hline(19, 36, 6, D)
    c.rect(14, 15, 15, 21, L, D)
    c.rect(40, 15, 41, 21, L, D)
    eyes(c, 16, 5, "half", brow="thick", brow_dy=-2)
    c.stamp("..3333..3333..\n.33333333333.\n33........33", 21, 21)
    mouth(c, 24, "small")
    return finish(c, 55)


def leader_galicia():
    c = new()
    # mist behind
    for (x, y) in ((6, 10), (49, 16), (10, 30), (47, 4)):
        c.ellipse(x, y, 5, 2, L, A)
    legs(c, 46, 54, gap=3, w=3, color=L)
    c.poly([(18, 34), (37, 34), (40, 48), (15, 48)], D, D)
    torso(c, 28, 36, 18, 18, fill=D)
    # knitted shawl
    c.poly([(14, 27), (41, 27), (35, 38), (27, 42), (20, 38)], A, D)
    for x in range(18, 38, 3):
        c.set(x, 30, L)
    c.set(27, 41, L)
    # closed umbrella used as a cane
    c.vline(44, 22, 54, D)
    c.poly([(42, 30), (46, 30), (46, 48), (42, 48)], A, D)
    c.stamp("33\n.3", 43, 20)
    arm(c, 36, 31, 43, 33, 4, fill=A)
    hand(c, 44, 33)
    arm(c, 19, 31, 16, 41, 4, fill=A)
    hand(c, 16, 42)
    head(c, 16, 12, 11)
    hair(c, 16, 12, 11, "long")
    bun(c, 16, 11, r=5)
    eyes(c, 16, 5, "half", brow="flat", brow_dy=-2)
    mouth(c, 22, "flat")
    return finish(c, 55)


ALL = {
    "player": (player, "player"),
    "mentor": (mentor, "mentor"),
    "rival_ppi": (lambda: rival("ppi"), "party_ppi"),
    "rival_asc": (lambda: rival("asc"), "party_asc"),
    "rival_myt": (lambda: rival("myt"), "party_myt"),
    "rival_pas": (lambda: rival("pas"), "party_pas"),
    "leader_madrid": (leader_madrid, "leader_madrid"),
    "leader_andalucia": (leader_andalucia, "leader_andalucia"),
    "leader_galicia": (leader_galicia, "leader_galicia"),
}
