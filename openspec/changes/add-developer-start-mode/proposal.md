## Why

Um ein neues Feature anzuspielen, muss man heute Titel, Prolog und Hub durchlaufen. Die vorhandenen Abkürzungen (Debug-Tasten `D1`–`D4` im Prolog, `F6` zum Räumen einer Welle, die Testflags in `Program.cs`) helfen erst, wenn man schon im richtigen Bereich ist. #44 wünscht einen Developer-Mode, der per `dotnet run`-Parameter direkt in einem gewählten Bereich startet. Er ist die Grundlage, um die folgenden Leveldesign-Changes für Biom I schnell zu prüfen.

## What Changes

- Neuer Startparameter `--dev`. Zusammen mit `--start <bereich>` beginnt das Spiel direkt im gewählten Bereich: `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub` oder `arena`.
- Für `arena` wählt `--wave <1-4>` die erste Welle; Default ist Welle 1.
- Ungültige Kombinationen (unbekannter Bereich, `--wave` ohne `arena`, `--start` ohne `--dev`, Kombination mit den bestehenden Testflags) brechen mit verständlicher Meldung, Liste der gültigen Bereiche und Exitcode `2` ab, statt still im Titel zu landen.
- Beim Dev-Start schreibt das Spiel eine Zeile `DEV_START ...` mit dem Bereich auf die Konsole.
- Ohne `--dev` startet das Spiel unverändert. Bestehende Testflags und Debug-Tasten bleiben unverändert.
- README und `openspec/config.yaml` halten fest, dass jeder umgesetzte Change den passenden Startbefehl nennt.

## Capabilities

### New Capabilities

- `developer-start-mode`: Startparameter, mit denen Entwickler direkt in Titel, Prolog-Abschnitte, Hub oder eine Arena-Welle springen, und deren Fehlerbehandlung.

### Modified Capabilities

Keine. Die regulären Abläufe aus `main-menu`, `death-layer-prologue` und `soul-furnace-antechamber` bleiben unverändert.

## Impact

Betrifft `Program.cs` (Parsen), einen neuen kleinen Parser unter `Debugging/`, `Game1` (Übergabe nach dem Laden) und `GameWorld` (Einstiegsmethode, die vorhandene Übergänge wiederverwendet). Keine neuen Assets, Abhängigkeiten oder Speicherstände. Spätere Changes (Biom-Level, Bosse) erweitern die Bereichsliste.
