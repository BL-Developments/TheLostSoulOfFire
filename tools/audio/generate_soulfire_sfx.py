#!/usr/bin/env python3
"""Generate ElevenLabs sound-effect candidates for The Lost Soul of Fire.

Authoring tool. Candidates land in an authoring workspace that is never loaded at
runtime; only selected, locally mastered WAVs are promoted into Content/Audio by
build_session2_sfx.py.

The API key is read from ELEVENLABS_API_KEY at execution time and is never
written to disk, logged, or recorded in any manifest.

Three rules learned the hard way on the first pass, when the results were
rejected as unusable:

1.  Ask for headroom, not for the final length. Requesting a 0.6 s cue returns a
    squashed artifact with no transient and no decay. Every request here asks for
    seconds of material; build_session2_sfx.py finds the onset and trims to the
    game length, exactly like the existing Ludo mastering step does.

2.  Write a foley brief, not a list of prohibitions. Generative audio handles
    negation badly and frequently introduces whatever is named. Short, concrete,
    physical descriptions of one event outperform long "no music, no voice"
    tails by a wide margin.

3.  Ask for lossless PCM. These cues live on sharp transients, and a 128 kbps MP3
    that is then trimmed, layered and re-normalised smears exactly the part that
    carries the impact.
"""

from __future__ import annotations

import json
import os
import struct
import sys
import time
import urllib.error
import urllib.request
import wave
from pathlib import Path

ENDPOINT = "https://api.elevenlabs.io/v1/sound-generation?output_format=pcm_44100"
RATE = 44_100

# cue -> component -> list of (prompt, seconds, prompt_influence)
#
# Components are generated separately so layering and balance stay local. A
# single generation is not expected to return a finished game cue.
REQUESTS: dict[str, dict[str, list[tuple[str, float, float]]]] = {
    # --- The reaction thesis -------------------------------------------------
    # Reading a committed attack must sound like timing, not like a hit.
    "severance_window": {
        "tension": [
            ("Steel cable pulled tight, rising strain creak, close foley", 3.0, 0.75),
            ("Bowstring drawn slowly to full tension, creaking fibres, dry close recording", 3.0, 0.7),
            ("Thin metal wire tightening under load, singing strain, intimate close mic", 3.0, 0.75),
        ],
        "air": [
            ("Sharp inward rush of cold air into a stone chamber", 3.0, 0.65),
            ("Low pressure drop in a vast empty hall, air pulled inward", 3.0, 0.6),
            ("Deep suction of air through a narrow furnace vent", 3.0, 0.65),
        ],
    },
    "severance_cut": {
        "cut": [
            ("Single sharp blade slicing a taut rope, close foley", 3.0, 0.8),
            ("Thick tendon severed in one clean stroke, dry close recording", 3.0, 0.75),
            ("Machete cutting through a tight cord, crisp snap, no reverb", 3.0, 0.8),
        ],
        "release": [
            ("Delicate glass harmonic ringing out and fading into silence", 4.0, 0.6),
            ("Soft exhale of air with a faint crystal shimmer dissolving", 4.0, 0.6),
            ("Fine ash sifting down as a faint bell tone decays away", 4.0, 0.55),
        ],
    },
    # --- Enemy identity ------------------------------------------------------
    # The Hollow currently reuses the Warden's steel weapon sample.
    "hollow_swipe": {
        "lash": [
            ("Heavy burial shroud whipped through still air, dry papery rustle", 3.0, 0.75),
            ("Dry bone limb sweeping fast past a close microphone, hollow rattle", 3.0, 0.75),
            ("Brittle desiccated cloth snapping through cold air", 3.0, 0.7),
        ],
    },
    "enemy_death": {
        "collapse": [
            ("Packed ash sculpture crumbling inward into dry powder", 4.0, 0.75),
            ("Dry clay armour cracking apart and falling into a heap", 4.0, 0.7),
            ("Brittle slag structure collapsing, grit and shards settling", 4.0, 0.7),
        ],
    },
    # --- Soul lifecycle ------------------------------------------------------
    "soul_exposed": {
        "surface": [
            ("Single thin crystal glass rung softly once, long delicate ring", 4.0, 0.7),
            ("Small glass bowl touched gently, fragile high resonance", 4.0, 0.7),
            ("Faint breathy air bloom with a tiny glass chime inside it", 4.0, 0.6),
        ],
    },
    # --- Local co-op ---------------------------------------------------------
    "warden_down": {
        "gutter": [
            ("Large fire collapsing down to a weak unstable ember, low guttering flutter", 4.0, 0.7),
            ("Gas flame losing pressure and sputtering down to almost nothing", 4.0, 0.7),
            ("Torch flame failing in wind, deep irregular flutter", 4.0, 0.65),
        ],
    },
    "warden_stabilize": {
        "ignite": [
            ("Ember catching and growing into a steady strong flame, warm low ignition", 4.0, 0.7),
            ("Forge fire being fed and taking hold, swelling into a confident burn", 4.0, 0.65),
            ("Low fire building from a spark into a sustained roar, close and warm", 4.0, 0.65),
        ],
        "lock": [
            ("Two glass tones beating against each other then sliding into unison", 4.0, 0.7),
            ("Two bronze bowls detuned, slowly drifting into one steady pitch", 4.0, 0.7),
            ("Two low metal resonances phasing and then locking together", 4.0, 0.65),
        ],
    },
    "resonance_ready": {
        "sync": [
            ("Two deep drum pulses drifting and then landing exactly together, dry and close", 4.0, 0.7),
            ("Low heartbeat doubling into a single aligned thud, felt not heard", 4.0, 0.65),
            ("Two muffled floor toms converging into one beat, no room reverb", 4.0, 0.7),
        ],
    },
}


def write_wav(path: Path, pcm: bytes) -> None:
    """Wrap raw stereo 16-bit PCM in a WAV container without touching samples."""
    frames = len(pcm) // 4
    left = bytearray()
    for index in range(frames):
        # Fold to mono at source: every cue in this bank is mono, and folding
        # here avoids a lossy stereo->mono decision later in the chain.
        first, second = struct.unpack_from("<hh", pcm, index * 4)
        left += struct.pack("<h", max(-32768, min(32767, (first + second) // 2)))

    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(bytes(left))


def generate(api_key: str, text: str, seconds: float, influence: float, destination: Path) -> bool:
    payload = json.dumps(
        {
            "text": text,
            "duration_seconds": round(seconds, 2),
            "prompt_influence": influence,
            "model_id": "eleven_text_to_sound_v2",
        }
    ).encode()
    request = urllib.request.Request(
        ENDPOINT,
        data=payload,
        headers={"xi-api-key": api_key, "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=240) as response:
            data = response.read()
    except urllib.error.HTTPError as error:
        print(f"FAIL {destination.name} http={error.code} {error.read()[:200]!r}", flush=True)
        return False
    except Exception as error:  # noqa: BLE001 - authoring tool, report and continue
        print(f"FAIL {destination.name} {type(error).__name__}", flush=True)
        return False

    write_wav(destination, data)
    print(f"OK   {destination.parent.name}/{destination.name} frames={len(data)//4}", flush=True)
    return True


def main() -> int:
    api_key = os.environ.get("ELEVENLABS_API_KEY", "")
    if not api_key:
        print("ELEVENLABS_API_KEY is not set", file=sys.stderr)
        return 2

    root = Path(sys.argv[1] if len(sys.argv) > 1 else "art/audio_candidates")
    only = sys.argv[2] if len(sys.argv) > 2 else ""
    failures = 0
    for cue, components in REQUESTS.items():
        if only and only != cue:
            continue
        for component, candidates in components.items():
            for index, (text, seconds, influence) in enumerate(candidates, start=1):
                destination = root / cue / f"{component}_{index:02d}.wav"
                if destination.exists():
                    print(f"SKIP {destination.name}", flush=True)
                    continue
                if not generate(api_key, text, seconds, influence, destination):
                    failures += 1
                time.sleep(0.8)

    print(f"DONE failures={failures}", flush=True)
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
