"""Ambience loops per area (stereo, seamless). The arena keeps its approved Ludo bed."""
from __future__ import annotations

import math

import numpy as np

import dsp
from recipes import recipe


def _bed(n: int, rng, colour: str, low: float, high: float, level: float, width: float = 1.0,
         movement: float = 0.3, rate: float = 0.08) -> np.ndarray:
    """A looping stereo noise band with slow level movement."""
    source = {"pink": dsp.pink, "brown": dsp.brown, "white": dsp.white}[colour]
    left, right = source(n, rng), source(n, rng)
    right = left * (1 - width) + right * width
    stereo = np.stack([left, right], axis=-1)
    stereo = dsp.circular(lambda x: dsp.bandpass(x, low, high, order=2), stereo)
    swell = (1 - movement + movement * dsp.smooth_loop(n, rate, rng))[:, None]
    return stereo / (np.std(stereo) + 1e-12) * swell * level


def _grains(n: int, rng, count: int, make, spread: float = 0.8, gain_range=(0.3, 1.0)) -> np.ndarray:
    out = np.zeros((n, 2))
    for _ in range(count):
        sound = make(rng)
        stereo = dsp.pan(sound, rng.uniform(-spread, spread))
        dsp.place(out, stereo, int(rng.integers(0, n)), rng.uniform(*gain_range))
    return out


def _clink(rng) -> np.ndarray:
    """A small iron chain link touching another: three inharmonic partials, quick decay."""
    n = dsp.seconds(0.6)
    t = dsp.time_axis(n)
    base = rng.uniform(1900, 2600)
    out = sum(np.sin(2 * np.pi * base * r * t) * a * np.exp(-t * d)
              for r, a, d in ((1.0, 1.0, 14), (2.71, 0.5, 22), (5.15, 0.25, 35)))
    return out * 0.6


def _ash(rng) -> np.ndarray:
    n = dsp.seconds(rng.uniform(0.25, 0.7))
    grain = dsp.bandpass(rng.standard_normal(n), 2500, 7000) * dsp.envelope(n, 0.12, 0.3, 1.5)
    return grain * 0.5


def _crackle(rng) -> np.ndarray:
    n = dsp.seconds(0.03)
    pop = rng.standard_normal(n) * np.exp(-np.arange(n) / (dsp.RATE * 0.004))
    return dsp.bandpass(pop, 900, 5000) * rng.uniform(0.3, 1.0)


@recipe("ambience-hub", "Aschene Vorhalle: Ofen hinter den Türen, Raum, Asche, Warden-Flammen, Ketten, ferne Werksglocke", loop=True)
def hub(rng) -> np.ndarray:
    n = dsp.seconds(32.0)
    room = dsp.impulse_response(3.2, rng, damping_hz=3500, predelay=0.02)
    # The furnace beyond the doors: a slow-breathing low rumble and a faint drone.
    rumble = _bed(n, rng, "brown", 30, 95, 0.26, width=0.5, movement=0.15, rate=0.04)
    drone = dsp.drone(41.2, 32.0, rng, movement=0.15) * 0.09
    rumble += np.stack([drone, drone], axis=-1)
    # Room tone: still air in a large stone hall.
    air = _bed(n, rng, "pink", 180, 1800, 0.035, movement=0.25, rate=0.05)
    # Warden flames: a soft, breathing hiss near the braziers (left and right of centre) with rare crackles.
    flames = np.zeros((n, 2))
    for position in (-0.35, 0.3):
        breath = dsp.circular(lambda x: dsp.bandpass(x, 400, 2600), dsp.pink(n, rng))
        breath *= 0.5 + 0.5 * dsp.smooth_loop(n, 0.35, rng)
        flames += dsp.pan(breath / (np.std(breath) + 1e-9) * 0.012, position)
        flames += _grains(n, rng, 40, _crackle, spread=0.15, gain_range=(0.01, 0.04))
    ash = _grains(n, rng, 26, _ash, spread=0.9, gain_range=(0.006, 0.02))
    chains = _grains(n, rng, 4, _clink, spread=0.9, gain_range=(0.01, 0.025))
    # One distant toll of the works bell per loop, far behind the doors.
    toll = np.zeros((n, 2))
    bell = dsp.lowpass(dsp.bell(98.0, 9.0, rng, brightness=0.6), 1400) * 0.05
    dsp.place(toll, dsp.pan(bell, -0.1), dsp.seconds(11.0))
    mix = rumble + air + flames + ash + chains + toll
    mix = dsp.reverb(mix, room, wet=0.35, loop=True)
    return dsp.normalise_loudness(mix, -30.0, peak_ceiling_db=-9.0)


def _lap(rng) -> np.ndarray:
    """One small wave slapping against a concrete edge: a soft swell, a wet slap, a sucking ebb."""
    duration = rng.uniform(0.9, 1.6)
    n = dsp.seconds(duration)
    t = dsp.time_axis(n)
    swell = dsp.lowpass(rng.standard_normal(n), 500) * np.exp(-((t - 0.25) / 0.18) ** 2)
    slap_at = dsp.seconds(rng.uniform(0.3, 0.42))
    slap = np.zeros(n)
    m = min(n - slap_at, dsp.seconds(0.25))
    slap[slap_at:slap_at + m] = dsp.bandpass(rng.standard_normal(m), 300, 2400) * np.exp(-np.arange(m) / (dsp.RATE * 0.05))
    ebb = dsp.bandpass(rng.standard_normal(n), 800, 4500) * np.exp(-((t - duration * 0.6) / (duration * 0.25)) ** 2) * 0.25
    # Bubbles in the ebb.
    for _ in range(rng.integers(3, 8)):
        start = int(rng.uniform(0.45, 0.85) * n)
        length = dsp.seconds(0.03)
        if start + length < n:
            f = rng.uniform(600, 1400)
            bt = dsp.time_axis(length)
            ebb[start:start + length] += np.sin(2 * np.pi * f * (1 + 2 * bt) * bt) * np.exp(-bt / 0.008) * 0.3
    return swell * 0.6 + slap * 0.8 + ebb


def _flap(rng) -> np.ndarray:
    """A split-flap board rattling through a few leaves: quick dry clicks."""
    clicks = rng.integers(4, 11)
    gap = rng.uniform(0.035, 0.06)
    n = dsp.seconds(clicks * gap + 0.2)
    out = np.zeros(n)
    for k in range(clicks):
        at = dsp.seconds(k * gap * rng.uniform(0.9, 1.1))
        m = dsp.seconds(0.012)
        click = dsp.bandpass(rng.standard_normal(m), 1500, 6000) * np.exp(-np.arange(m) / (dsp.RATE * 0.002))
        if at + m < n:
            out[at:at + m] += click * rng.uniform(0.6, 1.0)
    return dsp.resonator(out, 900, 4) * 0.5 + out * 0.5


@recipe("ambience-shore", "Unvollendetes Ufer: Wasser an der Bahnsteigkante, kalter Wind, Fallblätter, ferne Bojenglocke", loop=True)
def shore(rng) -> np.ndarray:
    n = dsp.seconds(40.0)
    space = dsp.impulse_response(2.2, rng, damping_hz=5000, predelay=0.03, stereo_spread=1.0)
    # Open sea and wind: wide, cold, slowly moving bands.
    sea = _bed(n, rng, "pink", 90, 900, 0.10, movement=0.35, rate=0.09)
    wind = np.zeros((n, 2))
    for _ in range(2):
        source = dsp.pink(n, rng)
        centre = 500 + 900 * dsp.smooth_loop(n, 0.06, rng)
        gust = dsp.circular(lambda x: dsp.swept_bandpass(x, np.concatenate([centre, centre]), q=1.6), source)
        level = (0.35 + 0.65 * dsp.smooth_loop(n, 0.05, rng)) * 0.022
        wind += dsp.pan(gust / (np.std(gust) + 1e-9) * level, rng.uniform(-0.7, 0.7))
    laps = _grains(n, rng, 22, _lap, spread=0.8, gain_range=(0.04, 0.09))
    flaps = _grains(n, rng, 3, _flap, spread=0.4, gain_range=(0.03, 0.05))
    buoy = np.zeros((n, 2))
    for at in (6.0, 26.5):
        toll = dsp.lowpass(dsp.bell(311.0, 6.0, rng, brightness=0.5), 2200) * 0.025
        dsp.place(buoy, dsp.pan(toll, 0.6), dsp.seconds(at))
    mix = sea + wind + laps + flaps + buoy
    mix = dsp.reverb(mix, space, wet=0.22, loop=True)
    return dsp.normalise_loudness(mix, -29.0, peak_ceiling_db=-8.0)


def _drip(rng) -> np.ndarray:
    n = dsp.seconds(0.25)
    t = dsp.time_axis(n)
    f = rng.uniform(900, 1800)
    return np.sin(2 * np.pi * f * (1 + 1.5 * t) * t) * np.exp(-t / 0.04) * 0.5


def _creak(rng) -> np.ndarray:
    """A mooring rope or timber creaking: a slowed train of friction pulses through wood resonances."""
    duration = rng.uniform(0.4, 0.9)
    n = dsp.seconds(duration)
    rate = rng.uniform(25, 60)
    pulses = np.zeros(n)
    t = 0.0
    while t < duration:
        i = dsp.seconds(t)
        if i < n:
            pulses[i] = rng.uniform(0.5, 1.0)
        t += 1.0 / (rate * (1 + 0.4 * math.sin(t * 9)))
    body = sum(dsp.resonator(pulses, f, 12) * g for f, g in ((340, 1.0), (720, 0.5), (1350, 0.25)))
    return body * dsp.envelope(n, 0.08, duration * 0.5, 1.2) * 0.25


def _slap(rng) -> np.ndarray:
    return _lap(rng) * 0.8


@recipe("ambience-harbour", "Suchgang: Wind durch das versunkene Hafenviertel, Tropfen, knarzende Taue, Wasser am Kai", loop=True)
def harbour(rng) -> np.ndarray:
    n = dsp.seconds(36.0)
    space = dsp.impulse_response(2.8, rng, damping_hz=4200, predelay=0.04)
    wind = np.zeros((n, 2))
    for _ in range(2):
        source = dsp.pink(n, rng)
        centre = 380 + 700 * dsp.smooth_loop(n, 0.07, rng)
        gust = dsp.circular(lambda x: dsp.swept_bandpass(x, np.concatenate([centre, centre]), q=1.4), source)
        level = (0.3 + 0.7 * dsp.smooth_loop(n, 0.05, rng)) * 0.02
        wind += dsp.pan(gust / (np.std(gust) + 1e-9) * level, rng.uniform(-0.6, 0.6))
    bed = _bed(n, rng, "brown", 40, 300, 0.05, movement=0.3, rate=0.06)
    laps = _grains(n, rng, 14, _slap, spread=0.9, gain_range=(0.025, 0.05))
    drips = _grains(n, rng, 30, _drip, spread=0.9, gain_range=(0.004, 0.012))
    creaks = _grains(n, rng, 6, _creak, spread=0.8, gain_range=(0.02, 0.04))
    mix = dsp.reverb(bed + wind + laps + drips + creaks, space, wet=0.3, loop=True)
    return dsp.normalise_loudness(mix, -29.0, peak_ceiling_db=-8.0)


@recipe("ambience-causeway", "Damm: offenes Wasser an den Steinkanten, stärkerer Wind, ferne Brandung", loop=True)
def causeway(rng) -> np.ndarray:
    n = dsp.seconds(36.0)
    space = dsp.impulse_response(1.6, rng, damping_hz=5000, predelay=0.02)
    sea = _bed(n, rng, "pink", 70, 1200, 0.12, movement=0.45, rate=0.11)
    wind = np.zeros((n, 2))
    for _ in range(3):
        source = dsp.pink(n, rng)
        centre = 500 + 1200 * dsp.smooth_loop(n, 0.09, rng)
        gust = dsp.circular(lambda x: dsp.swept_bandpass(x, np.concatenate([centre, centre]), q=1.8), source)
        level = (0.25 + 0.75 * dsp.smooth_loop(n, 0.07, rng)) * 0.024
        wind += dsp.pan(gust / (np.std(gust) + 1e-9) * level, rng.uniform(-0.8, 0.8))
    laps = _grains(n, rng, 26, _lap, spread=0.95, gain_range=(0.04, 0.09))
    mix = dsp.reverb(sea + wind + laps, space, wet=0.18, loop=True)
    return dsp.normalise_loudness(mix, -27.0, peak_ceiling_db=-7.0)


@recipe("ambience-crossing", "Überfahrt: Wellen am Rumpf, Gischt, knarzende Planken, der Warden-Antrieb als tiefes Pochen", loop=True)
def crossing(rng) -> np.ndarray:
    n = dsp.seconds(24.0)
    space = dsp.impulse_response(1.2, rng, damping_hz=5000, predelay=0.015)
    # Waves running along the hull: swells of filtered noise every few seconds, left to right.
    waves = np.zeros((n, 2))
    t = 0.0
    while t < 24.0 - 0.01:
        length = rng.uniform(1.6, 2.6)
        m = dsp.seconds(length)
        swell = dsp.bandpass(rng.standard_normal(m), 180, 1400) * np.sin(np.pi * np.linspace(0, 1, m)) ** 2
        pan = np.linspace(-0.7, 0.7, m)
        dsp.place(waves, dsp.pan(swell, pan), dsp.seconds(t), 0.06)
        t += rng.uniform(1.1, 1.9)
    hull = _bed(n, rng, "brown", 35, 140, 0.1, movement=0.3, rate=0.2)
    wind = _bed(n, rng, "pink", 400, 1600, 0.018, movement=0.5, rate=0.12)
    engine = np.zeros((n, 2))
    period = 0.75
    t = 0.0
    while t < 24.0 - 0.01:
        thump = dsp.lowpass(np.sin(2 * np.pi * 46 * dsp.time_axis(dsp.seconds(0.4))) * np.exp(-dsp.time_axis(dsp.seconds(0.4)) / 0.09), 200)
        dsp.place(engine, dsp.pan(thump, -0.4), dsp.seconds(t), 0.07)
        t += period
    spray = _grains(n, rng, 26, _lap, spread=0.95, gain_range=(0.04, 0.08))
    creaks = _grains(n, rng, 7, _creak, spread=0.7, gain_range=(0.03, 0.05))
    mix = dsp.reverb(waves + hull + wind + engine + spray + creaks, space, wet=0.15, loop=True)
    return dsp.normalise_loudness(mix, -26.0, peak_ceiling_db=-6.0)


@recipe("ambience-threshold", "Schwelle: tiefe Stille, ferner Grundton der Warden-Stadt, leise Böen, eine ferne Glocke, atmende Flammen", loop=True)
def threshold(rng) -> np.ndarray:
    n = dsp.seconds(32.0)
    space = dsp.impulse_response(4.5, rng, damping_hz=3200, predelay=0.05)
    tone = dsp.drone(61.7, 32.0, rng, movement=0.25) * 0.1  # B1: the city's low note
    tone += dsp.drone(123.5, 32.0, rng, movement=0.35) * 0.03
    tone = np.stack([tone, tone], axis=-1)
    # A faint glassy shimmer on B, the Warden city's note, breathing slowly instead of noise.
    shimmer = np.zeros(n)
    for harmonic, gain in ((4, 0.012), (6, 0.008), (8, 0.005)):
        shimmer += dsp.sine(61.7 * harmonic, n, rng.uniform(0, 6.28)) * gain * (0.4 + 0.6 * dsp.smooth_loop(n, 0.08, rng))
    gusts = np.stack([shimmer, np.roll(shimmer, dsp.seconds(0.013))], axis=-1)
    flames = np.zeros((n, 2))
    for position in (-0.25, 0.25):
        breath = dsp.circular(lambda x: dsp.bandpass(x, 500, 2000), dsp.pink(n, rng))
        breath *= (0.5 + 0.5 * dsp.smooth_loop(n, 0.3, rng)) ** 2
        flames += dsp.pan(breath / (np.std(breath) + 1e-9) * 0.0012, position)
    bell = np.zeros((n, 2))
    dsp.place(bell, dsp.pan(dsp.lowpass(dsp.bell(123.5, 10.0, rng, brightness=0.5), 1600) * 0.05, 0.2), dsp.seconds(9.0))
    mix = dsp.reverb(tone + gusts + flames + bell, space, wet=0.5, loop=True)
    return dsp.normalise_loudness(mix, -34.0, peak_ceiling_db=-10.0)
