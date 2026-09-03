#!/usr/bin/env python3
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
DELIVERY = ROOT / "art" / "ludo_delivery"
REVIEW = DELIVERY / "review"
FONT_PATH = "/System/Library/Fonts/Helvetica.ttc"


def font(size: int, bold: bool = False):
    try:
        index = 1 if bold else 0
        return ImageFont.truetype(FONT_PATH, size, index=index)
    except OSError:
        return ImageFont.load_default()


def checker(size, tile=16):
    image = Image.new("RGBA", size, (20, 19, 25, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], tile):
        for x in range(0, size[0], tile):
            if (x // tile + y // tile) % 2:
                draw.rectangle((x, y, x + tile - 1, y + tile - 1), fill=(29, 27, 36, 255))
    return image


def open_rgba(path):
    return Image.open(path).convert("RGBA")


def fit_alpha(image, box, margin=8):
    alpha_box = image.getchannel("A").getbbox()
    if alpha_box:
        image = image.crop(alpha_box)
    max_w = max(1, box[0] - margin * 2)
    max_h = max(1, box[1] - margin * 2)
    scale = min(max_w / image.width, max_h / image.height)
    scale = max(scale, 1 / max(image.width, image.height))
    target = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    return image.resize(target, Image.Resampling.NEAREST)


def frame(path, frame_size, index):
    sheet = open_rgba(path)
    columns = sheet.width // frame_size
    x = (index % columns) * frame_size
    y = (index // columns) * frame_size
    return sheet.crop((x, y, x + frame_size, y + frame_size))


def paste_center(board, image, box):
    x, y, w, h = box
    fitted = fit_alpha(image, (w, h))
    board.alpha_composite(fitted, (x + (w - fitted.width) // 2, y + (h - fitted.height) // 2))


def title(board, heading, subheading):
    draw = ImageDraw.Draw(board)
    draw.rectangle((0, 0, board.width, 92), fill=(8, 7, 12, 255))
    draw.text((36, 20), heading, fill=(239, 232, 247, 255), font=font(32, True))
    draw.text((38, 59), subheading, fill=(169, 152, 191, 255), font=font(17))


def static_board():
    board = Image.new("RGBA", (1600, 1020), (12, 11, 17, 255))
    title(board, "02 — Static cast, weapons, and pickups", "Accepted production masters; transparency shown on checker cards")
    items = [
        ("PLAYER — LOCKED", DELIVERY / "02_characters/player/player_master_128.png"),
        ("HOLLOW", DELIVERY / "02_characters/hollow/hollow_master_128.png"),
        ("BURNING", DELIVERY / "02_characters/burning/burning_master_128.png"),
        ("DEVOURER", DELIVERY / "02_characters/devourer/devourer_master_192.png"),
        ("SCYTHE — CLEAN DERIVED", DELIVERY / "03_weapons/scythe_physical_256_clean.png"),
        ("SOUL CANNON", DELIVERY / "03_weapons/soul_cannon_256.png"),
        ("LOST SOUL", DELIVERY / "05_pickups/lost_soul_64.png"),
        ("LIFE FLAME", DELIVERY / "06_ending/life_flame_128.png"),
    ]
    draw = ImageDraw.Draw(board)
    card_w, card_h = 370, 420
    for index, (label, path) in enumerate(items):
        col, row = index % 4, index // 4
        x, y = 35 + col * 390, 118 + row * 445
        card = checker((card_w, card_h - 50), 18)
        board.alpha_composite(card, (x, y + 50))
        draw.rectangle((x, y, x + card_w, y + 49), fill=(25, 22, 32, 255))
        draw.text((x + 16, y + 14), label, fill=(220, 211, 232, 255), font=font(18, True))
        paste_center(board, open_rgba(path), (x + 10, y + 60, card_w - 20, card_h - 70))
    board.convert("RGB").save(REVIEW / "02_static_cast_and_weapons.png", quality=95)


def arena_board():
    arena = open_rgba(DELIVERY / "01_environment/arena_base_1800x1000.png")
    board = Image.new("RGBA", (1600, 1000), (7, 6, 11, 255))
    title(board, "03 — Arena and gameplay-scale placement", "1800×1000 source mapped to current combat bounds; geometry remains code-authoritative")
    view = arena.resize((1440, 800), Image.Resampling.NEAREST)
    board.alpha_composite(view, (80, 130))
    draw = ImageDraw.Draw(board)
    bounds = (80 + round(105 * .8), 130 + round(95 * .8), 80 + round((105 + 1590) * .8), 130 + round((95 + 810) * .8))
    draw.rectangle(bounds, outline=(92, 209, 198, 210), width=3)
    draw.text((bounds[0] + 12, bounds[1] + 10), "IMPLEMENTED COMBAT BOUNDS", fill=(133, 232, 220, 255), font=font(15, True))
    sprites = [
        ("PLAYER + RUNTIME CORE", "player", 128, 100, (900, 500), "Player"),
        ("HOLLOW", "hollow", 128, 112, (610, 370), "Enemies/Hollow"),
        ("BURNING", "burning", 128, 104, (1210, 650), "Enemies/Burning"),
        ("DEVOURER", "devourer", 192, 174, (1420, 350), "Enemies/Devourer"),
    ]
    for label, family, frame_size, display_size, world, content_family in sprites:
        sheet = ROOT / "src/TheLostSoulOfFire/Content/Textures" / content_family / "Animations/idle/s.png"
        sprite = frame(sheet, frame_size, 4)
        sprite = sprite.resize((display_size, display_size), Image.Resampling.NEAREST)
        px, py = 80 + round(world[0] * .8), 130 + round(world[1] * .8)
        sprite = sprite.resize((round(display_size * .8), round(display_size * .8)), Image.Resampling.NEAREST)
        board.alpha_composite(sprite, (px - sprite.width // 2, py - sprite.height // 2))
        if family == "player":
            draw.ellipse((px - 6, py - 6, px + 6, py + 6), fill=(227, 208, 255, 255))
            draw.ellipse((px - 13, py - 13, px + 13, py + 13), outline=(133, 73, 190, 220), width=3)
        draw.text((px + sprite.width // 2 + 5, py - 10), label, fill=(236, 229, 245, 255), font=font(13, True))
    board.convert("RGB").save(REVIEW / "03_arena_and_gameplay.png", quality=95)


def animation_board():
    rows = [
        ("PLAYER", "Player", "idle", 128, 9), ("PLAYER", "Player", "move", 128, 9),
        ("HOLLOW", "Enemies/Hollow", "idle", 128, 9), ("HOLLOW", "Enemies/Hollow", "move", 128, 9), ("HOLLOW", "Enemies/Hollow", "swipe", 128, 9),
        ("BURNING", "Enemies/Burning", "idle", 128, 9), ("BURNING", "Enemies/Burning", "move", 128, 9), ("BURNING", "Enemies/Burning", "charge", 128, 9),
        ("DEVOURER", "Enemies/Devourer", "idle", 192, 9), ("DEVOURER", "Enemies/Devourer", "move", 192, 9), ("DEVOURER", "Enemies/Devourer", "slam", 192, 16), ("DEVOURER", "Enemies/Devourer", "devour", 192, 9),
    ]
    board = Image.new("RGBA", (1600, 1550), (11, 10, 16, 255))
    title(board, "04 — Character animation keyframes", "South-facing production sheets sampled at start, anticipation, contact, recovery, and end")
    draw = ImageDraw.Draw(board)
    row_h = 116
    for row, (family_label, family_path, action, size, count) in enumerate(rows):
        y = 112 + row * row_h
        draw.rectangle((25, y, 1575, y + row_h - 8), fill=(17, 15, 23, 255) if row % 2 == 0 else (22, 19, 29, 255))
        draw.text((45, y + 22), family_label, fill=(176, 153, 199, 255), font=font(17, True))
        draw.text((45, y + 53), action.upper(), fill=(235, 228, 244, 255), font=font(22, True))
        path = ROOT / "src/TheLostSoulOfFire/Content/Textures" / family_path / f"Animations/{action}/s.png"
        indices = [0, max(1, count // 4), count // 2, max(0, count - 3), count - 1]
        for column, index in enumerate(indices):
            cell_x = 330 + column * 240
            image = frame(path, size, index)
            fitted = fit_alpha(image, (104, 96), 2)
            board.alpha_composite(fitted, (cell_x + (104 - fitted.width) // 2, y + 5 + (96 - fitted.height) // 2))
            draw.text((cell_x + 114, y + 43), f"F{index + 1:02}", fill=(164, 157, 176, 255), font=font(15))
    board.convert("RGB").save(REVIEW / "04_character_animations.png", quality=95)


def vfx_board():
    effects = [
        ("SCYTHE SLASH 01", "fx_scythe_slash_01", 256, 9),
        ("SCYTHE SLASH 02", "fx_scythe_slash_02", 256, 9),
        ("SCYTHE CLEAVE", "fx_scythe_cleave", 256, 9),
        ("CORE HIT", "fx_core_hit", 128, 9),
        ("DASH IGNITION", "fx_dash_ignition", 128, 9),
        ("CANNON CHARGE LOOP", "fx_cannon_charge_loop", 128, 9),
        ("CANNON MUZZLE", "fx_cannon_muzzle_full", 256, 9),
        ("CANNON PROJECTILE", "fx_cannon_projectile_full", 128, 9),
        ("BURNING DETONATION", "fx_burning_detonation", 256, 16),
        ("SOUL RELEASE", "fx_soul_release", 128, 16),
        ("RESONANCE ACTIVATE", "fx_resonance_activate", 256, 16),
        ("DEATH FLAME LOOP", "fx_death_flame_loop", 128, 9),
    ]
    board = Image.new("RGBA", (1600, 1240), (9, 8, 14, 255))
    title(board, "05 — Required VFX", "All twelve transparent production sheets; three representative frames per effect")
    draw = ImageDraw.Draw(board)
    card_w, card_h = 370, 340
    for i, (label, filename, size, count) in enumerate(effects):
        col, row = i % 4, i // 4
        x, y = 35 + col * 390, 116 + row * 365
        board.alpha_composite(checker((card_w, card_h), 14), (x, y))
        draw.rectangle((x, y, x + card_w, y + 42), fill=(23, 20, 31, 255))
        draw.text((x + 13, y + 11), label, fill=(231, 223, 242, 255), font=font(16, True))
        path = DELIVERY / "04_vfx" / f"{filename}.png"
        indices = [max(0, count // 4), count // 2, max(0, count - 3)]
        for k, index in enumerate(indices):
            image = frame(path, size, index)
            fitted = fit_alpha(image, (108, 248), 2)
            px = x + 10 + k * 118 + (108 - fitted.width) // 2
            py = y + 62 + (248 - fitted.height) // 2
            board.alpha_composite(fitted, (px, py))
            draw.text((x + 47 + k * 118, y + 311), f"F{index + 1}", fill=(157, 145, 174, 255), font=font(13))
    board.convert("RGB").save(REVIEW / "05_vfx.png", quality=95)


if __name__ == "__main__":
    REVIEW.mkdir(parents=True, exist_ok=True)
    static_board()
    arena_board()
    animation_board()
    vfx_board()
