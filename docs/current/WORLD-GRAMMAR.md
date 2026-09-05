# Soulfire World Grammar

## 1. Kernregel — CANON

Eine Soulfire-Region beginnt nicht mit einem generischen Biom wie „Eiswelt“, „Waldwelt“ oder „Fabriklevel“.

Sie beginnt mit:

```text
MENSCHLICHER ORT
+ MENSCHLICHES EREIGNIS
+ UNGELÖSTES EMOTIONALES RESIDUUM
+ ÜBERNATÜRLICHE SEELENVERFORMUNG
= SOULFIRE-REGION
```

Jede visuelle, narrative und spielmechanische Entscheidung soll auf diese Gleichung zurückführbar sein.

## 2. Region Contract

Für jede vorgeschlagene Region müssen mindestens diese Felder definiert werden:

1. **Human Origin:** realer sozialer Zweck und Geschichte des Ortes.
2. **Human Event:** konkrete Katastrophe, Entscheidung oder langsame Zerstörung.
3. **Dominant Emotion:** stärkstes kollektives Residuum.
4. **Emotional Contradiction:** zwei zugleich verständliche, aber unvereinbare Bedürfnisse.
5. **Physical Place:** glaubwürdige Architektur und ursprüngliche Nutzung.
6. **Soul Distortion:** wie Lost Souls den Ort metaphysisch verändern.
7. **Visual Mutation Language:** wiederkehrende Formen, Bewegungen und Transformationen.
8. **Secondary Palette:** regionsspezifische Farben neben den globalen Flammenfarben.
9. **Material Language:** konkrete Materialien mit Geschichte und Gebrauchsspuren.
10. **Environmental Movement:** Wasser, Rauch, Förderbänder, Wurzeln, Wind, Aufzüge etc.
11. **Soul Sense Reveal:** was erst durch Soul Sense sichtbar oder verständlich wird.
12. **Enemy Family:** aus Rollen und Emotionen des Ortes abgeleitete Gegner.
13. **Hazard Family:** Gefahr als Folge des Ereignisses, nicht als beliebiges Hindernis.
14. **Room Grammar:** wiederkehrende räumliche Regeln der Region.
15. **Signature Landmark:** asymmetrisches, sofort erkennbares Motiv.
16. **Major Lost Soul:** Person oder Kollektiv, das den Konflikt verdichtet.
17. **Release Moment:** wie Loslassen visuell, spielerisch und emotional geschieht.
18. **Player Revelation:** was die Region über den Protagonisten oder sein Festhalten lehrt.

## 3. Emotionaler Kausalitätstest

Eine Region ist stark, wenn folgende Kette lesbar ist:

```text
Menschen bauten diesen Ort für einen Zweck.
        ↓
Ein Ereignis oder System zwang sie in einen Konflikt.
        ↓
Viele starben mit verwandtem, aber nicht identischem Festhalten.
        ↓
Das Residuum deformierte Architektur und Verhalten.
        ↓
Gegner, Gefahren und Raumregeln verkörpern den Konflikt.
        ↓
Der Release löst nicht nur einen Boss, sondern eine emotionale Bindung.
```

Wenn Gegner, Hazard oder Landmark problemlos in eine andere Region verschoben werden könnten, sind sie vermutlich noch zu generisch.

## 4. Lost-Soul-Grammar

Ein Lost-Soul-Design sollte von einer früheren menschlichen Identität ausgehen:

- Wer war die Person oder Gruppe?
- Woran hält sie fest?
- Welche Erinnerung ist noch vorhanden?
- Welche Tätigkeit wird zwanghaft wiederholt?
- Was wurde im Hollowing verloren?
- Welche Handlung des Spielers kann den Anchor sichtbar oder lösbar machen?

Der sichtbare Gegner ist die deformierte Konsequenz. Die menschliche Ursache bleibt der eigentliche Inhalt.

## 5. Globale visuelle Konstanten

- **Death Flame:** violet-white; Übergang, Trennung, Auflösung, Warden-Kraft.
- **Life Flame:** warm orange; Leben, Bindung, außergewöhnlich und visuell sparsam einzusetzen.
- Soulfire Gothic verbindet sakrale Monumentalität, menschliche Infrastruktur und übernatürliche Seelenverformung.
- Ein Raum muss auch ohne VFX räumlich und authored wirken.
- Kampfrelevante Figuren und Telegraphe behalten visuelle Priorität.
- Regionale Paletten dürfen variieren, die Bedeutung der Flammenfarben nicht.

## 6. Raum-Grammar für das Roguelike

Ziel ist **kuratierte Modularität**, kein zufälliger Raum-Salat.

Mögliche Raumtypen:

| Raumtyp | Primärer Zweck |
|---|---|
| Entrance | Region, Richtung und Konflikt etablieren |
| Standard Combat | Kernmechanik der Gegnerfamilie lehren |
| Compressed Combat | Druck und Nahraumvariante |
| Large Arena | Kombinationen, Elites oder Setpiece |
| Traversal | Rhythmuswechsel und physische Ortslogik |
| Environmental Story | menschliche Ursache ohne Exposition zeigen |
| Soul Sense Investigation | Anchor oder verborgene Erinnerung erschließen |
| Reward / Recovery | Entlastung mit narrativer Funktion |
| Elite | zugespitzte emotionale oder mechanische Variante |
| Major Encounter | zentrale Lost Soul bzw. Konfliktverdichtung |
| Release Chamber | Konsequenz, Stille und Loslassen |
| Transition | emotionale und räumliche Brücke zur nächsten Region |

Für jeden Raumtyp definieren:

- freie Kampffläche und Spawn-Sicherheit;
- Randdichte und Vordergrundgrenzen;
- Landmark- und Exit-Lesbarkeit;
- mögliche Soul-Sense-Information;
- Storytelling-Dichte;
- regionsspezifische Gefahren;
- Variation, die den Raumtyp nicht unkenntlich macht.

## 7. Procedural-Design-Leitlinie

- Connectivity, Collision, Spawn Safety und Progression bleiben deterministisch bzw. streng regelbasiert.
- Prozedurale Systeme dürfen Räume innerhalb einer handgebauten World Grammar kombinieren.
- Dressing, Prop-Varianten und kleine Storytelling-Kombinationen können stärker variieren.
- Keine generische Vollautomatisierung, die emotionale Kausalität oder Lesbarkeit opfert.

Kurzform:

> **Procedural rooms within authored world grammars — not procedural biomes.**

## 8. Qualitätsprüfung für neue Regionen

Ein Agent muss vor dem Lock beantworten:

- Ist der menschliche Ursprung ohne Lore-Text erkennbar?
- Tragen Ort, Gegner, Hazard und Boss dasselbe emotionale Thema?
- Gibt es eine echte emotionale Widersprüchlichkeit statt eines simplen Bösewichts?
- Unterscheidet sich die Region in Raumgefühl, Material und Bewegung von bestehenden Welten?
- Bleiben Death Flame und Life Flame semantisch konsistent?
- Gibt es einen Release-Moment statt nur eines Siegesmoments?
- Verändert die Region das Verständnis des Protagonisten?

Ohne überzeugende Antworten bleibt die Region ein Seed, nicht Canon.

## 9. Große Karten und Wiederbesuch

Der frühe Spielfluss darf größere, zusammenhängende Karten mit einem Erkundungsgefühl ähnlich **Diablo** verwenden. Das ist eine Strukturreferenz, keine Aufforderung, Perspektive, Loot-System oder Leveldesign zu kopieren.

Leitlinien:

- Eine große Karte besteht aus klar komponierten Teilräumen, Landmarken, Wegen und Begegnungszonen.
- Roguelike-Variation darf Routen, Begegnungen oder Dressing verändern, ohne Ortsidentität und Lore-Kausalität zu zerstören.
- Der unzivilisierte Startbereich der Death Layer bleibt später wieder besuchbar.
- Wiederbesuch soll neue Pfade, stärkere Resonanzinteraktionen, veränderte Lost Souls oder spätere Storyfolgen ermöglichen.
- Große Karten müssen im Koop navigierbar bleiben; keine langen erzwungenen Trennungen oder unauflösbaren Kamera-/Teleportkonflikte.
- Die genaue Karten-, Kamera- und Generierungstechnik bleibt offen.
