#!/usr/bin/env python3
"""Validate committed audio formats, levels, durations, loops, and manifests."""

from __future__ import annotations

import argparse
import math
import shutil
import subprocess
import tempfile
import wave
from array import array
from pathlib import Path


SFX_DURATIONS = {
    "scythe_swing_1.wav": 0.18,
    "scythe_swing_2.wav": 0.24,
    "soul_cleave.wav": 0.45,
    "scythe_hit.wav": 0.15,
    "dash.wav": 0.25,
    "cannon_charge.wav": 0.8,
    "cannon_full.wav": 0.22,
    "cannon_fire.wav": 0.55,
    "burning_charge.wav": 0.55,
    "burning_detonation.wav": 0.7,
    "core_hit.wav": 0.18,
    "soul_release.wav": 0.9,
    "resonance_ready.wav": 0.35,
    "resonance_activate.wav": 0.8,
    "player_hit.wav": 0.2,
    "player_death.wav": 1.2,
    "soul_sense_on.wav": 0.45,
    "soul_sense_off.wav": 0.3,
    "wave_start.wav": 0.65,
    "hollow_swipe.wav": 0.32,
    "devourer_slam.wav": 0.65,
    "devourer_devour.wav": 0.85,
    "enemy_death.wav": 0.55,
    "cannon_impact.wav": 0.32,
    "title_confirm.wav": 0.32,
    "wave_clear.wav": 0.75,
    "ending_reveal.wav": 1.6,
}

GAMEPLAY_CUES = {
    "ScytheSwing1", "ScytheSwing2", "SoulCleave", "ScytheHit", "Dash",
    "CannonCharge", "CannonFull", "CannonFire", "BurningCharge",
    "BurningDetonation", "CoreHit", "SoulRelease", "ResonanceReady",
    "ResonanceActivate", "PlayerHit", "PlayerDeath", "SoulSenseOn",
    "SoulSenseOff", "WaveStart", "HollowSwipe", "DevourerSlam",
    "DevourerDevour", "EnemyDeath", "CannonImpact", "TitleConfirm",
    "WaveClear", "EndingReveal",
}


def db(value: float) -> float:
    return 20.0 * math.log10(max(value, 1e-12))


def inspect_wav(path: Path) -> dict[str, float | int]:
    with wave.open(str(path), "rb") as source:
        channels = source.getnchannels()
        width = source.getsampwidth()
        rate = source.getframerate()
        frames = source.getnframes()
        raw = source.readframes(frames)
    samples = array("h")
    samples.frombytes(raw)
    peak = max((abs(sample) for sample in samples), default=0) / 32768.0
    rms = math.sqrt(sum(sample * sample for sample in samples) / max(1, len(samples))) / 32768.0
    first = [samples[channel] / 32768.0 for channel in range(channels)]
    last = [samples[len(samples) - channels + channel] / 32768.0 for channel in range(channels)]
    seam = max(abs(a - b) for a, b in zip(first, last))
    threshold = int(32768.0 * 10.0 ** (-60.0 / 20.0))
    leading_frame = 0
    for index in range(0, len(samples), channels):
        if any(abs(samples[index + channel]) > threshold for channel in range(channels)):
            leading_frame = index // channels
            break
    return {
        "channels": channels,
        "width": width,
        "rate": rate,
        "frames": frames,
        "duration": frames / rate,
        "peak_db": db(peak),
        "rms_db": db(rms),
        "seam_db": db(seam),
        "leading_ms": leading_frame * 1000.0 / rate,
    }


def inspect_ogg(path: Path) -> dict[str, float | int]:
    ogginfo = shutil.which("ogginfo")
    oggdec = shutil.which("oggdec")
    if ogginfo is None or oggdec is None:
        raise RuntimeError("ogginfo and oggdec (vorbis-tools) are required for full music validation")
    info = subprocess.run([ogginfo, str(path)], check=True, capture_output=True, text=True).stdout
    if "Vorbis stream" not in info or "Channels: 2" not in info or "Rate: 48000" not in info:
        raise ValueError("music is not 48 kHz stereo Vorbis")
    with tempfile.TemporaryDirectory(prefix="soulfire-audio-") as temporary:
        decoded = Path(temporary) / "music.wav"
        subprocess.run([oggdec, "--quiet", "-o", str(decoded), str(path)], check=True)
        return inspect_wav(decoded)


def expected_manifest_block(relative_path: str, processor: str) -> str:
    importer = "WavImporter" if relative_path.endswith(".wav") else "OggImporter"
    return "\n".join(
        (
            f"#begin {relative_path}",
            f"/importer:{importer}",
            f"/processor:{processor}",
            f"/build:{relative_path}",
        )
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--content-root",
        type=Path,
        default=Path(__file__).resolve().parents[2] / "src" / "TheLostSoulOfFire" / "Content",
    )
    args = parser.parse_args()
    failures: list[str] = []

    sfx_root = args.content_root / "Audio" / "Sfx"
    expected_paths: list[tuple[Path, str, float | None]] = [
        (sfx_root / filename, "SoundEffectProcessor", duration)
        for filename, duration in SFX_DURATIONS.items()
    ]
    expected_paths.extend(
        (
            (args.content_root / "Audio" / "Ambience" / "arena_ambience.wav", "SoundEffectProcessor", None),
            (args.content_root / "Audio" / "Music" / "arena_loop.ogg", "SongProcessor", None),
        )
    )

    manifest_path = args.content_root / "Content.mgcb"
    sources_path = args.content_root / "Audio" / "SOURCES.md"
    game_world_path = args.content_root.parent / "Game" / "GameWorld.cs"
    audio_director_path = args.content_root.parent / "Audio" / "AudioDirector.cs"
    manifest = manifest_path.read_text(encoding="utf-8-sig") if manifest_path.exists() else ""
    sources = sources_path.read_text(encoding="utf-8") if sources_path.exists() else ""
    game_world = game_world_path.read_text(encoding="utf-8") if game_world_path.exists() else ""
    audio_director = audio_director_path.read_text(encoding="utf-8") if audio_director_path.exists() else ""

    print("asset                              ch  rate  duration   peak     RMS    seam   lead")
    print("---------------------------------  --  -----  --------  -------  -------  ------  -----")
    for path, processor, expected_duration in expected_paths:
        if not path.exists():
            failures.append(f"missing asset: {path.relative_to(args.content_root)}")
            continue
        try:
            metrics = inspect_ogg(path) if path.suffix == ".ogg" else inspect_wav(path)
        except Exception as exc:  # Keep a complete failure report.
            failures.append(f"cannot inspect {path.name}: {exc}")
            continue

        relative = path.relative_to(args.content_root).as_posix()
        print(
            f"{relative:33}  {metrics['channels']:2}  {metrics['rate']:5}  "
            f"{metrics['duration']:8.3f}  {metrics['peak_db']:7.2f}  "
            f"{metrics['rms_db']:7.2f}  {metrics['seam_db']:6.1f}  {metrics['leading_ms']:5.1f}"
        )
        if metrics["rate"] != 48_000:
            failures.append(f"{relative}: expected 48 kHz")
        if metrics["width"] != 2:
            failures.append(f"{relative}: expected 16-bit PCM after decode")
        if path.parent == sfx_root and metrics["channels"] != 1:
            failures.append(f"{relative}: effects must be mono")
        if path.name in {"arena_ambience.wav", "arena_loop.ogg"} and metrics["channels"] != 2:
            failures.append(f"{relative}: loop must be stereo")
        if metrics["peak_db"] >= -1.0:
            failures.append(f"{relative}: peak {metrics['peak_db']:.2f} dBFS is not below -1 dBFS")
        if metrics["rms_db"] <= -70.0:
            failures.append(f"{relative}: file is effectively silent at {metrics['rms_db']:.2f} dBFS RMS")
        if expected_duration is not None and abs(metrics["duration"] - expected_duration) > 0.012:
            failures.append(f"{relative}: duration differs from {expected_duration:.3f}s")
        if path.name == "arena_ambience.wav" and not 20.0 <= metrics["duration"] <= 30.0:
            failures.append(f"{relative}: ambience duration must be 20–30s")
        if path.name == "arena_loop.ogg" and not 90.0 <= metrics["duration"] <= 150.0:
            failures.append(f"{relative}: music duration must be 90–150s")
        if path.name in {"arena_ambience.wav", "arena_loop.ogg"} and metrics["seam_db"] > -45.0:
            failures.append(f"{relative}: endpoint discontinuity is {metrics['seam_db']:.1f} dBFS")

        block = expected_manifest_block(relative, processor)
        if block not in manifest:
            failures.append(f"Content.mgcb missing correct {processor} block for {relative}")
        if manifest.count(f"#begin {relative}") != 1:
            failures.append(f"Content.mgcb must register {relative} exactly once")
        if path.name not in sources:
            failures.append(f"SOURCES.md missing {path.name}")

    if not sources_path.exists():
        failures.append("missing Audio/SOURCES.md")
    if "Ludo.ai" not in sources or "Ludo audio model (undisclosed)" not in sources:
        failures.append("SOURCES.md is missing the approved Ludo service/model record")
    for cue in sorted(GAMEPLAY_CUES):
        if f"AudioCue.{cue}" not in game_world:
            failures.append(f"gameplay has no event wiring for AudioCue.{cue}")

    expected_sfx = set(SFX_DURATIONS)
    actual_sfx = {path.name for path in sfx_root.glob("*.wav")}
    for filename in sorted(actual_sfx - expected_sfx):
        failures.append(f"unexpected shipped SFX candidate: Audio/Sfx/{filename}")
    if "MediaPlayer" not in audio_director or "Song" not in audio_director:
        failures.append("AudioDirector is missing arena music playback")

    if failures:
        print("\nFAIL")
        for failure in failures:
            print(f"- {failure}")
        return 1
    print(f"\nPASS: {len(expected_paths)} assets satisfy format, level, duration, loop, source, and manifest checks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
