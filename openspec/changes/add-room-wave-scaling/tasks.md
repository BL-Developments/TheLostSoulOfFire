Voraussetzung: `add-level-rooms` ist gemergt. Lies `design.md`, Abschnitt „Formel mit Obergrenzen“.

## 1. Berechnung ohne MonoGame

- [ ] 1.1 Balance-Werte aus der Tabelle in design.md im Abschnitt `// Levels` von `Game/GameBalance.cs` anlegen; `LevelPlaceholderPush` entfernen. Prüfen: Build (Fehler an den bisherigen Verwendungen zeigen, wo 2.2 ansetzt).
- [ ] 1.2 Neue Datei `Game/Levels/RoomEncounterPlan.cs`:
  - `public sealed record RoomEncounterPlan(IReadOnlyList<ArenaPush> Waves)` mit `static RoomEncounterPlan For(int progress, int seed)`.
  - Anzahlen je Typ nach der Formel in design.md, nur aus `progress`.
  - Danach verteilt `Random(seed)` die schweren Gegner reihum auf zufällig gewählte Wellen, jede Welle behält mindestens einen Hollow.
  - Eigenschaften `int TotalEnemies`, `int HeavyEnemies` und `int Devourers` für Tests.

  Prüfen: 1.3.
- [ ] 1.3 Tests `tests/TheLostSoulOfFire.Tests/RoomEncounterPlanTests.cs`:
  - `For_ProgressOne_OnlyHollow`
  - `For_BelowDevourerThreshold_NoDevourer`
  - `For_ProgressOneToForty_NeverDecreases` (Wellen, Gegner, schwere Gegner)
  - `For_HighProgress_RespectsMaxima`
  - `For_SameSeed_SamePlan`
  - `For_DifferentSeeds_SameCountsPerKind`
  - `For_AnyProgress_EveryWaveHasHollow`
  - `For_ProgressOneToEighteen_PrintsOverview`, das mit `TestContext.WriteLine` eine Tabelle Fortschritt / Wellen / Gegner / Burning / Devourer schreibt.

  Prüfen: `dotnet test`.

## 2. Anbindung

- [ ] 2.1 `LevelRun` (`Game/Levels/LevelRun.cs`) um `int CombatRoomsEntered` erweitern. Der Wert erhöht sich in `TryTakeExit`, wenn der neue Raum `Combat` ist. Dazu kommt `int RoomSeed => unchecked(Layout.Seed * 31 + Current.Id)`. Tests in `LevelRunTests`: `CombatRoomsEntered_CountsOnlyCombatRooms` und `CombatRoomsEntered_ForkChoice_SameProgress`. Prüfen: `dotnet test`.
- [ ] 2.2 In `GameWorld.Levels.cs`:
  - Felder `RoomEncounterPlan? _roomPlan`, `int _roomWaveIndex` und `float _roomWavePause`.
  - Beim Betreten eines Kampfraums: `_roomPlan = RoomEncounterPlan.For(_levelRun.CombatRoomsEntered, _levelRun.RoomSeed)` und `_roomWaveIndex = 0`.
  - `SpawnRoomWave()` nutzt `new ArenaWaveRun([_roomPlan.Waves[_roomWaveIndex]])`.
  - Im Fall „geräumt“ von `UpdateArenaLoop`: Ist noch eine Welle übrig, wird `_roomWaveIndex` erhöht und `_roomWavePause = GameBalance.RoomWavePause` gesetzt; nach Ablauf folgt `SpawnRoomWave()`, und der Zustand bleibt `Combat`. Erst nach der letzten Welle geht es weiter wie bisher (`MarkCleared`, `Intermission`).
  - Die Pause wird in `UpdateArenaLoop` heruntergezählt, ohne Allokation.

  Prüfen: Die Ausgänge öffnen sich erst nach der letzten Welle (3.1).
- [ ] 2.3 Die HUD-Methode `HudRenderer.DrawRoom` zeigt unter `RAUM <n>` eine Raute je Welle des Raums, erleuchtet ab Beginn der Welle, wie die Schub-Rauten in `DrawWave`. Prüfen: Aufnahme aus 3.1.

## 3. Prüfung

- [ ] 3.1 `--level-visual-test` um einen Lauf erweitern, der mit festem Seed bis zu einem Raum mit Fortschritt ≥ 4 räumt und eine Aufnahme mit Burning und Devourer sowie den Wellen-Rauten macht. Er prüft, dass die Ausgänge zwischen zwei Wellen geschlossen bleiben. Prüfen: PASS.
- [ ] 3.2 `openspec validate add-room-wave-scaling --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 3.3 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711 --armor 99`
