## 1. Charakterwerte

- [x] 1.1 `Combat/PlayerAttributes` mit Startwert, Bereich, Schadensskalierung und Rüstungsformel anlegen; per Unit-Tests (`PlayerAttributesTests`) abnehmen.
- [x] 1.2 `ScytheCombat` skaliert den Schlagschaden mit Stärke vor dem Resonance-Multiplikator.
- [x] 1.3 `SoulCannon` skaliert den Schussschaden mit Fähigkeitsstärke vor dem Resonance-Multiplikator.
- [x] 1.4 `Player` hält die Werte, reicht sie an Sense und Kanone weiter und verringert erlittenen Schaden mit Rüstung; der Audio-Test-Todestreffer in `GameWorld` übergeht Rüstung.

## 2. Developer-Mode

- [x] 2.1 `DeveloperStartOptions` um `--strength`, `--ability-power` und `--armor` erweitern, `DEV_START`-Zeile ergänzen und in `GameWorld.ApplyDeveloperStart` anwenden; per Unit-Tests abnehmen.
- [x] 2.2 README-Abschnitt zum Developer-Mode um die neuen Parameter ergänzen.

## 3. Gesamtabnahme

- [x] 3.1 `openspec validate add-player-attributes --strict` und `dotnet test` erfolgreich ausführen.
- [ ] 3.2 Manuell anspielen: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena --strength 20 --ability-power 20 --armor 0` und mit `--armor 50` vergleichen (Gegner fallen schneller, erlittener Schaden halbiert sich).
