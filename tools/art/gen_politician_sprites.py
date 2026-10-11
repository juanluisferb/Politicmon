"""Writes front/back/icon PNGs for every politician + contact sheets."""
import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import pixlib
from art import politicians_art as pa
from art import small_art as sa


def main():
    pals = pixlib.load_palettes()["sprites"]
    pol = json.load(open(os.path.join(pixlib.ROOT, "data", "politicians.json"), encoding="utf-8"))
    fronts, backs, icons, labels = [], [], [], []
    for pid in sorted(pa.ALL):
        info = pol[str(pid)]
        pal = pals[info["palette"]]
        base = "%02d_%s" % (pid, info["key"])
        f = pixlib.grid_to_image(pa.ALL[pid]().px, pal)
        b = pixlib.grid_to_image(sa.back_sprite(pid).px, pal)
        i = pixlib.grid_to_image(sa.icon(pid).px, pal)
        pixlib.save(f, "assets", "sprites", "politicians", base + "_front.png")
        pixlib.save(b, "assets", "sprites", "politicians", base + "_back.png")
        pixlib.save(i, "assets", "sprites", "politicians", base + "_icon.png")
        fronts.append(f)
        backs.append(b)
        icons.append(i)
        labels.append("%d %s" % (pid, info["key"]))
    os.makedirs(os.path.join(pixlib.ROOT, "docs", "contact_sheets"), exist_ok=True)
    pixlib.contact_sheet(fronts, 5, (56, 56), scale=3, labels=labels).save(
        os.path.join(pixlib.ROOT, "docs", "contact_sheets", "politicians_front.png"))
    pixlib.contact_sheet(backs, 5, (32, 32), scale=4, labels=labels).save(
        os.path.join(pixlib.ROOT, "docs", "contact_sheets", "politicians_back.png"))
    pixlib.contact_sheet(icons, 5, (16, 16), scale=6, labels=None).save(
        os.path.join(pixlib.ROOT, "docs", "contact_sheets", "politicians_icon.png"))
    print("politician sprites:", len(fronts))


if __name__ == "__main__":
    main()
