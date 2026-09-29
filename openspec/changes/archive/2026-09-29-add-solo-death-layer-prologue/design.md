## Context

Siehe `proposal.md` für die Motivation. Ausgangslage auf `main` nach PR #35: `GameWorld` führt die Phasen `Title → Antechamber → EnteringArena → Arena`, der Kampf läuft im Arena-Zweig von `GameWorld.Update`. Der Prolog existierte als Golden Slice auf `prototype/design-polish`, das vor Hauptmenü, Vollbild und Aschenvorhalle abzweigt und in acht Kerndateien konfliktet (`Game1`, `GameWorld`, `Player`, `InputState`, `Program`, `AudioDirector`, `CinematicPresentation`, `ScreenshotCapture`). Ein Gesamtmerge des Branches kam daher nicht in Frage.

## Goals / Non-Goals

**Goals:**

- Die vier Prolog-Abschnitte, Lehrkämpfe, Skiff-Überfahrt und Schwelle auf `main` spielbar machen.
- Den bestehenden Kampf-Update und die Gegner unverändert wiederverwenden.
- Build und Tests bleiben grün; automatisierte Läufe bleiben unbeeinflusst.

**Non-Goals:**

- Der Bruder als Begleiter und der lokale Koop (`WardenRoster`, Eingabeschicht `IPlayerInputSource`/`PlayerCommand`/`WardenField`). Vorgesehen für einen Folge-PR.
- Die Prolog-Ambience-Loops und die `AudioDirector`-Soundscapes (drei Schleifen, Content-Einträge).
- Der Soft-Light-Pass (`SoftShapes`) der Prolog-Umgebung.
- Neubalancierung der Überfahrt ohne Begleiter.
- Aufräumen des ungenutzten Altpfads (`HubScene`, `RaidScene`, `SceneManager`).

## Decisions

### Eigene Spielphase `Prologue`, aber derselbe Kampf-Update wie `Arena`

`GamePhase` erhält den Wert `Prologue`. Der Kampfteil von `GameWorld.Update` und `Draw` läuft für `Arena` und `Prologue` durch denselben Code; nur Grenzen, Schleifensteuerung, Hintergrund und Overlay werden per Phase gewählt (`ActiveCombatBounds`, `ActiveWorldBounds`, `UpdateLoop`).

Erwogene Alternativen:

- **Kampf-Update in eine eigene Methode extrahieren und zweimal aufrufen.** Verworfen: Ein großer Umbau des bestehenden Updates, dessen Nutzen erst mit einem dritten Aufrufer entsteht.
- **Kampf-Update für den Prolog duplizieren.** Verworfen: Doppelte Pflege für Gegner-, Seelen-, Cannon- und Audiologik.
- **Prolog als `ArenaLoopState`-Werte.** Verworfen aus demselben Grund wie beim Menü: `ArenaLoopState` wird an vielen Stellen ausgewertet und ein Prolog ist kein Arenazustand.

### Erzählablauf in einer eigenen Teilklasse

Die Prolog-Logik liegt in `GameWorld.Prologue.cs` (`partial class`). `PrologueDirector` hält nur Stage, Sektor, Zeiten, Ziele und Koordinaten; Gegner, Seelen und Kampf bleiben in `GameWorld`. Das hält `GameWorld.cs` lesbar und die Erzählschritte an einer Stelle.

### Einstiegspunkt über `GameFlowRules`

`ConfirmTitle` führt jetzt in den Prolog, `FinishPrologue` in die Aschenvorhalle. Ein Parameter `skipPrologue` bewahrt den direkten Weg für die automatisierten Läufe. Die Übergänge bleiben damit ohne MonoGame-Dienste testbar.

### Bruder-Stufen entfernt, nicht ausgeblendet

Die Stufen `FindBrother` und `BrotherMeeting` sowie alle Bruder-Erzählzeilen wurden aus `PrologueDirector` entfernt statt bedingt übersprungen. Der Folge-PR führt sie mit dem Koop-Unterbau wieder ein.

### Arena-Ofenlichter im Prolog aus

`SoulfireLighting.Draw` bekommt `drawArenaFurnaces`, im Prolog `false`. Die Ofenquellen liegen in Arenakoordinaten und würden sonst als fremde Lichtflecken im Prolog erscheinen.

### Abschnittswiederholung beginnt am Abschnittsanfang

`RestartPrologueSector` setzt auf den Startpunkt des Sektors zurück. Der Sektor `Escape` umfasst Devourer-Kampf und Überfahrt; ein Tod auf dem Skiff wiederholt daher den Devourer-Kampf, nicht nur die Überfahrt.

## Risks / Trade-offs

- **Überfahrt ohne Begleiter ist ungeprüft ausbalanciert.** Die 62-Sekunden-Verteidigung stammt aus einem Entwurf für zwei Wardens. → Manuell spielen; Anpassung im Folge-PR.
- **Tod auf dem Skiff wiederholt zu viel.** → Bewusst so übernommen, bei Bedarf eigenen Checkpoint für die Überfahrt ergänzen.
- **Der Prolog wird per Laufzeit-Skript nur teilweise abgelaufen.** Menü, Erwachen, Ufer-Anfang, Sprünge zu Suchgang, Devourer und Überfahrt liefen fehlerfrei; die Kämpfe im Suchgang und die Schwelle wurden nicht durchgespielt. → Nach dem Merge manuell durchspielen.
- **Ohne Prolog-Ambience klingt der Prolog wie die Arena-Stille.** → `AudioDirector`-Soundscapes im Folge-PR.
