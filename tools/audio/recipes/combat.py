"""Combat layers on top of the approved Ludo bank: what a blow sounds like on each material,
the wind-ups the enemies had no sound for, and how each enemy dies.

The Ludo cues stay the core of every action. These layers add what they could not carry:
- a contact transient at sample 0, so the first thing heard lands on the frame of the hit
  (scythe_hit and player_hit open with 40-55 ms of near silence);
- the material of the target (porcelain and cloth, ember crust, flesh and void, wood);
- warnings for the Hollow's and the Devourer's wind-ups, which were silent until the blow.

Mono 48 kHz, peaks below -2 dBFS, short tails; variants come from the seed (author.py --seed).
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe


def _finish(x: np.ndarray, lufs: float, peak: float = -2.0, fade_out: float = 0.03) -> np.ndarray:
    x = dsp.highpass(x, 28)
    x = dsp.fades(x, 0.0005, fade_out)
    return dsp.normalise_loudness(x, lufs, peak_ceiling_db=peak)


def _room(x: np.ndarray, rng, seconds: float = 0.5, wet: float = 0.15, damping: float = 4500) -> np.ndarray:
    ir = dsp.impulse_response(seconds, rng, damping_hz=damping, predelay=0.008)
    return dsp.reverb(x, ir, wet=wet).mean(axis=1)


def _burst(rng, n: int, at: float, low: float, high: float, decay: float, gain: float = 1.0) -> np.ndarray:
    """Band-limited noise with an instant attack and an exponential decay, placed at `at` seconds."""
    out = np.zeros(n)
    start = dsp.seconds(at)
    m = min(n - start, dsp.seconds(decay * 7))
    if m <= 0:
        return out
    noise = dsp.bandpass(rng.standard_normal(m), low, high)
    out[start:start + m] = noise * np.exp(-np.arange(m) / (dsp.RATE * decay)) * gain
    return out


def _thump(n: int, start_hz: float, end_hz: float, decay: float, at: float = 0.0) -> np.ndarray:
    """A pitched body blow: a sine that drops in pitch while it decays (weight without boom)."""
    out = np.zeros(n)
    start = dsp.seconds(at)
    m = n - start
    t = np.arange(m) / dsp.RATE
    freq = end_hz + (start_hz - end_hz) * np.exp(-t / (decay * 0.6))
    out[start:] = np.sin(2 * np.pi * np.cumsum(freq) / dsp.RATE) * np.exp(-t / decay)
    return out


def _grains(rng, n: int, start: float, span: float, count: int, low: float, high: float, gain: float,
            length: tuple[float, float] = (0.002, 0.008)) -> np.ndarray:
    """Scattered tiny noise grains (crackle, grit, splinters), thinning out over `span`."""
    out = np.zeros(n)
    for _ in range(count):
        at = dsp.seconds(start + rng.exponential(span / 3))
        m = dsp.seconds(rng.uniform(*length))
        if at + m >= n or m < 8:
            continue
        grain = dsp.bandpass(rng.standard_normal(m), low, high) * np.hanning(m)
        out[at:at + m] += grain * rng.uniform(0.25, 1.0) * gain * np.exp(-(at / dsp.RATE - start) / span)
    return out


def _ring(n: int, partials: list[tuple[float, float, float]], rng, at: float = 0.0) -> np.ndarray:
    """Inharmonic ringing partials (freq, amplitude, decay seconds): ceramic, iron, glass."""
    out = np.zeros(n)
    start = dsp.seconds(at)
    t = np.arange(n - start) / dsp.RATE
    for freq, amp, decay in partials:
        f = freq * rng.uniform(0.97, 1.03)
        out[start:] += np.sin(2 * np.pi * f * t + rng.uniform(0, 6.28)) * amp * np.exp(-t / decay)
    return out


# ---- contact layers by material ---------------------------------------------------------------

def _body(rng, n: int, low: float, high: float, decay: float, gain: float, at: float = 0.0) -> np.ndarray:
    """Weight without pitch: low band noise that hits at once and dies fast (a sine here reads as a drum)."""
    return dsp.lowpass(_burst(rng, n, at, low, high, decay, gain), high)


@recipe("hit-hollow", "Treffer auf Hollow: dumpfer Schlag in schweren Stoff, hohler Körper, Ascheknistern, ein feiner Porzellanknack")
def hit_hollow(rng) -> np.ndarray:
    n = dsp.seconds(0.32)
    thwack = _burst(rng, n, 0.0, 300, 3600, 0.022, 1.0)
    flap = _grains(rng, n, 0.004, 0.05, int(rng.integers(6, 10)), 500, 3000, 0.45, (0.006, 0.016))
    body = _body(rng, n, 60, 380, 0.03, 0.55)
    hollow = dsp.resonator(_burst(rng, n, 0.0, 200, 2000, 0.006, 1.0), rng.uniform(520, 640), 2.5) * 0.35
    ash = _grains(rng, n, 0.008, 0.11, int(rng.integers(30, 45)), 2500, 9500, 0.4)
    porcelain = _ring(n, [(2350, 0.16, 0.022), (3720, 0.12, 0.016), (5480, 0.08, 0.011)], rng, at=rng.uniform(0.004, 0.012))
    x = thwack + flap + body + hollow + ash + porcelain
    return _finish(_room(x, rng, 0.4, 0.12), -18.0, -2.5)


@recipe("hit-burning", "Treffer auf Burning: die verkohlte Kruste bricht, Glut springt knisternd heraus, kurzes Zischen")
def hit_burning(rng) -> np.ndarray:
    n = dsp.seconds(0.42)
    t = dsp.time_axis(n)
    crust = _burst(rng, n, 0.0, 900, 6000, 0.01, 0.9)
    crack = _grains(rng, n, 0.0, 0.035, int(rng.integers(14, 22)), 1500, 8000, 1.0, (0.001, 0.004))
    embers = _grains(rng, n, 0.015, 0.25, int(rng.integers(70, 110)), 2000, 11000, 0.7, (0.0008, 0.003))
    hiss = dsp.highpass(rng.standard_normal(n), 3500) * np.exp(-((t - 0.08) / 0.07) ** 2) * 0.22
    flare = dsp.bandpass(rng.standard_normal(n), 300, 1800) * dsp.envelope(n, 0.012, 0.08) * 0.4
    body = _body(rng, n, 60, 300, 0.025, 0.35)
    x = crust + crack + embers + hiss + flare + body
    return _finish(_room(x, rng, 0.45, 0.12), -18.0, -2.5)


@recipe("hit-devourer", "Treffer auf Devourer: schwerer dumpfer Schlag in Fleisch, ein nasses Nachgeben, darunter ein leeres Grollen")
def hit_devourer(rng) -> np.ndarray:
    n = dsp.seconds(0.55)
    t = dsp.time_axis(n)
    slap = _burst(rng, n, 0.0, 400, 3500, 0.012, 0.9)
    meat = _body(rng, n, 50, 700, 0.045, 1.2)
    give = dsp.bandpass(rng.standard_normal(n), 220, 1300) * np.exp(-((t - 0.04) / 0.035) ** 2) * 0.55
    give *= 1 + 0.7 * np.sin(2 * np.pi * rng.uniform(26, 38) * t)
    squelch = _grains(rng, n, 0.01, 0.08, int(rng.integers(8, 14)), 400, 2500, 0.35, (0.008, 0.02))
    void = dsp.lowpass(dsp.brown(n, rng), 160) * dsp.envelope(n, 0.04, 0.3) * 0.4
    x = slap + meat + give + squelch + void
    return _finish(_room(x, rng, 0.6, 0.14, 3000), -17.0, -2.0, 0.08)


@recipe("hit-dummy", "Treffer auf die Übungspuppe: Holz klopft hohl, Stroh raschelt, der Pfahl federt nach")
def hit_dummy(rng) -> np.ndarray:
    n = dsp.seconds(0.36)
    knock = sum(dsp.resonator(_burst(rng, n, 0.0, 300, 6000, 0.0015, 1.0), f * rng.uniform(0.96, 1.04), q) * g
                for f, q, g in ((780, 14, 0.7), (1290, 16, 0.5), (2150, 18, 0.3)))
    straw = _grains(rng, n, 0.004, 0.14, int(rng.integers(50, 80)), 2500, 9000, 0.45)
    body = _body(rng, n, 80, 500, 0.02, 0.4)
    x = _burst(rng, n, 0.0, 600, 5000, 0.004, 0.5) + knock + straw + body
    return _finish(_room(x, rng, 0.4, 0.12), -19.0, -3.0)


@recipe("hit-heavy", "Schwere Wucht unter Seelenspaltung und voller Kanone: ein tiefer, kurzer Druckstoß ohne Dröhnen")
def hit_heavy(rng) -> np.ndarray:
    n = dsp.seconds(0.4)
    punch = _thump(n, 90, 45, 0.05) * 0.5
    air = _body(rng, n, 30, 450, 0.05, 1.3)
    crack = _burst(rng, n, 0.0, 1500, 7000, 0.006, 0.5)
    x = punch + air + crack
    return _finish(_room(x, rng, 0.7, 0.16, 2500), -17.0, -2.0, 0.1)


@recipe("body-hit", "Spieler getroffen: sofortiger dumpfer Stoß in den Mantel, Stoff schlägt, kurzer Ruck")
def body_hit(rng) -> np.ndarray:
    n = dsp.seconds(0.24)
    coat = _burst(rng, n, 0.0, 250, 3500, 0.022, 1.0)
    body = _body(rng, n, 60, 400, 0.035, 0.6)
    flap = _grains(rng, n, 0.008, 0.06, int(rng.integers(8, 14)), 700, 4000, 0.5, (0.005, 0.014))
    x = coat + body + flap
    return _finish(_room(x, rng, 0.35, 0.1), -19.0, -2.5)


# ---- enemy wind-ups ---------------------------------------------------------------------------

@recipe("hollow-windup", "Hollow holt aus: die Maske knackt, ein rauer, hohler Atemzug durch Porzellan, Stoff spannt sich")
def hollow_windup(rng) -> np.ndarray:
    n = dsp.seconds(0.44)
    t = dsp.time_axis(n)
    creak = _ring(n, [(1850, 0.12, 0.02), (2900, 0.08, 0.015)], rng) + _burst(rng, n, 0.0, 1500, 6000, 0.004, 0.4)
    # A rasping inhale: noise through two hollow formants that rise as the arm draws back.
    rise = np.clip(t / 0.4, 0, 1)
    breath = rng.standard_normal(n)
    breath = dsp.swept_bandpass(breath, 520 + 380 * rise, q=4.0) * 0.8 + dsp.swept_bandpass(breath, 1300 + 700 * rise, q=5.0) * 0.4
    breath *= np.sin(np.pi * np.clip(t / 0.42, 0, 1)) ** 1.3 * (0.6 + 0.4 * rise)
    rasp = 1 + 0.5 * np.sin(2 * np.pi * (34 + 20 * rise) * t)
    cloth = dsp.bandpass(rng.standard_normal(n), 900, 5000) * np.exp(-((t - 0.3) / 0.1) ** 2) * 0.2
    x = creak * 0.6 + breath * rasp + cloth
    return _finish(_room(x, rng, 0.5, 0.16), -22.0, -4.0, 0.03)


@recipe("devourer-windup", "Devourer holt aus: ein tiefes Einsaugen, der Schlund knarrt, gefangene Seelen flüstern ansteigend")
def devourer_windup(rng) -> np.ndarray:
    n = dsp.seconds(0.86)
    t = dsp.time_axis(n)
    rise = np.clip(t / 0.8, 0, 1)
    inhale = dsp.swept_bandpass(rng.standard_normal(n), 180 + 420 * rise ** 1.5, q=2.5) * np.clip(t / 0.12, 0, 1) * (0.25 + 0.75 * rise) * 0.8
    groan = np.sin(2 * np.pi * np.cumsum(46 + 20 * rise) / dsp.RATE) * rise ** 1.3 * 0.5
    groan *= 1 + 0.3 * np.sin(2 * np.pi * 7 * t)
    groan = dsp.soft_clip(groan * 1.6, 1.2) * 0.6
    whispers = np.zeros(n)
    for f in (880, 1170, 1480):
        whispers += dsp.resonator(rng.standard_normal(n), f * rng.uniform(0.97, 1.03), 18) * 0.05
    whispers *= rise ** 2
    strain = _grains(rng, n, 0.2, 0.6, 14, 300, 1800, 0.18, (0.01, 0.03))
    x = inhale + groan + whispers + strain
    x *= np.clip((0.86 - t) / 0.05, 0, 1)  # cut just before the blow lands
    return _finish(_room(x, rng, 0.8, 0.2, 3000), -21.0, -3.0, 0.02)


@recipe("ground-break", "Der Boden bricht unter dem Devourer-Schlag: ein tiefer Einschlag, Stein birst und splittert nach außen, Platten mahlen, Geröll prasselt nieder")
def ground_break(rng) -> np.ndarray:
    n = dsp.seconds(1.7)
    t = dsp.time_axis(n)
    # The blow itself: a deep falling thump and a short pressure wave, no boom tail.
    thump = _thump(n, 70, 34, 0.16) * 0.9 + _body(rng, n, 25, 220, 0.09, 1.1)
    # The floor fractures outward: a cluster of sharp cracks over the first 120 ms, each a
    # broadband snap with a little stony ring, getting smaller and further apart.
    cracks = np.zeros(n)
    for k in range(int(rng.integers(7, 11))):
        at = 0.002 + rng.exponential(0.035) + k * 0.006
        if at > 0.16:
            continue
        snap = _burst(rng, n, at, 900, 7500, rng.uniform(0.003, 0.008), rng.uniform(0.5, 1.0) * (1 - at * 3))
        snap += dsp.resonator(_burst(rng, n, at, 400, 4000, 0.002, 1.0), rng.uniform(900, 1700), 6) * 0.25
        cracks += snap
    # Slabs grinding against each other as they settle.
    grind = dsp.bandpass(rng.standard_normal(n), 70, 520) * dsp.envelope(n, 0.02, 0.35) * 0.55
    grind *= 1 + 0.8 * np.abs(dsp.lowpass(rng.standard_normal(n), 18)) / 0.12
    # Rubble thrown up rains back: stones and grit patter over a second, thinning out.
    stones = _grains(rng, n, 0.18, 0.7, int(rng.integers(26, 38)), 700, 4200, 0.55, (0.004, 0.012))
    grit = _grains(rng, n, 0.12, 0.9, int(rng.integers(90, 130)), 2500, 9000, 0.22, (0.0008, 0.003))
    x = thump + cracks * 0.8 + grind + stones + grit
    # Tame the crack transients so the weight carries: the peaks would otherwise set the level.
    x = dsp.soft_clip(x / np.max(np.abs(x)) * 2.4, 1.0)
    return _finish(_room(x, rng, 0.9, 0.12, 3500), -18.0, -2.0, 0.25)


@recipe("burning-rush", "Burning stürmt los: Glut faucht auf, Luft reißt, ein brüllendes Feuerrauschen, das mitläuft")
def burning_rush(rng) -> np.ndarray:
    n = dsp.seconds(0.62)
    t = dsp.time_axis(n)
    roar = dsp.lowpass(dsp.pink(n, rng), 1600) * dsp.envelope(n, 0.03, 0.45) * 0.9
    roar *= 1 + 0.35 * dsp.lowpass(rng.standard_normal(n), 25) / 0.15
    whoosh = dsp.swept_bandpass(rng.standard_normal(n), 2400 - 1600 * np.clip(t / 0.5, 0, 1), q=1.5) * dsp.envelope(n, 0.02, 0.3) * 0.6
    crackle = _grains(rng, n, 0.0, 0.5, 60, 2000, 10000, 0.3, (0.0008, 0.003))
    kick = _thump(n, 100, 55, 0.05) * 0.7
    x = roar + whoosh + crackle + kick
    return _finish(_room(x, rng, 0.5, 0.12), -19.0, -3.0, 0.08)


# ---- deaths ------------------------------------------------------------------------------------

def _shard(rng, n: int, at: float, gain: float, q: float = 14) -> np.ndarray:
    """A piece of porcelain hit or breaking: a burst of grit ringing a few short, rough modes
    (resonators excited by noise: it knocks and cracks; clean sine partials read as a chime)."""
    out = np.zeros(n)
    start = dsp.seconds(at)
    m = min(n - start, dsp.seconds(0.06))
    if m <= 0:
        return out
    grit = rng.standard_normal(m) * np.exp(-np.arange(m) / (dsp.RATE * 0.0025))
    modes = sum(dsp.resonator(grit, f * rng.uniform(0.9, 1.1), q) * g for f, g in ((1900, 1.0), (3100, 0.7), (4700, 0.45), (6900, 0.25)))
    out[start:start + m] = (modes / (np.max(np.abs(modes)) + 1e-9) + dsp.highpass(grit, 2500) * 0.6) * gain
    return out


@recipe("death-hollow", "Hollow stirbt: die Porzellanmaske bricht mit trockenem Knack und Splittern, der Stoff fällt in sich zusammen, Scherben klappern auf den Boden; kein Glöckchen")
def death_hollow(rng) -> np.ndarray:
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    crack = _shard(rng, n, 0.0, 1.0) + _shard(rng, n, rng.uniform(0.008, 0.016), 0.6)
    crack += _burst(rng, n, 0.0, 2000, 9000, 0.005, 0.2)
    splinters = _grains(rng, n, 0.003, 0.06, 26, 3000, 10000, 0.35, (0.001, 0.003))
    collapse = dsp.lowpass(rng.standard_normal(n), 1800) * np.exp(-((t - 0.32) / 0.14) ** 2) * 0.45
    collapse += _thump(n, 90, 55, 0.08, at=0.42) * 0.5
    clatter = np.zeros(n)
    for k, at in enumerate((0.48, 0.56, 0.61, 0.69)):
        clatter += _shard(rng, n, at + rng.uniform(-0.01, 0.01), 0.35 / (k + 1), q=10)
    x = crack + splinters + collapse + clatter
    x = dsp.fades(dsp.highpass(_room(x, rng, 0.7, 0.14), 28), 0.0005, 0.1)
    return dsp.loud_and_limited(x, -19.0, -2.5)


@recipe("death-burning", "Burning erlischt: die Flammen fallen mit einem tiefen Fauchen in sich zusammen, Glut zischt aus, letzte Funken")
def death_burning(rng) -> np.ndarray:
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    gasp = dsp.lowpass(dsp.pink(n, rng), 1200) * dsp.envelope(n, 0.01, 0.25) * 0.8
    out = dsp.swept_bandpass(rng.standard_normal(n), 1800 * np.exp(-t / 0.3) + 200, q=1.2) * dsp.envelope(n, 0.02, 0.4) * 0.5
    hiss = dsp.highpass(rng.standard_normal(n), 4000) * np.exp(-t / 0.35) * 0.15
    sparks = _grains(rng, n, 0.05, 0.6, 35, 2500, 11000, 0.25, (0.0008, 0.003))
    settle = _thump(n, 80, 50, 0.07, at=0.35) * 0.4
    x = gasp + out + hiss + sparks + settle
    return _finish(_room(x, rng, 0.6, 0.15), -20.0, -3.0, 0.15)


@recipe("death-devourer", "Devourer bricht: ein schwerer Fall, das Gefängnis reißt auf, gefangene Seelen steigen als gläserner Chor auf")
def death_devourer(rng) -> np.ndarray:
    n = dsp.seconds(1.4)
    t = dsp.time_axis(n)
    fall = _thump(n, 90, 38, 0.16) * 1.0 + dsp.lowpass(_burst(rng, n, 0.0, 30, 500, 0.06, 1.2), 400)
    tear = dsp.swept_bandpass(rng.standard_normal(n), 300 + 1500 * np.clip((t - 0.05) / 0.4, 0, 1), q=2.0)
    tear *= np.exp(-((t - 0.25) / 0.15) ** 2) * 0.5
    choir = np.zeros(n)
    for k, f in enumerate((523.25, 659.25, 783.99, 1046.5)):
        rise = np.clip((t - 0.25 - k * 0.08) / 0.5, 0, 1)
        voice = dsp.resonator(rng.standard_normal(n), f * rng.uniform(0.995, 1.005), 60) * 0.06
        voice += np.sin(2 * np.pi * f * t) * 0.05
        choir += voice * rise * np.exp(-np.maximum(t - 0.9, 0) / 0.25)
    x = fall + tear + choir
    return _finish(_room(x, rng, 1.2, 0.25, 3500), -18.0, -2.0, 0.25)


# ---- weight under the scythe swings -----------------------------------------------------------
# The Ludo swings are light whooshes. Under each the stroke's weight: a heavy air displacement that
# peaks at the moment of contact (ScytheCombat strike times 0.062 / 0.085 / 0.155 s after the
# swing starts, which is when the cue plays), the Death Flame flaring as the fast stroke begins
# (StrokeStart: 0.02 / 0.03 / 0.10 s) and a short, dark ring of the big blade.

def _swing_weight(rng, length: float, stroke: float, contact: float, low: float, high: float, flare: float,
                  sub: float = 0.0) -> np.ndarray:
    n = dsp.seconds(length)
    t = dsp.time_axis(n)
    # Air: band noise whose centre sweeps up through the stroke (the blade passing close), with
    # a fast rise into contact and a slower fall after it.
    rise = np.clip((t - stroke * 0.5) / max(1e-3, contact - stroke * 0.5), 0, 1) ** 2.2
    fall = np.exp(-np.clip(t - contact, 0, None) / 0.07)
    envelope = rise * fall
    centre = low + (high - low) * np.clip((t - stroke) / max(1e-3, contact + 0.06 - stroke), 0, 1)
    air = dsp.swept_bandpass(rng.standard_normal(n), centre, q=1.6) * envelope
    air = air / (np.max(np.abs(air)) + 1e-9)
    # The flame flaring: a low whump with a little crackle as the stroke begins.
    flame = dsp.lowpass(_burst(rng, n, stroke, 70, 520, 0.06, 1.0), 600)
    flame += _grains(rng, n, stroke + 0.01, 0.12, int(rng.integers(6, 12)), 1500, 6000, 0.25)
    flame = flame / (np.max(np.abs(flame)) + 1e-9)
    # The blade: a short, dark metallic ring at contact (no bright ping).
    blade = _ring(n, [(rng.uniform(1100, 1300), 0.5, 0.05), (rng.uniform(1900, 2200), 0.3, 0.035),
                      (rng.uniform(3100, 3400), 0.12, 0.02)], rng, at=contact - 0.01)
    x = air * 1.0 + flame * flare + blade * 0.18
    if sub > 0:
        x += _thump(n, 75, 38, 0.09, at=contact) * sub
    return x


@recipe("scythe-weight-1", "Wucht unter dem schnellen Schnitt: schwerer Luftstoß zur Kontaktzeit, die Death Flame lodert auf, dunkler Klingenklang")
def scythe_weight_1(rng) -> np.ndarray:
    x = _swing_weight(rng, 0.42, 0.02, 0.062, 160, 900, 0.45)
    return _finish(_room(x, rng, 0.4, 0.12, 4000), -18.0, -2.0, 0.08)


@recipe("scythe-weight-2", "Wucht unter der Rückhand: schwerer und länger, die Flamme lodert stärker")
def scythe_weight_2(rng) -> np.ndarray:
    x = _swing_weight(rng, 0.48, 0.03, 0.085, 140, 820, 0.6)
    return _finish(_room(x, rng, 0.45, 0.13, 3800), -17.0, -2.0, 0.08)


@recipe("scythe-weight-3", "Wucht unter der Seelenspaltung: ein gewaltiger Luftstoß, die Flamme bricht auf, ein tiefer Druckstoß beim Kontakt")
def scythe_weight_3(rng) -> np.ndarray:
    x = _swing_weight(rng, 0.7, 0.10, 0.155, 110, 760, 0.85, sub=0.9)
    return _finish(_room(x, rng, 0.6, 0.16, 3200), -15.5, -2.0, 0.12)
