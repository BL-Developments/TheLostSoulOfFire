# Lizenzen der Produktionswerkzeuge

Stand: 05.10.2026, geprüft an der jeweiligen Quelle. Grundlage ist die
Entscheidung „Kostenlos zuerst“ im [Entscheidungslog](../../docs/current/DECISION-LOG.md).

Ein Modell oder Werkzeug kommt erst nach Lizenzprüfung in diese Tabelle. Nur
Einträge mit **Zugelassen: ja** dürfen Ausgaben für das Spiel erzeugen. Jede
Ausgabe nennt im Manifest (`art/production/manifest.json`) Modell und Lizenz.

## Modelle

| Modell | Zweck | Lizenz | Zugelassen | Quelle |
| --- | --- | --- | --- | --- |
| FLUX.2 [klein] 4B (`black-forest-labs/FLUX.2-klein-4B`) | Bilder erzeugen, Hausstil-LoRA | Apache 2.0 | ja | [Hugging Face](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B) |
| Z-Image Turbo (`Tongyi-MAI/Z-Image-Turbo`) | Bilder erzeugen, zweites Basismodell | Apache 2.0 | ja | [Hugging Face](https://huggingface.co/Tongyi-MAI/Z-Image-Turbo) |
| BiRefNet (`ZhengPeng7/BiRefNet`) | Freistellen über rembg | MIT | ja | [Hugging Face](https://huggingface.co/ZhengPeng7/BiRefNet), [GitHub](https://github.com/ZhengPeng7/BiRefNet) |
| Real-ESRGAN | Hochskalieren in ComfyUI | BSD-3-Clause | ja | [GitHub](https://github.com/xinntao/Real-ESRGAN) |
| TRELLIS.2 (`microsoft/TRELLIS.2-4B`) | Bild zu 3D, Weg A | MIT | ja, mit DINOv3-Pflichten | [Hugging Face](https://huggingface.co/microsoft/TRELLIS.2-4B), [GitHub](https://github.com/microsoft/TRELLIS.2) |
| DINOv3 (von TRELLIS.2 als Bildencoder genutzt) | Teil von Weg A | DINOv3 License: kommerziell erlaubt; Pflichthinweis „Built with DINOv3“; keine militärische, Waffen- oder Nuklearnutzung | ja, mit Hinweis | [Meta](https://ai.meta.com/resources/models-and-libraries/dinov3-license/) |

Wird Weg A genutzt, steht „Built with DINOv3“ in den Credits des Spiels.

## Werkzeuge

| Werkzeug | Zweck | Lizenz | Ausgaben | Quelle |
| --- | --- | --- | --- | --- |
| mflux | FLUX.2 und Z-Image auf Apple Silicon, LoRA-Training | MIT | frei | [GitHub](https://github.com/mflux-community/mflux) |
| ComfyUI / Comfy Desktop | Steuerbild, Inpainting, Hochskalieren (Workflows als JSON) | GPL-3.0 | frei; Lizenz des Werkzeugs gilt nicht für erzeugte Bilder | [GitHub](https://github.com/Comfy-Org/ComfyUI), [Desktop](https://github.com/Comfy-Org/desktop) |
| rembg | Freistellen | MIT | frei | [GitHub](https://github.com/danielgatis/rembg) |
| gradio_client | Aufruf der TRELLIS.2-Demo auf Hugging Face | Apache 2.0 | frei | [GitHub](https://github.com/gradio-app/gradio) |
| Blender 5.x | Rendern, Flipbooks, Simulation | GNU GPL; Ausgaben gehören dem Nutzer | frei | [blender.org](https://www.blender.org/about/license/) |
| MPFB2 | Grundkörper und Rig, Weg B | Code GPL-3.0, Assets CC0 1.0; Ausgaben ohne Einschränkung | frei | [LICENSE.md](https://github.com/makehumancommunity/mpfb2/blob/master/LICENSE.md) |
| Quaternius Universal Animation Library (Standard, kostenlos) | Ruhe, Laufen, Dash als Ausgangsbewegungen | CC0 1.0 | frei | [quaternius.com](https://quaternius.com/packs/universalanimationlibrary.html), [itch.io](https://quaternius.itch.io/universal-animation-library) |
| ShadowDusk.MgcbPlugin (NuGet) | `.fx` ohne Wine bauen | MIT (Repository `kaltinril/ShadowDusk`); nur zur Build-Zeit, nichts davon liegt im ausgelieferten Spiel | – | [GitHub](https://github.com/kaltinril/ShadowDusk), [NuGet](https://www.nuget.org/packages/ShadowDusk.MgcbPlugin) |
| StbImageSharp (NuGet, über MonoGame) | Bildprüfungen in `dotnet test` | Public Domain | – | [GitHub](https://github.com/StbSharp/StbImageSharp) |

Die kostenlose Standardfassung der Quaternius-Bibliothek enthält 45 Bewegungen.
Die Pro- und Source-Fassungen sind kostenpflichtig und fallen unter den
bezahlten Notweg.

## Nicht zugelassen

| Modell oder Dienst | Grund | Quelle |
| --- | --- | --- |
| FLUX.2 [klein] 9B, FLUX.2 [dev], FLUX.1 [dev] | FLUX Non-Commercial License | [Hugging Face](https://huggingface.co/black-forest-labs/FLUX.2-klein-9B) |
| RMBG-2.0 | CC BY-NC 4.0, nicht kommerziell | [Hugging Face](https://huggingface.co/briaai/RMBG-2.0) |
| Hunyuan3D | Lizenz gilt nicht in der EU | Entscheidung im Change `add-visual-vertical-slice` |
| Mixamo | Owner-Entscheidung: kein Adobe-Konto | Entscheidung im Change `add-visual-vertical-slice` |
| Bilder anderer Spiele | weder als Trainings- noch als Referenzeingabe | `docs/current/VISUAL-ART-DIRECTION.md` §2 |

## Bezahlter Notweg

fal.ai, Meshy, Scenario und kostenpflichtige Fassungen freier Pakete werden nur
genutzt, wenn der Owner nach einer Freigabe entscheidet, dass ein freier Weg
nicht reicht. Die Kostenbremse in `tools/visuals/` verweigert jeden bezahlten
Aufruf, solange `VISUALS_BUDGET_EUR` fehlt oder 0 ist.

## Zugangsdaten

Der Hugging-Face-Token steht nur in der Umgebung (`HF_TOKEN`) oder im lokalen
Token-Speicher von `huggingface_hub` (`~/.cache/huggingface/token`), nie im
Repository. `.env` und `art/production/candidates/` sind in `.gitignore`.
