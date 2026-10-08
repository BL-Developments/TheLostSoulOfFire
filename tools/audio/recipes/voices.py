"""Voices of the Lost Souls: what is left of a human voice in each enemy.

A small source-filter voice: a glottal pulse train (Rosenberg shape) with jitter, shimmer and
optional period doubling (the rough, broken voice of something that forgot how to speak), breath
noise, and vowel formants that slide over time. On top come the distortions that make each kind
its own and a little mad: the Hollow's voice rings through a porcelain mask and stutters, the
Burning's laughter breaks into crackle, the Devourer growls an octave below a man with its
prisoners wailing thinly above it.

Lore (docs/current/characters): the Hollow only grasps after people who leave; the Burning is an
unstable flame; the Devourer hungers for souls and carries the ones it ate.

Mono 48 kHz, short, peaks below -2 dBFS; takes differ by seed (author.py --seed).
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe

# Formant centres (Hz) of a few vowels for an adult voice: F1, F2, F3, F4.
VOWELS = {
    "a": (750, 1200, 2500, 3500),
    "o": (500, 850, 2400, 3300),
    "u": (330, 700, 2300, 3200),
    "e": (480, 1900, 2550, 3500),
    "i": (300, 2250, 2950, 3600),
    "aw": (620, 950, 2450, 3300),
}
GAINS = (1.0, 0.55, 0.28, 0.12)
QS = (5.0, 7.0, 9.0, 10.0)


def _finish(x: np.ndarray, lufs: float, peak: float = -2.0, fade_out: float = 0.04) -> np.ndarray:
    x = dsp.highpass(x, 40)
    x = dsp.fades(x, 0.002, fade_out)
    return dsp.normalise_loudness(x, lufs, peak_ceiling_db=peak)


def _room(x: np.ndarray, rng, seconds: float = 0.5, wet: float = 0.14, damping: float = 4500) -> np.ndarray:
    ir = dsp.impulse_response(seconds, rng, damping_hz=damping, predelay=0.006)
    return dsp.reverb(x, ir, wet=wet).mean(axis=1)


def _track(n: int, points: list[tuple[float, float]]) -> np.ndarray:
    """A contour through (time share, value) points, smoothly interpolated over n samples."""
    t = np.linspace(0, 1, n)
    at, values = zip(*points)
    return np.interp(t, at, values)


def _vowel_track(n: int, points: list[tuple[float, str]], shift: float = 1.0) -> list[np.ndarray]:
    """Per formant, its centre over time (Hz), gliding between the given vowels."""
    return [_track(n, [(at, VOWELS[v][k] * shift) for at, v in points]) for k in range(4)]


def glottis(rng, f0: np.ndarray, jitter: float = 0.01, shimmer: float = 0.1, doubling: float = 0.0,
            open_quotient: float = 0.6) -> np.ndarray:
    """Glottal flow derivative for a pitch contour: Rosenberg pulses, pitch and level wobbling per
    period, every second period weaker by `doubling` (a rough, broken voice)."""
    n = len(f0)
    # Fast jitter per period and a slow wander: no voice holds a pitch like an instrument does.
    wobble = 1 + jitter * (dsp.smooth_random(n, 40.0, rng) - 0.5) * 3 + jitter * (dsp.smooth_random(n, 5.0, rng) - 0.5) * 4
    phase = np.cumsum(f0 * wobble) / dsp.RATE
    period = np.floor(phase)
    p = phase - period
    rising = p < open_quotient * 0.75
    flow = np.where(rising, 0.5 * (1 - np.cos(np.pi * p / (open_quotient * 0.75))),
                    np.where(p < open_quotient, np.cos(0.5 * np.pi * (p - open_quotient * 0.75) / (open_quotient * 0.25)), 0.0))
    level = 1 + shimmer * (dsp.smooth_random(n, 30.0, rng) - 0.5) * 3
    if doubling > 0:
        level *= np.where(period % 2 == 0, 1.0, 1.0 - doubling)
    source = np.diff(flow * level, prepend=0.0) * dsp.RATE / np.maximum(f0, 30) * 0.05
    return dsp.lowpass(source, 7000)


def voice(rng, f0: np.ndarray, vowels: list[np.ndarray], loudness: np.ndarray, breath: float = 0.2,
          jitter: float = 0.01, shimmer: float = 0.1, doubling: float = 0.0, q_scale: float = 1.0,
          rough: float = 0.0, chaos: float = 0.0) -> np.ndarray:
    """A voice through gliding formants: voiced source plus breath, shaped by `loudness`.
    `rough` shakes the source's level at 25-90 Hz (a rasp, a growl): what keeps a synthetic
    voice from sounding like a horn. `chaos` is a strained scream: a second, unrelated pitch
    beats against the first (biphonation) and turbulent noise rides each pulse."""
    n = len(f0)
    source = glottis(rng, f0, jitter, shimmer, doubling)
    source /= np.std(source) + 1e-9
    if rough > 0:
        rasp = dsp.bandpass(rng.standard_normal(n), 25, 90)
        source *= np.clip(1 + rough * rasp / (np.std(rasp) + 1e-9), 0, None)
    if chaos > 0:
        second = glottis(rng, f0 * rng.uniform(1.32, 1.48), jitter * 2, shimmer, 0.0)
        source = source + chaos * second / (np.std(second) + 1e-9)
        turbulence = dsp.highpass(rng.standard_normal(n), 600) * np.abs(source) / (np.std(source) + 1e-9)
        source = source * (1 - 0.4 * chaos) + turbulence * chaos * 0.9
    air = dsp.highpass(rng.standard_normal(n), 400)
    # Breath rides the glottal cycle a little (pulsed aspiration), and stays under the whole call.
    phase = (np.cumsum(f0) / dsp.RATE) % 1.0
    air *= breath * (0.6 + 0.4 * (phase < 0.5))
    excitation = (source + air) * loudness
    out = np.zeros(n)
    for centre, gain, q in zip(vowels, GAINS, QS):
        out += dsp.swept_bandpass(excitation, centre, q=q * q_scale) * gain
    return out


def scream(rng, f0: np.ndarray, vowels: list[np.ndarray], loudness: np.ndarray, noise_share: float = 0.55,
           rasp_hz: float = 90.0, rasp: float = 0.5, drive: float = 3.0, q_scale: float = 0.5) -> np.ndarray:
    """A strained cry: half voice, half turbulent air through wide open formants, the voice
    rattled at 75-110 Hz (the roughness of a real scream) and driven hard. A clean pulse train
    through formants at this pitch reads as a horn; the air and the rasp make it a throat."""
    n = len(f0)
    t = dsp.time_axis(n)
    source = glottis(rng, f0, 0.06, 0.3, 0.3)
    source /= np.std(source) + 1e-9
    shake = 1 + rasp * np.sin(2 * np.pi * rasp_hz * t + 3 * (dsp.smooth_random(n, 8.0, rng) - 0.5))
    air = dsp.highpass(rng.standard_normal(n), 300)
    excitation = (source * shake * (1 - noise_share) + air * noise_share * 1.2) * loudness
    out = np.zeros(n)
    for centre, gain, q in zip(vowels, GAINS, QS):
        out += dsp.swept_bandpass(excitation, centre, q=q * q_scale) * gain
    return dsp.soft_clip(out / (np.std(out) + 1e-9) * 0.5 * drive, drive)


def _stutter(x: np.ndarray, rng, at: float, grain: float, repeats: int) -> np.ndarray:
    """Repeat a short grain in place, as if the voice catches and skips (a little mad)."""
    start, m = dsp.seconds(at), dsp.seconds(grain)
    if start + m * (repeats + 1) >= len(x):
        return x
    piece = x[start:start + m] * np.hanning(m) ** 0.3
    out = x.copy()
    tail = x[start + m:].copy()
    for k in range(repeats):
        out[start + m * (k + 1):start + m * (k + 2)] = piece * (0.9 ** (k + 1))
    rest = out[start + m * (repeats + 1):]
    rest[:] = tail[:len(rest)]
    return out


def _mask(x: np.ndarray, rng) -> np.ndarray:
    """The porcelain mask: a hard, ringing shell in front of the mouth."""
    shell = dsp.resonator(x, rng.uniform(1050, 1250), 7) * 0.5 + dsp.resonator(x, rng.uniform(2250, 2550), 9) * 0.3
    return x * 0.6 + shell


# ---- Hollow -----------------------------------------------------------------------------------

@recipe("hollow-call", "Hollow ruft: ein gebrochenes, hauchiges Klagen durch die Porzellanmaske, als riefe es jemandem nach, der geht; die Stimme hakt und stottert")
def hollow_call(rng) -> np.ndarray:
    length = rng.uniform(0.95, 1.25)
    n = dsp.seconds(length)
    base = rng.uniform(150, 190)
    f0 = _track(n, [(0, base * 1.05), (0.15, base * 1.25), (0.55, base * 1.0), (1, base * 0.72)])
    f0 *= 1 + 0.025 * np.sin(2 * np.pi * rng.uniform(4.5, 6.0) * dsp.time_axis(n))
    vowels = _vowel_track(n, [(0, "o"), (0.4, "aw"), (1, "u")], shift=rng.uniform(1.0, 1.08))
    loud = _track(n, [(0, 0), (0.08, 0.9), (0.5, 0.7), (0.85, 0.35), (1, 0)])
    x = voice(rng, f0, vowels, loud, breath=0.55, jitter=0.02, shimmer=0.2, doubling=0.15, rough=0.3)
    x = _stutter(x, rng, rng.uniform(0.12, 0.2), rng.uniform(0.05, 0.07), int(rng.integers(2, 4)))
    x = _mask(x, rng)
    return _finish(_room(x, rng, 0.5, 0.14), -22.0, -3.0, 0.08)


@recipe("hollow-grasp", "Hollow greift zu: ein scharfes, verzweifeltes Einatmen, dann ein abgerissener Schrei durch die Maske – nicht gehen!")
def hollow_grasp(rng) -> np.ndarray:
    n = dsp.seconds(0.42)
    t = dsp.time_axis(n)
    gasp = dsp.swept_bandpass(rng.standard_normal(n), 900 + 1600 * np.clip(t / 0.08, 0, 1), q=3.0)
    gasp *= np.exp(-((t - 0.04) / 0.035) ** 2) * 0.5
    base = rng.uniform(260, 320)
    f0 = _track(n, [(0, base * 0.9), (0.25, base * 1.35), (0.6, base * 1.25), (1, base * 0.9)])
    vowels = _vowel_track(n, [(0, "a"), (0.5, "e"), (1, "a")], shift=rng.uniform(1.0, 1.1))
    loud = _track(n, [(0, 0), (0.18, 0), (0.26, 1.0), (0.7, 0.7), (1, 0)])
    f0 *= 1 + 0.05 * np.sin(2 * np.pi * 10 * t)
    cry = scream(rng, f0, vowels, loud, rasp_hz=rng.uniform(75, 100))
    x = gasp + cry * 0.7 + dsp.resonator(cry, rng.uniform(1050, 1250), 7) * 0.12
    return _finish(_room(x, rng, 0.45, 0.12), -19.0, -2.5, 0.05)


# ---- Burning ----------------------------------------------------------------------------------

def _crackle(rng, n: int, count: int, gain: float) -> np.ndarray:
    out = np.zeros(n)
    for _ in range(count):
        at = int(rng.integers(0, n - 400))
        m = int(rng.integers(30, 260))
        pop = dsp.bandpass(rng.standard_normal(m), 1500, 9000) * np.exp(-np.arange(m) / (m * 0.25))
        out[at:at + m] += pop * rng.uniform(0.3, 1.0) * gain
    return out


@recipe("burning-cackle", "Burning lacht: ein irres, abgehacktes Kichern, das in Knistern zerbricht, als lache die Glut selbst")
def burning_cackle(rng) -> np.ndarray:
    # Syllables of uneven length, each a short "ha" falling in pitch; the run speeds up, sinks and
    # loses breath, and ends in a high squeal, as a laugh that cannot stop does.
    count = int(rng.integers(5, 8))
    lengths = rng.uniform(0.085, 0.15, count) * np.linspace(1.15, 0.8, count)
    lengths = np.append(lengths, rng.uniform(0.22, 0.3))
    starts = np.concatenate([[0.02], 0.02 + np.cumsum(lengths)[:-1]])
    n = dsp.seconds(starts[-1] + lengths[-1] + 0.2)
    t = dsp.time_axis(n)
    base = rng.uniform(300, 380)
    f0 = np.full(n, base)
    gate = np.zeros(n)
    for k, (at, length) in enumerate(zip(starts, lengths)):
        a, m = dsp.seconds(at), dsp.seconds(length)
        u = np.linspace(0, 1, m)
        last = k == count
        level = rng.uniform(0.6, 1.0) * (1.0 - 0.35 * k / count) if not last else 0.85
        pitch = base * (rng.uniform(0.85, 1.35) if not last else 1.9) * (1 - 0.25 * k / count)
        f0[a:a + m] = pitch * ((1.15 - 0.3 * u) if not last else (1.0 + 0.25 * np.sin(np.pi * u)))
        gate[a:a + m] = np.sin(np.pi * np.clip(u / 0.75, 0, 1)) ** 0.8 * level
    f0 = dsp.lowpass(f0, 60)
    vowels = _vowel_track(n, [(0, "a"), (0.8, "e"), (1, "i")], shift=rng.uniform(1.1, 1.25))
    x = voice(rng, f0, vowels, gate, breath=0.75, jitter=0.05, shimmer=0.3, doubling=0.25, q_scale=0.75, rough=0.5)
    x = dsp.soft_clip(x / (np.std(x) + 1e-9) * 0.7, 2.2)
    # The flame breaks through: crackle where the laugh is loudest, a hiss under it.
    crackle = _crackle(rng, n, int(rng.integers(40, 60)), 0.6) * (0.3 + gate)
    hiss = dsp.bandpass(rng.standard_normal(n), 2500, 7000) * gate * 0.08
    x = x + crackle + hiss
    return _finish(_room(x, rng, 0.4, 0.1), -21.0, -3.0, 0.06)


@recipe("burning-shriek", "Burning schreit auf, bevor es losstürmt: ein ansteigender, irrer Schrei, der in ein Fauchen von Feuer übergeht")
def burning_shriek(rng) -> np.ndarray:
    n = dsp.seconds(0.75)
    t = dsp.time_axis(n)
    base = rng.uniform(330, 400)
    f0 = _track(n, [(0, base), (0.55, base * 1.9), (1, base * 1.6)])
    f0 *= 1 + 0.04 * np.sin(2 * np.pi * rng.uniform(9, 12) * t)
    vowels = _vowel_track(n, [(0, "a"), (0.5, "e"), (1, "i")], shift=rng.uniform(1.1, 1.2))
    loud = _track(n, [(0, 0), (0.1, 0.6), (0.6, 1.0), (1, 0)])
    cry = scream(rng, f0, vowels, loud, rasp_hz=rng.uniform(85, 110))
    roar = dsp.lowpass(dsp.pink(n, rng), 2400) * _track(n, [(0, 0), (0.5, 0.3), (0.85, 1.0), (1, 0)]) * 0.6
    crackle = _crackle(rng, n, 50, 0.5) * _track(n, [(0, 0.2), (1, 1.0)])
    x = cry + roar / (np.std(roar) + 1e-9) * 0.35 + crackle
    return _finish(_room(x, rng, 0.45, 0.12), -19.0, -2.5, 0.06)


# ---- Devourer ---------------------------------------------------------------------------------

def _prisoners(rng, n: int, gain: float) -> np.ndarray:
    """The souls it ate, wailing thin and high inside it, out of tune with each other."""
    out = np.zeros(n)
    for k in range(3):
        base = rng.uniform(380, 620)
        f0 = _track(n, [(0, base), (rng.uniform(0.3, 0.7), base * rng.uniform(1.1, 1.4)), (1, base * rng.uniform(0.8, 1.0))])
        vowels = _vowel_track(n, [(0, "u"), (1, "o")], shift=1.3)
        loud = _track(n, [(0, 0), (0.25, 0.6), (0.75, 0.5), (1, 0)])
        out += voice(rng, f0, vowels, loud, breath=0.8, jitter=0.02, shimmer=0.2)
    return dsp.bandpass(out, 500, 3000) / (np.std(out) + 1e-9) * gain


@recipe("devourer-growl", "Devourer grollt: ein tiefes, kehliges Knurren eine Oktave unter einem Menschen, darin dünn die Klagen der Seelen, die es verschlungen hat")
def devourer_growl(rng) -> np.ndarray:
    length = rng.uniform(1.2, 1.5)
    n = dsp.seconds(length)
    base = rng.uniform(52, 64)
    f0 = _track(n, [(0, base * 0.9), (0.3, base * 1.15), (0.7, base * 1.05), (1, base * 0.8)])
    vowels = _vowel_track(n, [(0, "o"), (0.5, "aw"), (1, "o")], shift=0.62)
    loud = _track(n, [(0, 0), (0.15, 0.85), (0.6, 1.0), (1, 0)])
    growl = voice(rng, f0, vowels, loud, breath=0.4, jitter=0.06, shimmer=0.4, doubling=0.55, q_scale=0.7, rough=0.4)
    growl = dsp.soft_clip(growl / (np.std(growl) + 1e-9) * 0.7, 2.0)
    wet = dsp.lowpass(rng.standard_normal(n), 900) * loud * 0.15 * (1 + np.sin(2 * np.pi * 13 * dsp.time_axis(n)))
    x = growl + wet + _prisoners(rng, n, 0.18)
    return _finish(_room(x, rng, 0.8, 0.16, 3000), -20.0, -2.5, 0.1)


@recipe("devourer-hunger", "Devourer wittert eine Seele: gieriges Schnüffeln, ein langes hungriges Stöhnen, die gefangenen Seelen schreien auf")
def devourer_hunger(rng) -> np.ndarray:
    n = dsp.seconds(1.6)
    t = dsp.time_axis(n)
    sniffs = np.zeros(n)
    for k in range(3):
        at = 0.04 + k * 0.13
        m = dsp.seconds(0.08)
        s = dsp.bandpass(rng.standard_normal(m), 600, 3500) * np.sin(np.pi * np.linspace(0, 1, m)) ** 2
        dsp.place(sniffs, s, dsp.seconds(at), 0.5)
    base = rng.uniform(70, 82)
    f0 = _track(n, [(0, base), (0.3, base), (0.55, base * 1.35), (1, base * 0.85)])
    vowels = _vowel_track(n, [(0, "u"), (0.4, "u"), (0.6, "o"), (1, "aw")], shift=0.7)
    loud = _track(n, [(0, 0), (0.3, 0), (0.42, 0.9), (0.85, 0.6), (1, 0)])
    moan = voice(rng, f0, vowels, loud, breath=0.5, jitter=0.06, shimmer=0.4, doubling=0.55, q_scale=0.7, rough=0.7)
    moan = dsp.soft_clip(moan / (np.std(moan) + 1e-9) * 0.6, 1.6)
    wail = _prisoners(rng, n, 0.35) * _track(n, [(0, 0), (0.45, 0), (0.6, 1.0), (1, 0.3)])
    x = sniffs + moan + wail
    return _finish(_room(x, rng, 0.8, 0.16, 3000), -21.0, -3.0, 0.1)
