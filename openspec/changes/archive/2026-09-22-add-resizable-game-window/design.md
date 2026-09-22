## Context

`Program.cs` startet `Game1`. Dieses zeichnet Welt und HUD direkt in den Fensterpuffer und reicht dabei `GraphicsDevice.Viewport` als Parameter durch `GameWorld` bis in Kamera, HUD und Overlays. Weil das Fenster nicht skalierbar ist, war diese Kopplung bisher folgenlos: Viewport, Fenstergröße und Spielauflösung waren immer 1280×720.

Parallel existiert mit `Core/GameCore` ein älterer, szenenbasierter Ast, der Letterboxing über `Core/ResolutionManager` und ein virtuelles Render-Target bereits vollständig umsetzt. Er wurde vom Soulfire-Prototyp überholt, besitzt keinen Subklassen-Nutzer und ist seit dem Prototyp-Merge nur noch durch einen Namespace-Fix berührt worden. `ResolutionManager` selbst ist davon unabhängig, frei von Abhängigkeiten auf den toten Ast und durch `CollisionAndResolutionTests` abgedeckt.

## Goals / Non-Goals

**Goals:**

- Das Fenster frei skalierbar, maximierbar und randlos im Vollbild darstellbar machen.
- Das von außen beobachtbare Spielverhalten bei jeder Fenstergröße identisch zum heutigen 1280×720-Zustand halten.
- Zeigereingaben bei jeder Fenstergröße korrekt auf Spielkoordinaten abbilden.
- Den bestehenden `ResolutionManager` wiederverwenden statt eine zweite Skalierungslösung zu bauen.

**Non-Goals:**

- Das Sichtfeld mit der Fenstergröße wachsen lassen.
- Das HUD auflösungsabhängig neu layouten oder skalieren.
- Fenstergröße, Vollbildzustand oder Auflösung über Sitzungen hinweg speichern.
- Den toten `GameCore`-/`Gameplay`-Ast zurückbauen oder migrieren.
- Einen exklusiven Vollbildmodus mit Auflösungswechsel anbieten.

## Decisions

### Feste virtuelle Auflösung statt mitwachsendem Sichtfeld

Das Spiel wird weiterhin in 1280×720 gezeichnet und als Ganzes in das Fenster skaliert. Die Alternative — den Fensterpuffer nativ mitwachsen zu lassen — wurde verworfen, weil sie drei Folgeprobleme erzeugt: `Camera2D.Follow` leitet den sichtbaren Ausschnitt aus der Viewport-Größe ab, sodass größere Fenster mehr Welt zeigen und damit die Balance verändern; ab einer Viewport-Höhe über der Arenahöhe von 1000 Weltunits kippt die Klemmung in `Follow` in den Zentrierungszweig und gibt den Blick über den Arenarand frei; und das in absoluten Pixeln an die Viewport-Ränder gesetzte HUD müsste vollständig neu aufgebaut werden.

Mit fester virtueller Auflösung bleiben Kamera, Arena, HUD und Overlays unverändert, weil sie weiterhin denselben Viewport sehen wie heute.

### Skalierung mit PointClamp

Der Blit des virtuellen Ziels in den Fensterpuffer verwendet Punktfilterung, passend zur Pixel-Art-Sprache des Spiels und konsistent mit den übrigen `SpriteBatch.Begin`-Aufrufen. Bei nicht-ganzzahligen Skalierungsfaktoren entstehen dadurch ungleichmäßige Pixelgrößen; das wird zugunsten von Schärfe und freier Fenstergröße in Kauf genommen. Ganzzahlige Skalierung wurde verworfen, weil sie bei den meisten Fenstergrößen breite ungenutzte Ränder erzeugt.

### Das Root-Render-Target wird explizit durchgereicht

`SoulfireRenderer` besitzt ein eigenes Szenen-Target und setzt in `PresentScene` per `SetRenderTarget(null)` auf den Fensterpuffer zurück. MonoGame kennt keinen Target-Stack: `null` bedeutet Fensterpuffer, nicht „vorheriges Ziel". Bliebe das so, würde die gesamte Komposition nach `PresentScene` — Szenen-Grading, Soulfire-Licht, Vignette, Bildschirm-Feedback und HUD — am virtuellen Ziel vorbei direkt in den Fensterpuffer schreiben, woraufhin der abschließende Letterbox-Blit ein leeres Ziel darüber zeichnet.

`PresentScene` stellt daher auf ein vom Aufrufer bestimmtes Root-Target zurück. Ein automatisches Retten und Wiederherstellen über `GetRenderTargets` wurde verworfen, weil die explizite Übergabe die Besitzverhältnisse sichtbar macht und keine stillen Nebenwirkungen erzeugt.

Als Nebeneffekt sieht `SoulfireRenderer` künftig immer denselben virtuellen Viewport, sodass sein Szenen-Target nach der ersten Erzeugung nie wieder neu alloziert wird — beim Ziehen am Fensterrand entfällt damit die sonst frameweise Neuallokation.

### Viewport wird nach dem Binden des Ziels gelesen

MonoGame setzt `GraphicsDevice.Viewport` beim Binden eines Render-Targets automatisch auf dessen Maße. `Game1.Draw` liest den Viewport deshalb erst nach dem Binden des virtuellen Ziels; damit erhält `GameWorld` ohne zusätzliche Konstruktion den virtuellen Viewport.

### Zeigerumrechnung in `InputState` statt an den Aufrufstellen

Die Fenster-zu-Spiel-Umrechnung wird einmalig in `InputState` vorgenommen und als eigene Eigenschaft neben der rohen Fensterposition angeboten. `GameWorld` wertet Zeigerpositionen an mehreren Stellen aus — für das Zielen der Waffen sowie für Hover und Klick der Hauptmenüeinträge, deren Trefferflächen aus dem Viewport abgeleitet werden. Eine Umrechnung an jeder einzelnen Aufrufstelle wäre fehleranfällig und würde bei künftigen Zeigerabfragen erneut vergessen. `Core/InputManager` im toten Ast nutzt bereits dasselbe Muster.

### `F11` schaltet Vollbild, `Escape` bleibt unverändert

`Escape` beendet in `Game1` das Spiel, bevor andere Systeme die Taste sehen. Die verbreitete Erwartung, dass `Escape` den Vollbildmodus verlässt, würde damit kollidieren und das Spiel beenden. Der Vollbildmodus wird deshalb ausschließlich über `F11` betreten und verlassen; `Escape` behält seine bestehende Bedeutung.

Der Vollbildmodus ist randlos, ohne Auflösungswechsel. Das hält den Wechsel sofort wirksam und den Anwendungswechsel schnell.

### Fenstergröße wird nur innerhalb der Sitzung gemerkt

Beim Wechsel ins Vollbild wird die aktuelle Fenstergröße im Speicher gehalten und beim Zurückschalten wiederhergestellt, damit der Nutzer nicht auf der Startgröße landet. Über Sitzungen hinweg wird nichts gespeichert; das Spiel startet immer in 1280×720. Eine Einstellungsdatei existiert im Projekt nicht und würde den Umfang dieses Changes deutlich vergrößern.

### Screenshots werden vom virtuellen Ziel genommen

`ScreenshotCapture` liest heute den Fensterpuffer. Nach der Umstellung enthielte dieser die Letterbox-Balken und wäre von der Fenstergröße abhängig. Stattdessen wird das virtuelle Ziel ausgelesen, womit Screenshots unabhängig von Fenstergröße und Vollbildzustand reproduzierbar 1280×720 groß und randfrei sind.

## Risks / Trade-offs

- **Schwarze Balken bei abweichendem Seitenverhältnis.** Auf Breitbildmonitoren bleiben im Vollbild seitliche Balken. Das ist die bewusste Gegenleistung dafür, dass Balance und HUD unangetastet bleiben.
- **Unscharfe Pixelkanten bei krummen Skalierungsfaktoren.** Punktfilterung erzeugt bei nicht-ganzzahliger Skalierung ungleichmäßig große Pixel.
- **Signaturänderung an `SoulfireRenderer.PresentScene`.** Ein übersehener Aufrufer würde in den falschen Puffer zeichnen. Die Methode hat nur einen Aufrufer.
- **Wiedereintritt bei Größenänderung.** Das Behandeln der Größenänderung kann das auslösende Ereignis erneut auslösen. Die Behandlung wird gegen Wiedereintritt abgesichert.
- **Entartete Fenstergrößen.** Sehr kleine Fenster führen zu einem entarteten Zielrechteck. Eine Mindestgröße begrenzt das.

## Migration Plan

Der Change setzt auf dem Stand von `main` inklusive des Hauptmenüs auf, weil dieses zwei zusätzliche Zeigerauswertungen einführt, die ohne die Umrechnung falsch reagieren würden. Der aktuelle Arbeitsbranch liegt einen Commit dahinter und wird vorher aktualisiert.

Die Umstellung erfolgt in einem Schritt und ohne Datenmigration oder Kompatibilitätsschicht. Bestehendes Spielverhalten bleibt unverändert; ein Rückbau entspräche dem Zurücknehmen des Changes.

## Open Questions

Keine.
