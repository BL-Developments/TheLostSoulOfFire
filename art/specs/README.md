# Visual-Specs

Jede Visual-ID, die das Spiel zeichnet, hat hier eine Visual-Spec:
`art/specs/<visual-id>.md`, zum Beispiel `art/specs/enemy.hollow.md`. Die Spec
sagt, wie die Grafik einer ID aussehen soll, welche Animationen sie braucht und
wie weit sie ist. Welche Dateien, Frames und Größen tatsächlich geladen werden,
steht in `src/TheLostSoulOfFire/Content/Visuals/registry.json`.

Grundlage: Change `add-visual-vertical-slice`, Spec `visual-registry`.

## Aufbau

Oben stehen feste `Feld:`-Zeilen, danach Abschnitte mit `##`. `dotnet test`
liest diese Zeilen; Schreibweise und Reihenfolge der Feldnamen bitte genau so.

```markdown
# Visual-Spec: enemy.hollow

Art: character
Status: im-spiel
Stil: ludo
Weltgröße: 112 × 112
Akzentfarbe: Violett des Kerns, nur unter Soul Sense
Lore: [Figurenblatt Hollow](../../docs/current/characters/hollow.md)

## Merkmale
- Was die Grafik zeigen muss, in kurzen Punkten.

## Silhouette
Wie die Figur als schwarzer Schattenriss lesbar bleibt.

## Animationen
- `idle`: Ruhe, Schleife.
- `swipe`: Telegraph 0,42 s, Treffer 0,13 s, einmalig.

## Effekte
- `fx.core-hit`: Treffer auf den Kern.
```

## Felder

| Feld | Inhalt |
| --- | --- |
| `Art` | `character`, `effect`, `environment`, `prop` oder `sprite`; gleich der Art in der Registry |
| `Status` | `dummy`, `konzept`, `freigegeben` oder `im-spiel` (siehe unten) |
| `Stil` | `ludo` (Grafik aus der Ludo-Runde vom 29.08.2026, muss neu gemacht werden) oder `hausstil` (aus dem trainierten Hausstil) |
| `Weltgröße` | Breite × Höhe in Weltpixeln, wie in der Registry (`worldSize`) |
| `Akzentfarbe` | der eine gesättigte Akzent der Figur bzw. die Farbe des Effekts |
| `Lore` | Link auf das Lore-Blatt (Figurenblatt, Region-Vertrag oder Abschnitt der Art Direction). Die Spec verweist auf die Lore, sie kopiert sie nicht. |

## Abschnitte

| Abschnitt | Inhalt |
| --- | --- |
| `## Merkmale` | Pflicht. Was im Bild zu sehen sein muss. |
| `## Silhouette` | Pflicht. Form, Umriss und Unterscheidung von anderen Figuren. |
| `## Animationen` | Jede Zeile `- \`clip\`: …` ist ein Clip, den die Registry für diese ID haben muss, mit Spielzeiten. Einbildige Grafik hat den Clip `default`. |
| `## Effekte` | Zeilen `- \`visual-id\`: …` für Effekte, die zu dieser ID gehören. |

`## Animationen` oder `## Effekte` muss mindestens einen Eintrag haben.

## Status

| Status | Bedeutung | Prüfung in `dotnet test` |
| --- | --- | --- |
| `dummy` | Nur die ID existiert; das Spiel zeichnet einen Dummy. Gameplay kann damit bauen. | Spec vollständig |
| `konzept` | Konzept oder Key-Art liegt vor, noch keine Spielgrafik. | Spec vollständig |
| `freigegeben` | Der Owner hat das Konzept freigegeben; die Spielgrafik entsteht. | Spec vollständig |
| `im-spiel` | Die Grafik läuft im Spiel. | Registry-Eintrag vorhanden und enthält jede Animation aus `## Animationen` |

## Neue Figur oder neuer Effekt

1. Visual-ID als Konstante in `Rendering/Visuals/VisualIds.cs` anlegen.
2. Spec mit `Status: dummy` hier anlegen. Das Spiel zeigt einen Dummy, der Test ist grün.
3. Grafik entsteht nach `art/production/`, Registry-Eintrag folgt, Status steigt bis `im-spiel`.
