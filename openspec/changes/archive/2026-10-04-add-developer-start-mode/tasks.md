## 1. Parameter

- [x] 1.1 `DeveloperStartOptions` mit `TryParse` für `--dev`, `--start` und `--wave` anlegen, inklusive Bereichsliste und Fehlermeldungen
- [x] 1.2 `Program.cs` wertet die Optionen aus, bricht bei Fehlern mit Meldung auf `stderr` und Exitcode `2` ab und lehnt `--dev` zusammen mit Testflags ab
- [x] 1.3 Unit-Tests für gültige Bereiche, Groß-/Kleinschreibung, Default-Welle, Fehlerfälle und Start ohne `--dev`

## 2. Einstiege

- [x] 2.1 Arena-Initialisierung aus `EnterArena` in eine gemeinsame Methode ziehen, ohne den regulären Ablauf zu ändern
- [x] 2.2 `GameWorld.ApplyDeveloperStart` ordnet jeden Bereich dem vorhandenen Einstieg zu; `--wave n` lässt das Intro in Welle `n` übergehen
- [x] 2.3 `Game1` übergibt die Optionen nach dem Laden und schreibt die `DEV_START`-Zeile

## 3. Dokumentation

- [x] 3.1 README um einen Abschnitt Developer-Mode mit allen Bereichen ergänzen
- [x] 3.2 `openspec/config.yaml` um die Regel ergänzen, dass die letzte Aufgabe jedes Changes den Startbefehl nennt

## 4. Verifikation

- [x] 4.1 Build und alle Tests grün
- [x] 4.2 Jeden Bereich einmal im echten Spiel starten und prüfen; Startbefehl zum Anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --wave 3`
