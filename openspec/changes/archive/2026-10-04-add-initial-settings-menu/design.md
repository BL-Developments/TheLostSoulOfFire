## Context

`MenuController` hat bereits einen Seitenstapel und gemeinsame Maus-/Tastaturauswahl. `CinematicPresentation` zeichnet feste Menüzeilen; `GameWorld.UpdateMenu` wertet sie aus. `Game1` fängt `Escape` vor dem Menü ab und hält den bestehenden `F11`-Vollbildwechsel. `AudioDirector` mischt Musik, Ambience und Cues zustandsabhängig. `ScreenEffects` liefert Kameraerschütterung und Kamera-Kick. Bisher werden keine Benutzereinstellungen gespeichert.

## Goals / Non-Goals

**Goals:**

- Die vorhandene Menüstruktur und Darstellungsauflösung weiterverwenden; Werte und Platzhalter bleiben bei Maus- und Tastaturbedienung unterscheidbar.
- Genau eine wirksame Quelle für jeden Einstellungswert, auch wenn `F11` oder ein Menüeintrag denselben Zustand ändern.
- Einstellungen ohne verfügbares Audiogerät oder schreibbaren Speicherort nutzbar halten.

**Non-Goals:**

- Allgemeines UI-Framework, frei belegbare Tasten, Controller-Menünavigation oder Einstellungen im laufenden Run.
- Auflösungswahl, Grafik-Presets, Schwierigkeitsstufen, tatsächliche Errungenschaften und Statistiken.
- Die Bildbewegungsoption verändert keine Trefferzeiten, Hitstop-Logik oder Bildschirmblitze.

## Decisions

### Bestehende Menüseiten um Wertzeilen ergänzen

`MenuController` erhält die Einstellungsseiten und ihren Rückweg; die vorhandene Auswahl- und Zeichenlogik wird für Wertzeilen erweitert. Audio-Prozentwerte erscheinen als Zahl und Balken, Schalter und Bildbewegung als ausgeschriebene Werte. Links/Rechts ändert den ausgewählten Wert, ein Klick auf die Wertzeile beziehungsweise den Balken ändert denselben Wert. Einträge mit `IsPlaceholder` bleiben inert. Eine neue allgemeine UI-Hierarchie wäre für diese festen Seiten unnötig.

Der bestehende `PixelText`-Glyphensatz kennt `%` noch nicht; das Zeichen wird ergänzt oder die Prozentzahl wird ohne Symbol mit eindeutigem Kontext dargestellt. Bei sechs Zeilen in der Einstellungsübersicht müssen Text und Trefferflächen innerhalb der virtuellen 1280×720-Auflösung bleiben.

### Eine kleine Einstellungsdatei statt Spielstandssystem

Ein einfacher Einstellungszustand enthält Hinweise, Bildbewegungsstufe, Vollbild und die drei Audioanteile. Er wird als JSON im lokalen Anwendungsdatenverzeichnis gespeichert; dafür genügt `System.Text.Json`. Beim Laden werden Werte einzeln geprüft und auf Standardwerte zurückgesetzt, falls die Datei fehlt oder fehlerhaft ist. Nach einer Änderung werden Werte für die laufende Sitzung sofort angewendet und beim Verlassen der Einstellungen beziehungsweise nach `F11` gespeichert. Ein fehlgeschlagener Schreibvorgang blockiert das Spiel nicht. Eine temporäre Datei im selben Verzeichnis und anschließendes Ersetzen vermeiden eine teilweise geschriebene Einstellungsdatei.

Gespeichert wird nur die Vollbildwahl, nicht die Fenstergröße. Beim Start ohne gültige Datei bleibt der bisherige Fenstermodus Standard. Bei gespeichertem Vollbild wird der Zustand vor der ersten sichtbaren Spielinteraktion angewendet; beim späteren Zurückschalten nutzt das Spiel die aktuelle beziehungsweise die Standard-Fenstergröße dieser Sitzung.

### Vollbild, Audio und Bildbewegung an den bestehenden Wirkpfaden steuern

Menü und `F11` rufen denselben Vollbildwechsel auf; die Menüzeile liest den tatsächlichen Zustand. `AudioDirector` multipliziert seine bestehenden Musik-/Ambience-Mischungen mit den Nutzerwerten; Effekte und Ambience folgen dem Effektwert, während der Gesamtwert beide Tonpfade umfasst. Damit bleiben vorhandene Fades und Ducking erhalten. Die Bildbewegungsstufe skaliert den Kameraoffset aus Erschütterung und Kick beim Zeichnen; Spielzustand und Hitstop bleiben unberührt. Der Hinweiswert unterdrückt nur als optional markierte Tutorialtipps, nicht Toreingabe oder andere notwendige Aktionen.

### Escape nur in den Einstellungsseiten abfangen

`Game1` überlässt `Escape` dem Menü, solange eine Einstellungsseite offen ist; dort verhält sich die Taste wie `ZURÜCK`. In Titelkarte, Hauptmenü, Einzelspieler und Gameplay bleibt die bestehende Beenden-Wirkung erhalten. Das ist eine gezielte Ausnahme von der bisherigen globalen Regel und erfordert die entsprechende Aktualisierung der Hauptmenü-Spezifikation.

## Risks / Trade-offs

- **Der Hinweis-Schalter wirkt scheinbar nicht** → Mindestens ein sichtbarer optionaler Tutorialtipp wird ihm zugeordnet; notwendige Interaktionshinweise bleiben unabhängig davon sichtbar.
- **Ein gespeicherter Vollbildzustand erschwert den Start auf einem anderen Monitor** → `F11` bleibt jederzeit als Ausweg verfügbar; ungültige gespeicherte Werte fallen auf Fenstermodus zurück.
- **Ein Regler überdeckt Menünavigation oder passt nicht ins Bild** → Trefferflächen an sichtbare Wertzeilen koppeln und Maus-/Tastaturbedienung bei Startgröße sowie abweichendem Fensterformat prüfen.
- **Audio fehlt oder Speichern scheitert** → Einstellungen wirken soweit möglich in der Sitzung; Laden/Schreiben und Audiowiedergabe bleiben fehlertolerant.
