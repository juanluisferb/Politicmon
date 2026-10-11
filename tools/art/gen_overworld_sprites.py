import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import pixlib
from art import overworld_art as oa


def main():
    pals = pixlib.load_palettes()["sprites"]
    imgs, labels = [], []
    for name in oa.CHARACTERS:
        c, pal = oa.sheet(name)
        img = pixlib.grid_to_image(c.px, pals[pal])
        pixlib.save(img, "assets", "sprites", "overworld", name + ".png")
        imgs.append(img)
        labels.append(name)
    pixlib.contact_sheet(imgs, 9, (32, 64), scale=3, labels=labels).save(
        os.path.join(pixlib.ROOT, "docs", "contact_sheets", "overworld.png"))
    print("overworld sheets:", len(imgs))


if __name__ == "__main__":
    main()
