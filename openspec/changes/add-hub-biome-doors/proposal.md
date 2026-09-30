## Why

Der eigentliche Spielablauf soll von einem Hub ausgehen, aus dem die Biome erreicht werden (#11, #14). Hub und Homebase sind dasselbe; die bestehende Aschenvorhalle wird zu diesem Hub. Sie führt derzeit über ein einzelnes Tor direkt in die Arena und bietet noch keinen Ort für die späteren Biom-Zugänge.

## What Changes

- Die Aschenvorhalle wird zum Hub und erhält sieben schlicht dargestellte Türen, je eine pro Biom, beschriftet mit `BIOME I` bis `BIOME VII`.
- **BREAKING**: Das bisherige Ofen-/Kathedralentor zur Arena wird entfernt.
- Tür I ist offen. Ihre Aktivierung mit `E` startet den bisherigen Arena-Ablauf einschließlich inszeniertem Eintritt und Arena-Intro.
- Die Türen II bis VII sind versiegelt. In ihrer Nähe erscheint der Hinweis `SEALED · DEFEAT THE PREVIOUS GUARDIAN`; `E` startet dort keinen Übergang.
- Jede Tür besitzt einen Sperrzustand, an den die spätere Freischaltung durch Endbosssiege (#26, #27) andocken kann. Die Freischaltlogik selbst ist nicht Teil dieses Changes.
- Soul-Sense-Spuren führen künftig zu Tür I statt zum Tor.

## Capabilities

### New Capabilities

- `hub-biome-doors`: Sieben Biom-Türen im Hub mit Offen-/Versiegelt-Zustand, Näherungshinweis und Aktivierung der offenen Tür.

### Modified Capabilities

- `soul-furnace-antechamber`: Das Tor entfällt; der Arena-Eintritt erfolgt über Tür I. Soul-Sense-Spuren, Eintrittssequenz und Audioübergang beziehen sich auf Tür I.

## Impact

- Betrifft `SoulFurnaceAntechamber` (Raumgröße, Türen statt Tor, Interaktionszonen, Darstellung und Licht), die Interaktion und Hinweise in `GameWorld`, `GameFlowRules`, `PixelText` (Glyph `·`) sowie den Antechamber-Visual-Test in `Game1`.
- Bestehende Tests zu Tor und Flow werden auf Türen umgestellt.
- Keine neuen Assets, Abhängigkeiten oder Speicherstände; der Sperrzustand ist vorerst fest im Code hinterlegt.
