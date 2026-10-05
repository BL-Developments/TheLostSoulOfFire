#!/usr/bin/env python3
"""Download free model files the workflows expect into ComfyUI's model folders.

    tools/visuals/.venv/bin/python tools/visuals/fetch_models.py [realesrgan-x4plus ...]

Only files from art/production/LICENSES.md are listed here. mflux and rembg fetch their own
weights on first use (Hugging Face cache, ~/.u2net); diffusion models for ComfyUI workflows
are added here once their ComfyUI file names are fixed.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import sys
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

FILES = {
    "realesrgan-x4plus": {
        "url": "https://github.com/xinntao/Real-ESRGAN/releases/download/v0.1.0/RealESRGAN_x4plus.pth",
        "folder": "upscale_models",
        "filename": "RealESRGAN_x4plus.pth",
        "sha256": "4fa0d38905f75ac06eb49a7951b426670021be3018265fd191d2125df9d682f1",
    },
}


def models_root() -> Path:
    if os.environ.get("COMFYUI_MODELS"):
        return Path(os.environ["COMFYUI_MODELS"])
    settings = Path.home() / "Library" / "Application Support" / "Comfy Desktop" / "settings.json"
    if settings.exists():
        directories = json.loads(settings.read_text(encoding="utf-8")).get("modelsDirs", [])
        if directories:
            return Path(directories[0])
    raise SystemExit("ComfyUI-Modellordner unbekannt; COMFYUI_MODELS setzen.")


def fetch(key: str) -> Path:
    spec = FILES[key]
    common.model_info(key)  # refuses anything not in the licence table
    target = models_root() / spec["folder"] / spec["filename"]
    if target.exists() and common.sha256(target) == spec["sha256"]:
        print(f"vorhanden: {target}")
        return target
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_suffix(target.suffix + ".part")
    digest = hashlib.sha256()
    with urllib.request.urlopen(spec["url"], timeout=60) as response, temporary.open("wb") as stream:
        for chunk in iter(lambda: response.read(1 << 20), b""):
            digest.update(chunk)
            stream.write(chunk)
    if digest.hexdigest() != spec["sha256"]:
        temporary.unlink()
        raise SystemExit(f"Prüfsumme von {spec['filename']} stimmt nicht.")
    temporary.replace(target)
    print(f"geladen: {target}")
    return target


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("models", nargs="*", default=sorted(FILES), choices=sorted(FILES))
    args = parser.parse_args(argv)
    for key in args.models:
        fetch(key)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
