"""Small DSP kit for authoring the game's ambiences, music beds and missing cues locally.

Everything here is deterministic (seeded) numpy/scipy code, so a sound can be rebuilt exactly
from its recipe in tools/audio/recipes/*.py and its seed. No sample libraries, no network.
Signals are float64 arrays: mono (n,) or stereo (n, 2), at RATE.
"""
from __future__ import annotations

import math
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

RATE = 48000


def seconds(duration: float) -> int:
    return int(round(duration * RATE))


def time_axis(n: int) -> np.ndarray:
    return np.arange(n) / RATE


# ---- sources -------------------------------------------------------------------------------

def white(n: int, rng: np.random.Generator) -> np.ndarray:
    return rng.standard_normal(n)


def pink(n: int, rng: np.random.Generator) -> np.ndarray:
    """1/f noise by spectral shaping, unit RMS."""
    spectrum = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / RATE)
    freqs[0] = freqs[1]
    spectrum /= np.sqrt(freqs)
    out = np.fft.irfft(spectrum, n)
    return out / (np.std(out) + 1e-12)


def brown(n: int, rng: np.random.Generator) -> np.ndarray:
    spectrum = np.fft.rfft(rng.standard_normal(n))
    freqs = np.fft.rfftfreq(n, 1 / RATE)
    freqs[0] = freqs[1]
    spectrum /= freqs
    out = np.fft.irfft(spectrum, n)
    return out / (np.std(out) + 1e-12)


def smooth_random(n: int, rate_hz: float, rng: np.random.Generator) -> np.ndarray:
    """A slow random curve in 0..1 (cubic interpolation of random points every 1/rate_hz s)."""
    points = max(4, int(n / RATE * rate_hz) + 4)
    values = rng.random(points)
    x = np.linspace(0, points - 3, n)
    from scipy.interpolate import CubicSpline
    curve = CubicSpline(np.arange(points), values)(x + 1)
    return np.clip(curve, 0, 1)


def smooth_loop(n: int, rate_hz: float, rng: np.random.Generator) -> np.ndarray:
    """Like smooth_random, but periodic over n samples (for seamless loops)."""
    points = max(4, int(round(n / RATE * rate_hz)))
    values = rng.random(points)
    from scipy.interpolate import CubicSpline
    knots = np.arange(points + 1)
    curve = CubicSpline(knots, np.append(values, values[0]), bc_type="periodic")
    return np.clip(curve(np.linspace(0, points, n, endpoint=False)), 0, 1)


def circular(process, x: np.ndarray) -> np.ndarray:
    """Run a causal filter chain on a loop so its end flows into its start: process the loop
    twice in a row and keep the second pass (the filter state has wrapped around)."""
    doubled = np.concatenate([x, x], axis=0)
    return process(doubled)[len(x):]


def sine(freq: float | np.ndarray, n: int, phase: float = 0.0) -> np.ndarray:
    if np.isscalar(freq):
        return np.sin(2 * np.pi * freq * time_axis(n) + phase)
    return np.sin(2 * np.pi * np.cumsum(freq) / RATE + phase)


# ---- filters -------------------------------------------------------------------------------

def _sos(kind: str, cutoff, order: int = 2):
    return signal.butter(order, cutoff, btype=kind, fs=RATE, output="sos")


def lowpass(x: np.ndarray, cutoff: float, order: int = 2) -> np.ndarray:
    return signal.sosfilt(_sos("lowpass", cutoff, order), x, axis=0)


def highpass(x: np.ndarray, cutoff: float, order: int = 2) -> np.ndarray:
    return signal.sosfilt(_sos("highpass", cutoff, order), x, axis=0)


def bandpass(x: np.ndarray, low: float, high: float, order: int = 2) -> np.ndarray:
    return signal.sosfilt(_sos("bandpass", [low, high], order), x, axis=0)


def resonator(x: np.ndarray, freq: float, q: float) -> np.ndarray:
    """A two-pole resonance (peaking bandpass) at freq with quality q."""
    b, a = signal.iirpeak(freq, q, fs=RATE)
    return signal.lfilter(b, a, x, axis=0)


def swept_bandpass(x: np.ndarray, centre: np.ndarray, q: float = 2.0, block: int = 256) -> np.ndarray:
    """Bandpass whose centre frequency follows `centre` (Hz per sample), block-wise state-variable filter."""
    out = np.zeros_like(x)
    low = band = 0.0
    for start in range(0, len(x), block):
        f = float(centre[min(start, len(centre) - 1)])
        g = 2 * math.sin(math.pi * min(f, RATE / 6) / RATE)
        damping = 1.0 / q
        for i in range(start, min(start + block, len(x))):
            high = x[i] - low - damping * band
            band += g * high
            low += g * band
            out[i] = band
    return out


# ---- envelopes and shaping -----------------------------------------------------------------

def envelope(n: int, attack: float, release: float, curve: float = 2.0) -> np.ndarray:
    """Attack then exponential-ish decay over the whole length."""
    t = time_axis(n)
    attack_part = np.clip(t / max(attack, 1e-4), 0, 1)
    decay = np.exp(-np.maximum(t - attack, 0) / max(release, 1e-4) * curve)
    return attack_part * decay


def fades(x: np.ndarray, fade_in: float = 0.006, fade_out: float = 0.006) -> np.ndarray:
    x = x.copy()
    a, b = seconds(fade_in), seconds(fade_out)
    if a:
        ramp = np.linspace(0, 1, a)
        x[:a] *= ramp if x.ndim == 1 else ramp[:, None]
    if b:
        ramp = np.linspace(1, 0, b)
        x[-b:] *= ramp if x.ndim == 1 else ramp[:, None]
    return x


def soft_clip(x: np.ndarray, drive: float = 1.0) -> np.ndarray:
    return np.tanh(x * drive) / np.tanh(drive)


def pan(x: np.ndarray, position: float | np.ndarray) -> np.ndarray:
    """Equal-power pan of a mono signal; position -1 (left) .. 1 (right)."""
    angle = (np.asarray(position) + 1) * np.pi / 4
    return np.stack([x * np.cos(angle), x * np.sin(angle)], axis=-1)


def place(target: np.ndarray, sound: np.ndarray, at: int, gain: float = 1.0) -> None:
    """Add `sound` into `target` starting at sample `at`, wrapping around (for loops)."""
    n = len(target)
    end = at + len(sound)
    if end <= n:
        target[at:end] += sound * gain
    else:
        first = n - at
        target[at:] += sound[:first] * gain
        target[: len(sound) - first] += sound[first:] * gain


# ---- space ---------------------------------------------------------------------------------

def impulse_response(duration: float, rng: np.random.Generator, damping_hz: float = 4000.0,
                     predelay: float = 0.012, early: int = 10, stereo_spread: float = 1.0) -> np.ndarray:
    """A synthetic room: early reflections plus an exponentially decaying, darkening noise tail."""
    n = seconds(duration)
    t = time_axis(n)
    tail = np.stack([rng.standard_normal(n), rng.standard_normal(n)], axis=-1)
    tail[:, 1] = tail[:, 0] * (1 - stereo_spread) + tail[:, 1] * stereo_spread
    decay = np.exp(-6.9 * t / duration)[:, None]
    tail *= decay
    # Darken as it decays: lowpass the tail in two halves.
    tail = lowpass(tail, damping_hz, order=1) * 0.6 + lowpass(tail, damping_hz * 0.35, order=1) * 0.4
    ir = np.zeros((n, 2))
    start = seconds(predelay)
    ir[start:] += tail[: n - start] * 0.5
    for _ in range(early):
        at = start + int(rng.uniform(0.002, 0.06) * RATE)
        if at < n:
            ir[at] += rng.uniform(-0.8, 0.8, size=2)
    return ir / (np.sqrt(np.sum(ir ** 2)) + 1e-12)


def reverb(x: np.ndarray, ir: np.ndarray, wet: float = 0.3, loop: bool = False) -> np.ndarray:
    """Convolve with a stereo IR; with `loop` the tail wraps around so a loop stays seamless."""
    stereo = x if x.ndim == 2 else np.stack([x, x], axis=-1)
    n = len(stereo)
    out = np.zeros_like(stereo)
    for channel in range(2):
        tail = signal.fftconvolve(stereo[:, channel], ir[:, channel])
        if loop:
            folded = tail[:n].copy()
            rest = tail[n:]
            while len(rest):
                take = min(n, len(rest))
                folded[:take] += rest[:take]
                rest = rest[take:]
            out[:, channel] = folded
        else:
            out[:, channel] = tail[:n]
    return stereo * (1 - wet) + out * wet


# ---- instruments ---------------------------------------------------------------------------

#: Partials of a cast bell (hum, prime, tierce, quint, nominal, upper partials), relative to the strike note.
BELL_PARTIALS = [(0.5, 0.55, 1.0), (1.0, 1.0, 0.75), (1.183, 0.6, 0.55), (1.506, 0.35, 0.45), (2.0, 0.45, 0.35),
                 (2.514, 0.2, 0.25), (2.662, 0.18, 0.22), (3.011, 0.12, 0.18), (4.166, 0.08, 0.12)]


def bell(freq: float, duration: float, rng: np.random.Generator, brightness: float = 1.0) -> np.ndarray:
    n = seconds(duration)
    t = time_axis(n)
    out = np.zeros(n)
    for ratio, amp, decay_share in BELL_PARTIALS:
        detune = 1 + rng.uniform(-0.002, 0.002)
        partial = np.sin(2 * np.pi * freq * ratio * detune * t + rng.uniform(0, 6.28))
        # Beating of the slightly split partial pair.
        partial *= 1 + 0.25 * np.sin(2 * np.pi * rng.uniform(0.3, 1.2) * t)
        out += partial * amp * (brightness ** (ratio > 2)) * np.exp(-t / (duration * decay_share * 0.45))
    strike = rng.standard_normal(seconds(0.01)) * np.linspace(1, 0, seconds(0.01))
    out[: len(strike)] += highpass(strike, 2000) * 0.3
    return out / (np.max(np.abs(out)) + 1e-12)


def pad(freqs: list[float], duration: float, rng: np.random.Generator, attack: float = 2.5, release: float = 3.0,
        brightness: float = 1600.0, voices: int = 5) -> np.ndarray:
    """A bowed-string/choir-like pad: detuned saw voices per note, lowpassed, slow swell."""
    n = seconds(duration)
    t = time_axis(n)
    out = np.zeros(n)
    for freq in freqs:
        for voice in range(voices):
            detune = freq * (1 + rng.uniform(-0.006, 0.006))
            vibrato = 1 + 0.003 * np.sin(2 * np.pi * rng.uniform(4.5, 5.5) * t + rng.uniform(0, 6.28))
            phase = np.cumsum(detune * vibrato) / RATE
            saw = 2 * (phase % 1.0) - 1
            out += saw / voices
    out = lowpass(out, brightness, order=2)
    env = np.minimum(1, t / attack) * np.minimum(1, np.maximum(0, (duration - t)) / release)
    return out * env / (len(freqs) ** 0.5)


def pluck(freq: float, duration: float, rng: np.random.Generator, damping: float = 0.996, bright: float = 0.5) -> np.ndarray:
    """Karplus-Strong string."""
    n = seconds(duration)
    period = max(2, int(RATE / freq))
    buffer = rng.uniform(-1, 1, period)
    buffer = lowpass(buffer, 2000 + 6000 * bright, order=1)
    out = np.zeros(n)
    for i in range(n):
        value = buffer[i % period]
        out[i] = value
        buffer[i % period] = damping * 0.5 * (value + buffer[(i + 1) % period])
    return out


def drone(freq: float, duration: float, rng: np.random.Generator, movement: float = 0.2) -> np.ndarray:
    """Low organ/furnace drone: a few harmonics with slowly drifting levels."""
    n = seconds(duration)
    out = np.zeros(n)
    for harmonic, amp in ((1, 1.0), (2, 0.5), (3, 0.25), (4, 0.18), (6, 0.08)):
        level = 1 - movement + movement * smooth_random(n, 0.15, rng)
        out += sine(freq * harmonic * (1 + rng.uniform(-0.001, 0.001)), n, rng.uniform(0, 6.28)) * amp * level
    return out / 2.0


# ---- output --------------------------------------------------------------------------------

def normalise_peak(x: np.ndarray, peak_db: float) -> np.ndarray:
    peak = np.max(np.abs(x)) + 1e-12
    return x * (10 ** (peak_db / 20) / peak)


def limit(x: np.ndarray, ceiling_db: float, release: float = 0.04, lookahead: float = 0.0015) -> np.ndarray:
    """A peak limiter: the gain drops at once (looking a little ahead) and recovers over
    `release`, so a transient is held under the ceiling without dulling the body behind it."""
    from scipy.ndimage import minimum_filter1d
    ceiling = 10 ** (ceiling_db / 20)
    need = np.minimum(1.0, ceiling / np.maximum(np.abs(x), 1e-9))
    need = minimum_filter1d(need, size=2 * seconds(lookahead) + 1)
    gain = np.empty_like(need)
    current, coefficient = 1.0, np.exp(-1.0 / (RATE * release))
    for index, target in enumerate(need):
        current = target if target < current else target + (current - target) * coefficient
        gain[index] = current
    return x * gain


def loud_and_limited(x: np.ndarray, lufs: float, peak_db: float) -> np.ndarray:
    """Loudness and limiter in turn: a transient take reaches its loudness instead of stopping
    at the peak ceiling (normalising alone left such takes several dB quieter)."""
    for _ in range(3):
        x = limit(x * 10 ** ((lufs - loudness(x)) / 20), peak_db)
    return x


def loudness(x: np.ndarray) -> float:
    import pyloudnorm as pyln
    data = x if x.ndim == 2 else x[:, None]
    meter = pyln.Meter(RATE, block_size=min(0.4, len(data) / RATE * 0.9))
    return meter.integrated_loudness(data)


def normalise_loudness(x: np.ndarray, lufs: float, peak_ceiling_db: float = -1.0) -> np.ndarray:
    gain = 10 ** ((lufs - loudness(x)) / 20)
    y = x * gain
    peak = np.max(np.abs(y))
    ceiling = 10 ** (peak_ceiling_db / 20)
    return y * (ceiling / peak) if peak > ceiling else y


def loop_seam_db(x: np.ndarray) -> float:
    """Level step (dB) between the last and first 20 ms of a loop: near 0 means seamless."""
    a = x[-seconds(0.02):]
    b = x[: seconds(0.02)]
    ra = np.sqrt(np.mean(a ** 2)) + 1e-9
    rb = np.sqrt(np.mean(b ** 2)) + 1e-9
    return 20 * math.log10(rb / ra)


def write(path: Path, x: np.ndarray) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(path), np.clip(x, -1, 1), RATE, subtype="PCM_16")
