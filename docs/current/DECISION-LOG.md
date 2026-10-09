# Decision Log

Record owner-approved, rejected or revised product decisions here. Newer dated entries override older conflicting entries.

The entries from 2026-09-06 and earlier describe implementation work on the
`prototype/design-polish` branch. Files and systems they name (for example
`tools/visual-max/`, `SoftShapes.cs`, Team Resonance, Severance) are not
necessarily present on `main`.

## 2026-10-09 — Zufällige Raumfolge der Level

- Die Räume eines Levels werden zufällig erzeugt, grob linear, mit
  gelegentlichen parallelen Wegen zum Levelende.
- Man betritt die Räume einzeln wie bei Hades; ein Weg ist die Wahl zwischen
  den Ausgängen eines geräumten Raums.
- Räume bleiben vorerst wie die Arena aufgebaut. Die Wellen wachsen mit dem
  Raumfortschritt.
- Tür I führt nach der Umsetzung in den Biom-I-Run mit Graubox-Leveln; die
  Arena ist nur noch per `--dev --start arena` erreichbar.
- Anzahl der Kampfstufen und Gabelungswahrscheinlichkeit sind Arbeitswerte in
  `GameBalance`.

Nachweis: Entscheidungen des Owners im Projekt-Thread am 09.10.2026.

## 2026-10-06 — Teilsicherung, Extraktion und Niederlage (#53)

- Reisepunkte stehen am Ende von Level 1 und 2. Sie bieten Teilsichern und
  weiter, Weiter ohne Sichern oder Extrahieren.
- Teilsichern überträgt eine feste Quote von 50 % (Arbeitswert) beider
  Run-Bestände, Geld und Glut, in den gesicherten Bestand. Der Spieler wählt
  keinen Betrag.
- Höchstens eine Teilsicherung je Reisepunkt. Der gesicherte Anteil wird je
  Währung abgerundet; der Rest bleibt im Run. Keine Obergrenze pro Sicherung.
- Extrahieren sichert beide Run-Bestände vollständig und beendet den Run.
  Nach dem Bosssieg werden beide Run-Bestände in der Homebase vollständig
  gesichert.
- Bei Niederlage gehen beide Run-Bestände verloren (bestätigt aus #52).
- Regeln und Rechenbeispiele stehen in [ECONOMY.md](ECONOMY.md).

Nachweis: ausdrücklich bestätigte Produktentscheidung des Owners zu #53.

## 2026-10-05 — Freigabe 0: Lore-Grundlage der visuellen Scheibe

Der Owner gibt die Lore-Grundlage des Changes `add-visual-vertical-slice` mit
Korrekturen frei. Sie ist damit WORKING CANON:

- Region-Verträge [Prolog „Das unvollendete Ufer“](regions/prologue.md)
  (Seebahnhof, Fähre, Warten) und
  [Industrial Cathedral „Die Gießhalle“](regions/industrial-cathedral.md).
  Das Ufer und der Seed „The Drowned Line“ bestehen nebeneinander.
- Figurenblätter unter [`characters/`](characters/): Protagonist, Hollow,
  Burning, Devourer, Bruder und Vaelor.
- Düsternis-Charta, Symbolsatz und Epochenregel in
  [VISUAL-ART-DIRECTION.md](VISUAL-ART-DIRECTION.md) §10–§12; §5 „wie Diablo“
  umfasst jetzt auch die Stimmung.

Korrekturen gegenüber dem Entwurf:

- Der Protagonist ist Mitte zwanzig und trägt keine Kapuze. Er trägt **keinen
  Schal**; der rostrote Schal aus Session 3 (06.09.2026) ist damit ersetzt.
  Sein einziges persönliches Stück aus dem Leben ist ein **Kompass**; was er
  bedeutet, bleibt offen (Anchor UNRESOLVED).
- Der Bruder ist **nicht grün**. Sein Unterscheidungsmerkmal neben Kapuze und
  Schultermantel ist ein **aufgerolltes Bergungsseil** über der Schulter; er ist
  der, der Verlorene herauszieht. Woran er festhält, bleibt offen.
- Ob persönliche Gegenstände wie Kompass oder Seil eine Wirkung im Spiel haben,
  wird später eigens entschieden. Die Figurenblätter legen nur das Aussehen fest;
  Spielregeln ändern sich nicht.

## 2026-10-05 — Produktionsweg der gemalten Grafik

Der Owner gibt den Change
[`add-visual-vertical-slice`](../../openspec/changes/add-visual-vertical-slice/proposal.md)
frei. Damit gilt:

- **Figuren entstehen über 3D und Sprite-Rendering.** Konzept, 3D-Modell, ein
  Rig und Blender-Rendering liefern alle acht Richtungen aus einem Modell
  (§5b in `VISUAL-ART-DIRECTION.md`). Reine 2D-Generierung pro Frame, Spine und
  Laufzeit-3D sind verworfen.
- **Ein trainierter Hausstil** (LoRA auf einem frei lizenzierten Basismodell)
  trägt alle Umgebungs- und Konzeptbilder, statt den Stil in jedem Prompt neu
  zu beschreiben. Keine Bilder anderer Spiele als Trainings- oder
  Referenzeingabe, kein Spielname im Prompt.
- **Die Stilbasis umfasst den ganzen Prolog und die Arena:** Ufer, Suchgang,
  Überfahrt, Schwelle und Arena bekommen Stil-Frames, Farbskript und gesperrte
  Key-Arts. Fertig im Spiel sind in der Scheibe nur Ufer und Arena (Welle 1).
- **Vier Owner-Freigaben:** Lore-Grundlage (0), Stil-Frames mit Basismodell (1),
  3D-Modell des Spielers vor der Animation (2), Abnahme der Scheibe im Spiel (3).
  Dazwischen arbeiten Agenten ohne Rückfrage.
- **Kostenlose Werkzeuge zuerst.** Nur frei lokal ausführbare Modelle und
  Werkzeuge mit Lizenzen, die ein kommerzielles Spiel erlauben
  (`art/production/LICENSES.md`). Bezahlte Dienste sind ein Notweg, den nur der
  Owner nach einer Freigabe öffnet; bis dahin steht `VISUALS_BUDGET_EUR` auf 0.

## 2026-10-04 — Arena mit zehn Wellen

- Die Arena hat zehn Wellen statt vier (Change `extend-arena-waves`). Die
  Wellen 1 bis 4 bleiben unverändert; ab Welle 5 rücken Gegner in Schüben vom
  Arenarand nach, mit Ankündigung vor dem Erscheinen. Die Gegnerzahl steigt
  von 3 auf 15. Alle Werte sind Arbeitswerte in `GameBalance`.
- Geldkisten erscheinen nach den Wellen 3, 6 und 9 statt nach den Wellen 1
  bis 3. Das ersetzt die Kistenregel im Eintrag zu #52; es bleibt bei drei
  Kisten pro Run.

## 2026-10-04 — Erster Fähigkeitenpool bestätigt

Der Owner bestätigt die Auswahl und die beschriebenen grundlegenden Wirkungsregeln für #79:
AB-006 Zweiter Atem (Heilung), AB-012 Durchschlag (direkter Angriff),
AB-017 Rückstoßsprung (Movement), AB-021 Sog (Kontrolle),
AB-027 Vergeltung (Verteidigung) und AB-048 Vorlage (Vorbereitung/Koop).

Die gemeinsamen Regeln und Wirkungsgrenzen aus [FIRST-ABILITY-POOL.md](FIRST-ABILITY-POOL.md)
bilden die Designgrundlage. Konkrete Arbeits-/Balancewerte und die dort ausdrücklich
offenen Grenzfälle sind weiterhin zu spezifizieren. Auswahlbestätigung ist kein
Gameplay-Umsetzungsnachweis und schließt #79 noch nicht ab.
Die übrigen 44 Fähigkeiten und alle Ultimate-Ideen bleiben Vorschläge.

## 2026-10-04 — Zwei Währungen (#52)

- Arbeitsnamen: **Geld** und **Glut** (mystische Ressource). Endgültige Namen
  und Lore sind offen.
- Geld stammt aus Kisten in Levels und aus NPC-Belohnungen für eine vorgegebene
  Anzahl besiegter Monster eines Typs. Es bezahlt Waffenkauf und Schmied.
- Glut stammt aus besiegten Monstern, Menge je Monstertyp. Sie fliegt mit einer
  Animation vom Monster zum Spieler; gutgeschrieben wird beim Besiegen genau
  einmal, unabhängig von der Animation. Erlösung ist ein getrennter Vorgang
  ohne zweite Gutschrift. Das ersetzt die frühere Herkunft „aus Erlösung“.
- Aktive Fähigkeiten zahlen Glut aus dem Run-Bestand; Waffen- und
  Fähigkeits-Skilltrees zahlen aus dem gesicherten Bestand.
- Beide Währungen haben einen Run-Bestand und einen gesicherten
  Homebase-Bestand. Bei Niederlage gehen beide Run-Bestände verloren.
- Jeder Run beginnt mit einem kleinen kostenlosen Glut-Vorrat, der ebenfalls
  gesichert werden darf. Gesicherte Glut wird nicht in einen Run mitgenommen.
- Biome haben drei Levels mit dem Boss am Ende von Level 3. Geld kann am
  Levelende gesichert werden, nach Bosssieg in der Homebase.
- Vorläufig bis zur Entscheidung in #53 (Change `add-run-currencies`): Der
  Arena-Abschluss sichert beide Run-Bestände vollständig; nach jeder Welle
  außer der letzten erscheint eine Kiste mit Geld, die nach dem Öffnen
  verschwindet. Zahlen sind Arbeitswerte.

## 2026-10-02 — Grafikstil: gemalt wie Bastion

- Das Spiel soll aussehen wie gemalte, hochaufgelöste 2D-Grafik im Sinne von
  Bastion, nicht wie Pixel-Art. Bastion ist dafür die Stilreferenz.
- Die Pixel-Regeln aus `VISUAL-ART-DIRECTION.md` §5a („ein gezeichnetes Pixel
  ist ein Bildschirmpixel“, kein Anti-Aliasing, kein Dithering, mindestens
  zweipixelige Formen) gelten nicht mehr. Die übrigen Regeln dort (ein
  Schlüssellicht, wenige Materialfamilien, Emission als Budget, ein
  gesättigter Akzent pro Figur) bleiben.
- Assets werden in hoher Auflösung gebaut und weich skaliert (lineare
  Filterung, Mipmaps). Damit ist jede Ausgabeauflösung möglich; der erste
  Schritt ist eine Full-HD-Ausgabe (OpenSpec-Change `add-full-hd-rendering`).

## 2026-10-02 — Code-Basis für Charakter-Systeme

- `main` ist die Code-Basis für alle weiteren Systeme. Der Branch
  `prototype/design-polish` wird nicht gemergt.
- Systeme, die nur dort existieren (lokaler Koop, Team Resonance, Severance
  Window, Down/Stabilisieren, WardenRoster), werden einzeln als eigener
  OpenSpec-Change nach `main` portiert, sobald ein Issue sie braucht.
  Der Branch dient bis dahin als Referenz.

## 2026-10-02 — Maßgebliche Dokumente auf `main`

- Entscheidungslog, Aufnahme-Zusammenfassung, Produktidentität, Canon und Lore
  liegen unter `docs/current/` auf `main`. Bei Widersprüchen gilt dieses
  Verzeichnis vor `docs/mvp/` und `docs/vision/`.
- Zustandsberichte des Branches `prototype/design-polish` (`CURRENT-SLICE.md`,
  `HANDOFF.md`, `AGENT-CONTEXT-COMPACT.md`) werden nicht übernommen. Den
  spielbaren Stand von `main` beschreiben die Specs unter `openspec/specs/`.

## 2026-09-29 — Run-Niederlage und Soul-Echo-Plan

- Eine reguläre Run-Niederlage bedeutet Kampfunfähigkeit. Andere Wardens bergen
  die Unterlegenen vor der endgültigen Zerstörung und bringen sie zur Homebase.
  Im Koop kann der Bruder an der Bergung beteiligt sein; auch eine vollständige
  Team-Niederlage bleibt ein verlorener Run mit Rückkehr. Die genaue Inszenierung
  der Bergung ist offen. Verlorene Runs sind kein endgültiger Warden-Tod.
- Erst die endgültige Zerstörung eines Wardens vollendet den Übergang ins wahre
  Jenseits. Die bestehende Kosmologie bleibt dafür gültig.
- Der Sechs-Item-Soul-Echo-Proof entfällt als Produktziel. Einzelne brauchbare
  Effekte dürfen später als Fähigkeiten oder Waffenfortschritt neu entworfen
  werden; daraus folgt weder ein Inventar noch eine Pflicht zu sechs Effekten.

## 2026-09-08 — Owner direction: Konzeptaufnahme hat Vorrang

Quelle: vom Owner bereitgestelltes undatiertes Gesprächstranskript, ausgewertet
am 2026-09-08. Das Datum bezeichnet den Import, nicht das Aufnahmedatum.
Auftrag: Zusammenfassung sowie logisch kategorisierte GitHub-Meilensteine und
Issues. Bei Konflikten ausdrücklich die Aufnahme bevorzugen.

Die ausführliche Einordnung steht in
[`RECORDING-SUMMARY-2026-09-08.md`](RECORDING-SUMMARY-2026-09-08.md), die
24 Arbeitsaufgaben in sechs geplanten Meilensteinen in
[`../planning/recording-2026-09-08/backlog.json`](../planning/recording-2026-09-08/backlog.json).

### Festgelegte Richtung

- Dauerhafte Warden-Homebase als Vorbereitung und Rückkehrpunkt; Runs durch
  mehrere Levels eines gewählten Bioms. Neustart nach Niederlage/Extraktion
  bei Level 1 dieses Bioms. Freigeschaltete Biome bleiben direkt anwählbar.
- Der vorausgehende Biom-Boss schaltet das nächste Biom frei. Ein Meta-Level
  ersetzt diese Bedingung nicht. Boss-Handschrift prägt Umgebung und Gegner.
- Mindestens normales Geld und mystische Ressource aus erfolgter Erlösung.
  Die Seele geht weiter; ihre Person wird nicht gesammelt oder verbraucht.
- Mystische Ressource für Fähigkeiten im Run oder dauerhaften Fortschritt.
  Ungesicherter mystischer Bestand ist bei Niederlage verloren. Reisepunkte
  ermöglichen Teilsicherung mit Weiterreise oder vollständige Extraktion.
- Eine Hauptwaffe vor dem Run wählen: Nah- oder Fernkampf. Drei zufällige
  Fähigkeitsangebote, daraus zwei auswählen, als konkretes Startmodell.
  Geteilte Kräfte anderer jagender Wardens begründen die wechselnde Auswahl.
- Aktive Fähigkeiten, Ultimate, Waffenperks/Kombos und Timing-Counter sind
  gewünscht. Getrennte Waffen- und Fähigkeitsbäume; später dauerhafte Fähigkeiten.
- NPC-Begegnungen schalten Händler, Schmied und weitere Homebase-Dienste
  schrittweise frei. Feste Figur ohne anfängliche Klassenwahl.
- Tutorial mit Vaelors Auswahl, Sense, ersten Kämpfen und Bergung zur Homebase.
- Kein gewöhnliches Loot-Inventar. Collectibles/Easter Eggs belohnen Erkundung.

### Bewusste Abweichungen gegenüber bisherigen Texten

- Vaelors Auswahl des Protagonisten ist nun ein gewünschter Storybeat. Ältere
  absolute Verbote persönlicher Auswahl sind hierfür überholt. Die Aufnahme
  erklärt den metaphysischen Mechanismus nicht; Gottstatus, Siegel oder
  Allwissenheit werden dadurch nicht eingeführt.
- Die gleichzeitige Scythe/Cannon-Verfügbarkeit ist Implementierungsbestand;
  Ziel regulärer Runs ist eine gewählte Hauptwaffe. Systeme wiederverwenden.
- Den früher geplanten Sechs-Item-Soul-Echo-Proof nicht unverändert bauen;
  passende Effekte in Fähigkeiten-/Waffenfortschritt neu einordnen.
- Prolog-/Debug-Sektor-Retry ist keine Regel für reguläre Biom-Runs.
- Wiederholte Run-Niederlage führt zur Homebase. Ihre Lore-Erklärung gegenüber
  endgültiger Warden-Zerstörung bleibt auszuarbeiten.
- Bestehende TeamResonance ist nicht automatisch Geldbörse, Meta-Fortschritt
  und neue Ultimate-Ladung zugleich; Besitz-/Ausgaberegeln im Koop definieren.

### Offen, kein stillschweigender Beschluss

Ultimate-Aufladung/Kosten, Währungsnamen und genaue Ausgabenzuordnung,
Sicherungsquote/-limits, Geldverlust, Koop-Konten, Metalevel-Formel,
dauerhafte Fähigkeitsslots, Rüstungsanpassung, Biomthema/-anzahl,
Level-/Miniboss-/Waffenanzahl und genaue NPC-Freischaltfolge.
Heilung/Run-Händler und Sammlungsabschluss-Belohnungen sind Optionen.
1.000 Einheiten, 50 % Sicherung, drei Startwaffen und drei/fünf Levels sind
Beispiele. Das Meta-Level aus heimgebrachten Ressourcen ist eine Arbeitsrichtung.

Die nicht widersprechende Lore einschließlich Bruder-Koop, tragischer Souls,
Release, Flammenbedeutung, unbekanntem Keeper und Soulfire Gothic bleibt gültig.
Dieser Auftrag erstellt Planung; er genehmigt nicht rückwirkend offene
Spiel-/Grafikabnahmen und implementiert keine neuen Laufzeitsysteme.

## 2026-09-06 — Owner revision: the scythe is a held weapon, and the dash is a real out

### Reviewed

Camera, whole-body motion, weapons and VFX accepted. Two things named, then one
correction on the first.

### Revised

- **The scythe must read as a weapon someone is holding, not a prop attached to
  him.** First it was planted upright like a staff; corrected, it hung down at
  the floor like a farm tool; corrected again, it is now level across the body —
  a combat guard, seen side-on.
- **The chain keeps three hits**, choreographed: a sweep to the right, an answer
  back to the left, and a full turn on the third.
- **Every attack must resolve back into the hold**, not cut to it.
- **The dash interrupts any attack.** Waiting out a swing before being allowed to
  move was rejected outright.
- **Dash invulnerability must be visible.** The Warden goes "a little shadowy"
  for exactly as long as he cannot be hit.
- The Soul Cannon is finished.

### Implemented in response

- The pose now describes **where the scythe is**, and the hands are placed *on
  the haft*. Before this the hands and the weapon were posed independently,
  which is the reason the carry never flowed into a swing — they were never
  connected.
- The guard sits at about −56° from the aim, which is **exactly where the first
  hit winds up from**, so the hold is the start of the swing rather than a pose
  it has to leave.
- Three attack clips, each placing the hands on the overlay's haft using the
  runtime's own swing angle, so the Warden always holds the weapon being drawn
  for him. The third turns the whole body through a full revolution and arrives
  back where it started. All three ease their grip back onto the carry.
- The overlay is lifted to chest height so the blade and the hands meet; this is
  the one place the flat combat plane and the three-quarter character view are
  made to agree, and the constant is shared by the runtime and the forge.
- `ScytheCombat.CancelForDash` — a strike already created still lands, everything
  after it is abandoned, and the chain position is kept so dashing out of a swing
  and swinging again continues the combo.
- `Player.PhaseAmount` drives a darkening and thinning of the body that rises
  with the dash and falls with the i-frame window, so what is seen is exactly how
  long he is untouchable.

### Still open

Owner has not judged the guard, the chain choreography or the dash.

## 2026-09-06 — Owner revision: camera, whole-body motion, weapons and VFX

### Reviewed

The corrected character was accepted ("the character looks decent now"). Four
things were rejected as still below a professional bar.

### Revised

- **The camera must follow like a good action game does.** Children of Morta was
  named as the reference for *how the frame behaves*, not for its content. A
  camera that sits exactly on the player and tracks at a constant rate is not
  acceptable.
- **Animation must move the whole body.** Feet moving under a static torso reads
  as a puppet. Weight shift, counter-rotation and secondary motion are required.
- **The scythe and the Soul Cannon are not imposing enough.** They read as a farm
  tool and a pipe.
- **Every VFX is below standard.** Named example: the Burning's detonation "is
  like a square at the end". The complaint was explicitly general — all effects.
- **Enemy behaviour is not to change.** Only how it looks.

### Implemented in response

- **Camera rebuilt** (`Rendering/Camera2D.cs`): velocity and facing look-ahead
  with its own smoothing, a soft zone so small adjustments do not move the frame,
  critical damping instead of a fixed-rate lerp, vertical restraint, and a small
  bias toward the fight. Plus a zoom punch on impact.
- **Whole-body motion** in the rig: pelvis sway onto the stance leg, shoulder
  counter-rotation against the hips with the head overshooting late, arms
  crossing inboard, and planted feet excluded from all of it so nothing slides.
- **Weapons re-shaped**: a deep recurved blade with a back-spur, an iron collar
  holding one bound Soul, a counterweight spike and an S-curved haft; the Cannon
  became a braced reliquary with a caged chamber and a flared fluted mouth.
  Detail level unchanged — the weight comes from silhouette.
- **All twelve effect sheets re-authored** (`tools/visual-max/vfx_forge.py`) as
  effects rather than pictures: quantised energy fields, particles simulated once
  and sampled per frame so embers actually travel, shockwaves that thin as they
  expand, broken rather than perfectly circular rings, and an asserted coverage
  ceiling so nothing can become a square again.

### Measured

The old `fx_burning_detonation` covered 79–81% of its frame for three frames and
then spent nine frames as full-frame speckle — that is the square. The authored
one peaks at 11%. `fx_resonance_activate` went from 72% to 7%.

### Still open

Owner has not judged the camera, the reworked animation, the weapons or the VFX.

## 2026-09-06 — Owner revision: the character and the pixel language were wrong

### Revised

- **The protagonist is human first and supernatural second.** The winged,
  horned, over-accessorised reading is rejected. He is a person carrying a
  scythe who happens to be dead.
- **Detail is not quality.** Sheets that carry hundreds of near-identical
  colours and one-pixel ornament read as an over-rendered image pretending to be
  pixel art. Larger forms, stronger clusters, cleaner materials and fewer shiny
  surfaces are the standard.
- **Animation quality is a first-class requirement**, not something to be
  revisited after content. Walk smoothness, idle stability and readable attacks
  are acceptance criteria.
- **Facing must never look broken when the mouse moves.**
- This correction happens **before** any further content.

### Implemented in response

- `tools/visual-max/warden_forge.py` — the protagonist and his brother are now
  **authored from one rig** rather than generated per direction. One body plan,
  one palette, one camera, a real ground-plane projection and depth-sorted
  parts, so the eight directions are one character rotating.
- Character reset: wings, horns and the rifle/scythe hybrid removed; a visible
  face, hands and legs, a plain wood-and-iron scythe, one leather belt, one
  strap and a rust-red scarf. The entire supernatural budget is two ember eyes,
  a two-pixel bound Soul at the sternum, and a thin Death Flame line on the
  trailing hem.
- 24 colours in six material families; flat fills, hard three-value shading from
  one key light, no dithering, no anti-aliasing.
- Real 12-frame idle and run and a new 6-frame attack. **The run is driven by
  distance travelled, not by a clock**, so the gait is correct at every movement
  multiplier.
- Facing: bounded body turn rate, eight-sector selection with hysteresis, and
  reversed playback when backpedalling. `FacingDirection` remains the raw aim
  vector, so **no combat value changed**.
- `WardenDisplaySize = 128 / CombatCameraZoom`: one authored pixel is one screen
  pixel at combat zoom.
- The elder brother gets his own sheet from the same rig — hood, mantle, no
  scarf — resolving the Session 2 finding that the brothers separated only by
  mass and tint.

### Follow-on corrections found while implementing

- **The Soul Cannon still drew hard vector circles and a line in the fighting
  plane.** The 2026-09-06 "no hard lines in combat" revision had been applied to
  telegraphs and trails but missed the Cannon. Now painted with the feathered
  brush in the additive pass.
- **Threat telegraphs were drawn over the actors.** A Hollow's swipe band was
  painted across the Warden standing in it. Telegraphs are floor light and are
  now drawn beneath the actors. Same cues, same alpha, correct layer.
- **Actor light was over-driven.** It had been raised in Session 2 because the
  delivered sheet had no value of its own (median 22/255 against a 21/255
  floor). The authored sheet measures 56/255, so the Warden's silhouette light
  and bound-Soul core were roughly halved. Anything else washed the new art out.

### Still open

Owner has not judged the corrected character, animation or facing.

## 2026-09-06 — Owner revision: generated audio must not ship raw

### Revised

- **Raw ElevenLabs output is rejected.** Reviewed during Session 2 and described
  as sounding "raw and not professional" next to the existing bank.
- Generated audio is **source material only**. Every shipped cue must be produced
  locally: an already-approved sample carries the body, generated material sits
  underneath as texture, and the result must measure inside the existing bank's
  duration, level, brightness and noise-floor band.

### Implemented in response

`tools/audio/generate_soulfire_sfx.py` now requests seconds of headroom and
lossless PCM with short foley-brief prompts. `tools/audio/build_session2_sfx.py`
performs onset trimming, downward expansion, brightness matching, transient
shaping, explicit decay, layering and category peak normalisation. Six cues were
produced this way; ten existing cues were reviewed and deliberately left alone.

### Still open

The new cues have **not been auditioned by ear** — the agent cannot listen.
`artifacts/session2/audio-audition/` holds the shipped `hybrid` build and a
`bank-only` build for A/B. Owner approval required.

## 2026-09-06 — Session 2 direction (implemented, not yet approved)

### Proposed and implemented

- **Soulfire actor light grammar.** Living Death Flame light — violet-white —
  belongs to Wardens and Souls. Manifestations are separated by cold ash light
  only; their violet appears at the Anchor and the fractures. Introduced because
  measurement showed the Warden sheet and the casting floor sat at the same
  luminance, leaving the protagonist with no figure/ground separation at all.
- **Local co-op is a team, not two soloists.** Resonance is one shared pool;
  earning it credits the team, either brother may spend it, and spending it
  lights both. Residue feeds the same pool, so there is no pickup to race for.
- **The brothers share one Death Flame tether.** Separation is expressed as
  strain on that tether and a gradual draw-back, never a teleport, never
  split-screen.
- **Going down is not dying.** A Warden whose flame gutters while a brother
  stands cannot act, cannot be hit, and cannot be finished off. Both down ends
  the encounter. Solo death is unchanged.
- **Brother identity is temperature, tint and mass** over the same Warden sheet
  and the same kit — no second class.

### Still open

Owner has not judged the refined Golden Slice, the co-op proof, or the audio.

## 2026-09-06 — Owner revision: no hard lines in combat

### Revised

- **Combat feedback must be animation and VFX only.** Hard, shiny vector lines in
  the fighting plane are rejected: they read as interface laid over the pixel art
  and make the game feel inorganic.
- Applies to telegraphs, swing trails, auras, tethers, weak-point markers,
  detonations and death effects. Environment/prop linework and debug overlays
  (F1) are not affected.

### Implemented in response

All combat cues are painted with a feathered brush in a dedicated additive,
linear-filtered pass (`Rendering/SoftShapes.cs`, `GameWorld.DrawCombatLight`).
Telegraphs became gathering light instead of outlines. The arena centre pulse
ring and the aim crosshair were removed.

### Still open

Owner has not yet judged the Golden Combat Slice as a whole.

## 2026-09-05 — Current foundation

### Approved direction

- Vaelor is the First/Highest Warden, not a god.
- The Keeper is an unknown entity beyond the boundary.
- The Stillness is the exceptional communication path toward the true afterlife.
- Wardens are a rare new existence formed through mastery or resonance with the Death Flame.
- Wardens age extremely slowly and pass fully onward when their Warden form is destroyed.
- The story begins in the uncivilized Death Layer.
- In cooperative play, the second player is the protagonist's brother and is already a Warden.
- Children of Morta is the visual quality reference; Soulfire Gothic remains the original identity.
- Combat should reward well-timed reactions with interesting attacks or opportunities.
- The first milestone is a gold-standard encounter, followed immediately by a local co-op proof.

### Unresolved

- Exact metaphysical selection process for new Wardens.
- Precise nature and intent of the Keeper.
- Exact rules, limits and origin of the Stillness.
- Final form of Soul-based alternative attacks such as detonating residue.
- Final item taxonomy and long-term progression structure.
- Exact scale and generation method of later Death Layer regions.

Use [`CANON-STATUS.md`](CANON-STATUS.md) for the full Canon / Non-Canon / Unresolved classification.
