## Why

Hinter Tür I liegt bisher nur die Arena: ein einzelnes Rechteck mit zehn Wellen. Die beschlossenen Regeln verlangen etwas anderes:
- Ein Biom hat drei Level, und der Boss steht am Ende von Level 3 (#52, Beschluss 04.10.2026).
- Reisepunkte stehen am Ende von Level 1 und 2 (#53).
- Nach einer Niederlage geht es zurück in die Homebase, und der nächste Versuch beginnt bei Level 1 (#14, #68).

Im Code gibt es dafür kein Level-Modell, keine Räume, keine Wände und keinen Levelwechsel (#66). Ohne diesen Aufbau kann weder ein Biom-Brief (#10) noch ein Boss (#92) gespielt werden. Der Aufbau hängt nicht vom Thema des Bioms ab und kann deshalb jetzt entstehen.

## What Changes

- **Level aus Räumen:**
  - Ein Level ist eine handgebaute, deterministische Folge verbundener Räume.
  - Jeder Raum ist ein Rechteck mit Raumtyp nach `WORLD-GRAMMAR.md` §6.
  - Räume sind über Durchgänge verbunden. Außerhalb der Räume und Durchgänge liegt Wand; Spieler und Gegner kommen dort nicht hin.
  - Die Kamera folgt dem Spieler innerhalb der Levelgrenzen.
- **Raum-Begegnungen:**
  - Ein Kampfraum startet seine Begegnung, sobald der Spieler ihn betritt, und schließt dann seine Durchgänge.
  - Die Gegner kommen in Schüben wie in der Arena.
  - Ist der Raum geräumt, öffnen sich die Durchgänge wieder.
  - Gegner bleiben in ihrem Raum.
- **Biom-Run:**
  - Ein Biom ist eine geordnete Liste von drei Leveln.
  - Ein klarer Run-Zustand ersetzt die Arena-Sonderfälle: Homebase, laufendes Level, Levelende, Extraktion, Niederlage, Biom abgeschlossen.
  - Am Ende von Level 1 und 2 steht ein Reisepunkt mit den drei Entscheidungen aus #53; danach folgt das nächste Level.
  - Level 3 endet in einem Wächterraum. Bis es den Boss gibt (#92), steht dort eine verstärkte Begegnung.
  - Wer sie räumt, schließt das Biom ab: Beide Run-Bestände werden gesichert, und es geht zurück in den Hub.
- **Niederlage:** Der Tod in einem Biom-Level führt nach einer kurzen Bergungsblende in den Hub. Die Run-Bestände gehen verloren, und der nächste Versuch beginnt bei Level 1. Die genaue Bergungsinszenierung bleibt offen (#68).
- **Biom I als Graubox:**
  - Drei themenneutrale Level mit den vorhandenen Gegnern Hollow, Burning und Devourer.
  - Thema, Bossfigur und endgültige Raumfolge legt später der Biom-Brief fest (#10).
- **Austauschbare Grafik:**
  - Jedes Level hat einen Visual-ID-Slot für seine gemalte Bodenebene und einen für das Grading des Bioms.
  - Solange es dafür keine Grafik gibt (Status `dummy`), zeichnet das Spiel die Räume als Graubox in der Platzhalterpalette des Bioms.
  - Die Palette unterscheidet sich klar von Hub und Prolog und macht die Raumtypen erkennbar.
  - Später ersetzen PNG und Registry-Eintrag die Graubox, ohne dass sich Code ändert.
- **Tür I** startet den Biom-I-Run statt der Arena (Entscheidung Björn, 09.10.2026). Die Arena mit ihrem Reisepunkt nach Welle 5 bleibt unverändert über `--dev --start arena` spielbar.
- **Developer-Start:** `--dev --start biome:1` startet den Biom-I-Run bei Level 1, mit optionalem `--level <n>` direkt in Level `n`. Abschluss, Extraktion und Niederlage führen wie im regulären Run in den Hub.

## Capabilities

### New Capabilities

- `biome-levels`: Aufbau eines Levels aus Räumen, Durchgängen und Wänden, Raumtypen, Raum-Begegnungen, Kamera im Level und Grafik-Slots mit Graubox.
- `biome-run-flow`: Run-Zustände eines Biom-Runs, Levelfolge, Reisepunkte am Levelende, Wächterraum, Biomabschluss und Niederlage mit Rückkehr in den Hub.

### Modified Capabilities

- `hub-biome-doors`: Tür I startet den Biom-I-Run statt der Arena.
- `developer-start-mode`: neuer Bereich `biome:<n>` mit `--level`.
- `run-currencies`: Der Start eines Biom-Runs beginnt einen neuen Run. Der Biomabschluss sichert den ganzen Run-Bestand wie der Arena-Abschluss, und Kisten stehen in Belohnungsräumen.

## Impact

- **Neuer Code** unter `src/TheLostSoulOfFire/Game/Levels/`:
  - `LevelDefinition`, `RoomDefinition`, `BiomeDefinition` und der Katalog für Biom I;
  - Begehbarkeit und Kollision;
  - Raum-Begegnung;
  - `BiomeRun` als reine Zustandslogik.
- **Anbindung** in `GameWorld.BiomeRun.cs`. In `GameWorld.cs` ändern sich nur die Stellen, die heute fest `GamePhase.Arena` oder `_arena` voraussetzen: Grenzen, Kamera, Gutschrift von Glut, Kisten und Reisepunkt.
- **Zeichnen:** neuer `Rendering/LevelGreyboxRenderer`; Registry-Einträge sind nicht nötig, nur Visual-IDs und Visual-Specs mit `Status: dummy`.
- **Weitere Dateien:** `GameFlowRules`, `DeveloperStartOptions`, `GameBalance` (Begegnungen, Wächter-Platzhalter) und Tests.
- **Nicht enthalten:**
  - Boss und Soul-Release-Phase (#92), Freischaltung von Tür II (#93), Biomwahl im Hub (#62);
  - NPCs (#85), Collectibles (#72), Koop (#97);
  - gemalte Grafik (`add-visual-vertical-slice`) und zufällige Raumfolgen.
