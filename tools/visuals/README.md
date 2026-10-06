# Produktionsweg der gemalten Grafik

Skripte für den Change `add-visual-vertical-slice`. Jeder Schritt läuft über ein
Skript hier und landet im Produktionsmanifest `art/production/manifest.json`.
Damit ist die Scheibe zugleich das Rezept für den späteren Visual-Agenten.

Grundsatz: **kostenlos zuerst.** Nur Modelle und Werkzeuge aus
[`art/production/LICENSES.md`](../../art/production/LICENSES.md). Bezahlte Dienste
sind ein Notweg, den nur der Owner öffnet (siehe Kostenbremse).

## Werkzeuge

Stand auf dem Mac (Apple M1 Pro, 16 GB) am 05.10.2026, gemeldet von `doctor.py`:

| Werkzeug | Version | Mac | Linux-VM | Installation |
| --- | --- | --- | --- | --- |
| Python + uv | 3.12, uv 0.12 | ja | ja | `brew install uv` bzw. Paketquelle; dann die Umgebung unten |
| numpy, pillow, huggingface_hub, gradio_client | siehe `requirements.txt` | ja | ja | in `tools/visuals/.venv` |
| mflux (FLUX.2 [klein] 4B, Z-Image Turbo) | 0.21.0 | ja | nein (MLX braucht Apple Silicon) | `uv tool install mflux` |
| rembg mit BiRefNet | 2.0.85 | ja | ja | `uv tool install "rembg[cpu,cli]"` (mit ONNX-Laufzeit) |
| Blender | 5.2 LTS | ja (EEVEE) | ja (Cycles auf der CPU) | blender.org oder `brew install --cask blender` |
| MPFB2 (Blender-Erweiterung) | 2.0.17 | ja | ja | in Blender: Get Extensions → „MPFB“ |
| ComfyUI | 0.38 (Comfy Desktop) | ja | optional, nur CPU | Mac: Comfy Desktop; VM: `git clone` von ComfyUI und `pip install -r requirements.txt` |
| Hugging Face-Konto (kostenlos) | – | ja | ja | `hf auth login`; der Token liegt im lokalen Token-Speicher oder in `HF_TOKEN`, nie im Repository |
| .NET SDK | 9 oder neuer | ja | ja | dotnet.microsoft.com |

Hinweis für die VM: `ShadowDusk.MgcbPlugin` 0.20.0 bringt `dxcompiler` nur für
Linux **x64** mit, nicht für arm64. Eine VM, die Shader bauen soll, muss x64 sein.

Prüfen, ob alles da ist:

```sh
uv venv -p 3.12 tools/visuals/.venv
uv pip install -p tools/visuals/.venv/bin/python -r tools/visuals/requirements.txt
tools/visuals/.venv/bin/python tools/visuals/doctor.py
```

`doctor.py` meldet jedes Werkzeug mit Version und endet mit Exitcode 0, wenn alles
Nötige vorhanden ist und die Kostenbremse bezahlte Aufrufe verweigert.

## Umgebungsvariablen

| Variable | Bedeutung |
| --- | --- |
| `VISUALS_BUDGET_EUR` | Obergrenze für bezahlte Dienste in Euro. Fehlt sie oder ist sie 0, wird jeder bezahlte Aufruf verweigert. Lokal steht `VISUALS_BUDGET_EUR=0` in `tools/visuals/.env` (von git ignoriert). |
| `HF_TOKEN` | optional; ohne ihn nutzt `huggingface_hub` den lokalen Token-Speicher von `hf auth login` |
| `COMFYUI_URL` | ComfyUI-Server, Standard `http://127.0.0.1:8188` |
| `COMFYUI_PATH` | ComfyUI-Ordner, falls nicht die Installation von Comfy Desktop |
| `COMFYUI_MODELS` | ComfyUI-Modellordner, falls nicht der von Comfy Desktop |
| `VISUALS_MACHINE` | Rolle des Rechners im Manifest (`mac`, `linux-vm`); Standard nach Betriebssystem |

## Skripte

| Skript | Zweck |
| --- | --- |
| `doctor.py` | Werkzeuge und Versionen, Kostenbremse |
| `visuals_common.py` | Manifest-Schreiber, Kostenbremse, Lizenzschlüssel der Modelle |
| `generate.py` | Bild mit mflux erzeugen (Modell, LoRA, Seed, Referenzbilder) |
| `comfy.py` | ComfyUI-Workflows aus `comfy/` über die HTTP-API ausführen und prüfen (`validate`) |
| `comfy/upscale.json` | Hochskalieren mit Real-ESRGAN x4plus |
| `comfy/control.json` | Bild mit Steuerbild (ControlNet), Modell und ControlNet als Platzhalter |
| `comfy/inpaint.json` | Inpainting mit Maske, Modell als Platzhalter |
| `fetch_models.py` | freie Modelldateien für ComfyUI laden (mit Prüfsumme) |
| `cutout.py` | Freistellen mit rembg und BiRefNet |
| `blender/render_directions.py` | Figur in acht Richtungen rendern, Farb- und Normal-Durchgang, Fußpunkt fest |
| `blender/test_figure.py` | prozedurale Testfigur für die Pipeline |
| `blender/build_player.py` | Spielfigur: MPFB2-Körper, Mantel, Sense und Cannon im Rig (IK an den Griffen), Aktionen idle, run, swing1–3, aim |
| `blender/build_hollow.py` | Hollow: großer, dünner MPFB2-Körper, ausfransendes Gewand, Maske, Aktionen idle, move, swipe |
| `blender/figure_kit.py` | gemeinsame Bausteine: Toon-Material mit Pinselrauschen, Konturhülle, Poser nach Bühnenrichtungen, Kleidung aus dem Körper, Bodenkontakt |
| `blender/combat_kit.py` | Kampfanimation: Schlüsselposen über den Spielfortschritt mit monotonen Kubiken, Bein-IK mit gesetzten Füßen und Schrittbögen, Hüfte und Wirbelsäule, Rückrechnung der Spielvorwärtsbewegung, Neu-Keyen einzelner Aktionen (`--rekey`) |
| `blade_paths.py` | Klingenkernbahn der Hiebe (aufgezeichnet beim Keyen) als C#-Tabelle `Rendering/ScytheBladePaths.cs` für das Flammenband |
| `pack_sheets.py` | Frames zu Sheets packen, gemeinsamer Zuschnitt, Registry und `Content.mgcb` eintragen; mit `--pixels-per-unit` ein Zuschnitt und Ursprung je Clip bei festem Maßstab |
| `feather_frame_edges.py` | äußere Pixel jedes Frames ausblenden (transparenter Rand) |
| `value_distribution.py` | Wertverteilung der Düsternis-Charta (W1, W2, W4, W6) an Aufnahmen messen |

Beispiele:

```sh
# Bild erzeugen (lädt das Modell beim ersten Mal, etwa 24 GB)
tools/visuals/.venv/bin/python tools/visuals/generate.py --visual-id environment.shore \
  --model flux2-klein-4b --quantize 8 --seed 7 --prompt "painted harbour platform at night, ..."

# Hochskalieren über ComfyUI (startet den Server bei Bedarf ohne Oberfläche)
tools/visuals/.venv/bin/python tools/visuals/fetch_models.py realesrgan-x4plus
tools/visuals/.venv/bin/python tools/visuals/comfy.py run upscale --start-server \
  --visual-id environment.shore --model realesrgan-x4plus --image INPUT_IMAGE=bild.png

# Figur bauen, rendern und packen (320 px auf 3,2 m = 1,5 Pixel je Weltpixel, 66,7 Weltpixel je Meter)
blender -b -P tools/visuals/blender/build_player.py -- --out art/production/candidates/player/player.blend
blender -b art/production/candidates/player/player.blend -P tools/visuals/blender/render_directions.py -- \
  --out art/production/candidates/player/render --action run --animation move --frames 12 \
  --resolution 320 --ortho-scale 3.2 --foot 0.8
tools/visuals/.venv/bin/python tools/visuals/pack_sheets.py --render art/production/candidates/player/render \
  --animation move --fps 12 --visual-id player --texture-dir Textures/Player/Animations \
  --pixels-per-unit 1.5 --progress-distance 180 --decision angenommen --reason "..." --register
```

Kampfaktionen einer gebauten Figur neu keyen, ohne Körper und Kleidung neu zu bauen (die
Bildzahlen stehen in `COMBAT_FRAMES` des jeweiligen Skripts), eine Vorschau in zwei Richtungen
rendern und danach alle acht Richtungen wie oben rendern und packen:

```sh
blender -b art/production/candidates/player/player.blend -P tools/visuals/blender/build_player.py -- \
  --out art/production/candidates/player/player.blend --rekey swing1,swing2,swing3 \
  --paths art/production/candidates/player/swing_paths.json
blender -b art/production/candidates/player/player.blend -P tools/visuals/blender/render_directions.py -- \
  --out art/production/candidates/player/preview --action swing3 --animation swing3 --frames 26 \
  --resolution 400 --ortho-scale 4.0 --foot 0.8 --directions se,e --no-normals
tools/visuals/.venv/bin/python tools/visuals/blade_paths.py --paths art/production/candidates/player/swing_paths.json
```

`build_hollow.py`, `build_burning.py` und `build_devourer.py` kennen `--rekey` ebenso.

Gerenderte Figuren stehen mit den Füßen auf ihrer Spielposition. Was fliegt
(Schüsse, Funken, Hiebe), zeichnet das Spiel auf Körperhöhe darüber
(`Rendering/FigureHeights.cs`).

Prompts beschreiben Eigenschaften (gemalt, weiche Kanten, Schlüssellicht oben
links, wenige Materialfamilien), nie ein anderes Spiel.

## Manifest

`art/production/manifest.json` ist ein Protokoll, an das nur angehängt wird. Ein
Eintrag hat: `id`, `timestamp`, `visual_id`, `step`, `tool` (Name, Version),
`model` (Name, Lizenz, ggf. LoRA), `prompt`, `seed`, `parameters`, `inputs` und
`outputs` (Pfad und SHA-256), `machine`, `cost_eur` (lokal 0), `duration_s` und
`decision` (`kandidat`, `angenommen`, `verworfen` mit Grund).

Kandidaten liegen unter `art/production/candidates/` und sind von git
ausgenommen; das Manifest hält genug fest, um sie neu zu erzeugen. Eingecheckt
werden nur angenommene Quellen und fertige Texturen.

## Kostenbremse

Jeder bezahlte Aufruf muss vorher `visuals_common.authorize_paid_call(dienst, geschätzte_kosten)`
durchlaufen. Die Funktion verweigert, solange `VISUALS_BUDGET_EUR` fehlt oder 0
ist, und jeden Aufruf, der die bisher im Manifest verbuchten Kosten über die
Grenze heben würde. Bisher gibt es keinen bezahlten Aufruf im Repository.

## Tests

```sh
tools/visuals/.venv/bin/python -m unittest discover -s tools/visuals/tests
```
