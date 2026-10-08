#!/usr/bin/env python3
"""Run the ComfyUI workflows under tools/visuals/comfy/ over ComfyUI's HTTP API and record them.

    # check every node and input of the workflows against the running server
    tools/visuals/.venv/bin/python tools/visuals/comfy.py validate --start-server

    # upscale an image with Real-ESRGAN x4plus
    tools/visuals/.venv/bin/python tools/visuals/comfy.py run upscale --start-server \\
        --visual-id environment.arena --model realesrgan-x4plus --image INPUT_IMAGE=in.png

    # control image or inpainting (diffusion model files go into ComfyUI's model folders)
    tools/visuals/.venv/bin/python tools/visuals/comfy.py run control --model z-image-turbo \\
        --image CONTROL_IMAGE=layout.png --set UNET=... CLIP=... VAE=... CONTROLNET=... \\
        --set PROMPT="..." SEED=3

The server URL is COMFYUI_URL (default http://127.0.0.1:8188). `--start-server` launches the
local ComfyUI installation headless for the duration of the call (COMFYUI_PATH, otherwise the
Comfy Desktop installation) and uses the Desktop's shared model folders.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import urllib.parse
import urllib.request
import uuid
from contextlib import contextmanager
from pathlib import Path
from typing import Any, Iterator

sys.path.insert(0, str(Path(__file__).resolve().parent))
import visuals_common as common  # noqa: E402
from doctor import comfyui_path  # noqa: E402

WORKFLOWS = Path(__file__).resolve().parent / "comfy"
URL = os.environ.get("COMFYUI_URL", "http://127.0.0.1:8188")
DESKTOP = Path.home() / "Library" / "Application Support" / "Comfy Desktop"


# ---- HTTP -------------------------------------------------------------------------------

def get_json(path: str) -> Any:
    with urllib.request.urlopen(URL + path, timeout=30) as response:
        return json.load(response)


def post_json(path: str, payload: dict[str, Any]) -> Any:
    request = urllib.request.Request(URL + path, data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise SystemExit(f"ComfyUI lehnt den Workflow ab: {error.read().decode(errors='replace')}") from error


def upload_image(path: Path) -> str:
    boundary = uuid.uuid4().hex
    body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"image\"; filename=\"{path.name}\"\r\n"
            f"Content-Type: image/png\r\n\r\n").encode() + path.read_bytes() + f"\r\n--{boundary}\r\n".encode() + \
        (f"Content-Disposition: form-data; name=\"overwrite\"\r\n\r\ntrue\r\n--{boundary}--\r\n").encode()
    request = urllib.request.Request(URL + "/upload/image", data=body,
                                     headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
    with urllib.request.urlopen(request, timeout=60) as response:
        result = json.load(response)
    return result["name"] if not result.get("subfolder") else f"{result['subfolder']}/{result['name']}"


def server_up() -> bool:
    try:
        get_json("/system_stats")
        return True
    except OSError:
        return False


@contextmanager
def server(start: bool) -> Iterator[None]:
    if server_up() or not start:
        if not server_up():
            raise SystemExit(f"Kein ComfyUI-Server unter {URL}; mit --start-server starten oder Comfy Desktop öffnen.")
        yield
        return

    install = comfyui_path()
    if install is None:
        raise SystemExit("ComfyUI-Installation nicht gefunden; COMFYUI_PATH setzen.")
    # Comfy Desktop keeps torch in ComfyUI/.venv; a plain checkout may use any environment.
    candidates = [install / ".venv" / "bin" / "python", install.parent / "standalone-env" / "bin" / "python3"]
    python = next((candidate for candidate in candidates if candidate.exists()), Path(sys.executable))
    port = urllib.parse.urlparse(URL).port or 8188
    command = [str(python), str(install / "main.py"), "--listen", "127.0.0.1", "--port", str(port), "--disable-auto-launch"]
    extra = sorted((DESKTOP / "instance-model-paths").glob("*.yaml")) if DESKTOP.exists() else []
    if extra:
        command += ["--extra-model-paths-config", str(extra[0])]
    log = (common.CANDIDATES / "comfyui-server.log")
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open("w") as stream:
        process = subprocess.Popen(command, cwd=install, stdout=stream, stderr=subprocess.STDOUT)
        try:
            for _ in range(240):
                if server_up():
                    break
                if process.poll() is not None:
                    raise SystemExit(f"ComfyUI ist beim Start beendet worden; siehe {log}")
                time.sleep(0.5)
            else:
                raise SystemExit(f"ComfyUI startet nicht; siehe {log}")
            yield
        finally:
            process.terminate()
            try:
                process.wait(timeout=20)
            except subprocess.TimeoutExpired:
                process.kill()


# ---- Workflows --------------------------------------------------------------------------

def load_workflow(name: str) -> dict[str, Any]:
    return json.loads((WORKFLOWS / f"{name}.json").read_text(encoding="utf-8"))


def coerce(value: str) -> Any:
    for kind in (int, float):
        try:
            return kind(value)
        except ValueError:
            pass
    return value


def substitute(node: Any, values: dict[str, Any]) -> Any:
    if isinstance(node, dict):
        return {key: substitute(item, values) for key, item in node.items()}
    if isinstance(node, list):
        return [substitute(item, values) for item in node]
    if isinstance(node, str) and node.startswith("{{") and node.endswith("}}"):
        name = node[2:-2]
        if name not in values:
            raise SystemExit(f"Platzhalter {name} ohne Wert; mit --set {name}=… setzen.")
        return values[name]
    return node


def validate(names: list[str]) -> list[str]:
    """Every node class and input name of the workflows must exist on the running server."""
    info = get_json("/object_info")
    problems = []
    for name in names:
        for node_id, node in load_workflow(name)["graph"].items():
            spec = info.get(node["class_type"])
            if spec is None:
                problems.append(f"{name}: Knoten {node_id} '{node['class_type']}' gibt es nicht")
                continue
            known = set(spec.get("input", {}).get("required", {})) | set(spec.get("input", {}).get("optional", {}))
            for key in node["inputs"]:
                if key not in known:
                    problems.append(f"{name}: Knoten {node_id} '{node['class_type']}' hat keinen Eingang '{key}'")
    return problems


def wait_for(prompt_id: str, timeout: float) -> dict[str, Any]:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        history = get_json(f"/history/{prompt_id}")
        if prompt_id in history:
            entry = history[prompt_id]
            status = entry.get("status", {})
            if status.get("status_str") == "error":
                raise SystemExit(f"ComfyUI meldet einen Fehler: {json.dumps(status.get('messages', []))[:2000]}")
            if status.get("completed", entry.get("outputs")):
                return entry
        time.sleep(0.5)
    raise SystemExit("ComfyUI hat nicht rechtzeitig geantwortet.")


def download_outputs(entry: dict[str, Any], output: Path) -> list[Path]:
    paths = []
    for node_output in entry.get("outputs", {}).values():
        for index, image in enumerate(node_output.get("images", [])):
            query = urllib.parse.urlencode({"filename": image["filename"], "subfolder": image.get("subfolder", ""), "type": image.get("type", "output")})
            target = output if index == 0 and not paths else output.with_name(f"{output.stem}-{len(paths)}{output.suffix}")
            with urllib.request.urlopen(f"{URL}/view?{query}", timeout=60) as response:
                target.write_bytes(response.read())
            paths.append(target)
    if not paths:
        raise SystemExit("Der Workflow hat kein Bild ausgegeben.")
    return paths


def run(args: argparse.Namespace) -> int:
    workflow = load_workflow(args.workflow)
    values: dict[str, Any] = dict(workflow.get("_defaults", {}))
    for assignment in args.set:
        key, _, value = assignment.partition("=")
        values[key] = coerce(value)
    images = {}
    for assignment in args.image:
        key, _, value = assignment.partition("=")
        images[key] = Path(value)

    output = args.output or common.CANDIDATES / args.visual_id / f"{args.workflow}-{time.strftime('%Y%m%d-%H%M%S')}.png"
    output.parent.mkdir(parents=True, exist_ok=True)
    with server(args.start_server):
        for key, path in images.items():
            values[key] = upload_image(path)
        graph = substitute(workflow["graph"], values)
        stats = get_json("/system_stats")
        step = common.Step(
            visual_id=args.visual_id,
            step=args.step,
            tool={"name": "ComfyUI", "version": str(stats.get("system", {}).get("comfyui_version", "?")), "workflow": f"tools/visuals/comfy/{args.workflow}.json"},
            model=common.model_info(args.model),
            prompt=str(values.get("PROMPT", "")),
            seed=values.get("SEED") if isinstance(values.get("SEED"), int) else None,
            parameters={key: value for key, value in values.items() if key not in images and key != "PROMPT"},
            inputs=[*images.values(), WORKFLOWS / f"{args.workflow}.json"],
        )
        with common.record_step(step) as recorded:
            queued = post_json("/prompt", {"prompt": graph, "client_id": uuid.uuid4().hex})
            entry = wait_for(queued["prompt_id"], args.timeout)
            recorded.outputs = download_outputs(entry, output)
    print("geschrieben: " + ", ".join(common.relative(path) for path in recorded.outputs))
    return 0


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest="command", required=True)

    check = commands.add_parser("validate", help="check workflows against /object_info")
    check.add_argument("workflows", nargs="*", default=[path.stem for path in sorted(WORKFLOWS.glob("*.json"))])
    check.add_argument("--start-server", action="store_true")

    execute = commands.add_parser("run", help="run one workflow and record it")
    execute.add_argument("workflow", choices=[path.stem for path in sorted(WORKFLOWS.glob("*.json"))])
    execute.add_argument("--visual-id", required=True)
    execute.add_argument("--model", required=True, help="key from the licence table, e.g. realesrgan-x4plus")
    execute.add_argument("--image", nargs="*", default=[], metavar="PLACEHOLDER=PATH")
    execute.add_argument("--set", nargs="*", default=[], metavar="PLACEHOLDER=VALUE")
    execute.add_argument("--output", type=Path)
    execute.add_argument("--step", default="comfyui")
    execute.add_argument("--timeout", type=float, default=900)
    execute.add_argument("--start-server", action="store_true")

    args = parser.parse_args(argv)
    if args.command == "validate":
        with server(args.start_server):
            problems = validate(args.workflows)
        for problem in problems:
            print(problem)
        print(f"{len(args.workflows)} Workflows geprüft, {len(problems)} Probleme.")
        return 1 if problems else 0
    return run(args)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
