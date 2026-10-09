Lies vor dem Start `design.md`, vor allem die Tabelle „Ein Level läuft in der Phase `Arena`“. Ein Level bekommt **keine** neue `GamePhase`.

## 1. Logik ohne MonoGame

- [ ] 1.1 Balance-Werte in `Game/GameBalance.cs` im Abschnitt `// Levels`:
  - `LevelPlaceholderPush = new ArenaPush(2, 2, 0)`;
  - `RoomExitInteractRadius = 70f`;
  - `RoomTransitionDuration = 0.6f`;
  - `LevelDefeatDelay = 2.5f`.

  Prüfen: Build.
- [ ] 1.2 Neue Datei `Game/Levels/LevelRun.cs`: `public sealed class LevelRun`.
  - Konstruktor `LevelRun(LevelLayout layout)`.
  - Properties: `LevelLayout Layout`, `LevelRoom Current` (startet mit `layout.Start`), `bool IsCleared` (für Start und Levelende sofort `true`), `int Seed => Layout.Seed`.
  - Methoden:
    - `void MarkCleared()`;
    - `bool TryTakeExit(int exitIndex, out LevelRoom next)` mit Erfolg nur bei `IsCleared` und gültigem Index; setzt `Current` und `IsCleared` für den neuen Raum;
    - `bool HasEncounter => Current.Kind == LevelRoomKind.Combat`.

  Prüfen: 1.4.
- [ ] 1.3 Neue Datei `Game/Levels/RoomExit.cs`: `public static class RoomExit`.
  - `public static Vector2 Position(Rectangle combatBounds, int exitCount, int exitIndex)`: ein Ausgang mittig, zwei bei 30 % und 70 % der Breite, jeweils 40 Einheiten unter `combatBounds.Top`.
  - `public static int? InReach(Rectangle combatBounds, int exitCount, Vector2 player)` liefert den Index des Ausgangs in `RoomExitInteractRadius`.

  Prüfen: 1.4.
- [ ] 1.4 Tests `tests/TheLostSoulOfFire.Tests/LevelRunTests.cs`:
  - `TryTakeExit_BeforeClear_Refuses`
  - `TryTakeExit_AfterClear_EntersConnectedRoom`
  - `TryTakeExit_RightExitOfFork_EntersSecondRoom`
  - `StartAndLevelEnd_AreClearedOnEntry`
  - `Position_TwoExits_LeftAndRightOfCentre`
  - `InReach_OutsideRadius_ReturnsNull`

  Layouts kommen aus `LevelLayoutGenerator.Generate` mit festen Seeds; für eine Gabelung einen Seed suchen, der eine liefert, und im Test als Konstante festhalten. Prüfen: `dotnet test`.
- [ ] 1.5 `GameFlowRules.ReturnToHubAfterDefeat(GamePhase phase)` (Arena → Antechamber, sonst unverändert) mit Test in einer neuen Testmethode `ReturnToHubAfterDefeat_FromArena_GoesToAntechamber`. Prüfen: `dotnet test`.

## 2. Anbindung im Spiel

- [ ] 2.1 Neue Datei `Game/GameWorld.Levels.cs` (`public sealed partial class GameWorld`):
  - Feld `LevelRun? _levelRun` und Eigenschaft `bool InLevel => _levelRun is not null`.
  - `internal void StartLevel(int? seed, Viewport viewport)`:
    1. `ClearRunState()`;
    2. Seed festlegen (`seed ?? Environment.TickCount`);
    3. `Console.WriteLine($"LEVEL_SEED {seed}")`;
    4. Layout erzeugen und `_levelRun` setzen;
    5. `_phase = GamePhase.Arena`;
    6. `BeginArenaIntro(viewport)`.
  - `ClearRunState` setzt `_levelRun = null`.

  Prüfen: Spiel startet mit `--dev --start level` (nach 3.1).
- [ ] 2.2 `ClearRoomState()` in `GameWorld.Levels.cs`: leert Gegner, Seelen, Geschosse, `_pendingSpawns`, Partikel, Sprite-Effekte, Bodentreffer und `_waveRun = ArenaWaveRun.Empty`. Spieler, Fähigkeiten und Wallet bleiben. Prüfen: 2.6.
- [ ] 2.3 Weichen in `UpdateArenaLoop` (`GameWorld.cs`):
  - `Intro`: Bei `InLevel` und Raum ohne Begegnung geht es direkt nach `Intermission`, sonst zu `SpawnRoomWave()`. `SpawnRoomWave` arbeitet wie `SpawnWave`, aber mit `new ArenaWaveRun([GameBalance.LevelPlaceholderPush])` und Spawns über `ArenaWaves.ChooseSpawnPositions`.
  - `Combat` geräumt: Bei `InLevel` wird `_levelRun.MarkCleared()` aufgerufen und nach `Intermission` gewechselt; `SpawnChestAfterWave` und `SpawnTravelPointAfterWave` werden nicht aufgerufen. `_waveNumber` bleibt im Level 0.
  - `Transition`: Bei `InLevel` folgt nach `RoomTransitionDuration` der Raumwechsel aus 2.4 statt `SpawnWave`.

  Prüfen: Die Arena per `--dev --start arena` verhält sich unverändert, und `--currency-visual-test` und `--travel-visual-test` bleiben grün.
- [ ] 2.4 Raumwechsel: In `UpdateCurrency` (`GameWorld.Currency.cs`) gilt bei `InLevel` in `Intermission` die `E`-Reihenfolge Kiste, Reisepunkt, Ausgang (`RoomExit.InReach`); die Auslösezone in der Mitte gibt es im Level nicht.
  - Ein Ausgang in Reichweite ruft `TryTakeExit`, `_loopState = ArenaLoopState.Transition` und `_presentation.BeginWaveTransition()` auf.
  - Nach der Blende folgen `ClearRoomState()`, `_player.PlaceAt(Südtor)` (Mitte der Unterkante der Kampffläche, 90 Einheiten darüber) und `_loopState = ArenaLoopState.Intro` mit `_presentation.BeginIntro(true)`.
  - **Nicht** `_player.Reset` verwenden: Es setzt `Health = MaxHealth`. Stattdessen in `Entities/Player.cs` eine Methode `PlaceAt(Vector2 position)` ergänzen, die nur Position, Geschwindigkeit, Dash und Angriffszustand zurücksetzt, Gesundheit und Fähigkeiten aber nicht. Unit-Test `PlaceAt_KeepsHealth` in einer passenden Testdatei.

  Prüfen: Unit-Test oder 3.2 zeigt gleiche Gesundheit vor und nach dem Wechsel.
- [ ] 2.5 Levelende: Im Raum `LevelEnd` zeigt der Raum `LEVEL GESCHAFFT` (mit `DrawCenteredPrompt` aus `GameWorld.Currency.cs`). `E` am Ausgang ruft `SecureRunCurrencies()`, `_phase = GameFlowRules.ExtractToHub(_phase)`, `BeginAntechamber(viewport)` und `ClearRunState()` auf. Prüfen: Unit-Test oder 3.2 mit Beständen vor und nach.
- [ ] 2.6 Niederlage: `RetryCurrentEncounter` kehrt bei `InLevel` sofort zurück (kein `R`). Im Update-Zweig `if (_player.IsDead)` in `GameWorld.cs` folgen bei `InLevel` nach `LevelDefeatDelay` Sekunden Todeszustand `LoseRunCurrencies()`, `_phase = GameFlowRules.ReturnToHubAfterDefeat(_phase)`, `BeginAntechamber(viewport)` und `ClearRunState()`. Prüfen: 3.2.
- [ ] 2.7 Zeichnen: In `DrawArenaLoop` werden bei `InLevel` statt der Auslösezone die Ausgänge mit `ShapeRenderer`-Formen gezeichnet, geschlossen als dunkles Gitter und offen mit kaltem Licht. In `DrawHud` (`GameWorld.cs`) folgt bei `InLevel` statt `HudRenderer.DrawWave` eine neue Methode `HudRenderer.DrawRoom(batch, pixel, viewport, progress)`, die wie `DrawWave` ein Schild mit `RAUM` und Zahl zeichnet. Prüfen: Aufnahmen aus 3.2.

## 3. Developer-Start und Prüfung

- [ ] 3.1 `Debugging/DeveloperStartOptions.cs`:
  - `DeveloperStartArea.Level` und Eintrag `("level", DeveloperStartArea.Level)`;
  - neues Feld `int? Seed` im Record;
  - `--seed <ganze Zahl>` nur mit `level`;
  - Fehlermeldungen wie bei `--wave`;
  - `Describe()` liefert `DEV_START area=level seed=<n>` bzw. `DEV_START area=level`.

  `GameWorld.ApplyDeveloperStart` ruft für `Level` die Methode `StartLevel(options.Seed, viewport)` auf. README-Bereichsliste ergänzen. Tests in `DeveloperStartOptionsTests.cs` nach dem vorhandenen Muster: gültiger Start, mit Seed, Seed ohne Level, Seed keine Zahl. Prüfen: `dotnet test`.
- [ ] 3.2 Automatischer Lauf `--level-visual-test` nach dem Muster von `--travel-visual-test` (`Program.cs` Flag, `Game1.ConfigureTravelVisualTest` als Vorlage):
  - Er startet `StartLevel(Seed mit Gabelung)`, räumt Räume über `DefeatAutomatedEnemies` und nimmt per `ScreenshotCapture` diese Bilder auf: Startraum, laufende Begegnung, geräumter Raum mit zwei Ausgängen, Levelende und Hub.
  - Ein zweiter Lauf stirbt in Raum 1 und landet im Hub.
  - Er prüft Bestände und Gesundheit nach jedem Schritt und gibt `LEVEL_VISUAL_TEST_PASS` bzw. `_FAIL` aus.
  - `--dev` zusammen mit dem Testflag bleibt verboten (Liste in `DeveloperStartOptions`).

  Prüfen: Der Lauf endet mit PASS.
- [ ] 3.3 `openspec validate add-level-rooms --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen. Lokaler Koop: NOT_RUN (#97).
- [ ] 3.4 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711`
