"""One-shot cues the game had no sound for: footsteps, UI, chest, currency, abilities, doors.

Mono 48 kHz like the Ludo bank; peaks below -1 dBFS; short, clean onsets, no padding. Variants
of the same cue come from the seed (author.py --seed 1..n), so footsteps never repeat exactly.
"""
from __future__ import annotations

import math

import numpy as np

import dsp
from recipes import recipe


def _finish(x: np.ndarray, lufs: float, peak: float = -2.0) -> np.ndarray:
    x = dsp.highpass(x, 30)
    x = dsp.fades(x, 0.002, 0.02)
    return dsp.normalise_loudness(x, lufs, peak_ceiling_db=peak)


def _room(x: np.ndarray, rng, seconds: float = 0.6, wet: float = 0.18, damping: float = 5000) -> np.ndarray:
    ir = dsp.impulse_response(seconds, rng, damping_hz=damping, predelay=0.006)
    return dsp.reverb(x, ir, wet=wet).mean(axis=1)


def _impact(rng, n: int, at: int, low: float, high: float, decay: float, gain: float) -> np.ndarray:
    out = np.zeros(n)
    m = min(n - at, dsp.seconds(decay * 6))
    burst = dsp.bandpass(rng.standard_normal(m), low, high) * np.exp(-np.arange(m) / (dsp.RATE * decay))
    out[at:at + m] = burst * gain
    return out


def _crunch(rng, n: int, start: float, span: float, count: int, low: float, high: float, gain: float) -> np.ndarray:
    """Many tiny grains of grit under a sole: the texture that makes a step a step."""
    out = np.zeros(n)
    for _ in range(count):
        at = dsp.seconds(start + rng.exponential(span / 3))
        m = dsp.seconds(rng.uniform(0.002, 0.007))
        if at + m >= n:
            continue
        grain = dsp.bandpass(rng.standard_normal(m), low, high) * np.hanning(m)
        out[at:at + m] += grain * rng.uniform(0.2, 1.0) * gain * np.exp(-(at / dsp.RATE - start) / span)
    return out


@recipe("footstep-stone", "Schritt auf Stein: Absatz, Ballen, Knirschen von Sand und Asche unter der Sohle")
def footstep_stone(rng) -> np.ndarray:
    n = dsp.seconds(0.3)
    heel = _impact(rng, n, 0, 60, 1400, 0.018, 1.0)
    body = dsp.lowpass(_impact(rng, n, 0, 40, 300, 0.035, 1.2), 250)
    toe = _impact(rng, n, dsp.seconds(rng.uniform(0.06, 0.09)), 200, 2600, 0.012, 0.45)
    crunch = _crunch(rng, n, 0.005, 0.09, rng.integers(25, 45), 1200, 7000, 0.35)
    x = _room(heel + body + toe + crunch, rng, 0.35, 0.1)
    return _finish(x, -26.0, -6.0)


@recipe("footstep-wood", "Schritt auf Planken: hohler Aufsatz, Ballen, die Bohle gibt nach")
def footstep_wood(rng) -> np.ndarray:
    n = dsp.seconds(0.34)
    heel = _impact(rng, n, 0, 80, 1100, 0.02, 1.0)
    boards = sum(dsp.resonator(_impact(rng, n, 0, 60, 2000, 0.004, 1.0), f * rng.uniform(0.92, 1.08), 5) * g
                 for f, g in ((150, 0.5), (310, 0.3)))
    toe = _impact(rng, n, dsp.seconds(rng.uniform(0.07, 0.1)), 150, 1800, 0.016, 0.5)
    creak = _crunch(rng, n, 0.03, 0.12, rng.integers(8, 16), 500, 1500, 0.2)
    x = _room(heel + boards * 0.6 + toe + creak, rng, 0.3, 0.12)
    return _finish(x, -26.0, -6.0)


@recipe("step-hollow", "Hollow-Schritt: schleifende Sohle über Stein, schwerer Stoff, kaum Absatz")
def step_hollow(rng) -> np.ndarray:
    n = dsp.seconds(0.42)
    t = dsp.time_axis(n)
    # The foot is dragged rather than set down: a short scrape that swells and fades.
    scrape = dsp.bandpass(rng.standard_normal(n), 400, 3200) * np.sin(np.clip(t / 0.26, 0, 1) * np.pi) ** 1.5
    scrape *= 0.55 + 0.45 * dsp.lowpass(rng.standard_normal(n), 30) / 0.2
    thud = dsp.lowpass(_impact(rng, n, dsp.seconds(0.05), 40, 260, 0.04, 1.0), 220)
    cloth = dsp.bandpass(rng.standard_normal(n), 900, 5000) * np.exp(-((t - 0.12) / 0.07) ** 2) * 0.35
    grit = _crunch(rng, n, 0.04, 0.16, rng.integers(12, 24), 1500, 6000, 0.25)
    x = _room(scrape * 0.35 + thud * 0.8 + cloth + grit, rng, 0.4, 0.14)
    return _finish(x, -29.0, -8.0)


@recipe("step-burning", "Burning-Schritt: weicher Tritt unter knisternder Glut, kurzes Zischen auf dem Stein")
def step_burning(rng) -> np.ndarray:
    n = dsp.seconds(0.26)
    t = dsp.time_axis(n)
    # Mostly the embers: a soft footfall under crackle and a short sizzle where it touches stone.
    tap = _impact(rng, n, 0, 300, 2400, 0.008, 0.35)
    body = dsp.lowpass(_impact(rng, n, 0, 60, 400, 0.018, 0.25), 350)
    crackle = _crunch(rng, n, 0.0, 0.16, rng.integers(30, 50), 2000, 10000, 0.9)
    hiss = dsp.highpass(rng.standard_normal(n), 3000) * np.sin(np.clip(t / 0.18, 0, 1) * np.pi) * 0.22
    x = _room(tap + body + crackle + hiss, rng, 0.3, 0.1)
    return _finish(x, -28.0, -7.0)


@recipe("step-devourer", "Devourer-Schritt: schwerer Aufschlag, dumpfes Nachbeben, knirschender Stein")
def step_devourer(rng) -> np.ndarray:
    n = dsp.seconds(0.7)
    t = dsp.time_axis(n)
    thump = dsp.lowpass(_impact(rng, n, 0, 25, 180, 0.07, 1.4), 160)
    sub = np.sin(2 * np.pi * rng.uniform(42, 50) * t) * np.exp(-t / 0.12) * 0.6
    stone = _crunch(rng, n, 0.01, 0.2, rng.integers(30, 50), 600, 4000, 0.35)
    rattle = dsp.bandpass(rng.standard_normal(n), 1800, 6000) * np.exp(-((t - 0.05) / 0.04) ** 2) * 0.12
    x = _room(thump + sub + stone + rattle, rng, 0.7, 0.2, damping=3000)
    return _finish(x, -24.0, -4.0)


@recipe("ui-move", "Menü: leises gläsernes Klicken mit violettem Nachklang")
def ui_move(rng) -> np.ndarray:
    n = dsp.seconds(0.18)
    t = dsp.time_axis(n)
    tick = dsp.bandpass(rng.standard_normal(n), 2000, 7000) * np.exp(-t / 0.004)
    ring = sum(np.sin(2 * np.pi * f * t) * g * np.exp(-t / d) for f, g, d in ((1568, 0.5, 0.05), (2349, 0.25, 0.035), (3136, 0.12, 0.025)))
    return _finish(tick * 0.4 + ring, -30.0, -8.0)


@recipe("ui-back", "Menü zurück: tieferes, gedämpftes Klicken, nach unten")
def ui_back(rng) -> np.ndarray:
    n = dsp.seconds(0.2)
    t = dsp.time_axis(n)
    tick = dsp.bandpass(rng.standard_normal(n), 800, 3500) * np.exp(-t / 0.005)
    ring = sum(np.sin(2 * np.pi * f * (1 - 0.15 * t) * t) * g * np.exp(-t / d) for f, g, d in ((1046, 0.5, 0.05), (1568, 0.2, 0.03)))
    return _finish(tick * 0.4 + ring, -31.0, -8.0)


@recipe("ui-open", "Menü öffnen: ein kurzer, weicher Atemzug von Asche und Stoff, gläserner Schimmer")
def ui_open(rng) -> np.ndarray:
    n = dsp.seconds(0.45)
    t = dsp.time_axis(n)
    swell = dsp.bandpass(rng.standard_normal(n), 400, 3000) * np.sin(np.pi * np.clip(t / 0.4, 0, 1)) ** 2
    shimmer = sum(np.sin(2 * np.pi * f * t) * g for f, g in ((1244, 0.2), (1661, 0.12))) * np.clip((t - 0.1) / 0.1, 0, 1) * np.exp(-np.maximum(t - 0.2, 0) / 0.12)
    return _finish(swell * 0.6 + shimmer, -30.0, -8.0)


@recipe("ui-close", "Menü schließen: der Atemzug rückwärts, kürzer")
def ui_close(rng) -> np.ndarray:
    return ui_open(rng)[::-1][: dsp.seconds(0.32)].copy()


@recipe("chest-open", "Kiste: Eisenriegel springt, Deckel knarrt auf, ein Schimmer von Glut")
def chest_open(rng) -> np.ndarray:
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    latch = np.zeros(n)
    m = dsp.seconds(0.08)
    latch[:m] = sum(np.sin(2 * np.pi * f * dsp.time_axis(m)) * g for f, g in ((1830, 0.6), (4120, 0.3))) * np.exp(-dsp.time_axis(m) / 0.015)
    latch[:m] += dsp.bandpass(rng.standard_normal(m), 1500, 6000) * np.exp(-dsp.time_axis(m) / 0.006)
    creak = np.zeros(n)
    start = dsp.seconds(0.09)
    length = dsp.seconds(0.45)
    pulses = np.zeros(length)
    tt = 0.0
    while tt < 0.45:
        i = dsp.seconds(tt)
        if i < length:
            pulses[i] = rng.uniform(0.6, 1.0)
        tt += 1.0 / (40 + 30 * tt / 0.45)
    creak[start:start + length] = sum(dsp.resonator(pulses, f, 14) * g for f, g in ((300, 1.0), (640, 0.5), (1250, 0.2))) * np.sin(np.pi * np.linspace(0, 1, length))
    glow_start = dsp.seconds(0.3)
    glow = np.zeros(n)
    m = n - glow_start
    gt = dsp.time_axis(m)
    glow[glow_start:] = sum(np.sin(2 * np.pi * f * gt) * g for f, g in ((622, 0.35), (933, 0.2), (1244, 0.15))) * np.minimum(1, gt / 0.08) * np.exp(-gt / 0.35)
    x = _room(latch * 0.7 + creak * 0.25 + glow * 0.5, rng, 0.7, 0.2)
    return _finish(x, -20.0, -3.0)


@recipe("currency-gain", "Währung: ein kurzes Klirren mehrerer Münzen, ein heller Funke")
def currency_gain(rng) -> np.ndarray:
    n = dsp.seconds(0.5)
    out = np.zeros(n)
    for k in range(rng.integers(5, 9)):
        at = dsp.seconds(rng.uniform(0, 0.22))
        m = dsp.seconds(0.25)
        mt = dsp.time_axis(m)
        f = rng.uniform(2800, 4800)
        coin = sum(np.sin(2 * np.pi * f * r * mt + rng.uniform(0, 6)) * g * np.exp(-mt / d)
                   for r, g, d in ((1.0, 1.0, 0.1), (2.41, 0.6, 0.07), (3.93, 0.35, 0.05), (5.28, 0.2, 0.035)))
        coin += dsp.bandpass(rng.standard_normal(m), 4000, 10000) * np.exp(-mt / 0.003) * 0.6
        if at + m < n:
            out[at:at + m] += coin * rng.uniform(0.35, 1.0)
    return _finish(_room(out, rng, 0.3, 0.12), -27.0, -7.0)


@recipe("ability-heal", "Zweiter Atem: ein tiefer Atemzug, warmer Akkord schwillt an (Death-Flame, kein Feuer)")
def ability_heal(rng) -> np.ndarray:
    n = dsp.seconds(1.1)
    t = dsp.time_axis(n)
    breath = _spectral_whoosh(rng, 1.1, 300, 1800, True) * 0.4
    chord = sum(np.sin(2 * np.pi * f * t) * g for f, g in ((311.1, 0.3), (392.0, 0.22), (466.2, 0.18), (622.3, 0.1)))
    chord *= np.minimum(1, t / 0.5) * np.exp(-np.maximum(t - 0.6, 0) / 0.25)
    return _finish(_room(breath + chord, rng, 1.2, 0.3), -20.0, -3.0)


@recipe("ability-pierce", "Durchschlag: ein harter gläserner Riss, der durchschlägt und davonzieht")
def ability_pierce(rng) -> np.ndarray:
    n = dsp.seconds(0.6)
    t = dsp.time_axis(n)
    crack = dsp.bandpass(rng.standard_normal(n), 1500, 9000) * np.exp(-t / 0.015)
    ring = sum(np.sin(2 * np.pi * f * (1 - 0.3 * t) * t) * g for f, g in ((880, 0.4), (1320, 0.25), (2640, 0.12))) * np.exp(-t / 0.12)
    tail = _spectral_whoosh(rng, 0.6, 3000, 600, False) * 0.35
    return _finish(_room(crack + ring + tail, rng, 0.6, 0.18), -17.0, -2.0)


@recipe("ability-leap", "Rückstoßsprung: ein dumpfer Stoß, Stoff und Luft reißen nach hinten")
def ability_leap(rng) -> np.ndarray:
    n = dsp.seconds(0.55)
    t = dsp.time_axis(n)
    thump = np.sin(2 * np.pi * (60 + 80 * np.exp(-t / 0.03)) * t) * np.exp(-t / 0.08)
    burst = dsp.bandpass(rng.standard_normal(n), 300, 3000) * np.exp(-t / 0.03) * 0.6
    whoosh = _spectral_whoosh(rng, 0.55, 2400, 400, False) * 0.5
    return _finish(_room(thump + burst + whoosh, rng, 0.5, 0.15), -17.0, -2.0)


@recipe("ability-vortex", "Sog: ein ansteigender Wirbel, der nach innen zieht")
def ability_vortex(rng) -> np.ndarray:
    n = dsp.seconds(1.2)
    t = dsp.time_axis(n)
    swirl = _spectral_whoosh(rng, 1.2, 250, 2500, True) * (0.6 + 0.4 * np.sin(2 * np.pi * (3 + 6 * t) * t))
    tone = np.sin(2 * np.pi * (110 + 220 * t ** 2) * t) * np.minimum(1, t / 0.3) * np.exp(-np.maximum(t - 0.9, 0) / 0.1) * 0.35
    return _finish(_room(swirl + tone, rng, 0.9, 0.25), -18.0, -2.5)


@recipe("ability-guard", "Vergeltung: ein hell klingender, gefasster Eisenring der Abwehr, tief nachhallend")
def ability_guard(rng) -> np.ndarray:
    n = dsp.seconds(1.0)
    t = dsp.time_axis(n)
    strike = dsp.bandpass(rng.standard_normal(n), 1500, 7000) * np.exp(-t / 0.004) * 0.4
    ring = sum(np.sin(2 * np.pi * 329.6 * r * t + rng.uniform(0, 6)) * g * np.exp(-t / d)
               for r, g, d in ((1.0, 1.0, 0.45), (2.32, 0.6, 0.3), (4.25, 0.35, 0.18), (6.1, 0.2, 0.1)))
    low = np.sin(2 * np.pi * 82 * t) * np.exp(-t / 0.25) * 0.5
    return _finish(_room(strike + ring * 0.7 + low, rng, 0.8, 0.22), -18.0, -2.5)


@recipe("ability-mark", "Vorlage: ein Zeichen wird eingebrannt, kurzes Zischen und heller Punkt")
def ability_mark(rng) -> np.ndarray:
    n = dsp.seconds(0.5)
    t = dsp.time_axis(n)
    sizzle = dsp.bandpass(rng.standard_normal(n), 3000, 9000) * np.exp(-t / 0.12) * 0.35
    ping = sum(np.sin(2 * np.pi * f * t) * g for f, g in ((1661, 0.4), (2489, 0.2))) * np.exp(-t / 0.08)
    return _finish(_room(sizzle + ping, rng, 0.4, 0.15), -20.0, -3.0)


@recipe("door-awaken", "Tür erwacht: Stein schiebt sich mahlend, das Siegel löst sich mit einem tiefen Glockenton")
def door_awaken(rng) -> np.ndarray:
    n = dsp.seconds(1.8)
    t = dsp.time_axis(n)
    roughness = 0.4 + 0.6 * dsp.smooth_random(n, 18, rng)
    grind = dsp.bandpass(rng.standard_normal(n), 90, 900) * roughness
    grind *= np.clip(t / 0.35, 0, 1) * np.clip((1.3 - t) / 0.4, 0, 1)
    grit = dsp.bandpass(rng.standard_normal(n), 1500, 5000) * roughness * np.clip(t / 0.35, 0, 1) * np.clip((1.3 - t) / 0.4, 0, 1) * 0.15
    toll = np.zeros(n)
    at = dsp.seconds(0.55)
    bell = dsp.lowpass(dsp.bell(103.8, 1.25, rng, brightness=0.7), 3200)
    toll[at:at + len(bell)] = bell[: n - at]
    x = _room(grind * 0.35 + grit + toll * 1.0, rng, 2.0, 0.35, damping=3500)
    return _finish(x, -18.0, -2.0)


@recipe("enemy-emerge", "Gegner erscheint: Death Flame saugt sich zusammen und gibt eine Gestalt frei, tiefer Stoß mit violettem Schimmer")
def enemy_emerge(rng) -> np.ndarray:
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    # A reversed swell: air drawn in, rising in pitch, cut off by a low push.
    draw = dsp.bandpass(rng.standard_normal(n), 300, 2400) * np.clip(t / 0.42, 0, 1) ** 2.2 * (t < 0.44)
    sweep = np.sin(2 * np.pi * np.cumsum(np.interp(t, [0, 0.44], [140, 420])) / dsp.RATE) * np.clip(t / 0.44, 0, 1) ** 3 * (t < 0.44) * 0.25
    push = dsp.lowpass(_impact(rng, n, dsp.seconds(0.44), 30, 220, 0.09, 1.3), 180)
    shimmer = sum(np.sin(2 * np.pi * f * t) * g for f, g in ((622.3, 0.5), (932.3, 0.3), (1244.5, 0.15)))
    shimmer = shimmer * np.exp(-np.clip(t - 0.44, 0, None) / 0.22) * (t >= 0.44) * 0.18
    x = _room(draw * 0.5 + sweep + push + shimmer, rng, 0.9, 0.25, damping=3500)
    return _finish(x, -21.0, -4.0)


def _iron_clunk(rng, n: int, at: int, weight: float = 1.0) -> np.ndarray:
    """A heavy piece of blackened iron landing: a low body thump and a few short metallic partials."""
    out = np.zeros(n)
    m = min(n - at, dsp.seconds(0.4))
    t = dsp.time_axis(m)
    body = dsp.lowpass(rng.standard_normal(m), 260) * np.exp(-t / 0.05) * 1.4
    base = rng.uniform(410, 470)
    ring = sum(np.sin(2 * np.pi * base * r * t + rng.uniform(0, 6.3)) * a * np.exp(-t * d)
               for r, a, d in ((1.0, 0.5, 26), (2.43, 0.32, 38), (3.97, 0.2, 55), (6.1, 0.1, 80)))
    knock = dsp.bandpass(rng.standard_normal(m), 900, 5200) * np.exp(-t / 0.006) * 0.8
    out[at:at + m] = (body + ring * 0.6 + knock) * weight
    return out


def _strap(rng, n: int, start: float, length: float, gain: float) -> np.ndarray:
    """Leather under load: a quick train of creaks with a little cloth rustle."""
    out = np.zeros(n)
    a, m = dsp.seconds(start), dsp.seconds(length)
    pulses = np.zeros(m)
    tt = 0.0
    while tt < length:
        i = dsp.seconds(tt)
        if i < m:
            pulses[i] = rng.uniform(0.5, 1.0)
        tt += 1.0 / rng.uniform(70, 120)
    creak = sum(dsp.resonator(pulses, f, 10) * g for f, g in ((520, 1.0), (1100, 0.45), (2300, 0.15)))
    rustle = dsp.bandpass(rng.standard_normal(m), 1800, 7000) * 0.08
    out[a:a + m] = (creak + rustle) * np.sin(np.pi * np.linspace(0, 1, m)) * gain
    return out


@recipe("cannon-draw", "Seelenkanone gezogen: der Riemen knarzt, das schwere Eisen schwingt in die Hände und schlägt an, die Kammer rastet ein")
def cannon_draw(rng) -> np.ndarray:
    n = dsp.seconds(0.42)
    swing = dsp.bandpass(rng.standard_normal(n), 250, 1400) * np.exp(-((dsp.time_axis(n) - 0.08) / 0.05) ** 2) * 0.25
    latch = np.zeros(n)
    at, m = dsp.seconds(0.165), dsp.seconds(0.05)
    lt = dsp.time_axis(m)
    latch[at:at + m] = (np.sin(2 * np.pi * 2300 * lt) * 0.4 + dsp.bandpass(rng.standard_normal(m), 2500, 8000)) * np.exp(-lt / 0.008)
    x = _strap(rng, n, 0.0, 0.12, 0.5) + swing + _iron_clunk(rng, n, dsp.seconds(0.13), 1.0) + latch * 0.35
    x = _room(x, rng, 0.45, 0.14)
    return _finish(x, -21.0, -3.0)


@recipe("cannon-stow", "Seelenkanone verstaut: Eisen schlägt gedämpft gegen den Rücken, der Riemen zieht nach")
def cannon_stow(rng) -> np.ndarray:
    n = dsp.seconds(0.36)
    # No hard crack: the iron is laid against the coat, so it thuds softly and then rings.
    t = dsp.time_axis(n)
    base = rng.uniform(380, 440)
    ring = sum(np.sin(2 * np.pi * base * r * t + rng.uniform(0, 6.3)) * a * np.exp(-t * d)
               for r, a, d in ((1.0, 0.5, 14), (2.43, 0.3, 22), (3.97, 0.16, 32)))
    thud = dsp.lowpass(rng.standard_normal(n), 180) * np.exp(-t / 0.035) * 0.6
    clunk = (ring * 0.5 + thud) * np.clip(t / 0.004, 0, 1)
    rattle = np.zeros(n)
    for k in range(3):  # the strap's buckle and the chamber bars settling
        at = dsp.seconds(0.05 + k * rng.uniform(0.025, 0.045))
        m = dsp.seconds(0.06)
        rt = dsp.time_axis(m)
        f = rng.uniform(1700, 2900)
        rattle[at:at + m] += (np.sin(2 * np.pi * f * rt) + 0.5 * np.sin(2 * np.pi * f * 2.7 * rt)) * np.exp(-rt / 0.012) * rng.uniform(0.15, 0.3)
    x = clunk + rattle + _strap(rng, n, 0.03, 0.2, 0.55)
    x = _room(x, rng, 0.4, 0.12)
    return _finish(x, -23.0, -4.0)


@recipe("cannon-hum", "Seelenkanone lädt: Brummen der Kammer, darüber ein Seelenwimmern, leises Vibrieren; nahtlose Mono-Schleife, das Spiel hebt Tonhöhe und Pegel mit der Ladung")
def cannon_hum(rng) -> np.ndarray:
    seconds = 2.0  # every frequency below completes whole cycles in the loop: seamless
    n = dsp.seconds(seconds)
    t = dsp.time_axis(n)
    hum = sum(np.sin(2 * np.pi * f * t + rng.uniform(0, 6.3)) * a for f, a in ((55, 0.6), (110, 1.0), (165, 0.45), (220, 0.3), (330, 0.12)))
    vibrato = 1.0 + 0.004 * np.sin(2 * np.pi * 5.0 * t)
    whine = (np.sin(2 * np.pi * 880 * np.cumsum(vibrato) / dsp.RATE) + 0.35 * np.sin(2 * np.pi * 1320 * np.cumsum(vibrato) / dsp.RATE))
    whine *= 0.5 + 0.5 * np.sin(2 * np.pi * 1.5 * t) ** 2
    tremolo = 0.82 + 0.18 * np.sin(2 * np.pi * 18.0 * t)
    grit = dsp.circular(lambda x: dsp.bandpass(x, 2000, 6000), dsp.white(n, rng)) * 0.02
    x = (hum * 0.55 + whine * 0.12) * tremolo + grit
    return dsp.normalise_loudness(x, -22.0, peak_ceiling_db=-4.0)


@recipe("soul-release-soft", "Seele frei: ein ruhiger, warmer Seelenton, eine weiche tiefe Glocke, ein leises Ausatmen nach oben; nicht schrill, damit er im Gedränge nicht piept")
def soul_release_soft(rng) -> np.ndarray:
    n = dsp.seconds(1.1)
    t = dsp.time_axis(n)
    # A soft bell in G-sharp minor's colour (the music's key), struck gently: few, low partials.
    base = rng.choice([311.1, 370.0, 415.3])  # D#4, F#4, G#4
    bell = sum(np.sin(2 * np.pi * base * r * t + rng.uniform(0, 6.3)) * a * np.exp(-t * d)
               for r, a, d in ((1.0, 1.0, 3.2), (2.0, 0.28, 5.0), (2.76, 0.12, 7.5), (0.5, 0.35, 2.4)))
    bell *= np.clip(t / 0.012, 0, 1)
    # The soul leaving: a breathy rise, quiet, filtered so it never hisses.
    breath = dsp.bandpass(rng.standard_normal(n), 500, 2400) * np.exp(-((t - 0.35) / 0.22) ** 2) * 0.25
    tone = np.sin(2 * np.pi * base * 2 * t * (1 + 0.04 * t)) * np.exp(-((t - 0.3) / 0.25) ** 2) * 0.12
    x = _room(bell * 0.6 + breath + tone, rng, 1.2, 0.3, damping=3500)
    x = dsp.lowpass(x, 5000)
    return _finish(x, -22.0, -4.0)
