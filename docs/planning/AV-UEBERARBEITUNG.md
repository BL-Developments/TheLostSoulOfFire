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
| A1 | Titel | Arena-Boden als Hintergrund, Pixel-Schrift | eigenes Titelbild mit Landmarke, Logo-Typografie, ruhige Bewegung | ☐ |
| A2 | Prolog I Ufer | gemalt (Stil B), wenig Tiefe am Wasser | Wasserkante, Gischt, Dammreste, Tiefe | ◐ |
| A3 | Prolog II Suchgang | flache Platzhalter-Formen | Hafenviertel: Lagerhausmauern, Viaduktbögen, Kofferzone, Signalmast mit Suchfeuer | ☐ |
| A4 | Prolog III Damm | flache Platzhalter-Formen | gebrochener Damm mit Schienen, Anleger mit Skiff | ☐ |
| A5 | Prolog Überfahrt (Deck) | flache Platzhalter-Formen | Skiff-Deck mit Reling; versunkene Giebel und Masten ziehen vorbei | ☐ |
| A6 | Schwelle | flache Platzhalter-Formen | Warden-Architektur, monumentale Schwelle, Tür in Menschengröße, Stadtsilhouette | ☐ |
| A7 | Hub „Ashen Antechamber“ | flache Platzhalter-Formen | Vorhalle des Seelenofens: sieben Türen, Sockel, Asche, Warden-Material | ☐ |
| A8 | Arena (Gießhalle) | gemalt (Stil B) | Tiefe am Nordrand, Rosette und Ofen, Vordergrundträger | ◐ |
| A9 | Türübergang, Abschluss, Ende (Life Flame) | einfache Überblendung | gestaltete Übergänge | ☐ |

## B Figuren und Animation

| # | Figur | Ist | Ziel | Status |
| --- | --- | --- | --- | --- |
| B1 | Spieler | idle, move, swing1–3, aim gerendert | zusätzlich cannon_draw, cannon_fire (Rückstoß), dash, hit, death | ◐ |
| B2 | Hollow | idle, move, swipe gerendert | zusätzlich Treffer, Taumeln, Erholen, Tod mit Maskenbruch | ☐ |
| B3 | Burning | Ludo-Sprite | gerenderte Figur, alle Zustände (Anlauf, Ankündigung, Sturm, Erholung, Detonation) | ☐ |
| B4 | Devourer | Ludo-Sprite | gerenderte Figur, alle Zustände (Gang, Schlag, Verschlingen, Taumeln, Tod) | ☐ |
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
| D1 | Schrift: Pixel-Schrift durch gesetzte Schriften ersetzen (Titel und Fließtext, OFL) | ☐ |
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
| E1 | Musik: Titel, Ufer und Prolog, Überfahrt, Hub, Ende; Übergänge zwischen den Bereichen | ☐ |
| E2 | Atmosphären je Bereich: Ufer (Wasser, Wind, Fallblätter), Hafen, Damm, Deck, Schwelle, Hub | ☐ |
| E3 | Schritte und Kleidung (Spieler, Hollow, Devourer) | ☐ |
| E4 | UI-Töne: Navigation, Bestätigen, Zurück, Menü öffnen und schließen, Reiter, Ausrüsten | ☐ |
| E5 | Fehlende Spielsignale: Kiste, Währung, Fähigkeiten (6), Türen, Hollow-Treffer, Taumeln, Erscheinen | ☐ |
| E6 | Mix: Lautheit, Frequenzverteilung, Ducking, Stille | ☐ |

Werkzeuge: Ludo-Bank (Ableitungen erlaubt), lokale Synthese (numpy/scipy), ACE-Step v1
(Apache-2.0) für Musik, CLAP (Apache-2.0) und Spektralanalyse als Hörprobe.
Stable Audio Open ist auf Hugging Face zugangsbeschränkt (Lizenzzustimmung des Owners fehlt).

## Fortschritt

Die Commits auf `docs/visual-vertical-slice` tragen den Fortschritt; diese Liste wird
mit jedem Meilenstein aktualisiert.
