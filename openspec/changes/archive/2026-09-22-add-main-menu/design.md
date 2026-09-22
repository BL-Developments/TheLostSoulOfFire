## Context

Der Einstieg in das Spiel liegt heute vollständig im Arenaablauf. `Game/GameWorld` führt die Zustandsfolge `Title → Intro → Combat → Transition → Complete`; der Titelzweig reagiert auf jede beliebige Eingabe und springt direkt nach `Intro`. Die Darstellung der Titelkarte liegt in `Rendering/CinematicPresentation` und besteht aus Schwarzschleier, Letterbox, Zierlinien, Titelzeilen, einer atmenden Startaufforderung und einer in der Welt gezeichneten Flamme hinter dem Schleier.

Drei bestehende Eigenschaften des Projekts begrenzen den Lösungsraum:

- Die Pixel-Schrift `Rendering/PixelText` kennt nur `A`–`Z`, `0`–`9` sowie `: - / .` und zwingt jede Ausgabe auf Großbuchstaben. Unbekannte Zeichen entfallen ersatzlos.
- Die Escape-Taste beendet in `Game1` die Anwendung, bevor irgendein Spielzustand die Eingabe sieht. Diese Wirkung bleibt laut Änderungsumfang bestehen.
- `Game1` enthält automatisierte Prüfmodi, die im Titelzustand eine Taste einspeisen, um den Arenaablauf zu erreichen.

Es existiert außerdem eine zweite, nicht verdrahtete Architektur (`Core/SceneManager`, `Scenes/HubScene`, `Scenes/RaidScene`, `TheLostSoulOfFireGame`), die von `Program.cs` nicht verwendet wird.

## Goals / Non-Goals

**Goals:**

- Ein Hauptmenü und ein Einzelspieler-Untermenü, die in Komposition und Rhythmus als Fortsetzung der Titelkarte gelesen werden.
- Gleichwertige Bedienung per Maus und per Tastatur.
- Eine Menüstruktur, die weitere Seiten aufnimmt, ohne dass der Arenaablauf davon berührt wird.
- Erhalt der bestehenden automatisierten Prüfläufe.

**Non-Goals:**

- Pausefunktion oder Rückkehr aus dem laufenden Kampf in das Menü.
- Umbau auf die ruhende Szenenarchitektur.
- Speicherstände, Mehrspielerbetrieb, Einstellungsinhalte.
- Gamepad-Navigation.

## Decisions

### Menü als eigene Ebene vor dem Arenaablauf, nicht als weitere Zustände von `ArenaLoopState`

Der Menüzustand wird von einer eigenen Einheit mit einem Seitenstapel geführt und liegt vor dem Arenaablauf. `ArenaLoopState` bleibt unverändert.

Erwogene Alternativen:

- **`ArenaLoopState` um `MainMenu` und `SingleplayerMenu` erweitern.** Verworfen: Das Enum wird an rund fünfzehn Stellen ausgewertet, unter anderem für Kameraführung, Overlay-Auswahl, HUD-Sichtbarkeit, Debug-Anzeige und Screenshot-Kontext. Jeder neue Wert erzwingt dort eine Entscheidung, obwohl ein Menü kein Zustand des Arenaablaufs ist. Zudem bräuchte jede weitere Menüseite einen weiteren Enum-Wert.
- **Wechsel auf `Core/SceneManager` mit einer `MenuScene`.** Verworfen: Diese Architektur ist nicht verdrahtet; ihre Übernahme hieße, den Einstiegspunkt der Anwendung auszutauschen und den gesamten Arenaablauf zu portieren. Das steht in keinem Verhältnis zu einem Menü.

### Ein Seitenstapel statt einzelner Menüzustände

Hauptmenü und Untermenü sind gleichartige Seiten aus beschrifteten Einträgen. Das Öffnen eines Untermenüs legt eine Seite ab, `ZURÜCK` nimmt sie herunter. Einstellungen und weitere Seiten fügen sich damit ohne strukturelle Änderung ein.

### Übergang an Ort und Stelle statt Szenenwechsel

Die Titelkarte bleibt stehen; die Startaufforderung blendet aus, die Menüliste blendet an derselben Stelle ein. Titelzeilen, Zierlinien, Schleier, Letterbox und die langsame Kamerafahrt laufen ununterbrochen weiter. Das ist zugleich die einfachere und die ruhigere Lösung gegenüber einem zweiten, eigenständig komponierten Bildschirm.

Ein Klick, der die Titelkarte bestätigt, darf keinen darunterliegenden Menüeintrag auslösen. Eingaben auf der Menüliste werden deshalb erst angenommen, wenn deren Einblendung abgeschlossen ist.

### Eine gemeinsame Auswahl für Maus und Tastatur

Beide Eingabewege führen denselben Auswahlindex. Ein ruhender Zeiger darf eine per Tastatur getroffene Auswahl nicht zurücksetzen, deshalb verändert die Zeigerposition die Auswahl nur bei tatsächlicher Bewegung. `Input/InputState` hält den vorherigen Mauszustand bereits privat und wird um die dafür nötige Abfrage ergänzt.

### Umlaute im Glyphensatz ergänzen statt Beschriftungen umschreiben

Da Escape kein Rückweg ist, braucht das Untermenü einen sichtbaren Eintrag `ZURÜCK`. Statt der Ersatzschreibweise `ZURUECK` werden `Ä`, `Ö`, `Ü` und `ß` als Glyphen ergänzt. Das kostet einmalig vier Bitmaps und nimmt allen künftigen deutschen Beschriftungen dieselbe Einschränkung ab.

### Automatisierte Prüfläufe umgehen das Menü

Die Prüfmodi in `Game1` speisen heute im Titelzustand eine Taste ein. Mit zwei Menüebenen davor wäre eine nachgebildete Navigation unnötig zerbrechlich. Die Prüfmodi überspringen das Menü stattdessen und gelangen unmittelbar in den Arenaablauf.

## Risks / Trade-offs

- **Drei von sechs Einträgen sind wirkungslos und sehen aus wie gültige Einträge** → Bewusste Festlegung. Falls es sich im Spielbetrieb defekt anfühlt, genügt später ein abgesenkter Alphawert auf den Platzhaltern.
- **Escape beendet auch im Untermenü die Anwendung** → Bewusst außerhalb des Umfangs gehalten. Der sichtbare Eintrag `ZURÜCK` stellt den Rückweg sicher, sodass niemand auf Escape angewiesen ist.
- **Umlaut-Glyphen sind bei sieben Pixeln Höhe eng** → Die Punkte belegen die oberste Zeile; der Grundbuchstabe wird dafür gestaucht. Prüfung am gerenderten Bild, nicht am Bitmap.
- **Trefferflächen und Textbreiten weichen voneinander ab** → Die Trefferfläche wird aus der gemessenen Textbreite abgeleitet und großzügig gepolstert, damit der anklickbare Bereich dem sichtbaren Eintrag entspricht.
- **Die Prüfmodi umgehen das Menü und decken es damit nicht ab** → Akzeptiert. Das Menü wird von Hand abgenommen; die Prüfläufe belegen weiterhin Audio und Arenaablauf.

## Open Questions

Keine offenen Punkte. Umfang, Bedienung, Escape-Verhalten und Platzhalterverhalten sind festgelegt.
