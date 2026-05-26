using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generischer A*-Kern. Weiß NICHTS über Figuren.
/// Die einzige figurenspezifische Stelle ist die reingegebene getNeighbors-Funktion
/// (= euer GetNeighbors). Die Heuristik wird ebenfalls reingegeben, nicht vererbt.
///
/// Bewusst KEINE abstrakte Klasse / keine Subklassen pro Figur:
/// A* ist die EINE Maschine, Figuren-Logik lebt in getNeighbors (+ optional heuristic).
/// </summary>
public static class Pathfinding
{
    /// <summary>
    /// Sucht den zugminimalen Pfad von start zu IRGENDEINEM Feld in goalSet.
    /// </summary>
    /// <param name="start">Startzelle (aktuelle Position der Figur).</param>
    /// <param name="goalSet">Menge gültiger Zielfelder (z.B. Angriffsfelder neben dem Spieler).</param>
    /// <param name="getNeighbors">
    ///   Liefert alle erreichbaren UND nutzbaren Nachbarn (bereits gefiltert nach
    ///   Terrain + Belegung). Jumper: nur Landefeld; Slider: Strahl bis erster Blocker.
    /// </param>
    /// <param name="heuristic">
    ///   Geschätzte Restzüge ab einer Zelle. Default = 0  ->  reines Dijkstra.
    ///   Spätere piece-Heuristik einfach hier reingeben, der Kern bleibt unangetastet.
    /// </param>
    public static PathResult FindPath(
        Vector3Int start,
        HashSet<Vector3Int> goalSet,
        Func<Vector3Int, IEnumerable<Vector3Int>> getNeighbors,
        Func<Vector3Int, int> heuristic = null)
    {
        // Default-Heuristik 0  ->  formal Dijkstra, aber volle A*-Struktur steht schon.
        if (heuristic == null) heuristic = _ => 0;

        var frontier   = new MinHeap<Vector3Int>();      // offene Knoten, sortiert nach f = g + h
        var cameFrom   = new Dictionary<Vector3Int, Vector3Int>();
        var costSoFar  = new Dictionary<Vector3Int, int>(); // das ist g

        frontier.Enqueue(start, 0);
        costSoFar[start] = 0;

        bool found = false;
        Vector3Int reached = start;

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();

            // Ziel erreicht? -> fertig (Zieltest auf die MENGE, nicht eine Zelle)
            if (goalSet.Contains(current))
            {
                found = true;
                reached = current;
                break;
            }

            foreach (Vector3Int next in getNeighbors(current))
            {
                int newCost = costSoFar[current] + 1; // jede Kante = ein Zug = Kosten 1

                // Das "||" ist NICHT optional: erlaubt einen billigeren Weg
                // zu einem schon gesehenen Feld. Ohne das -> subtil falsche Pfade.
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    int priority = newCost + heuristic(next);   // f = g + h
                    frontier.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }

        // Frontier leer OHNE Ziel  ->  "kein Pfad". KEIN Fehler, sondern legitimer Ausgang
        // (Spieler abgeriegelt / Läufer falsche Feldfarbe / Weg dauerhaft verstopft).
        // Der Aufrufer entscheidet dann: Fallback "so nah wie möglich" (siehe ClosestApproach)
        // oder State-Wechsel Chase -> Patrol/Idle.
        List<Vector3Int> path = found
            ? Reconstruct(cameFrom, start, reached)
            : new List<Vector3Int>();

        return new PathResult(found, path, reached, cameFrom, costSoFar);
    }

    /// <summary>
    /// Fallback "so nah wie möglich" (Stufe 0). KEIN zweiter A*-Lauf:
    /// nutzt das costSoFar, das FindPath ohnehin schon berechnet hat.
    /// Gibt das erreichbare Feld mit minimaler Distanz zu approachTarget zurück
    /// (z.B. die Spielerzelle) inklusive Pfad dorthin.
    /// </summary>
    public static List<Vector3Int> ClosestApproach(
        PathResult result,
        Vector3Int start,
        Vector3Int approachTarget)
    {
        Vector3Int best = start;
        int bestDist = int.MaxValue;

        foreach (Vector3Int cell in result.CostSoFar.Keys)
        {
            // Chebyshev-Distanz als simple Nähe-Metrik (reine Luftlinie, kein Pathfinding)
            int dx = Mathf.Abs(cell.x - approachTarget.x);
            int dy = Mathf.Abs(cell.y - approachTarget.y);
            int dist = Mathf.Max(dx, dy);

            if (dist < bestDist)
            {
                bestDist = dist;
                best = cell;
            }
        }

        return Reconstruct(result.CameFrom, start, best);
    }

    // Vom erreichten Feld über cameFrom rückwärts bis zum Start, dann umdrehen.
    private static List<Vector3Int> Reconstruct(
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Vector3Int start,
        Vector3Int target)
    {
        var path = new List<Vector3Int>();
        Vector3Int cur = target;

        while (cur != start)
        {
            path.Add(cur);
            if (!cameFrom.TryGetValue(cur, out cur))
                break; // defensiv: sollte bei erreichbarem target nie passieren
        }
        path.Add(start);
        path.Reverse();
        return path; // [start, ..., target]
    }
}

/// <summary>
/// Ergebnis eines A*-Laufs. Drei unterscheidbare Ausgänge: Pfad gefunden / kein Pfad.
/// CameFrom + CostSoFar werden exponiert, damit der Fallback sie wiederverwenden kann
/// (nur lesen).
/// </summary>
public readonly struct PathResult
{
    public readonly bool Found;                                  // wurde ein Zielfeld erreicht?
    public readonly List<Vector3Int> Path;                       // [start..ziel], leer wenn !Found
    public readonly Vector3Int ReachedGoal;                      // welches Zielfeld erreicht wurde
    public readonly Dictionary<Vector3Int, Vector3Int> CameFrom; // für Pfad-Rekonstruktion
    public readonly Dictionary<Vector3Int, int> CostSoFar;       // g aller besuchten Felder (Fallback)

    public PathResult(
        bool found,
        List<Vector3Int> path,
        Vector3Int reachedGoal,
        Dictionary<Vector3Int, Vector3Int> cameFrom,
        Dictionary<Vector3Int, int> costSoFar)
    {
        Found = found;
        Path = path;
        ReachedGoal = reachedGoal;
        CameFrom = cameFrom;
        CostSoFar = costSoFar;
    }

    /// <summary>Erster auszuführender Zug = erste Kante (NICHT eine einzelne Kachel beim Slider!).</summary>
    public bool TryGetFirstStep(out Vector3Int step)
    {
        // Path[0] ist der Start selbst -> der erste echte Zug ist Path[1].
        if (Found && Path.Count >= 2)
        {
            step = Path[1];
            return true;
        }
        step = default;
        return false;
    }
}

/// <summary>
/// Minimaler binärer Min-Heap. Selbst-enthalten, damit das Script versionsunabhängig
/// kompiliert (System.Collections.Generic.PriorityQueue gibt es erst ab .NET 6 und ist
/// unter Unitys .NET Standard 2.1 NICHT verfügbar).
/// Erlaubt Duplikate (lazy): ein Feld kann mehrfach mit unterschiedlicher Priorität
/// eingefügt werden. Die costSoFar-Prüfung im A* hält das korrekt.
/// </summary>
public class MinHeap<T>
{
    private readonly List<(T item, int priority)> _heap = new List<(T, int)>();

    public int Count => _heap.Count;

    public void Enqueue(T item, int priority)
    {
        _heap.Add((item, priority));
        int i = _heap.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_heap[parent].priority <= _heap[i].priority) break;
            (_heap[parent], _heap[i]) = (_heap[i], _heap[parent]);
            i = parent;
        }
    }

    public T Dequeue()
    {
        T root = _heap[0].item;
        int last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);
        last--;

        int i = 0;
        while (true)
        {
            int l = 2 * i + 1, r = 2 * i + 2, smallest = i;
            if (l <= last && _heap[l].priority < _heap[smallest].priority) smallest = l;
            if (r <= last && _heap[r].priority < _heap[smallest].priority) smallest = r;
            if (smallest == i) break;
            (_heap[smallest], _heap[i]) = (_heap[i], _heap[smallest]);
            i = smallest;
        }
        return root;
    }
}