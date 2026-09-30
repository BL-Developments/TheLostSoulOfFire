## 1. Raum und Türmodell

- [ ] 1.1 `SoulFurnaceAntechamber` nach oben vergrößern und die begehbare Fläche direkt unter der Nordwand anschließen lassen
- [ ] 1.2 Tor, `Gate` und die einzelne `InteractionZone` entfernen
- [ ] 1.3 Sieben Türen mit Index, Rechteck, nicht überlappender Interaktionszone und Sperrzustand anlegen (I offen, II–VII versiegelt)
- [ ] 1.4 Abfrage ergänzen, welche Tür der Spieler gerade erreicht
- [ ] 1.5 Soul-Sense-Spuren und Lichtakzente auf Tür I ausrichten

## 2. Darstellung

- [ ] 2.1 Schlichte Türbögen mit römischer Ziffer zeichnen; versiegelte Türen dunkler und mit Siegel-Symbol
- [ ] 2.2 Öffnungsanimation von Tür I für die Eintrittssequenz zeichnen
- [ ] 2.3 `PixelText` um den Glyph `·` ergänzen

## 3. Interaktion und Ablauf

- [ ] 3.1 Hinweis `E  ENTER BIOME I` an Tür I und `SEALED · DEFEAT THE PREVIOUS GUARDIAN` an versiegelten Türen anzeigen, Panelbreite aus Textbreite
- [ ] 3.2 `E` an Tür I startet `EnteringArena`; `E` an versiegelten Türen oder außerhalb aller Zonen bleibt wirkungslos
- [ ] 3.3 Eintrittssequenz, Kamera-Vorschub und Einblendetext auf Tür I umstellen
- [ ] 3.4 `GameFlowRules` sprachlich von Tor auf Tür umbenennen, Verhalten unverändert

## 4. Verifikation

- [ ] 4.1 `AntechamberFlowTests` und `PrologueFlowTests` auf Türen umstellen und Tests für Sperrzustand, Zonen und wirkungsloses `E` an versiegelten Türen ergänzen
- [ ] 4.2 Antechamber-Visual-Test in `Game1` auf Tür I umstellen und Screenshots für Hub, versiegelten Hinweis und Eintritt prüfen
- [ ] 4.3 Build und alle Tests grün
