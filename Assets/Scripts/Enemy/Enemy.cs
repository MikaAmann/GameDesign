using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class Enemy : MonoBehaviour
{
    
    [Header("Manager References")] 
    public GridEntity entity;

    [SerializeField] private SO_MoveSet moveSet;
    [SerializeField] private WalkabilityService walkabilityService;
    [SerializeField] private GridState gridState; 
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ActionResolver actionResolver;
    
    [SerializeField] private Tilemap tilemap;
    
    [SerializeField] private UnitView view;
    
    private enum PendingActionKind { None, Move, Attack }
    private PendingActionKind pendingKind;
    private Vector3Int pendingStep;
    private GameObject pendingTarget;

    //Todo: SCriptableObject mit allen Relevanten Stats und mechanismen zum füllen der Variablen
    //Todo: Logiken der Stats erweitern
    [Header("Stats (Readonly)")] 
    [SerializeField] private int health; 
    [SerializeField] private int damage; 
    
    // Awake läuft vor Start — Referenzen hier holen
    void Awake()
    {
        entity = GetComponent<GridEntity>();
        view = GetComponent<UnitView>();
        walkabilityService = Services.I.Walkability;
        gridState          = Services.I.Grid;
        actionResolver     = Services.I.Resolver;
        tilemap            = Services.I.Tilemap;
        playerController   = Services.I.PlayerController;
    }
    
    private void Start()
    {
        entity.CurrentCell = tilemap.WorldToCell(transform.position);
        gridState.Register(entity.CurrentCell, gameObject);  // neu
        SnapToCell();
    }

    // --- Gemeinsame Logik: in der Basis implementiert, von allen geteilt ---
    
    public virtual void DecideTurn()
    {
        pendingKind = PendingActionKind.None;   // Reset zu Beginn jeder Runde

        Vector3Int playerCell = playerController.entity.CurrentCell;
        
        if (GetAttackCells(entity.CurrentCell).Contains(playerCell))
        {
            pendingKind = PendingActionKind.Attack;
            pendingTarget = playerController.gameObject;
            return;
        }
        
        List<Vector3Int> myNeighbours = GetNeighbours(entity.CurrentCell, playerCell);

        // Direkter Angriff hat Priorität: Spieler in Reichweite?
        if (myNeighbours.Contains(playerCell))
        {
            pendingKind = PendingActionKind.Attack;
            pendingTarget = playerController.gameObject;
            return;
        }

        // Sonst: Pfad zur Spielerzelle suchen
        PathResult result = Pathfinding.FindPath(
            entity.CurrentCell,
            new HashSet<Vector3Int> { playerCell },
            c => GetNeighbours(c, playerCell));
        
        //PFad gedfunden
        if (result.Found && result.TryGetFirstStep(out Vector3Int step))
        {
            TryCommitMove(step);
        }
        else //Kein Pfad gefunden: wir gehen so nah wie möglich ran
        {
            List<Vector3Int> fallback = Pathfinding.ClosestApproach(result, entity.CurrentCell, playerCell);

            if (fallback.Count >= 2)
                TryCommitMove(fallback[1]);
        }
    }
    
    private bool TryCommitMove(Vector3Int step)
    {
        ActionResolver.MoveReturn result = actionResolver.MoveRequest(entity, step);

        if (result.moveResult != ActionResolver.MoveResult.Moved)
        {
            pendingKind = PendingActionKind.None;
            return false;
        }

        pendingKind = PendingActionKind.Move;
        pendingStep = step;
        return true;
    }

    public virtual IEnumerator PlayTurn()
    {
        switch (pendingKind)
        {
            case PendingActionKind.Move:
                // Hier nur visuell zur neuen Zelle gleiten.
                yield return view.MoveTo(pendingStep);
                break;

            case PendingActionKind.Attack:
                // Hop zum Spieler -> Aufprall -> Logik feuert -> Hop zurück.
                yield return view.AttackHop(
                    playerController.entity.CurrentCell,
                    () => actionResolver.ApplyDamage(pendingTarget, damage));
                break;

            case PendingActionKind.None:
                // Kein Pfad / nichts zu tun -> kein Wartezeit.
                yield break;
        }
    }
    
    // Liefert die gültigen Nachbarfelder.
    protected virtual List<Vector3Int> GetNeighbours(
        Vector3Int cell, 
        Vector3Int? allowOccupiedAt = null)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();
        bool isJumper = moveSet.pieceType == SO_MoveSet.PieceType.Knight;

        foreach (var offset in moveSet.Offsets)
        {
            for (int i = 1; i <= moveSet.Range; i++)
            {
                Vector3Int targetCell = cell + offset * i;

                if (!walkabilityService.IsWalkable(targetCell)) break;

                bool isAllowedOccupant = 
                    allowOccupiedAt.HasValue && targetCell == allowOccupiedAt.Value;

                if (gridState.IsOccupied(targetCell) && !isAllowedOccupant) break;

                if (!isJumper)
                {
                    Vector3Int from = cell + offset * (i - 1);
                    if (walkabilityService.BlocksEdge(from, targetCell)) break;
                }

                neighbours.Add(targetCell);

                // Spieler blockiert physisch -> Strahl endet HIER, aber Zelle ist drin
                if (isAllowedOccupant) break;
            }
        }
        return neighbours;
    }
    
    // Welche Felder kann ich angreifen? Standard: identisch zur Bewegung.
    // Subklassen ueberschreiben das, wenn sie anders treffen als sie ziehen.
    protected virtual List<Vector3Int> GetAttackCells(Vector3Int from)
    {
        return GetNeighbours(from);
    }
    
    
    private void onDeath()
    {
        //Todo: noch offen
    }
    
    private void SnapToCell()
    { 
        transform.position = tilemap.GetCellCenterWorld(entity.CurrentCell);
    }
}