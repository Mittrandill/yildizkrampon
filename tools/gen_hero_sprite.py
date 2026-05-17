"""
Stardew Valley style top-down pixel art character sprite.
Front-facing (looking toward viewer/down), 48x64 px, RGBA.
"""
from PIL import Image, ImageDraw

W, H = 48, 64

# Palette
TRANS   = (0,   0,   0,   0)
OUTLINE = (30,  20,  10,  255)
SKIN    = (255, 198, 152, 255)
SKIN_D  = (230, 165, 115, 255)
HAIR    = (50,  30,  15,  255)
HAIR_H  = (80,  50,  25,  255)
WHITE   = (240, 240, 240, 255)
EYE     = (55,  35,  15,  255)
PUPIL   = (20,  10,   5,  255)
BLUSH   = (255, 180, 155, 255)
HOODIE  = (72,  148, 72,  255)
HOODIE_D= (50,  110, 50,  255)
HOODIE_L= (100, 180, 100, 255)
STRIPE  = (60,  130, 60,  255)
JEANS   = (65,  105, 180, 255)
JEANS_D = (45,   80, 145, 255)
JEANS_L = (90,  130, 200, 255)
SHOE    = (245, 245, 245, 255)
SHOE_D  = (180, 180, 180, 255)
SOLE    = (50,  50,  50,  255)
MOUTH   = (200, 120, 100, 255)

img = Image.new("RGBA", (W, H), TRANS)
px  = img.load()

def rect(x0, y0, x1, y1, color):
    for y in range(y0, y1+1):
        for x in range(x0, x1+1):
            if 0 <= x < W and 0 <= y < H:
                px[x, y] = color

def hline(y, x0, x1, color):
    for x in range(x0, x1+1):
        if 0 <= x < W and 0 <= y < H:
            px[x, y] = color

def vline(x, y0, y1, color):
    for y in range(y0, y1+1):
        if 0 <= x < W and 0 <= y < H:
            px[x, y] = color

# ── HAIR (top of head) ──────────────────────────────────────────
# Full hair block first, then cut face out below
rect(12, 1,  35, 18, HAIR)
rect(14, 0,  33,  0, HAIR)       # very top
rect(13, 1,  34,  1, HAIR)

# Hair highlights
hline(2, 16, 20, HAIR_H)
hline(3, 18, 22, HAIR_H)

# Sides of hair going lower (ears area)
rect(12, 6, 13, 18, HAIR)        # left side hair
rect(34, 6, 35, 18, HAIR)        # right side hair

# ── FACE ────────────────────────────────────────────────────────
rect(14, 6,  33, 20, SKIN)       # face fill
# Soften face edges (ears)
px[14, 6]  = SKIN; px[15, 6]  = SKIN
px[33, 6]  = SKIN; px[32, 6]  = SKIN
# Jawline
rect(15, 19, 32, 21, SKIN)
rect(17, 21, 30, 22, SKIN_D)

# Blush
rect(15, 15, 17, 16, BLUSH)
rect(30, 15, 32, 16, BLUSH)

# ── EYES ────────────────────────────────────────────────────────
# Left eye
rect(16, 10, 19, 13, WHITE)
rect(17, 11, 18, 12, PUPIL)
px[16, 10] = EYE; px[19, 10] = EYE
px[16, 13] = EYE; px[19, 13] = EYE
# Right eye
rect(28, 10, 31, 13, WHITE)
rect(29, 11, 30, 12, PUPIL)
px[28, 10] = EYE; px[31, 10] = EYE
px[28, 13] = EYE; px[31, 13] = EYE
# Eyebrows
hline(9,  16, 19, HAIR)
hline(9,  28, 31, HAIR)

# ── MOUTH / NOSE ────────────────────────────────────────────────
px[23, 15] = SKIN_D              # nose shadow
px[24, 15] = SKIN_D
hline(18, 20, 27, MOUTH)        # smile
px[19, 17] = MOUTH; px[28, 17] = MOUTH  # corners

# ── NECK ────────────────────────────────────────────────────────
rect(20, 22, 27, 25, SKIN)

# ── HOODIE / BODY ───────────────────────────────────────────────
# Main body
rect(10, 25, 37, 44, HOODIE)
# Hood collar
rect(15, 25, 32, 28, HOODIE_D)
px[23, 25] = HOODIE_D; px[24, 25] = HOODIE_D  # V-neck center
# Side shadows
rect(10, 25, 11, 44, HOODIE_D)
rect(36, 25, 37, 44, HOODIE_D)
# Chest highlight
rect(16, 29, 20, 38, HOODIE_L)
# Kangaroo pocket
rect(16, 37, 31, 43, HOODIE_D)
rect(17, 38, 30, 42, STRIPE)
# Sleeve hints
rect(10, 25, 14, 38, HOODIE_D)
rect(33, 25, 37, 38, HOODIE_D)
# Wrists / cuffs (skin peeking)
rect(11, 38, 13, 42, SKIN)
rect(34, 38, 36, 42, SKIN)

# ── JEANS ───────────────────────────────────────────────────────
# Left leg
rect(11, 44, 22, 56, JEANS)
rect(11, 44, 11, 56, JEANS_D)
rect(22, 44, 22, 56, JEANS_D)
hline(44, 12, 21, JEANS_D)      # waistband seam
# Right leg
rect(25, 44, 36, 56, JEANS)
rect(25, 44, 25, 56, JEANS_D)
rect(36, 44, 36, 56, JEANS_D)
# Highlight seam on each leg
vline(16, 45, 55, JEANS_L)
vline(30, 45, 55, JEANS_L)
# Gap between legs
rect(23, 44, 24, 56, TRANS)

# ── SHOES ───────────────────────────────────────────────────────
# Left shoe
rect(10, 57, 23, 62, SHOE)
rect(10, 62, 23, 63, SOLE)
rect(10, 57, 10, 63, SHOE_D)
# Right shoe
rect(24, 57, 37, 62, SHOE)
rect(24, 62, 37, 63, SOLE)
rect(37, 57, 37, 63, SHOE_D)

# ── OUTLINE ─────────────────────────────────────────────────────
# Head outline
hline(0,  14, 33, OUTLINE)
hline(5,  12, 13, OUTLINE); hline(5, 34, 35, OUTLINE)
vline(12,  1, 21, OUTLINE)
vline(35,  1, 21, OUTLINE)
hline(22, 17, 30, OUTLINE)

# Body outline
hline(24, 14, 33, OUTLINE)
vline(9,  25, 44, OUTLINE)
vline(38, 25, 44, OUTLINE)
hline(44, 10, 22, OUTLINE); hline(44, 25, 37, OUTLINE)

# Leg outlines
vline(10, 44, 63, OUTLINE)
vline(23, 44, 63, OUTLINE)
vline(24, 44, 63, OUTLINE)
vline(37, 44, 63, OUTLINE)
hline(63, 10, 23, OUTLINE)
hline(63, 24, 37, OUTLINE)

img.save("assets/img/hero_sprite.png")
print(f"Saved hero_sprite.png ({W}x{H})")

# Also save a 3x upscaled version for preview
big = img.resize((W*3, H*3), Image.NEAREST)
big.save("assets/img/hero_sprite_3x.png")
print(f"Saved hero_sprite_3x.png ({W*3}x{H*3})")
