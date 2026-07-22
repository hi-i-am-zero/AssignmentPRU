"""Make the generated lava texture horizontally tileable and write it into the
project's lava_tile slot (used by the scrolling LavaRiver)."""
import os
from PIL import Image

SRC = os.path.expanduser(
    "~/.cursor/projects/Users-tungld-Documents-Program-Game-AssignmentPRU/assets")
DST = os.path.join(os.path.dirname(__file__),
                   "..", "Assets", "Art", "Sprites", "Environment")
DST = os.path.abspath(DST)


def make_h_tileable(im, blend=80):
    im = im.convert("RGB")
    w, h = im.size
    left = im.crop((0, 0, blend, h)).load()
    right = im.crop((w - blend, 0, w, h))
    pr = right.load()
    for x in range(blend):
        t = x / (blend - 1)  # 0 at strip start, 1 at far right edge
        for y in range(h):
            rr, rg, rb = pr[x, y]
            lr, lg, lb = left[x, y]
            pr[x, y] = (int(rr * (1 - t) + lr * t),
                        int(rg * (1 - t) + lg * t),
                        int(rb * (1 - t) + lb * t))
    im.paste(right, (w - blend, 0))
    return im


def main():
    im = Image.open(os.path.join(SRC, "lava_src.png")).resize((512, 512))
    im = make_h_tileable(im)
    out = os.path.join(DST, "lava_tile.png")
    im.save(out)
    print("wrote", out, im.size)


if __name__ == "__main__":
    main()
