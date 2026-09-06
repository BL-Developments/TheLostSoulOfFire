#!/usr/bin/env python3
"""Produce the Session 2 cues for The Lost Soul of Fire.

Generated audio is treated strictly as *source material*, never as a finished
cue. The first attempt shipped raw ElevenLabs output and it was rejected on
listening: raw text-to-audio has three obvious tells, and all three are fixed
here rather than hoped away.

    broadband hiss      -> gated and low-passed to the bank's measured darkness
    no defined onset    -> onset-detected, trimmed and transient-shaped
    tails that stop     -> explicit exponential decay and fades

Measured against the shipped bank (tools/audio/validate_audio.py), the existing
27 cues sit at peak -2..-5 dBFS, RMS -11..-17 dBFS, 0.18..0.90 s, and a
zero-crossing rate of 0.004..0.035 for everything except the deliberately bright
core_hit. Anything brighter or longer than that band sounds foreign in this game,
which is precisely what happened.

So each new cue is built as:

    body    an already-approved sample from Content/Audio/Sfx, which carries the
            character and guarantees the cue belongs to this game;
    texture a heavily filtered, gated slice of a generation, mixed well under the
            body, which adds detail the bank does not contain;
    tail    an explicit decay.

Run with --bank-only to build the same cues with no generated content at all.
Both variants are written to the audition folder so they can be compared before
anything is promoted into Content.

Stdlib only, matching tools/audio/master_ludo_audio.py.
"""

from __future__ import annotations

import argparse
import math
import wave
from array import array
from pathlib import Path

RATE = 48_000
SOURCE_RATE = 44_100

REPO = Path(__file__).resolve().parents[2]
BANK = REPO / "src" / "TheLostSoulOfFire" / "Content" / "Audio" / "Sfx"
CANDIDATES = REPO / "art" / "audio_candidates"
AUDITION = REPO / "artifacts" / "session2" / "audio-audition"


# --------------------------------------------------------------------------- io


def read_wav(path: Path) -> tuple[list[float], int]:
    with wave.open(str(path), "rb") as source:
        channels = source.getnchannels()
        rate = source.getframerate()
        samples = array("h")
        samples.frombytes(source.readframes(source.getnframes()))
    mono = [samples[index] / 32768.0 for index in range(0, len(samples), channels)]
    return mono, rate


def write_wav(path: Path, signal: list[float]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    frames = array("h", (int(round(max(-1.0, min(1.0, value)) * 32767.0)) for value in signal))
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(frames.tobytes())


def resample(signal: list[float], source_rate: int, target_rate: int = RATE) -> list[float]:
    if source_rate == target_rate or not signal:
        return list(signal)
    count = int(len(signal) * target_rate / source_rate)
    output: list[float] = []
    for index in range(count):
        position = index * (len(signal) - 1) / max(1, count - 1)
        left = int(position)
        fraction = position - left
        right = min(left + 1, len(signal) - 1)
        output.append(signal[left] + (signal[right] - signal[left]) * fraction)
    return output


# ------------------------------------------------------------------------- dsp


def low_pass(signal: list[float], cutoff: float, poles: int = 2) -> list[float]:
    alpha = 1.0 - math.exp(-math.tau * cutoff / RATE)
    output = list(signal)
    for _ in range(poles):
        value = 0.0
        for index, sample in enumerate(output):
            value += alpha * (sample - value)
            output[index] = value
    return output


def high_pass(signal: list[float], cutoff: float) -> list[float]:
    low = low_pass(signal, cutoff, poles=1)
    return [sample - low[index] for index, sample in enumerate(signal)]


def dc_block(signal: list[float]) -> list[float]:
    if not signal:
        return signal
    mean = sum(signal) / len(signal)
    return [sample - mean for sample in signal]


def envelope(signal: list[float], attack_ms: float = 2.0, release_ms: float = 40.0) -> list[float]:
    attack = 1.0 - math.exp(-1.0 / max(1.0, attack_ms * 0.001 * RATE))
    release = 1.0 - math.exp(-1.0 / max(1.0, release_ms * 0.001 * RATE))
    value = 0.0
    output: list[float] = []
    for sample in signal:
        level = abs(sample)
        coefficient = attack if level > value else release
        value += coefficient * (level - value)
        output.append(value)
    return output


def noise_gate(signal: list[float], threshold_db: float = -42.0, ratio: float = 6.0) -> list[float]:
    """Downward expander. This is the single biggest fix for the "raw" quality:
    generated audio carries a constant hiss bed that the bank does not have."""
    threshold = 10.0 ** (threshold_db / 20.0)
    level = envelope(signal, attack_ms=1.5, release_ms=60.0)
    output: list[float] = []
    for index, sample in enumerate(signal):
        current = max(level[index], 1e-9)
        if current >= threshold:
            output.append(sample)
            continue
        # Expand downward instead of hard gating, so the floor falls away
        # smoothly rather than chattering.
        gain = (current / threshold) ** (ratio - 1.0)
        output.append(sample * gain)
    return output


def find_onset(signal: list[float], threshold_ratio: float = 0.16, backoff_ms: float = 6.0) -> int:
    if not signal:
        return 0
    level = envelope(signal, attack_ms=0.6, release_ms=25.0)
    peak = max(level) if level else 0.0
    if peak <= 1e-9:
        return 0
    target = peak * threshold_ratio
    for index, value in enumerate(level):
        if value >= target:
            return max(0, index - int(backoff_ms * 0.001 * RATE))
    return 0


def segment(signal: list[float], start: float, duration: float) -> list[float]:
    first = max(0, int(round(start * RATE)))
    count = int(round(duration * RATE))
    window = signal[first:first + count]
    return window + [0.0] * (count - len(window))


def transient_shape(signal: list[float], attack_ms: float, punch: float = 1.0) -> list[float]:
    """Sharpens the leading edge so an impact reads as an impact."""
    attack = max(1, int(attack_ms * 0.001 * RATE))
    output = list(signal)
    for index in range(min(attack, len(output))):
        amount = index / attack
        output[index] *= amount * amount
    for index in range(min(attack * 3, len(output))):
        boost = 1.0 + punch * (1.0 - index / max(1, attack * 3))
        output[index] *= boost
    return output


def decay(signal: list[float], start: float, curve: float = 3.2) -> list[float]:
    """Explicit exponential tail. Generated audio frequently just stops."""
    first = int(start * len(signal))
    output = list(signal)
    span = max(1, len(output) - first)
    for index in range(first, len(output)):
        amount = (index - first) / span
        output[index] *= math.exp(-curve * amount)
    return output


def reverse(signal: list[float]) -> list[float]:
    return list(reversed(signal))


def mix(duration: float, *layers: tuple[list[float], float, float]) -> list[float]:
    output = [0.0] * int(round(duration * RATE))
    for signal, gain, offset in layers:
        start = int(round(offset * RATE))
        for index, sample in enumerate(signal):
            destination = start + index
            if 0 <= destination < len(output):
                output[destination] += sample * gain
    return output


def master(signal: list[float], duration: float, peak_db: float) -> list[float]:
    count = int(round(duration * RATE))
    output = (list(signal) + [0.0] * count)[:count]
    if not output:
        return output

    output = dc_block(output)
    fade = min(int(0.006 * RATE), max(1, len(output) // 8))
    for index in range(fade):
        gain = index / fade
        output[index] *= gain
        output[-1 - index] *= gain

    peak = max((abs(sample) for sample in output), default=1.0)
    target = 10.0 ** (peak_db / 20.0)
    scale = target / max(peak, 1e-9)
    return [max(-1.0, min(1.0, sample * scale)) for sample in output]


def db(value: float) -> float:
    return 20.0 * math.log10(max(value, 1e-9))


def measure(signal: list[float]) -> dict[str, float]:
    if not signal:
        return {"seconds": 0.0, "peak": -99.0, "rms": -99.0, "tail": -99.0, "zcr": 0.0}
    peak = max(abs(sample) for sample in signal)
    rms = math.sqrt(sum(sample * sample for sample in signal) / len(signal))
    tail = signal[int(len(signal) * 0.8):] or [0.0]
    tail_rms = math.sqrt(sum(sample * sample for sample in tail) / len(tail))
    crossings = sum(
        1 for index in range(1, len(signal))
        if (signal[index] >= 0.0) != (signal[index - 1] >= 0.0)
    )
    return {
        "seconds": len(signal) / RATE,
        "peak": db(peak),
        "rms": db(rms),
        "tail": db(tail_rms),
        "zcr": crossings / len(signal),
    }


# ------------------------------------------------------------------- sourcing


class Sources:
    """Approved bank samples plus, optionally, prepared generated textures."""

    def __init__(self, use_generated: bool) -> None:
        self._bank: dict[str, list[float]] = {}
        self._textures: dict[str, list[float]] = {}
        self.use_generated = use_generated
        self.generated_used: list[str] = []
        self.choices: dict[str, int] = {}

    def bank(self, name: str) -> list[float]:
        if name not in self._bank:
            signal, rate = read_wav(BANK / f"{name}.wav")
            self._bank[name] = resample(signal, rate)
        return list(self._bank[name])

    def choose(self, cue: str, component: str, shape: str) -> int:
        """Pick a candidate by measurement, because these were not auditioned.

        This cannot replace listening and is not claimed to. It only rejects the
        candidates that are objectively unusable — a hiss bed that survives
        gating, a missing transient where the cue needs one, or a swell that is
        actually an impact — and reports the choice so it can be overruled.
        """
        best_index = 1
        best_score = -1e9
        for index in range(1, 4):
            path = CANDIDATES / cue / f"{component}_{index:02d}.wav"
            if not path.exists():
                continue

            raw, rate = read_wav(path)
            prepared = high_pass(dc_block(resample(raw, rate)), 55.0)
            prepared = noise_gate(prepared, -40.0)
            onset = find_onset(prepared)
            window = segment(prepared[onset:], 0.0, 0.9)
            stats = measure(window)

            # A quiet floor after gating is the strongest signal that a
            # generation will not sound like raw model output in the mix.
            score = -stats["tail"] * 0.5
            crest = stats["peak"] - stats["rms"]
            if shape == "impact":
                score += crest * 2.0
            elif shape == "swell":
                score -= abs(crest - 9.0) * 2.0
            # Brightness is punished: the bank is dark and anything bright reads
            # as foreign no matter how good it is on its own.
            score -= max(0.0, stats["zcr"] - 0.06) * 300.0

            if score > best_score:
                best_score = score
                best_index = index

        self.choices[f"{cue}/{component}"] = best_index
        return best_index

    def texture(
        self,
        cue: str,
        component: str,
        candidate: int,
        duration: float,
        cutoff: float,
        gate_db: float = -40.0,
    ) -> list[float]:
        """One generated component, prepared to sit under a bank body.

        Onset-trimmed, high-passed to remove model rumble, expanded to kill the
        hiss bed, then low-passed hard so its brightness lands inside the band the
        rest of the game occupies.
        """
        if not self.use_generated:
            return [0.0] * int(round(duration * RATE))

        path = CANDIDATES / cue / f"{component}_{candidate:02d}.wav"
        if not path.exists():
            return [0.0] * int(round(duration * RATE))

        key = str(path)
        if key not in self._textures:
            raw, rate = read_wav(path)
            prepared = resample(raw, rate)
            prepared = dc_block(prepared)
            prepared = high_pass(prepared, 55.0)
            prepared = noise_gate(prepared, gate_db)
            onset = find_onset(prepared)
            self._textures[key] = prepared[onset:]
            self.generated_used.append(f"{cue}/{component}_{candidate:02d}")

        window = segment(self._textures[key], 0.0, duration)
        return low_pass(window, cutoff, poles=2)


# ---------------------------------------------------------------------- cues


def build(sources: Sources) -> dict[str, tuple[list[float], float, float]]:
    """Each cue: body from the approved bank, texture from a generation, tail shaped.

    Generated layers are mixed 12-20 dB under the body on purpose. The character
    has to come from material this game already sounds like.
    """
    cues: dict[str, tuple[list[float], float, float]] = {}

    # --- Severance window: pressure drawing tight, nothing lands --------------
    # Body is the approved cannon charge played backwards, which is already the
    # game's "supernatural tension" texture, low-passed into an inward pull.
    pull = low_pass(reverse(segment(sources.bank("cannon_charge"), 0.10, 0.42)), 900.0, poles=2)
    strain = sources.texture("severance_window", "tension", sources.choose("severance_window", "tension", "swell"), 0.42, 3200.0)
    strain = transient_shape(strain, 90.0, punch=0.0)
    air = sources.texture("severance_window", "air", sources.choose("severance_window", "air", "swell"), 0.42, 1600.0)
    cues["severance_window"] = (
        decay(mix(0.42, (pull, 0.9, 0.0), (strain, 0.22, 0.02), (air, 0.16, 0.0)), 0.62, 4.0),
        0.42,
        -6.0,
    )

    # --- Severance cut: separation, then release ----------------------------
    # Body is core_hit, the approved crystalline fracture, so the payoff is
    # recognisably the same world as a weak-point hit rather than a new sound.
    fracture = transient_shape(segment(sources.bank("core_hit"), 0.0, 0.18), 1.5, punch=0.55)
    blade = sources.texture("severance_cut", "cut", sources.choose("severance_cut", "cut", "impact"), 0.2, 5200.0)
    blade = transient_shape(blade, 1.2, punch=0.7)
    letting_go = low_pass(segment(sources.bank("soul_release"), 0.12, 0.5), 2600.0, poles=2)
    shimmer = sources.texture("severance_cut", "release", sources.choose("severance_cut", "release", "swell"), 0.5, 3400.0)
    cues["severance_cut"] = (
        decay(
            mix(
                0.62,
                (fracture, 1.0, 0.0),
                (blade, 0.28, 0.0),
                (letting_go, 0.5, 0.1),
                (shimmer, 0.18, 0.12),
            ),
            0.5,
            3.6,
        ),
        0.62,
        -2.6,
    )

    # --- Soul exposed: fragile, human, quiet --------------------------------
    surface = low_pass(segment(sources.bank("soul_release"), 0.0, 0.36), 3200.0, poles=2)
    glass = sources.texture("soul_exposed", "surface", sources.choose("soul_exposed", "surface", "impact"), 0.36, 4200.0)
    cues["soul_exposed"] = (
        decay(mix(0.4, (surface, 0.85, 0.0), (glass, 0.2, 0.01)), 0.45, 3.8),
        0.4,
        -7.0,
    )

    # --- Warden down: a flame guttering, not going out ----------------------
    # Body is the approved player_death collapse, shortened and low-passed so it
    # reads as losing pressure rather than as an ending.
    collapse = low_pass(segment(sources.bank("player_death"), 0.05, 0.8), 700.0, poles=2)
    gutter = sources.texture("warden_down", "gutter", sources.choose("warden_down", "gutter", "texture"), 0.8, 1800.0)
    cues["warden_down"] = (
        decay(mix(0.85, (collapse, 0.9, 0.0), (gutter, 0.3, 0.05)), 0.4, 2.4),
        0.85,
        -4.0,
    )

    # --- Warden stabilise: relit, and the two flames agreeing ---------------
    ignite = sources.texture("warden_stabilize", "ignite", sources.choose("warden_stabilize", "ignite", "swell"), 0.9, 2200.0)
    ignite = transient_shape(ignite, 60.0, punch=0.0)
    lock = sources.texture("warden_stabilize", "lock", sources.choose("warden_stabilize", "lock", "swell"), 0.9, 2800.0)
    warmth = low_pass(segment(sources.bank("ending_reveal"), 0.0, 0.9), 2400.0, poles=2)
    cues["warden_stabilize"] = (
        decay(mix(1.0, (warmth, 0.8, 0.05), (ignite, 0.34, 0.0), (lock, 0.22, 0.16)), 0.55, 2.6),
        1.0,
        -3.4,
    )

    # --- Resonance ready: two pulses landing together -----------------------
    # The existing cue is a single low-passed slam. Doubling it with a measured
    # offset is the whole idea: synchronisation, not a louder thump.
    pulse = low_pass(segment(sources.bank("resonance_ready"), 0.0, 0.3), 420.0, poles=2)
    sync = sources.texture("resonance_ready", "sync", sources.choose("resonance_ready", "sync", "impact"), 0.4, 900.0)
    cues["resonance_ready"] = (
        decay(mix(0.44, (pulse, 0.92, 0.0), (pulse, 0.5, 0.085), (sync, 0.18, 0.02)), 0.5, 3.0),
        0.44,
        -4.0,
    )

    return cues


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--bank-only", action="store_true", help="build with no generated content")
    parser.add_argument("--promote", action="store_true", help="write into Content/Audio/Sfx")
    arguments = parser.parse_args()

    sources = Sources(use_generated=not arguments.bank_only)
    cues = build(sources)
    variant = "bank-only" if arguments.bank_only else "hybrid"

    print(f"{'cue':22s} {'sec':>5} {'peak':>7} {'rms':>7} {'tail':>7} {'zcr':>6}  variant={variant}")
    for name, (signal, duration, peak_db) in cues.items():
        rendered = master(signal, duration, peak_db)
        stats = measure(rendered)
        print(
            f"{name:22s} {stats['seconds']:5.2f} {stats['peak']:7.2f} "
            f"{stats['rms']:7.2f} {stats['tail']:7.2f} {stats['zcr']:6.3f}"
        )
        write_wav(AUDITION / variant / f"{name}.wav", rendered)
        if arguments.promote and not arguments.bank_only:
            write_wav(BANK / f"{name}.wav", rendered)

    if sources.choices:
        print("candidate selection (measured, not auditioned):")
        for key in sorted(sources.choices):
            print(f"  {key} -> {sources.choices[key]:02d}")

    if sources.generated_used:
        print("generated components used:")
        for entry in sorted(set(sources.generated_used)):
            print(f"  {entry}")

    print(f"audition written to {AUDITION / variant}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
