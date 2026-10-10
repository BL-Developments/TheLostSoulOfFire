## 1. Charakterwerte

- [x] 1.1 `PlayerAttributes` um Tempo, Glück, Kernschärfe, Einklang, Gewandtheit, Fokus und Standfestigkeit mit Faktoren, `ScaleCoreDamage` und `ScaleReward` erweitern; per Unit-Tests (`PlayerAttributesTests`) abnehmen.
- [x] 1.2 Tempo in `ScytheCombat` (Schwungzeit) und `SoulCannon` (Ladezeit) anwenden.
- [x] 1.3 Kernschärfe bei Kerntreffern von Sense und Kanone in `GameWorld` anwenden.
- [x] 1.4 Einklang, Gewandtheit und Standfestigkeit in `Player`, Fokus in `RunAbilities` anwenden; per `PlayerAttributeEffectsTests` abnehmen.
- [x] 1.5 Glück auf Glut aus besiegten Gegnern und Geld aus Kisten in `GameWorld.Currency` anwenden.

## 2. Anzeige und Sandbox

- [x] 2.1 `CharacterSheet` um die Wirkungszeilen erweitern und die Charakterseite in zwei Spalten zeichnen; per `CharacterMenuTests` abnehmen.
- [x] 2.2 Dev-Menü der Sandbox um die neuen Werte erweitern und die Zeilen so verdichten, dass alle Einträge auf den Bildschirm passen; per `SandboxCharacterValueTests` und `DevMenuTests` abnehmen.

- [x] 2.3 `DeveloperStartOptions` um die Parameter der neuen Werte erweitern und die README ergänzen; per `DeveloperStartOptionsTests` abnehmen.

## 3. Gesamtabnahme

- [x] 3.1 `dotnet test` erfolgreich ausführen.
- [x] 3.2 Charaktermenü und Dev-Menü per `--tour-visual-test` aufnehmen und prüfen.
- [ ] 3.3 Manuell anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start sandbox`, im Dev-Menü (`F`) Tempo, Gewandtheit und Standfestigkeit erhöhen und gegen die Trainingspuppe und einen Hollow vergleichen; für Glück `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --luck 30` und die Glut pro Gegner beobachten (Hollow 6 statt 3).
