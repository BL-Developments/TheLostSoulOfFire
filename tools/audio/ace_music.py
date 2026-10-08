#!/usr/bin/env python3
"""Generate instrumental music candidates locally with ACE-Step v1 (Apache-2.0).

    ~/.local/share/lsof-audio/.venv-ace/bin/python tools/audio/ace_music.py \\
        --out art/production/candidates/audio/music/<cue> --prompt "..." \\
        [--duration 90] [--seeds 11 12 13] [--steps 60] [--guidance 15]

The model runs on the Mac (MPS, float16), optionally with CPU offload; no network service is called
after the checkpoint is cached. Each candidate is written as <seed>.wav next to a JSON
file with the exact prompt, seed and settings, so the selected take can be recorded in
Content/Audio/SOURCES.md. Lyrics are always "[instrumental]".
"""
from __future__ import annotations

import argparse
import json
import sys
import time
from pathlib import Path


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--prompt", required=True)
    parser.add_argument("--duration", type=float, default=90.0)
    parser.add_argument("--seeds", type=int, nargs="+", default=[11])
    parser.add_argument("--steps", type=int, default=60)
    parser.add_argument("--guidance", type=float, default=15.0)
    parser.add_argument("--checkpoint", default="", help="checkpoint directory (default: Hugging Face cache)")
    parser.add_argument("--offload", action="store_true", help="CPU offload between stages (slower; for low memory)")
    args = parser.parse_args(argv)

    from acestep.pipeline_ace_step import ACEStepPipeline

    args.out.mkdir(parents=True, exist_ok=True)
    pipeline = ACEStepPipeline(checkpoint_dir=args.checkpoint, dtype="bfloat16", cpu_offload=args.offload, overlapped_decode=True)
    for seed in args.seeds:
        started = time.monotonic()
        target = args.out / f"{seed}.wav"
        pipeline(
            format="wav",
            audio_duration=args.duration,
            prompt=args.prompt,
            lyrics="[instrumental]",
            infer_step=args.steps,
            guidance_scale=args.guidance,
            scheduler_type="euler",
            cfg_type="apg",
            omega_scale=10.0,
            manual_seeds=[seed],
            guidance_interval=0.5,
            guidance_interval_decay=0.0,
            min_guidance_scale=3.0,
            use_erg_tag=True,
            use_erg_lyric=False,
            use_erg_diffusion=True,
            oss_steps=[],
            save_path=str(target),
        )
        record = {
            "model": "ACE-Step/ACE-Step-v1-3.5B (Apache-2.0)",
            "prompt": args.prompt,
            "lyrics": "[instrumental]",
            "seed": seed,
            "duration": args.duration,
            "steps": args.steps,
            "guidance": args.guidance,
            "seconds": round(time.monotonic() - started, 1),
        }
        (args.out / f"{seed}.json").write_text(json.dumps(record, indent=2, ensure_ascii=False) + "\n")
        print("ACE_MUSIC_DONE " + json.dumps(record, ensure_ascii=False), flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
