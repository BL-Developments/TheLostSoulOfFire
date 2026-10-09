## 1. Logik

- [ ] 1.1 `LevelRun` (aktueller Raum, geräumt, Ausgänge nehmen, Seed) und `RoomExit` (Position je Ausgangszahl) unter `Game/Levels/`; Balance-Werte `LevelPlaceholderPush`, `RoomExitInteractRadius`, `RoomTransitionDuration` und `LevelDefeatDelay` in `GameBalance`. Prüfen: Unit-Tests für Ausgang vor und nach der Räumung, Start und Levelende ohne Begegnung, Wahl des rechten Ausgangs führt in den verbundenen Raum, Ausgangspositionen bei ein und zwei Ausgängen.
- [ ] 1.2 `GamePhase.Level` und die Übergänge `StartLevel`, `LeaveLevelToHub` und `DefeatInLevel` in `GameFlowRules`. Prüfen: `GameFlowRules`-Tests.

## 2. Spiel

- [ ] 2.1 `GameWorld.Levels.cs`: Level starten (Seed, Layout, Run-Beginn, `LEVEL_SEED`), Raum betreten am Südtor, Platzhalter-Begegnung mit kurzer Ankündigung, Räumung, Ausgänge zeichnen und mit `E` nutzen, Blende und Raumwechsel mit erhaltenem Spielerzustand. Prüfen: Ein Level mit `--seed` lässt sich vom Startraum bis zum Levelende spielen.
- [ ] 2.2 `IsRunPhase` für Glut, Fähigkeiten, Kampf-HUD, `ActiveCombatBounds` und `ActiveWorldBounds`. Prüfen: Alle bestehenden Tests sowie `--currency-visual-test` und `--travel-visual-test` bleiben grün.
- [ ] 2.3 Levelende mit `LEVEL GESCHAFFT`: Der Ausgang sichert alles, speichert und führt in den Hub. Niederlage: Run-Bestände weg, kein `R`, nach `LevelDefeatDelay` in den Hub. Prüfen: Unit-Tests zu Beständen bei Levelende und Niederlage.
- [ ] 2.4 HUD-Zeile `RAUM <n>` mit `PixelText.Measure`. Prüfen: Aufnahme aus 3.2.

## 3. Developer-Start und Prüfung

- [ ] 3.1 `DeveloperStartOptions`: Bereich `level`, `--seed <ganze Zahl>`, Fehlermeldungen und `DEV_START area=level seed=<n>`; README ergänzen. Prüfen: Parser-Tests.
- [ ] 3.2 Automatischer Lauf `--level-visual-test` mit festem Seed, sodass eine Gabelung vorkommt: Er nimmt Startraum, Begegnung, geräumten Raum mit zwei Ausgängen, Blende, Levelende und Hub auf und beendet sich. Prüfen: Bilder liegen vor, Bestände werden in jedem Schritt geprüft.
- [ ] 3.3 `openspec validate add-level-rooms --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen. Lokaler Koop: NOT_RUN (#97).
- [ ] 3.4 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711`
