## Context

- **Grundlage:** `add-level-room-generation` liefert `LevelLayout` mit Stufen, Räumen, Ausgängen und Fortschritt.
- **Arena heute:** Sie bringt alles mit, was ein Raum braucht: Grenzen (`GameBalance.ArenaBounds`, `CombatBounds`), gemalte Ebenen, Props, Südtor, Kamera, Intro sowie `ArenaWaveRun` mit Ankündigung und Nachschub.
- **Bewegung:** Spieler und Gegner werden in `CombatBounds` geklemmt.
- **Beschluss vom 09.10.2026 (Björn):**
  - einzelne Räume wie bei Hades;
  - Räume vorerst wie die Arena;
  - parallele Wege als Wahl zwischen zwei Ausgängen.

## Goals / Non-Goals

**Goals:**
- Ein Level von Startraum bis Levelende spielbar, mit Wegwahl.
- Möglichst viel aus der Arena wiederverwenden, nichts an der Arena selbst ändern.
- Reine Zustandslogik (`LevelRun`) für Raumwechsel, Räumung und Ausgänge, testbar ohne `GraphicsDevice`.

**Non-Goals:**
- Wachsende Wellen (`add-room-wave-scaling`).
- Drei Level, Reisepunkte, Wächterraum, Tür I (`add-biome-run-flow`).
- Eigene Grafik und Graubox-Palette (`add-level-visual-slots`).
- Kisten in Räumen, verschiedene Raumgrößen oder Hindernisse.

## Decisions

### Der Raum ist die Arena

Jeder Raum nutzt die Instanz `_arena` für Grenzen und Zeichnen.

Die Platzhalter-Begegnung ist ein Schub mit der Zusammensetzung von Arena-Welle 2. Sie kommt aus `GameBalance.LevelPlaceholderPush` und wird über `ArenaWaveRun` mit `ArenaWaves.ChooseSpawnPositions` abgespielt. Die Wellen-Pause der Arena (Intermission, Auslösezone in der Mitte) gibt es im Level nicht.

Unterscheidbar wird ein Raum erst durch `add-level-visual-slots`. Bis dahin ist das bewusst ein Arena-Klon.

### `LevelRun` als reiner Zustand

`LevelRun` hält das Layout, den aktuellen Raum, `IsCleared` und den Seed:
- `EnterRoom(id)` prüft, ob der Raum ein Ausgang des aktuellen Raums ist.
- `MarkCleared()` markiert den aktuellen Raum als geräumt.
- `TryTakeExit(index, out next)` nimmt einen Ausgang nur, wenn der Raum geräumt ist oder keine Begegnung hat.

`GameWorld.Levels.cs` übersetzt das in Spawns, Blende, Kamera und HUD.

### Ausgänge in der Nordwand

`RoomExit` liegt bei einem Ausgang mittig in der Nordwand, bei zwei Ausgängen bei 30 % und 70 % der Breite.
- Ein geschlossener Ausgang wird als dunkles Gitter gezeichnet, ein offener mit kaltem Licht. Beides sind Platzhalterformen.
- `E` in `GameBalance.RoomExitInteractRadius` startet die Blende. Die Reihenfolge der Interaktion bleibt: Kiste, Reisepunkt, Ausgang.

### Phase und Übergänge

`GamePhase.Level` kommt neu dazu. `GameFlowRules` bekommt drei Übergänge:
- `StartLevel`: Hub oder Dev-Start → Level;
- `LeaveLevelToHub`: Level → Antechamber;
- `DefeatInLevel`: Level → Antechamber.

`AllowsCombat` umfasst `Level`. `ActiveCombatBounds` und `ActiveWorldBounds` liefern im Level die Arena-Grenzen.

Glutgutschrift, Fähigkeiten und HUD fragen eine kleine Eigenschaft `IsRunPhase` ab (Arena oder Level).

### Niederlage

Der vorhandene Todeszustand läuft ab. Danach geht es ohne `R`-Neuversuch nach `GameBalance.LevelDefeatDelay` in den Hub, und `LoseRunCurrencies` wird aufgerufen.

Die Bergungsinszenierung bleibt offen (#68). `add-biome-run-flow` kann sie später ergänzen.

### Seed

- Ohne `--seed` wird der Seed aus `Environment.TickCount` gewürfelt.
- Mit `--seed` wird der Wert genommen.
- Beides wird als `LEVEL_SEED <n>` über den vorhandenen Konsolenweg des Dev-Starts ausgegeben.

## Risks / Trade-offs

- **Räume sehen alle gleich aus:** Das ist bewusst so, bis `add-level-visual-slots` kommt. Wegwahl ist darum vorerst nur über den HUD-Fortschritt und die Gegner spürbar.
- **Mehr Phasenabfragen in `GameWorld`:** Eine gemeinsame Eigenschaft `IsRunPhase` hält das klein; die neue Logik liegt in `GameWorld.Levels.cs`.
- **Levelende sichert alles:** Das ist ein Platzhalter für den Reisepunkt. Er wird mit `add-biome-run-flow` ersetzt, damit sich niemand an „Level = Extraktion“ gewöhnt.
