# Entwicklungsplanung aus der Konzeptaufnahme

Stand: 2026-09-08 (Importdatum; Aufnahmedatum unbekannt).

[Ausführliche Zusammenfassung](../../current/RECORDING-SUMMARY-2026-09-08.md) · [Entscheidungslog](../../current/DECISION-LOG.md) · [Daten für GitHub-Übertragung](backlog.json)

## Veröffentlichungsstatus

**Vorbereitet: 6 Meilensteine, 24 Arbeits-Issues und 1 Übersichts-Issue in 9 Kategorien. Auf GitHub erstellt: 0.**

Ziel: [BL-Developments/TheLostSoulOfFire](https://github.com/BL-Developments/TheLostSoulOfFire). Die bisherige lokale Remote-Adresse unter `bjsc-dev` leitet zu dieser Organisation weiter. Öffentliche Prüfung ergab keine bestehenden Issues oder Milestones. Die Issue-Erstellung über den MCP wurde mit `403 Resource not accessible by integration` abgelehnt; es entstand kein Issue. Der MCP bietet außerdem keine Milestone-Erstellung an. CLI nicht angemeldet; kein verbundener Browser.

Die GitHub-Übertragung ist nach regulärer Anmeldung ausführbar (PowerShell 7, im Repository):

```powershell
gh auth login
pwsh -NoProfile -File tools/github/Publish-RecordingBacklog.ps1
```

Das Skript verwendet die Anmeldung der GitHub-CLI. Es legt echte Meilensteine, Bereichslabels und Issues an, verknüpft Abhängigkeiten und erstellt eine Übersicht. Erneute Ausführung erkennt bereits erstellte Einträge anhand eindeutiger Marker bzw. Milestone-Titel. Bestehende Beschreibungen fremder Issues werden nicht überschrieben. Die Planung enthält keine Termine oder Zuweisungen an Personen. Nach vollständiger Übertragung enthält `published.json` die verifizierten GitHub-Links.

Die erzeugte Übersicht wird bei Wiederholung vollständig aktualisiert; eigene Ergänzungen gehören dort in Kommentare. Bereits vorhandene Arbeits-Issue-Beschreibungen bleiben erhalten. Fehlende Bereichs-/Typ-Labels werden ergänzt, ohne andere Labels zu entfernen.

Lokale Prüfung ohne Anmeldung und ohne Schreibzugriffe auf GitHub:

```powershell
pwsh -NoProfile -File tools/github/Publish-RecordingBacklog.ps1 -ValidateOnly
```

## Meilensteine

| Etappe | Ergebnis | Arbeits-Issues |
|---|---|---:|
| [M1 — Produktregeln und Lore-Abgleich](#m1) | Aufnahme und bestehende Lore sind nachvollziehbar abgeglichen; Ressourcen-, Rückkehr- und Koop-Regeln sind beschrieben; der bestehende Prolog ist für Vaelor-Auswahl und Startwaffe angepasst. Offene Details bleiben als solche markiert. | 4 |
| [M2 — Homebase und vollständiger Run-Kreislauf](#m2) | Homebase → freigeschaltetes Biom ab Level 1 → Levelwechsel → Sichern/Extraktion oder Niederlage → Homebase funktioniert mit dauerhaftem Spielstand in Solo und lokalem Koop. | 4 |
| [M3 — Waffenwahl, Fähigkeiten und Reaktionskampf](#m3) | Eine gewählte Hauptwaffe, drei Fähigkeitsangebote mit zwei Auswahlen, ressourcenbasierte Fähigkeiten, eine bewusst definierte Ultimate und faire Waffen-/Team-Counter sind spielbar. | 6 |
| [M4 — NPC-Dienste und dauerhafter Fortschritt](#m4) | Entdeckte NPCs schalten Homebase-Dienste frei; Händler, Schmied und getrennte Waffen-/Fähigkeitsbäume erzeugen nachvollziehbaren Fortschritt. Die diskutierte Meta-Level-Richtung wird geprüft und konkret entschieden. | 4 |
| [M5 — Erstes Biom, Boss und Erkundung](#m5) | Ein menschlich verankertes Biom mit mehreren Levels, erkennbarer Boss-Handschrift, Freischaltung des nächsten Bioms und Collectible-Erkundung ist spielbar. Keine verbindliche Kampagnen- oder Levelanzahl aus Gesprächsbeispielen ableiten. | 4 |
| [M6 — Balance und integrierte Solo-/Koop-Abnahme](#m6) | Der komplette Kreislauf ist deterministisch geprüft und real gespielt; Ausgabe versus Sicherung bleibt eine faire Entscheidung, Fortschritt bleibt konsistent und bekannte Grenzen sind dokumentiert. | 2 |

Etappen beschreiben eine vorgeschlagene Integrationsfolge. Die Abhängigkeiten unten sind maßgeblich; der Biom-Entwurf kann früh vorbereitet werden. Die Aufnahme beschließt keine Kalendertermine und keine Gesamtzahl späterer Biome. Der vorhandene Prolog/Combat-Stand wird wiederverwendet. Hier wird kein neuer Spielcode implementiert.

<a id="m1"></a>

## M1 — Produktregeln und Lore-Abgleich

Abnahme: Aufnahme und bestehende Lore sind nachvollziehbar abgeglichen; Ressourcen-, Rückkehr- und Koop-Regeln sind beschrieben; der bestehende Prolog ist für Vaelor-Auswahl und Startwaffe angepasst. Offene Details bleiben als solche markiert.

<a id="lore"></a>

### Aufnahme als neue Produktgrundlage und Lore-Revision einarbeiten

ID: `lore` · Kategorie: Lore & Story · Typ: design

Die Aufnahme hat laut Owner bei Konflikten Vorrang. Vaelor wählt den Protagonisten aus und die Sense wird Startwaffe. Das widerspricht der bisherigen Ablehnung persönlicher Auswahl. Wiederholte Run-Niederlagen führen zur Homebase, während endgültige Warden-Zerstörung bisher den Übergang vollendet.

Abnahme:

- [ ] Entscheidungslog, Canon, Story Opening und betroffene Produkt-/Roadmaptexte konsistent auf die Aufnahme beziehen; lokale Zusammenfassung als Ausgangspunkt verwenden.
- [ ] Auswahl durch Vaelor und Sense erhalten; Ursprung/Mechanik der Auswahl offen markieren. Gottstatus, Warden-Siegel und genaue Ortungsfähigkeit nicht daraus ableiten.
- [ ] Run-Niederlage/Rückkehr gegenüber endgültigem Warden-Tod erzählerisch klären; die Rückkehr als festes Gameplayziel erhalten.
- [ ] Eine Hauptwaffe statt dauerhaftem Doppelsystem und Verzicht auf Inventar/Loot festhalten; den bisherigen Sechs-Item-Soul-Echo-Plan ersetzen oder als Fähigkeiten neu fassen.
- [ ] Lost Souls bleiben tragische Menschen; Release und Besiegen bleiben getrennt. Bruder als Spieler 2, unbekannter Keeper und Soulfire Gothic bleiben erhalten.

Abhängigkeiten: keine vorgelagerte Arbeitsaufgabe.

<a id="economy_design"></a>

### Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren

ID: `economy_design` · Kategorie: Ökonomie · Typ: design

Bestätigt sind mindestens normales Geld und eine mystische Ressource aus der Erlösung. Nicht die intakte Seele wird gesammelt. Die mystische Ressource steht im Konflikt zwischen aktiven Fähigkeiten im Run und langfristigem Fortschritt.

Abnahme:

- [ ] Für jede Ressource Herkunft, Verwendungen, ungesicherten Run-Bestand und gesicherten Bestand definieren.
- [ ] Festlegen, welche Ausgaben mit Geld bzw. mystischer Ressource erfolgen und wann eine erfolgreiche Erlösung genau einmal gutgeschrieben wird.
- [ ] Teilsicherung mit Weiterreise und vollständige Extraktion unterscheidbar beschreiben; ungesicherte mystische Ressource geht bei Niederlage verloren.
- [ ] Konkrete Rechenbeispiele für Verdienen, Ausgeben, Teilsichern, Extraktion und Niederlage dokumentieren.
- [ ] Namen und Zahlen als Arbeitswerte markieren; das normale Geld erhält keine unbelegte Todesverlustregel.

Abhängigkeiten: [Aufnahme als neue Produktgrundlage und Lore-Revision einarbeiten](#lore).

Offen: Endgültige Namen, Sicherungsquote und Limits; Schicksal von Geld bei Niederlage; Heilung und Run-Händler sind diskutierte Optionen. 50 % und 1.000 Einheiten sind Beispiele.

<a id="coop_contract"></a>

### Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären

ID: `coop_contract` · Kategorie: Koop · Typ: design

Der bestehende Prototyp verwendet TeamResonance als gemeinsamen Pool. Daraus folgt nicht automatisch, dass Geld, Meta-Fortschritt und Ultimate-Ladung ebenfalls gemeinsam sein sollen. Alle neuen Systeme müssen solo und mit beiden Brüdern funktionieren.

Abnahme:

- [ ] Besitz/Gutschrift von Geld, mystischer Ressource, Freischaltungen und späterem Meta-Level pro Profil oder Team ausdrücklich festlegen.
- [ ] Regeln für Ausgaben, Teilsicherung, Extraktion und Startbereitschaft beider Spieler definieren; nachvollziehbare Vorschau der Konsequenzen.
- [ ] Fähigkeitsangebote und Waffenwahl für beide Spieler sowie Umgang mit unterschiedlichen Freischaltungen festlegen.
- [ ] Down/Stabilisieren, vollständige Team-Niederlage und Solo-Rückkehr verbinden; Doppelauszahlungen verhindern.
- [ ] Abnahmefälle für gleichzeitige Interaktion und widersprüchliche Entscheidungen dokumentieren; vorhandene Kamera, Targeting und Tether wiederverwenden.

Abhängigkeiten: [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design).

<a id="tutorial"></a>

### Bestehenden Prolog um Vaelor-Auswahl und Sense als Startwaffe überarbeiten

ID: `tutorial` · Kategorie: Lore & Story · Typ: feature

Ein spielbarer Prolog bis zur Homebase-Schwelle existiert bereits. Gewünscht ist New Game → Vaelors Auswahl → Sense → erste Kämpfe → Bergung durch einen Warden → Homebase. Der etablierte Bruder passt in die Bergung.

Abnahme:

- [ ] Bestehenden PrologueDirector und echte Kämpfe anpassen, statt einen zweiten Tutorialpfad zu bauen.
- [ ] Vaelors Auswahl und Erhalt der Sense verständlich inszenieren; Auswahlmechanik nicht unbelegt erklären.
- [ ] Bewegung, Ausweichen, erste Angriffe und Release verständlich einführen; später freigeschaltete Dienste nicht voraussetzen.
- [ ] Bergung/Bruder-Handoff und Weg zur Homebase solo sowie mit Spieler 2 erhalten; Umgang mit Tutorial-Sonderwaffen explizit festlegen.
- [ ] Deterministische Prologroute prüfen und passende Vorher-/Nachher-Captures am nativen Bild ansehen.

Abhängigkeiten: [Aufnahme als neue Produktgrundlage und Lore-Revision einarbeiten](#lore).

<a id="m2"></a>

## M2 — Homebase und vollständiger Run-Kreislauf

Abnahme: Homebase → freigeschaltetes Biom ab Level 1 → Levelwechsel → Sichern/Extraktion oder Niederlage → Homebase funktioniert mit dauerhaftem Spielstand in Solo und lokalem Koop.

<a id="hub"></a>

### Homebase als spielbaren Ort für Vorbereitung und Run-Start ausbauen

ID: `hub` · Kategorie: Homebase · Typ: feature

Der aktuelle Prolog endet am äußeren Homebase-Schwellwert; ein benutzbarer Hub fehlt. Die Aufnahme macht die Warden-Heimat zum wiederkehrenden Vorbereitungs- und Rückkehrort.

Abnahme:

- [ ] Homebase betreten und dort zu Waffen-/Fähigkeitsvorbereitung sowie Biom-Auswahl gelangen können.
- [ ] Freigeschaltete und noch fehlende NPC-Dienste verständlich zeigen; Ausrüstung und Kosten lesbar darstellen.
- [ ] Nur freigeschaltete Biome anbieten; Auswahl startet das Biom bei Level 1.
- [ ] Interaktion und Startbereitschaft für Solo und zwei lokale Spieler umsetzen.
- [ ] Ein konkreter Raum/Zustandsablauf genügt; keine allgemeine Quest-/Dialog-Engine voraussetzen. Native Darstellung prüfen.

Abhängigkeiten: [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract); [Bestehenden Prolog um Vaelor-Auswahl und Sense als Startwaffe überarbeiten](#tutorial).

<a id="save"></a>

### Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern

ID: `save` · Kategorie: Run-Struktur & Speichern · Typ: feature

Hub-Fortschritt muss mehrere Runs und Neustarts überstehen. Der aktuelle Stand besitzt noch keinen persistenten Spielstand.

Abnahme:

- [ ] Gesicherte Währungen, freigeschaltete Biome/Waffen/Fähigkeiten, NPC-Dienste, Skillknoten und Collectible-Flags als versionierte Daten speichern.
- [ ] Ungesicherten Run-Bestand von dauerhaftem Bestand trennen; Laden darf weder Kopien erzeugen noch gesicherte Werte verlieren.
- [ ] Die beschlossene Solo-/Koop-Profilzuordnung unterstützen.
- [ ] Atomaren Schreib-/Ladevorgang und definierten Umgang mit fehlendem/ungültigem Spielstand prüfen.
- [ ] Mehrfaches Laden sowie Wiederholung derselben Sicherungs-/Freischaltaktion ohne doppelte Belohnung testen; mitten im Run fortsetzbare Saves sind kein automatisch zugesagtes Feature.

Abhängigkeiten: [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

<a id="run_flow"></a>

### Biom- und Level-Runs mit Rückkehr und Neustart auf Level 1 umsetzen

ID: `run_flow` · Kategorie: Run-Struktur & Speichern · Typ: feature

Ein Run besteht aus mehreren Levels eines gewählten Bioms. Nach Niederlage oder beendeter Extraktion beginnt ein neuer Versuch bei Level 1 dieses Bioms. Dauerhaft freigeschaltete spätere Biome bleiben direkt anwählbar.

Abnahme:

- [ ] Konkrete Zustände für Homebase, Run-Start, laufendes Level, Levelabschluss, Extraktion, Niederlage und Bossabschluss einführen.
- [ ] Biom 2 / Level 3 → Niederlage → Homebase → Biom 2 / Level 1 deterministisch nachweisen.
- [ ] Freigeschaltetes Biom 3 direkt starten können, ohne Biom 1/2 erneut zu spielen.
- [ ] Vorhandene Prolog-Sektor-Retries und Debugsprünge ausdrücklich vom regulären Run-Verhalten abgrenzen.
- [ ] Solo-Niederlage und vollständige Team-Niederlage sauber abschließen; keine ungerechtfertigten Level- oder Ressourcenreste behalten.

Abhängigkeiten: [Homebase als spielbaren Ort für Vorbereitung und Run-Start ausbauen](#hub); [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save).

<a id="checkpoints"></a>

### Warden-Reisepunkte mit Teilsicherung, Extraktion und Ressourcenanzeige bauen

ID: `checkpoints` · Kategorie: Ökonomie · Typ: feature

An Levelgrenzen sollen Warden-Reisepunkte eine Entscheidung ermöglichen: einen Teil sichern und weitergehen oder den Run beenden und alles dafür vorgesehene sichern. Die genaue Quote ist noch nicht entschieden.

Abnahme:

- [ ] Beschlossene Geld-/Ressourcenkonten beim Sammeln, Ausgeben und Sichern korrekt führen; Erlösung bleibt visuell von der Ressourcengutschrift getrennt.
- [ ] Am Reisepunkt Weiterreise mit Teilsicherung und vollständige Extraktion samt Folgen anbieten.
- [ ] Gesicherte Werte persistieren; Fortsetzung behält nur den verbleibenden Run-Bestand; Rückkehr beendet den Run.
- [ ] HUD zeigt beide Währungen, verfügbar/gesichert und Vorschau der Entscheidung, mit verständlichem Feedback bei fehlenden Mitteln.
- [ ] Grenzfälle null Bestand, Rundung, doppelte Aktivierung, gleichzeitige Koop-Eingaben und Niederlage nach Teilsicherung prüfen.

Abhängigkeiten: [Biom- und Level-Runs mit Rückkehr und Neustart auf Level 1 umsetzen](#run_flow); [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

Offen: 50 % ist nur ein Beispiel. Quote, Wiederholbarkeit und Transfers müssen vor Implementierung aus den Ressourcenregeln hervorgehen.

<a id="m3"></a>

## M3 — Waffenwahl, Fähigkeiten und Reaktionskampf

Abnahme: Eine gewählte Hauptwaffe, drei Fähigkeitsangebote mit zwei Auswahlen, ressourcenbasierte Fähigkeiten, eine bewusst definierte Ultimate und faire Waffen-/Team-Counter sind spielbar.

<a id="weapons"></a>

### Eine Nah- oder Fernkampfwaffe vor dem Run auswählen

ID: `weapons` · Kategorie: Kampf & Builds · Typ: feature

Die Aufnahme legt eine vorab gewählte Waffe fest. Der aktuelle Player besitzt gleichzeitig Scythe und Soul Cannon; beide vorhandenen Systeme dienen als Ausgangspunkt für unterschiedliche Loadouts.

Abnahme:

- [ ] Sense als erste Tutorialwaffe erhalten; vor regulären Runs eine freigeschaltete Nah- oder Fernkampfwaffe ausrüsten.
- [ ] Im normalen Run nur das gewählte Hauptwaffen-Kit verfügbar machen und Waffenwahl im HUD sichtbar halten.
- [ ] Vorhandene Sense, Cannon, Kombos und Dash-Cancel wiederverwenden; Freischaltungen/Upgrades datenbasiert anbinden.
- [ ] Jedes Loadout kann notwendige Kämpfe und Releases alleine bewältigen; keine versteckte Pflicht zur gleichzeitig ausgerüsteten zweiten Waffe.
- [ ] Beide Brüder können ihre Wahl gemäß Koop-Vertrag treffen; Klassenwahl und Loot-Inventar nicht einführen.
- [ ] Umfang von äußerer Anpassung/Rüstung und möglicher Spielwirkung als kleine Designentscheidung dokumentieren; die erwähnte Anpassung nicht unbelegt als Rüstungsloot oder fertiges Statsystem umsetzen.

Abhängigkeiten: [Homebase als spielbaren Ort für Vorbereitung und Run-Start ausbauen](#hub); [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save).

<a id="draft"></a>

### Drei zufällige Fähigkeiten anbieten und zwei für den Run wählen

ID: `draft` · Kategorie: Kampf & Builds · Typ: feature

Das besprochene Startmodell lautet: aus einem freigeschalteten Gesamtpool drei Angebote, daraus zwei Fähigkeiten wählen. Verfügbarkeit wird durch andere jagende Wardens und geteilte Kräfte in der Homebase erklärt.

Abnahme:

- [ ] Vor dem Run drei unterschiedliche zulässige Angebote bilden und zwei auswählbar machen; Auswahl bleibt für den Run eindeutig.
- [ ] Zufall über einen Seed reproduzierbar machen; gesperrte Fähigkeiten nicht anbieten.
- [ ] Für einen kleinen Freischaltpool einen definierten Fallback bieten; Verhalten bei Abbruch und erneutem Run-Start als Designregel festlegen.
- [ ] Warden-Verfügbarkeit knapp erzählerisch erklären; spätere dauerhafte Fähigkeiten mit Slot-/Angebotsregeln vorbereiten.
- [ ] Waffenwahl und beide Koop-Spieler berücksichtigen; Fähigkeiten, Kosten und Wirkung vor Bestätigung zeigen.

Abhängigkeiten: [Homebase als spielbaren Ort für Vorbereitung und Run-Start ausbauen](#hub); [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

Offen: Drei Angebote / zwei Auswahlen ist der konkrete Prototypansatz. Dauerhafte Fähigkeiten und ihr Einfluss auf freie Slots benötigen Regeln. Ob Neuwürfeln bei Abbruch begrenzt wird, ist eine Designempfehlung und keine Entscheidung der Aufnahme.

<a id="abilities"></a>

### Run-Fähigkeiten mit mystischer Ressource als Einsatzkosten implementieren

ID: `abilities` · Kategorie: Kampf & Builds · Typ: feature

Die gewählten Fähigkeiten sollen den Run verändern und die zentrale Ausgabe-versus-Fortschritt-Entscheidung erzeugen. Kampf- und Bewegungsfähigkeiten wurden besprochen; konkrete Inhalte sind offen.

Abnahme:

- [ ] Einen kleinen, konkret beschriebenen Fähigkeitenpool umsetzen, der die Angebotsauswahl demonstriert; Fähigkeiten verbinden sich mit Flame, Anchor, Release oder Warden-Rollen.
- [ ] Kosten aus dem verfügbaren Run-Konto abbuchen; gesicherte Meta-Ressourcen nicht versehentlich verbrauchen.
- [ ] Bei unzureichenden Mitteln klare Rückmeldung; keine negativen Werte oder doppelten Abbuchungen bei Inputwiederholung.
- [ ] Angriffs-, Bewegungs- und Koop-Nutzen unterscheiden sich spürbar; Grundwaffe/Ausweichen bleiben als handlungsfähiges Basiskit erhalten.
- [ ] Timing, Treffer und Effekte im laufenden Kampf prüfen; Kosten durch angemessene Kontrollregeln gegen Spam ergänzen, ohne das Ressourcenmodell stillschweigend durch reine Cooldowns zu ersetzen.

Abhängigkeiten: [Drei zufällige Fähigkeiten anbieten und zwei für den Run wählen](#draft); [Warden-Reisepunkte mit Teilsicherung, Extraktion und Ressourcenanzeige bauen](#checkpoints); [Eine Nah- oder Fernkampfwaffe vor dem Run auswählen](#weapons).

<a id="ult_design"></a>

### Ultimate-Aufladung und Verhältnis zur Fortschrittswährung entscheiden

ID: `ult_design` · Kategorie: Kampf & Builds · Typ: design

Eine Ultimate ist ausdrücklich gewünscht, ihr Kostenmodell blieb offen. Diskutiert wurden gemeinsame Ressource, getrennte Ladung/Mana, Zeit, besiegte Gegner und Aufladeorte. Die aktuelle TeamResonance leert den ganzen Pool; diese Regel ist nicht automatisch die neue Wirtschaft.

Abnahme:

- [ ] Aktuelle TeamResonance sowie zwei begrenzte Alternativen anhand gleicher Kampfsituationen vergleichen.
- [ ] Festlegen, wodurch Ladung entsteht, was Aktivierung kostet, wie lange die Wirkung dauert und was bei Levelwechsel/Niederlage erhalten bleibt.
- [ ] Verhältnis zu aktiven Fähigkeiten, langfristigem Fortschritt und Stabilisierung des Bruders ausdrücklich entscheiden.
- [ ] Solo-/Teamwirkung, Besitz und Aktivierungsrecht nach Koop-Vertrag definieren.
- [ ] Empfehlung mit Auswirkungen auf Spielentscheidungen dokumentieren; Ergebnis vor Integration konkret beurteilen lassen.

Abhängigkeiten: [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

Offen: Keine der genannten Varianten ist bereits beschlossen. Kein automatisches Leeren des gesamten Geld-/Meta-Kontos ableiten.

<a id="ult"></a>

### Ultimate nach gewähltem Ressourcenmodell in das Run-Kit integrieren

ID: `ult` · Kategorie: Kampf & Builds · Typ: feature

Die Ultimate ergänzt Waffen und zwei Run-Fähigkeiten. Vorhandene Resonance-Präsentation und Zustände können weiterverwendet werden, wenn sie zum ausgewählten Modell passen.

Abnahme:

- [ ] Das Ergebnis der Ultimate-Entscheidung implementieren; bestehende TeamResonance nicht unbeabsichtigt als Geldbörse verwenden.
- [ ] Ladung, Bereitschaft, Aktivierung und Ende im HUD sowie im Kampf klar unterscheiden.
- [ ] Effekt, Dauer und Wiederaufladung bei Solo, Koop, Down/Stabilisieren und Levelwechsel korrekt führen.
- [ ] Aktive Fähigkeiten und gesicherte Fortschrittswährung werden nur nach expliziten Regeln beeinflusst.
- [ ] Deterministische Aktivierungs-/Resetfälle plus echte Kampfprüfung und native Screenshots liefern.

Abhängigkeiten: [Ultimate-Aufladung und Verhältnis zur Fortschrittswährung entscheiden](#ult_design); [Run-Fähigkeiten mit mystischer Ressource als Einsatzkosten implementieren](#abilities).

<a id="reactions"></a>

### Timing-Counter für jedes Waffen-Loadout und Teamaktionen ausbauen

ID: `reactions` · Kategorie: Kampf & Builds · Typ: feature

Die Aufnahme bestätigt aktive Reaktionen: beispielsweise einen anstürmenden Gegner im richtigen Moment treffen und eine Detonation auslösen. Bereits vorhanden sind Severance Window und Burning-Detonation.

Abnahme:

- [ ] Bestehende Counter zuerst prüfen und für getrennte Nah-/Fernkampf-Loadouts zugänglich machen.
- [ ] Mindestens eine lesbare zeitkritische Gegnerinteraktion mit konkreter Folgeaktion ausarbeiten; Treffer vor, im und nach dem Fenster vergleichen.
- [ ] Eine Detonation trifft die feindliche Manifestation bzw. geeignete Restenergie; intakte erlöste Seelen werden nicht als Sprengstoff verwendet.
- [ ] Telegraphe, deterministische Kollision und faire Fehlerrückmeldung erhalten; Teampartner kann die Gelegenheit sinnvoll nutzen.
- [ ] Solo und zwei Spieler mit unterschiedlichen Waffen real prüfen; VFX und Kamera erhalten lesbare Bedrohungen.

Abhängigkeiten: [Eine Nah- oder Fernkampfwaffe vor dem Run auswählen](#weapons); [Aufnahme als neue Produktgrundlage und Lore-Revision einarbeiten](#lore).

<a id="m4"></a>

## M4 — NPC-Dienste und dauerhafter Fortschritt

Abnahme: Entdeckte NPCs schalten Homebase-Dienste frei; Händler, Schmied und getrennte Waffen-/Fähigkeitsbäume erzeugen nachvollziehbaren Fortschritt. Die diskutierte Meta-Level-Richtung wird geprüft und konkret entschieden.

<a id="npcs"></a>

### NPC-Begegnungen schalten Homebase-Dienste schrittweise frei

ID: `npcs` · Kategorie: Homebase · Typ: feature

Im Run gefundene oder gerettete NPCs sollen Händler, Schmied und Fähigkeitstraining in der Homebase verfügbar machen. Das erste Biom führt diese Funktionen schrittweise ein.

Abnahme:

- [ ] Entdecken/Ansprechen bzw. Rettung als konkrete Freischaltereignisse modellieren und persistent speichern.
- [ ] Vor und nach Freischaltung sichtbar machen, welcher Dienst verfügbar ist.
- [ ] Die Reihenfolge im ersten Biom als begründeten Ablauf festlegen; im Gespräch genannte Levelnummern nicht als Canon behandeln.
- [ ] Klarmachen, wann eine Freischaltung gesichert ist und wie Extraktion/Niederlage davor oder danach wirken.
- [ ] Gemeinsame Begegnungen im Koop ohne doppelte NPCs oder doppelte Freischaltbelohnung abschließen; NPCs tragen glaubwürdige Warden-Rollen.

Abhängigkeiten: [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save); [Biom- und Level-Runs mit Rückkehr und Neustart auf Level 1 umsetzen](#run_flow); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

Offen: Händler in Level 1, Schmied in Level 2 und Skilltrees nach Boss sind ein Beispiel für gestaffelte Einführung.

<a id="shops"></a>

### Händler und Schmied für Waffenfreischaltung und Verbesserung umsetzen

ID: `shops` · Kategorie: Ökonomie · Typ: feature

Die Aufnahme sieht Kauf/Freischaltung weiterer Waffen und Verbesserungen bestehender Waffen vor. NPCs können Wardens auch unterwegs versorgen; ein Run-Händler bleibt eine Option.

Abnahme:

- [ ] Händler verkauft/freischaltet Waffen aus dem vereinbarten Angebot; Schmied verbessert besessene Waffen.
- [ ] Kosten und Voraussetzungen aus dem Ressourcen-/Fortschrittsmodell anzeigen und atomar abbuchen.
- [ ] Neue Waffe erscheint anschließend in der Vorbereitungswahl; Upgrades über Neustarts erhalten.
- [ ] Schmied-Upgrades gegenüber Waffen-Skilltree-Knoten abgrenzen: Zuständigkeit, Kosten und Kombination der Effekte gemeinsam definieren; Zahlenänderungen und Handhabungs-/Perk-Wirkungen transparent darstellen.
- [ ] Koop-Besitzregeln beachten; keine zufälligen Lootgegenstände oder Inventarsortierung voraussetzen.

Abhängigkeiten: [NPC-Begegnungen schalten Homebase-Dienste schrittweise frei](#npcs); [Eine Nah- oder Fernkampfwaffe vor dem Run auswählen](#weapons); [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design).

Offen: Ein Händler unterwegs sowie Heilung oder temporäre Upgrades sind optionale Erweiterungen, keine Abnahmevoraussetzung dieses ersten Dienstes.

<a id="trees"></a>

### Getrennte Waffen- und Fähigkeits-Skilltrees aufbauen

ID: `trees` · Kategorie: Metaprogression · Typ: feature

Gewünscht sind getrennte Fortschrittspfade für die vorab gewählte Waffe und die variablen Run-Fähigkeiten. Höherer Fortschritt soll auch dauerhaften Zugang zu ausgewählten Fähigkeiten ermöglichen.

Abnahme:

- [ ] Zwei klar getrennte kleine Bäume mit nachvollziehbaren Voraussetzungen, Kosten und Wirkung darstellen.
- [ ] Waffenbaum erweitert Handhabung, Kombos/Perks und passende Werte; Grenzen und Zusammenspiel mit Schmied-Upgrades gemäß gemeinsamem Fortschrittsmodell umsetzen. Fähigkeitenbaum verbessert Fähigkeiten und Verfügbarkeit.
- [ ] Mindestens einen späten Knoten für dauerhaften Fähigkeitszugang als Progressionsziel konkretisieren; Wirkung auf drei Angebote/zwei Slots eindeutig regeln.
- [ ] Kauf, fehlende Mittel, Mehrfachkauf und Speichern/Laden testen; gesicherte mystische Ressource nach Wirtschaftsregel verwenden.
- [ ] Die festgelegte Figur wird durch Ausrüstung/Fähigkeiten entwickelt, ohne Krieger-/Magier-Klasse. Rüstungsanpassung bleibt ein gesondert offenes Detail.

Abhängigkeiten: [Händler und Schmied für Waffenfreischaltung und Verbesserung umsetzen](#shops); [Drei zufällige Fähigkeiten anbieten und zwei für den Run wählen](#draft); [NPC-Begegnungen schalten Homebase-Dienste schrittweise frei](#npcs).

<a id="meta_level"></a>

### Meta-Level aus heimgebrachten Ressourcen konkretisieren und prüfen

ID: `meta_level` · Kategorie: Metaprogression · Typ: design

Ein über erfolgreich heimgebrachte Ressourcen wachsendes Grundlevel wurde als ergänzende Richtung diskutiert. Es kann spätere Waffen/Skillknoten öffnen, darf aber nicht die Boss-Freischaltung von Biomen ersetzen.

Abnahme:

- [ ] Arbeitsmodell für kumulativ gesicherten Fortschritt gegenüber aktuellem ausgebbarem Kontostand vergleichen und einen Vorschlag ausarbeiten.
- [ ] Festlegen, welche Ressource zählt und ob Teilsicherung, Extraktion sowie Collectible-Belohnungen beitragen.
- [ ] Doppelte Zählung derselben Ressource ausschließen; Ausgaben dürfen den erreichten Rang im empfohlenen Modell nicht rückwirkend entziehen.
- [ ] Freigabeschwellen und Solo-/Koop-Zuordnung definieren; ein kleiner Daten-/UI-Prototyp zeigt Händler- und Skilltree-Gates.
- [ ] Biomfreischaltung bleibt ausschließlich an den vorherigen Biom-Boss gebunden; endgültiges Modell vor vollständiger Integration beurteilen.

Abhängigkeiten: [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save); [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design); [Getrennte Waffen- und Fähigkeits-Skilltrees aufbauen](#trees).

Offen: Meta-Level ist eine besprochene Arbeitsrichtung; konkrete Formel und Kurve sind nicht beschlossen.

<a id="m5"></a>

## M5 — Erstes Biom, Boss und Erkundung

Abnahme: Ein menschlich verankertes Biom mit mehreren Levels, erkennbarer Boss-Handschrift, Freischaltung des nächsten Bioms und Collectible-Erkundung ist spielbar. Keine verbindliche Kampagnen- oder Levelanzahl aus Gesprächsbeispielen ableiten.

<a id="biome_design"></a>

### Erstes Biom aus menschlicher Geschichte und Boss-Handschrift entwerfen

ID: `biome_design` · Kategorie: Biome & Erkundung · Typ: design

Die Aufnahme entscheidet Struktur und Boss-Handschrift, benennt aber kein Startbiom. Vorhandene Region Seeds sind nutzbare Kandidaten, keine automatisch festgelegte Kampagne.

Abnahme:

- [ ] Einen begrenzten Vorschlag für menschlichen Ort, Ereignis, emotionales Residuum und Soul-Verformung erstellen.
- [ ] Eine tragische Bossfigur und ihre erkennbare Wirkung auf Architektur, Gegner und Hindernisse beschreiben.
- [ ] Mehrere Level, Reisepunkte, Freischaltbegegnungen und kurze Erkundungsabzweigungen als spielbaren Ablauf skizzieren.
- [ ] Vorhandene Lore, Warden-Reisestruktur und Soulfire-Gothic-Materialien verbinden; Lesbarkeit für beide Spieler berücksichtigen.
- [ ] Anzahl der Levels, Minibosse und Thema ausdrücklich als neuen Vorschlag kennzeichnen; Empfehlung vor großer Content-Produktion beurteilen.

Abhängigkeiten: [Aufnahme als neue Produktgrundlage und Lore-Revision einarbeiten](#lore); [Zwei Währungen sowie Ausgabe-, Verlust- und Sicherungsregeln definieren](#economy_design).

Offen: Drei/fünf Level, konkrete Biomanzahl und Minibosse wurden als Beispiele genannt. Die gestalterische Auswahl kann früh parallel zu M2–M4 vorbereitet werden.

<a id="biome_levels"></a>

### Erstes Biom mit mehreren Levels und Wiederholbarkeit bauen

ID: `biome_levels` · Kategorie: Biome & Erkundung · Typ: feature

Auf Basis des Biom-Briefs soll ein vollständiger Abschnitt zwischen Homebase-Aufbruch und Endboss entstehen. Bestehende Gegner, Levelkomposition und Capturesystem bilden die technische Basis.

Abnahme:

- [ ] Die beschlossene kleine Zahl authored Levels samt Levelausgang, Reisepunkt und nächstem Level umsetzen.
- [ ] Boss-Handschrift, menschliche Spuren und unterschiedliche Encounter-Räume in jedem Level erkennen lassen.
- [ ] NPC-Freischaltungen gemäß Onboarding-Ablauf sowie Erkundungsabzweigungen integrieren.
- [ ] Alle notwendigen Wege, Releases und Ausgänge sind mit jeder Hauptwaffe in Solo/Koop erreichbar.
- [ ] Denselben Biom-Run wiederholen können; Variation deterministisch halten und keine vorgezogene Prozedural-Engine voraussetzen. Native Vorher-/Nachher-Captures prüfen.

Abhängigkeiten: [Erstes Biom aus menschlicher Geschichte und Boss-Handschrift entwerfen](#biome_design); [Biom- und Level-Runs mit Rückkehr und Neustart auf Level 1 umsetzen](#run_flow); [Warden-Reisepunkte mit Teilsicherung, Extraktion und Ressourcenanzeige bauen](#checkpoints); [NPC-Begegnungen schalten Homebase-Dienste schrittweise frei](#npcs); [Timing-Counter für jedes Waffen-Loadout und Teamaktionen ausbauen](#reactions).

<a id="boss"></a>

### Biom-Endboss und dauerhafte Freischaltung des nächsten Bioms umsetzen

ID: `boss` · Kategorie: Biome & Erkundung · Typ: feature

Jedes Biom endet mit einem Boss. Der Sieg über den vorherigen Endboss ist die Bedingung für das nächste Biom; Meta-Level oder Ausgaben dürfen diese Regel nicht ersetzen.

Abnahme:

- [ ] Einen lesbaren Boss mit erkennbarer menschlicher Geschichte und Bezug zu den vorherigen Gegnern entwickeln.
- [ ] Boss besiegen als Fortschrittsereignis behandeln und die getrennte Soul-Release-Phase narrativ/mechanisch erhalten.
- [ ] Das nächste Biom genau einmal dauerhaft freischalten; kein zusätzliches stillschweigendes Rang- oder Release-Gate für die besprochene Freischaltregel einführen.
- [ ] Auswahl des freigeschalteten Bioms ab dessen Level 1 nach Rückkehr und Neustart nachweisen; für den Test genügt ein klar gekennzeichneter kleiner Folgebereich.
- [ ] Solo und beide Brüder einschließlich Down/Revive, Boss-Niederlage und erneutem Sieg prüfen; Telegraphen und Kamera im realen Kampf ansehen.

Abhängigkeiten: [Erstes Biom mit mehreren Levels und Wiederholbarkeit bauen](#biome_levels); [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save); [Ultimate nach gewähltem Ressourcenmodell in das Run-Kit integrieren](#ult).

<a id="collectibles"></a>

### Collectibles und Easter Eggs ohne Loot-Inventar integrieren

ID: `collectibles` · Kategorie: Biome & Erkundung · Typ: feature

Erkundung soll durch sammelbare Entdeckungen und Easter Eggs belohnt werden. Ein normales Inventar und zufälliger Gegenstandsloot wurden ausdrücklich ausgeschlossen.

Abnahme:

- [ ] Eine kleine Sammlung thematisch passender Entdeckungen in Abzweigungen und Umweltspuren anlegen.
- [ ] Fundstatus pro vereinbarter Profil-/Teamregel dauerhaft verfolgen und in einer kompakten Sammlung anzeigen.
- [ ] Bereits gefundene Einträge verständlich markieren; Wiederholung darf keine unbeabsichtigte Belohnungsschleife erzeugen.
- [ ] Eine abgeschlossene Kategorie kann nach bewusster Entscheidung Geld oder mystische Ressource vergeben; Belohnung und Zeitpunkt sind vor Integration festzulegen.
- [ ] Kein Taschenmanagement, Drop-Raritätssystem oder zusätzliche Ausrüstungsslots für diese Funde; Solo-/Koop-Fund und Speichern/Laden prüfen.

Abhängigkeiten: [Erstes Biom aus menschlicher Geschichte und Boss-Handschrift entwerfen](#biome_design); [Dauerhafte Profile, gesicherte Ressourcen und Freischaltungen speichern](#save); [Besitz, Ausgaben und gemeinsame Run-Entscheidungen im Koop klären](#coop_contract).

Offen: Sammlungsabschluss-Belohnung ist eine diskutierte Option; Menge, Währung und Verlust-/Sicherungsregel bleiben offen.

<a id="m6"></a>

## M6 — Balance und integrierte Solo-/Koop-Abnahme

Abnahme: Der komplette Kreislauf ist deterministisch geprüft und real gespielt; Ausgabe versus Sicherung bleibt eine faire Entscheidung, Fortschritt bleibt konsistent und bekannte Grenzen sind dokumentiert.

<a id="balance"></a>

### Ausgeben versus Sichern sowie Build-Fortschritt im Spiel balancieren

ID: `balance` · Kategorie: Balance & Qualität · Typ: validation

Der Kernkonflikt soll spürbare Entscheidungen erzeugen: Fähigkeiten erhöhen die Überlebenschance, reduzieren aber möglichen Meta-Fortschritt. Konkrete Schwellen aus der Aufnahme sind keine Balancevorgaben.

Abnahme:

- [ ] Auf demselben Abschnitt sparsame, aktive und früh extrahierende Spielweise vergleichen; Einkommen, Kosten, Niederlagen und Bankfortschritt erfassen.
- [ ] Verhindern, dass ausschließlich Horten, ausschließlich Spam oder erzwungenes Farmen ohne sinnvolle Wahl dominiert.
- [ ] Nah-/Fernkampf, Fähigkeitskombinationen und Ultimate getrennt sowie im Team vergleichen; Kämpfe bleiben ohne verbrauchbare Fähigkeiten grundsätzlich handhabbar.
- [ ] NPC-Einführung, Skillkosten und etwaige Meta-Level-Gates am realen Verlauf prüfen; optionale Heil-/Run-Händler nur bei begründetem Nutzen prototypisieren.
- [ ] Nachvollziehbare Tuningentscheidung und verbleibende Balancefragen dokumentieren; Owner spielt den vollständigen Abschnitt.

Abhängigkeiten: [Warden-Reisepunkte mit Teilsicherung, Extraktion und Ressourcenanzeige bauen](#checkpoints); [Ultimate nach gewähltem Ressourcenmodell in das Run-Kit integrieren](#ult); [Getrennte Waffen- und Fähigkeits-Skilltrees aufbauen](#trees); [Meta-Level aus heimgebrachten Ressourcen konkretisieren und prüfen](#meta_level); [Biom-Endboss und dauerhafte Freischaltung des nächsten Bioms umsetzen](#boss); [Collectibles und Easter Eggs ohne Loot-Inventar integrieren](#collectibles).

<a id="integration"></a>

### Gesamten Run-Kreislauf mit Spielstand und Koop abnehmen

ID: `integration` · Kategorie: Balance & Qualität · Typ: validation

Die Planung ist erst spielbar erfüllt, wenn Hub, Run, Ressourcen, Kampf und Fortschritt zusammen funktionieren. Build-Erfolg alleine genügt nicht.

Abnahme:

- [ ] Deterministische Route Homebase → Auswahl → mehrere Levels → Teilsicherung → Boss → Rückkehr → neuer Run nachweisen.
- [ ] Zusätzliche Fälle Niederlage nach Teilsicherung, freiwillige Extraktion, direkter Start eines späteren Bioms, Laden/Neustart und doppelte Ereignisse prüfen.
- [ ] Solo und lokaler Koop mit unterschiedlichen Waffen, gleichzeitigen Interaktionen und vollständiger Team-Niederlage abdecken.
- [ ] Prolog/Golden-Slice-Regressionen gezielt prüfen; Release-Build, relevante automatisierte Checks und native Screenshot-Inspektion als PASS/FAIL/NOT_RUN dokumentieren.
- [ ] Reale Spielprüfung und, falls verfügbar, Controllerprüfung samt bekannten Grenzen und reproduzierbaren Start-/Testanweisungen liefern; Owner-Abnahme erbitten.

Abhängigkeiten: [Ausgeben versus Sichern sowie Build-Fortschritt im Spiel balancieren](#balance); [Bestehenden Prolog um Vaelor-Auswahl und Sense als Startwaffe überarbeiten](#tutorial).

## Gemeinsame Verifikation bei späterer Umsetzung

Vorhandenes C#/.NET-9-/MonoGame-Spiel und Content-Pipeline weiterverwenden. Relevante deterministische Checks, echte Solo-/Koop-Prüfung und bei visuellen Änderungen passende native Vorher-/Nachher-Captures liefern. Ergebnisse als PASS/FAIL/NOT_RUN dokumentieren. Ein Build allein bestätigt keine spielerische Abnahme.
