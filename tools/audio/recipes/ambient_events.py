"""Sounds of the places themselves, heard now and then from somewhere in the room
(AudioDirector.UpdateAmbientEvents), on top of each zone's steady bed.

Owner, 07.10.2026: atmosphere, tension and feeling may be more creative. The beds hold the room;
these give it a life and a history, from the region contracts:
- the foundry (industrial-cathedral.md): the works bell that gave the town its rhythm still
  tolls far off, for a shift that never ends;
- the shore (prologue.md): the departure board flips its letters for a ferry that never comes,
  and out on the water that ferry's horn sounds, far away, never closer.

Mono 48 kHz, distant (filtered, with room), peaks below -3 dBFS; takes differ by seed.
"""
from __future__ import annotations

import numpy as np

import dsp
from recipes import recipe


def _finish(x: np.ndarray, lufs: float, peak: float = -3.0, fade_out: float = 0.2) -> np.ndarray:
    x = dsp.highpass(x, 35)
    x = dsp.fades(x, 0.003, fade_out)
    return dsp.normalise_loudness(x, lufs, peak_ceiling_db=peak)


def _distant(x: np.ndarray, rng, seconds: float, wet: float, damping: float, lowpass: float) -> np.ndarray:
    """Far away in a big room: the highs gone, much of it reverberation."""
    ir = dsp.impulse_response(seconds, rng, damping_hz=damping, predelay=0.03)
    return dsp.lowpass(dsp.reverb(x, ir, wet=wet).mean(axis=1), lowpass)


def _hit(rng, n: int, at: float, decay: float, low: float, high: float) -> np.ndarray:
    """A broadband knock: the excitation that metal rings from."""
    out = np.zeros(n)
    start = dsp.seconds(at)
    m = min(n - start, dsp.seconds(decay * 6))
    if m > 8:
        out[start:start + m] = dsp.bandpass(rng.standard_normal(m), low, high) * np.exp(-np.arange(m) / (dsp.RATE * decay))
    return out


@recipe("foundry-bell", "Gießhalle: die Werksglocke schlägt einmal, weit weg über der Stadt, die es nicht mehr gibt")
def foundry_bell(rng) -> np.ndarray:
    x = dsp.bell(rng.uniform(196, 233), 5.5, rng, brightness=0.8)
    n = len(x)
    x = np.concatenate([x, np.zeros(dsp.seconds(1.0))])
    return _finish(_distant(x, rng, 3.0, 0.5, 2000, 2600), -29.0, fade_out=0.6)


@recipe("shore-board", "Ufer: die Fallblattanzeige blättert ihre Buchstaben durch, ein trockenes Klappern, das abrupt stehen bleibt")
def shore_board(rng) -> np.ndarray:
    n = dsp.seconds(1.8)
    x = np.zeros(n)
    flaps = int(rng.integers(18, 30))
    at = 0.02
    for k in range(flaps):
        # Each flap: a thin card snapping against the next, a tick and a tiny hollow knock.
        tick = _hit(rng, n, at, 0.0012, 3000, 12000)
        knock = _hit(rng, n, at + 0.002, 0.004, 800, 3000)
        x += tick * rng.uniform(0.6, 1.0) + dsp.resonator(knock, rng.uniform(1400, 1900), 6) * 0.35
        at += rng.uniform(0.028, 0.042) * (1 + 0.8 * (k / flaps) ** 4)
        if at > 1.4:
            break
    # The last flap lands harder: the board stops on its word.
    x += _hit(rng, n, at + 0.03, 0.006, 600, 8000) * 1.4
    return _finish(_distant(x, rng, 1.2, 0.25, 6000, 9000), -30.0)


@recipe("shore-horn", "Ufer: weit draußen auf dem Wasser das Horn einer Fähre, tief und lang, im Nebel verweht – sie kommt nie näher")
def shore_horn(rng) -> np.ndarray:
    n = dsp.seconds(5.0)
    t = dsp.time_axis(n)
    base = rng.uniform(68, 78)
    drift = 1 + 0.006 * np.sin(2 * np.pi * 0.4 * t)
    env = np.clip(t / 0.5, 0, 1) * np.clip((3.6 - t) / 0.9, 0, 1)
    phase = np.cumsum(base * drift) / dsp.RATE
    tone = sum(np.sin(2 * np.pi * phase * h + rng.uniform(0, 6.28)) / h ** 1.1 for h in range(1, 9))
    # A second, slightly flat whistle a fifth up: ship horns rarely speak with one voice.
    tone += 0.5 * sum(np.sin(2 * np.pi * phase * 1.495 * h + rng.uniform(0, 6.28)) / h ** 1.3 for h in range(1, 6))
    x = dsp.lowpass(tone, 900) * env
    x += dsp.bandpass(rng.standard_normal(n), 150, 800) * env * 0.05
    return _finish(_distant(x, rng, 3.5, 0.6, 1500, 1200), -30.0, fade_out=0.8)


@recipe("skiff-creak", "Überfahrt: das Boot legt sich in die Dünung, seine Planken und Spanten ächzen langsam, Wasser schlägt dumpf gegen den Rumpf")
def skiff_creak(rng) -> np.ndarray:
    n = dsp.seconds(2.2)
    t = dsp.time_axis(n)
    # Wood under slow load: stick-slip friction, its pulse rate rising and falling with the
    # strain, ringing the hull's low wooden modes (a slow groan, not a door's squeak).
    pulses = np.zeros(n)
    tt = 0.12
    while tt < 1.6:
        i = dsp.seconds(tt)
        pulses[i] = rng.uniform(0.5, 1.0) * np.sin(np.pi * np.clip((tt - 0.1) / 1.5, 0, 1))
        tt += 1.0 / (18 + 40 * np.sin(np.pi * np.clip((tt - 0.1) / 1.5, 0, 1)) + rng.uniform(-4, 4))
    wood = sum(dsp.resonator(pulses, f * rng.uniform(0.93, 1.07), q) * g for f, q, g in ((140, 8, 1.0), (260, 9, 0.7), (430, 10, 0.4), (720, 12, 0.2)))
    wood = wood / (np.max(np.abs(wood)) + 1e-9)
    # The water answering: a dull slap against the hull and its wash.
    slap_at = rng.uniform(1.2, 1.5)
    slap = dsp.lowpass(_hit(rng, n, slap_at, 0.05, 80, 900), 700) * 0.9
    wash = dsp.bandpass(rng.standard_normal(n), 300, 2500) * np.exp(-((t - slap_at - 0.15) / 0.18) ** 2) * 0.25
    x = wood * 0.8 + slap + wash
    return _finish(_distant(x, rng, 0.6, 0.15, 3000, 4000), -27.0, fade_out=0.3)
