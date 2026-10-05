## 1. Rahmen

- [x] 1.1 Nach Freigabe des Changes im `DECISION-LOG` datieren: Figuren über 3D und Sprite-Rendering, trainierter Hausstil, Stilbasis über Prolog und Arena, vier Owner-Freigaben, kostenlose Werkzeuge zuerst und bezahlte Dienste nur als Notweg auf Owner-Entscheidung. Prüfen: Eintrag vorhanden und verlinkt diesen Change.
- [ ] 1.2 Werkzeuge auf Mac und VM installiert (Liste in `tools/visuals/README.md`), `VISUALS_BUDGET_EUR=0`. Prüfen: `tools/visuals/doctor.py` meldet auf beiden Rechnern jedes Werkzeug mit Version, und die Skripte aus 6.1 verweigern jeden bezahlten Aufruf.
- [x] 1.3 Kostenloses Konto bei Hugging Face (Token `HF_TOKEN`), Token nur in der Umgebung. `.gitignore` deckt `.env` und `art/production/candidates/` ab. Lizenztabelle aller Modelle und Werkzeuge mit Quelle in `art/production/LICENSES.md`. Prüfen: `git status` zeigt nach einem Probelauf weder Tokens noch Kandidaten.

## 2. Lore-Grundlage (Freigabe 0)

- [x] 2.1 Region-Vertrag Prolog in `docs/current/regions/prologue.md`: alle 18 Felder aus `WORLD-GRAMMAR.md` §2, die vier Abschnitte Ufer, Suchgang, Überfahrt und Schwelle als Room Grammar, Status `PROPOSED`. Prüfen: Kein Feld leer, der Emotionale Kausalitätstest (§3) und die Qualitätsprüfung für neue Regionen (§8) sind im Dokument beantwortet.
- [x] 2.2 Region-Vertrag Industrial Cathedral in `docs/current/regions/industrial-cathedral.md`: alle 18 Felder, die Arena hinter Tür I als Gold-Standard-Raum, Status `PROPOSED`. Prüfen wie 2.1.
- [x] 2.3 Figurenblätter in `docs/current/characters/` nach der Lost-Soul-Grammatik (§4): Protagonist, Hollow, Burning und Devourer ausführlich (wer im Leben, Epoche, Kleidung, Gesicht, Haltung, Silhouette, Materialien, Akzentfarbe, was die Figur festhält); Bruder und Vaelor kurz. Status `PROPOSED`. Prüfen: Jedes Blatt beantwortet die Fragen der Lost-Soul-Grammatik und widerspricht weder `CANON-STATUS.md` noch §5b.
- [x] 2.4 Neue Abschnitte in `VISUAL-ART-DIRECTION.md`, Status `PROPOSED`: Düsternis-Charta (das Diablo-Maß: erlaubt und verboten, Licht und Dunkel, Wertverteilung), Symbolsatz (Death Flame, Wardens, Keeper, je mit Formregel) und Epochenregel für Erinnerungsorte. Prüfen: Jede Regel ist als Ja/Nein an einem Bild prüfbar formuliert.
- [x] 2.5 **Freigabe 0:** Owner korrigiert und gibt frei. Danach: freigegebene Teile auf `WORKING CANON` in `CANON-STATUS.md`, Eintrag im `DECISION-LOG`, `REGION-SEEDS.md` verlinkt die Verträge, §5 „wie Diablo“ in `VISUAL-ART-DIRECTION.md` um die Stimmung erweitert. Prüfen: Kein Lore-Dokument der Scheibe trägt noch `PROPOSED`.

## 3. Visual-Registry

- [x] 3.1 Konstantenklasse der Visual-IDs, `Content/Visuals/registry.json` (Schema wie in `design.md`) und Lader anlegen. Die Registry wird ins Ausgabeverzeichnis kopiert. Unit-Tests: gültige Registry lädt; fehlerhafte Einträge nennen ID und Feld.
- [x] 3.2 `ArtAssets` vollständig auf die Registry umstellen: Arena, Waffen, Seele, Life Flame, alle Directional-Clips und alle Effekte. Keine fest verdrahteten Texturpfade mehr. Prüfen: `--antechamber-visual-test` und `--currency-visual-test` liefern vorher und nachher gleich aussehende Bilder.
- [x] 3.3 Dummy-Darstellung über `ShapeRenderer` je Art (Figur mit Blickrichtungsmarke in Weltgröße, Effekt, Prop mit Fußpunkt) und Ersatz-Clip bei fehlendem Clip. Das `F1`-Overlay listet fehlende IDs und Clips je einmal. Prüfen: Unit-Test für die Ersatzregel; manuell mit einer absichtlich entfernten ID in der Arena.
- [x] 3.4 `art/specs/README.md` (Vorlage, Felder, Statuswerte, Verweis auf das Lore-Blatt statt kopierter Lore) und eine Visual-Spec je vorhandener ID mit `Stil: ludo`, `Status: im-spiel`. Prüfen: Owner kann jede Spec ohne Code lesen.
- [x] 3.5 Tests für Registry und Specs: jede Code-ID und jede Registry-ID hat eine Spec, Pflichtfelder gefüllt, Status gültig, `im-spiel` nur mit vollständigem Registry-Eintrag. Prüfen: `dotnet test` grün, und ein absichtlich gelöschtes Spec-Feld lässt ihn mit Visual-ID scheitern.

## 4. Shader und gemalte Szene

- [ ] 4.1 `ShadowDusk.MgcbPlugin` in `Content.mgcb` einbinden und einen Test-Shader bauen. Prüfen: `dotnet build` auf dem Mac ohne Wine und CI auf Ubuntu und Windows grün.
- [x] 4.2 `SceneGrade.fx` in `SoulfireRenderer.PresentScene`: LUT pro Bereich (Ufer, Arena) und Soul-Sense-LUT, überblendet über die bisherige Soul-Sense-Stärke. HUD und Menüs bleiben ungegradet. Neutral-LUT als Ausgangswert für Bereiche ohne eigene LUT. Prüfen: Mit Neutral-LUT gleicht das Bild dem bisherigen; Aufnahmen mit und ohne Soul Sense.
- [x] 4.3 `SpriteLit.fx`: Beleuchtungsdurchgang für Figuren mit Normal-Map, Schlüssellicht oben links und bis zu acht Soulfire-Punktlichtern aus den vorhandenen Lichtquellen. Figuren ohne Normal-Map unverändert. Prüfen: Testfigur mit flacher und mit gekippter Normal-Map neben einer Death Flame.
- [x] 4.4 `Dissolve.fx` und Auflösung besiegter Gegner mit Registry-Eintrag `dissolve`, ohne Einfluss auf Kollision, Welle, Prologablauf und Seelen. Prüfen: `ArenaWaveTests` und `PrologueFlowTests` unverändert grün; Aufnahme der Auflösung.
- [x] 4.5 `DeathFlame.fx`: Flow-Textur plus Death-Flame-Verlaufstabelle für Spurbänder und Flipbooks. Prüfen: Testspur in der Arena und Farbmessung gegen die Verlaufstabelle.
- [x] 4.6 Feste Ebenenreihenfolge, Sortierung von Akteuren und Props nach Fußpunkt, Occluder werden durchscheinend, solange sie Spieler, Gegner oder Telegraph verdecken. Unit-Tests für Sortierung und Verdeckungsprüfung.
- [x] 4.7 Sichtbare Blickrichtung mit begrenzter Drehrate und Hysterese, getrennt von der Spielrichtung; Clip-Fortschritt `distance` für Laufanimationen. Unit-Tests: schnelles Kreisen ohne Sprünge, kein Flackern an der Grenze, halbe Geschwindigkeit ergibt halbe Animationsrate, Angriffsrichtung unverändert.

## 5. Prüfwerkzeuge

- [x] 5.1 Asset-Prüfungen in `dotnet test` mit StbImageSharp: Datei, Raster, acht Richtungen, Normal-Map-Maße, transparenter Rand, Schachbrett- und Hintergrundmuster, Farbbudget `death-flame`. Mit kleinen Testbildern für jeden Fehlerfall. Prüfen: Jeder Fehlerfall scheitert mit Visual-ID, Datei und Grund; alle aktuellen Ludo-Assets bestehen oder werden korrigiert.
- [x] 5.2 `--slice-visual-test` nach dem Muster der vorhandenen Bildtests: erst das Ufer, dann die Arena bei Welle 1, mit den Steuerhaken in `GameWorld`, die die Aufnahmen brauchen (Spieler platzieren und ausrichten, Spur mit Soul Sense, Hieb, Dash, Core-Treffer, Hollow-Swipe, Besiegen, Soul Release, Soul Sense, Occluder). Prüfen: Lauf endet mit Exitcode 0 und allen benannten Aufnahmen; ohne Hollow endet er mit Fehler und Aufnahmenamen.
- [x] 5.3 `art/production/QUALITY-RUBRIC.md` aus §8, Düsternis-Charta und Stil-Bibel, mit dem Punkt „Bild erzählt die menschliche Geschichte seines Ortes“, sowie die Anleitung für die Agentenbewertung der Bildreihe nach `art/production/reviews/`. Prüfen: Eine Probebewertung der aktuellen Ludo-Arena und des Formen-Ufers liegt vor.

## 6. Produktionsweg

- [ ] 6.1 `tools/visuals/` mit Python-Umgebung, gemeinsamem Manifest-Schreiber (`art/production/manifest.json`) und Kostenbremse, die vor jedem bezahlten Aufruf gegen `VISUALS_BUDGET_EUR` prüft. Prüfen: Probeaufruf erzeugt einen vollständigen Manifest-Eintrag; ein Aufruf über der Grenze wird verweigert.
- [ ] 6.2 Lokale Generierung als Skripte: `generate.py` (mflux, Modell, LoRA, Seed, Referenzbilder), ComfyUI-Workflows als JSON unter `tools/visuals/comfy/` (Steuerbild, Inpainting, Hochskalieren) mit Aufruf über die HTTP-API, `cutout.py` (rembg mit BiRefNet). Jeder Aufruf schreibt ins Manifest. Prüfen: je ein Probebild aus mflux und ComfyUI und ein freigestelltes Bild mit Manifest-Eintrag.
- [ ] 6.3 Blender-Skripte `render_directions.py` (orthografische Kamera, Winkel aus der Stil-Bibel, acht Richtungen durch Drehen, Farb- und Normal-Durchgang, Ursprung am Fußpunkt) und `pack_sheets.py` (gemeinsamer Zuschnitt je Animation, Sheets wie bisher). Auf dem Mac (EEVEE) und auf der VM (Cycles auf CPU) lauffähig. Prüfen: Eine einfache Blender-Testfigur ergibt auf beiden Rechnern Sheets, die 5.1 bestehen und im Spiel über die Registry laufen.

## 7. Stilbasis (Freigabe 1)

- [ ] 7.1 Stil-Vorstudie: Ufer, Arena-Ecke, Spielerkonzept und Death-Flame-Treffer je mit FLUX.2 klein 4B und mit Z-Image Turbo, gestützt auf die freigegebene Lore, mit Manifest-Einträgen und gemessener Zeit pro Bild. Prüfen: acht Bilder unter `art/production/review/style-study/`.
- [ ] 7.2 Etwa 60 Stil-Frames, verteilt über Ufer, Suchgang, Überfahrt mit Skiff, Schwelle, Arena und alle Figuren der Figurenblätter, dazu Requisiten und Effekte. Prüfen: Bildseite für den Owner, nach Ort und Figur gruppiert.
- [ ] 7.3 **Freigabe 1:** Owner wählt 30 bis 40 Stil-Frames und das Basismodell und sagt, ob die freie Qualität reicht. Prüfen: Auswahl mit Datum im Manifest.
- [ ] 7.4 Hausstil-LoRA mit mflux auf dem Mac auf der Auswahl trainieren und mit zehn Prüf-Prompts außerhalb der Trainingsmotive testen, darunter je ein Ort und eine Figur, die nicht im Training waren. Prüfen: Prüfbilder unter `art/production/review/lora/`, LoRA-Datei mit Prüfsumme im Manifest und Trainingsdauer gemessen.
- [ ] 7.5 `art/production/STYLE-BIBLE.md`: Kamerawinkel, Figurenhöhe, Schlüssellicht, Materialfamilien, Wertstufen, Emissionsbudget, Death-Flame-Verlaufstabelle, Düsternis-Charta, Symbolsatz und Epochenregel als Prompt-Regeln, Verbotsliste. Dazu ein Farbskript pro Ort (Ufer, Suchgang, Überfahrt, Schwelle, Arena) mit Lichtquellen, Sekundärpalette, Wertverteilung und 2 bis 3 gesperrten Key-Arts, alle Referenzen mit SHA-256. Prüfen: Owner kann sie ohne weitere Erklärung lesen; jede Key-Art liegt unter `art/production/key-art/`.

## 8. Umgebung Arena

- [ ] 8.1 Steuerbild aus dem Kollisionslayout der Arena, Bodenplatte in 2700×1500 aus dem Hausstil über den ComfyUI-Workflow mit Steuerbild, mit Real-ESRGAN hochskaliert und in Kacheln von höchstens 2048 Pixeln geteilt. Prüfen: Gemalte Hindernisse decken sich in der Arena mit den Kollisionen (Debug-Overlay).
- [ ] 8.2 Etwa zehn Props mit Fußpunkt, eine Void-Hintergrundebene und Vordergrund-Rahmen als Occluder. Registry-Einträge und Visual-Specs mit `Stil: hausstil`. Prüfen: 5.1 grün; Occluder-Aufnahme aus 5.2 zeigt den Spieler.
- [ ] 8.3 Arena-LUT und Atmosphärenpartikel nach dem Farbskript der Arena. Prüfen: Überblicksaufnahme ohne HUD besteht die Rubrik-Punkte zu Raum, Komposition und menschlicher Geschichte.

## 9. Umgebung Ufer

- [ ] 9.1 Steuerbild aus begehbarer Fläche, Spur und Auslösepunkt des ersten Hollow im Abschnitt Ufer; Boden, Props mit Fußpunkt, Hintergrund- und Vordergrundebenen aus dem Hausstil nach dem Farbskript des Ufers. Registry-Einträge und Visual-Specs mit `Stil: hausstil`. Prüfen: 5.1 grün; begehbare Fläche, Spur und Auslöser decken sich mit dem Gemalten (Debug-Overlay).
- [ ] 9.2 Formenkomposition des Ufers (`PrologueSector.Emergence` in `PrologueEnvironment`) durch die Registry-Ebenen ersetzen und den Formen-Code des Ufers löschen; Suchgang, Überfahrt und Schwelle bleiben unverändert. Prüfen: `PrologueFlowTests` grün; Ufer-Aufnahmen aus 5.2.
- [ ] 9.3 Ufer-LUT und Atmosphäre. Prüfen: Ufer und Arena sind in der Bildreihe klar unterscheidbar, Flammenfarben in beiden gleich.

## 10. Spieler (Freigabe 2)

- [ ] 10.1 Spielerkonzept aus Figurenblatt und Hausstil (Mensch zuerst, lesbares Gesicht, Hände und Beine, Sense und Cannon im Konzept) und orthografische Ansichten von vorn, seitlich und hinten. Prüfen: Ansichten unter `art/production/review/player/`.
- [ ] 10.2 Zwei 3D-Wege für den Spieler: A) TRELLIS.2 über die Hugging-Face-Demo per `gradio_client`, in Blender vereinfacht; B) MPFB2-Grundkörper mit Mantel, Kapuze und Waffen per Skript und projizierter Bemalung aus den Ansichten. Je ein Drehbild. Prüfen: `.glb` beider Wege und Drehbilder eingecheckt, Manifest vollständig.
- [ ] 10.3 **Freigabe 2:** Owner wählt einen der beiden Wege und gibt das Modell frei oder nennt Änderungen. Prüfen: Entscheidung mit Datum im Manifest.
- [ ] 10.4 Rig (MPFB2-Rig, bei Weg A mit übertragenen Gewichten) und Bewegungen: Ruhe, Laufen und Dash aus der Quaternius Universal Animation Library per Skript übertragen, die drei Sensenhiebe als Schlüsselposen per Skript, alles auf die Spielzeiten getrimmt. Prüfen: Tabelle Animation → Spielzeit → Framezahl im Manifest; Treffer-Frame liegt im Kombofenster.
- [ ] 10.5 Acht Richtungen mit Farb- und Normal-Durchgang rendern, packen, in die Registry eintragen. Die getrennte Waffenzeichnung für den Spieler entfällt. Prüfen: 5.1 grün, Grafikspeicher gemessen und im Budget, Aufnahmen aus 5.2 für alle Richtungen und Hiebe in Ufer und Arena.
- [ ] 10.6 Visual-Spec `player` auf `Stil: hausstil`, `Status: im-spiel`; ersetzte Ludo-Texturen löschen. Prüfen: 3.5 grün.

## 11. Hollow

- [ ] 11.1 Hollow aus seinem Figurenblatt auf dem bei Freigabe 2 gewählten Weg ohne Zwischenfreigabe: Konzept, Ansichten, 3D, Rig, Ruhe, Bewegung, Swipe (Telegraph 0,42 s, Treffer 0,13 s), Treffer, Todespose mit Auflösung. Prüfen: 5.1 grün, Swipe-Aufnahme im Telegraph, Auflösungsaufnahme, Hollow im Ufer; Anzahl menschlicher Eingriffe im Manifest.
- [ ] 11.2 Visual-Spec `enemy.hollow` auf `Stil: hausstil`, `Status: im-spiel`; ersetzte Ludo-Texturen löschen. Prüfen: 3.5 grün.

## 12. Effekte

- [ ] 12.1 Sensenhiebe 1–3 als Spurband mit `DeathFlame.fx` und kurzen Flipbook-Spitzen, in drei klar unterscheidbaren Gewichten. Prüfen: Aufnahmen der drei Hiebe am Treffermoment.
- [ ] 12.2 Dash-Zündung und Core-Treffer als Blender-Flipbooks mit Partikeln. Prüfen: 5.1 grün inklusive Farbbudget.
- [ ] 12.3 Soul Release als ruhige Partikel mit Leuchten, ohne Explosion. Prüfen: Aufnahme Soul Release.
- [ ] 12.4 Ersetzte Ludo-Effekte löschen, Visual-Specs der neuen Effekte auf `im-spiel`. Prüfen: 3.5 und 5.1 grün.

## 13. Vorbereitung für den Visual-Agenten

- [ ] 13.1 Visual-Specs mit `Status: konzept` und Verweis auf Lore-Blatt und Key-Art für Suchgang, Überfahrt mit Skiff, Schwelle, Burning, Devourer und Bruder. Prüfen: 3.5 grün; jede dieser Specs nennt ihre Key-Art aus 7.5 und ihre Animationen mit Spielzeiten.
- [ ] 13.2 Reihenfolge dieser Specs als vorgeschlagene erste Warteschlange des Visual-Agenten in `art/production/NEXT.md`, mit Begründung je Eintrag. Prüfen: Jeder Eintrag verweist auf eine Visual-Spec.

## 14. Abnahme (Freigabe 3)

- [ ] 14.1 `openspec validate add-visual-vertical-slice --strict`, `dotnet build`, `dotnet test` und `--slice-visual-test` erfolgreich ausführen.
- [ ] 14.2 Agentenbewertung der Bildreihe nach der Rubrik und `art/production/SLICE-REPORT.md` mit Minuten pro Asset, Anzahl menschlicher Eingriffe, Kosten (Ziel 0 €), Wachstum des Repositorys und Empfehlung für den Agenten-Change. Prüfen: Bericht und Bewertung liegen vor.
- [ ] 14.3 README um `--slice-visual-test`, `art/specs/` und `docs/current/regions/` und `docs/current/characters/` ergänzen.
- [ ] 14.4 **Freigabe 3:** Owner spielt das Ufer mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start prologue` (Spur mit Soul Sense, erster Hollow) und die Arena mit `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 1` (Spieler in allen Richtungen, drei Hiebe, Dash, Hollow-Swipe und Auflösung, Soul Release, Soul Sense, hinter den Occluder laufen) und nimmt die Scheibe ab oder nennt Änderungen.
