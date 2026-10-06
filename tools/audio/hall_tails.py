#!/usr/bin/env python3
"""Hall tails: the reverberation of a hall, rendered offline for the cues that should fill it.

    tools/audio/.venv/bin/python tools/audio/hall_tails.py [--only scythe_hit ...]

MonoGame's SoundEffect has no reverb on DesktopGL, and the cues are shared between the open-air
prologue and the halls. So each listed cue gets a wet-only companion <name>_hall.wav: the dry take
convolved with the impulse response of its hall, without the direct sound. The game plays it in
that hall only, together with the cue, at a send level per cue (AudioDirector.HallSends).

Halls (RT60 = the IR length): the foundry of the arena (brick and iron, 2.4 s, darker), and the
Ashen Antechamber (dressed stone, 3.2 s, the room of its ambience bed in recipes/ambiences.py).
Every tail is set to RELATIVE_LU below the dry take's integrated loudness (a plain convolution
would let long, low takes such as the cannon swell far above short hits); the send level the game
plays it at then means the same for every cue. The tail starts with the hall's predelay and is cut
where it falls 60 dB below its peak. Prints the ledger lines for SOURCES.md.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dsp  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SFX = ROOT / "src" / "TheLostSoulOfFire" / "Content" / "Audio" / "Sfx"

#: Hall per asset: (RT60 seconds, damping Hz, predelay seconds, seed).
FOUNDRY = (2.4, 2600.0, 0.035, 71)
ANTECHAMBER = (3.2, 3500.0, 0.02, 73)

#: Loudness of a tail relative to its dry take (LU).
RELATIVE_LU = -8.0

#: Assets that get a tail, and in which hall. Hits, blows and deaths of the arena; the player's
#: footsteps in the antechamber, where they are the main sound of the room.
TAILS = {
    "scythe_hit": FOUNDRY,
    "core_hit": FOUNDRY,
    "cannon_fire": FOUNDRY,
    "cannon_impact": FOUNDRY,
    "burning_detonation": FOUNDRY,
    "devourer_slam": FOUNDRY,
    "enemy_death": FOUNDRY,
    "soul_cleave": FOUNDRY,
    "player_hit": FOUNDRY,
    "wave_start": FOUNDRY,
    "footstep_stone_1": ANTECHAMBER,
}


def tail(x: np.ndarray, hall: tuple[float, float, float, int]) -> np.ndarray:
    rt60, damping, predelay, seed = hall
    ir = dsp.impulse_response(rt60, np.random.default_rng(seed), damping_hz=damping, predelay=predelay).mean(axis=1)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-12
    wet = signal.fftconvolve(x, ir)
    # Cut where the tail has fallen 60 dB below its own peak, with a short fade.
    envelope = np.sqrt(np.convolve(wet ** 2, np.ones(dsp.seconds(0.02)) / dsp.seconds(0.02), mode="same"))
    peak = envelope.max()
    alive = np.nonzero(envelope > peak * 10 ** (-60 / 20))[0]
    end = min(len(wet), alive[-1] + dsp.seconds(0.05)) if len(alive) else len(wet)
    wet = wet[:end]
    fade = min(len(wet), dsp.seconds(0.15))
    wet[-fade:] *= np.linspace(1.0, 0.0, fade)
    return wet


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--only", nargs="+")
    args = parser.parse_args(argv)
    for name, hall in TAILS.items():
        if args.only and name not in args.only:
            continue
        x, rate = sf.read(str(SFX / f"{name}.wav"))
        if rate != dsp.RATE:
            raise SystemExit(f"{name}: {rate} Hz")
        if x.ndim > 1:
            x = x.mean(axis=1)
        wet = tail(x, hall)
        wet *= 10 ** ((dsp.loudness(x) + RELATIVE_LU - dsp.loudness(wet)) / 20)
        peak = np.max(np.abs(wet))
        if peak > 10 ** (-1.5 / 20):
            raise SystemExit(f"{name}: tail peak {20 * np.log10(peak):.1f} dBFS, would clip")
        target = SFX / f"{name}_hall.wav"
        sf.write(str(target), wet, rate, subtype="PCM_16")
        hall_name = "Gießhalle" if hall is FOUNDRY else "Vorhalle"
        print(f"| `Audio/Sfx/{target.name}` | Hallfahne von {name}.wav ({hall_name}, RT60 {hall[0]:.1f} s) | "
              f"{len(wet) / rate:.2f} s, 1 Kanal, {dsp.loudness(wet):.1f} LUFS, Spitze {20 * np.log10(peak):.1f} dBFS |")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
