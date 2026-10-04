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
