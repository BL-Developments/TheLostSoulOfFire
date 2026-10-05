# Spielbarer Fähigkeiten-Prototyp

Stand 04.10.2026. Die sechs in #79 bestätigten Fähigkeiten sind lokal in der Solo-Arena umgesetzt. Arbeitswerte, keine finale Balance. C öffnet die Auswahl in der Homebase oder vor/zwischen Arena-Wellen; links/rechts wählt einen Slot, 1–6 rüstet aus. Z/X setzt die beiden Fähigkeiten im Kampf ein. Doppelte Auswahl ist gesperrt.

| Fähigkeit | Kosten (Glut) | Wirkung / Arbeitswert |
|---|---:|---|
| Zweiter Atem | 3 | 25 Leben, maximal bis 100; 3s Wiederverwendung |
| Durchschlag | 3 | Durchdringendes Projektil, 40 Basisschaden mit AbilityPower; 1s |
| Rückstoßsprung | 2 | 180 Distanz in 0,22s, nahe Gegner zurückstoßen; 2s |
| Sog | 4 | 2s Feld, Radius 155, Reichweite 350; 4s |
| Vergeltung | 3 | 2s Schutz für einen Treffer; danach +24 auf nächsten Waffentreffer innerhalb 5s; 4s |
| Vorlage | 2 | Nächster Waffentreffer markiert; Folgetreffer +25; beide Fenster 5s; 2s |

Kosten kommen ausschließlich aus dem Run-Bestand. Kein Guthabenverbrauch bei ungültigem Einsatz. Devourer bleibt bei Sog/Rückstoß am Ort. Auswahl und Pause frieren Simulation ein. Aktive Effekte enden bei Tod, Reset und Abschluss. Normale Waffen/Ausweichen bleiben nutzbar.

## Start und Prüfung

```powershell
dotnet run --project src/TheLostSoulOfFire -- --dev --start arena
dotnet test tests/TheLostSoulOfFire.Tests/TheLostSoulOfFire.Tests.csproj
dotnet run --project src/TheLostSoulOfFire -- --ability-visual-test
```

Grenzen: regulärer Zufallsdraft (drei Angebote / zwei Auswahlen), lokale Koop-Partnerwirkung, persistente Freischaltungen und finale Balance sind nicht umgesetzt. Der Prolog verwendet noch seinen bestehenden Kampfpfad. Arena besitzt rechteckige Kollisionsgrenzen; kein neues Hindernissystem. Vorläufige VFX nutzen bestehende Partikel und einfache Formen.


## Fähigkeitenanzeige und Fähigkeiten-Katalog

- Die beiden HUD-Karten zeigen Taste, Namen, Effekt, Glut-Kosten, Live-Status und einen Balken für die Abklingzeit. Jede Kategorie hat eine eigene Akzentfarbe.
- Tab öffnet das Charaktermenü. Im Reiter **Fähigkeiten** werden alle sechs spielbaren Fähigkeiten mit Kategorie, Wirkung, Anwendung, Kosten und Abklingzeit angezeigt.
- Ausgerüstete Fähigkeiten werden farbig umrahmt und mit **Z** beziehungsweise **X** markiert. Schaden des Durchschlags berücksichtigt die aktuelle Fähigkeitsstärke.
- Die Auswahl erfolgt direkt im **Fähigkeiten**-Reiter: **Z/X** oder die Slot-Schaltflächen wählen den Zielslot; ein Klick auf eine Karte oder **1–6** rüstet die Fähigkeit aus. **C** öffnet den Reiter im Hub sowie vor und zwischen Wellen. Im Kampf ist die Ansicht verfügbar, die Auswahl gesperrt. Doppelte Fähigkeiten in beiden Slots sind ausgeschlossen; das Menü zeigt den Grund bei einem abgelehnten Wechsel. Das Tab-Menü pausiert den Kampf.
- Start zum Ausprobieren: `dotnet run --project src/TheLostSoulOfFire -- --dev --start arena`.

- Der Reiter **Skills** bleibt separat als Platzhalter erhalten. Die sechs aktiven Run-Fähigkeiten und ihre Auswahl stehen im Reiter **Fähigkeiten**.

- Sandbox: Auswahl jederzeit, Kostenanzeige **FREI**, Abklingzeiten und sonstige Wirkbedingungen bleiben aktiv.
