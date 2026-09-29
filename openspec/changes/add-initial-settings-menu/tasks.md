## 1. Menüseiten und Eingaben

- [x] 1.1 `EINSTELLUNGEN` mit `GAMEPLAY`, `GRAFIK`, `AUDIO`, `STEUERUNG`, `BARRIEREFREIHEIT` und `ZURÜCK` öffnen; die letzten zwei Einträge und `ERRUNGENSCHAFTEN UND STATISTIKEN` im Hauptmenü als wirkungslose Platzhalter ergänzen und Menünavigation automatisiert prüfen.
- [x] 1.2 Wertzeilen mit sichtbarem Zustand, Mausbedienung und Links-/Rechts-Eingabe ergänzen; Auswahl, Grenzen und Klickziele automatisiert prüfen.
- [x] 1.3 `Escape` nur innerhalb der Einstellungsseiten als Rückweg verwenden und per Test prüfen, dass Einzelspieler-Menü und Gameplay ihr bisheriges Beenden-Verhalten behalten.

## 2. Einstellungen und Vollbild

- [x] 2.1 Kleine lokale Einstellungsdatei mit Standardwerten, Werteprüfung und fehlertolerantem Laden/Speichern einführen; fehlende, ungültige und nicht beschreibbare Datei durch gezielte Tests prüfen.
- [ ] 2.2 `VOLLBILD` im Menü und `F11` an denselben gespeicherten Zustand anbinden; Neustart im Vollbild und Rückkehr zur Fenstergröße im echten DesktopGL-Lauf prüfen.

## 3. Wirkung der Menüpunkte

- [x] 3.1 `OPTIONALE HINWEISE` auf mindestens einen sichtbaren Tutorialtipp anwenden; prüfen, dass erforderliche Interaktionsaufforderungen bei beiden Werten sichtbar bleiben.
- [x] 3.2 `BILDBEWEGUNG` auf Kameraerschütterung und Kamera-Kick anwenden; die Stufen normal/reduziert/aus prüfen, ohne Treffer- oder Hitstop-Logik zu verändern.
- [x] 3.3 Gesamt-, Musik- und Effektlautstärke einschließlich Ambience in den bestehenden Audiomix einbeziehen; getrennte Wirkung, laufende Sounds und Audio-Fallback prüfen.

## 4. Gesamtabnahme

- [ ] 4.1 `openspec validate add-initial-settings-menu --strict` und `dotnet test` erfolgreich ausführen; Titelmenü, Unterseiten, Platzhalter, Neustart und Maus-/Tastaturbedienung bei Startgröße sowie abweichendem Fensterformat manuell prüfen und native Captures ansehen.
