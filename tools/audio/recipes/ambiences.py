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


@recipe("ambience-hub", "Aschene Vorhalle: Ofen hinter den Türen, Raum, Asche, Ketten, ferne Werksglocke (die Warden-Flammen sind Punktquellen im Spiel)", loop=True)
def hub(rng) -> np.ndarray:
    n = dsp.seconds(32.0)
    room = dsp.impulse_response(3.2, rng, damping_hz=3500, predelay=0.02)
    # The furnace beyond the doors: a slow-breathing low rumble and a faint drone.
    rumble = _bed(n, rng, "brown", 30, 95, 0.26, width=0.5, movement=0.15, rate=0.04)
    drone = dsp.drone(41.2, 32.0, rng, movement=0.15) * 0.09
    rumble += np.stack([drone, drone], axis=-1)
    # Room tone: still air in a large stone hall.
    air = _bed(n, rng, "pink", 180, 1800, 0.035, movement=0.25, rate=0.05)
    # The Warden flames are point sources in the game now (warden-flame), heard where they burn;
    # their old baked-in layer is still drawn, so the random stream and every other layer stay
    # as they were, but no longer mixed.
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
    mix = rumble + air + ash + chains + toll
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


def _pop(rng, body: bool) -> np.ndarray:
    """One crackle of burning wood: a 1-4 ms burst, bright or with a short woody body."""
    n = dsp.seconds(0.06)
    width = rng.uniform(0.0008, 0.003)
    burst = rng.standard_normal(n) * np.exp(-np.arange(n) / (dsp.RATE * width))
    centre = rng.uniform(1500, 6000)
    out = dsp.bandpass(burst, centre * 0.5, min(centre * 1.8, 16000))
    if body:
        out = out * 0.6 + dsp.resonator(burst, rng.uniform(280, 900), rng.uniform(4, 9)) * 0.8
    return out


@recipe("life-flame", "Life Flame im kalten Ofen: warmes, ruhiges Feuer, Knistern in Büscheln, springende Glut, tiefes Brausen; Punktquelle (mono), nahtlos", loop=True)
def life_flame(rng) -> np.ndarray:
    seconds = 9.0
    n = dsp.seconds(seconds)
    # A low, warm roar under everything, breathing slowly: the fire is felt more than heard.
    roar = dsp.circular(lambda x: dsp.bandpass(x, 45, 240), dsp.brown(n, rng))
    roar = roar / (np.std(roar) + 1e-9) * (0.7 + 0.3 * dsp.smooth_loop(n, 0.25, rng))
    # Crackling comes in irregular clusters, a few loud, most small (heavy-tailed gains).
    crackle = np.zeros(n)
    for _ in range(int(seconds * 2.6)):
        at = int(rng.integers(0, n))
        for _ in range(int(rng.integers(1, 7))):
            gain = min(1.0, 0.08 * rng.pareto(1.6) + 0.04)
            dsp.place(crackle, _pop(rng, rng.random() < 0.35), at, gain)
            at = (at + dsp.seconds(rng.uniform(0.004, 0.045))) % n
    # A sap pocket bursting now and then: loud, woody.
    for _ in range(int(seconds * 0.5)):
        dsp.place(crackle, _pop(rng, True), int(rng.integers(0, n)), rng.uniform(0.6, 1.0))
    # Sizzle: a faint bed of tiny ticks.
    sizzle = np.zeros(n)
    for _ in range(int(seconds * 45)):
        dsp.place(sizzle, _pop(rng, False), int(rng.integers(0, n)), rng.uniform(0.01, 0.05))
    mix = roar * 0.09 + crackle + sizzle
    room = dsp.impulse_response(0.9, rng, damping_hz=5000, predelay=0.008)
    mix = dsp.reverb(mix, room, wet=0.14, loop=True).mean(axis=1)
    mix = dsp.circular(lambda x: dsp.highpass(x, 35), mix)
    # The loop is seamless all round; start it where two neighbouring samples are nearly silent,
    # so its ends meet without a step.
    quiet = np.abs(mix) + np.abs(np.roll(mix, 1))
    mix = np.roll(mix, -int(np.argmin(quiet)))
    return dsp.normalise_loudness(mix, -24.0, peak_ceiling_db=-4.0)


def _seamless(mix: np.ndarray) -> np.ndarray:
    """Start a loop where two neighbouring samples are nearly silent, so its ends meet cleanly."""
    quiet = np.abs(mix) + np.abs(np.roll(mix, 1))
    return np.roll(mix, -int(np.argmin(quiet)))


@recipe("presence-burning", "Burning in der Nähe: instabiles Grollen der Death Flame, flatternd, Knistern aus den Rissen; Punktquelle (mono), nahtlos", loop=True)
def presence_burning(rng) -> np.ndarray:
    seconds = 5.0
    n = dsp.seconds(seconds)
    # Unstable flame: a low roar whose level jumps irregularly several times a second.
    roar = dsp.circular(lambda x: dsp.bandpass(x, 60, 520), dsp.brown(n, rng))
    flutter = 0.45 + 0.35 * dsp.smooth_loop(n, 5.5, rng) + 0.2 * dsp.smooth_loop(n, 1.3, rng)
    roar = roar / (np.std(roar) + 1e-9) * flutter
    # A hiss over it, the flame tongues licking out of the cracks.
    tongue = dsp.circular(lambda x: dsp.bandpass(x, 900, 3800), dsp.pink(n, rng))
    tongue = tongue / (np.std(tongue) + 1e-9) * np.clip(dsp.smooth_loop(n, 3.0, rng), 0, None) ** 2
    crackle = np.zeros(n)
    for _ in range(int(seconds * 9)):
        dsp.place(crackle, _pop(rng, rng.random() < 0.25), int(rng.integers(0, n)), min(1.0, 0.05 * rng.pareto(1.8) + 0.03))
    mix = roar * 0.22 + tongue * 0.05 + crackle * 0.8
    mix = dsp.circular(lambda x: dsp.highpass(x, 45), mix)
    return dsp.normalise_loudness(_seamless(mix), -24.0, peak_ceiling_db=-4.0)


@recipe("presence-hollow", "Hollow in der Nähe: leises Atmen durch die Porzellanmaske, verzerrtes Flüstern, Stoff knarzt; Punktquelle (mono), nahtlos", loop=True)
def presence_hollow(rng) -> np.ndarray:
    seconds = 6.0
    n = dsp.seconds(seconds)
    t = dsp.time_axis(n)
    # Two slow breaths per loop, hollow and resonant behind the mask.
    breath_env = np.zeros(n)
    for start, length, gain in ((0.3, 1.1, 0.9), (1.5, 1.3, 0.6), (3.3, 1.1, 1.0), (4.5, 1.2, 0.55)):
        a, m = dsp.seconds(start), dsp.seconds(length)
        breath_env[a:a + m] += np.sin(np.pi * np.linspace(0, 1, m)) ** 1.5 * gain
    air = dsp.circular(lambda x: dsp.bandpass(x, 300, 2600), dsp.pink(n, rng))
    air = air / (np.std(air) + 1e-9)
    breath = sum(dsp.resonator(air, f, 6) * g for f, g in ((520, 1.0), (1150, 0.6), (2300, 0.25))) * breath_env
    breath = breath / (np.std(breath) + 1e-9)
    # Whispering: noise shaped into syllables, formants sliding, pitched oddly low.
    whisper = np.zeros(n)
    for _ in range(9):
        at = int(rng.integers(0, n))
        m = dsp.seconds(rng.uniform(0.12, 0.35))
        grain = dsp.bandpass(rng.standard_normal(m), 600, 4500)
        centre = np.linspace(rng.uniform(700, 1400), rng.uniform(1600, 2800), m)
        grain = dsp.swept_bandpass(grain, centre, q=4.0) * np.hanning(m)
        dsp.place(whisper, grain, at, rng.uniform(0.3, 1.0))
    whisper = whisper / (np.std(whisper) + 1e-9)
    creak = np.zeros(n)
    for _ in range(3):
        pulses = np.zeros(dsp.seconds(0.25))
        tt = 0.0
        while tt < 0.25:
            pulses[dsp.seconds(tt)] = rng.uniform(0.4, 1.0)
            tt += 1.0 / rng.uniform(60, 110)
        dsp.place(creak, dsp.resonator(pulses, rng.uniform(400, 800), 9) * np.hanning(len(pulses)), int(rng.integers(0, n)), 0.5)
    mix = breath * 0.5 + whisper * 0.12 + creak * 0.25
    room = dsp.impulse_response(0.6, rng, damping_hz=4500, predelay=0.006)
    mix = dsp.reverb(mix, room, wet=0.12, loop=True).mean(axis=1)
    mix = dsp.circular(lambda x: dsp.highpass(x, 80), mix)
    return dsp.normalise_loudness(_seamless(mix), -26.0, peak_ceiling_db=-6.0)


@recipe("presence-devourer", "Devourer in der Nähe: tiefes Grollen aus dem Rumpf, ein verzerrter Chor gefangener Seelen, nasses Atmen; Punktquelle (mono), nahtlos", loop=True)
def presence_devourer(rng) -> np.ndarray:
    seconds = 7.0
    n = dsp.seconds(seconds)
    t = dsp.time_axis(n)
    # The prison: a low, slowly beating drone (two close pitches) under a rumble.
    drone = (np.sin(2 * np.pi * 55.0 * t) + 0.7 * np.sin(2 * np.pi * 57.3 * t + 1.0) + 0.3 * np.sin(2 * np.pi * 110.4 * t)) * 0.25
    rumble = dsp.circular(lambda x: dsp.bandpass(x, 35, 180), dsp.brown(n, rng))
    rumble = rumble / (np.std(rumble) + 1e-9) * (0.6 + 0.4 * dsp.smooth_loop(n, 0.4, rng))
    # Trapped souls: a few breathy vowel tones drifting in pitch, very quiet and detuned.
    choir = np.zeros(n)
    for base in (196.0, 233.1, 277.2):
        drift = 1.0 + 0.012 * dsp.smooth_loop(n, 0.2, rng)
        phase = np.cumsum(base * drift) / dsp.RATE * 2 * np.pi
        tone = np.sin(phase) + 0.4 * np.sin(2 * phase) + 0.2 * np.sin(3 * phase)
        breathy = dsp.circular(lambda x: dsp.bandpass(x, base * 0.9, base * 4), dsp.pink(n, rng))
        voice = (tone * 0.85 + breathy / (np.std(breathy) + 1e-9) * 0.12) * np.clip(dsp.smooth_loop(n, 0.25, rng), 0, None) ** 2
        choir += voice
    choir = dsp.circular(lambda x: dsp.bandpass(x, 300, 1600), choir)
    choir = choir / (np.std(choir) + 1e-9)
    # A throat: a slow pulse train (the glottis, ~40 Hz) through two low formants, swelling with
    # each breath, so it reads as a creature, not as weather.
    breath_cycle = 0.5 - 0.5 * np.cos(2 * np.pi * t / (seconds / 2))
    rate = 38.0 + 6.0 * dsp.smooth_loop(n, 0.3, rng)
    pulse_phase = np.cumsum(rate) / dsp.RATE
    # Each glottal pulse a short decaying click (a rattle, not hiss): jitter in level per pulse.
    glottis = (np.diff(np.floor(pulse_phase), prepend=0) > 0).astype(float)
    glottis *= rng.uniform(0.6, 1.0, n)
    glottis = np.convolve(glottis, np.exp(-np.arange(dsp.seconds(0.004)) / dsp.seconds(0.001)), mode="same")
    throat = sum(dsp.resonator(glottis, f, q) * g for f, q, g in ((280, 5, 1.0), (640, 6, 0.6), (1300, 8, 0.2)))
    throat = throat / (np.std(throat) + 1e-9) * breath_cycle ** 1.5
    mix = drone * 0.35 + rumble * 0.12 + throat * 0.32 + choir * 0.2
    room = dsp.impulse_response(1.0, rng, damping_hz=3000, predelay=0.01)
    mix = dsp.reverb(mix, room, wet=0.18, loop=True).mean(axis=1)
    mix = dsp.circular(lambda x: dsp.highpass(x, 30), mix)
    return dsp.normalise_loudness(_seamless(mix), -25.0, peak_ceiling_db=-5.0)


@recipe("resonance-rumble", "Resonanz: die eigene Death Flame brennt im Körper, tiefes ruhiges Grollen mit langsamen Schüben und einem leisen Herzschlag; Punktquelle (mono), nahtlos", loop=True)
def resonance_rumble(rng) -> np.ndarray:
    seconds = 4.0
    n = dsp.seconds(seconds)
    t = dsp.time_axis(n)
    # A deep, steady flame: low band noise, surging slowly (two surges per loop).
    body = dsp.circular(lambda x: dsp.bandpass(x, 40, 260), dsp.brown(n, rng))
    surge = 0.7 + 0.3 * (0.5 - 0.5 * np.cos(2 * np.pi * t / (seconds / 2)))
    body = body / (np.std(body) + 1e-9) * surge
    # Its breath: a soft, high-ish shimmer of the flame tongues (violet, not fire-orange: no crackle).
    shimmer = dsp.circular(lambda x: dsp.bandpass(x, 1800, 5200), dsp.pink(n, rng))
    shimmer = shimmer / (np.std(shimmer) + 1e-9) * (0.4 + 0.6 * dsp.smooth_loop(n, 1.5, rng)) * 0.06
    # The core's heartbeat under it, 60 bpm, felt more than heard.
    beat = np.zeros(n)
    for k in range(int(seconds)):
        for offset, gain in ((0.0, 1.0), (0.24, 0.6)):
            at = dsp.seconds(k + offset)
            m = dsp.seconds(0.18)
            bt = dsp.time_axis(m)
            thump = np.sin(2 * np.pi * (52 - 14 * bt / 0.18) * bt) * np.exp(-bt / 0.05) * gain
            dsp.place(beat, thump, at)
    # A few soft sparks: it is a flame, not weather.
    sparks = np.zeros(n)
    for _ in range(int(seconds * 3)):
        dsp.place(sparks, _pop(rng, False), int(rng.integers(0, n)), rng.uniform(0.04, 0.12))
    mix = body * 0.3 + shimmer + beat * 0.6 + sparks
    mix = dsp.circular(lambda x: dsp.highpass(x, 28), mix)
    return dsp.normalise_loudness(_seamless(mix), -25.0, peak_ceiling_db=-4.0)


@recipe("warden-flame", "Warden-Flamme (Feuerschale, Wandleuchter): weich atmendes Zischen der Death Flame, leckendes Flattern, ein leises tiefes Summen in Gis, seltenes weiches Knistern; Punktquelle (mono), nahtlos", loop=True)
def warden_flame(rng) -> np.ndarray:
    seconds = 6.0
    n = dsp.seconds(seconds)
    t = dsp.time_axis(n)
    # The flame's body: a low, soft roar that flutters as the tongues lick (several times a second)
    # and swells slowly, with only a little air on top.
    roar = dsp.circular(lambda x: dsp.bandpass(x, 120, 900), dsp.brown(n, rng) + dsp.pink(n, rng) * 0.3)
    roar = roar / (np.std(roar) + 1e-9)
    lick = np.clip(0.4 + 0.5 * dsp.smooth_loop(n, 12.0, rng) + 0.15 * dsp.smooth_loop(n, 2.2, rng), 0.05, None)
    roar *= lick * (0.75 + 0.25 * dsp.smooth_loop(n, 0.35, rng))
    air = dsp.circular(lambda x: dsp.bandpass(x, 900, 2600), dsp.pink(n, rng))
    air = air / (np.std(air) + 1e-9) * lick ** 2
    # The Death Flame's voice under it: a faint hum in G-sharp, wavering.
    hum = sum(np.sin(2 * np.pi * f * t + rng.uniform(0, 6.3)) * g for f, g in ((51.9, 1.0), (103.8, 0.5), (155.7, 0.2)))
    hum *= 0.75 + 0.25 * dsp.smooth_loop(n, 0.3, rng)
    # Rarely a soft tick of an ember, never the snap of wood.
    ticks = np.zeros(n)
    for _ in range(int(seconds * 0.7)):
        dsp.place(ticks, _pop(rng, False), int(rng.integers(0, n)), rng.uniform(0.03, 0.08))
    ticks = dsp.circular(lambda x: dsp.lowpass(x, 3000), ticks)
    mix = roar * 0.07 + air * 0.012 + hum * 0.03 + ticks
    mix = dsp.circular(lambda x: dsp.highpass(x, 38), mix)
    return dsp.normalise_loudness(_seamless(mix), -26.0, peak_ceiling_db=-6.0)
