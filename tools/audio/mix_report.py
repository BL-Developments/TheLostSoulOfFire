#!/usr/bin/env python3
"""Mix check without ears: how loud each cue lands against the beds of its zone.

    tools/audio/.venv/bin/python tools/audio/mix_report.py [--zone arena]

Reads every `_audio.Play(AudioCue.X, volume …)` call in the game code (the volume a cue is
played at), the asset of each cue (AudioDirector `Add`/`AddVariants`), and the ambience and
music level of each zone (AudioDirector `AmbienceLevel`/`MusicLevel`, gameplay values). For each
asset it measures the loudest 400 ms (momentary loudness, ITU-R BS.1770 K-weighting) and the
integrated loudness of the beds, then reports each cue's peak loudness in the mix in LU above
the zone's combined bed. Bands from the mix plan (docs/audio/AUDIO_PRODUCTION_PLAN.md):
danger and hits clearly above the bed, swings and steps under the hits, UI quiet.

A cue played at several volumes is listed with its loudest call. This is a proxy for a listening
pass in the game, not a replacement: masking by frequency and the dynamic of a real fight are
not modelled.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / "src" / "TheLostSoulOfFire"
CONTENT = GAME / "Content"
DIRECTOR = GAME / "Audio" / "AudioDirector.cs"
RATE = 48000

#: Gameplay bed levels per zone (AmbienceLevel, MusicLevel in AudioDirector, not calm).
ZONE_BEDS = {
    "arena": ("Audio/Ambience/arena_ambience", 0.12, "Audio/Music/arena_loop", None),
    "hub": ("Audio/Ambience/hub_ambience", 0.13, "Audio/Music/hub_theme", 0.3),
    "shore": ("Audio/Ambience/shore_ambience", 0.11, "Audio/Music/shore_theme", 0.5),
    "crossing": ("Audio/Ambience/crossing_ambience", 0.10, "Audio/Music/crossing_theme", 0.32),
    # A hot arena fight: the combat score's three stems at full (AudioDirector.CombatStemGains,
    # times MusicGameplayVolume); the arena loop steps back under it.
    "combat": ("Audio/Ambience/arena_ambience", 0.12,
               [("Audio/Music/combat_pulse", 0.6), ("Audio/Music/combat_drive", 0.54), ("Audio/Music/combat_frenzy", 0.48)], None),
}

#: Expected bands in LU above the bed: (lower, upper).
BANDS = {
    "danger": (6.0, 20.0),
    "hit": (4.0, 18.0),
    "action": (0.0, 14.0),
    "step": (-14.0, 2.0),
    "ui": (-12.0, 4.0),
    "sense": (-12.0, 6.0),
    "event": (2.0, 18.0),
    # Layers under another cue (material under a hit, a body blow under the hurt sound): they
    # colour and time the blow, the cue on top carries it.
    "layer": (-6.0, 12.0),
    # Enemy calls between attacks (EnemyVoices): heard over the room, never over a warning.
    "call": (-4.0, 8.0),
    # The place's own sounds now and then (AudioDirector.PlaceEvents): part of the room.
    "place": (-12.0, 2.0),
}

CLASS = {
    "PlayerHit": "danger", "PlayerDeath": "danger", "HollowSwipe": "danger", "BurningCharge": "danger",
    "DevourerSlam": "danger", "BurningDetonation": "danger",
    "ScytheHit": "hit", "CoreHit": "hit", "CannonImpact": "hit", "EnemyDeath": "hit",
    "ScytheSwing1": "action", "ScytheSwing2": "action", "SoulCleave": "action", "Dash": "action",
    "CannonCharge": "action", "CannonFull": "action", "CannonFire": "action", "SoulRelease": "action",
    "CannonDraw": "action", "CannonStow": "action",
    "ScytheWeight1": "action", "ScytheWeight2": "action", "ScytheWeight3": "action",
    "EnemyEmerge": "event",
    "Footstep": "step", "FootstepWood": "step", "HollowStep": "step", "BurningStep": "step", "DevourerStep": "step",
    "UiMove": "ui", "UiBack": "ui", "UiOpen": "ui", "UiClose": "ui", "CurrencyGain": "ui",
    "SoulSenseOn": "sense", "SoulSenseOff": "sense", "SoulThrob": "sense",
    "HitHollow": "layer", "HitBurning": "layer", "HitDevourer": "layer", "HitDummy": "layer", "HitHeavy": "layer",
    "BodyHit": "layer", "DeathHollow": "layer", "DeathBurning": "layer", "DeathDevourer": "layer",
    "HollowWindup": "danger", "DevourerWindup": "danger", "BurningRush": "danger",
    "HollowCall": "call", "BurningCackle": "call", "DevourerGrowl": "call",
    "HollowGrasp": "layer", "BurningShriek": "layer",
    "FoundryBell": "place", "ShoreHorn": "place", "ShoreBoard": "place",
    "CannonStage": "action", "CannonBlast": "layer",
}

#: Menu sounds play over the paused beds (AudioDirector.PausedBedVolume).
PAUSED_BED_DB = 20 * np.log10(0.4)

#: Cues that never sound in a zone (no chests or waves in the prologue, no combat in the hub).
ARENA_ONLY = {"EnemyEmerge", "ChestOpen", "CurrencyGain", "WaveStart", "WaveClear", "EndingReveal", "TitleConfirm", "DoorAwaken"}
ZONE_CUES = {
    "arena": lambda cue, kind: cue not in {"DoorAwaken", "TitleConfirm"},
    "hub": lambda cue, kind: (kind in ("ui", "sense") and cue not in ("CurrencyGain", "SoulThrob")) or cue in ("Footstep", "DoorAwaken"),
    "shore": lambda cue, kind: cue not in ARENA_ONLY,
    "crossing": lambda cue, kind: cue not in ARENA_ONLY,
    # During a wave: no menus, chests or wave calls, only the fight.
    "combat": lambda cue, kind: kind not in ("ui",) and cue not in ARENA_ONLY,
}


def k_weight(x: np.ndarray) -> np.ndarray:
    from scipy.signal import lfilter
    # BS.1770 pre-filter (high shelf) and RLB high-pass at 48 kHz.
    b1, a1 = [1.53512485958697, -2.69169618940638, 1.19839281085285], [1.0, -1.69065929318241, 0.73248077421585]
    b2, a2 = [1.0, -2.0, 1.0], [1.0, -1.99004745483398, 0.99007225036621]
    return lfilter(b2, a2, lfilter(b1, a1, x))


def load(relative: str) -> np.ndarray | None:
    for ext in (".wav", ".ogg"):
        path = CONTENT / f"{relative}{ext}"
        if path.exists():
            data, rate = sf.read(str(path), always_2d=True)
            if rate != RATE:
                raise SystemExit(f"{path}: {rate} Hz")
            return data
    return None


def momentary_max(data: np.ndarray) -> float:
    weighted = np.stack([k_weight(data[:, c]) for c in range(data.shape[1])], axis=1)
    power = (weighted ** 2).sum(axis=1)
    window = int(0.4 * RATE)
    if len(power) < window:
        power = np.pad(power, (0, window - len(power)))
    cumulative = np.cumsum(np.insert(power, 0, 0.0))
    mean = (cumulative[window:] - cumulative[:-window]) / window
    return -0.691 + 10 * np.log10(max(mean.max(), 1e-12))


def integrated(data: np.ndarray) -> float:
    weighted = np.stack([k_weight(data[:, c]) for c in range(data.shape[1])], axis=1)
    return -0.691 + 10 * np.log10(max((weighted ** 2).sum(axis=1).mean(), 1e-12))


def cue_assets() -> dict[str, str]:
    text = DIRECTOR.read_text(encoding="utf-8")
    assets = {cue: path for cue, path in re.findall(r"Add\(content, AudioCue\.(\w+), \"([^\"]+)\"", text)}
    assets.update({cue: f"{path}_1" for cue, path in re.findall(r"AddVariants\(content, AudioCue\.(\w+), \"([^\"]+)\"", text)})
    return assets


#: Cues chosen at run time (a switch or a computed volume), with the loudest volume they play at.
INDIRECT = {
    "ScytheSwing1": 0.42, "ScytheSwing2": 0.42, "SoulCleave": 0.72,
    "Footstep": 0.42, "FootstepWood": 0.42,
    "HollowStep": 0.4, "BurningStep": 0.4, "DevourerStep": 0.5,
    "AbilityHeal": 0.72, "AbilityPierce": 0.72, "AbilityLeap": 0.72, "AbilityVortex": 0.72,
    "AbilityGuard": 0.72, "AbilityMark": 0.72,
    "HitHollow": 0.78, "HitBurning": 0.78, "HitDevourer": 0.78, "HitDummy": 0.78,
    "DeathHollow": 0.66, "DeathBurning": 0.66, "DeathDevourer": 0.64,
    "HollowCall": 0.55, "BurningCackle": 0.6, "DevourerGrowl": 0.72,
    "FoundryBell": 0.55, "ShoreHorn": 0.6, "ShoreBoard": 0.5,
}


def cue_volumes() -> dict[str, float]:
    volumes: dict[str, float] = dict(INDIRECT)
    for path in GAME.rglob("*.cs"):
        if "Debugging" in path.parts:
            continue
        for cue, expr in re.findall(r"_audio\.Play\(AudioCue\.(\w+)(?:,\s*([^,)]+))?", path.read_text(encoding="utf-8")):
            value = 1.0
            numbers = re.findall(r"(\d+\.\d+|\d+)f", expr or "")
            if numbers:
                value = max(float(n) for n in numbers if float(n) <= 1.0) if any(float(n) <= 1.0 for n in numbers) else 1.0
            volumes[cue] = max(volumes.get(cue, 0.0), value)
    return volumes


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--zone", default="arena", choices=sorted(ZONE_BEDS))
    args = parser.parse_args(argv)
    ambience_path, ambience_level, music_path, music_level = ZONE_BEDS[args.zone]
    if music_level is None:
        text = DIRECTOR.read_text(encoding="utf-8")
        found = re.search(r"MusicGameplayVolume\s*=\s*([\d.]+)f", text)
        music_level = float(found.group(1)) if found else 0.5
    beds = []
    music = [(path, gain * music_level) for path, gain in music_path] if isinstance(music_path, list) else [(music_path, music_level)]
    for path, level in [(ambience_path, ambience_level), *music]:
        data = load(path)
        if data is None:
            print(f"fehlt: {path}")
            continue
        beds.append(10 ** ((integrated(data) + 20 * np.log10(level)) / 10))
    bed = 10 * np.log10(sum(beds))
    print(f"Zone {args.zone}: Bett (Atmosphäre {ambience_level}, Musik {music_level:.2f}) = {bed:.1f} LUFS")
    print(f"{'Cue':20s} {'Klasse':7s} {'Vol':>5s} {'LU über Bett':>12s}  Band")
    assets, volumes = cue_assets(), cue_volumes()
    problems = 0
    rows = []
    for cue, volume in volumes.items():
        if cue not in assets:
            continue
        data = load(assets[cue])
        if data is None:
            continue
        kind = CLASS.get(cue, "event")
        if not ZONE_CUES[args.zone](cue, kind):
            continue
        reference = bed + (PAUSED_BED_DB if kind == "ui" else 0.0)
        level = momentary_max(data) + 20 * np.log10(max(volume, 1e-4)) - reference
        rows.append((cue, kind, volume, level))
    for cue, kind, volume, level in sorted(rows, key=lambda row: -row[3]):
        low, high = BANDS[kind]
        verdict = "ok" if low <= level <= high else ("zu leise" if level < low else "zu laut")
        problems += verdict != "ok"
        print(f"{cue:20s} {kind:7s} {volume:5.2f} {level:12.1f}  {verdict} ({low:+.0f}..{high:+.0f})")
    print(f"MIX_REPORT zone={args.zone} cues={len(rows)} outside_band={problems}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
