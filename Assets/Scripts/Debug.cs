using UnityEngine;
using UnityEngine.Tilemaps;

public class GridDebugMarker : MonoBehaviour
{
    public Tilemap tilemap;
    public Vector3Int cell = Vector3Int.zero;

    void OnDrawGizmos()
    {
        if (tilemap == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tilemap.GetCellCenterWorld(cell), 0.2f);
    }
}