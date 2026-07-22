"""Chroma-key magenta background out of generated platform sprites, despill the
pink fringe, autocrop tight, and write into the project's sprite slots."""
import os
from PIL import Image

SRC = os.path.expanduser(
    "~/.cursor/projects/Users-tungld-Documents-Program-Game-AssignmentPRU/assets")
DST = os.path.join(os.path.dirname(__file__),
                   "..", "Assets", "Art", "Sprites", "Environment")
DST = os.path.abspath(DST)


def key_and_crop(src_name):
    img = Image.open(os.path.join(SRC, src_name)).convert("RGBA")
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # "magentaness": both R and B high while G is low
            m = min(r, b) - g
            if m > 120:
                px[x, y] = (r, g, b, 0)
            elif m > 40:
                # feather edge + despill
                alpha = int(255 * (120 - m) / 80)
                spill = m
                nr = max(0, r - spill)
                nb = max(0, b - spill)
                px[x, y] = (nr, g, nb, alpha)
            else:
                # opaque, but neutralise any slight magenta spill
                if m > 0:
                    px[x, y] = (max(0, r - m), g, max(0, b - m), 255)
                else:
                    px[x, y] = (r, g, b, 255)
    # autocrop to non-transparent bounds
    bbox = img.getbbox()
    if bbox:
        img = img.crop(bbox)
    return img


def save(img, name):
    img.save(os.path.join(DST, name))
    print("wrote", name, img.size)


def main():
    cloud = key_and_crop("plat_cloud_src.png")
    for n in ("cloud_platform_s.png", "cloud_platform_m.png",
              "cloud_platform_l.png", "cloud_platform_xl.png",
              "platform_cloud.png"):
        save(cloud, n)

    save(key_and_crop("plat_grass_src.png"), "platform_grass.png")
    save(key_and_crop("plat_stone_src.png"), "platform_stone.png")
    save(key_and_crop("plat_ice_src.png"), "platform_ice.png")

    rock = key_and_crop("plat_rock_src.png")
    for n in ("rock_volcanic_l.png", "rock_volcanic_m.png",
              "rock_volcanic_s.png", "platform_volcanic.png"):
        save(rock, n)


if __name__ == "__main__":
    main()
