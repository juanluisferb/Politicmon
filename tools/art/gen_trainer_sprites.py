import os, sys
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import pixlib
from art import trainers_art as ta


def main():
    pals = pixlib.load_palettes()["sprites"]
    imgs, labels = [], []
    for key, (fn, pal) in ta.ALL.items():
        img = pixlib.grid_to_image(fn().px, pals[pal])
        pixlib.save(img, "assets", "sprites", "trainers", key + ".png")
        imgs.append(img)
        labels.append(key)
    back = pixlib.grid_to_image(ta.player_back().px, pals["player"])
    pixlib.save(back, "assets", "sprites", "trainers", "player_back.png")
    imgs.append(back)
    labels.append("player_back")
    pixlib.contact_sheet(imgs, 5, (56, 56), scale=3, labels=labels).save(
        os.path.join(pixlib.ROOT, "docs", "contact_sheets", "trainers.png"))
    print("trainer sprites:", len(imgs))


if __name__ == "__main__":
    main()
