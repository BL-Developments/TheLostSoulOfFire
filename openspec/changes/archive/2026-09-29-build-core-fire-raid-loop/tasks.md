## 1. Projektgrundlage und Abhängigkeiten

- [x] 1.1 MonoGame-DesktopGL-Solution und Anwendung für .NET 9 im aktuellen Repository wiederherstellen beziehungsweise erzeugen, Paketversionen für MonoGame 3.8.5.1 und MonoGame.Extended 6.0.0 festschreiben und einen erfolgreichen Restore nachweisen
- [x] 1.2 Ein separates Testprojekt anlegen, es mit der Solution und dem Spielprojekt verbinden und einen leeren Testlauf sowie Debug-Build erfolgreich ausführen
- [x] 1.3 Content-Struktur für Level, Sprites, Partikel und Fonts anlegen, konsistente case-sensitive Assetnamen in `Content.mgcb` registrieren und den Content-Build verifizieren

## 2. Core-, Szenen- und Entity-Grundlage

- [x] 2.1 Einen minimalen `Core`-Spielhost mit festem 60-Hz-Update, gemeinsamem `SpriteBatch`, virtueller Auflösung, proportionalem Letterboxing und sauberer Ressourcenfreigabe implementieren
- [x] 2.2 Einen zentralen `InputManager` für vorherigen und aktuellen Tastatur-/Mauszustand, gehaltene und einmalige Aktionen sowie Mausumrechnung in virtuelle Koordinaten implementieren und testen
- [x] 2.3 `Scene` und `SceneManager` mit sicher angeforderten Szenenwechseln, eigenem `ContentManager` pro Szene sowie deterministischem Initialize-/Load-/Unload-/Dispose-Lebenszyklus implementieren und testen
- [x] 2.4 `GameBehaviour`, `GameEntity` und `GameWorld` mit Transform, einmaligen `Awake`-/`Start`-Aufrufen, gefilterten Update-/Draw-Listen sowie verzögerter Erzeugung und Zerstörung implementieren und ihre Lebenszyklen testen
- [x] 2.5 Gemeinsame Gameplay-Verträge und Behaviours für Hitbox, Schaden, Gesundheit und Sprite-Darstellung ergänzen, wobei Komponentenreferenzen außerhalb der Hot Paths gecacht werden

## 3. Leveldaten und Spielzustände

- [x] 3.1 `ProfileState`, `RunState` und `LevelState` mit den im Design festgelegten Lebensdauern einführen und Tests für Erzeugung und vollständiges Verwerfen eines Runs schreiben
- [x] 3.2 Datenmodelle und Content-Dateien für Hub und erste Raid mit Grenzen, Wänden, Spieler-Spawn, Gegner-Spawns, Portal und Abschlussaltar erstellen
- [x] 3.3 Eine Level-Factory implementieren, die dieselbe Definition reproduzierbar in eine frische `GameWorld` mit blockierender Geometrie und Levelobjekten übersetzt, und dies automatisiert testen
- [x] 3.4 Achsenweise AABB-Bewegungsauflösung gegen Levelgrenzen und Wände implementieren und Tests für gerades, diagonales und gleitendes Bewegen entlang einer Wand ergänzen

## 4. Hub und Fire-Raid-Fluss

- [x] 4.1 `HubScene` mit steuerbarer Hub-Figur, erkennbarem Raid-Portal und Interaktionshinweis innerhalb des Portalbereichs implementieren
- [x] 4.2 Portalinteraktion so verbinden, dass nur eine gültige Interaktion einen neuen `RunState` erzeugt und einen sicheren Wechsel in `RaidScene` anfordert
- [x] 4.3 `RaidScene` mit frischer Raid-Levelinstanz, eigenem `LevelState` und einer Kamera implementieren, die der Spielfigur folgt und ihre inverse Matrix pro Update für das Zielen bereitstellt
- [x] 4.4 Erfolgs- und Todeswechsel so implementieren und testen, dass der Run verworfen wird, die Hub-Szene frisch geladen wird und ein anschließender Raid-Start keinerlei Gegner-, Projektil- oder Schadenszustand übernimmt

## 5. Bewegung und Kampf

- [x] 5.1 Spielerbewegung für WASD und Pfeiltasten mit normalisiertem Acht-Richtungs-Vektor, zeitbasierter Geschwindigkeit und Wandkollision in Hub und Raid implementieren
- [x] 5.2 Einen vorallokierten Feuerprojektil-Pool mit Lebensdauer, Geschwindigkeit, AABB-Hitbox, erstem Treffer und sicherer Rückgabe in den Pool implementieren
- [x] 5.3 Mauszielrichtung und gehaltene linke Maustaste mit einer zeitbasierten Angriffsabklingzeit verbinden und automatisierte Tests für Richtung und Schussfrequenz ergänzen
- [x] 5.4 Den grundlegenden Gegnertyp mit Verfolgungsbewegung, Gesundheit, Projektilschaden und Entfernung bei null Gesundheit implementieren
- [x] 5.5 Kontaktschaden des Gegners mit zeitbasierter Spieler-Unverwundbarkeit implementieren und Tests gegen wiederholten Schaden in aufeinanderfolgenden Frames ergänzen
- [x] 5.6 Begegnungsfortschritt mit dem Gegnerbestand verbinden, den Abschlussaltar bis zum letzten erforderlichen Gegner sperren und danach eine erfolgreiche Interaktion ermöglichen
- [x] 5.7 Spielertod bei null Gesundheit mit deaktivierter Kampfsteuerung, kurzer Niederlagenphase und anschließender Hub-Rückkehr implementieren und testen

## 6. Düstere Feuerdarstellung

- [x] 6.1 Ein kohärentes Platzhalter-Artset für Terrain, Wände, Spieler, Gegner, Portal, Altar, Projektil und Partikel in der festgelegten Kohle-/Rot-/Orange-Palette erstellen und über die Content-Pipeline laden
- [x] 6.2 Welt-Rendering in Terrain-, Entity- und additive Effektpässe mit benannten Layer-Tiefen aufteilen und UI in einem separaten SpriteBatch-Pass ohne Kameratransformation zeichnen
- [x] 6.3 Feste MonoGame.Extended-Partikelemitter für kontinuierliche Portal-/Altar-Glut sowie manuelle Schuss- und Trefferbursts beim Laden konfigurieren und ohne Erzeugung neuer Emitter im Game Loop auslösen
- [x] 6.4 Gesundheits-HUD, Portal-/Altar-Interaktionshinweis, gesperrten/aktiven Altarzustand und Niederlagenrückmeldung lesbar über der Welt darstellen
- [x] 6.5 Darstellung bei mindestens zwei Fenstergrößen und einem abweichenden Seitenverhältnis manuell auf Letterboxing, unverzerrte Skalierung und Kampferkennbarkeit prüfen

## 7. Abschlussvalidierung und Dokumentation

- [x] 7.1 Automatisierte Tests aus allen Szenarien der vier Capability-Spezifikationen vervollständigen und den gesamten Testlauf ohne Fehler ausführen
- [x] 7.2 Hot Paths in `Update` und `Draw` auf LINQ, wiederkehrende Collection-/Ressourcenallokationen, ungecachte Komponentenabfragen und Logik im Draw-Pfad prüfen und gefundene Verstöße beseitigen
- [ ] 7.3 Debug-Build starten und den kompletten Loop sowohl über erfolgreichen Abschluss als auch über Tod und anschließenden erneuten Raid-Start manuell verifizieren
- [x] 7.4 README um Voraussetzungen, Start-/Testbefehle, Steuerung, Ziel der Vertical Slice und bewusst nicht enthaltene Systeme ergänzen
