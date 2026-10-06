# Audiovisuelle Überarbeitung: Arbeitsliste

Stand: 06.10.2026. Lebende Liste für die Überarbeitung von Bild, Animation und Ton im
ganzen Spiel. Sie baut auf dem Change `add-visual-vertical-slice` auf (Stil B: gemalte
Böden aus FLUX.2 klein, Figuren aus Blender mit MPFB2, Toon-Material und Kontur).

**Rahmen:** Nur Präsentation. Regeln, Balance, Steuerung, Treffererkennung und
spielrelevantes Timing bleiben unverändert. Animationen und Töne werden an bestehende
Zustände und Zeitgeber gekoppelt, nie umgekehrt. Keine bezahlten Aufrufe.

**Prüfung:** `--tour-visual-test` läuft ohne Eingabe durch alle Bereiche und Zustände und
legt Standbilder und Bildserien unter `artifacts/tour/<lauf>/` ab (Filter:
`TOUR_ONLY=arena,hub`). Dazu `--slice-visual-test`, `dotnet test` und die Audio-Tests.

Legende: ☐ offen · ◐ in Arbeit · ☑ erledigt und im Spiel geprüft

## Leitlinie räumliche Tiefe (Owner-Wunsch 06.10.2026)

Das Spiel soll nicht flach wirken. Umgebungen und Props entstehen deshalb als 3D-Szenen in
Blender und werden mit derselben Kamera wie die Figuren gerendert: orthografisch, 35°
Erhebung, Licht von links oben. Den Boden staucht die Szene so, dass Weltkoordinaten
1:1 auf dem Bild liegen. Echte Höhen, Schlagschatten und Umgebungsverdeckung
erzeugen die Tiefe. Hohe Teile werden als eigene Ebenen gerendert und nach Fußpunkt
sortiert oder als Occluder durchscheinend. Dazu kommen Parallaxe für ferne Ebenen,
Bodennebel und Normal-Map-Licht der Death Flame.

## A Umgebungen

| # | Bereich | Ist (Rundgang 06.10.) | Ziel | Status |
| --- | --- | --- | --- | --- |
| A1 | Titel | Arena-Boden als Hintergrund, Pixel-Schrift | 3D-Schlüsselbild mit der Spielfigur am Ufer, Cinzel-Titel, driftendes Bild, atmende Flamme, Nebel; Menü rechts | ☑ |
| A2 | Prolog I Ufer | gemalt (Stil B), wenig Tiefe am Wasser | Wasserkante, Gischt, Dammreste, Tiefe | ◐ |
| A3 | Prolog II Suchgang | flache Platzhalter-Formen | 3D-Hafenkai: nasses Pflaster, Gleise, Warden-Bohlen, Stege, versunkene Dächer, Koffer, Warden-Marken, Signalmast mit Suchfeuer | ☑ |
| A4 | Prolog III Damm | flache Platzhalter-Formen | 3D-Gleisdamm mit Bruchkanten, Signalträger, Anleger und vertäutem Skiff | ☑ |
| A5 | Prolog Überfahrt (Deck) | flache Platzhalter-Formen | stehendes Deck, scrollendes Meer, zwei Parallax-Bänder der versunkenen Stadt, Reling als Vordergrund | ☑ |
| A6 | Schwelle | flache Platzhalter-Formen | Basaltvorplatz, Warden-Mauer mit Strebepfeilern, Torhaus als Prop (man geht wirklich hindurch), Spalt mit Warden-Flamme, offener Ring | ☑ |
| A7 | Hub „Ashen Antechamber“ | flache Platzhalter-Formen | 3D-Vorhalle aus Einzelsteinen, sieben Türen (gleitende Flügel, Siegel), Pilaster mit Warden-Flammen, offener Ring, Feuerschalen | ☑ |
| A8 | Arena (Gießhalle) | gemalt (Stil B) | Tiefe am Nordrand, Rosette und Ofen, Vordergrundträger | ◐ |
| A9 | Türübergang, Abschluss, Ende (Life Flame) | einfache Überblendung | gestaltete Übergänge | ☐ |

## B Figuren und Animation

| # | Figur | Ist | Ziel | Status |
| --- | --- | --- | --- | --- |
| B1 | Spieler | idle, move, swing1–3, aim gerendert | zusätzlich cannon_draw, cannon_fire (Rückstoß), dash, hit, death | ☑ |
| B2 | Hollow | idle, move, swipe gerendert | zusätzlich Treffer, Taumeln, Erholen, Tod mit fallender Maske | ☑ |
| B3 | Burning | Ludo-Sprite | gerenderte Figur: Ruhe, Pirschen, Aufflammen, Anlauf, Erholung, Treffer, Tod | ☑ |
| B4 | Devourer | Ludo-Sprite | gerenderte Figur mit Rumpföffnung: Ruhe, Gang, Schlag, Erholung, Verschlingen, Taumeln, Treffer, Tod | ☑ |
| B5 | Seelen und Pickups | Formen und Glühen | gemalte Seelenflamme, Release | ☐ |
| B6 | Trainingspuppe (Sandbox) | Platzhalter | gerendertes Prop | ☐ |

## C Effekte (VFX)

Sense-Schläge 1–3 und Seelenhieb, Mündung, Projektil und Ladung der Soul Cannon,
Core-Treffer, Seelen-Release, Dash-Zündung, Resonanz, Burning-Detonation,
Devourer-Schlag, Gegner-Erscheinen, Death-Flame-Spur, Kisten-Öffnen. Status: ☐ (alte
Ludo-Sprites, teils Pixelformen).

## D Oberfläche und Typografie

| # | Element | Status |
| --- | --- | --- |
| D1 | Schrift: Pixel-Schrift durch gesetzte Schriften ersetzen (Cinzel, Alegreya Sans; OFL) | ☑ |
| D2 | HUD: Leben, Dash, Resonanz, Fähigkeitenkarten, Wellenanzeige, Währungen | ☐ |
| D3 | Titel- und Pausenmenü, Einstellungen | ☐ |
| D4 | Charaktermenü (Charakter, Karte, Skills, Fähigkeiten) | ☐ |
| D5 | Erzählzeilen, Abschnittstitel, Hinweise, Todesbildschirm | ☐ |
| D6 | Dev-Menü (nur Sandbox) | ☐ |

## E Ton

Bestand: 27 Ludo-Effekte, eine Arena-Atmosphäre, eine Arena-Musik (`docs/audio`).
Außerhalb der Arena läuft nur die Arena-Atmosphäre leise; Musik gibt es nur in der Arena.

| # | Thema | Status |
| --- | --- | --- |
| E1 | Musik: Titel, Ufer/Hafen, Damm, Überfahrt, Schwelle, Hub; Überblendung zwischen Zonen | ☑ (Hörabnahme durch Owner offen) |
| E2 | Atmosphären je Bereich: Ufer, Hafen, Damm, Deck, Schwelle, Hub | ☑ (Hörabnahme durch Owner offen) |
| E3 | Schritte des Spielers (Stein, Planken; Varianten); Gegner-Schritte offen | ◐ |
| E4 | UI-Töne: Navigation, Zurück, Menü öffnen und schließen, Reiter, Werte | ☑ |
| E5 | Fehlende Spielsignale: Kiste, Währung, Fähigkeiten (6), Hub-Tür | ☑ |
| E6 | Mix: Treffer über Schwüngen, Schaden und Gefahrensignale angehoben, Zonenpegel | ◐ |

Werkzeuge: Ludo-Bank (Ableitungen erlaubt), lokale Synthese (numpy/scipy), ACE-Step v1
(Apache-2.0) für Musik, CLAP (Apache-2.0) und Spektralanalyse als Hörprobe.
Stable Audio Open ist auf Hugging Face zugangsbeschränkt (Lizenzzustimmung des Owners fehlt).

## Fortschritt

Die Commits auf `docs/visual-vertical-slice` tragen den Fortschritt; diese Liste wird
mit jedem Meilenstein aktualisiert.
