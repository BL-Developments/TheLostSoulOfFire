## 1. Spielphasen und Reset-Semantik

- [x] 1.1 Eine übergeordnete `GamePhase` für Titel, Aschenvorhalle, Toreintritt und Arena einführen
- [x] 1.2 Den bestehenden `ArenaLoopState` auf die internen Arena-Phasen Intro, Combat, Transition und Complete begrenzen
- [x] 1.3 Getrennte Pfade für Arena-Retry nach Tod und vollständigen Neustart nach erfolgreichem Abschluss implementieren
- [x] 1.4 Screenshot-Kontext, Fenstertitel, Debuganzeige und automatisierte Testzustände um die neue GamePhase ergänzen

## 2. Aschenvorhalle

- [x] 2.1 `SoulFurnaceAntechamber` mit Bounds, Spieler-Spawn, Torposition und Interaktionszone implementieren
- [x] 2.2 Eine kurze, eindeutig zum Tor führende begehbare Raumform ohne zusätzliche Hinderniskollisionen festlegen
- [x] 2.3 Gotisch-industrielle Umgebung aus schwarzem Stein, Metall, Rohren, Ketten, erloschenen Öfen und zurückhaltender Soulfire-Energie zeichnen
- [x] 2.4 Spielerbewegung, Blickrichtung und Kamera gegen die Antechamber-Bounds integrieren
- [x] 2.5 Ignition Dash im Pre-Level ermöglichen und Sensen-/Cannon-Aktionen dort zuverlässig unterdrücken

## 3. Soul Sense und Interaktion

- [x] 3.1 Soul Sense im Pre-Level über den bestehenden Darstellungs- und Audiopfad aktivieren
- [x] 3.2 Optionale Seelenspuren, eingeschlossene Energie und eine Soul-Sense-Reaktion des Tores darstellen
- [x] 3.3 Sicherstellen, dass Weg, Tor und Interaktion auch ohne Soul Sense vollständig lesbar bleiben
- [x] 3.4 Näheprüfung und zurückhaltenden `E`-Interaktionshinweis für das Tor implementieren

## 4. Inszenierter Arena-Eintritt

- [x] 4.1 Nach gültiger `E`-Interaktion Gameplay-Input sperren und die nicht abbrechbare Toreintrittssequenz starten
- [x] 4.2 Torbewegung, Kamera-Vorschub, Soulfire-Lichtimpuls und kurzen visuellen Übergang implementieren
- [x] 4.3 Spieler und Kamera nach Abschluss der Sequenz auf den Arena-Spawn setzen und das bestehende Arena-Intro starten
- [x] 4.4 Ruhige Vorhallen-Ambience und verzögerten Einsatz der Arena-Musik einschließlich fehlertolerantem Audio-Fallback integrieren

## 5. Verifikation

- [x] 5.1 Zustandsübergänge für Titel → Vorhalle → Toreintritt → Arena-Intro automatisiert prüfen
- [x] 5.2 Verifizieren, dass Tod-Retry direkt in der Arena und Complete-Restart erneut über Titel und Vorhalle führt
- [x] 5.3 Verifizieren, dass Angriffe im Pre-Level gesperrt, Bewegung, Dash und Soul Sense aber aktiv sind
- [x] 5.4 Pre-Level bei normaler Darstellung und Soul Sense auf Lesbarkeit, Kameragrenzen und Interaktionsreichweite prüfen
- [x] 5.5 Bestehende Gameplay-/Audio-Laufzeittests an den zusätzlichen Gate-Schritt anpassen und vollständig ausführen
- [x] 5.6 DesktopGL-Solution bauen und sicherstellen, dass keine neuen unversionierten Build-Artefakte sichtbar werden
