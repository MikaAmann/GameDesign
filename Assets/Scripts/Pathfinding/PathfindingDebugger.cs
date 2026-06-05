using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Standalone-Debugger fuer den Pathfinding-A*-Kern.
/// Nutzt die ECHTEN Services (WalkabilityService + GridState) statt nachgebauter Listen.
/// Braucht KEINEN Gegner – du testest GetNeighbors + A* isoliert, aber gegen die reale Welt.
///
/// Bedienung:
///  1. Leeres GameObject in der Szene -> dieses Script dranhaengen.
///  2. WalkabilityService + GridState aus der Szene in die Slots ziehen.
///  3. Zwei leere Child-GameObjects als "Start" und "Ziel" anlegen und zuweisen.
///     (Im Scene-View hin- und herziehen -> Pfad aktualisiert sich live.)
///  4. Movement-Typ im Dropdown waehlen.
///
/// Gizmos (nur sichtbar wenn das Objekt selektiert ist):
///  - gruen   = Start
///  - magenta = Ziel(e)
///  - cyan    = gefundener Pfad
///  - gelb    = ClosestApproach-Fallback, falls kein Pfad existiert
///  - rot     = (optional) alle vom Start aus direkt erreichbaren Nachbarn
/// </summary>
// Laeuft nur im Play-Mode (Play druecken -> Marker ziehen -> Pfad).
// Kein [ExecuteAlways] -> WalkabilityService ist im Edit-Mode noch nicht initialisiert
// (sein Lookup baut erst in Awake), deshalb wuerde es sonst NullReference werfen.
public class PathfindingDebugger : MonoBehaviour
{
    public enum MovementType { Pawn, Rook, Bishop, Knight, King, Queen }

    [Header("ECHTE Services (aus der Szene ziehen)")]
    public WalkabilityService walkability;
    [Tooltip("Optional. Leer lassen -> es wird nur Terrain/Kanten geprueft, keine Belegung.")]
    public GridState gridState;

    [Header("Start / Ziel (Transforms in den Scene-View ziehen)")]
    public Transform startMarker;
    public Transform goalMarker;
    [Tooltip("Optional: weitere Zielfelder, um goalSet mit mehreren Zellen zu testen.")]
    public List<Transform> extraGoalMarkers = new List<Transform>();

    [Header("Figur")]
    public MovementType movement = MovementType.Rook;
    [Tooltip("Reichweite fuer Slider (Turm/Laeufer/Dame). Bei Steppern/Springern ignoriert.")]
    public int sliderRange = 8;

    [Header("Suchraum-Begrenzung (verhindert Endlossuche)")]
    public int searchRadius = 20;

    [Header("Gizmo-Optik")]
    public float cellSize = 1f;
    public bool drawSearchBounds = false;
    [Tooltip("Zeigt zusaetzlich alle direkten Nachbarn der Startzelle (rot) – gut zum GetNeighbors-Debuggen.")]
    public bool drawStartNeighbors = false;

    // --- intern gecachte Ergebnisse fuer Gizmos ---
    private PathResult _lastResult;
    private List<Vector3Int> _lastFallback;
    private Vector3Int _lastStart, _lastGoal;
    private bool _hasResult;

    private void Update() => Recompute();

    private void Recompute()
    {
        if (startMarker == null || goalMarker == null || walkability == null)
        {
            _hasResult = false;
            return;
        }

        Vector3Int start = WorldToCell(startMarker.position);
        Vector3Int goal  = WorldToCell(goalMarker.position);

        var goalSet = new HashSet<Vector3Int> { goal };
        foreach (var t in extraGoalMarkers)
            if (t != null) goalSet.Add(WorldToCell(t.position));

        _lastResult = Pathfinding.FindPath(start, goalSet, GetNeighbors);
        _lastStart = start;
        _lastGoal = goal;
        _hasResult = true;

        _lastFallback = _lastResult.Found
            ? null
            : Pathfinding.ClosestApproach(_lastResult, start, goal);
    }

    // ------------------------------------------------------------------
    //  GetNeighbors – Platzhalter pro Figur, aber gegen die ECHTEN Services.
    //  Spaeter: durch Aufruf deiner echten GetNeighbors / MovementPatternSO ersetzen.
    // ------------------------------------------------------------------
    private IEnumerable<Vector3Int> GetNeighbors(Vector3Int from)
    {
        switch (movement)
        {
            case MovementType.Pawn:   return Stepper(from, FourDir);
            case MovementType.King:   return Stepper(from, EightDir);
            case MovementType.Knight: return Jumper(from, KnightOffsets);   // springt ueber alles
            case MovementType.Rook:   return Slider(from, FourDir, sliderRange);
            case MovementType.Bishop: return Slider(from, DiagonalDir, sliderRange);
            case MovementType.Queen:  return Slider(from, EightDir, sliderRange);
            default: return new List<Vector3Int>();
        }
    }

    // Stepper (Bauer/Koenig): EIN Schritt pro Offset.
    // Prueft Terrain, Belegung UND Kanten-Wand, da er Feld-an-Feld geht.
    // BlocksEdge erkennt orthogonal/diagonal selbst.
    private IEnumerable<Vector3Int> Stepper(Vector3Int from, Vector3Int[] offsets)
    {
        foreach (var off in offsets)
        {
            var c = from + off;
            if (!InBounds(c, from)) continue;
            if (!walkability.IsWalkable(c)) continue;
            if (walkability.BlocksEdge(from, c)) continue;  // duenne Kanten-Wand zwischen from und c
            if (IsOccupied(c)) continue;
            yield return c;
        }
    }

    // Jumper (Springer): springt ueber alles -> NUR Landefeld pruefen.
    // KEIN BlocksEdge (Kanten blocken nur Feld-an-Feld-Schritte, nicht Spruenge).
    private IEnumerable<Vector3Int> Jumper(Vector3Int from, Vector3Int[] offsets)
    {
        foreach (var off in offsets)
        {
            var c = from + off;
            if (!InBounds(c, from)) continue;
            if (walkability.IsWalkable(c) && !IsOccupied(c))
                yield return c;
        }
    }

    // Slider (Turm/Laeufer/Dame): Strahl pro Richtung.
    // Abbruch beim ersten: nicht-walkable ODER belegt ODER Kanten-Wand auf dem Schritt.
    // BlocksEdge erkennt orthogonal/diagonal selbst -> hier keine Fallunterscheidung noetig.
    private IEnumerable<Vector3Int> Slider(Vector3Int from, Vector3Int[] dirs, int range)
    {
        foreach (var dir in dirs)
        {
            var prev = from;
            var c = from + dir;
            int steps = 0;
            while (steps < range && InBounds(c, from) && walkability.IsWalkable(c))
            {
                if (walkability.BlocksEdge(prev, c)) break;  // Kanten-Wand auf der Bahn -> Strahl endet
                if (IsOccupied(c)) break;                    // Blocker -> Strahl endet (Feld NICHT zurueckgeben)
                yield return c;
                prev = c;
                c += dir;
                steps++;
            }
        }
    }

    // --- Service-Zugriff ---
    private bool IsOccupied(Vector3Int c) => gridState != null && gridState.IsOccupied(c);

    private bool InBounds(Vector3Int c, Vector3Int origin)
        => Mathf.Abs(c.x - origin.x) <= searchRadius
        && Mathf.Abs(c.y - origin.y) <= searchRadius;

    // --- Offset-Tabellen ---
    private static readonly Vector3Int[] FourDir =
    {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0)
    };
    private static readonly Vector3Int[] DiagonalDir =
    {
        new Vector3Int(1,1,0), new Vector3Int(-1,1,0),
        new Vector3Int(1,-1,0), new Vector3Int(-1,-1,0)
    };
    private static readonly Vector3Int[] EightDir =
    {
        new Vector3Int(1,0,0), new Vector3Int(-1,0,0),
        new Vector3Int(0,1,0), new Vector3Int(0,-1,0),
        new Vector3Int(1,1,0), new Vector3Int(-1,1,0),
        new Vector3Int(1,-1,0), new Vector3Int(-1,-1,0)
    };
    private static readonly Vector3Int[] KnightOffsets =
    {
        new Vector3Int(1,2,0), new Vector3Int(2,1,0),
        new Vector3Int(-1,2,0), new Vector3Int(-2,1,0),
        new Vector3Int(1,-2,0), new Vector3Int(2,-1,0),
        new Vector3Int(-1,-2,0), new Vector3Int(-2,-1,0)
    };

    // --- Koordinaten (simpel; spaeter ggf. tilemap.WorldToCell / tilemap.GetCellCenterWorld) ---
    // FloorToInt: ein Marker IRGENDWO in einer Zelle wird korrekt dieser Zelle zugeordnet.
    private Vector3Int WorldToCell(Vector3 w)
        => new Vector3Int(Mathf.FloorToInt(w.x / cellSize), Mathf.FloorToInt(w.y / cellSize), 0);
    // +0.5: Gizmo landet in der ZELLMITTE, nicht auf der Ecke (Grid-Linie).
    private Vector3 CellToWorld(Vector3Int c)
        => new Vector3((c.x + 0.5f) * cellSize, (c.y + 0.5f) * cellSize, 0f);

    // ==================================================================
    //  GIZMOS
    // ==================================================================
    private void OnDrawGizmosSelected()
    {
        if (!_hasResult) return;

        if (drawStartNeighbors)
        {
            Gizmos.color = Color.red;
            foreach (var n in GetNeighbors(_lastStart)) DrawCell(n, 0.4f);
        }

        if (drawSearchBounds)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            float side = (searchRadius * 2 + 1) * cellSize;
            Gizmos.DrawWireCube(CellToWorld(_lastStart), new Vector3(side, side, 0f));
        }

        Gizmos.color = Color.green;
        DrawCell(_lastStart, 0.6f);

        Gizmos.color = Color.magenta;
        DrawCell(_lastGoal, 0.5f);
        foreach (var t in extraGoalMarkers)
            if (t != null) DrawCell(WorldToCell(t.position), 0.5f);

        if (_lastResult.Found) 
        {
            Gizmos.color = Color.cyan;
            DrawPath(_lastResult.Path);
        }
        else if (_lastFallback != null && _lastFallback.Count >= 2)
        {
            Gizmos.color = Color.yellow;
            DrawPath(_lastFallback);
        }
    }

    private void DrawPath(List<Vector3Int> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = CellToWorld(path[i]);
            Gizmos.DrawSphere(p, cellSize * 0.12f);
            if (i > 0) Gizmos.DrawLine(CellToWorld(path[i - 1]), p);
        }
    }

    private void DrawCell(Vector3Int cell, float fill)
    {
        Gizmos.DrawWireCube(CellToWorld(cell), Vector3.one * cellSize * 0.95f);
        var c = Gizmos.color; var prev = c; c.a = 0.25f;
        Gizmos.color = c;
        Gizmos.DrawCube(CellToWorld(cell), Vector3.one * cellSize * fill);
        Gizmos.color = prev;
    }
}