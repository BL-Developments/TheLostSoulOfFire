## Why

Der Feature-Branch `prototype/soulfire-mvp` hat die einfache Fire-Raid-Vertical-Slice zu einem eigenständigen Soulfire-Kampfprototyp mit vollständigem Arsenal, Gegnerrollen, Seelenmechanik und audiovisueller Präsentation ausgebaut. Diese bereits implementierten Neuerungen benötigen nachträglich präzise, testbare Anforderungen, damit ihr Verhalten bei weiterer Entwicklung erhalten bleibt.

## What Changes

- Den Nahkampf durch eine dreistufige Sensenkombo mit Soul Cleave, Trefferreaktionen und schwachpunktabhängigen Boni erweitern.
- Einen richtungsgebundenen Ignition Dash mit Unverwundbarkeit, Abklingzeit und Resonance-Verstärkung einführen.
- Die Soul Cannon als aufladbaren Fernangriff mit drei visuellen Ladestufen, Vollaufladungsbonus und besonderen Gegnerinteraktionen einführen.
- Soul Sense als gehaltenen Wahrnehmungsmodus einführen, der die Welt dämpft, Seelenstrukturen und Schwachpunkte hervorhebt und außerhalb von Resonance die Bewegung verlangsamt.
- Getötete Manifestationen in einen automatischen Soul-Release-Ablauf überführen, dessen erfolgreicher Abschluss Resonance aufbaut.
- Resonance als manuell aktivierbaren, zeitlich begrenzten Verstärkungszustand für das bestehende Arsenal einführen.
- Hollow, Burning und Devourer als unterscheidbare Gegnerrollen mit eigenen Telegraphen, Angriffen, Schwachpunkten und Seeleninteraktionen einführen.
- Den Prototyp als inszenierten Arena-Showcase mit Titelzustand, Kampfphasen, Wellen, Abschluss und Neustart strukturieren.
- Kampfaktionen, Zustandswechsel und Seelenmomente durch Hitstop, Kamerareaktion, Licht, Partikel, HUD und authored Audio klar lesbar machen.

## Capabilities

### New Capabilities

- `scythe-and-ignition-dash`: Dreistufige Sensenkombo, Soul Cleave und richtungsgebundene Ausweichbewegung.
- `soul-cannon`: Aufladbarer Fernangriff mit Ladestufen, Vollaufladung und spezialisierten Trefferfolgen.
- `soul-sense`: Gehaltener Wahrnehmungsmodus für Seelen, Schwachpunkte und reduzierte Weltdarstellung.
- `soul-release`: Zustandsbasierter Übergang von besiegter Manifestation zu freigesetzter Seele und Soul Residue.
- `resonance`: Aufbau, Aktivierung, Dauer und Verstärkung des bestehenden Spieler-Arsenals.
- `soulfire-enemy-roster`: Hollow, Burning und Devourer mit klar getrennten Kampf- und Seelenrollen.
- `arena-showcase-flow`: Titel, Wellenfortschritt, Arenaabschluss, Tod und Neustart des Prototyps.
- `cinematic-combat-presentation`: Lesbarkeitsregeln für Sprites, Soulfire-Licht, VFX, Hitstop, Kamera und HUD.
- `adaptive-game-audio`: Zentral gesteuerte Musik, Ambience und priorisierte Audio-Cues für Kampf und Spielzustände.

### Modified Capabilities

Keine. Im Haupt-Spec-Verzeichnis existieren noch keine archivierten Capabilities; die älteren Anforderungen liegen weiterhin im offenen Change `build-core-fire-raid-loop`.

## Impact

- Betrifft den zentralen Arena- und Spielzustand, Spieler-, Waffen-, Gegner- und Seelenlogik sowie Rendering, Effekte, HUD und Audio.
- Erweitert die MonoGame-Content-Pipeline um produktionsnahe Sprite-, VFX-, Musik-, Ambience- und SFX-Assets.
- Nutzt MonoGame.Extended weiterhin für die bestehende Partikeldarstellung und ergänzt keine Online-, Datenbank- oder Netzwerkabhängigkeiten.
- Dokumentiert den Ist-Zustand des bereits gemergten Feature-Branches; es werden keine Breaking Changes oder Datenmigrationen eingeführt.
