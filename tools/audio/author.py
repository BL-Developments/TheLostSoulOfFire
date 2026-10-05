#!/usr/bin/env python3
"""Author the game's synthesized sounds from recipes: ambiences, music beds and cues.

    tools/audio/.venv/bin/python tools/audio/author.py <recipe> [--seed N] [--out PATH]
    tools/audio/.venv/bin/python tools/audio/author.py --list

Recipes live in tools/audio/recipes/*.py, one function per sound, registered with @recipe.
Output goes to art/production/candidates/audio/<recipe>/<seed>.wav (ignored by git) with a
spectrogram PNG and a JSON report (duration, LUFS, peak, loop seam) for review; the selected
take is copied into Content/Audio and recorded in Content/Audio/SOURCES.md.
"""
from __future__ import annotations

import argparse
import importlib
import json
import pkgutil
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import dsp  # noqa: E402
import recipes  # noqa: E402
from recipes import REGISTRY  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]


def spectrogram(path: Path, x: np.ndarray, title: str) -> None:
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    mono = x.mean(axis=1) if x.ndim == 2 else x
    fig, axes = plt.subplots(2, 1, figsize=(10, 5), gridspec_kw={"height_ratios": [1, 2]})
    t = np.arange(len(mono)) / dsp.RATE
    axes[0].plot(t, mono, linewidth=0.3)
    axes[0].set_xlim(0, t[-1])
    axes[0].set_title(title)
    axes[1].specgram(mono, NFFT=2048, Fs=dsp.RATE, noverlap=1536, cmap="magma", vmin=-140)
    axes[1].set_ylim(0, 12000)
    fig.tight_layout()
    fig.savefig(path, dpi=80)
    plt.close(fig)


def main(argv: list[str]) -> int:
    for module in pkgutil.iter_modules(recipes.__path__):
        importlib.import_module(f"recipes.{module.name}")
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("recipe", nargs="?")
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--out", type=Path)
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args(argv)
    if args.list or not args.recipe:
        for name, entry in sorted(REGISTRY.items()):
            print(f"{name:28s} {entry['description']}")
        return 0
    entry = REGISTRY[args.recipe]
    rng = np.random.default_rng(args.seed)
    x = entry["build"](rng)
    out = args.out or ROOT / "art" / "production" / "candidates" / "audio" / args.recipe / f"{args.seed}.wav"
    dsp.write(out, x)
    report = {
        "recipe": args.recipe,
        "seed": args.seed,
        "description": entry["description"],
        "seconds": round(len(x) / dsp.RATE, 3),
        "channels": 1 if x.ndim == 1 else x.shape[1],
        "lufs": round(dsp.loudness(x), 1),
        "peak_db": round(20 * np.log10(np.max(np.abs(x)) + 1e-12), 1),
        "loop_seam_db": round(dsp.loop_seam_db(x), 2) if entry.get("loop") else None,
    }
    out.with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n")
    spectrogram(out.with_suffix(".png"), x, f"{args.recipe} seed {args.seed}")
    print("AUTHOR_DONE " + json.dumps(report))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
