## 1. Raumfolge

- [ ] 1.1 `LevelRoomKind`, `LevelRoom`, `LevelLayout` und `LevelLayoutSettings` unter `Game/Levels/`; Balance-Werte `LevelCombatStagesMin`, `LevelCombatStagesMax` und `LevelForkChance` in `GameBalance`. Prüfen: Build ohne neue Warnungen.
- [ ] 1.2 `LevelLayoutGenerator.Generate(seed, settings)` nach design.md. Prüfen: Unit-Tests für Start und Levelende, ein bis zwei Räume je Stufe, höchstens zwei Ausgänge, Erreichbarkeit aller Räume, gleicher Seed gleiche Folge, Grenzen über 1000 Seeds, Gabelungen kommen vor, Fortschritt paralleler Räume.

## 2. Abschluss

- [ ] 2.1 Eintrag in `docs/current/DECISION-LOG.md`: zufällige, grob lineare Raumfolge mit parallelen Wegen, einzelne Räume wie bei Hades, Räume vorerst wie die Arena (Björn, 09.10.2026).
- [ ] 2.2 `openspec validate add-level-room-generation --strict`, `dotnet build` und `dotnet test` ohne Fehler und neue Warnungen.
- [ ] 2.3 Dieser Change ist noch nicht spielbar. Einen Startbefehl gibt es erst mit `add-level-rooms`; bis dahin bleibt `dotnet test` die Prüfung.
