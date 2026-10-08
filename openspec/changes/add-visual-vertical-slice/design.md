## Context

Motivation: siehe `proposal.md`. Ausgangslage im Code:

- `Rendering/ArtAssets` lädt alle Texturen fest verdrahtet (Arena, Waffen, 12 Directional-Animationen, 12 Effekte) und zeichnet Figuren über `DrawDirectional` mit zeitbasierten Clips. `Entities/Player` zeichnet Sense und Cannon zusätzlich als eigene, gedrehte Sprites.
- Die Szene wird in ein eigenes Render-Target gezeichnet und in `SoulfireRenderer.PresentScene` mit einem Farbton plus Schleier ausgegeben. Licht entsteht additiv aus Glow-Texturen. Es gibt keine `.fx`-Datei.
- Logische Auflösung 1280×720, Ausgabe 1920×1080 (`RenderResolution`), lineare Filterung und Mipmaps für alle Texturen (`add-full-hd-rendering`).
- Automatische Bildtests gibt es schon (`--antechamber-visual-test`, `--currency-visual-test`): zeitgesteuerte Schritte, `ScreenshotCapture` schreibt nach `artifacts/screenshots/`.
- Welle 1 der Arena besteht aus drei Hollows. Die Scheibe ist damit über `--dev --start arena --wave 1` spielbar, ohne neuen Bereich.
- Der Prolog hat vier Abschnitte: Ufer (`PrologueSector.Emergence`), Suchgang (`Search`), Überfahrt (`Escape`) und Schwelle (`Threshold`). Alle vier zeichnet `Rendering/PrologueEnvironment` aus Formen, ohne eine einzige Textur. Im Ufer kommen nur Soul Sense, die Spur und der erste Hollow vor; `--dev --start prologue` beginnt dort.
- Lore: `WORLD-GRAMMAR.md` gibt mit dem Region-Vertrag (18 Felder) und der Lost-Soul-Grammatik einen starken Rahmen. Für die Orte der Scheibe ist er aber leer: In `REGION-SEEDS.md` sind beim Prolog die menschliche Herkunft und bei der Industrial Cathedral (Arena) Ereignis, zentrale Lost Soul und Release offen. Figuren haben kaum ein beschriebenes Aussehen. „Wie Diablo“ meint in `VISUAL-ART-DIRECTION.md` §5 bisher nur große Karten, nicht die Stimmung.
- MonoGame 3.8.5 baut Shader auf macOS und Linux nur mit Wine. Der Visual-Agent soll später auf einer Linux-VM laufen.
- Rechner: ein Mac mit Apple M1 Pro und 16 GB gemeinsamem Speicher (Metal-GPU) und die Linux-VM ohne GPU. Bild-KI läuft praktikabel nur auf dem Mac.
- Owner-Vorgabe (05.10.2026): erst kostenlose Werkzeuge ausreizen; bezahlte Dienste nur, wenn die freien nachweislich nicht reichen.

## Goals / Non-Goals

**Goals:**

- Die Scheibe beantwortet drei Fragen für den Owner: Erreicht der Weg die Bastion-Qualitätsstufe? Was kostet ein Asset an Geld und Zeit? Wie oft muss ein Mensch eingreifen?
- Jeder Produktionsschritt ist ein wiederholbares Skript mit Manifest-Eintrag, damit der Visual-Agent später dasselbe ohne Zuschauer tun kann.
- Gameplay kann ab dem ersten Schritt neue Figuren mit Dummys bauen.
- Eine Stilbasis, aus der spätere Agenten ohne Rückfrage neue Orte, Figuren und Effekte ableiten: freigegebene Lore-Grundlage, Hausstil-Modell, Stil-Bibel mit Farbskript und gesperrten Key-Arts pro Ort.
- Der Stil trägt nachweislich über zwei gegensätzliche Orte: den Erinnerungsort Ufer und die Soulfire-Gothic-Arena.

**Non-Goals:**

- Visual-Agent, Skills und VM-Einrichtung. Das folgt nach der Abnahme als eigener Change; die Scheibe läuft auf dem Mac.
- Fertige Spielgrafik für Suchgang, Überfahrt, Schwelle, Hub, Burning, Devourer, Cannon, Bruder und HUD. Sie bekommen in der Scheibe nur Key-Arts und Visual-Specs mit Status `konzept`.
- Laufzeit-3D im Spiel, Spine oder andere Animationslaufzeiten.
- Ein Kachel-Baukasten für Biome. Die Scheibe braucht zwei Räume; der Baukasten wird mit dem ersten Biom (#10, #23) entschieden.

## Decisions

**Lore zuerst: Freigabe 0.** Bevor ein Stil-Frame entsteht, entwirft ein Agent die fehlende Lore-Grundlage nach der Lost-Soul-Grammatik und dem Region-Vertrag aus `WORLD-GRAMMAR.md`. Der Owner korrigiert und gibt frei. Entwürfe tragen den Status `PROPOSED` nach `CANON-STATUS.md`; Freigegebenes wird `WORKING CANON`.

| Teil | Ort | Inhalt |
| --- | --- | --- |
| Region-Vertrag Prolog | `docs/current/regions/prologue.md` | alle 18 Felder, die vier Abschnitte als Room Grammar |
| Region-Vertrag Industrial Cathedral | `docs/current/regions/industrial-cathedral.md` | alle 18 Felder, die Arena als Gold-Standard-Raum |
| Figurenblätter | `docs/current/characters/<figur>.md` | Protagonist, Hollow, Burning, Devourer ausführlich (wer im Leben, Epoche, Kleidung, Gesicht, Haltung, Silhouette, Material, Akzentfarbe); Bruder und Vaelor kurz, damit sie später passen |
| Düsternis-Charta | `VISUAL-ART-DIRECTION.md`, neuer Abschnitt | das Diablo-Maß: Dunkelheit mit Lichtinseln, Verfall, sakrale Ruinen, Bedrohung durch Größe und Stille; kein Blut, kein Gore, kein Körperhorror; Grauen aus Verlust statt Ekel |
| Symbolsatz und Epochenregel | `VISUAL-ART-DIRECTION.md`, neue Abschnitte | wenige feste Zeichen (Death Flame, Wardens, Keeper) und welche Zeiten Erinnerungsorte haben dürfen |

Visual-Specs verweisen auf diese Blätter, statt Lore zu kopieren; so gibt es jede Aussage nur einmal. Verworfen: direkt mit Stil-Frames beginnen. Ohne menschliches Ereignis entsteht Kulisse statt Geschichte, und genau das schließt die eigene Grammatik aus.

**Stilbasis breit, Spielgrafik tief.**

| Tiefe | Umfang |
| --- | --- |
| Stilbasis | Stil-Frames, Hausstil-Modell und Farbskript für alle vier Prolog-Abschnitte und die Arena, dazu alle Figuren der Figurenblätter |
| fertig im Spiel | Arena (Welle 1) und Ufer: Umgebung, Spieler, Hollow, Kampfeffekte |
| für den Agenten vorbereitet | Suchgang, Überfahrt mit Skiff, Schwelle, Burning, Devourer und Bruder: je Key-Art und Visual-Spec mit Status `konzept`; das ist die erste Warteschlange des Visual-Agenten |

Ufer und Arena sind die zwei Pole der Welt: ein menschlicher Erinnerungsort und Soulfire-Gothic. Das Ufer braucht nur die Figuren, die die Scheibe ohnehin baut. Verworfen: der ganze Prolog im Spiel, weil Burning, Devourer und Skiff den Aufwand etwa verdoppeln, ohne mehr über den Stil zu lernen.

**Figuren entstehen über 3D und werden zu Sprites gerendert.** So entstanden Bastions Figuren: 3D-Modelle mit handgemalten Texturen, als Frames ausgegeben. Ein Modell mit einem Rig liefert alle acht Richtungen ohne Spiegeln, wie §5b verlangt. Neue Spielzeiten, Waffenvarianten (#15) oder Ausrüstung bedeuten neu rendern statt neu zeichnen. Verworfen:
- reine 2D-Generierung pro Frame: bricht die Ein-Rig-Regel, und Bildmodelle halten eine Figur über viele Frames nicht stabil (Ludo-Runde);
- Spine: Animation entsteht von Hand im Editor, nicht per Skript, und acht Richtungen brauchen mehrere Skelette;
- Laufzeit-3D: verlässt die gemalte 2D-Grafik und ist ein großer Engine-Umbau.

Mit der Freigabe dieses Changes bestätigt der Owner diesen Weg; danach wird er im `DECISION-LOG` datiert.

**Kostenlos zuerst: Der Mac erzeugt, die VM rendert und prüft.** Alle Modelle laufen lokal und tragen Lizenzen, die ein kommerzielles Spiel erlauben:

| Aufgabe | Werkzeug | Lizenz | Rechner |
| --- | --- | --- | --- |
| Bilder erzeugen, Hausstil trainieren | mflux (MLX, Kommandozeile) mit FLUX.2 klein 4B und Z-Image Turbo | Apache 2.0 | Mac |
| Bilder mit Steuerbild, Inpainting, Hochskalieren | ComfyUI (Workflows als JSON im Repository, HTTP-API) | GPL-Werkzeug, Modelle wie oben, Real-ESRGAN BSD | Mac |
| Freistellen | rembg mit BiRefNet | MIT | Mac und VM |
| Bild zu 3D, Weg A | TRELLIS.2 über die offizielle Hugging-Face-Demo mit freiem GPU-Kontingent, per `gradio_client` | MIT, DINOv3-Lizenz prüfen | Cloud, kostenlos |
| Bild zu 3D, Weg B | MPFB2-Grundkörper in Blender, Kleidung per Skript, gemalte Ansichten auf das Modell projiziert | Ausgaben CC0 | Mac oder VM |
| Rig | MPFB2-Rig (Rigify-kompatibel); für Weg A Gewichte von einem angepassten MPFB2-Körper übertragen | Ausgaben CC0 | Blender |
| Bewegungen | Quaternius Universal Animation Library (120+ Bewegungen) per Skript übertragen; Sensenhiebe als Schlüsselposen per Skript | CC0 | Blender |
| Rendern, Flipbooks, Simulationen | Blender 5.x | GPL, Ausgaben frei | Mac (EEVEE) oder VM (Cycles auf CPU) |

Nicht verwendet werden FLUX.1/FLUX.2 [dev] (nicht kommerzielle Modelllizenz), Hunyuan3D (Lizenz gilt nicht in der EU), RMBG-2.0 (nicht kommerziell) und Mixamo (Owner-Entscheidung, kein Adobe-Konto). Bezahlte Dienste (fal.ai, Meshy, Scenario) bleiben ein Notweg: Sie werden erst genutzt, wenn der Owner nach einer Freigabe entscheidet, dass ein freier Weg nicht reicht. Die Kostenbremse steht dafür standardmäßig auf 0.

**Ein trainierter Hausstil statt Stilbeschreibung im Prompt.** Etwa 60 Stil-Frames entstehen lokal, verteilt über alle Orte und Figuren der Stilbasis und gestützt auf die freigegebene Lore. FLUX.2 klein nimmt dabei mehrere Referenzbilder an. Der Owner wählt 30 bis 40 davon. Auf ihnen trainiert mflux über Nacht auf dem Mac ein LoRA auf dem gewählten Basismodell: ein kleines Zusatzmodell, das genau diesen Stil lernt. Die LoRA-Datei gehört dem Projekt und läuft auf jedem Rechner mit dem Basismodell. Alle Umgebungs- und Konzeptbilder entstehen danach aus diesem LoRA. Eine Stil-Vorstudie erzeugt dieselben vier Motive (Ufer, Arena-Ecke, Spielerkonzept, Death-Flame-Treffer) mit FLUX.2 klein 4B und mit Z-Image Turbo; der Owner wählt mit den Stil-Frames auch das Basismodell. Keine Bilder aus Bastion oder anderen Spielen dienen als Trainings- oder Referenzeingabe, und kein Prompt nennt ein Spiel; Prompts beschreiben Eigenschaften (gemalt, weiche Kanten, Schlüssellicht oben links, wenige Materialfamilien). So bleibt die Grafik eigenständig (`VISUAL-ART-DIRECTION.md` §2 und §8).

**Die Stil-Bibel legt eine Kamera und einen Maßstab für alles fest.** `art/production/STYLE-BIBLE.md` hält fest:
- den Kamerawinkel, gemessen an der Arena; dieselbe Zahl gilt für Umgebungsbilder und die Blender-Kamera;
- die Figurenhöhe in Ausgabepixeln, Schlüssellicht, Materialfamilien, Wertstufen und Emissionsbudget;
- die Death-Flame-Verlaufstabelle;
- ein Farbskript pro Ort (Ufer, Suchgang, Überfahrt, Schwelle, Arena): Lichtquellen, Sekundärpalette aus dem Region-Vertrag, Wertverteilung, Grading-LUT und 2 bis 3 gesperrte Key-Arts;
- Düsternis-Charta, Symbolsatz und Epochenregel als Prompt-Regeln;
- die gesperrten Referenzen mit SHA-256 und die Verbotsliste.

Die Stil-Bibel verweist auf Lore und Art Direction, statt sie zu wiederholen. Sie ist das Dokument, mit dem ein späterer Agent einen neuen Ort beginnt.

Grafik wird für ihre Darstellungsgröße bei 1920×1080 erzeugt, höchstens 1,5-fach größer; Mipmaps übernehmen das Verkleinern. Budget für Spieler und Hollow zusammen: höchstens 96 MB Grafikspeicher. Wird es überschritten, wird DXT-Kompression geprüft.

**Die Umgebung ist eine gemalte Bodenplatte mit einzelnen Props und Ebenen.**
- Der Boden der Arena (1800×1000 Weltpixel) entsteht in Ausgabeauflösung (2700×1500) aus dem Hausstil. Als Steuerbild dient das Kollisionslayout der Arena, damit Gemaltes und Spielfläche übereinstimmen. Die Platte wird hochskaliert und in Kacheln von höchstens 2048 Pixeln geteilt.
- Props sind einzelne Sprites mit Fußpunkt.
- Dazu kommen eine Void-Hintergrundebene, Vordergrund-Rahmen als Occluder und Atmosphäre als Laufzeitpartikel.

- Das Ufer folgt demselben Aufbau. Seine Formenkomposition (`PrologueSector.Emergence` in `PrologueEnvironment`) wird durch gemalte Ebenen aus der Registry ersetzt und gelöscht. Als Steuerbild dienen die begehbare Fläche des Abschnitts, die Spur und der Auslösepunkt des Hollow. Suchgang, Überfahrt und Schwelle behalten ihre Formen bis zu ihrer eigenen Grafik.

Ein Kachel-Baukasten wäre für viele Level günstiger, ist für zwei Räume aber Mehraufwand ohne Erkenntnis.

**Figuren-Pipeline.**
1. Konzept aus Figurenblatt und Hausstil, dann orthografische Ansichten von vorn, seitlich und hinten.
2. 3D-Modell auf zwei freien Wegen, beide für den Spieler:
   - Weg A: TRELLIS.2 erzeugt aus der Vorderansicht ein texturiertes Modell; Blender vereinfacht und bereinigt es.
   - Weg B: Ein MPFB2-Grundkörper in den Proportionen des Konzepts, Mantel, Kapuze und Maske als einfache Formen per Skript, darauf die gemalten Ansichten aus Kamerarichtung projiziert. Einfache Formen mit gemalter Textur sind nah an Bastions Figuren.
3. Drehbilder beider Wege zur Owner-Freigabe; der gewählte Weg gilt auch für den Hollow.
4. Rig und Bewegungen ohne Web-Dienst: Weg B bringt das MPFB2-Rig mit. Für Weg A wird ein MPFB2-Körper an das Modell angepasst und seine Gewichte werden übertragen. Ruhe, Laufen und Dash kommen aus der Quaternius Universal Animation Library (CC0) und werden per Skript auf das Rig übertragen. Die drei Sensenhiebe setzt ein Skript als Schlüsselposen (Ausholen, Hieb, Nachschwung) genau auf die Spielzeiten: Kombofenster der Sense, Dash-Dauer, Hollow-Swipe 0,42/0,13 s. Gerade Bastion-artige Animation lebt von klaren Schlüsselposen.
5. Blender 5.x rendert ohne Oberfläche (`blender -b -P tools/visuals/render_directions.py`): orthografische Kamera im Winkel der Stil-Bibel, acht Richtungen durch Drehen des Modells. Ausgegeben werden ein Farbdurchgang (gemalte Textur, weiche Schattierungsrampe, leicht eingebackenes Schlüssellicht und Umgebungsverdeckung) und ein Normal-Durchgang für die Soulfire-Lichter zur Laufzeit.
6. Frames je Richtung zu Sheets packen (zeilenweise wie bisher), gemeinsamer Zuschnitt je Animation, Ursprung am Fußpunkt.

Sense und Cannon sind Teil des Rigs und damit der Frames, wie §5b verlangt. Für so gerenderte Figuren entfällt die getrennte Waffenzeichnung; Treffer- und Kollisionslogik bleiben unverändert. Blender läuft für die Scheibe auf dem Mac.

**Effekte: Shader und Partikel zuerst, Flipbooks gezielt.** Alle Death-Flame-Effekte färbt dieselbe Verlaufstabelle im Shader (dunkles Violett → Violett → fast Weiß). So bleibt die Farbe über alle Effekte gleich, unabhängig von der Quelle.

| Effekt | Umsetzung |
| --- | --- |
| Sensenhiebe 1–3 | Laufzeit-Spurband entlang des Klingenbogens mit Flow-Textur und Death-Flame-Shader, dazu kurze Flipbook-Spitzen. Ersetzt die festen Bogen-Sheets: Der Bogen folgt der Animation in jeder Größe. |
| Dash-Zündung, Core-Treffer | in Blender gerenderte Flipbooks mit echter Transparenz plus Partikel |
| Soul Release | Partikel, ruhiges Leuchten, keine Explosion |
| Gegnerauflösung | Auflösungs-Shader mit Rauschmaske über der letzten Pose, plus Partikel |
| organische Flammenschleifen und Rauschtexturen | Blender-Simulation und prozedurales Rauschen als Flipbook; bezahltes KI-Video nur als Notweg |

**Shader-Build mit ShadowDusk.** `ShadowDusk.MgcbPlugin` kompiliert `.fx` im MGCB-Prozess ohne `mgfxc`, Wine oder Windows SDK auf allen drei Systemen. Verworfen:
- Wine auf dem Mac: unzuverlässig auf Apple Silicon und auf der späteren VM unerwünscht;
- vorkompilierte Shader im Repository: Build-Ergebnisse in Git, kein schnelles Iterieren;
- warten auf native MonoGame-Unterstützung: kein Termin.

Neue Effekte:
- `SceneGrade.fx`: zwei LUT-Streifen, überblendet für Soul Sense;
- `SpriteLit.fx`: Farbe plus Normal-Map, Schlüssellicht und bis zu acht Punktlichter, die Normal-Map als zweite Textur im gleichen Sheet-Layout;
- `Dissolve.fx`;
- `DeathFlame.fx`: Flow plus Verlaufstabelle.

**Registry als JSON, Visual-Specs als Markdown.**
- `Content/Visuals/registry.json` wird ins Ausgabeverzeichnis kopiert und beim Start mit `System.Text.Json` gelesen; Texturen lädt weiter der `ContentManager`.
- Ein Eintrag hat: ID, Art (`character`, `effect`, `environment`, `prop`, `sprite`), Palette (`death-flame`, `life-flame`, `world`), Weltgröße, Ursprung und Clips. Ein Clip hat: Pfadmuster mit `{dir}`, Framegröße, Frames, Bildrate, Schleife, optionale Normal-Map und Fortschritt (`time` oder `distance`). Dazu kommen Ersatz-Clip und optionale Auflösung.
- Die im Code verwendeten IDs stehen als Konstanten in einer einzigen Klasse; die Tests lesen sie von dort.
- Visual-Specs sind Markdown mit festen `Feld:`-Zeilen oben (Art, Status, Stil, Weltgröße, Akzentfarbe) und Abschnitten für Lore, Merkmale, Silhouette, Animationen und Effekte. Lesbar für den Owner, prüfbar für Tests. Vorlage: `art/specs/README.md`.
- Das Feld `Stil` (`ludo` oder `hausstil`) zeigt dem späteren Agenten, was noch neu zu machen ist.

Verworfen: Registry als MGCB-Content mit eigenem Importer, weil das mehr Pipeline-Code und langsamere Iteration bedeutet.

**Produktion als Skripte mit Manifest und Kostenbremse.**
- `tools/visuals/` enthält Python-3-Skripte, je Schritt eine Kommandozeile, und die Blender-Skripte.
- Jeder Generierungsschritt schreibt in `art/production/manifest.json`: Visual-ID, Schritt, Werkzeug, Modell mit Lizenz, Prompt, Seed, Eingabe-Hashes, Ausgabedatei, Rechner, Kosten (lokal 0), Dauer, Entscheidung mit Grund.
- Die Skripte verweigern jeden bezahlten Aufruf, solange `VISUALS_BUDGET_EUR` fehlt oder 0 ist, und jeden, der die Obergrenze überschreiten würde.
- Schlüssel und Tokens (Hugging Face, später bezahlte Dienste) stehen nur in Umgebungsvariablen oder in der Harness-Konfiguration, nie im Repository.
- Modelle liegen außerhalb des Repositorys im Modellordner des jeweiligen Werkzeugs; das Manifest nennt Modellname, Version und Prüfsumme.
- Eingecheckt werden angenommene Quellen (Konzepte, Ansichten, `.glb`-Rigs, `.blend`-Szenen, LUTs) und die fertigen Texturen. Kandidaten liegen in `art/production/candidates/` und sind ignoriert; das Manifest hält genug fest, um sie neu zu erzeugen.

**Prüfen in zwei Stufen.**
- `dotnet test` übernimmt alles Mechanische: Registry, Specs, Raster, Transparenz, Schachbrett, acht Richtungen, Farbbudget. Bilddateien liest der Test über StbImageSharp, das MonoGame selbst nutzt und das plattformunabhängig ist.
- Die Bildreihe aus `--slice-visual-test` deckt Ufer und Arena ab und folgt dem Muster der vorhandenen Bildtests: zeitgesteuerte Schritte, Steuerhaken in `GameWorld`, eine Aufnahme pro Schritt.
- Ein Agent bewertet die Bildreihe mit `art/production/QUALITY-RUBRIC.md` (abgeleitet aus §8 und der Stil-Bibel) und schreibt das Ergebnis nach `art/production/reviews/`. Das ist eine Empfehlung, kein Testkriterium; entscheiden tut der Owner.

**Vier Freigaben, dazwischen kein Warten.**

| Freigabe | Was der Owner sieht | Form |
| --- | --- | --- |
| 0 | Lore-Grundlage | Dokumente zum Korrigieren |
| 1 | Stil-Frames mit Basismodell | Bildseite |
| 2 | Drehbilder des Spielermodells beider 3D-Wege | Bildseite |
| 3 | die gespielte Scheibe in Ufer und Arena | Spiel und Bildreihe |

Alles andere arbeitet ohne Rückfrage durch. Die Code-Gruppen (Registry, Shader, Prüfungen, Skripte) warten auf keine Freigabe.

## Risks / Trade-offs

- [Per Skript gesetzte Schlüsselposen wirken steif] → Posen aus der Bibliothek als Ausgangspunkt, Nachschwung und Überlappung von Mantel und Kapuze per Skript. Reicht das nicht, nimmt der Owner bei Freigabe 3 vereinfachte Hiebe für die Scheibe an und der Punkt geht in den Agenten-Change.
- [Bild-zu-3D verliert gemalte Details oder liefert schlechte Topologie] → zwei Wege im Vergleich (TRELLIS.2 und MPFB2 mit projizierter Bemalung), Remesh, Textur aus dem Konzept neu projizieren; Freigabe 2 liegt vor jeder Animationsarbeit.
- [Gerenderte Figuren wirken wie ein 3D-Spiel statt gemalt] → gemalte Texturen, weiche Rampe, eingebackenes Licht, Kantendunkelung, gemeinsames Grading; Vergleich mit den gesperrten Stil-Frames. Übermalen einzelner Frames ist ausgeschlossen, weil es flimmert.
- [ShadowDusk ist ein junges Paket] → Notweg: Shader in der Windows-CI bauen und als Build-Artefakt bereitstellen; die Shader-Quellen bleiben gleich.
- [Das LoRA lernt den Stil zu eng oder zu lose] → 30 bis 40 unterschiedliche Frames über alle Orte und Figuren, Prüf-Prompts außerhalb der Trainingsmotive; das zweite Basismodell als Ausweichweg.
- [Gemischter Look: ab Welle 2 bleiben Burning, Devourer und Cannon Ludo, nach dem Ufer bleibt der Prolog aus Formen] → für die Scheibe akzeptiert; abgenommen werden Welle 1 und das Ufer. Ihre Visual-Specs tragen `Stil: ludo` oder Status `konzept` und sind damit die erste Arbeit des Agenten.
- [Freigabe 0 verzögert die Grafik] → nur die Stilarbeit wartet auf die Lore; Registry, Shader, Prüfungen und Skripte laufen parallel.
- [Agenten malen trotz Lore generisch] → Key-Arts pro Ort und Figur als gesperrte Referenzen; die Rubrik prüft, ob ein Bild die menschliche Geschichte seines Ortes erzählt (§8).
- [Gemalte Ufer-Ebenen passen nicht zur begehbaren Fläche oder zu den Auslösern] → Steuerbild aus dem Code, Vergleich im Debug-Overlay und Aufnahmen der Spur und des Hollow.
- [Freie Modelle erreichen die Qualitätsstufe nicht] → Stil-Vorstudie und Freigabe 1 zeigen das, bevor in Masse erzeugt wird. Dann entscheidet der Owner, ob genau dieser Schritt den bezahlten Notweg nimmt; alle anderen bleiben frei.
- [16 GB reichen für ein Modell nicht] → quantisierte Fassungen (4 oder 8 Bit), immer nur ein Modell geladen, Training über Nacht. TRELLIS.2 läuft deshalb nicht lokal, sondern über die Hugging-Face-Demo.
- [Das freie Hugging-Face-Kontingent ist aufgebraucht] → am nächsten Tag weiter oder Weg B; die Scheibe misst, wie viele Modelle pro Tag möglich sind.
- [Der Mac wird zum Engpass, sobald der Agent auf der VM läuft] → entscheidet der Agenten-Change: Mac als Generierungsdienst über Tailscale oder eine GPU an der VM.
- [Repository wächst durch Quellen] → nur angenommene Quellen einchecken und das Wachstum im Abschlussbericht nennen; Git LFS entscheidet der Agenten-Change.
- [Nutzungsrechte generierter Grafik] → nur Modelle und Werkzeuge aus der Lizenztabelle; jede Ausgabe nennt im Manifest Modell und Lizenz. Ein neues Modell kommt erst nach Lizenzprüfung in die Tabelle.

## Migration Plan

1. Registry einführen und alle Ludo-Assets ohne sichtbare Änderung umziehen. Die vorhandenen Bildtests liefern vorher und nachher dieselben Bilder.
2. Shader, Beleuchtung, Grading und Ebenen einbauen, zunächst mit den Ludo-Assets und neutralem Grading.
3. Neue Grafik ersetzt Visual-ID für Visual-ID. Ersetzte Ludo-Texturen werden gelöscht; ihre Quellen bleiben in `art/ludo_delivery/` und in der Git-Historie.

Rückweg: Jeder Schritt ist ein eigener Commit und einzeln umkehrbar.

## Open Questions

- Ob ein bezahlter Notweg nötig wird und mit welcher Obergrenze: entscheidet der Owner bei einer Freigabe, falls ein freier Weg nicht reicht. Bis dahin steht `VISUALS_BUDGET_EUR` auf 0.
- Ob Git LFS für `.blend`- und `.glb`-Dateien nötig wird: entscheidet der Agenten-Change anhand des gemessenen Wachstums.
