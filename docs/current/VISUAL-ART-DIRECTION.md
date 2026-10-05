# Visual Art Direction

> **Gültige Richtung:** gemalte, hochaufgelöste 2D-Grafik wie Bastion (Beschluss vom 02.10.2026). Pixel-Art-Vorgaben sind aufgehoben. Frühere Fassungen stehen in der Git-Historie.

## 1. Zielbild

**The Lost Soul of Fire** soll wie ein hochwertiges, eigenständiges 2D-Action-Roguelike mit starkem räumlichem Eindruck wirken. Die Welt bleibt klar 2D, soll aber durch Layering, Maßstab, Kontakt, Licht und Überlagerung fast dreidimensional gelesen werden.

Der Stil heißt **Soulfire Gothic**:

- menschliche Industrie- und Alltagsorte;
- sakrale Monumentalität;
- melancholische, gemalte 2D-Grafik in hoher Auflösung (Stilreferenz: Bastion);
- tote oder zwanghaft weiterlaufende Infrastruktur;
- violett-weiße Death Flame;
- seltene warme orange Life Flame;
- moderne, kontrollierte Licht- und VFX-Unterstützung.

## 2. Stilreferenz Bastion und ergänzende Qualitätsprinzipien

Bastion ist die primäre Stilreferenz für gemalte, hochaufgelöste 2D-Grafik. Children of Morta bleibt eine ergänzende **Qualitäts- und Prinzipienreferenz** für Raumtiefe, Figurenlesbarkeit und Kamera. Keine Referenz ist eine Vorlage zum Kopieren.

Zu übersetzende Prinzipien:

- detailreiche Grafik mit klaren Silhouetten;
- starke räumliche Tiefe trotz 2D-Grundlage;
- große Umweltmassen statt flacher Kampfbretter;
- sichtbarer Bodenkontakt für Figuren und Props;
- lesbare Charaktere in dunklen, detailreichen Räumen;
- Vordergrund-/Hintergrundüberlagerung;
- Atmosphäre und weiches Licht, ohne Formen zu verwischen;
- Umgebungen, die bewohnt, benutzt und historisch gewachsen wirken;
- emotionale Nähe zwischen Figuren trotz düsterer Welt;
- deutliche Animation-Pose und Impact-Lesbarkeit.

Nicht übernehmen:

- konkrete Figuren, Familienkonstellationen oder Storymotive;
- Architektur, Gegner, Räume oder Asset-Designs;
- Palette oder Beleuchtung als 1:1-Rezept;
- UI, Perspektive oder Animationen als direkte Kopie;
- generische „Morta-like“-Fantasy.

## 3. Soulfire-Eigenständigkeit

Jede Referenz muss durch die Lore transformiert werden:

| Referenzprinzip | Soulfire-Übersetzung |
|---|---|
| räumliche 2D-Komposition | Death-Layer-Architektur mit sichtbarer Höhe, Überlagerung und metaphysischen Brüchen |
| warme Familiennähe | Beziehung der Brüder und Warden-Gemeinschaft innerhalb einer kalten Zwischenwelt |
| Umweltgeschichte | konkrete menschliche Orte, Ereignisse und emotionale Residuen |
| moderne Beleuchtung | Licht erklärt Form und Flammenbedeutung, nicht bloß „mehr Glow“ |
| klare Action | Death-Flame-Telegraphe, präzise Silhouetten und ruhige Kampfflächen |

## 4. Raumtiefe

Empfohlene visuelle Schichten:

1. Void oder entfernte Erinnerungslandschaft.
2. Ferne Architektur und große Silhouetten.
3. struktureller Hintergrund.
4. Grundfläche.
5. Bodendetails und Materialwechsel.
6. niedrige Props.
7. Spieler, Gegner und kampfrelevante Objekte.
8. hohe Props und Architektur im Spielraum.
9. kontrollierte Occluder.
10. Vordergrundrahmen.
11. Atmosphäre.
12. übernatürliche Emission und VFX.
13. HUD.

Layering muss Spielbarkeit erhöhen, nicht verstecken. Vordergrundobjekte dürfen Telegraphe und Spieler nur kontrolliert überdecken.

## 5. Komposition großer Karten

Für die großen Death-Layer-Karten:

- dominante Landmarken zur Orientierung;
- klare Hauptwege plus begrenzte, lohnende Abzweigungen;
- ruhige Kampfbereiche zwischen dichten Randzonen;
- erkennbare Übergänge zwischen Teilräumen;
- starke Maßstabswechsel von intimen Spuren zu monumentalen Ruinen;
- Aussichtspunkte, die spätere Wege und das Warden-Gefährt zeigen können;
- wiederkehrende visuelle Marker für Warden-Sicherheit und Lost-Soul-Gefahr.

„Wie Diablo“ meint größere navigierbare Flächen und Reisegefühl, nicht Loot-Überladung oder optische Nachahmung.

## 5a. Bildsprache — gültig seit 02.10.2026

Detailmenge ist keine Qualität. Hochaufgelöste, gemalte Assets werden weich skaliert; lineare Filterung und Mipmaps sind die festgelegte Richtung. Full-HD ist der erste Ausgabeschritt, kein festes Asset-Pixelraster.

- Ein Schlüssellicht von oben links macht Formen derselben Figur lesbar.
- Wenige Materialfamilien mit klarer Werthierarchie statt einer unkontrollierten Farbliste.
- Silhouette und große Formen vor winzigem Ornament.
- Übernatürliche Emission bleibt ein begrenztes Budget; Lichtakzente dürfen Gesicht, Hände und Waffenführung nicht verdecken.
- Ein gesättigter Akzent pro Figur.

Die historischen Regeln „ein gezeichnetes Pixel ist ein Bildschirmpixel“, harte Wertstufen ohne Anti-Aliasing/Dithering und zweipixelige Mindestformen gelten nicht mehr. Detaillierte Rig- und Animationsregeln unten beschreiben die Qualitätsrichtung; ihre Verfügbarkeit auf `main` ist am Code zu prüfen.

## 5b. Figurenrichtung — verbindlich seit Session 3

- **Mensch zuerst, übernatürlich danach.** Der Protagonist ist ein Mensch mit
  einer Sense, der zufällig tot ist — kein geflügeltes Wesen.
- **Lesbares Gesicht, lesbare Hände, lesbare Beine.** Emotion lebt im Gesicht.
- **Silhouette vor Ornament.** Zubehör nur, wenn es die Silhouette verbessert.
- **Waffe stützt die Figur.** Gleiche Palette, gleiche Wertstufen, gleiche
  Detailtiefe wie der Körper; in Ruhe Teil des Sheets.
- **Acht Richtungen entstehen aus einem Rig**, nie aus acht getrennten
  Zeichnungen. Eine Bodenebenen-Projektion, eine Tiefensortierung, ein Körperbau.
  Anders gedreht wird die Figur beim Zielen zu einem anderen Wesen.
- **Gangart folgt der zurückgelegten Strecke**, nicht einer Uhr.
- **Blickrichtung dreht sich mit begrenzter Rate und mit Hysterese.** Die
  Schultern springen nie auf die Maus.
- Brüder unterscheiden sich **strukturell** (Kapuze, Mantel), nicht nur durch
  Farbe oder Größe.

## 6. Figuren und Koop-Lesbarkeit

- Beide Brüder brauchen unterschiedliche Silhouetten, Bewegungsrhythmen und Death-Flame-Führung.
- Spielerfarben dürfen die Canon-Farbbedeutung der Flammen nicht beliebig auflösen.
- Koop-Effekte müssen auf einen Blick ihrer Quelle zugeordnet werden können.
- Partikeleffekte werden bei dichtem Kampf reduziert oder priorisiert.
- Counter-, Parry- und Combo-Fenster brauchen klare Posen und akustisch-visuelle Vorzeichen.
- Kamera und Komposition müssen zwei Spieler unterstützen, ohne die Welt ständig herauszuzoomen und emotional zu entwerten.

## 7. VFX-Regeln

- Death Flame bleibt violett-weiß und wirkt wie kontrollierte Auflösung, Zug und Übergang.
- Life Flame bleibt warm orange und visuell außergewöhnlich.
- Umgebungslicht zeigt Volumen, Material und Entfernung; es ersetzt keine Komposition.
- Bloom, Partikel und Screen Effects sind Akzente, keine Qualitätsabkürzung.
- Soul Resonance muss von Hollowing, Release und gewöhnlichem Schaden unterscheidbar sein.
- Soul-Detonation, falls umgesetzt, braucht eine eigene moralisch und visuell lesbare Form; sie darf nicht wie beliebige explosive Munition wirken.

## 8. Qualitätsbar

Eine Szene besteht den visuellen Anspruch, wenn:

- sie ohne HUD als The Lost Soul of Fire erkennbar ist;
- sie ohne VFX räumlich und komponiert wirkt;
- Spieler, Bruder, Gegner und Telegraphe gleichzeitig lesbar bleiben;
- Materialien und Architektur menschliche Geschichte vermitteln;
- der Blick über klare Wert-, Größen- und Licht-Hierarchie geführt wird;
- Atmosphäre die Entfernung vertieft statt alles gleichmäßig zu verschleiern;
- keine Referenz so direkt ist, dass die Szene wie Fan-Art eines anderen Spiels wirkt.

## 9. Weitere Qualitätsreferenzen — sekundär

- **CrossCode:** sichtbare vertikale Flächen, Ebenen und räumliche Illusion in 2D.
- **Hades:** Encounter-Komposition, negative Fläche, Landmarken, Eingänge und visuelle Priorität.
- **God of War:** Kampfinszenierung und Reaktionsbefriedigung, nicht der grundlegende Artstyle.

Bastion bleibt die primäre Stilreferenz. Children of Morta ergänzt Raumtiefe und Lesbarkeit. Soulfire Gothic bleibt die kreative Identität.

## 10. Düsternis-Charta — PROPOSED

> Entwurf für Freigabe 0 im Change `add-visual-vertical-slice`. Intern heißt
> dieses Maß „das Diablo-Maß“; in Prompts und Stilbeschreibungen wird kein
> Spiel genannt (§2).

Die Welt ist düster, weil Menschen etwas verloren haben, nicht weil etwas
eklig ist. Grauen entsteht aus Verlust, Größe und Stille.

Jede Regel ist eine Ja/Nein-Frage an ein einzelnes Bild. Ein Bild besteht die
Charta, wenn jede Frage die geforderte Antwort hat.

### Erlaubt und verboten

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| D1 | Liegt der größte Teil des Bildes im Dunkel, mit klar abgegrenzten Lichtinseln? | Ja |
| D2 | Zeigt das Bild Verfall (Risse, Rost, Staub, Bruch) an menschengemachten Dingen? | Ja |
| D3 | Gibt es einen Gegenstand, der zeigt, dass hier Menschen lebten oder arbeiteten (Bank, Koffer, Werkzeug, Spind)? | Ja |
| D4 | Zeigt das Bild Blut, Wunden, offenes Fleisch, Organe oder Gore? | Nein |
| D5 | Zeigt das Bild Körperhorror: verdrehte Gliedmaßen als Ekel, Parasiten, Schleim, Fäulnis, Maden? | Nein |
| D6 | Liegen Leichen, Schädel oder Knochen als Dekoration im Bild? | Nein |
| D7 | Zeigt das Bild Folterwerkzeug oder Hinrichtungsszenen? | Nein |
| D8 | Enthält das Bild Höllen- oder Dämonenzeichen (Pentagramm, umgedrehtes Kreuz, Hörner, Fledermausflügel)? | Nein |
| D9 | Geht die Bedrohung von Größe, Leere oder Stille aus statt von Ekel? | Ja |
| D10 | Haben Lost Souls Masken, verkohlte oder verschlossene Gesichter statt verletzter Gesichter? | Ja |

### Licht und Dunkel

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| L1 | Kommt das Hauptlicht von oben links (Schlüssellicht, §5a)? | Ja |
| L2 | Gibt es höchstens drei Lichtinseln auf einem Bildschirm? | Ja |
| L3 | Ist die hellste Lichtinsel spiel- oder storyrelevant (Spieler, Gegner-Telegraph, Seele, Landmarke, Ausgang)? | Ja |
| L4 | Leuchtet irgendeine Umgebungslampe warm orange? | Nein |

### Wertverteilung

Gemessen an der Luminanz (0–255) in sieben gleich breiten Stufen: Stufe 1 bis
36, Stufe 2 bis 73, Stufe 3 bis 109, Stufe 4 bis 146, Stufe 5 bis 182, Stufe 6
bis 219, Stufe 7 darüber.

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| W1 | Liegen mindestens 60 % der Bildfläche in den Stufen 1 bis 3? | Ja |
| W2 | Liegen höchstens 3 % der Bildfläche in Stufe 7? | Ja |
| W3 | Gehört alles in Stufe 7 zu Flamme, Seele, Kernlicht oder Effekt? | Ja |
| W4 | Liegt der Boden der Kampffläche überwiegend in Stufe 2 oder 3? | Ja |
| W5 | Hebt sich jede Figur an ihrer Kontur um mindestens eine Stufe vom Boden dahinter ab? | Ja |
| W6 | Gibt es innerhalb der Kampffläche eine Fläche mit Luminanz unter 8, die größer ist als eine Spielerfigur? | Nein |

## 11. Symbolsatz — PROPOSED

> Entwurf für Freigabe 0 im Change `add-visual-vertical-slice`.

Wenige feste Zeichen mit festen Formregeln. Ein Zeichen bedeutet immer dasselbe
und erscheint nie als bloßes Ornament.

### Death Flame

Übergang, Trennung, Auflösung, Warden-Kraft.

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| S1 | Ist die Flamme außen tiefviolett, innen hellviolett und im Kern fast weiß? | Ja |
| S2 | Enthält die Flamme orange, gelbe, grüne oder blaue Bereiche? | Nein |
| S3 | Zeigt mindestens eine Flammenzunge seitwärts, abwärts oder rückwärts statt nur nach oben? | Ja |
| S4 | Sind die Zungen spitz und teils kantig statt rund und weich? | Ja |

### Life Flame

Leben und Bindung. Außergewöhnlich und sparsam.

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| S5 | Ist die Flamme warm orange mit gelbweißem Kern, weich und nach oben steigend? | Ja |
| S6 | Gibt es mehr als eine Life Flame im Bild? | Nein |
| S7 | Ist die Life Flame Teil eines Kampfeffekts? | Nein |

### Wardens: die gefasste Flamme

Wardens beherrschen die Death Flame. Ihr Zeichen ist eine senkrechte, ruhige
Death Flame in einer von Hand gemachten Fassung: Warden-Marken, Suchfeuer, der
Spalt über der Tür der Schwelle.

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| S8 | Steht die Warden-Flamme senkrecht und ruhig, höchstens leicht atmend? | Ja |
| S9 | Ist sie von bearbeitetem Material gefasst (Eisen, Stein, Holz) und in Menschengröße? | Ja |
| S10 | Schwebt eine Warden-Flamme frei oder bewegt sie sich wild? | Nein |
| S11 | Steht eine Flamme völlig reglos, ohne dass das Bild Vaelor oder einen Ort der Stillness zeigt? (Die reglose Flamme ist der Stillness vorbehalten.) | Nein |

### The Keeper: der offene Ring

The Keeper wird nie erklärt und nie gezeigt. Sein einziges Zeichen ist eine
Abwesenheit: ein Ring mit einer Lücke oben, die Grenze, über die man sprechen,
aber die man nicht überschreiten kann.

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| S12 | Zeigt das Bild den Keeper als Gestalt, Gesicht, Auge, Hand oder Lichtquelle? | Nein |
| S13 | Ist der offene Ring gefüllt, leuchtend oder enthält er etwas? | Nein |
| S14 | Erscheint der offene Ring außerhalb eines Warden-Orts oder mehr als einmal im Bild? | Nein |

## 12. Epochenregel für Erinnerungsorte — PROPOSED

> Entwurf für Freigabe 0 im Change `add-visual-vertical-slice`.

Erinnerungsorte entstehen aus menschlicher Geschichte von der
Industrialisierung bis zur Klimazukunft der Erde. Jede Region hat eine
**Bauepoche** (Architektur) und eine **Ereignisepoche** (Gegenstände,
Kleidung, Technik zur Zeit des Ereignisses). Der Region-Vertrag nennt beide.

| Region | Bauepoche | Ereignisepoche |
| --- | --- | --- |
| Prolog, Ufer | Seebahnhof um 1900 | Mitte des 20. Jahrhunderts (Fallblattanzeige, Fähre, Pappkoffer) |
| Industrial Cathedral | Gießhalle um 1900 | Stahlkrise im späten 20. Jahrhundert |

| Nr. | Prüffrage | Gefordert |
| --- | --- | --- |
| E1 | Stammt jedes Bauwerk und jeder Gegenstand aus der Zeit zwischen Industrialisierung (etwa 1850) und der Klimazukunft? | Ja |
| E2 | Zeigt das Bild Mittelalter, Antike, Burgen, Ritterrüstung oder Fantasy-Zauberei? | Nein |
| E3 | Ist ein Gegenstand jünger als die Ereignisepoche seiner Region? | Nein |
| E4 | Sind gotische Formen als Bauten der Bauepoche erkennbar (Neugotik in Industrie, Bahnhof, Kirche) statt als mittelalterliche Ruine? | Ja |
| E5 | Tragen Lost Souls nur Kleidungsreste ihrer Ereignisepoche? | Ja |
| E6 | Sind Stücke einer anderen Epoche mit einem einzelnen Prop verschmolzen? | Nein |
| E7 | Steht ein Stück einer anderen Epoche innerhalb einer Region oder ohne sichtbare Bruchkante? (Erlaubt ist es nur in der wilden Death Layer zwischen Regionen.) | Nein |
| E8 | Zeigt Warden-Technik (Skiff, Waffen, Marken) Elektronik, Bildschirme oder Life-Flame-Technik? | Nein |
| E9 | Zeigt ein Bild der Scheibe Technik der Life-Flame-Zeit (Life Reactors, Vital Resonance)? | Nein |
