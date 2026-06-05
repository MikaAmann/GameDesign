using System.Collections.Generic;
using UnityEngine;

public class GridState : MonoBehaviour
{
    // Belegung: welche Zelle hält welches Objekt – die EINE Wahrheit.
    private Dictionary<Vector3Int, GameObject> occupancy = new();

    // Dictionaries zeigt der Inspector nicht an, darum spiegeln wir die
    // Belegung in eine ausklappbare Liste. Nur zum Anschauen.
    [System.Serializable]
    private struct DebugEntry
    {
        public Vector3Int cell;
        public GameObject obj;
    }

    [SerializeField]
    [Tooltip("Live-Spiegel der Belegung – nur zur Anzeige.")]
    private List<DebugEntry> debugOccupancy = new();

    public void Register(Vector3Int cell, GameObject obj)
    {
        occupancy[cell] = obj;
        RefreshDebugView();
    }

    public void Unregister(Vector3Int cell)
    {
        occupancy.Remove(cell);
        RefreshDebugView();
    }

    public void Move(Vector3Int from, Vector3Int to)
    {
        if (!occupancy.TryGetValue(from, out var obj))
        {
            Debug.LogWarning($"Move: keine Figur auf {from} – nichts zu bewegen.");
            return;
        }

        if (occupancy.ContainsKey(to))
            Debug.LogWarning($"Move: Zielzelle {to} bereits belegt – wird überschrieben.");

        occupancy.Remove(from);
        occupancy[to] = obj;
        RefreshDebugView();
    }

    public bool IsOccupied(Vector3Int cell)
    {
        return occupancy.ContainsKey(cell);
    }

    public GameObject GetAt(Vector3Int cell)
    {
        return occupancy.TryGetValue(cell, out var obj) ? obj : null;
    }



    private void RefreshDebugView()
    {
        debugOccupancy.Clear();
        foreach (var kvp in occupancy)
            debugOccupancy.Add(new DebugEntry { cell = kvp.Key, obj = kvp.Value });
    }
}