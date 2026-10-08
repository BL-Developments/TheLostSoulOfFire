#!/usr/bin/env python3
"""Generate an image locally with mflux (FLUX.2 [klein] 4B or Z-Image Turbo) and record it.

    tools/visuals/.venv/bin/python tools/visuals/generate.py \\
        --visual-id environment.shore --model flux2-klein-4b \\
        --prompt "painted dark harbour station at night, soft edges, key light from the upper left" \\
        --seed 42 [--steps 4] [--width 1024 --height 576] [--quantize 8] \\
        [--lora art/production/lora/hausstil.safetensors 0.9] [--reference img.png ...] \\
        [--init layout.png --init-strength 0.55]

Only models from art/production/LICENSES.md are accepted. Output goes to
art/production/candidates/<visual-id>/ (ignored by git) unless --output is given; every run
appends one entry to art/production/manifest.json. Prompts describe properties, never a game.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

#: mflux command and built-in model name per licensed model key.
COMMANDS = {
    "flux2-klein-4b": ("mflux-generate-flux2", "flux2-klein-4b", "mflux-generate-flux2-edit"),
    "z-image-turbo": ("mflux-generate-z-image-turbo", "z-image-turbo", None),
}
DEFAULT_STEPS = {"flux2-klein-4b": 4, "z-image-turbo": 9}


def mflux_version() -> str:
    path = shutil.which("mflux-generate")
    if path is None:
        raise SystemExit("mflux fehlt; siehe tools/visuals/README.md")
    python = Path(path).resolve().parent / "python"
    result = subprocess.run([str(python), "-c", "import importlib.metadata as m; print(m.version('mflux'))"],
                            capture_output=True, text=True, check=True)
    return result.stdout.strip()


def build_command(args: argparse.Namespace, output: Path) -> list[str]:
    command, model, edit_command = COMMANDS[args.model]
    if args.reference:
        if edit_command is None:
            raise SystemExit(f"Referenzbilder werden für {args.model} nicht unterstützt.")
        command = edit_command
    parts = [command, "--model", model, "--prompt", args.prompt, "--seed", str(args.seed),
             "--steps", str(args.steps or DEFAULT_STEPS[args.model]),
             "--width", str(args.width), "--height", str(args.height), "--output", str(output)]
    if args.quantize:
        parts += ["--quantize", str(args.quantize)]
    if args.lora:
        parts += ["--lora-paths", args.lora[0], "--lora-scales", args.lora[1] if len(args.lora) > 1 else "1.0"]
    if args.reference:
        parts += ["--image-paths", *[str(path) for path in args.reference]]
    if args.init:
        parts += ["--image-path", str(args.init), "--image-strength", str(args.init_strength)]
    if args.low_ram:
        parts.append("--low-ram")
    return parts


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--model", required=True, choices=sorted(COMMANDS))
    parser.add_argument("--prompt", required=True)
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument("--steps", type=int)
    parser.add_argument("--width", type=int, default=1024)
    parser.add_argument("--height", type=int, default=1024)
    parser.add_argument("--quantize", type=int, choices=[4, 8])
    parser.add_argument("--lora", nargs="+", metavar=("PATH", "SCALE"))
    parser.add_argument("--reference", nargs="+", type=Path, default=[])
    parser.add_argument("--init", type=Path, help="init image for image-to-image, e.g. a control image from control_image.py")
    parser.add_argument("--init-strength", type=float, default=0.55, help="share of the init image kept (mflux --image-strength)")
    parser.add_argument("--low-ram", action="store_true")
    parser.add_argument("--output", type=Path)
    parser.add_argument("--step", default="generate", help="manifest step name, e.g. style-frame, concept")
    args = parser.parse_args(argv)

    model = common.model_info(args.model)
    output = args.output or common.CANDIDATES / args.visual_id / f"{datetime.now():%Y%m%d-%H%M%S}-{args.model}-s{args.seed}.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    inputs = list(args.reference) + ([Path(args.lora[0])] if args.lora else []) + ([args.init] if args.init else [])
    if args.lora:
        model["lora"] = common.relative(Path(args.lora[0]))

    step = common.Step(
        visual_id=args.visual_id,
        step=args.step,
        tool={"name": "mflux", "version": mflux_version()},
        model=model,
        prompt=args.prompt,
        seed=args.seed,
        parameters={"steps": args.steps or DEFAULT_STEPS[args.model], "width": args.width, "height": args.height,
                    "quantize": args.quantize, "init_strength": args.init_strength if args.init else None, "lora_scale": float(args.lora[1]) if args.lora and len(args.lora) > 1 else None},
        inputs=inputs,
        outputs=[output],
    )
    with common.record_step(step):
        subprocess.run(build_command(args, output), check=True)
        if not output.exists():
            raise SystemExit(f"mflux hat {output} nicht geschrieben.")
    print(f"geschrieben: {common.relative(output)}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
