using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    public GridEntity entity;

    // Awake läuft vor Start — Referenzen hier holen
    void Awake()
    {
        entity = GetComponent<GridEntity>();
    }

    // --- Gemeinsame Logik: in der Basis implementiert, von allen geteilt ---
    
    public void DecideTurn()
    {
        
    }

    //return type?
    public int PlayTurn()
    {
        return 1;
    }

    // A*-Kern: für ALLE Gegner gleich, lebt komplett hier.
    // Kein virtual nötig — die Bewegungsvariation kommt über GetNeighbours.
    protected Vector3Int Pathfinder()
    {
        // ... A*-Logik, ruft intern GetNeighbours() auf ...
        return Vector3Int.zero; // Platzhalter
    }

    // --- Typ-spezifisch: MUSS jede Subklasse definieren ---

    // Liefert die für diesen Figurentyp gültigen Nachbarfelder.
    // Hier steckt der ganze Unterschied Läufer/Springer/etc.
    protected abstract List<Vector3Int> GetNeighbours(Vector3Int cell);
    
    
    private void onDeath()
    {
        
    }
}