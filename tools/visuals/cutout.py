#!/usr/bin/env python3
"""Cut a subject out of its background with rembg and BiRefNet (MIT) and record it.

    tools/visuals/.venv/bin/python tools/visuals/cutout.py INPUT.png --visual-id player [--output OUT.png]

The first run downloads the BiRefNet weights into rembg's model folder (~/.u2net). RMBG-2.0
is not used (non-commercial licence, art/production/LICENSES.md).
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

REMBG_MODEL = "birefnet-general"


def rembg_version() -> str:
    path = shutil.which("rembg")
    if path is None:
        raise SystemExit("rembg fehlt; siehe tools/visuals/README.md")
    python = Path(path).resolve().parent / "python"
    result = subprocess.run([str(python), "-c", "import importlib.metadata as m; print(m.version('rembg'))"],
                            capture_output=True, text=True, check=True)
    return result.stdout.strip()


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("input", type=Path)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--step", default="cutout")
    args = parser.parse_args(argv)

    output = args.output or common.CANDIDATES / args.visual_id / f"{args.input.stem}-cutout.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    step = common.Step(
        visual_id=args.visual_id,
        step=args.step,
        tool={"name": "rembg", "version": rembg_version()},
        model=common.model_info(REMBG_MODEL),
        parameters={"rembg_model": REMBG_MODEL},
        inputs=[args.input],
        outputs=[output],
    )
    with common.record_step(step):
        subprocess.run(["rembg", "i", "-m", REMBG_MODEL, str(args.input), str(output)], check=True)
    print(f"geschrieben: {common.relative(output)}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
