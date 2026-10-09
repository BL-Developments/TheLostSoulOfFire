Voraussetzungen: `add-level-rooms`, `add-room-wave-scaling` und `add-arena-travel-point` sind gemergt und archiviert. Ein Biom-Run läuft wie ein Level in der Phase `Arena`; es kommt keine neue `GamePhase` dazu.

## 1. Logik ohne MonoGame

- [ ] 1.1 Neue Datei `Game/Levels/BiomeDefinition.cs`:
  - `public sealed record BiomeDefinition(int Number, string Numeral, int LevelCount, LevelLayoutSettings Layout)`;
  - `public static class BiomeCatalog` mit `public static BiomeDefinition One { get; } = new(1, "I", 3, LevelLayoutSettings.Default)` und `TryGet(int number, out BiomeDefinition biome)`.

  Prüfen: Build.
- [ ] 1.2 Neue Datei `Game/Levels/BiomeRun.cs`: `public enum BiomeRunState { Homebase, InLevel, LevelEnd, Extracted, Defeated, BiomeComplete }` und `public sealed class BiomeRun`.
  - Properties: `BiomeDefinition Biome`, `int Level`, `int RunSeed`, `BiomeRunState State`, `int CombatRoomsEntered`.
  - Methoden:
    - `Start(BiomeDefinition biome, int runSeed, int level = 1)`, erlaubt aus Homebase, Extracted, Defeated und BiomeComplete;
    - `bool ReachLevelEnd()`, `bool TravelOn()` (nur aus LevelEnd und nur, wenn `Level < LevelCount`; erhöht `Level`), `bool Extract()`, `bool Defeat()`, `bool CompleteBiome()` (nur in `InLevel` des letzten Levels) und `bool ReturnHome()`;
    - `int LevelSeed => unchecked(RunSeed * 7919 + Level)`.

  Prüfen: 1.3.
- [ ] 1.3 Tests `tests/TheLostSoulOfFire.Tests/BiomeRunTests.cs`:
  - je ein Test pro erlaubtem Übergang;
  - `ReachLevelEnd_InHomebase_IsIgnored`, `TravelOn_FromLastLevel_IsRefused` und `CompleteBiome_BeforeLastLevel_IsRefused`;
  - `Defeat_InLevelThree_ThenStart_BeginsAtLevelOne` (#67);
  - `LevelSeed_SameRunSeed_SameSequence`.

  Prüfen: `dotnet test`.
- [ ] 1.4 Balance-Werte in `GameBalance`: `GuardianProgressBonus = 2`, `GuardianExtraWaves = 1` und `BiomeCompleteDuration = 4.5f`. Prüfen: Build.

## 2. Anbindung

- [ ] 2.1 In `GameWorld.Levels.cs`:
  - Feld `BiomeRun? _biomeRun`.
  - `internal void StartBiomeRun(int? seed, int level, Viewport viewport)` startet `_biomeRun`, erzeugt das Layout mit `LevelLayoutGenerator.Generate(_biomeRun.LevelSeed, _biomeRun.Biome.Layout)` und startet das Level wie `StartLevel`, aber ohne neuen Run, wenn `level > 1` per Weiterreise erreicht wird.
  - Der Fortschritt `CombatRoomsEntered` wird beim Levelwechsel von `LevelRun` an `BiomeRun` übergeben und dem neuen `LevelRun` als Startwert gesetzt (Konstruktorparameter `int combatRoomsEntered = 0`).
  - `ClearRunState` setzt `_biomeRun = null`.

  Prüfen: Unit-Test `CombatRoomsEntered_CarriesIntoNextLevel` in `LevelRunTests`.
- [ ] 2.2 Levelende von Level 1 und 2 und im Bereich `level`: Statt des vorläufigen Ausgangs aus `add-level-rooms` wird ein `TravelPoint` am Ort des Ausgangs aufgestellt; die Position kommt aus `RoomExit.Position(…, 1, 0)`.
  - In `GameWorld.TravelPoint.cs`, `ApplyTravelChoice` (der Block mit `case TravelChoice.Extract`): Bei `InLevel` führen Teilsichern und Weiter zu `_biomeRun.TravelOn()` und zum Start des nächsten Levels mit erhaltenem Spielerzustand, statt eine Welle zu starten.
  - Extrahieren bleibt wie es ist und ruft zusätzlich `_biomeRun?.Extract()` auf.
  - Im Bereich `level` ohne Biom führt jede Weiterreise in den Hub (vorher sichern bzw. teilsichern wie gewählt).
  - Den Code des vorläufigen Levelendes löschen.

  Prüfen: Tests für die drei Entscheidungen am Levelende; `--travel-visual-test` bleibt grün.
- [ ] 2.3 Wächterraum: Im letzten Level ist der Raum `LevelEnd` ein Kampfraum mit `RoomEncounterPlan.For(CombatRoomsEntered + GuardianProgressBonus, RoomSeed)` und `GuardianExtraWaves` zusätzlichen Wellen. Die Wellenzahl wird dabei über `RoomWavesMax` hinaus erlaubt; dafür bekommt `For` einen Parameter `int extraWaves = 0`, mit Test.
  - Nach der Räumung folgen `_biomeRun.CompleteBiome()`, `SecureRunCurrencies()` und die Anzeige des Biomabschlusses (wie `DrawSecuredSummary` mit dem Präfix `BIOM I ABGESCHLOSSEN`).
  - Nach `BiomeCompleteDuration` folgen `ReturnHome()`, `ExtractToHub`, `BeginAntechamber` und `ClearRunState`.

  Prüfen: Test, dass der Wächterplan mehr Wellen und schwere Gegner hat als der vorherige Raum.
- [ ] 2.4 Niederlage: Der Ablauf aus `add-level-rooms` ruft zusätzlich `_biomeRun?.Defeat()` auf. Prüfen: 3.2.
- [ ] 2.5 Tür I: In `GameWorld.cs` ruft `UpdateDoorTransition` nach der Sequenz `StartBiomeRun(null, 1, viewport)` statt `EnterArena(viewport)` auf. `EnterArena` bleibt für den Developer-Start der Arena, falls noch genutzt; sonst löschen. Hub-Tests in `AntechamberFlowTests.cs` anpassen. Prüfen: `dotnet test`; `--dev --start arena` unverändert; `--antechamber-visual-test` grün.
- [ ] 2.6 HUD: `HudRenderer.DrawRoom` bekommt einen optionalen Präfix. Im Biom-Run zeigt es `BIOM I · LEVEL <n> · RAUM <m>`, wobei die Breite mit `PixelText.MeasureFace` ermittelt wird. Prüfen: Aufnahme aus 3.2.

## 3. Developer-Start und Prüfung

- [ ] 3.1 `DeveloperStartOptions`:
  - Bereich `biome:1` (`DeveloperStartArea.Biome` mit Feld `int Biome`); unbekannte Biome sind ein unbekannter Bereich;
  - `--level 1..3` nur mit `biome:<n>`; `--seed` auch für `biome:<n>`;
  - `Describe()` liefert `DEV_START area=biome:1 level=<n>`, mit Seed zusätzlich ` seed=<n>`.

  `ApplyDeveloperStart` ruft `StartBiomeRun(options.Seed, options.Level, viewport)` auf. README ergänzen. Parser-Tests nach vorhandenem Muster. Prüfen: `dotnet test`.
- [ ] 3.2 `--biome-visual-test`:
  - Lauf 1 mit festem Seed: Level 1 bis zum Reisepunkt, Teilsichern, Level 2, Weiter ohne Sichern, Level 3, Wächterraum, Biomabschluss, Hub.
  - Lauf 2: Niederlage in Level 2, Hub, Tür I, Start in Level 1.
  - Bestände, Fortschritt und Level werden nach jedem Schritt geprüft; Aufnahmen per `ScreenshotCapture`; Ausgabe `BIOME_VISUAL_TEST_PASS`/`_FAIL`.

  Prüfen: PASS.
- [ ] 3.3 `openspec validate add-biome-run-flow --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen. Lokaler Koop: NOT_RUN (#97).
- [ ] 3.4 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start biome:1 --level 3 --seed 4711` für den Wächterraum oder `--dev --start hub` und Tür I für den ganzen Run.
