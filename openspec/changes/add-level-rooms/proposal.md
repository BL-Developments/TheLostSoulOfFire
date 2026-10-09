## Why

`add-level-room-generation` erzeugt die Raumfolge eines Levels, spielen kann man sie aber noch nicht. Dieser Change macht ein Level spielbar: einzelne Räume, die wie die Arena aufgebaut sind, mit Ausgängen, zwischen denen man wählt (Entscheidung Björn, 09.10.2026).

## What Changes

- **Neuer Spielbereich Level.**
  - Beim Start wird ein Seed gewürfelt und daraus die Raumfolge erzeugt.
  - Der Spieler betritt einen Raum nach dem anderen.
  - Ein Raum hat die Grenzen, die Kampffläche und das Aussehen der Arena.
- **Startraum:** Er ist ohne Gegner und hat seine Ausgänge sofort offen.
- **Kampfraum:**
  - Die Begegnung startet kurz nach dem Betreten.
  - Vorerst ist es eine feste Platzhalter-Welle, `add-room-wave-scaling` ersetzt sie.
  - Die Ausgänge bleiben zu, bis der Raum geräumt ist.
- **Ausgänge:**
  - Ein geräumter Raum öffnet ein oder zwei Ausgänge in der Nordwand.
  - `E` an einem Ausgang führt mit kurzer Blende in den nächsten Raum.
  - Bei zwei Ausgängen wählt der Spieler den Weg.
  - Gesundheit, Run-Bestände und Fähigkeiten bleiben dabei erhalten.
- **Levelende:**
  - Der Raum ist ohne Gegner und zeigt `LEVEL GESCHAFFT`.
  - Sein Ausgang führt vorerst in den Hub und sichert dabei den ganzen Run-Bestand, wie der Arena-Abschluss.
  - Reisepunkte ersetzen das in `add-biome-run-flow`.
- **Niederlage:** Der Tod in einem Raum führt nach dem Todeszustand in den Hub, und beide Run-Bestände gehen verloren.
- **HUD:** Es zeigt `RAUM <n>` mit dem Fortschritt des Raums.
- **Developer-Start:** `--dev --start level` startet ein Level, `--seed <n>` legt die Raumfolge fest. Jeder Levelstart schreibt seinen Seed in die Konsole.
- **Tür I** führt weiter in die Arena; das ändert erst `add-biome-run-flow`.

## Capabilities

### New Capabilities

- `level-rooms`: Ablauf eines Levels aus einzelnen Räumen mit Begegnung, Ausgängen, Wegwahl, Raumwechsel, Levelende und Niederlage.

### Modified Capabilities

- `developer-start-mode`: neuer Bereich `level` mit `--seed`.

## Impact

- **Neuer Code:** `GameWorld.Levels.cs`, `LevelRun` (reiner Zustand: aktueller Raum, geräumt, gewählter Ausgang) und `RoomExit` unter `Game/Levels/`.
- **Geänderte Stellen:** Ein Level läuft in der vorhandenen Phase `Arena` mit aktivem `LevelRun`. Geändert werden nur die Weichen im Arena-Ablauf (`UpdateArenaLoop`, `UpdateCurrency`, `DrawArenaLoop`, Wellenanzeige im HUD), `RetryCurrentEncounter` und `GameFlowRules.ReturnToHubAfterDefeat`.
- **Wiederverwendet:** `Arena` (Geometrie und Zeichnen) und `ArenaWaveRun`.
- **Developer-Start:** `DeveloperStartOptions` und README.
- **Abhängig von:** `add-level-room-generation`.
