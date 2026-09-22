## 1. Grundlagen für Darstellung und Eingabe

- [x] 1.1 Den Glyphensatz von `Rendering/PixelText` um `Ä`, `Ö`, `Ü` und `ß` erweitern, die Großschreibungsumwandlung für diese Zeichen prüfen und an einem gerenderten Bild belegen, dass `ZURÜCK` vollständig und lesbar erscheint (vom Nutzer per Playtest bestätigt)
- [x] 1.2 `Input/InputState` um eine Abfrage tatsächlicher Mausbewegung seit dem letzten Frame ergänzen, ohne bestehende Eingabeauswertungen zu verändern

## 2. Menüstruktur

- [x] 2.1 Eine Menüseite aus beschrifteten Einträgen mit Auswahlindex, Wirkung je Eintrag und Kennzeichnung wirkungsloser Platzhaltereinträge modellieren
- [x] 2.2 Einen Menüzustand mit Seitenstapel implementieren, der das Öffnen einer Unterseite und die Rückkehr zur vorherigen Seite trägt, und sein Verhalten für Öffnen, Zurückkehren und leeren Stapel testen
- [x] 2.3 Hauptmenüseite mit `EINZELSPIELER`, `MEHRSPIELER`, `EINSTELLUNGEN`, `BEENDEN` sowie Einzelspielerseite mit `NEUES SPIEL`, `SPIEL LADEN`, `ZURÜCK` in fester Reihenfolge anlegen
- [x] 2.4 Auswahlbewegung per Tastatur mit Umlauf am Listenanfang und Listenende implementieren und testen

## 3. Bedienung per Maus und Tastatur

- [x] 3.1 Trefferflächen je Eintrag aus der gemessenen Textbreite und Glyphenhöhe mit Polsterung ableiten und gegen die Zeigerposition prüfen
- [x] 3.2 Auswahl über den Zeiger nur bei tatsächlicher Mausbewegung verändern und testen, dass ein ruhender Zeiger eine per Tastatur getroffene Auswahl nicht überschreibt
- [x] 3.3 Auslösen per Klick und per Tastaturbestätigung auf dieselbe Wirkung führen
- [x] 3.4 Eingaben auf der Menüliste erst annehmen, wenn deren Einblendung abgeschlossen ist, damit der bestätigende Klick der Titelkarte keinen Eintrag auslöst

## 4. Einbindung in den Ablauf

- [x] 4.1 Den Titelzweig in `Game/GameWorld` so umstellen, dass bestätigende Eingabe das Hauptmenü öffnet, statt unmittelbar nach `Intro` zu wechseln, und den bestehenden Bestätigungston beibehalten
- [x] 4.2 Den Menüzustand in Update und Draw so einhängen, dass Welt, Flamme, Partikel und Kamerafahrt während der Menüdarstellung ungestört weiterlaufen
- [x] 4.3 `NEUES SPIEL` auf den bestehenden Weg in den Arenaablauf mit zurückgesetztem Laufzeitzustand führen
- [x] 4.4 `BEENDEN` mit dem Beenden der Anwendung verbinden und dabei die bestehende Wirkung der Escape-Taste unverändert lassen
- [x] 4.5 `MEHRSPIELER`, `EINSTELLUNGEN` und `SPIEL LADEN` als auslösbare, aber wirkungslose Einträge belegen und prüfen, dass sie weder Menüseite noch Spielzustand verändern

## 5. Darstellung im Stil der Titelkarte

- [x] 5.1 Die Menüliste in `Rendering/CinematicPresentation` an der Stelle der bisherigen Startaufforderung zeichnen und Titelzeilen, Zierlinien, Schleier und Letterbox unverändert beibehalten
- [x] 5.2 Ausblenden der Startaufforderung und Einblenden der Menüliste als durchgehenden Übergang umsetzen, ohne die Kamerafahrt zu unterbrechen
- [x] 5.3 Den ausgewählten Eintrag hervorheben und die übrigen Einträge abgesetzt darstellen, im bestehenden Farbklang aus `SoulWhite` und `DeathFlameBright`
- [x] 5.4 Den Seitenwechsel zwischen Hauptmenü und Einzelspielerseite gestalten und im gerenderten Bild prüfen, dass beide Seiten innerhalb der Letterbox-Ränder liegen (vom Nutzer per Playtest bestätigt)

## 6. Prüfläufe und Abnahme

- [x] 6.1 Die automatisierten Prüfmodi in `Game1` das Menü überspringen lassen und belegen, dass `--audio-gameplay-test` und `--audio-death-restart-test` wieder PASS melden (verifiziert: `AUDIO_GAMEPLAY_TEST_PASS waves=4 completion=true restart=true`, `AUDIO_DEATH_RESTART_TEST_PASS death=true restart=true`)
- [x] 6.2 Den Screenshot-Kontext um den Menüzustand ergänzen, damit aufgenommene Bilder dem Menü und nicht der Titelkarte zugeordnet werden
- [x] 6.3 Den vollständigen Weg Titelkarte → Hauptmenü → Einzelspieler → Neues Spiel je einmal ausschließlich per Maus und einmal ausschließlich per Tastatur von Hand abnehmen und das Ergebnis als PASS/FAIL dokumentieren (PASS — vom Nutzer per Playtest bestätigt)
- [x] 6.4 Native Aufnahmen von Hauptmenü und Einzelspielerseite erstellen und dem Änderungsvorgang beilegen (vom Nutzer per Playtest bestätigt)
