#!/usr/bin/env python3
"""Read-only structural audit for the final Ludo MVP delivery."""

from hashlib import sha256
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
DELIVERY = ROOT / "art/ludo_delivery"
CONTENT = ROOT / "src/TheLostSoulOfFire/Content/Textures"
DIRECTIONS = ("n", "ne", "e", "se", "s", "sw", "w", "nw")
ERRORS = []


def check_image(path, expected_size, require_alpha=True):
    if not path.exists():
        ERRORS.append(f"missing: {path.relative_to(ROOT)}")
        return
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        if rgba.size != expected_size:
            ERRORS.append(f"size {rgba.size} != {expected_size}: {path.relative_to(ROOT)}")
        if require_alpha:
            low, high = rgba.getchannel("A").getextrema()
            if low != 0 or high == 0:
                ERRORS.append(f"invalid alpha range {(low, high)}: {path.relative_to(ROOT)}")


locked = {
    DELIVERY / "00_reference/style_anchor_gameplay.png": "7651721ff0bcf4d806dbfcd0d3c2ae9e7de8c227032160c203bdba214bcebba8",
    DELIVERY / "02_characters/player/player_master_128.png": "537c624dcdcc2eab2a6d7aa1c1d7a8e122963429143392c2ca27d5742aecec64",
    DELIVERY / "03_weapons/scythe_physical_256.png": "864b0dbe3c204e36a7bcebca4bcb02d7139e29f047ccf2994d27874641959eb3",
}
for path, expected in locked.items():
    actual = sha256(path.read_bytes()).hexdigest()
    if actual != expected:
        ERRORS.append(f"locked hash changed: {path.relative_to(ROOT)}")

statics = {
    DELIVERY / "01_environment/arena_base_1800x1000.png": ((1800, 1000), False),
    DELIVERY / "02_characters/hollow/hollow_master_128.png": ((128, 128), True),
    DELIVERY / "02_characters/burning/burning_master_128.png": ((128, 128), True),
    DELIVERY / "02_characters/devourer/devourer_master_192.png": ((192, 192), True),
    DELIVERY / "03_weapons/soul_cannon_256.png": ((256, 256), True),
    DELIVERY / "05_pickups/lost_soul_64.png": ((64, 64), True),
    DELIVERY / "06_ending/life_flame_128.png": ((128, 128), True),
}
for path, (size, alpha) in statics.items():
    check_image(path, size, alpha)

packs = {
    "player": (("idle", "move"), 128),
    "hollow": (("idle", "move", "swipe"), 128),
    "burning": (("idle", "move", "charge"), 128),
    "devourer": (("idle", "move", "slam", "devour"), 192),
}
animation_count = 0
for character, (actions, frame_size) in packs.items():
    for action in actions:
        frames = 16 if character == "devourer" and action == "slam" else 9
        columns = 4 if frames == 16 else 3
        for direction in DIRECTIONS:
            animation_count += 1
            path = DELIVERY / f"02_characters/{character}/animations/{action}/{direction}.png"
            check_image(path, (columns * frame_size, columns * frame_size), True)

vfx = {
    "fx_scythe_slash_01.png": (768, 768),
    "fx_scythe_slash_02.png": (768, 768),
    "fx_scythe_cleave.png": (768, 768),
    "fx_core_hit.png": (384, 384),
    "fx_dash_ignition.png": (384, 384),
    "fx_cannon_charge_loop.png": (384, 384),
    "fx_cannon_muzzle_full.png": (768, 768),
    "fx_cannon_projectile_full.png": (384, 384),
    "fx_burning_detonation.png": (1024, 1024),
    "fx_soul_release.png": (512, 512),
    "fx_resonance_activate.png": (1024, 1024),
    "fx_death_flame_loop.png": (384, 384),
}
for name, size in vfx.items():
    check_image(DELIVERY / "04_vfx" / name, size, True)

content_count = len(list(CONTENT.rglob("*.png")))
if content_count != 116:
    ERRORS.append(f"Content PNG count {content_count} != 116")

if ERRORS:
    print("DELIVERY AUDIT: FAILED")
    print("\n".join(f"- {error}" for error in ERRORS))
    raise SystemExit(1)

print("DELIVERY AUDIT: PASSED")
print(f"locked_hashes=3 static_assets={len(statics)} animation_sheets={animation_count} vfx_sheets={len(vfx)} content_pngs={content_count}")
