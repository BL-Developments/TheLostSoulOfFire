#!/usr/bin/env python3
"""Install a selected authored take into Content/Audio and register it in Content.mgcb.

    tools/audio/.venv/bin/python tools/audio/install.py CANDIDATE.wav Audio/Sfx/ui_move.wav
    tools/audio/.venv/bin/python tools/audio/install.py CANDIDATE.wav Audio/Music/title_theme.ogg

WAV targets are copied as 16-bit PCM (effects mono, ambience stereo, 48 kHz); OGG targets are
encoded with oggenc at quality 5 (48 kHz stereo), like the approved arena loop. The mgcb block
matches what tools/audio/validate_audio.py expects. Prints the ledger line for SOURCES.md
(recipe, seed, LUFS, peak) from the candidate's JSON report next to it.
"""
from __future__ import annotations

import json
import shutil
import subprocess
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[2]
CONTENT = ROOT / "src" / "TheLostSoulOfFire" / "Content"
MGCB = CONTENT / "Content.mgcb"


def block(relative: str) -> str:
    music = relative.endswith(".ogg")
    importer = "OggImporter" if music else "WavImporter"
    processor = "SongProcessor" if music else "SoundEffectProcessor"
    return f"#begin {relative}\n/importer:{importer}\n/processor:{processor}\n/build:{relative}\n"


def main(argv: list[str]) -> int:
    source, relative = Path(argv[0]), argv[1]
    target = CONTENT / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    data, rate = sf.read(str(source), always_2d=True)
    if rate != 48000:
        raise SystemExit(f"{source}: {rate} Hz, erwartet 48000")
    if relative.startswith("Audio/Sfx/") and data.shape[1] != 1:
        data = data.mean(axis=1, keepdims=True)
    if relative.startswith(("Audio/Ambience/", "Audio/Music/")):
        # Loops: an 8 ms fade at both edges keeps the seam free of a sample jump (a downbeat
        # that starts on sample 0 would otherwise click once per loop).
        edge = int(0.008 * rate)
        ramp = np.linspace(0.0, 1.0, edge)[:, None]
        data = data.copy()
        data[:edge] *= ramp
        data[-edge:] *= ramp[::-1]
    if relative.endswith(".ogg"):
        if data.shape[1] == 1:
            data = np.repeat(data, 2, axis=1)
        temporary = target.with_suffix(".tmp.wav")
        sf.write(str(temporary), data, rate, subtype="PCM_16")
        subprocess.run(["oggenc", "-Q", "-q", "5", "-o", str(target), str(temporary)], check=True)
        temporary.unlink()
    else:
        sf.write(str(target), data, rate, subtype="PCM_16")
    text = MGCB.read_text(encoding="utf-8")
    if f"#begin {relative}\n" not in text:
        text = text.rstrip("\n") + "\n\n" + block(relative)
        MGCB.write_text(text, encoding="utf-8")
    report = source.with_suffix(".json")
    info = json.loads(report.read_text()) if report.exists() else {}
    peak = 20 * np.log10(np.max(np.abs(data)) + 1e-12)
    print(f"| `{relative}` | {info.get('recipe', '?')} Seed {info.get('seed', '?')} | {len(data) / rate:.2f} s, "
          f"{data.shape[1]} Kanal/Kanäle, {info.get('lufs', '?')} LUFS, Spitze {peak:.1f} dBFS |")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
