## Context

`Program.cs` erkennt heute nur Testflags (`--audio-runtime-test`, `--antechamber-visual-test` usw.) per `Array.Exists`. `GameWorld` hat bereits alle Einstiege als private Methoden: `BeginPrologue`, `DebugEnterPrologueStage` (für `FindTrace`, `SearchApproach`, `DevourerPressure`, `Transit`), `BeginAntechamber` und `EnterArena`. Die Arena beginnt mit einem Intro, nach dem `SpawnWave(_waveNumber + 1)` die erste Welle startet. `GameFlowRules` hält die erlaubten Phasenübergänge als reine Funktionen.

## Goals / Non-Goals

**Goals:**

- Ein Befehl pro Bereich, der direkt dort startet, wo ein Feature geprüft werden soll.
- Fehler beim Aufruf sind sofort sichtbar und erklären die gültigen Werte.
- Der Parser ist ohne MonoGame testbar.
- Die Einstiege nutzen dieselben Methoden wie der reguläre Ablauf, damit ein Dev-Start keinen eigenen Spielzustand erzeugt.

**Non-Goals:**

- Debug-Tasten neu ordnen oder hinter `--dev` sperren.
- Ein eigener Sandbox- oder Testraum.
- Bereiche, die es noch nicht gibt (Biom-Level, Boss). Sie kommen mit den jeweiligen Changes dazu.
- Persistente Dev-Einstellungen oder ein In-Game-Menü dafür.

## Decisions

### Parser als reine Klasse

`DeveloperStartOptions.TryParse(string[] args, out options, out error)` unter `Debugging/` liefert entweder Optionen (`Area`, `Wave`) oder eine Fehlermeldung. `Program.cs` gibt bei Fehler die Meldung und die Bereichsliste auf `stderr` aus und beendet mit Exitcode `2`, ohne ein Fenster zu öffnen. Unbekannte Argumente, die nicht zum Dev-Mode gehören, bleiben erlaubt, damit bestehende Flags und spätere Erweiterungen nicht brechen.

Alternative: `System.CommandLine` als Abhängigkeit. Verworfen, weil drei Optionen keine neue Bibliothek rechtfertigen.

### Bereichsnamen

Kleingeschrieben und englisch wie die Prolog-Stufen im Code: `title`, `prologue`, `prologue:find-trace`, `prologue:search`, `prologue:devourer`, `prologue:transit`, `hub`, `arena`. Der Doppelpunkt gruppiert Unterbereiche; Biom-Level können später als `biome:1` folgen. Groß-/Kleinschreibung wird ignoriert.

### Einstieg nach dem Laden

`Game1` erhält die Optionen und ruft nach dem Erzeugen von `GameWorld` in `LoadContent` `GameWorld.ApplyDeveloperStart(options, viewport)` auf. `GameWorld` ordnet jedem Bereich den vorhandenen Einstieg zu:

- `title`: nichts, das Hauptmenü erscheint wie gewohnt.
- `prologue`: `BeginPrologue`.
- `prologue:<abschnitt>`: `BeginPrologue`, danach `DebugEnterPrologueStage` mit der passenden Stufe.
- `hub`: `BeginAntechamber`.
- `arena`: wie das Ende der Tür-Sequenz; die Arena-Initialisierung wird dafür aus `EnterArena` in eine gemeinsame Methode gezogen. Bei `--wave n` wird der Wellenzähler auf `n - 1` gesetzt, sodass das reguläre Intro direkt Welle `n` startet.

Damit bleibt der reguläre Ablauf nach dem Einstieg unverändert: Tod und `R` setzen die Arena zurück, das Ende führt zum Titel.

### Keine Kombination mit Testflags

`--dev` zusammen mit einem bestehenden Testflag ist ein Fehler, weil die automatisierten Tests ihren eigenen Ablauf steuern.

### Startbefehl als Teil jedes Changes

`openspec/config.yaml` erhält eine Regel für `tasks`: Die letzte Aufgabe nennt den Startbefehl, mit dem sich der Change anspielen lässt. Die README listet alle Bereiche.

## Risks / Trade-offs

- [Ein Dev-Einstieg weicht unbemerkt vom regulären Einstieg ab] → Einstiege rufen dieselben Methoden auf; die Arena-Initialisierung wird geteilt statt kopiert.
- [Prolog-Abschnitte setzen Zustand aus früheren Abschnitten voraus] → `DebugEnterPrologueStage` wird bereits für `D1`–`D4` genutzt; der Dev-Start ruft vorher `BeginPrologue` auf, wie beim Drücken der Taste im laufenden Prolog.
- [Spätere Wellen ohne vorher aufgebaute Resonanz sind schwerer] → Bewusst so; `F5` füllt die Resonanz bei Bedarf.
