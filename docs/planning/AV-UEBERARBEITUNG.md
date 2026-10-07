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

## Arbeitsnotiz Durchgang 3 (ab 06.10.2026, abends)

Lebende Notiz für die laufenden Zyklen; nach jeder Verbesserung nachgeführt. Ziel: das
ganze Spiel auf Bastion-Niveau heben, Kampfanimationen aus Durchgang 2 bewahren.
Rahmen wie oben (nur Darstellung und Ton).

**Erledigt und im Spiel geprüft:**
- Schutthaufen der Arena ohne den hellen Bodenfleck aus der Freistellung; weicher
  Kontaktschatten statt dessen (`tools/visuals/clean_prop_halo.py`, `--check` prüft alle Props).
- Gang- und Ruhezyklen doppelt so dicht abgetastet, gleiche Bewegung und Dauer: Spieler
  (Lauf 24, Ruhe 24), Hollow (24/24), Burning (20/16), Devourer (20/20). Vorher standen die
  Posen beim Laufen 2–3 Bilder lang. Jedes zweite neue Bild ist die alte Pose (Abweichung
  ≤ 0,3/255); die Schritte bleiben auf den Aufsätzen (Phase 0,26/0,77). Nebenbei behoben: Das
  erste Ruhebild des Hollow war eine aufrechte Fehlpose (er zuckte jeden Zyklus hoch).

- Ende: Die Life Flame entzündet sich, wie ihre Spezifikation sagt, im kalten Ofen der Nordwand
  (`Arena.FurnaceHearth`, am Wandbild vermessen) statt frei über dem Boden. Glutbett, Wachsen
  aus einer niedrigen Zunge, Funken; warmes Licht auf Wand, Boden und Figur; die Kamera wendet
  sich dem Ofen zu (Herd bei ~22 % Bildhöhe, Zoom 0,6–0,9 je nach Abstand der Figur), die Figur
  dreht sich zur Flamme. Ton: Die Musik wechselt zum Schwellen-Thema, in dem sich das Motiv
  auflöst; die Flamme knistert als Mono-Schleife aus Richtung des Ofens (Rezept `life-flame`,
  CLAP „fireplace“ 0,78 + „campfire“ 0,18, etwa 3 LU unter der Musik). Rundgang protokolliert
  `TOUR_AUDIO song=threshold_theme life_flame=0.44`; Gameplay-Audiotest (10 Wellen, Ende,
  Neustart) und `validate_audio.py` (125 Assets) bestehen.

- Vorhalle: Die Aschehaufen am Wandfuß waren gestauchte Kugeln mit sehr hellem Material
  (wirkten wie weiße Kiesel); jetzt flache, unregelmäßige, dunklere Verwehungen (Plate neu,
  sonst pixelgleich). Feuerschalen mit größerer, flackernder Lichtinsel; die sechs
  Pilasterflammen leuchten erstmals ihren Stein an.
- Prolog II und III: Das Pflaster war eine gespiegelte, gekachelte Textur (Kaleidoskop-Muster).
  Jetzt einzelne Steine als ein Mesh (`kit.setts`, je Stein ein Zufallswert als Attribut,
  `kit.stone(..., random_attribute=, wet=)`), mit gekippten, abgesackten und wenigen fehlenden
  Steinen, Pfützen und echten Fugen; mittlere Helligkeit wie vorher. Gleise im Hafen auf
  nassem Schotterstreifen (sonst verschwanden die Schwellen zwischen den Steinen); das
  Schotterbett des Damms als gebrochener Stein (Voronoi) statt glatter heller Fläche.

- Prolog I: Die Bahnsteigplatten waren 2,4–3,8 m lang und 1,8 m tief und wirkten neben der
  Figur wie eine leere Fläche; jetzt 1,3–2,3 m × 1,15 m, jede leicht gesetzt und gekippt
  (`kit.paving(..., tilt=)`, Standard unverändert). Pfützen mit eigenem Zufallsstrom und nicht
  unter der Laterne (sie spiegelte sich dort als flache helle Fläche).

- Arena: Ofenlicht und Glut kamen von drei Stellen eines älteren Hintergrundbilds (orange
  Flecken mitten auf dem Boden); jetzt eine Quelle im gemalten Ofenmund, violett, mit
  Lichtspill auf den Boden und violetten Funken; kühlt nach der letzten Welle ab.
- Ton: Je zwei Varianten für Seelenspaltung, Seelenfreigabe, Erscheinen (oft gleichzeitig,
  phasten sonst), Kanonenschuss, Burning-Detonation und -Ankündigung, Spielertreffer, Währung.
  CLAP 0,86–0,99 zum Original, höchstens 0,60 zu anderen Tönen; Laufzeit- und Gameplay-
  Audiotest, `validate_audio.py` (141 Assets), `mix_report.py` (Arena ohne Ausreißer).

- Material `kit.stone`: Steine länger als die gemalte Fläche schmierten deren letzte Spalte als
  Streifen über ihre Enden (Stufen der Schwelle, Schwellen der Vorhallentüren, Deckplanken der
  Überfahrt); und aufrechte Flächen eines Bodenmaterials streiften senkrecht. Jetzt gespiegelt
  statt verlängert, und flache Flächen nehmen X/Y, aufrechte X/Z. Schwelle (mit Torhaus),
  Deck (mit Reling) und Vorhalle neu gerendert; Türflügel und Siegel pixelgleich. Auch Ufer,
  Hafen und Damm neu (Kantsteinfronten, Wassertreppe, Trümmer ohne Streifen). Das Hafenpflaster
  behält bewusst `extension="EXTEND"` (ein ruhiger Ton je Stein); gespiegelt oder mit einer
  Farbe je Stein aus der Fläche wurde es zu unruhig und dunkler (verworfen).

- Raumklang: Alle Effekte klangen in der Gießhalle so trocken wie draußen. Jetzt vorgerechnete
  Hallfahnen (`tools/audio/hall_tails.py`, Gießhalle RT60 2,4 s, Vorhalle 3,2 s), die nur in
  ihrer Halle mit dem Ton spielen (`AudioDirector.HallSends`, Anteil 0,3–0,4, höchstens sechs
  gleichzeitig): Treffer, Kanone, Detonation, Schlag, Tode, Seelenspaltung, Spielertreffer,
  Wellenstart in der Arena; Schritte in der Vorhalle. Prolog im Freien bleibt trocken.
  Rundgang zählt `TOUR_AUDIO hall_tails=Hub:2,Arena:12` (Prolog 0). Nachgezogen: Schritte auch in
  der Gießhalle (eigene Fahne, Anteil 0,2; eine Fahne je Ton und Halle). CLAP: mit Fahne „large
  reverberant stone hall“ 0,90–0,94, Trefferfolge nicht „washy“; Audiotests grün.

- Titel: Die Figur stand flach und grau vor der Flamme (das Toon-Material macht Licht farblos).
  Nur im Schlüsselbild ein violetter Kantenterm in ihren Materialien (`flame_rim`, Stärke 0,7,
  Exponent 4): Kopf, Schultern und Mantel lösen sich vom Nebel. Verworfen: Stärke 1,1/Exponent
  2,4 (Figur wirkte wie ein Geist).

- Seelenkanone (Owner-Wunsch: „wirkt wie eine Spielzeugpistole, sollte eine dicke Kanone sein,
  idealerweise größer beim Aufladen“): neues Modell nach 07_SOUL_CANNON und Figurenblatt, etwa
  1,2 m statt 46 cm: Reliquienkammer aus geschwärztem Eisen mit Gittern über der Death Flame,
  Schaft, dickes Rohr mit Silberbändern und zwei Leitungen, weite gerippte Mündung; Knochenweiß
  nur an Kolbenplatte und Runen (die weißen Längsstreben ließen die alte wie ein Spielzeug
  wirken). Auf dem Rücken vom linken Hüftbereich bis über die rechte Schulter; im Kampf an der
  rechten Hüfte angeschlagen, Lauf 11° nach innen, Oberkörper gegen das Gewicht zurückgelehnt.
  Sie wächst beim Laden um bis zu 30 % (Stand: stufenlos, Clip `aim` nach Ladung; Gehen:
  `aim_move`, `_2`, `_3` je Ladestufe, gleicher Zyklus, die Beine springen beim Wechsel nicht)
  und entlädt sich nach dem Schuss auf Ruhegröße. Ladeleuchten und Mündungsfeuer an der
  gezeichneten Mündung (`FigureHeights.MuzzleOf(..., charge)`); Schuss, Schaden und Timing
  unverändert. `build_player.py --recannon` ersetzt die Kanone in der bestehenden Figur. Alle
  18 Spielerclips neu gerendert; Titelbild mit der neuen Kanone (Kantenterm nur auf Körper und
  Kleidung). Geprüft: Vorschauen in vier Richtungen, Rundgang (Ziehen, Laden, Schuss, Gehen mit
  Kanone), Slice- und Fähigkeitentest, 258 Unit-Tests.

- Truhe: Offen verschmolz der Deckel (dunkle Holzinnenseite) mit dem Kasten. Jetzt mit dunkelrotem
  Samt gefüttert, vom Goldlicht getroffen, darunter ein Münzhaufen (Geld); geschlossene Truhe
  pixelgleich, Öffnen-Zeit unverändert. Verworfen: heller Samt (brannte weiß aus).

- Ton der Kanone: Ziehen (Riemen, Eisen schlägt in die Hände, Kammer rastet) und Verstauen
  (gedämpft gegen den Rücken, Riemen) an den bestehenden Zustandswechseln; Rundgang protokolliert
  Ziehen → Laden (+0,16 s) → Schuss → Verstauen (+0,28 s). Mix „action“ +2,0/+1,6 LU über dem
  Bett, Audiotests grün. Verworfen: Verstauen mit hartem Anschlag (CLAP: „gunshot“).

- Prolog-Erwachen: Die Figur stand während „YOU REMEMBER THE IMPACT“ schon aufrecht. Neuer
  Clip `wake` (30 Bilder, nach dem Prolog-Zeitgeber, `PrologueDirector.WakingDuration` 3,6 s
  unverändert): liegt bäuchlings, atmet, stützt sich ab, kniet, greift die Sense, steht auf; das
  letzte Bild ist das erste der Ruhe. Kernlicht und Seelensinn folgen der Körperhöhe
  (`Player.DrawnCoreHeight`).

- Devourer: Taumeln nach voller Kanone war kaum Bewegung, der Tod nur ein Zusammensacken in die
  Hocke. Neu (18/14 Bilder, nach den bestehenden Zeitgebern): Rückwurf mit schwerem Schritt
  zurück, Arme hochgerissen, Schlund weit, Wanken, Sammeln in die Haltung; Tod: Aufbäumen mit
  hochgeworfenen Armen, auf die Knie, Sturz nach vorn. Rundgang-Station `arena_devourer_end`
  zeigt beides (vorher war der Spieler dort schon tot).

- Arena-Licht: Die Lichtinsel des Rosettenfensters wirkte nur mit rund 0,5 % statt der gemeinten
  7 % (die additive Mischung gewichtet die vormultiplizierte Farbe ein zweites Mal mit Alpha).
  Stärke jetzt in der Farbe bei vollem Alpha (9 %/7 %, etwas größer): das Mittelschiff hat ein
  helles Herz, großflächige Helligkeitsstreuung 10,5 → 13,5 (Gauß 90 px), Figuren als dunkle
  Silhouetten darin gut lesbar.

- Präsenz der Gegner (16_AUDIO_DIRECTION verlangt sie, sie fehlte): je Art eine leise nahtlose
  Schleife in Richtung des nächsten Gegners, nach Abstand (bis 850), etwas voller bei mehreren,
  Burning im Anlauf lauter, tritt bei Gefahrensignalen zurück, aus bei Tod und außerhalb des
  Kampfs. Burning: Knistern und instabiles Grollen; Hollow: Atmen, Flüstern, Stoff; Devourer:
  Kehle, Drone, Seelenchor. Pegel unter dem Bett (etwa −9 bis −2 LU, wie die Schritte).
  Rundgang protokolliert `presence Hollow=0.10@+0.36 Burning=0.28@-0.09 Devourer=0.08@+0.56`.

- Resonanz: Während der 10 s brennt die eigene Death Flame hörbar (Rezept `resonance-rumble`:
  tiefes Grollen mit langsamen Schüben, Herzschlag 60 bpm, weiche Funken; CLAP „deep rumbling
  fire“ 0,78, keine Sirene), blendet in 0,6 s ein und 1,2 s aus; Rundgang aktiviert die Resonanz
  jetzt wirklich (R) und protokolliert `resonance=0.40`.

- Kanonenladung hörbar ansteigend (16_AUDIO_DIRECTION): Ladebrummen `cannon_hum` (Kammer,
  Seelenwimmern, Vibrieren) unter dem Ludo-Ladeton, Tonhöhe −0,4 → +0,2 und Pegel mit der Ladung,
  hält beim vollen Laden vibrierend, nach dem Schuss sofort weg; passt zur wachsenden Kanone.
  Rundgang: `hum=0.25@-0.07` beim Laden, `0.00` danach.

- Sense (Owner: „hält sie mit der Spitze nach oben statt seitlich“, „nicht so mächtig wie die
  Kanone“): neues Modell (Schaft 2 m, dicker, Lederwicklungen und Eisenringe, Klinge etwa 1 m mit
  dunklem Eisenrücken und hellem Schliff, schwerer Kragen mit Death-Flame-Kern auf beiden Seiten,
  langer Gegengewichtsdorn); reicht jetzt etwa 1,9 m weit, passend zur Trefferweite 1,7–2,1 m.
  Grundhaltung: Schaft schräg nach vorn-links, Klinge hängt seitlich herab (aus sechs von acht
  Richtungen diagonal, nie Spitze nach oben; in dieser Kamera erscheint „links der Figur“ von
  Osten als „oben“, daher nach vorn-links). Laufen, Dash, Halten mit Kanone, Tod und Erwachen
  angepasst. `build_player.py --rescythe` ersetzt die Sense und keyt alle Aktionen neu; alle 19
  Clips und das Titelbild neu gerendert; `key_idle` nutzt jetzt `weapon_idle`.
  Verworfen: Varianten A (Klinge waagrecht nach vorn: von vorn ein Haken), C (Schaft aufrecht:
  verdeckt den Körper), Schaft nach links ansteigend (von Osten senkrecht, Spitze oben).
- Hiebe ohne Linien (Owner): statt des dünnen Flammenbands eine breite, weiche Wischspur über die
  ganze überstrichene Klingenfläche (Shader-Pass `Smear`, Kragen bis knapp hinter die Spitze,
  zur Spitze und zum neuen Ende heller, weich auslaufend); Spitzenbahnen der Klinge aus den
  Keys im Spiel (`ScytheBladePaths.TipAt`). Der dritte Hieb zeigt eine helle Sichel.
- Warnzeichen ohne Linien: Ringe und Wurfbogen als weiche Zonen (innen schwach gefüllt, zum
  Rand heller, weicher Rand exakt auf dem Radius) statt Haarlinien; gilt überall (Wellenstart,
  Erscheinen, Detonation, Schlag, Truhe, Fähigkeiten, Hollow-Griff).

- Schläge mächtiger (Owner): Wuchtschicht `scythe-weight-1/2/3` unter jedem Ludo-Schwung
  (schwerer Luftstoß mit Maximum zur Kontaktzeit, Auflodern der Flamme, dunkler Klingenklang;
  dritter Hieb mit tiefem Druckstoß; CLAP „heavy sword swing whoosh“ 0,99), etwa +2,6/+4,0/+6,2 LU
  über dem Bett; jeder Treffer mit Druckstoß (`HitHeavy` 0,32/0,42/0,62), `ScytheHit` lauter
  (0,82/0,95, jetzt +5,7 LU). Treffer bleiben über den Schwüngen.
- Seelen einsammeln subtiler (Owner): der helle Ludo-Freigabeton (8,4 kHz, CLAP „piercing
  electronic beep“ 0,64) durch eine weiche tiefe Glocke ersetzt (`soul-release-soft`, 1,8 kHz,
  „bell“ 0,64, „beep“ 0,09); schnell aufeinanderfolgende Freigaben werden je um Faktor 0,62
  leiser (bis 0,3), und nur die erste einer Folge duckt die Musik (vorher pumpte sie bei jeder).
  Gleiches Abklingen für die Währungstöne.

- Seelen einsammeln ohne Linien (Owner: „muss nicht mal Licht haben, reicht, wenn die
  Seelen-Inkremente nach der Erlösung zum nächsten Ziel fliegen“): während der Erlösung kein
  Strahl und kein Ring mehr; danach fliegt jedes Seelenteil als kleine Seele mit kurzem Schweif
  zur Figur und steigt dabei zum Kern auf (Flugzeit und Resonanzgewinn unverändert).
  Verworfen: Lichtstrom aus Punkten zwischen Seele und Kern (Owner wollte kein Licht, nur Flug).
- Tod: Das Licht der Death Flame lag ab dem ersten Bild am Boden, wo der Körper erst nach 1,3 s
  liegt. Jetzt sinkt das Kernlicht mit dem Fall und flammt auf, wenn die Flamme den Körper nimmt
  (0,85 s, wie die Darstellung); `Player.SinceDeath` als Darstellungszeit.
- Arena-Intro, Wellenwechsel und Tod: die Ringe um Figur und Hallenmitte durch weiches,
  atmendes Licht ersetzt (die Flamme sammelt sich, statt als Kreislinie zu erscheinen).
- Weitere Ringe durch Licht ersetzt: ungeöffnete Truhe (Goldschimmer am Boden), Resonanz aktiv
  (Aura), Resonanz bereit (pulsierender Kern), Wellenstart (weiche Lichtfläche; der Hinweis
  erscheint beim Betreten). Bleiben als Zonen: Erscheinen von Gegnern, Warnungen der Gegner,
  Fähigkeitsbereiche (sie tragen Ort oder Reichweite eines Angriffs).
- Seelensinn ohne Striche: Spuren der Echos (Prolog, Arena) und der Weg durch Tür I in der
  Vorhalle als Lichtreste, die einzeln flackern und langsam in Gehrichtung treiben
  (`SoulSensePresentation.DrawResidueTrail`); Knoten als weiches Licht statt Kreise.
- Warden-Flammen als Klangquellen: In der Vorhalle waren die Flammen in die Atmosphäre eingebacken
  und standen still, wo immer man ging. Jetzt flattern Feuerschalen (voll) und Wandleuchter
  (schwächer) dort, wo sie brennen, lauter im Vorbeigehen, auch die Flammen des Prologs
  (`warden-flame`, CLAP „torch flame“ 0,92; `PresenceSource.WardenFlames`, im Seelensinn
  leiser). Im Vorbeigehen 5 LU unter Bett und Musik, direkt daneben gleichauf.
  `ambience-hub` ohne die alte Flammenschicht neu erzeugt (gleicher Seed). Rundgang protokolliert
  Pegel und Seite (Vorhalle 0,19 rechts beim Ankommen, 0,53 mittig nach dem Gang nach rechts).
- Ofen der Gießhalle als Klangquelle: dieselbe Death Flame wie die Warden-Flammen, tiefer
  gestimmt (Tonhöhe −0,3; CLAP gestimmt „torch flame“ 0,77), aus der Nordwand, lauter zur Wand
  hin (Mitte etwa −10 LU, an der Wand −4 LU unter dem Bett), im Ende übernimmt die Life Flame.
  Verworfen: eigenes Rezept `furnace-breath` (CLAP „wind“ bis 0,74 bzw. „thunder“ bis 0,46 –
  tiefes Brausen klingt nach Wind, dumpfes Aufwallen nach Donner).
- Schwelle: Die Titelkarte „YOU ARE NOT ALONE“ stand genau über der Figur im Tor; jetzt im
  Dunkel über dem Tor, die Raute der unteren Trennlinie sitzt über der Torflamme.
- Burning ohne Blase, Strahl und Bodenring: Beim Aufladen der Detonation saß der Kern in einer
  Seifenblase (Ring, Striche zu den Bruchstellen, harte weiße Scheibe), beim Anlauf zog ein
  gerader Strahl hinterher, die Druckwelle war ein Ring. Jetzt sammelt sich Licht im Kern und
  zieht sich zusammen (die Zeit bis zur Detonation bleibt lesbar), aus den Bruchstellen fließen
  Lichtreste hinein, hinter dem Anlauf zieht ein Kielwasser aus Glut, die Druckwelle ist ein sich
  ausbreitender Lichtschein. Die Warnzone vor dem Anlauf (Ring und Bahn) bleibt als Gefahrenzone.
- Letzte Zierringe und Striche im Spiel ersetzt: Beim Erscheinen eines Gegners wird Licht von
  ringsum in den Punkt gezogen und sammelt sich dort (vorher zwei Ringe); der Sog zeigt seine
  Reichweite als weichen Schein (die kreisenden Lichtpunkte zeigen den Zug); Schutz und
  Vergeltung als atmende Aura statt Ring; Glutfunken mit verblassenden Lichtkopien statt
  Strich. Stationen `arena_spawn` (neu, Welle 5) und Fähigkeitentest. Nur Rückfallwege ohne
  Grafik zeichnen noch Linien und Ringe.
- Ringe aus den Flipbooks genommen (`vfx_kit.py`, neues `wash`): Kern-Treffer, Ladung der Kanone,
  Bodenring der Detonation, Freigabe der Seele, Bodenringe der Resonanz sind jetzt Lichtschein ohne
  Umriss (Vorher-nachher-Vorschau und Stationen `arena_player`, `arena_burning`).
- Verschlingen des Devourer ohne Ring und Strahl: Die gejagte Seele steckte in einer harten weißen
  Ringblase, ein gerader Strahl führte in den Rumpf (wie Fadenkreuz und Laser). Jetzt glimmt die
  Zielseele beim Anpirschen und ein dünner Zug ihres Lichts neigt sich schon zur Brust; beim
  Verschlingen fließen ihre Lichtreste beschleunigend in die Brust, die Seele flackert, die Brust
  glimmt auf (`WorldMarks.Stream`/`Glow`). Neue Station `arena_devour` (20 Aufnahmen).
- Wenig Leben (30 % oder weniger) meldete nur die Lebensleiste in der Ecke, obwohl der Blick im
  Kampf auf der Figur liegt. Jetzt pocht die gebundene Seele: dumpfer Doppelschlag in Gis
  (`soul-throb`, CLAP „slow heartbeat“ 0,65–0,68 + „muffled heartbeat“ 0,20–0,25), etwa 58 Schläge je
  Minute an der Schwelle, 78 kurz vor dem Tod, der erste 0,45 s nach dem Treffer (nicht auf dem
  Schmerzlaut). Im selben Takt rückt der Bildrand dunkel heran und glimmt violett, die
  Lebensleiste schlägt mit (vorher gleichmäßiger Sinus). Nur in Kämpfen, nicht im Ende.
  `LowHealthPresentation`, Rundgang `arena_death_low` (18 Aufnahmen, Takt im Protokoll).
- Seelensinn subtiler (Owner: „das Licht ist viel zu krass“): Kerne der Hollows, Bruchstellen der
  Burning und der Rumpf des Devourer als kleine violette Lichtpunkte statt flacher Scheiben mit
  Ringen und weißer Blüte; Lichtdurchgang im Seelensinn stark gesenkt (Hollow-Kern 0,38 weiß →
  0,14 violett, Devourer etwa halbiert, Seelen 1,42× → 1,12×), Spuren halb so hell, Augen ohne
  Linie zum Kern. Schwachpunkte bleiben als Punkte erkennbar.
- Eigener Treffer: weicher Lichtstoß aus dem Körper statt hellem Ring; Dash: weiche Flammenspur
  statt zweier gerader Strahlen.

- Hollow-Taumeln (16 Bilder, Zeitgeber unverändert): Rückwurf mit hochgerissenen Armen und Maske,
  zwei Stolperschritte zurück, benommenes Wanken, Zurückschlurfen in die Haltung; im Rundgang
  (`arena_wave1_hollow_stagger`) zu sehen.

- Rundgang-Station `arena_swings`: die Kombo in alle acht Richtungen, je eine Aufnahme zur
  Kontaktzeit jedes Hiebs. Sense und Wischspur lesen sich von allen Seiten.
- Wellenstart-Markierung der Zwischenpause: eine ruhige, atmende Zone statt zweier kräftiger Ringe
  (mit den Zonen-Texturen doppelte sich die Linie); Radius unverändert.

**Geprüft (Stand 07.10.2026, 00:45):** voller Rundgang über 21 Stationen (611 Aufnahmen, kein
Platzhalter; neu `arena_devour`, `arena_spawn`, Serien `arena_death_low` und `arena_swings_sweep`),
CPU je Frame 0,25–0,57 ms, Wandzeit im Mittel höchstens 1,6 ms (ohne den Titel mit dem
Programmstart); 263 Unit-Tests; Fähigkeiten-, Vorhallen- und Währungstest; Audio-Laufzeit- (64
Töne, kein Ersatzklang), Gameplay- (10 Wellen) und Tod-Neustart-Test; `validate_audio.py` (172
Assets); `mix_report.py` in Arena, Überfahrt, Vorhalle und Ufer ohne Ausreißer.

**Nächste Schritte (nach Wirkung):**
1. Hörabnahme durch den Owner: Wuchtschichten der Hiebe, sanfte Seelenglocke, Präsenz der
   Gegner, Hallfahnen, Ladebrummen, Resonanzgrollen, Feuer der Life Flame, Ende-Musik, Pochen
   bei wenig Leben, Warden-Flammen in Vorhalle und Prolog, Ofen der Gießhalle.
2. Sichtabnahme der neuen Sense und der Wischspur mit eigener Steuerung. Automatisch geprüft
   (Station `arena_swings`, Serie `sweep`, kreisende Maus während der Combo): Figur und Spur
   behalten die Richtung, mit der der Hieb begann, der nächste Schlag dreht zur neuen Mausrichtung,
   und keine verlöschende Spur schwenkt mit. Bleibt nur das Gefühl unter echter Hand.
3. Lampenscheibe am Ufer meldet `clean_prop_halo.py --check` (echt, kein Fehler).

**Befunde, offen (nach Wirkung):**
1. Sprache: Erzähltexte englisch, HUD deutsch (Inhaltsfrage, nicht ohne Owner ändern).
2. Warnzonen (Ausholbogen des Hollow, Anlaufring und -bahn des Burning, Schlagzone des
   Devourer) sind bewusst geblieben: weiche Zonen, aber mit hellerem Rand. Sie zeigen den
   Wirkbereich eines Angriffs; ob sie noch weicher werden sollen, entscheidet der Owner nach
   dem Spielen (Lesbarkeit geht vor).

**Hinweis:** Nach `place_environment.py` immer `dotnet build` vor dem Rundgang, sonst zeigt
das Spiel noch die alten Kacheln (Content-Pipeline).

**Geprüft, kein Mangel:** Nordwand im Endbild (der vermutete Dunstschleier ist nicht messbar;
Luftperspektive auf die Wand begrenzt: Kontrast unverändert, Änderung zurückgenommen). Hollow-
Taumeln und -Tod, Burning-Tod, Burning-Angriff, HUD, Seelensinn der Vorhalle.

**Verworfen:**
- Wellen an der Ufer-Kante als eigene Klangquelle: synthetische Wellen hörte CLAP als „wind“
  (über 0,9) oder, mit dichten Blasen, als „bubbles“/„toilet flushing“ – nie als Wellen am Ufer.
  Das Ufer-Bett trägt die Wellen weiter; Rezepte entfernt.
- Eigenes Ofen-Rezept (`furnace-breath`): „wind“/„thunder“; ersetzt durch die tiefer gestimmte
  Warden-Flamme.
- Alten Freigabeton nur in den Höhen absenken: Grundton selbst zu hoch, blieb ein Piepen.
- Devourer-Präsenz aus Rauschgrollen und gehauchtem Chor: CLAP hörte „wind“ (0,7–0,9); jetzt
  tonale Kehle (Pulsfolge durch Formanten) und tonaler Chor.
- Ziehende Rauchschatten über dem Arenaboden (gegen das gleichmäßige Licht): dunkelten vor allem
  die hellen Stellen ab; großflächige Helligkeitsstreuung sank von 10,5 auf 8,0 (Gauß 90 px),
  das Bild wurde nur dunkler. Besser über gezielte Lichtinseln lösen, nicht über Schatten.
- Pflaster aus Granit mit der Tönung der Kantsteine (0,56): sechs- bis achtmal zu hell, die
  Streuung je Stein ging in der Tonkurve unter.
- Feuerknistern mit kräftigem Brausen und Atemband: CLAP hörte „Wind“ (0,5–0,8); das Brausen
  ist jetzt leise und tief, das Knistern trägt.

## Arbeitsnotiz Durchgang 4 (ab 07.10.2026)

**Owner-Befund 07.10.:** Die Soul Cannon ergibt noch keinen Sinn; man versteht die Story
nicht; die Stimmung ist zu filmisch-atmosphärisch, zu wenig actionreich, spannend und etwas
verrückt, mit Klangsignalen der Monster; die Monster wirken in Bewegung und Aussehen
monoton. Beispiel: Der Devourer soll Flächenschaden nicht durch einen Kreis zeigen, sondern
durch einen harten Schlag auf den Boden, der den Boden ringsum sichtbar trifft. Sense- und
Kanonenklang sind gut; bei Atmosphäre, Spannung und Gefühl darf der Ton kreativer werden.

**Prioritäten (nach Wirkung):**
1. ☑ Devourer-Schlag als Bodenbruch statt Kreis.
2. ☑ Monster mit Stimme: Rufe beim Bemerken, Angriffsschreie, Schmerzlaute, verrückte
   Zwischenrufe, je Art eigen und lore-treu (Hollow: Greifen nach Gehenden; Burning: instabile
   Glut; Devourer: Hunger, gefangene Seelen).
3. ☐ Kampfmusik treibender: Schichten, die mit der Kampflage anziehen (Gegnerzahl, Gefahr,
   wenig Leben); weniger Flächen, mehr Puls.
4. ☐ Monster weniger monoton: Hollow-Maske lesbar, Unterschiede im Körperbau und im Gang;
   unruhige, zuckende Bewegungen.
5. ☐ Soul Cannon verständlich: Die Flamme wandert sichtbar Kern → Schulter → Arm → Kanone
   (07_SOUL_CANNON, Schritt 3), die Kammer füllt sich.
6. ☐ Story verständlich: Prolog-Zeilen sagen klar, wer man ist, wo man ist und warum man
   kämpft (ohne neue Mechanik).

**Erledigt und im Spiel geprüft:**
- Devourer-Schlag (Owner-Beispiel): Beim Ausholen sickert Licht entlang der Hauptrisse aus
  dem Boden und kriecht bis an den Rand des Schlags; lose Steine am Rand beginnen zu hüpfen,
  Staub hebt sich. Beim Aufschlag bricht der Boden: Krater mit gekippten Platten, Risse bis
  genau zum Wirkradius (sie ersetzen den Ring als Anzeige), Aufflammen der Risse, Staubwalze
  bis an den Rand, aufsteigende Staubwolke über den Figuren, große Bodenplatten und Steine
  als Trümmer, Kamerastoß nach unten mit Nachbeben auch ohne Treffer; die Risse bleiben 2,4 s
  liegen und verblassen. Unterbrochenes Ausholen blendet aus. Neu: `tools/visuals/ground_kit.py`
  (Texturen `ground_shatter`, `ground_fissures` mit 8 Stufen, `dust_puffs`),
  `Rendering/GroundImpacts.cs`. Der Abwärtsschwung des Clips liegt jetzt in den letzten 0,07 s
  des Ausholens, die Fäuste treffen im Frame der Schadensauflösung auf (vorher 0,07 s danach);
  Ausholdauer, Radius, Schaden unverändert. Ton: `ground-break` unter dem Ludo-Schlag (Risse,
  mahlende Platten, Geröll; CLAP „heavy impact cracking a stone floor“ 0,73/0,83), Mix
  +9,3 LU über dem Arena-Bett. Ohne Texturen bleibt der alte Ring als Rückfall.
  Geprüft: Rundgang `arena_devourer` (Serie vor und nach), 263 Unit-Tests, Audio-Laufzeittest
  (65 Töne, kein Ersatzklang), `validate_audio.py` (174), `mix_report.py --zone arena`.

- Monster mit Stimme (Owner: „verrückt, mit Sound Cues von Monstern“): `tools/audio/recipes/voices.py`
  baut eine Quelle-Filter-Stimme (Stimmlippenpulse mit Jitter, Schimmer, Periodenverdopplung,
  Rauheit; Atem; gleitende Vokalformanten) und einen Schrei-Baustein (halb Stimme, halb Luft,
  75–110-Hz-Rauheit, stark gesättigt). Zwischen den Angriffen rufen die Gegner
  (`Audio/EnemyVoices.cs`): Hollow klagt hauchig durch die Maske und stottert, Burning kichert
  irre und zerbricht in Knistern, Devourer knurrt mit den gefangenen Seelen darin. Jeder
  Gegner hat einen eigenen Takt, neue rufen oft kurz nach dem Erscheinen, mehr Gegner rufen je
  seltener, Abstand mindestens 0,55 s, eigene Stimmgruppe (höchstens zwei, sie nimmt Warnungen
  keine Stimme), sie treten bei Gefahrensignalen zurück, leiser mit Abstand. An Angriffen: Hollow
  schreit beim Griff auf, Burning schreit vor dem Anlauf, Devourer schnüffelt und stöhnt hungrig,
  wenn es eine Seele wittert. CLAP: „monster growling“ 0,90–0,95, „maniacal laughter“ 0,70–0,87,
  „scream“ 0,60–0,79, Hollow-Rufe „zombie groan“/„ghostly moaning voice“. Mix: alle in allen
  Zonen im Band (`mix_report.py`, neue Klasse `call` −4…+8 LU). Rundgang protokolliert
  `TOUR_AUDIO voices=…` je Station und den kürzesten Abstand zweier Rufe (0,62 s). Audio-
  Laufzeit- (71 Töne), Gameplay- (10 Wellen) und Tod-Neustart-Test grün, 263 Unit-Tests.

**Verworfen (Durchgang 4):**
- Schreie aus reiner Stimmlippenquelle durch Formanten, auch mit Biphonation: CLAP „horn“ und
  „synthesizer tone“ bis 0,57. Erst Luft durch die Formanten und tiefe Rauheit machen eine Kehle.
- Gleichmäßig getaktetes Kichern: mechanisch; jetzt ungleiche Silben, fallender Atem, Kiekser.
- Ausholrisse mit allen Verästelungen als scharfe Lichtlinien: wirkten wie violette Blitze.
  Jetzt nur die Hauptrisse als weiche Lichtnaht; alle Risse erst beim Bruch.
