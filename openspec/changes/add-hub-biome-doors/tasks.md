## 1. Raum und Türmodell

- [x] 1.1 `SoulFurnaceAntechamber` nach oben vergrößern und die begehbare Fläche direkt unter der Nordwand anschließen lassen
- [x] 1.2 Tor, `Gate` und die einzelne `InteractionZone` entfernen
- [x] 1.3 Sieben Türen mit Art (Biom/Final), Biom-Index, Rechteck, nicht überlappender Interaktionszone und Sperrzustand anlegen: I, II, III, Final, IV, V, VI; I offen, alle anderen versiegelt
- [x] 1.4 Abfrage ergänzen, welche Tür der Spieler gerade erreicht
- [x] 1.5 Soul-Sense-Spuren und Lichtakzente auf Tür I ausrichten

## 2. Darstellung

- [x] 2.1 Schlichte Türbögen mit römischer Ziffer zeichnen; versiegelte Türen dunkler und mit Siegel-Symbol
- [x] 2.2 Final-Tür in der Mitte größer, ohne Ziffer und mit eigenem, auffälligerem Rahmen zeichnen
- [x] 2.3 Öffnungsanimation von Tür I für die Eintrittssequenz zeichnen
- [x] 2.4 `PixelText` um den Glyph `·` ergänzen

## 3. Interaktion und Ablauf

- [x] 3.1 Hinweis `E  ENTER BIOME I` an Tür I `SEALED · DEFEAT THE PREVIOUS GUARDIAN` an versiegelten Biom-Türen und `SEALED · DEFEAT ALL GUARDIANS` an der Final-Tür anzeigen, Panelbreite aus Textbreite
- [x] 3.2 `E` an Tür I startet `EnteringArena`; `E` an versiegelten Türen oder außerhalb aller Zonen bleibt wirkungslos
- [x] 3.3 Eintrittssequenz, Kamera-Vorschub und Einblendetext auf Tür I umstellen
- [x] 3.4 `GameFlowRules` sprachlich von Tor auf Tür umbenennen, Verhalten unverändert

## 4. Verifikation

- [x] 4.1 `AntechamberFlowTests` und `PrologueFlowTests` auf Türen umstellen und Tests für Sperrzustand, Zonen und wirkungsloses `E` an versiegelten Türen ergänzen
- [x] 4.2 Antechamber-Visual-Test in `Game1` auf Tür I umstellen und Screenshots für Hub, versiegelten Hinweis und Eintritt prüfen
- [x] 4.3 Build und alle Tests grün
