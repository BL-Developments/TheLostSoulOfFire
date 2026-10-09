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

### Ein Level läuft in der Phase `Arena`

Ein Level bekommt keine eigene `GamePhase`. Es läuft in `GamePhase.Arena`, solange ein `LevelRun` aktiv ist; die Eigenschaft heißt `InLevel => _levelRun is not null`.

So bleibt alles, was die Arena schon kann, ohne Änderung erhalten: Grenzen, Kamera, Glutgutschrift, Fähigkeiten, Kampf-HUD, Musik und Todeszustand. In `GameWorld` gibt es über 60 Abfragen auf `GamePhase.Arena`; eine neue Phase müsste jede davon anfassen.

Der Arena-Ablauf (`ArenaLoopState`) wird im Level so genutzt:

| Zustand | Arena heute | Im Level |
|---|---|---|
| `Intro` | Arena-Intro, danach `SpawnWave(1)` | Raum betreten (kurzes Intro mit `BeginIntro(true)`), danach `SpawnRoomWave()`; Start- und Levelende-Raum gehen direkt nach `Intermission` |
| `Combat` | Welle läuft, Nachschub über `UpdateReinforcements` | gleich |
| `Combat` geräumt | `Intermission` mit Kiste, Reisepunkt, Auslösezone | `MarkCleared()` und `Intermission` mit offenen Ausgängen, ohne Kiste, Reisepunkt und Auslösezone |
| `Intermission` | `E` in der Mitte startet die nächste Welle | `E` an einem Ausgang startet den Raumwechsel |
| `Transition` | Wellenübergang | Blende des Raumwechsels; danach `EnterRoom(next)` und `Intro` |
| `Complete` | Abschluss nach Welle 10 | wird im Level nicht genutzt |

Die Weichen sitzen an vier Stellen:
- `UpdateArenaLoop` (Fall „geräumt“ und die Fälle `Intro` und `Transition`);
- `UpdateCurrency` (`E`-Reihenfolge Kiste, Reisepunkt, Ausgang statt Auslösezone);
- `DrawArenaLoop` (Ausgänge statt Auslösezone zeichnen);
- `HudRenderer.DrawWave` (im Level stattdessen `RAUM <n>`).

`ClearRunState` setzt `_levelRun = null`. Für den Raumwechsel gibt es ein eigenes `ClearRoomState()`, das nur Gegner, Seelen, Geschosse, Effekte und `_waveRun` leert und Spielerzustand, Fähigkeiten und Bestände behält.

`GameFlowRules` bekommt nur `ReturnToHubAfterDefeat(GamePhase)` (Arena → Antechamber). Für das Levelende wird das vorhandene `ExtractToHub` genutzt.

### Niederlage

Der vorhandene Todeszustand läuft ab. Im Level ignoriert `RetryCurrentEncounter` die Taste `R` (früh zurückkehren, wenn `InLevel`). Nach `GameBalance.LevelDefeatDelay` Sekunden Todeszustand wird `LoseRunCurrencies` aufgerufen; danach folgen `_phase = GameFlowRules.ReturnToHubAfterDefeat(_phase)` und `BeginAntechamber(viewport)`.

Die Bergungsinszenierung bleibt offen (#68). `add-biome-run-flow` kann sie später ergänzen.

### Seed

- Ohne `--seed` wird der Seed aus `Environment.TickCount` gewürfelt.
- Mit `--seed` wird der Wert genommen.
- Beides wird als `LEVEL_SEED <n>` über den vorhandenen Konsolenweg des Dev-Starts ausgegeben.

## Risks / Trade-offs

- **Räume sehen alle gleich aus:** Das ist bewusst so, bis `add-level-visual-slots` kommt. Wegwahl ist darum vorerst nur über den HUD-Fortschritt und die Gegner spürbar.
- **Level und Arena teilen eine Phase:** Jede Weiche im Arena-Ablauf muss `InLevel` beachten, sonst erscheinen im Level Kisten, Reisepunkt oder Auslösezone der Arena. Die Weichen sind auf die vier Stellen oben begrenzt, und `--level-visual-test` prüft, dass im Level keine Auslösezone erscheint.
- **Levelende sichert alles:** Das ist ein Platzhalter für den Reisepunkt. Er wird mit `add-biome-run-flow` ersetzt, damit sich niemand an „Level = Extraktion“ gewöhnt.
