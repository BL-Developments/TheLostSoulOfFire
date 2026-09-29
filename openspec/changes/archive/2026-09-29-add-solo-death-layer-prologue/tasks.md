## 1. Prolog-Grundlagen aus `prototype/design-polish` übernehmen

- [x] 1.1 `Game/PrologueDirector`, `Rendering/PrologueEnvironment` und `Rendering/ProloguePresentation` übernehmen; Bruder-Stufen und Bruder-Erzählzeilen entfernen, das Titel-Overlay und die Soft-Light-Funktion weglassen. Nachweis: `dotnet build` ohne Fehler.
- [x] 1.2 `ShapeRenderer.FillEllipse` ergänzen, weil die Umgebung sie nutzt. Nachweis: `dotnet build` ohne Fehler.
- [x] 1.3 `Devourer.SeedHeldSoul` ergänzen. Nachweis: Test `SeededDevourerHoldsItsSoulUntilAFullCannonExpelsIt`.

## 2. Spielphase und Ablauf

- [x] 2.1 `GamePhase.Prologue` einführen und `GameFlowRules` um `FinishPrologue` sowie den Parameter `skipPrologue` erweitern; `AllowsCombat` schließt den Prolog ein. Nachweis: Tests in `PrologueFlowTests` und angepasste `AntechamberFlowTests`.
- [x] 2.2 In `GameWorld` Grenzen, Kameraschleife, Hintergrund, Kampf-HUD, Zeichenreihenfolge und Bildschirmtext phasenabhängig wählen, ohne den Arena-Ablauf zu verändern. Nachweis: bestehende Tests grün; Sichtprüfung per Laufzeit-Screenshot.
- [x] 2.3 Den Erzählablauf in `GameWorld.Prologue.cs` umsetzen: Abschnittswechsel, Lehrkämpfe, Devourer-Abschnitt, Überfahrt, Schwelle, Übergang in die Aschenvorhalle. Nachweis: Lauf unter virtuellem Display durch Menü, Ufer, Suchgang-Sprung, Devourer-Abschnitt und Überfahrt ohne Ausnahmen.
- [x] 2.4 Abschnittswiederholung mit `R` und `F8` sowie Checkpoint-Sprünge `D1`–`D4` ergänzen. Nachweis: Sprünge im Laufzeit-Lauf ausgelöst.
- [x] 2.5 Arena-Ofenlichter im Prolog ausschalten. Nachweis: Laufzeit-Screenshot ohne fremde Lichtflecken.

## 3. Einbindung und Absicherung

- [x] 3.1 Menü und Titelstart führen in den Prolog; `Game1` reicht `skipPrologue` an automatisierte Läufe. Nachweis: `dotnet build`, Tests grün.
- [x] 3.2 Tests für Phasenfolge, Sektorzuordnung, Startpunkte innerhalb der Grenzen, Ziele je Stufe, Timer und Devourer mit Seele ergänzen. Nachweis: 46 Tests grün, davon 10 neu.
- [x] 3.3 CI auf dem PR grün. Nachweis: Build und Test auf Ubuntu und Windows, CodeQL und Dependency Review erfolgreich (PR #37, gemergt).
