#!/usr/bin/env python3
"""Report every tool of the visual production path with its version (tools/visuals/README.md).

    tools/visuals/.venv/bin/python tools/visuals/doctor.py [--json]

Exit code 0 when every required tool is present and paid calls are refused (budget 0) or
inside an opened budget; 1 otherwise. Optional tools (the ComfyUI server, which only runs
while generating) are reported but do not fail the check.
"""
from __future__ import annotations

import argparse
import importlib.metadata
import json
import os
import platform
import re
import shutil
import subprocess
import sys
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402

HOME = Path.home()


def run(command: list[str], timeout: float = 30) -> str | None:
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=timeout)
    except (OSError, subprocess.TimeoutExpired):
        return None
    return (result.stdout or result.stderr).strip() if result.returncode == 0 else None


def tool_python_version(executable: str, package: str) -> str | None:
    """Version of a package inside the uv tool environment that provides `executable`."""
    path = shutil.which(executable)
    if path is None:
        return None
    python = Path(os.path.realpath(path)).parent / "python"
    return run([str(python), "-c", f"import importlib.metadata as m; print(m.version('{package}'))"])


def blender_config_dirs() -> list[Path]:
    if platform.system() == "Darwin":
        return [HOME / "Library" / "Application Support" / "Blender"]
    return [HOME / ".config" / "blender"]


def mpfb_version() -> str | None:
    for base in blender_config_dirs():
        for manifest in sorted(base.glob("*/extensions/*/mpfb/blender_manifest.toml")):
            match = re.search(r'^version\s*=\s*"([^"]+)"', manifest.read_text(encoding="utf-8"), re.MULTILINE)
            if match:
                return f"{match.group(1)} ({manifest.parent})"
    return None


def comfyui_path() -> Path | None:
    if os.environ.get("COMFYUI_PATH"):
        return Path(os.environ["COMFYUI_PATH"])
    installations = HOME / "Library" / "Application Support" / "Comfy Desktop" / "installations.json"
    if installations.exists():
        for installation in json.loads(installations.read_text(encoding="utf-8")):
            candidate = Path(installation["installPath"]) / "ComfyUI"
            if candidate.exists():
                return candidate
    return None


def comfyui_version(path: Path | None) -> str | None:
    if path is None:
        return None
    version_file = path / "comfyui_version.py"
    if not version_file.exists():
        return None
    match = re.search(r'__version__\s*=\s*"([^"]+)"', version_file.read_text(encoding="utf-8"))
    return f"{match.group(1)} ({path})" if match else None


def comfyui_server() -> str | None:
    url = os.environ.get("COMFYUI_URL", "http://127.0.0.1:8188")
    try:
        with urllib.request.urlopen(url + "/system_stats", timeout=2) as response:
            stats = json.load(response)
        return f"läuft auf {url} (ComfyUI {stats.get('system', {}).get('comfyui_version', '?')})"
    except OSError:
        return None


def huggingface_login() -> str | None:
    if os.environ.get("HF_TOKEN"):
        return "Token in HF_TOKEN"
    output = run([shutil.which("hf") or "hf", "auth", "whoami"])
    if output and "user=" in output:
        return "angemeldet als " + output.split("user=", 1)[1].split()[0] + " (Token im lokalen Token-Speicher)"
    return None


def package_version(name: str) -> str | None:
    try:
        return importlib.metadata.version(name)
    except importlib.metadata.PackageNotFoundError:
        return None


def paid_calls() -> str:
    try:
        common.authorize_paid_call("doctor-probe", 0.01)
    except common.PaidCallRefused as refusal:
        return f"verweigert ({common.BUDGET_VARIABLE}={common.budget_eur():g}): {refusal}"
    return f"erlaubt bis {common.budget_eur():.2f} €, bisher {common.spent_eur():.2f} € ausgegeben"


def checks() -> list[dict[str, object]]:
    blender = run(["blender", "--version"])
    rows = [
        ("Python (tools/visuals/.venv)", platform.python_version(), True),
        ("numpy", package_version("numpy"), True),
        ("pillow", package_version("pillow"), True),
        ("huggingface_hub", package_version("huggingface_hub"), True),
        ("gradio_client", package_version("gradio_client"), True),
        ("uv", run(["uv", "--version"]), True),
        ("Blender", blender.splitlines()[0] if blender else None, True),
        ("MPFB2 (Blender-Erweiterung)", mpfb_version(), True),
        ("mflux", tool_python_version("mflux-generate", "mflux"), True),
        ("rembg", tool_python_version("rembg", "rembg"), True),
        ("rembg-Laufzeit (onnxruntime)", tool_python_version("rembg", "onnxruntime"), True),
        ("ComfyUI", comfyui_version(comfyui_path()), True),
        ("ComfyUI-Server", comfyui_server(), False),
        ("Hugging Face", huggingface_login(), True),
        (".NET SDK", run(["dotnet", "--version"]), True),
        ("Kostenbremse", paid_calls(), True),
    ]
    return [{"tool": name, "version": version, "required": required, "ok": version is not None} for name, version, required in rows]


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    results = checks()
    budget_ok = common.budget_eur() == 0 or common.spent_eur() <= common.budget_eur()
    failed = [row for row in results if row["required"] and not row["ok"]]

    if args.json:
        print(json.dumps({"machine": common.machine_name(), "checks": results}, indent=2, ensure_ascii=False))
    else:
        print(f"Rechner: {common.machine_name()}")
        width = max(len(str(row["tool"])) for row in results)
        for row in results:
            mark = "ok " if row["ok"] else ("-- " if not row["required"] else "FEHLT")
            print(f"  {mark:5} {str(row['tool']):{width}}  {row['version'] or 'nicht gefunden'}")
    return 0 if not failed and budget_ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
