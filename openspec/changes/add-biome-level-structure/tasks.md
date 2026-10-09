## 1. Level-Modell

- [ ] 1.1 `Game/Levels/`: `RoomKind`, `RoomDefinition`, `DoorwayDefinition`, `LevelDefinition` (Grenzen aus den Räumen), `BiomeDefinition` mit Platzhalterpalette und Visual-IDs. Prüfen: Build ohne neue Warnungen.
- [ ] 1.2 `LevelDefinitionValidator`: Jeder Raum ist vom Eingang erreichbar; genau ein Eingang und genau ein Levelende oder Wächter; Kampfräume haben mindestens einen Schub; Durchgänge sind breiter als der größte Gegner und reichen in beide Räume hinein. Prüfen: Unit-Tests mit kaputten Definitionen schlagen mit Level- und Raumnamen fehl.
- [ ] 1.3 `LevelWalkArea.Resolve` mit Gleiten (voll, nur X, nur Y, sonst alte Position) und geschlossenen Durchgängen. Prüfen: Unit-Tests für Gleiten an Wand und Ecke, Durchgang offen und geschlossen, Dash gegen die Wand.
- [ ] 1.4 `RoomEncounter` auf Basis von `ArenaWaveRun` und `ArenaWaves.ChooseSpawnPositions` mit Raumgrenzen: Start erst, wenn der Spieler ganz im Raum steht; Durchgänge zu bis geräumt; geräumt bleibt geräumt. Prüfen: Unit-Tests für Start im Durchgang, Start im Raum, Spawns im Raum mit Mindestabstand, Öffnen nach dem letzten Gegner.

## 2. Biom-Run

- [ ] 2.1 `BiomeRun` mit den Zuständen Homebase, InLevel, LevelEnd, Extracted, Defeated und BiomeComplete; unerlaubte Übergänge bleiben wirkungslos. Prüfen: Unit-Tests für jeden erlaubten Übergang, unerlaubte Übergänge und den Ablauf „Level 3 → Niederlage → Homebase → Start bei Level 1“ (#67).
- [ ] 2.2 `GamePhase.BiomeLevel`; `EnteringArena` wird zu `EnteringBiome`; `GameFlowRules` für Tür I, Weiterreise, Extraktion, Abschluss und Niederlage. Prüfen: `GameFlowRules`-Tests angepasst und grün.
- [ ] 2.3 `BiomeCatalog` mit Biom I: drei Graubox-Level nach design.md, Begegnungen und Wächter-Platzhalter als Werte in `GameBalance`. Prüfen: Validator aus 1.2 über alle Level grün; jeder Raumtyp kommt mindestens einmal vor.

## 3. Anbindung im Spiel

- [ ] 3.1 `GameWorld.BiomeRun.cs`: Level aufbauen, Spieler am Eingang, Begehbarkeit für den Spieler, Gegner auf ihren Raum geklemmt, Kamera an den Levelgrenzen, Begegnungen starten und räumen. Prüfen: Biom I Level 1 lässt sich vom Eingang bis zum Levelende durchspielen.
- [ ] 3.2 Phasenabfragen in `GameWorld.cs`, `GameWorld.Currency.cs` und `GameWorld.Abilities.cs` auf Arena und Biom-Level erweitern (Glut, Kisten, Fähigkeiten, HUD, `ActiveCombatBounds`, `ActiveWorldBounds`). Prüfen: bestehende Tests und `--currency-visual-test` bleiben grün.
- [ ] 3.3 Reisepunkt im Raum Levelende von Level 1 und 2 mit dem vorhandenen Menü; Weiterreise ins nächste Level mit erhaltenen Beständen, Gesundheit und Fähigkeiten; Extraktion in den Hub. Prüfen: Unit-Tests zur Weiterreise; `--travel-visual-test` für die Arena bleibt grün.
- [ ] 3.4 Kiste in Belohnungsräumen, die beim Verlassen des Levels verfällt. Prüfen: Unit-Test für einmalige Gutschrift und Verfall.
- [ ] 3.5 Wächterraum: Abschluss sichert alles, zeigt den Biomabschluss und führt in den Hub. Niederlage: Bergungsblende (`GameBalance.RecoveryFadeDuration`), Run-Bestände verloren, Hub, kein `R`-Neuversuch. Prüfen: Unit-Tests zu Beständen bei Abschluss und Niederlage.
- [ ] 3.6 Tür I startet über die Eintrittssequenz Biom I Level 1; die Arena ist nur noch per Developer-Start erreichbar. Prüfen: Hub-Tests angepasst; Arena per `--dev --start arena` unverändert.
- [ ] 3.7 HUD-Zeile `BIOM I · LEVEL <n>` mit `PixelText.Measure`. Prüfen: Aufnahme aus 4.2.

## 4. Graubox und Grafik-Slots

- [ ] 4.1 Visual-IDs `environment.biome1-level1` bis `-level3` und `grade.biome1` in `VisualIds`, Visual-Specs unter `art/specs/` mit `Status: dummy`. Graubox im neuen `LevelGreyboxRenderer`: Wände, Böden, offene und geschlossene Durchgänge, Raumtyp-Markierung. Gemalte Bodenebene statt Graubox, sobald die Registry sie liefert; Grading `grade.biome1`, ohne LUT neutral. Prüfen: Registry-Tests (3.5 aus `add-visual-vertical-slice`) grün; ein Test mit einer Test-Registry zeigt, dass eine eingetragene Bodenebene die Graubox ersetzt.
- [ ] 4.2 Automatischer Lauf `--biome-visual-test`: Er nimmt in jedem Level Eingang, einen geschlossenen Kampfraum, Levelende oder Wächter sowie die HUD-Zeile auf und beendet sich. Prüfen: Bilder liegen vor; Wände, Durchgänge und Raumtypen sind ohne Text erkennbar.

## 5. Developer-Start und Abschluss

- [ ] 5.1 `DeveloperStartOptions`: Bereich `biome:1`, `--level 1..3`, Fehlermeldungen und `DEV_START area=biome:1 level=<n>`; README-Bereichsliste ergänzen. Prüfen: Parser-Tests für gültige und ungültige Kombinationen.
- [ ] 5.2 `openspec validate add-biome-level-structure --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 5.3 Lokaler Koop: NOT_RUN, weil es auf dieser Code-Basis keinen Koop gibt (#97).
- [ ] 5.4 Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start biome:1 --level 1` (oder `--level 3` für den Wächterraum); der reguläre Weg führt über `--dev --start hub` und Tür I.
