using UnityEngine;
using UnityEngine.Tilemaps;   // <- nötig für Tilemap

public class WalkabilityService : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;       // <- SerializeField, nicht Serializable
    [SerializeField] private Tilemap[] blockingTilemaps;

    public bool IsWalkable(Vector3Int cell)
    {
        foreach (var map in blockingTilemaps)
            if (map.HasTile(cell))
                return false;

        if (!groundTilemap.HasTile(cell))
            return false;

        return true;
    }
}