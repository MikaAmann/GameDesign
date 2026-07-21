# Prototyp-Roadmap "One Man Army"

Stand: 21.07.2026, Revision 2 (nach Code-Review)
Zeitbudget: 2 Tage Vollzeit
Leitfrage fuer jede Entscheidung: Zahlt das auf **Uebermacht erlebt zu Verzweiflung zu Triumph** ein?

## Pflegehinweis

- Status im Kopf jedes Features setzen: `TODO` / `WIP` / `DONE` / `CUT`
- Einzelne Zeilen mit `- [x]` abhaken
- Datei nach jeder Session zurueck ins Claude-Projekt legen, damit der Stand geteilt ist
- Notizen und Abweichungen direkt unter dem betroffenen Feature ergaenzen

## Uebersicht

| ID | Feature | Prio | Status | Abhaengig von |
|----|---------|------|--------|---------------|
| F0 | Bugfixes und Vorarbeiten | Must | TODO | - |
| F1 | Stats-Fundament | Must | TODO | F0 |
| F2 | Schaden, Tod, Spielende | Must | TODO | F1 |
| F3 | Spielerangriff | Must | TODO | F2 |
| F4 | Gegnerangriff wirksam | Must | TODO | F2 |
| F5 | MovementService extrahieren | Must | TODO | F0 |
| F6 | Klick-Input und Highlighting | Must | TODO | F5 |
| F7 | Progression und Promotion | Must | TODO | F1, F6 |
| F8 | AttackPattern als Daten | Should | TODO | F3, F4 |
| F9 | Gegner-Tiers | Should | TODO | F1, F8 |
| F10 | Leveldesign Raumsequenz | Must | TODO | F7, F9 |
| F11 | Balancing-Pass | Must | TODO | F10 |
| B1 | Heuristik-Fix | Klein | TODO | - |
| B2 | Feuerspur / Feldeffekte | Cut | CUT | - |
| B3 | Aktive Faehigkeiten mit Cooldown | Cut | CUT | - |
| B4 | Augments und Truhen | Cut | CUT | - |

---

# F0 Bugfixes und Vorarbeiten

**Status:** TODO
**Zeit:** ca. 90 Minuten
**Ziel:** Bekannte Fehler beseitigen und die Nahtstellen legen, an denen F3 bis F8 sonst teuer werden.

## F0.1 Bug: `Enemy.DecideTurn` verwirft das Ergebnis von `MoveRequest`

Aktuell:
```csharp
actionResolver.MoveRequest(entity, step);
pendingKind = PendingActionKind.Move;
pendingStep = step;
```

Ist die Zelle inzwischen belegt (anderer Gegner ist im selben Zug vorher dorthin gezogen), bleibt `GridState` unveraendert, aber `PlayTurn` animiert trotzdem. Sprite und Logik laufen auseinander.

- [ ] Rueckgabe pruefen, `pendingKind` nur bei `MoveResult.Moved` setzen, sonst `None`
- [ ] Gleiche Korrektur im `ClosestApproach`-Fallback-Zweig

## F0.2 Bug: Todesrichtung ist invertiert (Verstoss gegen Invariante 2 und 6)

`EnemyManager.Unregister` ruft `actionResolver.Unregister(...)`. Laut Architektur-Referenz erkennt der Resolver den Tod, gibt die Zelle frei und meldet weiter.

- [ ] `actionResolver.Unregister(...)` aus `EnemyManager.Unregister` entfernen
- [ ] `EnemyManager.Unregister` macht nur noch `enemyList.Remove(enemy)`
- [ ] `ActionResolver.Unregister` von `public` auf `private` (wird nur noch intern in `ApplyDamage` genutzt)

## F0.3 Bug: `RunEnemyTurn` iteriert ueber eine Liste, die sich aendern kann

Aktuell harmlos, weil Gegner nur im Spielerzug sterben. Ab F8 (Splash) knallt es mit `InvalidOperationException`.

- [ ] `foreach (var enemy in new List<Enemy>(enemyList))`
- [ ] `if (enemy == null) continue;` als erste Zeile im Schleifenkoerper

## F0.4 Bug: `canMove` kodiert zwei Dinge

`MovePlayer` setzt `canMove = true`, ruft dann `deductActionPoints()`, das ggf. `EndTurn()` mit `canMove = false` ausloest. Funktioniert derzeit durch Zufall der Reihenfolge. Mit dem Angriff aus F3 kommt ein dritter Setzer dazu.

- [ ] `canMove` ersetzen durch `isMyTurn` und `isAnimating`
- [ ] Eingabe nur bei `isMyTurn && !isAnimating`
- [ ] `setActiveTurn()` setzt `isMyTurn = true`, `EndTurn()` setzt `isMyTurn = false`
- [ ] `currentActionPoints == 0` zu `<= 0` aendern

## F0.5 Vorarbeit: `Services` als Verdrahtungspunkt

**Grund:** Prefabs im Projektordner koennen keine Szenenreferenzen speichern. `Enemy` haelt aktuell fuenf davon. Ohne Fix muss in F9 jede platzierte Gegner-Instanz von Hand verdrahtet werden.

### Neue Datei
- [ ] `Services.cs`: MonoBehaviour mit `static Services I`, serialisierten Feldern fuer `GridState`, `WalkabilityService`, `ActionResolver`, `Tilemap`, Zuweisung in `Awake`

### Aenderungen
- [ ] Leeres GameObject `_Services` in der Szene, einmal verdrahten
- [ ] `Enemy`: serialisierte Felder `walkabilityService`, `gridState`, `actionResolver`, `tilemap` entfernen, in `Awake` aus `Services.I` holen
- [ ] `PlayerController`: dasselbe
- [ ] `playerController` in `Enemy` bleibt vorerst serialisiert oder kommt spaeter ueber `Services`

### Definition of Done
- Ein Gegner-Prefab laesst sich in die Szene ziehen und funktioniert ohne manuelles Verdrahten

### Notiz
Bewusster Kompromiss. Globaler Zugriffspunkt gegen Verdrahtungsaufwand. Die Regel bleibt: Schreiben auf `GridState` ausschliesslich durch den Resolver.

## F0.6 Vorarbeit: `UnitView` extrahieren

**Grund:** `PlayerController` und `Enemy` haben bereits duplizierte Animations- und Snap-Logik. F3 wuerde eine dritte Kopie erzeugen.

### Neue Datei
- [ ] `UnitView.cs` (MonoBehaviour, haelt `tilemap` aus `Services`)
  - [ ] `IEnumerator MoveTo(Vector3Int cell)`
  - [ ] `IEnumerator AttackHop(Vector3Int targetCell, System.Action onImpact)`
  - [ ] `void SnapToCell(Vector3Int cell)`
  - [ ] Felder `moveDuration`, `attackSpeed`, `returnSpeed`

### Aenderungen
- [ ] `Enemy`: `AnimateMove`, `AnimateAttack`, `Hop`, `SnapToCell` entfernen, `UnitView` nutzen
- [ ] `PlayerController`: `MovePlayer`, `SnapToCell` entfernen, `UnitView` nutzen
- [ ] Komponente auf Spieler-Prefab und alle Gegner-Prefabs

### Definition of Done
- Kein Lerp- oder Hop-Code mehr ausserhalb von `UnitView`
- Schadenszeitpunkt liegt sichtbar im `onImpact`-Callback der Aufrufstelle

## F0.7 Vorarbeit: Naht fuer getrennte Angriffsreichweite

**Grund:** `DecideTurn` prueft den Angriff aktuell gegen das Bewegungsmuster. Schachlogisch falsch und blockiert den Bauern-Diagonalangriff aus F7.

- [ ] `protected virtual List<Vector3Int> GetAttackCells(Vector3Int from) => GetNeighbours(from);`
- [ ] `DecideTurn` nutzt `GetAttackCells` fuer die Reichweitenpruefung, `GetNeighbours` nur noch fuers Pathfinding

Verhalten bleibt identisch. F8 wird dadurch zum Ueberschreiben einer Methode.

## F0.8 Manueller Zugabbruch

- [ ] Taste (Enter oder Leertaste) ruft `EndTurn()` auf
- Noetig fuer den Uebermachtraum, in dem Warten und Ausweichen taktisch sinnvoll sind

---

# F1 Stats-Fundament

**Status:** TODO
**Ziel:** Spieler und Gegner haben symmetrisch dieselbe Datenbasis fuer Leben und Schaden.

### Neue Dateien
- [ ] `SO_UnitStats.cs`: `maxHealth`, `damage`
- [ ] `UnitStats.cs`: `MaxHealth`, `CurrentHealth`, `Damage`, `IsDead`, init aus Config in `Awake`, `TakeDamage(int)`, Events `OnDamaged`, `OnDied`

### Aenderungen an bestehendem
- [ ] `Enemy`: Felder `health` und `damage` entfernen, `UnitStats` in `Awake` holen
- [ ] `PlayerController`: Felder `maxHealth` und `currentHealth` entfernen, `UnitStats` in `Awake` holen
- [ ] AP bleibt im `PlayerController`

### Neue Datei (Gegner-Konfiguration, getrennt von Kampfwerten)
- [ ] `SO_EnemyConfig.cs`: `xpReward`, `detectionRadius`
- [ ] `Enemy` referenziert es, `EnemyManager.attentionRadius` kann spaeter daraus kommen

### Unity-Arbeit
- [ ] SO-Assets: `Stats_Player`, `Stats_Rook`
- [ ] `UnitStats` auf Spieler-Prefab und alle Gegner-Prefabs

### Definition of Done
- Alle Einheiten zeigen im Inspector zur Laufzeit korrekte HP
- Keine Klasse ausser `UnitStats` haelt HP oder Schaden

### Notiz
`UnitStats` kennt niemanden. Kein Zugriff auf Resolver, Manager oder GridState. Das SO wird nur gelesen, nie beschrieben.

---

# F2 Schaden, Tod, Spielende

**Status:** TODO
**Ziel:** Eine Einheit kann sterben, die Zelle wird frei, das Spiel reagiert.

### Neue Datei
- [ ] `GameManager.cs` (existiert noch nicht): Sieg bei leerer Gegnerliste, Niederlage bei Spielertod. Vorerst `Debug.Log` plus Zeit anhalten reicht.

### Aenderungen an bestehendem
- [ ] `ActionResolver.ApplyDamage`: `TryGetComponent<UnitStats>`, `TakeDamage`, bei `IsDead` Zelle ueber `GridEntity.CurrentCell` in `GridState` freigeben
- [ ] Reihenfolge: Zelle freigeben, **bevor** irgendwer `Destroy` aufruft
- [ ] `Enemy`: `OnDied` abonnieren, `enemyManager.Unregister(this)` plus `Destroy(gameObject)`
- [ ] `Enemy`: Referenz auf `EnemyManager` ergaenzen (fehlt aktuell)
- [ ] `Enemy.onDeath()` entfernen, ersetzt durch Event-Abo
- [ ] `PlayerController`: `OnDied` abonnieren, Eingabe sperren, `GameManager` melden
- [ ] `EnemyManager`: nach `Unregister` auf `enemyList.Count == 0` pruefen

### Definition of Done
- Keine einzige Typpruefung (`is Enemy`, `is Player`) im `ActionResolver`
- Gegner stirbt, verschwindet, Zelle ist wieder betretbar
- Gegnertod mitten im Enemy-Turn ohne Haenger
- Spielertod loest Niederlagezustand aus

---

# F3 Spielerangriff

**Status:** TODO
**Ziel:** Der Spieler kann Gegner toeten.

### Aenderungen an bestehendem
- [ ] `PlayerController`, Zweig `MoveResult.Occupied`: `TryGetComponent<UnitStats>` auf `result.targetObject`
- [ ] Bei Treffer: `isAnimating = true`, `unitView.AttackHop(zielzelle, () => actionResolver.ApplyDamage(target, stats.Damage))`
- [ ] Nach der Animation: `isAnimating = false`, `deductActionPoints()`, Buffer pruefen
- [ ] Angriff kostet 1 AP, wie Bewegung
- [ ] `entity.CurrentCell` aendert sich beim Angriff nicht, der Spieler bleibt stehen

### Definition of Done
- Angriff kostet AP und kann den Zug beenden
- Kein Endlos-Zuschlagen in einem Frame moeglich
- Ablauf bleibt zweistufig: Move anfragen, Ergebnis lesen, dann Schaden

---

# F4 Gegnerangriff wirksam

**Status:** TODO
**Ziel:** Der bestehende leere Angriff richtet echten Schaden an.

### Aenderungen an bestehendem
- [ ] `Enemy.AnimateAttack` ist durch `UnitView.AttackHop` ersetzt, `onImpact` ruft `actionResolver.ApplyDamage(pendingTarget, stats.Damage)`
- [ ] `damage` kommt aus `UnitStats`, nicht mehr aus dem eigenen Feld

### Definition of Done
- Der Spieler kann durch Gegner sterben
- Schaden wird beim Aufprall angewendet, nicht beim Entscheiden

---

# F5 MovementService extrahieren

**Status:** TODO
**Ziel:** Ein figuragnostischer Codepfad fuer alle Bewegungsmuster. Voraussetzung fuer Promotion.

### Neue Datei
- [ ] `MovementService.cs` (static)
  - [ ] `GetNeighbours(origin, moveSet, allowOccupiedAt?)` (Logik 1:1 aus `Enemy` uebernehmen)
  - [ ] `GetReachableCells(origin, moveSet, apBudget)` liefert `Dictionary<Vector3Int, int>` mit AP-Kosten

### Aenderungen an bestehendem
- [ ] `Enemy.GetNeighbours` wird zum duennen Wrapper auf `MovementService`
- [ ] Diagonal-Blocking und `BlocksEdge` wandern mit
- [ ] `Pathfinding`: Nachbarfunktion aus `MovementService` injizieren

### Definition of Done
- `PathfindingDebugger` zeigt identisches Verhalten wie vorher (Regressionstest)
- Kein Bewegungsmuster-Wissen mehr in einer Enemy-Subklasse

### Notiz
Der teuerste Refactor, aber ohne ihn ist Promotion nicht umsetzbar. Nicht verschieben. Gut geeignet fuer Claude Code, weil das Ergebnis visuell pruefbar ist.

---

# F6 Klick-Input und Highlighting

**Status:** TODO
**Ziel:** Zielfeld-Auswahl statt WASD. Bewegungsmuster wird sichtbar und lernbar.

### Neue Dateien
- [ ] `CellHighlighter.cs`: eigenes Tilemap-Layer oder gepoolte Quads
- [ ] optional Farbschema: erreichbar / angreifbar / Gefahrenzone

### Aenderungen an bestehendem
- [ ] `PlayerController`: Maus, `Grid.WorldToCell`, Klickvalidierung gegen erreichbare Zellen
- [ ] Nach jedem Zug erreichbare Zellen neu berechnen (AP hat sich geaendert)
- [ ] WASD als Debug-Pfad belassen, nicht mehr pflegen
- [ ] Der Player braucht jetzt ein eigenes `SO_MoveSet` (bisher nur Gegner)

### Unity-Arbeit
- [ ] Tilemap-Layer fuer Highlights ueber dem Boden
- [ ] Halbtransparentes Highlight-Sprite

### Definition of Done
- Erreichbare Felder sind vor dem Zug sichtbar
- Klick auf nicht markiertes Feld tut nichts
- Bei 0 AP ist nichts markiert

### Notiz
Kein `Instantiate` pro Frame. Pool anlegen oder Tilemap setzen und leeren.

---

# F7 Progression und Promotion

**Status:** TODO
**Ziel:** Der Kipppunkt der Lernkurve. Stat-Level, Faehigkeit und Promotion sind derselbe Mechanismus.

### Neue Dateien
- [ ] `SO_ProgressionTable.cs`: Liste von Stufen mit `xpSchwelle`, `healthDelta`, `damageDelta`, `apDelta`, optional `newMoveSet`, optional `newAttackPattern`, optional `newSprite`
- [ ] `PlayerProgression.cs`: `XP`, `Level`, wendet Stufe an

### Aenderungen an bestehendem
- [ ] `ActionResolver`: bei Gegnertod `xpReward` aus `SO_EnemyConfig` an `PlayerProgression` melden
- [ ] `PlayerController`: MoveSet-Referenz zur Laufzeit austauschbar machen
- [ ] Sprite-Wechsel ueber den SpriteRenderer

### Tabelle (auf drei Stufen gekuerzt, siehe Notiz)
| Level | Effekt |
|-------|--------|
| 2 | +HP, +Schaden |
| 3 | Promotion zum Laeufer: MoveSet, AttackPattern und Sprite tauschen |
| 4 | +1 AP (nur falls erreichbar) |

### Definition of Done
- Promotion ist in einem Durchlauf tatsaechlich erreichbar
- Nach Promotion zeigt das Highlighting sofort das neue Muster
- Kein neuer Spielertyp, keine Vererbung, nur Datentausch

### Notiz
Fuenf Stufen waren zu viel. Die meisten Spieler haetten die Promotion nie gesehen. Promotion muss vor dem letzten Raum liegen, sonst fehlt der Beweisraum und damit das Hoch.

### Offene Entscheidung
- [ ] Promotion-Ziel: Laeufer oder Springer? (Empfehlung Laeufer, groesster optischer Kontrast)
- [ ] XP-Quelle ausser Kills noetig, falls der Uebermachtraum durch Ausweichen loesbar sein soll

---

# F8 AttackPattern als Daten

**Status:** TODO
**Ziel:** Angriffsflaechen sind konfigurierbar und lesbar. Zahlt auf "Gegner verstehen" ein.

Dank der Naht aus F0.7 nur noch: SO anlegen und `GetAttackCells` ueberschreiben.

### Neue Datei
- [ ] `SO_AttackPattern.cs`: Offsets relativ zur Richtung, Flag `isSplash`

### Aenderungen an bestehendem
- [ ] `Enemy.GetAttackCells`: aus `SO_AttackPattern` statt Default
- [ ] `ActionResolver`: Zellenmenge iterieren, pro getroffener Entitaet `ApplyDamage`
- [ ] `CellHighlighter`: Gegner-Schlagfelder im Spielerzug einfaerben
- [ ] Bauer: gerade ziehen, diagonal schlagen

### Definition of Done
- Splash funktioniert ohne neue Klasse
- Der Spieler sieht vor seinem Zug, welche Felder bedroht sind

---

# F9 Gegner-Tiers

**Status:** TODO
**Ziel:** Steigende Bedrohung ohne neuen Code.

### Unity-Arbeit
- [ ] SO-Assets `Stats_Basic`, `Stats_Offizier`, `Stats_Elite` pro Gegnertyp
- [ ] Passende `SO_EnemyConfig` mit steigendem `xpReward` und `detectionRadius`
- [ ] Prefab-Varianten mit Tint oder abweichendem Sprite
- [ ] Elite bekommt Flaechen-AttackPattern aus F8

### Definition of Done
- Drei Stufen visuell unterscheidbar
- Keine neue C#-Klasse entstanden
- Platzieren per Drag and Drop ohne Verdrahtung (haengt an F0.5)

---

# F10 Leveldesign Raumsequenz

**Status:** TODO
**Ziel:** Die emotionale Kurve. Entsteht durch Raumfolge, nicht durch Zahlen.

- [ ] Raum 1 Lehrraum: ein Basic-Gegner, sicher gewinnbar, Muster lernen
- [ ] Raum 2 Uebermachtraum: drei Gegner, sichtbar, frontal nicht gewinnbar. Das Tief.
- [ ] Kipppunkt: gesammelte XP loesen die Promotion aus
- [ ] Raum 3 Beweisraum: strukturell derselbe Encounter wie Raum 2, jetzt beherrschbar. Der Triumph.
- [ ] Raum 4 Koenig plus zwei Elite-Leibgarde: klares Ende

### Definition of Done
- Raum 2 und Raum 3 sind als dieselbe Situation erkennbar
- Ein Durchlauf dauert unter 15 Minuten

### Notiz
Wenn Zeit knapp wird, hier kuerzen statt Balancing streichen.

---

# F11 Balancing-Pass

**Status:** TODO
**Ziel:** Herausfordernd, aber nicht chancenlos.

- [ ] Ein Basic-Gegner braucht zwei bis drei Treffer
- [ ] Raum 2 fuehlt sich beim ersten Versuch verloren an
- [ ] Raum 3 fuehlt sich beim ersten Versuch gewonnen an
- [ ] Mindestens ein Testdurchlauf durch Maja oder eine dritte Person

### Notiz
Diese Zeit ist nicht verhandelbar. Ein unbalancierter Prototyp vermittelt die Kernerfahrung nicht.

### Offene Entscheidung
- [ ] Spielertod: Raum-Neustart oder Level-Neustart? (Empfehlung Raum)

---

# Backlog

## B1 Heuristik-Fix
- [ ] Chebyshev-Distanz als Tie-Breaker statt Dijkstra-Default `_ => 0`
- Ca. 15 Minuten. Mitnehmen wenn ohnehin im Pathfinding (F5).

## B2 Feuerspur und Feldeffekte
Braucht persistenten Zellzustand ueber Runden plus Tick im TurnManager. Eigener `FieldEffectManager`. Nach der Abgabe.

## B3 Aktive Faehigkeiten mit Cooldown
Zusaetzlicher Input-Layer. Nach der Abgabe.

## B4 Augments und Truhen
Nach der Abgabe.

## B5 `SO_MoveSet.OnValidate` ueberschreibt Offsets
Handgetunte Muster gehen bei jeder Inspector-Beruehrung verloren. Fuer den Prototyp irrelevant, aber gut zu wissen.

## B6 Injection statt `Services`
`GameManager` verdrahtet Gegner beim Spawn, `Services` faellt weg. Sauberer, aber im Zeitbudget nicht drin.

---

# Zeitplan

| Slot | Inhalt |
|------|--------|
| Tag 1 vormittags | F0 (ca. 90 min), dann F1, F2 |
| Tag 1 nachmittags | F3, F4. Ziel: erster echter Kill in beide Richtungen |
| Tag 1 abends oder Tag 2 frueh | F5, F6. Ziel: Zielfeld-Auswahl mit Highlighting laeuft |
| Tag 2 vormittags | F7, F8. Ziel: Promotion einmal durchspielbar |
| Tag 2 nachmittags | F9, F10, F11 |

## Testreihenfolge nach F1 bis F4
1. `UnitStats` auf beiden, Werte im Inspector sichtbar
2. Spieler schlaegt Rook, HP sinkt im Inspector
3. Rook stirbt, verschwindet, Zelle wieder betretbar
4. `PathfindingDebugger` zeigt die freigewordene Zelle als begehbar (Konsistenzcheck fuer `GridState`)
5. Rook schlaegt zurueck, Spieler-HP sinkt, Spieler stirbt
