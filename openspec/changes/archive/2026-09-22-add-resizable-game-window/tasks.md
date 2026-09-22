## 1. Vorbereitung

- [x] 1.1 Arbeitsbranch auf den Stand von `main` inklusive Hauptmenü bringen, da dieses zusätzliche Zeigerauswertungen einführt
- [x] 1.2 `Core/ResolutionManager` als gemeinsame Skalierungsquelle für den `Game1`-Stack festlegen und die bestehenden Tests in `CollisionAndResolutionTests` als Absicherung bestätigen

## 2. Virtuelle Auflösung und Render-Target-Kette

- [x] 2.1 `SoulfireRenderer.PresentScene` so umstellen, dass es statt auf den Fensterpuffer auf ein vom Aufrufer übergebenes Root-Render-Target zurückstellt
- [x] 2.2 Alle weiteren Stellen in `SoulfireRenderer` prüfen, die Render-Targets binden, und sicherstellen, dass keine implizit auf den Fensterpuffer zurückfällt
- [x] 2.3 In `Game1` ein virtuelles Render-Target in 1280×720 anlegen, verwalten und beim Beenden freigeben
- [x] 2.4 `Game1.Draw` in zwei Phasen teilen: Welt und HUD in das virtuelle Ziel zeichnen, anschließend den Fensterpuffer leeren und das Ziel mit Punktfilterung in das eingepasste Zielrechteck blitten
- [x] 2.5 Den an `GameWorld` übergebenen Viewport erst nach dem Binden des virtuellen Ziels ermitteln, damit er die virtuelle Auflösung beschreibt

## 3. Fenstergröße und Vollbild

- [x] 3.1 Freies Skalieren und den Maximize-Button aktivieren
- [x] 3.2 Größenänderungen des Fensters auf den `ResolutionManager` übertragen und die Behandlung gegen Wiedereintritt absichern
- [x] 3.3 Eine Mindestfenstergröße erzwingen, damit das Zielrechteck nicht entartet
- [x] 3.4 `F11` als Umschalter für den randlosen Vollbildmodus ohne Auflösungswechsel implementieren
- [x] 3.5 Die Fenstergröße vor dem Wechsel ins Vollbild im Speicher merken und beim Zurückschalten wiederherstellen; keine Persistenz über Sitzungen hinweg
- [x] 3.6 Sicherstellen, dass `Escape` sein bestehendes Verhalten behält und den Vollbildzustand nicht beeinflusst

## 4. Zeigereingaben

- [x] 4.1 `InputState` um eine auf die virtuelle Auflösung umgerechnete Zeigerposition neben der rohen Fensterposition erweitern
- [x] 4.2 Die Zeigerauswertung für das Zielen der Waffen in `GameWorld` auf die umgerechnete Position umstellen
- [x] 4.3 Hover- und Klickauswertung der Hauptmenüeinträge in `GameWorld` auf die umgerechnete Position umstellen
- [x] 4.4 Prüfen, dass Zeigerpositionen im Letterbox-Randbereich keine Menüauswahl auslösen

## 5. Screenshots

- [x] 5.1 `ScreenshotCapture` vom Fensterpuffer auf das virtuelle Ziel umstellen, sodass Aufnahmen unabhängig von Fenstergröße und Vollbildzustand 1280×720 groß und randfrei sind

## 6. Verifikation

- [x] 6.1 Automatisierte Tests für die Abbildung von Fenster- auf Spielkoordinaten bei mehreren Fenstergrößen und Seitenverhältnissen ergänzen
- [x] 6.2 Manuell prüfen: Ziehen, Maximieren, `F11` hin und zurück, Zielen und Menübedienung bei mehreren Fenstergrößen und auf einem Breitbildseitenverhältnis
- [x] 6.3 Bestätigen, dass der sichtbare Weltausschnitt und das HUD-Layout gegenüber dem bisherigen Zustand unverändert sind
- [x] 6.4 Bestätigen, dass die bestehenden automatisierten Laufzeittests weiterhin durchlaufen
