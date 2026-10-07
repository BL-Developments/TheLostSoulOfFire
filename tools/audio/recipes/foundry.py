"""Signals of the fight that read as electronic beeps (CLAP, 07.10.2026), made of the foundry's own
matter instead: iron, chains, fire and the throat of the thing that eats souls.

- wave-gate: a wave begins. The furnace gate slams shut, its chains run, the furnace roars up
  and a drum of the works answers (the Ludo take, a gate and a bronze bell: "electronic beep" 0.90).
- wave-ease: a wave is won. The fight's fire sinks into itself, ash settles, a large iron plate
  hums once, low (the Ludo take, glass and bronze: "electronic beep" 0.66).
- devourer-devour: the Devourer drinks a soul. A long draw through its throat, the soul's
  whisper pulled in, a wet swallow and the cavity snapping shut (Ludo: "sci-fi laser" 0.56).

Mono 48 kHz, peaks below -1 dBFS; takes differ by seed (author.py --seed).
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe
from recipes.cannon import _finish, _flame, _iron, _room, _tongue
from recipes.combat import _burst, _grains, _ring, _thump
from recipes.voices import GAINS, QS, _vowel_track


def _chains(rng, n: int, start: float, span: float, count: int, gain: float) -> np.ndarray:
    """Chain links running over a pulley: a train of small metallic knocks that slows down."""
    out = np.zeros(n)
    at = start
    gap = rng.uniform(0.012, 0.018)
    for _ in range(count):
        if at >= start + span:
            break
        m = dsp.seconds(0.03)
        i = dsp.seconds(at)
        if i + m >= n:
            break
        link = _ring(m, [(rng.uniform(1900, 2600), 0.4, 0.008), (rng.uniform(3800, 4700), 0.25, 0.005)], rng)
        link += dsp.bandpass(rng.standard_normal(m), 1500, 6000) * np.exp(-np.arange(m) / (dsp.RATE * 0.002)) * 0.6
        fade = np.exp(-(at - start) / (span * 0.6))
        out[i:i + m] += link * fade * rng.uniform(0.5, 1.0) * gain
        at += gap
        gap *= rng.uniform(1.03, 1.09)
    return out


@recipe("wave-gate", "Eine Welle beginnt: das Ofentor der Gießhalle schlägt zu, seine Ketten rasseln nach, der Ofen brüllt auf, eine Trommel der Gießerei antwortet; schwer und körperlich, kein Glockenton, kein Piepen")
def wave_gate(rng) -> np.ndarray:
    n = dsp.seconds(1.5)
    # The gate: a huge iron slab hitting stone, low and ringing darkly.
    slam = _thump(n, 70, 34, 0.32) * 1.2 + dsp.lowpass(_burst(rng, n, 0.0, 30, 240, 0.12, 1.4), 240)
    plate = _ring(n, [(rng.uniform(118, 132), 0.6, 0.5), (rng.uniform(287, 305), 0.4, 0.3), (rng.uniform(463, 490), 0.25, 0.18),
                      (rng.uniform(731, 770), 0.15, 0.1)], rng, 0.002)
    crack = _burst(rng, n, 0.0, 600, 5000, 0.008, 0.8)
    chains = _chains(rng, n, 0.06, 0.55, 40, 0.35)
    # The furnace answers: a roar that surges up and falls back.
    roar = _flame(rng, n, 0.08, 0.16, 0.45, 50, 900, 10.0) + _tongue(rng, n, 0.1, 0.12, 0.3, 1000, 3000) * 0.12
    # A drum of the works, a beat after the gate.
    drum_at = rng.uniform(0.32, 0.38)
    drum = _thump(n, 96, 52, 0.22, drum_at) * 0.9 + dsp.lowpass(_burst(rng, n, drum_at, 60, 900, 0.05, 0.7), 900)
    x = slam + plate * 0.4 + crack + chains + roar * 0.45 + drum
    x = _room(x, rng, 0.8, 0.16, 3000)
    return _finish(x, -17.0, -1.5, 0.12)


@recipe("wave-ease", "Eine Welle ist geschafft: das Feuer des Kampfs sinkt leise fauchend in sich zusammen, die Glut knistert aus, Asche rieselt, darunter ein tiefes Summen der Halle; Erleichterung, kein Signalton, kein Glöckchen, kein Schlag")
def wave_ease(rng) -> np.ndarray:
    from recipes.ambiences import _pop
    n = dsp.seconds(2.0)
    t = dsp.time_axis(n)
    # The flames sink: a soft rush that comes in slowly (no blow at the start) and falls in pitch.
    swell = np.clip(t / 0.22, 0, 1) ** 2 * np.exp(-np.maximum(t - 0.22, 0) / 0.5)
    sink = dsp.swept_bandpass(dsp.pink(n, rng) + dsp.brown(n, rng) * 0.4, 1400 * np.exp(-t / 0.45) + 180, q=1.3)
    sink = sink / (np.std(sink) + 1e-9) * swell
    # The embers die out: crackles that thin and soften over the next second and a half.
    crackle = np.zeros(n)
    for _ in range(45):
        at = 0.1 + rng.exponential(0.45)
        if at > 1.8:
            continue
        dsp.place(crackle, _pop(rng, rng.random() < 0.15), dsp.seconds(at), min(1.0, 0.05 * rng.pareto(1.8) + 0.03) * np.exp(-at / 0.7))
    crackle = dsp.lowpass(crackle, 4500)
    ash = _grains(rng, n, 0.3, 1.2, 60, 1500, 6000, 0.08)
    # The hall's low hum underneath, coming and going softly.
    hum = dsp.bandpass(dsp.brown(n, rng), 45, 120)
    hum = hum / (np.std(hum) + 1e-9) * np.sin(np.pi * np.clip(t / 1.8, 0, 1)) ** 2
    x = sink * 0.35 + crackle * 0.9 + ash + hum * 0.12
    x = _room(x, rng, 1.0, 0.18, 2500)
    return _finish(x, -23.0, -4.0, 0.25)


@recipe("devourer-devour", "Devourer verschlingt eine Seele: ein langer Zug durch die Kehle, das Flüstern der Seele wird hineingerissen, ein nasses Schlucken, die Rumpföffnung schnappt mit Knochen zu; kein Laser, kein Synthesizer")
def devourer_devour(rng) -> np.ndarray:
    n = dsp.seconds(1.25)
    t = dsp.time_axis(n)
    draw = np.clip(t / 0.95, 0, 1)
    # The draw: air through a huge throat, its vowel closing from an open roar to a narrow "oo".
    vowels = _vowel_track(n, [(0.0, "aw"), (0.5, "o"), (0.8, "u"), (1.0, "u")], shift=0.55)
    air = dsp.highpass(rng.standard_normal(n), 60) * np.clip(t / 0.08, 0, 1) * (0.4 + 0.6 * draw) * (t < 0.98)
    throat = sum(dsp.swept_bandpass(air, centre, q=q * 0.6) * g for centre, g, q in zip(vowels, GAINS, QS))
    throat = throat / (np.std(throat) + 1e-9)
    # A low, wet rumble under it (no pitch: band noise, shaken).
    rumble = dsp.bandpass(dsp.brown(n, rng), 35, 160) * (0.5 + 0.5 * np.sin(2 * np.pi * 6 * t)) * draw
    rumble = rumble / (np.std(rumble) + 1e-9)
    # The soul's whisper, pulled in: a thin breathy "ee" that rises and is cut off.
    soul_vowels = _vowel_track(n, [(0.0, "i"), (1.0, "e")], shift=1.35)
    breath = dsp.highpass(rng.standard_normal(n), 900) * np.sin(np.pi * np.clip((t - 0.15) / 0.8, 0, 1)) ** 2 * (t < 0.95)
    whisper = sum(dsp.swept_bandpass(breath, centre, q=q) * g for centre, g, q in zip(soul_vowels, GAINS, QS))
    whisper = whisper / (np.std(whisper) + 1e-9)
    # The swallow and the cavity snapping shut.
    gulp_at = rng.uniform(0.95, 1.0)
    gulp = _thump(n, 120, 60, 0.07, gulp_at) * 0.9 + dsp.lowpass(_burst(rng, n, gulp_at, 80, 700, 0.03, 0.8), 700)
    wet = _grains(rng, n, gulp_at, 0.08, 10, 400, 2500, 0.35, (0.004, 0.012))
    snap = _burst(rng, n, gulp_at + 0.05, 1200, 6000, 0.004, 0.7) + _ring(n, [(rng.uniform(900, 1100), 0.2, 0.02)], rng, gulp_at + 0.05)
    x = throat * 0.45 + rumble * 0.35 + whisper * 0.12 + gulp + wet + snap
    x = _room(x, rng, 0.6, 0.14, 3500)
    return _finish(x, -19.0, -2.0, 0.08)
