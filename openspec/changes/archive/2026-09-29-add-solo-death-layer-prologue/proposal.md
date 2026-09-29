## Why

Nach dem Menü landet der Spieler heute direkt in der Aschenvorhalle und dann in der Arena, ohne dass irgendwo erzählt oder gelehrt wird, wer der Warden ist, was Soul Sense und Soul Release bedeuten und wie die drei Gegnertypen zu lesen sind. Ein spielbarer Prolog stellt diesen Einstieg her. Er lag bereits als Golden Slice auf `prototype/design-polish` (ohne das Menü und die Aschenvorhalle von `main`) und wird hier für den Einzelspielerbetrieb übernommen.

Diese Änderung entstand nachträglich zur Umsetzung in PR #37 und dokumentiert deren Umfang.

## What Changes

- Zwischen Hauptmenü und Aschenvorhalle einen spielbaren Prolog einführen: Titel → Prolog → Aschenvorhalle → Arena.
- Der Prolog besteht aus vier Abschnitten: `DAS UNVOLLENDETE UFER`, `DER SUCHGANG`, `DIE LETZTE ÜBERFAHRT` und `DIE WARDEN-SCHWELLE` (im Spiel englisch beschriftet).
- Der Prolog führt Soul Sense, Hollow, Burning, den Devourer mit gehaltener Seele, Soul Release sowie eine 62 Sekunden lange Verteidigung auf dem Todesflammen-Skiff ein.
- Ziele und kurze Erzählzeilen erscheinen als Bildschirmtext; Abschnittstitel blenden beim Betreten ein.
- Scheitern startet den aktuellen Abschnitt neu, nicht den ganzen Prolog.
- Der Prolog ist ausschließlich Einzelspieler. Der Bruder als Begleiter und der lokale Koop des Ursprungsbranches sind **nicht** enthalten.
- Automatisierte Prüfläufe überspringen den Prolog.
- Entwicklerhilfen: `D1`–`D4` springen zu Prolog-Checkpoints, `F8` oder `R` starten den Abschnitt neu.

## Capabilities

### New Capabilities

- `death-layer-prologue`: Der spielbare Einzelspieler-Prolog mit seinen vier Abschnitten, Lehrmomenten, Abschnittswiederholung und Übergang in die Aschenvorhalle.

### Modified Capabilities

- `main-menu`: `NEUES SPIEL` beginnt den Prolog statt unmittelbar den Arenadurchlauf; automatisierte Läufe umgehen den Prolog.

## Impact

- Betrifft `GameWorld` (neue Spielphase, Ablauf in `GameWorld.Prologue.cs`), `GameFlowRules` und die Übergabe der Prüflauf-Optionen in `Game1`.
- Neue Dateien: `Game/PrologueDirector`, `Rendering/PrologueEnvironment`, `Rendering/ProloguePresentation`.
- `Devourer` erhält einen Weg, mit bereits gehaltener Seele zu beginnen; `ShapeRenderer` eine gefüllte Ellipse; `SoulfireLighting` lässt sich ohne die Arena-Ofenlichter zeichnen.
- Führt keine neuen externen Abhängigkeiten, Netzwerkdienste oder persistenten Daten ein.
