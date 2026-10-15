#!/usr/bin/env python3
"""Generates the mod's pixel-art icons (Railgunner).

Each icon is drawn on a 16x16 grid (the game's item-art resolution), given a dark outline, and scaled 4x
(nearest neighbour) to 64x64. Run from the repo root:

    python3 art/make_icons.py

Writes PNGs into src/SoRCharacters/Resources/ and a labelled preview sheet
into art/preview.png.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SIZE = 16
SCALE = 4
OUTLINE = (24, 22, 28, 255)
OUT_DIR = Path("src/SoRCharacters/Resources")
PREVIEW = Path("art/preview.png")


def canvas():
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def outline(img):
    """Paint every transparent pixel that touches a filled pixel."""
    src = img.copy()
    px, out = src.load(), img.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if px[x, y][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < SIZE and 0 <= ny < SIZE and px[nx, ny][3] and px[nx, ny] != OUTLINE:
                    out[x, y] = OUTLINE
                    break
    return img


def shade(img, region, light, dark, base):
    """Recolour `base` pixels inside region: top-left edge light, bottom-right edge dark."""
    src = img.copy().load()
    px = img.load()
    x0, y0, x1, y1 = region
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if src[x, y] != base:
                continue
            up = src[x, y - 1] if y > 0 else (0, 0, 0, 0)
            left = src[x - 1, y] if x > 0 else (0, 0, 0, 0)
            down = src[x, y + 1] if y < SIZE - 1 else (0, 0, 0, 0)
            right = src[x + 1, y] if x < SIZE - 1 else (0, 0, 0, 0)
            if up != base or left != base:
                if up[3] == 0 or left[3] == 0 or up == OUTLINE or left == OUTLINE:
                    px[x, y] = light
            elif down[3] == 0 or right[3] == 0:
                px[x, y] = dark


def railshot():
    img, d = canvas()
    steel, hi, lo = (88, 92, 104, 255), (146, 152, 168, 255), (52, 55, 66, 255)
    cyan, cyan_hi, cyan_lo = (60, 220, 240, 255), (200, 255, 255, 255), (20, 130, 170, 255)
    d.rectangle((1, 5, 5, 9), fill=steel)
    d.rectangle((2, 10, 3, 13), fill=steel)
    d.rectangle((6, 6, 13, 9), fill=steel)
    shade(img, (0, 0, SIZE - 1, SIZE - 1), hi, lo, steel)
    d.rectangle((7, 7, 14, 8), fill=cyan)
    d.line((7, 8, 14, 8), fill=cyan_lo)
    d.line((8, 7, 13, 7), fill=cyan_hi)
    for x in (8, 11):
        d.line((x, 5, x, 10), fill=lo)
        d.point((x, 5), fill=cyan)
        d.point((x, 10), fill=cyan_lo)
        d.point((x, 7), fill=cyan_hi)
        d.point((x, 8), fill=cyan)
    d.rectangle((3, 6, 4, 7), fill=cyan_lo)
    d.point((3, 6), fill=cyan)
    d.point((14, 7), fill=cyan_hi)
    return outline(img)


def railgun_held():
    """The railgun as the game draws a held gun: side-on, pointing right (+x), centred in the frame."""
    img, d = canvas()
    steel, hi, lo = (88, 92, 104, 255), (146, 152, 168, 255), (52, 55, 66, 255)
    cyan, cyan_hi, cyan_lo = (60, 220, 240, 255), (200, 255, 255, 255), (20, 130, 170, 255)
    d.rectangle((1, 7, 2, 9), fill=steel)
    d.rectangle((3, 6, 8, 9), fill=steel)
    d.rectangle((5, 10, 6, 11), fill=steel)
    d.rectangle((9, 6, 14, 6), fill=steel)
    d.rectangle((9, 9, 14, 9), fill=steel)
    shade(img, (0, 0, SIZE - 1, SIZE - 1), hi, lo, steel)
    d.line((9, 7, 14, 7), fill=cyan_hi)
    d.line((9, 8, 14, 8), fill=cyan)
    for x in (10, 13):
        d.line((x, 5, x, 10), fill=lo)
        d.point((x, 5), fill=cyan)
        d.point((x, 10), fill=cyan_lo)
    d.rectangle((4, 7, 5, 7), fill=cyan_lo)
    d.point((4, 7), fill=cyan)
    d.point((14, 7), fill=(255, 255, 255, 255))
    return outline(img)


ICONS = {
    "railshot.png": ("Railshot", railshot),
    "railgun_held.png": ("Held railgun", railgun_held),
}


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    big = []
    for file, (label, draw) in ICONS.items():
        img = draw()
        img.resize((SIZE * SCALE, SIZE * SCALE), Image.NEAREST).save(OUT_DIR / file)
        big.append((label, img.resize((SIZE * 12, SIZE * 12), Image.NEAREST)))
    cell = SIZE * 12 + 24
    sheet = Image.new("RGBA", (cell * len(big), cell + 30), (58, 62, 70, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for i, (label, img) in enumerate(big):
        sheet.paste(img, (i * cell + 12, 12), img)
        draw.text((i * cell + 12, cell + 4), label, fill=(240, 240, 240, 255), font=font)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(PREVIEW)
    print(f"wrote {len(big)} icons to {OUT_DIR} and preview to {PREVIEW}")


if __name__ == "__main__":
    main()
