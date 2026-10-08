"""Music beds per zone, composed as short scores and rendered with dsp instruments.

Every piece shares the arena music's key (G-sharp minor) and one motif: a falling figure
D#-C#-B-G# that stops on A# (unresolved: the lost soul that cannot leave). The threshold and
the ending let it resolve upward to B. Instruments are the ones synthesis does well: drones,
cast bells, plucked strings, slow string and choir pads under a hall; no exposed synthetic lead.
Pieces loop seamlessly (the hall's tail wraps around).
"""
from __future__ import annotations

import math
from dataclasses import dataclass

import numpy as np

import dsp
from recipes import recipe


def hz(midi: float) -> float:
    return 440.0 * 2 ** ((midi - 69) / 12)


# Pitches (MIDI) in G-sharp minor.
GS2, DS3, GS3, B3, CS4, DS4, E4, FS4, GS4, AS4, B4, CS5, DS5, E5, FS5, GS5 = 44, 51, 56, 59, 61, 63, 64, 66, 68, 70, 71, 73, 75, 76, 78, 80
MOTIF = [(DS5, 1.0), (CS5, 1.0), (B4, 1.0), (GS4, 2.0), (AS4, 3.0)]
RESOLVED = [(DS5, 1.0), (CS5, 1.0), (B4, 1.0), (GS4, 1.5), (AS4, 1.0), (B4, 3.5)]
# Chords (root position voicings around the middle register).
CHORDS = {
    "i": [56, 63, 68, 71],     # G#m
    "VI": [52, 59, 64, 68],    # E
    "III": [47, 59, 63, 66],   # B
    "VII": [54, 58, 61, 66],   # F#
    "iv": [49, 56, 61, 64],    # C#m
    "I": [59, 63, 66, 71],     # B major (threshold)
    "IV": [52, 59, 64, 68],    # E major (threshold)
}


@dataclass
class Piece:
    bpm: float
    beats: float

    @property
    def seconds(self) -> float:
        return self.beats * 60.0 / self.bpm

    def at(self, beat: float) -> int:
        return dsp.seconds(beat * 60.0 / self.bpm)

    def length(self, beats: float) -> float:
        return beats * 60.0 / self.bpm


def choir(freqs: list[float], duration: float, rng, vowel=(700, 1220, 2600)) -> np.ndarray:
    """A wordless choir: detuned voices on an 'ah', slow swell."""
    raw = dsp.pad(freqs, duration, rng, attack=duration * 0.4, release=duration * 0.35, brightness=4200, voices=6)
    out = sum(dsp.resonator(raw, f, q) * g for f, q, g in zip(vowel, (6, 8, 10), (1.0, 0.6, 0.25)))
    return out / (np.max(np.abs(out)) + 1e-9) * 0.6


def bowed(freq: float, duration: float, rng) -> np.ndarray:
    """A low bowed string note (cello-like body), soft attack, slow vibrato, warm."""
    n = dsp.seconds(duration)
    t = dsp.time_axis(n)
    vibrato = 1 + 0.004 * np.sin(2 * np.pi * 5.0 * t) * np.minimum(1, t / 0.6)
    phase = np.cumsum(freq * vibrato) / dsp.RATE
    saw = 2 * (phase % 1.0) - 1
    body = dsp.resonator(saw, 220, 2.5) * 0.5 + dsp.resonator(saw, 450, 3) * 0.4 + dsp.lowpass(saw, 1800) * 0.4
    env = np.minimum(1, t / 0.35) * np.minimum(1, (duration - t) / 0.5).clip(0, 1)
    return body * env * 0.5


def spiccato(freq: float, rng) -> np.ndarray:
    """A short bowed note (off the string): quick attack, quick decay, woody body."""
    note = bowed(freq, 0.32, rng)
    t = dsp.time_axis(len(note))
    return note * np.exp(-t / 0.11) * 1.6


def tremolo(freqs: list[float], duration: float, rng, rate: float = 7.5) -> np.ndarray:
    """String tremolo: the pad's level shivering, darker and quieter, for tension."""
    sound = dsp.pad(freqs, duration, rng, attack=duration * 0.3, release=1.5, brightness=1500, voices=6)
    t = dsp.time_axis(len(sound))
    shiver = 0.65 + 0.35 * np.abs(np.sin(np.pi * rate * t + rng.uniform(0, 3)))
    return sound * shiver


def low_drum(rng, depth: float = 1.0) -> np.ndarray:
    """A big skin drum (taiko, frame drum): a noise strike ringing through the drum's body
    modes, a slight pitch sag, a long soft tail; not an electronic kick."""
    n = dsp.seconds(1.6)
    t = dsp.time_axis(n)
    strike = rng.standard_normal(n) * np.exp(-t / 0.012)
    body = np.zeros(n)
    for freq, q, gain in ((62 * depth, 9, 1.0), (98 * depth, 11, 0.6), (141 * depth, 13, 0.4), (215 * depth, 15, 0.25)):
        body += dsp.resonator(strike, freq * (1 + 0.04 * np.exp(-t[0] / 0.05)), q) * gain
    body *= np.exp(-t / (0.42 * depth))
    skin = dsp.bandpass(rng.standard_normal(n), 300, 2400) * np.exp(-t / 0.025) * 0.15
    out = body + skin
    return out / (np.max(np.abs(out)) + 1e-9) * 0.9


def render(piece: Piece, events, rng, hall_seconds: float = 4.5, wet: float = 0.45) -> np.ndarray:
    """events: (beat, instrument, args...) -> stereo mix, reverb wrapped so the loop is seamless."""
    n = piece.at(piece.beats)
    mix = np.zeros((n, 2))
    for beat, kind, *args in events:
        start = piece.at(beat) % n
        if kind == "bell":
            midi, gain, pan = args
            sound = dsp.bell(hz(midi), 7.0, rng, brightness=0.55) * gain
        elif kind == "pluck":
            midi, gain, pan = args
            sound = dsp.lowpass(dsp.pluck(hz(midi), 3.0, rng, damping=0.997, bright=0.35), 3200) * gain
        elif kind == "pad":
            chord, beats, gain, pan = args
            sound = dsp.pad([hz(m) for m in CHORDS[chord]], piece.length(beats) + 2.0, rng, attack=piece.length(beats) * 0.35,
                            release=2.5, brightness=1300, voices=5) * gain
        elif kind == "choir":
            chord, beats, gain, pan = args
            sound = choir([hz(m + 12) for m in CHORDS[chord][1:]], piece.length(beats) + 2.0, rng) * gain
        elif kind == "drone":
            midi, beats, gain, pan = args
            sound = dsp.drone(hz(midi), piece.length(beats), rng, movement=0.25) * gain
            sound *= np.minimum(1, dsp.time_axis(len(sound)) / 3.0) * np.minimum(1, (piece.length(beats) - dsp.time_axis(len(sound))) / 3.0).clip(0, 1)
        elif kind == "bowed":
            midi, beats, gain, pan = args
            sound = bowed(hz(midi), piece.length(beats) + 0.4, rng) * gain
        elif kind == "drum":
            gain, pan = args
            sound = low_drum(rng, depth=rng.uniform(0.85, 1.25)) * gain * rng.uniform(0.85, 1.1)
        elif kind == "spiccato":
            midi, gain, pan = args
            sound = spiccato(hz(midi), rng) * gain * rng.uniform(0.8, 1.1)
        elif kind == "tremolo":
            chord, beats, gain, pan = args
            sound = tremolo([hz(m) for m in CHORDS[chord]], piece.length(beats) + 1.5, rng) * gain
        else:
            raise ValueError(kind)
        dsp.place(mix, dsp.pan(sound, pan), start)
    ir = dsp.impulse_response(hall_seconds, rng, damping_hz=3800, predelay=0.03)
    out = dsp.reverb(mix, ir, wet=wet, loop=True)
    return dsp.circular(lambda x: dsp.highpass(x, 32), out)


def motif(start: float, voice: str, gain: float, transpose: int = 0, figure=MOTIF, pan: float = 0.0) -> list:
    events, beat = [], start
    for midi, length in figure:
        events.append((beat, voice, midi + transpose, gain, pan))
        beat += length
    return events


@recipe("music-title", "Titel: Glocken-Motiv über Drone und Streicherflächen, Chor in der zweiten Hälfte", loop=True)
def title(rng) -> np.ndarray:
    piece = Piece(bpm=66, beats=64)
    events = [(0, "drone", GS2, 64, 0.22, 0.0), (0, "drone", DS3, 64, 0.1, 0.0)]
    progression = ["i", "VI", "III", "VII"] * 2
    for index, chord in enumerate(progression):
        events.append((index * 8, "pad", chord, 8, 0.16, -0.2 + 0.4 * (index % 2)))
    for bar in (0, 16, 32, 48):
        events += motif(bar + 2, "bell", 0.5, pan=0.15)
    for index, chord in enumerate(progression[4:]):
        events.append((32 + index * 8, "choir", chord, 8, 0.14, 0.0))
        notes = CHORDS[chord]
        for k in range(8):
            events.append((32 + index * 8 + k, "pluck", notes[k % 4] + 12, 0.18, -0.4 + 0.1 * k))
    mix = render(piece, events, rng)
    return dsp.normalise_loudness(mix, -20.0, peak_ceiling_db=-3.0)


@recipe("music-shore", "Ufer und Hafen: sparsam, Drone, ferne Glockenfragmente, tiefe Streichertöne", loop=True)
def shore(rng) -> np.ndarray:
    piece = Piece(bpm=56, beats=64)
    events = [(0, "drone", GS2, 64, 0.2, 0.0)]
    for index, chord in enumerate(["i", "VI", "i", "VII", "i", "VI", "iv", "VII"]):
        events.append((index * 8, "pad", chord, 8, 0.11, 0.0))
    for start, transpose in ((4, 0), (36, -12)):
        events += motif(start, "bell", 0.36, transpose=transpose, pan=-0.3)
    for beat, midi in ((16, GS3), (24, E4 - 12), (48, CS4 - 12), (56, DS3)):
        events.append((beat, "bowed", midi, 6, 0.32, 0.2))
    mix = render(piece, events, rng, hall_seconds=5.5, wet=0.5)
    return dsp.normalise_loudness(mix, -23.0, peak_ceiling_db=-4.0)


@recipe("music-hub", "Vorhalle: Orgelartige Warden-Fläche, tiefe Glocke, Motiv in der Tiefe", loop=True)
def hub(rng) -> np.ndarray:
    piece = Piece(bpm=60, beats=64)
    events = [(0, "drone", GS2, 64, 0.24, 0.0), (0, "drone", GS2 - 12, 64, 0.16, 0.0)]
    for index, chord in enumerate(["i", "iv", "VI", "VII"] * 2):
        events.append((index * 8, "pad", chord, 8, 0.15, 0.0))
    for beat in (0, 32):
        events.append((beat, "bell", GS3 - 12, 0.4, -0.1))
    events += motif(10, "pluck", 0.3, transpose=-12, pan=0.25)
    events += motif(42, "bell", 0.28, transpose=-12, pan=-0.25)
    mix = render(piece, events, rng, hall_seconds=6.0, wet=0.55)
    return dsp.normalise_loudness(mix, -22.0, peak_ceiling_db=-4.0)


@recipe("music-causeway", "Damm: Herzschlag-Trommel, Streicher-Puls, das Motiv gestrichen", loop=True)
def causeway(rng) -> np.ndarray:
    piece = Piece(bpm=72, beats=64)
    events = [(0, "drone", GS2, 64, 0.22, 0.0)]
    for bar in range(0, 16, 2):
        events.append((bar * 4, "drum", 0.6, 0.0))
        if bar % 4 == 2:
            events.append((bar * 4 + 3.5, "drum", 0.28, 0.25))
    for index, chord in enumerate(["i", "VI", "iv", "VII"] * 2):
        events.append((index * 8, "tremolo", chord, 8, 0.12, 0.0))
        notes = CHORDS[chord]
        for k in (0, 3, 4, 6.5):
            events.append((index * 8 + k, "spiccato", notes[0] - 12, 0.3, -0.25))
    for start in (8, 40):
        for midi, length in MOTIF:
            events.append((start, "bowed", midi - 12, length + 0.5, 0.38, 0.2))
            start += length
    mix = render(piece, events, rng, hall_seconds=4.5, wet=0.5)
    return dsp.normalise_loudness(mix, -20.0, peak_ceiling_db=-3.0)


@recipe("music-crossing", "Überfahrt: treibende Trommeln, Zupf-Ostinato, Streicher, Motiv", loop=True)
def crossing(rng) -> np.ndarray:
    piece = Piece(bpm=104, beats=96)
    events = [(0, "drone", GS2, 96, 0.18, 0.0)]
    patterns = [[0, 2, 2.75], [0, 1.5, 2, 3.5], [0, 2, 2.75], [0, 0.75, 1.5, 2, 3, 3.5]]
    for bar in range(24):
        for hit in patterns[bar % 4]:
            events.append((bar * 4 + hit, "drum", 0.55 if hit in (0, 2) else 0.3, 0.0 if hit in (0, 2) else (-0.3 if hit < 2 else 0.3)))
    for index, chord in enumerate(["i", "VI", "III", "VII", "i", "iv", "VI", "VII"] * 1 + ["i", "VI", "III", "VII"]):
        events.append((index * 8, "tremolo", chord, 8, 0.13, 0.0))
        notes = CHORDS[chord]
        for k in range(16):
            events.append((index * 8 + k * 0.5, "spiccato", notes[(k * 3) % 4] - (12 if k % 4 == 0 else 0), 0.24, -0.3 + 0.04 * (k % 8)))
    for start in (16, 48, 80):
        events += motif(start, "bell", 0.42, pan=0.2)
    mix = render(piece, events, rng, hall_seconds=3.6, wet=0.42)
    return dsp.normalise_loudness(mix, -18.0, peak_ceiling_db=-2.5)


@recipe("music-threshold", "Schwelle: ruhige Wärme, Chor und Streicher in H-Dur, das Motiv löst sich auf", loop=True)
def threshold(rng) -> np.ndarray:
    piece = Piece(bpm=58, beats=48)
    events = [(0, "drone", 47, 48, 0.2, 0.0)]
    for index, chord in enumerate(["I", "IV", "I", "VII", "I", "IV"]):
        events.append((index * 8, "pad", chord, 8, 0.15, 0.0))
        events.append((index * 8, "choir", chord, 8, 0.12, 0.0))
    events += motif(6, "bell", 0.4, figure=RESOLVED, pan=0.1)
    events += motif(30, "bell", 0.3, transpose=-12, figure=RESOLVED, pan=-0.2)
    mix = render(piece, events, rng, hall_seconds=6.0, wet=0.55)
    return dsp.normalise_loudness(mix, -22.0, peak_ceiling_db=-4.0)
