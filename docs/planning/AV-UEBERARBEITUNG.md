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
| A2 | Prolog I Ufer | gemalt (Stil B), wenig Tiefe am Wasser | 3D-Bahnsteig am Meer: abgesackte Kanten, geflutetes Gleisbett, Dammreste, Pfützen, Fallblattanzeige im Wasser, gerenderte Props, Seenebel | ☑ |
| A3 | Prolog II Suchgang | flache Platzhalter-Formen | 3D-Hafenkai: nasses Pflaster, Gleise, Warden-Bohlen, Stege, versunkene Dächer, Koffer, Warden-Marken, Signalmast mit Suchfeuer | ☑ |
| A4 | Prolog III Damm | flache Platzhalter-Formen | 3D-Gleisdamm mit Bruchkanten, Signalträger, Anleger und vertäutem Skiff | ☑ |
| A5 | Prolog Überfahrt (Deck) | flache Platzhalter-Formen | stehendes Deck, scrollendes Meer, zwei Parallax-Bänder der versunkenen Stadt, Reling als Vordergrund | ☑ |
| A6 | Schwelle | flache Platzhalter-Formen | Basaltvorplatz, Warden-Mauer mit Strebepfeilern, Torhaus als Prop (man geht wirklich hindurch), Spalt mit Warden-Flamme, offener Ring | ☑ |
| A7 | Hub „Ashen Antechamber“ | flache Platzhalter-Formen | 3D-Vorhalle aus Einzelsteinen, sieben Türen (gleitende Flügel, Siegel), Pilaster mit Warden-Flammen, offener Ring, Feuerschalen | ☑ |
| A8 | Arena (Gießhalle) | gemalt (Stil B) | gemalte Schattierung (Wandfuß, Ränder, Flecken), Rauch unter der Nordwand; Rosette, Ofen und Träger bleiben aus dem Plate | ☑ |
| A9 | Türübergang, Abschluss, Ende (Life Flame) | einfache Überblendung | Türlicht und Schriftzug mit Ornament, Kampf-HUD erst nach dem Intro, Life Flame als warme Flammenschleife, Abschlusszeilen in Cinzel | ☑ |

## B Figuren und Animation

| # | Figur | Ist | Ziel | Status |
| --- | --- | --- | --- | --- |
| B1 | Spieler | idle, move, swing1–3, aim gerendert | zusätzlich cannon_draw, cannon_fire (Rückstoß), dash, hit, death | ☑ |
| B2 | Hollow | idle, move, swipe gerendert | zusätzlich Treffer, Taumeln, Erholen, Tod mit fallender Maske | ☑ |
| B3 | Burning | Ludo-Sprite | gerenderte Figur: Ruhe, Pirschen, Aufflammen, Anlauf, Erholung, Treffer, Tod | ☑ |
| B4 | Devourer | Ludo-Sprite | gerenderte Figur mit Rumpföffnung: Ruhe, Gang, Schlag, Erholung, Verschlingen, Taumeln, Treffer, Tod | ☑ |
| B5 | Seelen und Pickups | Formen und Glühen | atmende Lichtkugel mit Saum und Funken, Release-Flipbook, Strahlen zum Kern | ☑ |
| B7 | Arena-Truhe | Rechtecke | gerendertes Reliquiar mit Öffnen-Clip | ☑ |
| B6 | Trainingspuppe (Sandbox) | Platzhalter | gerendertes Prop, wankt bei Treffern | ☑ |

## C Effekte (VFX)

| # | Effekt | Status |
| --- | --- | --- |
| C1 | Sensenhiebe 1–3: Death-Flame-Band entlang der Klingenbahn (Trail-Shader) | ☑ |
| C2 | Flipbooks aus `tools/visuals/vfx_kit.py`: Core-Treffer, Mündung, Projektil, Ladung, Burning-Detonation, Seelenfreigabe, Resonanz, Dash-Zündung, Death Flame | ☑ |
| C3 | Warnzeichen als Licht (Ausholbogen, Anlaufring und -bahn, Detonations- und Schlagring, Seelenstrahlen), Bodenmarken (Wellenstart, Erscheinen) | ☑ |
| C4 | Partikel als Lichtpunkte und Funkenschlieren | ☑ |
| C5 | Fähigkeiten im Feld (Durchschlag, Sog, Marke, Schutz), Glutfunken, Resonanz-Aura | ☑ |
| C6 | Maskensplitter beim Hollow-Tod, Steinsplitter beim Devourer-Schlag | ☑ |
| C7 | Seelensinn: Spuren, Knoten, Kerne und die Echos auf der Bank als Licht | ☑ |

## D Oberfläche und Typografie

| # | Element | Status |
| --- | --- | --- |
| D1 | Schrift: Pixel-Schrift durch gesetzte Schriften ersetzen (Cinzel, Alegreya Sans; OFL) | ☑ |
| D2 | HUD in Warden-Eisen (`UiKit`): Medaillon und Lebensleiste mit Schadensspur, Dash, Währungen mit Symbolen, Wellenplakette, Resonanzplatte, Fähigkeitenkarten; Kanonenladung am Fadenkreuz | ☑ |
| D3 | Titel- und Pausenmenü, Einstellungen (Seitentitel, Werte hervorgehoben, Auswahl mit Glut und Rauten) | ☑ |
| D4 | Charaktermenü (Eisenplatte, Reiter mit Ornament, Fähigkeitenkatalog) | ☑ |
| D5 | Erzählband, Abschnittstitel, Ziele, Hinweise mit Tastenkappen, Todes- und Abschlusszeilen | ☑ |
| D6 | Dev-Menü und Sandbox-Banner | ☑ |

## E Ton

Bestand: 27 Ludo-Effekte, eine Arena-Atmosphäre, eine Arena-Musik (`docs/audio`).
Außerhalb der Arena läuft nur die Arena-Atmosphäre leise; Musik gibt es nur in der Arena.

| # | Thema | Status |
| --- | --- | --- |
| E1 | Musik: Titel, Ufer/Hafen, Damm, Überfahrt, Schwelle, Hub; Überblendung zwischen Zonen | ☑ (Hörabnahme durch Owner offen) |
| E2 | Atmosphären je Bereich: Ufer, Hafen, Damm, Deck, Schwelle, Hub | ☑ (Hörabnahme durch Owner offen) |
| E3 | Schritte des Spielers (Stein, Planken) und der Gegner (Hollow, Burning, Devourer; je vier Takes, nach Abstand und Seite) | ☑ (Hörabnahme durch Owner offen) |
| E4 | UI-Töne: Navigation, Zurück, Menü öffnen und schließen, Reiter, Werte | ☑ |
| E5 | Fehlende Spielsignale: Kiste, Währung, Fähigkeiten (6), Hub-Tür | ☑ |
| E6 | Mix: Treffer über Schwüngen, Gefahrensignale angehoben, Zonenpegel; `mix_report.py` prüft alle Cues gegen das Bett jeder Zone (alle im Band) | ☑ (Hörabnahme durch Owner offen) |

Werkzeuge: Ludo-Bank (Ableitungen erlaubt), lokale Synthese (numpy/scipy), ACE-Step v1
(Apache-2.0) für Musik, CLAP (Apache-2.0) und Spektralanalyse als Hörprobe.
Stable Audio Open ist auf Hugging Face zugangsbeschränkt (Lizenzzustimmung des Owners fehlt).

## Fortschritt

Die Commits auf `docs/visual-vertical-slice` tragen den Fortschritt; diese Liste wird
mit jedem Meilenstein aktualisiert.

## Bericht (Stand 06.10.2026, nachts)

**Geändert (nur Darstellung und Ton):**
- Alle Bereiche als 3D-Szenen mit der Spielkamera: Titel, Ufer, Suchgang, Damm, Überfahrt
  (Parallaxe, die in die Nacht zurücktritt), Schwelle, Vorhalle; Arena mit gemalter
  Schattierung. Treibender Nebel und Dunst je Raum.
- Spieler und alle Gegner als gerenderte Figuren mit vollständigen Zustandsclips; Truhe und
  Trainingspuppe als gerenderte Props.
- Effekte als Death-Flame-Licht: Sensenbänder entlang der Klinge, neue Flipbooks (Treffer,
  Kanone, Detonation, Resonanz, Dash, Seelen, Death und Life Flame), Warnzeichen als
  Lichtringe und -bahnen in unveränderter Geometrie, Partikel als Licht, Trümmer als Materie.
- Oberfläche in Warden-Eisen mit gesetzter Schrift (Cinzel, Alegreya Sans): HUD, Menüs,
  Charakter- und Fähigkeitenseiten, Hinweise mit Tastenkappen, Erzählband, Titelzeilen.
- Ton: Musik und Atmosphären je Zone mit Überblendung, Schritte von Spieler und Gegnern,
  Menü-, Truhen-, Währungs-, Fähigkeiten-, Tür- und Erscheinungstöne, Mix gegen das Bett jeder
  Zone geprüft und nachgezogen.

- Nachgezogen im zweiten Zyklus: Figuren setzen sich nach Aktionen weich (0,1 s), Nachbilder
  zeigen die echte Pose, Arena-Dunst und -Rauch weich statt Pixelformen, Gegner- und
  Treffertöne aus ihrer Richtung, nahtlose Atmosphäre der Überfahrt, Tod im Prolog wie in der
  Arena inszeniert, Schritte von Spieler und Gegnern auf den gezeichneten Fußaufsätzen
  (protokolliert: Phase 0,26 und 0,77 bei Aufsätzen auf 0,25 und 0,75).

**Geprüft:** 257 Unit-Tests; Rundgang über 17 Stationen (rund 440 Aufnahmen, Schlüsselbilder und
Bildserien gesichtet; er schlägt fehl, sobald irgendwo ein Platzhalter gezeichnet wird – keiner
gefunden). Leistung auf dem M1 Pro: CPU-Zeit je Frame im Mittel 0,3–0,6 ms; Wandzeit ohne
Bildsynchronisation (CPU und GPU) im Mittel rund 1 ms, 95. Perzentil höchstens rund 2 ms;
seit die Atmosphären und Musik aller Zonen beim Start geladen werden, bleibt jeder Frame nach
dem Start unter 12 ms (Zonenwechsel vorher bis 18 ms); Slice-,
Fähigkeiten-, Vorhallen- und Währungstest; Audio-Laufzeit-, Gameplay- und Tod-Neustart-Test;
`validate_audio.py` (75 Assets); `mix_report.py` (alle Zonen im Band); CLAP als Hörprobe für
neue Klänge.

**Grenzen und offene Punkte:**
- Ich kann nicht hören. CLAP, Spektrogramme und Lautheitsmessung sind Ersatz; die Hörabnahme
  im Spiel (Musik, Atmosphären, Schritte, Mix) steht beim Owner aus.
- Stable Audio Open ist auf Hugging Face zugangsbeschränkt (Lizenzzustimmung des Owners
  fehlt). ACE-Step läuft auf diesem Rechner nicht, solange Docker läuft: ein zweiter Versuch
  am 06.10. mit CPU-Auslagerung trieb den Swap auf 23,9 von 24,5 GB und wurde abgebrochen.
  Musik und Klänge sind deshalb lokal synthetisiert.
- Die Leistungswerte stammen von einem Mac (M1 Pro); schwächere Rechner und andere
  Grafiktreiber sind nicht gemessen.
- Kein Push und kein PR ohne Freigabe des Owners.
