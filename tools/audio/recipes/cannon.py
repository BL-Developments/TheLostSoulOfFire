"""The Soul Cannon: a heavy conduit of blackened iron that the player feeds his own Death Flame.

07_SOUL_CANNON: it cannot make the flame, it only holds and fires it. So it sounds like iron
and fire, never like electricity or glass: the Ludo charge read as a sci-fi energy weapon and
the old hum as a bell (CLAP), which is what the owner wanted gone (07.10.2026).

- ignition when the charge begins: the flame catches in the chamber and is drawn in;
- the charge loop: fire roaring under pressure inside an iron vessel (the game raises its
  pitch and level with the charge);
- a ratchet at each charge stage: the chamber's latch takes the next notch, the flame flares;
- full charge: the latch slams home, the flame bursts up against the bars;
- the shot: crack, a heavy low blow, the flame blasting out, the cannon slamming back into
  the hands and the chamber venting; a full shot adds a deep pressure blow and a roaring tail.

Mono 48 kHz, peaks below -1 dBFS; takes differ by seed (author.py --seed).
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe
from recipes.combat import _burst, _grains, _ring, _thump


def _limit(x: np.ndarray, ceiling_db: float, release: float = 0.04, lookahead: float = 0.0015) -> np.ndarray:
    """A peak limiter: the gain drops at once (looking a little ahead) and recovers over
    `release`, so a transient is held under the ceiling without dulling the body behind it."""
    from scipy.ndimage import minimum_filter1d
    ceiling = 10 ** (ceiling_db / 20)
    need = np.minimum(1.0, ceiling / np.maximum(np.abs(x), 1e-9))
    need = minimum_filter1d(need, size=2 * dsp.seconds(lookahead) + 1)
    gain = np.empty_like(need)
    current, coefficient = 1.0, np.exp(-1.0 / (dsp.RATE * release))
    for index, target in enumerate(need):
        current = target if target < current else target + (current - target) * coefficient
        gain[index] = current
    return x * gain


def _finish(x: np.ndarray, lufs: float, peak: float = -2.0, fade_out: float = 0.04) -> np.ndarray:
    """High-pass, fades, then loudness and limiter in turn: every take of a cue lands at the same
    loudness (normalising alone stops at the peak ceiling, so transient takes ended up quieter)."""
    x = dsp.highpass(x, 28)
    x = dsp.fades(x, 0.0005, fade_out)
    for _ in range(3):
        x = _limit(x * 10 ** ((lufs - dsp.loudness(x)) / 20), peak)
    return x


def _room(x: np.ndarray, rng, seconds: float = 0.5, wet: float = 0.14, damping: float = 4500) -> np.ndarray:
    ir = dsp.impulse_response(seconds, rng, damping_hz=damping, predelay=0.006)
    return dsp.reverb(x, ir, wet=wet).mean(axis=1)


def _flame(rng, n: int, at: float, attack: float, decay: float, low: float = 60, high: float = 900,
           lick_hz: float = 11.0) -> np.ndarray:
    """A body of flame: low roaring noise that swells in, licks (flutters) and dies away."""
    t = dsp.time_axis(n)
    local = t - at
    env = np.where(local < 0, 0.0, np.clip(local / max(attack, 1e-4), 0, 1) ** 1.5 * np.exp(-np.maximum(local - attack, 0) / decay))
    roar = dsp.bandpass(dsp.brown(n, rng) + dsp.pink(n, rng) * 0.35, low, high)
    roar /= np.std(roar) + 1e-9
    lick = np.clip(0.55 + 0.45 * (dsp.smooth_random(n, lick_hz, rng) - 0.5) * 2.6, 0.1, None)
    return roar * env * lick


def _tongue(rng, n: int, at: float, attack: float, decay: float, low: float = 1100, high: float = 3600) -> np.ndarray:
    """The flame's tongues: a bright, breathy rush above the roar (never a hiss on its own)."""
    t = dsp.time_axis(n)
    local = t - at
    env = np.where(local < 0, 0.0, np.clip(local / max(attack, 1e-4), 0, 1) * np.exp(-np.maximum(local - attack, 0) / decay))
    air = dsp.bandpass(dsp.pink(n, rng), low, high)
    return air / (np.std(air) + 1e-9) * env


def _iron(rng, n: int, at: float, base: float, weight: float, decay: float = 1.0) -> np.ndarray:
    """Blackened iron struck: a low knock and a few short, inharmonic partials (no bell)."""
    partials = [(base, 0.5, 0.05 * decay), (base * 2.43, 0.32, 0.03 * decay), (base * 3.97, 0.2, 0.02 * decay),
                (base * 6.1, 0.1, 0.012 * decay)]
    ring = _ring(n, partials, rng, at)
    knock = _burst(rng, n, at, 700, 5200, 0.005, 0.9)
    body = dsp.lowpass(_burst(rng, n, at, 40, 300, 0.04, 1.3), 280)
    return (ring * 0.55 + knock + body) * weight


def _latch(rng, n: int, at: float, gain: float) -> np.ndarray:
    """A steel latch tooth snapping over: a short, dry, bright click with a tiny body."""
    click = _burst(rng, n, at, 2200, 9000, 0.0025, 1.0)
    tick = _ring(n, [(rng.uniform(2600, 3400), 0.25, 0.006), (rng.uniform(5200, 6400), 0.12, 0.004)], rng, at)
    return (click + tick) * gain


@recipe("cannon-ignite", "Seelenkanone beginnt zu laden: die Death Flame fängt in der Kammer Feuer (ein dumpfes Aufwallen), wird hörbar hineingesogen, das Eisen tickt; kein Laser, kein Glas")
def cannon_ignite(rng) -> np.ndarray:
    n = dsp.seconds(0.75)
    t = dsp.time_axis(n)
    catch = _burst(rng, n, 0.0, 300, 2600, 0.03, 0.6)
    whoomp = _flame(rng, n, 0.01, 0.06, 0.22, 50, 700, 13.0)
    # Drawn in: the flame from the core rushing into the chamber, rising as it goes.
    draw = dsp.swept_bandpass(rng.standard_normal(n), 380 + 1300 * np.clip(t / 0.6, 0, 1) ** 1.3, q=2.2)
    draw = draw / (np.std(draw) + 1e-9) * np.sin(np.pi * np.clip((t - 0.05) / 0.62, 0, 1)) ** 2
    ticks = _iron(rng, n, 0.12, rng.uniform(900, 1150), 0.12, 0.4) + _iron(rng, n, 0.38, rng.uniform(1000, 1300), 0.08, 0.4)
    x = catch + whoomp * 0.75 + draw * 0.16 + ticks
    x = _room(x, rng, 0.4, 0.12)
    return _finish(x, -21.0, -3.0)


@recipe("cannon-charge-loop", "Seelenkanone lädt: Feuer unter Druck in einem eisernen Kessel, ein flackerndes Fauchen mit Glutknistern, darunter ein hohles Brausen, ab und zu tickt das heiße Eisen; nahtlose Mono-Schleife, das Spiel hebt Tonhöhe und Pegel mit der Ladung", loop=True)
def cannon_charge_loop(rng) -> np.ndarray:
    from recipes.ambiences import _pop
    seconds = 2.0
    n = dsp.seconds(seconds)
    # The fire: a mid roar that flickers fast and deep (a smooth low rush reads as wind).
    lick = np.clip(0.3 + 0.7 * dsp.smooth_loop(n, 15.0, rng), 0.04, None) ** 1.4
    lick *= 0.8 + 0.2 * dsp.smooth_loop(n, 3.0, rng)
    roar = dsp.circular(lambda x: dsp.bandpass(x, 110, 1100), dsp.brown(n, rng) + dsp.pink(n, rng) * 0.5)
    roar = roar / (np.std(roar) + 1e-9) * lick
    tongue = dsp.circular(lambda x: dsp.bandpass(x, 1100, 3400), dsp.pink(n, rng))
    tongue = tongue / (np.std(tongue) + 1e-9) * lick ** 2
    # Inside an iron vessel: a hollow low body under it, steadier than the flame.
    body = dsp.circular(lambda x: dsp.resonator(dsp.bandpass(x, 50, 260), 98.0, 3.0), dsp.brown(n, rng))
    body = body / (np.std(body) + 1e-9) * (0.75 + 0.25 * dsp.smooth_loop(n, 1.0, rng))
    # Embers crackling against the grilles, and the hot iron ticking now and then.
    crackle = np.zeros(n)
    for _ in range(int(seconds * 14)):
        dsp.place(crackle, _pop(rng, rng.random() < 0.2), int(rng.integers(0, n - dsp.seconds(0.06))), min(1.0, 0.06 * rng.pareto(1.8) + 0.03))
    for _ in range(3):
        m = dsp.seconds(0.05)
        dsp.place(crackle, _iron(rng, m, 0.0, rng.uniform(1100, 1700), 1.0, 0.35), int(rng.integers(0, n - m)), rng.uniform(0.04, 0.08))
    crackle = dsp.circular(lambda x: dsp.lowpass(x, 5000), crackle)
    mix = roar * 0.32 + tongue * 0.05 + body * 0.16 + crackle * 0.9
    mix = dsp.circular(lambda x: dsp.highpass(x, 40), mix)
    quiet = np.abs(mix) + np.abs(np.roll(mix, 1))
    mix = np.roll(mix, -int(np.argmin(quiet)))
    return dsp.normalise_loudness(mix, -22.0, peak_ceiling_db=-4.0)


def _tooth(rng, n: int, at: float, gain: float) -> np.ndarray:
    """One ratchet tooth: a tiny impulse ringing the pawl's steel (resonant, not a noise burst:
    short broadband clicks read as gunshots)."""
    pulse = np.zeros(n)
    start = dsp.seconds(at)
    if start >= n:
        return pulse
    pulse[start] = 1.0
    if start + 1 < n:
        pulse[start + 1] = -0.6
    ring = sum(dsp.resonator(pulse, f * rng.uniform(0.95, 1.05), q) * g
               for f, q, g in ((2900, 22, 1.0), (4600, 26, 0.6), (6900, 30, 0.35), (1500, 14, 0.4)))
    return ring * gain


@recipe("cannon-stage", "Seelenkanone erreicht die nächste Ladestufe: die Raste der Kammer rattert über einige Zähne (helles Stahlrattern in der Kammer), das Eisen setzt sich dumpf; kein Knall, keine Glocke")
def cannon_stage(rng) -> np.ndarray:
    n = dsp.seconds(0.4)
    count = int(rng.integers(5, 8))
    gap = rng.uniform(0.009, 0.013)
    teeth = sum(_tooth(rng, n, k * gap * rng.uniform(0.85, 1.15), 0.5 + 0.5 * np.sin(np.pi * (k + 1) / (count + 1)))
                for k in range(count))
    teeth /= np.max(np.abs(teeth)) + 1e-9
    # The chamber settling into the notch: a soft, dull thud (a ringing partial read as a bell,
    # a flare of flame as a shot).
    t = dsp.time_axis(n)
    end = count * gap
    settle = dsp.lowpass(rng.standard_normal(n), 380) * np.clip((t - end) / 0.004, 0, 1) * np.exp(-np.maximum(t - end, 0) / 0.03)
    x = teeth + settle * 0.5
    x = _room(x, rng, 0.3, 0.12)
    return _finish(x, -22.0, -3.0)


@recipe("cannon-full", "Seelenkanone voll geladen: die Raste schlägt mit schwerem, nachklingendem Eisen ein, dann faucht die Flamme hinter den Gittern auf und will hinaus; unverwechselbar, kein Glas, kein Piepen, kein Knall")
def cannon_full(rng) -> np.ndarray:
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    base = rng.uniform(235, 265)
    clank = _ring(n, [(base, 0.6, 0.16), (base * 2.43, 0.4, 0.1), (base * 3.97, 0.25, 0.07), (base * 6.1, 0.12, 0.04)], rng, 0.0)
    clank *= np.clip(t / 0.0015, 0, 1)
    knock = _burst(rng, n, 0.0, 600, 4000, 0.004, 0.6)
    latch = _latch(rng, n, 0.0, 0.8)
    body = dsp.lowpass(_burst(rng, n, 0.0, 60, 300, 0.03, 0.5), 300)
    # The flame surges up behind the bars a moment after the iron, and shudders against them.
    surge = _flame(rng, n, 0.05, 0.09, 0.3, 120, 1400, 16.0)
    tongue = _tongue(rng, n, 0.06, 0.08, 0.2, 1100, 3600)
    shudder = 1 + 0.4 * np.sin(2 * np.pi * 19 * t) * np.exp(-np.maximum(t - 0.15, 0) / 0.25) * (t > 0.15)
    x = clank * 0.5 + knock + latch + body + (surge * 0.55 + tongue * 0.12) * shudder
    x = _room(x, rng, 0.5, 0.14)
    return _finish(x, -18.5, -2.0)


@recipe("cannon-fire", "Seelenkanone feuert: ein trockener Knall, ein schwerer tiefer Schlag, die Flamme bricht fauchend aus der Mündung, die Kanone schlägt eisern in die Hände zurück, die Kammer bläst zischend ab, Asche rieselt")
def cannon_fire(rng) -> np.ndarray:
    n = dsp.seconds(0.95)
    t = dsp.time_axis(n)
    crack = _burst(rng, n, 0.0, 900, 8000, 0.006, 1.0) + _burst(rng, n, 0.0, 200, 1800, 0.012, 0.8)
    blow = _thump(n, rng.uniform(105, 120), 46, 0.16) * 1.1 + dsp.lowpass(_burst(rng, n, 0.0, 30, 220, 0.09, 1.4), 220)
    blast = _flame(rng, n, 0.0, 0.006, 0.13, 70, 2600, 18.0)
    tail = _flame(rng, n, 0.03, 0.05, 0.32, 50, 700, 10.0)
    recoil = _iron(rng, n, rng.uniform(0.014, 0.02), rng.uniform(260, 310), 0.7, 1.0)
    # The chamber vents through its grilles as the cannon empties.
    vent = dsp.bandpass(rng.standard_normal(n), 1800, 5200) * np.clip((t - 0.07) / 0.04, 0, 1) * np.exp(-np.maximum(t - 0.11, 0) / 0.16)
    vent *= np.clip(0.6 + 0.4 * (dsp.smooth_random(n, 25.0, rng) - 0.5) * 2.5, 0.2, None)
    ash = _grains(rng, n, 0.06, 0.35, 40, 1500, 6500, 0.18)
    x = crack * 0.9 + blow + blast * 0.8 + tail * 0.35 + recoil + vent * 0.07 + ash
    x = dsp.soft_clip(x / (np.max(np.abs(x)) + 1e-9) * 1.4, 1.4)
    x = _room(x, rng, 0.5, 0.12)
    return _finish(x, -16.5, -1.5, 0.06)


@recipe("cannon-fire-full", "Unter dem vollen Schuss: ein gewaltiger tiefer Druckstoß, den man mehr spürt als hört, und die ganze Ladung Death Flame, die brüllend aus dem Rohr fährt und lange nachfaucht")
def cannon_fire_full(rng) -> np.ndarray:
    n = dsp.seconds(1.4)
    pressure = _thump(n, 74, 31, 0.32) * 1.2 + dsp.lowpass(_burst(rng, n, 0.0, 20, 140, 0.18, 1.5), 140)
    roar = _flame(rng, n, 0.01, 0.04, 0.55, 45, 1400, 12.0)
    # The roar breathes like a throat: the band opens and closes a few times as the flame leaves.
    t = dsp.time_axis(n)
    throat = dsp.swept_bandpass(dsp.brown(n, rng) + dsp.pink(n, rng) * 0.5, 300 + 500 * np.exp(-t / 0.35) * (1 + 0.3 * np.sin(2 * np.pi * 4 * t)), q=1.6)
    throat = throat / (np.std(throat) + 1e-9) * np.clip(t / 0.03, 0, 1) * np.exp(-t / 0.45)
    tongue = _tongue(rng, n, 0.01, 0.03, 0.3, 1000, 3400)
    x = pressure + roar * 0.7 + throat * 0.35 + tongue * 0.08
    x = _room(x, rng, 0.6, 0.14)
    return _finish(x, -18.5, -2.0, 0.1)
