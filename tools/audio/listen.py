#!/usr/bin/env python3
"""A machine 'ear' for review: rank text descriptions against a sound with LAION-CLAP.

    ~/.local/share/lsof-audio/.venv-ace/bin/python tools/audio/listen.py SOUND.wav \\
        --labels "furnace rumble in a stone hall" "white noise" "rain on a roof" ...

CLAP (laion/clap-htsat-unfused, Apache-2.0) embeds audio and text in one space; the ranking
shows whether a synthesized sound reads as what it should be and not as something else
(noise, rain, an engine). It complements spectrograms and loudness and does not replace
judging the sound in the game: it is a check, not a verdict.
"""
from __future__ import annotations

import argparse
import json
import sys

import numpy as np


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("sounds", nargs="+")
    parser.add_argument("--labels", nargs="+", required=True)
    parser.add_argument("--seconds", type=float, default=10.0, help="window per sound (from the start)")
    args = parser.parse_args(argv)

    import librosa
    import torch
    from transformers import ClapModel, ClapProcessor
    model = ClapModel.from_pretrained("laion/clap-htsat-unfused")
    processor = ClapProcessor.from_pretrained("laion/clap-htsat-unfused")
    for path in args.sounds:
        audio, _ = librosa.load(path, sr=48000, mono=True, duration=args.seconds)
        inputs = processor(text=args.labels, audios=[audio], sampling_rate=48000, return_tensors="pt", padding=True)
        with torch.no_grad():
            logits = model(**inputs).logits_per_audio[0]
        probs = torch.softmax(logits, dim=-1).numpy()
        ranked = sorted(zip(args.labels, probs), key=lambda item: -item[1])
        print("LISTEN " + json.dumps({"sound": path, "ranking": [[label, round(float(p), 3)] for label, p in ranked]}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
