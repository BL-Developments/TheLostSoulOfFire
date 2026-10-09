## 1. Logik

- [ ] 1.1 `BiomeRun` und `BiomeDefinition` (Biom I) unter `Game/Levels/`; Balance-Werte `GuardianProgressBonus`, `GuardianExtraWaves` und `BiomeCompleteDuration`. Prüfen: Unit-Tests für alle erlaubten und unerlaubten Übergänge, Level-Seeds aus dem Run-Seed, Ablauf „Level 3 → Niederlage → Homebase → Start bei Level 1“ (#67).
- [ ] 1.2 `GameFlowRules`: `EnteringBiome`, Start des Biom-Runs und Weiterreise. Prüfen: `GameFlowRules`-Tests.

## 2. Spiel

- [ ] 2.1 `GameWorld.BiomeRun.cs`: Run starten, Level nacheinander erzeugen, Fortschritt und Spielerzustand beim Levelwechsel erhalten. Prüfen: Unit-Test, dass der erste Kampfraum von Level 2 den fortgesetzten Fortschritt hat.
- [ ] 2.2 Reisepunkt im Levelende von Level 1 und 2 sowie im Bereich `level`; das vorläufige Levelende aus `add-level-rooms` entfernen. Prüfen: Unit-Tests zu Teilsichern, Weiter und Extrahieren am Levelende; `--travel-visual-test` der Arena bleibt grün.
- [ ] 2.3 Wächterraum am Ende von Level 3 und Biomabschluss mit Sicherung und Rückkehr in den Hub. Prüfen: Unit-Tests zur Wächter-Begegnung (schwerer als der vorherige Raum) und zu den Beständen beim Abschluss.
- [ ] 2.4 Tür I startet den Biom-Run; Arena nur per Developer-Start. Prüfen: Hub-Tests angepasst; `--dev --start arena` unverändert.
- [ ] 2.5 HUD `BIOM I · LEVEL <n> · RAUM <m>`. Prüfen: Aufnahme aus 3.2.

## 3. Developer-Start und Prüfung

- [ ] 3.1 `DeveloperStartOptions`: `biome:1`, `--level 1..3`, `--seed` auch für Biome, Fehlermeldungen, `DEV_START`-Zeile; README. Prüfen: Parser-Tests.
- [ ] 3.2 Automatischer Lauf `--biome-visual-test` mit festem Seed: Level 1 bis zum Reisepunkt, Teilsichern, Level 2, Weiter, Level 3, Wächterraum, Biomabschluss, Hub; zweiter Lauf mit Niederlage in Level 2 und Neustart bei Level 1. Prüfen: Bestände in jedem Schritt geprüft, Aufnahmen liegen vor.
- [ ] 3.3 `openspec validate add-biome-run-flow --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen. Lokaler Koop: NOT_RUN (#97).
- [ ] 3.4 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start biome:1 --level 3 --seed 4711` für den Wächterraum oder `--dev --start hub` und Tür I für den ganzen Run.
