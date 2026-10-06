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
  (protokolliert: Phase 0,26 und 0,77 bei Aufsätzen auf 0,25 und 0,75); die acht häufigsten
  Kampftöne mit je zwei abgeleiteten Varianten (CLAP-Ähnlichkeit zum Original 0,94–0,98,
  zu anderen Tönen höchstens 0,88), zufällig gewählt; Lautstärken in den Einstellungen als
  Leisten, Bildbewegung mit deutschen Werten, Beenden-Abfrage lesbar.

**Geprüft:** 257 Unit-Tests; Rundgang über 17 Stationen (450 Aufnahmen, auch Menü-Unterseiten,
Beenden-Abfrage, Tod im Prolog und Wellenwechsel; Schlüsselbilder und
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

## Polish-Durchgang Kampf, Bewegung und Raum (06.10.2026, abends)

Schwerpunkt nach Owner-Auftrag: Kampfanimation, Kampfton, Bewegungsqualität, Räumlichkeit.
Weiterhin nur Darstellung und Ton; Treffererkennung, Zeitfenster, Hitstop-Dauern, Balance und
Steuerung sind unverändert.

**Geändert:**
- Spieler: Die drei Sensenhiebe sind neu gekeyt (`tools/visuals/blender/build_player.py`,
  Bausteine in `combat_kit.py`). Die Beine stehen auf IK, Hüfte und Brust drehen gegeneinander, und
  jeder Hieb hat Ausholen, Kontakt und Nachschwung im bestehenden Zeitfenster, bei 14/16/26 statt 7/8/12 Frames:
  ein schneller flacher Schnitt mit Schritt rechts, eine schwerere aufsteigende Rückhand mit Schritt
  links, die Seelenspaltung mit hohem Ausholen über die Schulter und tiefem Drehausfall. Die
  Spielvorwärtsbewegung im Hieb wird im Rig zurückgerechnet, damit stehende Füße stehen bleiben.
  Im Stand sammelt sich die Figur nach jedem Hieb in die Grundhaltung zurück (`swingN_return`,
  am Combo-Zeitgeber). Neu sind außerdem: Dash (Absprung, Gleitphase, Abfangen), Treffer (Kopf und
  Brust schnappen zurück, Schritt zurück) und Kanonenrückstoß mit Rückschritt. Dazu ein Sprung
  für den Rückstoßsprung (statt Gleiten) und Gehen mit geladener Kanone (Clip nach Strecke,
  rückwärts beim Zurückweichen).
- Flammenband: folgt dem gerenderten Death-Flame-Kern im Sensenkragen in Richtung, Höhe und Zeit
  (`ScytheBladePaths.cs`, erzeugt von `tools/visuals/blade_paths.py`), zeichnet den Teil hinter
  der Figur vor ihr und erscheint erst mit dem schnellen Hieb, nicht im Ausholen. Die
  Zündfunken kommen aus dem Kragen statt vor der Figur; die Flamme leuchtet beim Durchziehen
  Boden und nahe Figuren an.
- Gegner: Hollow-Griff (Bemerken, Ausholen mit wachsender Spannung statt starrem Halten,
  Schritt, Überstrecken; 20 Frames), Erholung aus der Endpose, Treffer. Devourer-Schlag
  (Einatmen, Arme mit Stützschritt über den Kopf, Halten, Hammerschlag; 24 Frames), Erholung,
  Treffer. Burning-Ankündigung (Atem und Aufflammen, Scharren, Sprinterhocke mit Zittern),
  Anlauf mit Flugphase, Erholung (Bremsen, Keuchen, Aufrichten), Treffer.
- Treffer: Figuren blitzen auf (Shader `SpriteLit.fx`), stauchen und kippen mit dem Schlag und
  wippen zurück; ein Rücklicht je Raum hebt sie vom Boden ab.
- Kamera: weiches Rauschen statt Zufallssprünge beim Schütteln, Stoß als gedämpfte Feder, kurzes
  Heranlehnen bei schweren Treffern (nur Bild, nicht die Mauszuordnung), leichte Führung in Ziel-
  und Laufrichtung. Blitze brechen als Licht vom Treffpunkt aus; der Aufprallmoment
  verdunkelt zum Rand hin statt das ganze Bild zu schwärzen. Schüttelstärken leicht gesenkt.
- Raum: Schlagschatten aller Figuren in Richtung des Raumlichts und weg vom stärksten nahen
  Licht; Luftperspektive auf dem Boden (fern dunstig, nah dunkel); Lichtinsel der Fensterrose
  mit treibendem Staub in der Gießhalle; wenige unscharfe Schwebeteilchen mit Parallaxe vor der
  Szene. Warnringe ohne den eckigen Lichtrand, der schon vorher bei großen Ringen sichtbar war.
- Ton (`tools/audio/recipes/combat.py`): Unter jedem Treffer liegen jetzt ein Kontakt-Transient
  auf Sample 0 (der Ludo-Treffer setzt erst nach 40–55 ms ein) und das Material des Ziels
  (Stoff/Porzellan, Glutkruste, Fleisch, Holz). Seelenspaltung und volle Kanone bekommen einen kurzen
  Druckstoß, der Spieler einen Körpertreffer. Hollow und Devourer kündigen ihr Ausholen hörbar an,
  der Burning faucht beim Losstürmen, jeder Gegner stirbt mit eigenem Klang. Je 2–3 angeglichene
  Takes. Gefahrensignale lassen neu startende Schwünge und Schritte kurz zurücktreten, und viele
  gleichzeitige Töne kommen leiser hinzu. Schwung 1 ohne den leisen Anlauf (`retime.py`), damit er
  bei der Klingenspitze voll ist.

**Geprüft:** Vorschauen jedes neuen Clips in zwei Richtungen; Rundgang (474 Aufnahmen, neu: Laden im
Rückwärtsgehen, Rückstoßsprung) mit Bildserien von Combo, Gegnerangriffen, Treffern und
Massenkampf, Vorher-Nachher-Vergleiche; Slice-, Fähigkeiten-, Vorhallen- und Währungstest; 258
Unit-Tests; `validate_audio.py` (124 Assets); `mix_report.py` in allen vier Zonen ohne Ausreißer;
Audio-Laufzeit-, Gameplay- (10 Wellen) und Tod-Neustart-Test. Leistung auf dem M1 Pro: CPU je Frame
im Mittel 0,2–0,45 ms; Wandzeit im Mittel rund 1 ms, 95. Perzentil höchstens 3,9 ms.

**Grenzen:**
- Ich kann nicht hören. Die neuen Schichten sind nach Absicht, Spektrogramm, Lautheit und
  Einsatzzeit gebaut; CLAP unterscheidet bei so kurzen synthetischen Treffern das Material nicht
  verlässlich (auch der Ludo-Treffer allein gilt ihm als „Holzpfosten“). Die Hörabnahme der
  Materialschichten, Ausholwarnungen und Tode steht beim Owner aus.
- Figuren haben acht gerenderte Richtungen: Das Flammenband folgt dem genauen Zielwinkel, die
  Figur dem nächsten Achtel; bis zu 22,5° Versatz bleiben.
- Schlägt der Spieler im vollen Lauf, gleiten die Füße der Hiebe (das Spiel bewegt ihn währenddessen
  mit voller Geschwindigkeit; Beine und Oberkörper sind ein Sprite). Stehend und beim Vorwärtsschritt
  des Hiebs stehen sie.
- Gegner rutschen beim Rückstoß (Spielposition) im Trefferclip; Kippen und Stauchen mildern das.
- Die Bewegung der übrigen Gegnerclips (Gehen, Taumeln, Tod) stammt aus dem vorigen Durchgang.
- Kein Push und kein PR ohne Freigabe des Owners.
