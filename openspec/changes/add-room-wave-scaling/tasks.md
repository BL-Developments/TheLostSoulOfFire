## 1. Berechnung

- [ ] 1.1 Balance-Werte aus design.md in `GameBalance`; `LevelPlaceholderPush` entfernen. Prüfen: Build ohne neue Warnungen.
- [ ] 1.2 `RoomEncounterPlan.For(progress, seed)` mit Wellen als `ArenaPush`-Listen. Prüfen: Unit-Tests für nur Hollow bei Fortschritt 1, keine Devourer unter der Schwelle, Monotonie von Fortschritt 1 bis 40, Obergrenzen, gleicher Seed gleicher Plan, Anzahlen je Typ unabhängig vom Seed und mindestens ein Hollow je Welle; Übersicht für Fortschritt 1 bis 18 in der Testausgabe.

## 2. Spiel

- [ ] 2.1 `RoomEncounter` mit Wellenfolge und `RoomWavePause` auf Basis von `ArenaWaveRun`. Prüfen: Unit-Test, dass die Ausgänge erst nach der letzten Welle aufgehen.
- [ ] 2.2 `LevelRun.CombatRoomsEntered` und Raum-Seed aus Level-Seed und Raum-Id; Anbindung in `GameWorld.Levels.cs` statt der Platzhalter-Welle. Prüfen: Unit-Test, dass der gewählte Raum einer Gabelung den erwarteten Fortschritt hat.
- [ ] 2.3 `--level-visual-test` um eine Aufnahme eines späten Raums mit mehreren Wellen und schweren Gegnern erweitern. Prüfen: Die Aufnahme zeigt Burning bzw. Devourer.

## 3. Abschluss

- [ ] 3.1 `openspec validate add-room-wave-scaling --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 3.2 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start level --seed 4711 --armor 99` und mehrere Räume durchlaufen.
