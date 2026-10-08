"""Shared pieces of the visual production scripts: paths, the production manifest and the
cost brake.

Every generation step records one entry in `art/production/manifest.json`, so a later
agent can repeat it without watching. Paid services are a fallback only the owner opens:
`authorize_paid_call` refuses every paid call while `VISUALS_BUDGET_EUR` is missing or 0,
and every call that would push the recorded spend over the budget.
"""
from __future__ import annotations

import hashlib
import json
import os
import platform
import time
import uuid
from contextlib import contextmanager
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterator

REPO_ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = REPO_ROOT / "art" / "production"
MANIFEST = PRODUCTION / "manifest.json"
CANDIDATES = PRODUCTION / "candidates"
LOCAL_ENV = Path(__file__).resolve().parent / ".env"

BUDGET_VARIABLE = "VISUALS_BUDGET_EUR"

REQUIRED_FIELDS = (
    "id", "timestamp", "visual_id", "step", "tool", "model", "prompt", "seed",
    "inputs", "outputs", "machine", "cost_eur", "duration_s", "decision",
)
DECISIONS = ("kandidat", "angenommen", "verworfen")


def load_local_env(path: Path = LOCAL_ENV) -> None:
    """Reads KEY=VALUE lines from tools/visuals/.env (ignored by git) without overriding the shell."""
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        os.environ.setdefault(key.strip(), value.strip().strip('"').strip("'"))


load_local_env()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def relative(path: Path) -> str:
    path = Path(path).resolve()
    try:
        return path.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path)


def machine_name() -> str:
    """A role, not a host name: VISUALS_MACHINE, else 'mac' or 'linux-vm', plus the CPU architecture."""
    role = os.environ.get("VISUALS_MACHINE") or ("mac" if platform.system() == "Darwin" else "linux-vm")
    return f"{role} ({platform.system()} {platform.machine()})"


# ---- Manifest ---------------------------------------------------------------------------

def read_manifest(path: Path = MANIFEST) -> dict[str, Any]:
    if not path.exists():
        return {"version": 1, "entries": []}
    return json.loads(path.read_text(encoding="utf-8"))


def validate_entry(entry: dict[str, Any]) -> list[str]:
    """Problems with an entry; an empty list means it is complete."""
    problems = [f"Feld '{name}' fehlt" for name in REQUIRED_FIELDS if name not in entry]
    if problems:
        return problems
    for name in ("tool", "model"):
        if not isinstance(entry[name], dict) or not entry[name].get("name"):
            problems.append(f"Feld '{name}.name' fehlt")
    if not entry["model"].get("license"):
        problems.append("Feld 'model.license' fehlt")
    if not entry["outputs"]:
        problems.append("Feld 'outputs' ist leer")
    for item in entry["inputs"] + entry["outputs"]:
        if not item.get("path") or not item.get("sha256"):
            problems.append(f"Datei ohne Pfad oder Prüfsumme: {item}")
    decision = entry["decision"]
    if not isinstance(decision, dict) or decision.get("status") not in DECISIONS or not decision.get("reason"):
        problems.append(f"Feld 'decision' braucht status aus {DECISIONS} und reason")
    if not isinstance(entry["cost_eur"], (int, float)) or entry["cost_eur"] < 0:
        problems.append("Feld 'cost_eur' muss eine Zahl ≥ 0 sein")
    return problems


def append_entry(entry: dict[str, Any], path: Path = MANIFEST) -> dict[str, Any]:
    problems = validate_entry(entry)
    if problems:
        raise ValueError("Manifest-Eintrag unvollständig: " + "; ".join(problems))
    manifest = read_manifest(path)
    manifest["entries"].append(entry)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(".json.tmp")
    temporary.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    temporary.replace(path)
    return entry


@dataclass
class Step:
    """One recorded production step. Use with `record_step`."""

    visual_id: str
    step: str
    tool: dict[str, str]
    model: dict[str, str]
    prompt: str = ""
    seed: int | None = None
    parameters: dict[str, Any] = field(default_factory=dict)
    inputs: list[Path] = field(default_factory=list)
    outputs: list[Path] = field(default_factory=list)
    cost_eur: float = 0.0
    decision: str = "kandidat"
    reason: str = "Probeaufruf, noch nicht bewertet"

    def to_entry(self, duration_s: float) -> dict[str, Any]:
        return {
            "id": uuid.uuid4().hex,
            "timestamp": datetime.now(timezone.utc).isoformat(timespec="seconds"),
            "visual_id": self.visual_id,
            "step": self.step,
            "tool": self.tool,
            "model": self.model,
            "prompt": self.prompt,
            "seed": self.seed,
            "parameters": self.parameters,
            "inputs": [{"path": relative(path), "sha256": sha256(path)} for path in self.inputs],
            "outputs": [{"path": relative(path), "sha256": sha256(path)} for path in self.outputs],
            "machine": machine_name(),
            "cost_eur": round(float(self.cost_eur), 4),
            "duration_s": round(duration_s, 2),
            "decision": {"status": self.decision, "reason": self.reason},
        }


@contextmanager
def record_step(step: Step, manifest: Path = MANIFEST) -> Iterator[Step]:
    """Times the block and appends the manifest entry when it finishes without error."""
    started = time.monotonic()
    yield step
    append_entry(step.to_entry(time.monotonic() - started), manifest)


# ---- Cost brake -------------------------------------------------------------------------

class PaidCallRefused(RuntimeError):
    pass


def budget_eur() -> float:
    raw = os.environ.get(BUDGET_VARIABLE, "").strip()
    try:
        return max(0.0, float(raw)) if raw else 0.0
    except ValueError:
        return 0.0


def spent_eur(path: Path = MANIFEST) -> float:
    return round(sum(float(entry.get("cost_eur", 0)) for entry in read_manifest(path)["entries"]), 4)


def authorize_paid_call(service: str, estimated_eur: float, path: Path = MANIFEST) -> None:
    """Must run before every call that costs money. Raises PaidCallRefused unless the owner
    opened the paid fallback (VISUALS_BUDGET_EUR > 0) and the call fits the remaining budget."""
    budget = budget_eur()
    if budget <= 0:
        raise PaidCallRefused(
            f"Bezahlter Aufruf '{service}' verweigert: {BUDGET_VARIABLE} fehlt oder ist 0. "
            "Den bezahlten Notweg öffnet nur der Owner.")
    spent = spent_eur(path)
    if spent + estimated_eur > budget:
        raise PaidCallRefused(
            f"Bezahlter Aufruf '{service}' verweigert: {spent:.2f} € ausgegeben + {estimated_eur:.2f} € "
            f"geschätzt überschreitet {budget:.2f} € ({BUDGET_VARIABLE}).")


# ---- Licences (art/production/LICENSES.md) ------------------------------------------------

MODELS = {
    "flux2-klein-4b": {"name": "black-forest-labs/FLUX.2-klein-4B", "license": "Apache-2.0"},
    "z-image-turbo": {"name": "Tongyi-MAI/Z-Image-Turbo", "license": "Apache-2.0"},
    "birefnet-general": {"name": "ZhengPeng7/BiRefNet", "license": "MIT"},
    "realesrgan-x4plus": {"name": "xinntao/Real-ESRGAN RealESRGAN_x4plus", "license": "BSD-3-Clause"},
    "blender-procedural": {"name": "Blender procedural test figure", "license": "CC0 (eigene Ausgabe)"},
}

#: Models that are never allowed, with the reason (LICENSES.md, "Nicht zugelassen").
FORBIDDEN_MODELS = {
    "flux2-klein-9b": "FLUX Non-Commercial License",
    "flux2-klein-9b-kv": "FLUX Non-Commercial License",
    "flux2-klein-base-9b": "FLUX Non-Commercial License",
    "dev": "FLUX.1 [dev], nicht kommerziell",
    "krea-dev": "FLUX.1 [dev]-Ableitung, nicht kommerziell",
}


def model_info(key: str) -> dict[str, str]:
    if key in FORBIDDEN_MODELS:
        raise SystemExit(f"Modell '{key}' ist nicht zugelassen: {FORBIDDEN_MODELS[key]} (art/production/LICENSES.md).")
    if key not in MODELS:
        raise SystemExit(f"Modell '{key}' steht nicht in der Lizenztabelle; erst prüfen und in LICENSES.md eintragen.")
    return dict(MODELS[key])
