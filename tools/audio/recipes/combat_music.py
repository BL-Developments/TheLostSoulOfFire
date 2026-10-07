"""Combat music for the foundry: three stems of one 16-bar score that the game layers by how hot
the fight runs (AudioDirector.SetCombatIntensity).

Owner, 07.10.2026: the mood was too cinematic, not action-packed, tense or a little mad enough.
The approved Ludo arena loop stays for the quiet between waves; during a wave this score takes
over. It is built from the place itself: the foundry's hammers and anvils are the percussion, the
furnace's pedal tone the drone, and the souls of the hall cry in the top layer.

- combat-pulse   the floor of the fight: taiko and sub kick, a galloping distorted bass, a G#
                 pedal drone. Always on during a wave.
- combat-drive   the foundry at work: anvils on the backbeat, an iron snare, hammered hats,
                 brass-like power stabs, tom fills at the end of each phrase. Several enemies.
- combat-frenzy  the madness: tremolo string clusters with the flat second, choir shrieks, a
                 detuned music-box arpeggio, risers into each phrase. Danger, a crowd, low health.

128 bpm, so a beat is exactly 22,500 samples and the 16 bars exactly 30 s; G# Phrygian (the arena
loop and the zone themes sit on G#), i - bII movement over the pedal. All three stems come from
the same fixed score (seed below), so they line up sample for sample; the recipe seed only
varies noise. Stereo 48 kHz, reverb tails wrap around the loop point.
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe
from recipes import voices as V

BPM = 128
BEAT = 22_500
SIXTEENTH = BEAT // 4
BARS = 16
N = BARS * 4 * BEAT
SCORE_SEED = 1287
#: Samples from the file start to the first downbeat: the loop point sits there, between hits,
#: where all three stems (seed 1) step by less than -57 dBFS from one sample to the next.
OFFSET = 1714

#: Root (MIDI) of each bar: i i i bII | i i bVI bVII | i i i bII | bVI bVII bII V.
ROOTS = [44, 44, 44, 45, 44, 44, 40, 42, 44, 44, 44, 45, 40, 42, 45, 39]


def hz(midi: float) -> float:
    return 440.0 * 2 ** ((midi - 69) / 12)


def at(bar: int, sixteenth: int = 0) -> int:
    return bar * 16 * SIXTEENTH + sixteenth * SIXTEENTH


def _put(track: np.ndarray, sound: np.ndarray, start: int, gain: float = 1.0, pan: float = 0.0) -> None:
    stereo = sound if sound.ndim == 2 else dsp.pan(sound, pan)
    dsp.place(track, stereo, start % N, gain)


def _norm(x: np.ndarray) -> np.ndarray:
    return x / (np.max(np.abs(x)) + 1e-12)


# ---- percussion: the foundry ------------------------------------------------------------------

def taiko(rng, pitch: float = 68.0) -> np.ndarray:
    n = dsp.seconds(0.7)
    t = dsp.time_axis(n)
    freq = pitch + pitch * 0.7 * np.exp(-t / 0.03)
    body = np.sin(2 * np.pi * np.cumsum(freq) / dsp.RATE) * np.exp(-t / 0.22)
    skin = dsp.bandpass(rng.standard_normal(n), 90, 700) * np.exp(-t / 0.035) * 0.6
    slap = dsp.highpass(rng.standard_normal(n), 1500) * np.exp(-t / 0.006) * 0.25
    return _norm(dsp.soft_clip(body + skin + slap, 1.4))


def kick(rng) -> np.ndarray:
    n = dsp.seconds(0.4)
    t = dsp.time_axis(n)
    freq = 42 + 90 * np.exp(-t / 0.025)
    body = np.sin(2 * np.pi * np.cumsum(freq) / dsp.RATE) * np.exp(-t / 0.16)
    click = dsp.bandpass(rng.standard_normal(n), 1200, 6000) * np.exp(-t / 0.003) * 0.3
    return _norm(dsp.soft_clip(body * 1.3 + click, 1.6))


def anvil(rng, pitch: float) -> np.ndarray:
    """A hammer on an anvil: hard inharmonic ring, a dull thud under it."""
    n = dsp.seconds(0.9)
    t = dsp.time_axis(n)
    out = np.zeros(n)
    for ratio, amp, decay in ((1.0, 1.0, 0.32), (2.32, 0.6, 0.18), (4.25, 0.4, 0.09), (6.63, 0.25, 0.05), (9.38, 0.15, 0.03)):
        out += np.sin(2 * np.pi * pitch * ratio * (1 + rng.uniform(-0.003, 0.003)) * t + rng.uniform(0, 6.28)) * amp * np.exp(-t / decay)
    thud = dsp.lowpass(rng.standard_normal(n), 400) * np.exp(-t / 0.02) * 0.8
    click = dsp.highpass(rng.standard_normal(n), 3000) * np.exp(-t / 0.002) * 0.6
    return _norm(out * 0.7 + thud + click)


def iron_snare(rng) -> np.ndarray:
    n = dsp.seconds(0.35)
    t = dsp.time_axis(n)
    crack = dsp.bandpass(rng.standard_normal(n), 1400, 7500) * np.exp(-t / 0.08)
    body = np.sin(2 * np.pi * 185 * t) * np.exp(-t / 0.05) * 0.7
    metal = (dsp.resonator(crack, 830, 14) * 0.25 + dsp.resonator(crack, 1370, 16) * 0.2)
    return _norm(dsp.soft_clip(crack + body + metal, 1.5))


def hammer_hat(rng) -> np.ndarray:
    n = dsp.seconds(0.09)
    t = dsp.time_axis(n)
    tick = dsp.highpass(rng.standard_normal(n), 5500) * np.exp(-t / 0.018)
    ring = np.sin(2 * np.pi * rng.uniform(6800, 7600) * t) * np.exp(-t / 0.012) * 0.3
    return _norm(tick + ring)


def tom(rng, pitch: float) -> np.ndarray:
    n = dsp.seconds(0.45)
    t = dsp.time_axis(n)
    freq = pitch * (1 + 0.5 * np.exp(-t / 0.025))
    body = np.sin(2 * np.pi * np.cumsum(freq) / dsp.RATE) * np.exp(-t / 0.16)
    skin = dsp.bandpass(rng.standard_normal(n), 150, 1500) * np.exp(-t / 0.025) * 0.4
    return _norm(body + skin)


def riser(rng, length: float) -> np.ndarray:
    n = dsp.seconds(length)
    t = dsp.time_axis(n)
    sweep = dsp.swept_bandpass(rng.standard_normal(n), 400 + 5000 * (t / length) ** 2, q=1.5)
    return _norm(sweep * (t / length) ** 2.5)


# ---- pitched ----------------------------------------------------------------------------------

def bass_note(rng, midi: int, length: float) -> np.ndarray:
    """A short, growling bass: two detuned saws and a sub sine, bright on the attack, distorted."""
    n = dsp.seconds(length)
    t = dsp.time_axis(n)
    f = hz(midi)
    saw = sum(2 * ((f * d * t + rng.uniform(0, 1)) % 1.0) - 1 for d in (0.996, 1.004)) * 0.5
    sub = np.sin(2 * np.pi * f * t) * 0.8
    bright = dsp.lowpass(saw, 1800) * np.exp(-t / 0.04)
    dark = dsp.lowpass(saw, 380)
    env = np.minimum(1, t / 0.004) * np.minimum(1, (length - t) / 0.02)
    return dsp.soft_clip((bright * 0.8 + dark + sub) * env * 1.6, 1.8)


def power_stab(rng, midi: int, length: float) -> np.ndarray:
    notes = [hz(midi - 12), hz(midi - 5), hz(midi)]
    x = dsp.pad(notes, length, rng, attack=0.012, release=length * 0.6, brightness=2600, voices=4)
    return _norm(dsp.soft_clip(x * 3.0, 2.0))


def tremolo_cluster(rng, midis: list[int], length: float) -> np.ndarray:
    x = dsp.pad([hz(m) for m in midis], length, rng, attack=0.25, release=0.3, brightness=4200, voices=6)
    t = dsp.time_axis(len(x))
    rate = BPM / 60 * 4  # sixteenths
    bow = 0.55 + 0.45 * np.clip(np.sin(2 * np.pi * rate * t) * 3, -1, 1)
    return _norm(dsp.highpass(x * bow, 180))


def spiccato(rng, midi: int, length: float = 0.12) -> np.ndarray:
    """A short, bitten string note: a bowed source (detuned saws, a little noise of the bow's
    hair), through a wooden body (a few broad resonances), gone at once."""
    n = dsp.seconds(length)
    t = dsp.time_axis(n)
    f = hz(midi) * (1 + rng.uniform(-0.004, 0.004))
    saw = sum(2 * ((f * (1 + rng.uniform(-0.004, 0.004)) * t + rng.uniform(0, 1)) % 1.0) - 1 for _ in range(3)) / 3
    hair = dsp.bandpass(rng.standard_normal(n), 2500, 9000) * np.exp(-t / 0.012) * 0.35
    source = dsp.lowpass(saw, 5000) + hair
    body = sum(dsp.resonator(source, fr * rng.uniform(0.97, 1.03), q) * g
               for fr, q, g in ((290, 3.0, 0.7), (460, 3.5, 0.6), (1100, 3.0, 0.35), (2700, 2.5, 0.5), (3600, 3.0, 0.3)))
    env = np.minimum(1, t / 0.006) * np.exp(-t / (length * 0.5))
    return dsp.highpass((body + source * 0.15) * env, 180)


def tick(rng) -> np.ndarray:
    n = dsp.seconds(0.03)
    t = dsp.time_axis(n)
    return _norm(dsp.resonator(dsp.highpass(rng.standard_normal(n), 2000) * np.exp(-t / 0.002), rng.uniform(3200, 3800), 20))


def choir_stab(rng, midis: list[int], length: float = 0.55) -> np.ndarray:
    """Souls shrieking in a chord, then sliding down a minor third, as if dragged."""
    n = dsp.seconds(length)
    out = np.zeros(n)
    t = dsp.time_axis(n)
    fall = 2 ** (-3 / 12 * np.clip((t / length - 0.45) / 0.55, 0, 1) ** 1.5)
    for m in midis:
        for k in range(2):
            f0 = np.full(n, hz(m) * (1 + rng.uniform(-0.006, 0.006))) * fall
            f0 *= 1 + 0.012 * np.sin(2 * np.pi * rng.uniform(5, 6.5) * dsp.time_axis(n))
            vowels = V._vowel_track(n, [(0, "a"), (1, "o")], shift=1.15)
            loud = V._track(n, [(0, 0), (0.03, 1.0), (0.45, 0.8), (1, 0)])
            out += V.voice(rng, f0, vowels, loud, breath=0.6, jitter=0.015, shimmer=0.15, rough=0.35)
    return _norm(out)


def music_box(rng, midi: int) -> np.ndarray:
    x = dsp.bell(hz(midi) * 1.004, 0.9, rng, brightness=1.3)
    return x


# ---- stems ------------------------------------------------------------------------------------

def _score_rng() -> np.random.Generator:
    return np.random.default_rng(SCORE_SEED)


def _stem(track: np.ndarray, rng, wet: float, room: float, lufs: float) -> np.ndarray:
    ir = dsp.impulse_response(room, rng, damping_hz=3200, predelay=0.012)
    x = dsp.reverb(track, ir, wet=wet, loop=True)
    x = dsp.circular(lambda s: dsp.highpass(s, 30), x)
    x = dsp.soft_clip(x / (np.max(np.abs(x)) + 1e-12) * 1.4, 1.2)
    # The file starts OFFSET samples before the downbeat, so the loop point falls between hits
    # (the same for every stem; they stay aligned).
    x = np.roll(x, OFFSET, axis=0)
    return dsp.normalise_loudness(x, lufs, peak_ceiling_db=-1.5)


@recipe("combat-pulse", "Kampfmusik, Puls: Taiko und Sub-Kick, galoppierender verzerrter Bass in G# phrygisch, Orgelpunkt des Ofens; 128 bpm, 16 Takte, nahtlos", loop=True)
def combat_pulse(rng) -> np.ndarray:
    score = _score_rng()
    track = np.zeros((N, 2))
    drum = taiko(rng)
    low = kick(rng)
    for bar in range(BARS):
        end_of_phrase = bar % 4 == 3
        for step, gain in ((0, 1.0), (6, 0.55), (8, 0.85), (11, 0.5)):
            _put(track, drum, at(bar, step), gain, -0.1)
        if end_of_phrase:
            for step, gain in ((12, 0.5), (13, 0.6), (14, 0.75), (15, 0.9)):
                _put(track, drum, at(bar, step), gain, 0.15)
        _put(track, low, at(bar, 0), 0.9)
        _put(track, low, at(bar, 8), 0.75)
        root = ROOTS[bar]
        # Gallop: x . x x on every beat; the first of each beat an octave down.
        for beat in range(4):
            for sub, octave, gain in ((0, -12, 1.0), (2, 0, 0.7), (3, 0, 0.8)):
                note = bass_note(rng, root + octave, SIXTEENTH / dsp.RATE * (1.6 if sub == 0 else 0.9))
                _put(track, note, at(bar, beat * 4 + sub), gain * (0.9 + 0.2 * score.random()))
    # The furnace's pedal tone under everything (G#1 and its fifth), breathing slowly.
    pedal = dsp.drone(hz(32), N / dsp.RATE, rng, movement=0.3) + 0.5 * dsp.drone(hz(39), N / dsp.RATE, rng, movement=0.3)
    track += dsp.pan(dsp.lowpass(pedal, 220), 0.0) * 0.18
    return _stem(track, rng, 0.16, 1.6, -20.0)


@recipe("combat-drive", "Kampfmusik, Antrieb: Ambosse auf dem Backbeat, Eisen-Snare, gehämmerte Hats, Blechstöße in Quinten, Tom-Fills am Phrasenende; 128 bpm, 16 Takte, nahtlos", loop=True)
def combat_drive(rng) -> np.ndarray:
    score = _score_rng()
    track = np.zeros((N, 2))
    anvils = [anvil(rng, 1180.0), anvil(rng, 1560.0)]
    snare = iron_snare(rng)
    hats = [hammer_hat(rng) for _ in range(3)]
    toms = [tom(rng, p) for p in (190, 150, 120, 95)]
    for bar in range(BARS):
        root = ROOTS[bar]
        for beat, gain in ((1, 0.8), (3, 1.0)):
            _put(track, anvils[beat // 2], at(bar, beat * 4), gain * 0.55, -0.35 if beat == 1 else 0.35)
            _put(track, snare, at(bar, beat * 4), gain * 0.8, 0.05)
        for ghost in (7, 15):
            if score.random() < 0.6:
                _put(track, snare, at(bar, ghost), 0.18, 0.1)
        for step in range(16):
            accent = (1.0, 0.35, 0.65, 0.4)[step % 4]
            _put(track, hats[score.integers(0, 3)], at(bar, step), 0.32 * accent * (0.85 + 0.3 * score.random()), 0.45)
        if bar % 4 == 0:
            _put(track, power_stab(rng, root + 12, 0.42), at(bar, 0), 0.75)
        if bar % 4 == 2:
            _put(track, power_stab(rng, root + 12, 0.26), at(bar, 6), 0.6)
        if bar % 8 == 7:
            for k, step in enumerate(range(8, 16)):
                _put(track, toms[min(3, k // 2)], at(bar, step), 0.55 + 0.05 * k, -0.4 + 0.1 * k)
        if bar % 4 == 3 and score.random() < 0.7:
            _put(track, anvils[1], at(bar, 14), 0.3, 0.6)
    return _stem(track, rng, 0.2, 1.8, -21.0)


@recipe("combat-frenzy", "Kampfmusik, Raserei: Streicher-Tremolo in Clustern mit kleiner Sekunde, Chorschreie, verstimmte Spieluhr, Riser in jede Phrase; 128 bpm, 16 Takte, nahtlos", loop=True)
def combat_frenzy(rng) -> np.ndarray:
    score = _score_rng()
    track = np.zeros((N, 2))
    bar_seconds = 16 * SIXTEENTH / dsp.RATE
    clock = tick(rng)
    for bar in range(BARS):
        root = ROOTS[bar]
        # Root, flat second and fifth: a grinding cluster held quietly underneath...
        cluster = tremolo_cluster(rng, [root + 12, root + 13, root + 19], bar_seconds + 0.25)
        _put(track, cluster, at(bar, 0), 0.18, -0.5 if bar % 2 else 0.5)
        # ...and the same notes bitten off in sixteenths: the urgency of the fight.
        figure = [root + 12, root + 13, root + 12, root + 19, root + 12, root + 13, root + 24, root + 19]
        for step in range(16):
            accent = 1.0 if step % 4 == 0 else (0.75 if step % 2 == 0 else 0.55)
            note = figure[step % 8] + (12 if bar % 4 == 3 and step >= 8 else 0)
            _put(track, spiccato(rng, note), at(bar, step), 0.55 * accent, -0.35)
            _put(track, spiccato(rng, note + 12), at(bar, step), 0.3 * accent, 0.35)
        if bar % 2 == 1:
            _put(track, choir_stab(rng, [root + 24, root + 25, root + 31]), at(bar, 0), 0.5, 0.0)
        # A clock running too fast, high and dry.
        for step in range(0, 16):
            if step % 2 == 1 or score.random() < 0.3:
                _put(track, clock, at(bar, step), 0.12 * (0.7 + 0.3 * score.random()), 0.7 if step % 4 < 2 else -0.7)
        if bar >= 8:
            arp = [root + 36, root + 39, root + 43, root + 37]
            for step in range(0, 16, 2):
                _put(track, music_box(rng, arp[(step // 2) % 4]), at(bar, step), 0.16, -0.6 + 1.2 * ((step // 2) % 2))
    for phrase_start in (0, 8):
        swell = riser(rng, 1.8)
        _put(track, swell, at(phrase_start) - len(swell), 0.5, 0.0)
    return _stem(track, rng, 0.28, 2.2, -22.0)
