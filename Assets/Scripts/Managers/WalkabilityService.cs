using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WalkabilityService : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap[] blockingTilemaps;

    // NEU: die Tilemap, auf die du die dünnen Kanten-Tiles malst.
    // NICHT zusätzlich in blockingTilemaps eintragen!
    [SerializeField] private Tilemap edgeTilemap;

    // NEU: einmal im Inspector befüllen – jedes Kanten-Tile + welche Seite es blockt.
    [SerializeField] private List<EdgeTileMapping> edgeTileMappings = new List<EdgeTileMapping>();

    [Flags]
    public enum WallSides { None = 0, Up = 1, Right = 2, Down = 4, Left = 8 }

    [Serializable]
    public struct EdgeTileMapping
    {
        public TileBase tile;
        public WallSides side;   // bei Ecken-Tiles mehrere Seiten anklickbar (Flags)
    }

    private Dictionary<TileBase, WallSides> _tileToSide;   // Tile-Typ  -> Seite
    private Dictionary<Vector3Int, WallSides> _edges;      // Zelle     -> blockierte Seiten

    private void Awake() => BuildLookup();

    private void BuildLookup()
    {
        _tileToSide = new Dictionary<TileBase, WallSides>();
        foreach (var m in edgeTileMappings)
            if (m.tile != null)
                _tileToSide[m.tile] = m.side;

        _edges = new Dictionary<Vector3Int, WallSides>();
        if (edgeTilemap == null) return;

        foreach (var cell in edgeTilemap.cellBounds.allPositionsWithin)
        {
            var tile = edgeTilemap.GetTile(cell);
            if (tile == null) continue;
            if (_tileToSide.TryGetValue(tile, out var side))
            {
                _edges.TryGetValue(cell, out var existing);
                _edges[cell] = existing | side;   // mehrere Kanten auf einer Zelle -> ODER
            }
        }
    }

    // unverändert
    public bool IsWalkable(Vector3Int cell)
    {
        foreach (var map in blockingTilemaps)
            if (map.HasTile(cell))
                return false;

        if (!groundTilemap.HasTile(cell))
            return false;

        return true;
    }
    
    public bool IsWall(Vector3Int cell)
    {
        foreach (var map in blockingTilemaps)
            if (map.HasTile(cell))
                return true;
        return false;   // kein blockierendes Tile -> begehbar ODER void, aber KEINE Mauer
    }
    
    // NEU: blockt eine Kante den Schritt from -> to?
    public bool BlocksEdge(Vector3Int from, Vector3Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
 
        // --- Orthogonaler Schritt (genau eine Achse) ---
        if (dx == 0 || dy == 0)
        {
            WallSides side = SideOf(to - from);
            if (side == WallSides.None) return false;          // kein 1-Schritt -> nichts blockt
            return Has(from, side) || Has(to, Opposite(side)); // duenne Kante, symmetrisch
        }
 
        // --- Diagonaler Schritt (beide Achsen) ---
        // Zwei orthogonale L-Umwege um die Ecke. Ein L-Pfad ist frei, wenn:
        //   - KEINE Wall-Edge auf beiden Teilschritten liegt  UND
        //   - das Eckfeld KEINE dicke Mauer ist (void ist erlaubt!)
        Vector3Int horiz = from + new Vector3Int(dx, 0, 0);
        Vector3Int vert  = from + new Vector3Int(0, dy, 0);
 
        bool pathViaHoriz =
            !BlocksEdge(from, horiz) &&   // 1. Teilschritt kanten-frei
            !BlocksEdge(horiz, to)   &&   // 2. Teilschritt kanten-frei
            !IsWall(horiz);               // Eckfeld keine dicke Mauer (void waere ok)
 
        bool pathViaVert =
            !BlocksEdge(from, vert)  &&
            !BlocksEdge(vert, to)    &&
            !IsWall(vert);
 
        // LOCKER: diagonal blockiert, wenn KEINER der beiden L-Pfade frei ist.
        return !(pathViaHoriz || pathViaVert);
    }





    private bool Has(Vector3Int cell, WallSides side)
        => _edges.TryGetValue(cell, out var s) && (s & side) != 0;

    private static WallSides SideOf(Vector3Int dir)
    {
        if (dir == Vector3Int.up)    return WallSides.Up;
        if (dir == Vector3Int.right) return WallSides.Right;
        if (dir == Vector3Int.down)  return WallSides.Down;
        if (dir == Vector3Int.left)  return WallSides.Left;
        return WallSides.None;
    }

    private static WallSides Opposite(WallSides side) => side switch
    {
        WallSides.Up    => WallSides.Down,
        WallSides.Down  => WallSides.Up,
        WallSides.Left  => WallSides.Right,
        WallSides.Right => WallSides.Left,
        _ => WallSides.None
    };
}