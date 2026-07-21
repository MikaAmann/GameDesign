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
    
    private enum PendingActionKind { None, Move, Attack }
    private PendingActionKind pendingKind;
    private Vector3Int pendingStep;
    private GameObject pendingTarget;

    //Todo: SCriptableObject mit allen Relevanten Stats und mechanismen zum füllen der Variablen
    //Todo: Logiken der Stats erweitern
    [Header("Stats (Readonly)")] 
    [SerializeField] private int health; 
    [SerializeField] private int damage; 
    
    [Header("Animations parameter")] 
    [SerializeField] private float moveDuration = .12f;
    [SerializeField] private float attackSpeed = .07f;
    [SerializeField] private float returnSpeed = .12f;
    
    // Awake läuft vor Start — Referenzen hier holen
    void Awake()
    {
        entity = GetComponent<GridEntity>();
        
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
            actionResolver.MoveRequest(entity, step);   // committet GridState sofort
            pendingKind = PendingActionKind.Move;
            pendingStep = step;
        }
        else //Kein Pfad gefunden: wir gehen so nah wie möglich ran
        {
            List<Vector3Int> fallback = Pathfinding.ClosestApproach(result, entity.CurrentCell, playerCell);
            
            if (fallback.Count >= 2)
            {
                Vector3Int fallbackStep = fallback[1];
                actionResolver.MoveRequest(entity, fallbackStep);
                pendingKind = PendingActionKind.Move;
                pendingStep = fallbackStep;
            }
        }
    }

    public virtual IEnumerator PlayTurn()
    {
        switch (pendingKind)
        {
            case PendingActionKind.Move:
                // Hier nur visuell zur neuen Zelle gleiten.
                yield return AnimateMove(pendingStep);
                break;

            case PendingActionKind.Attack:
                // Hop zum Spieler -> Aufprall -> Logik feuert -> Hop zurück.
                yield return AnimateAttack(pendingTarget);
                break;

            case PendingActionKind.None:
                // Kein Pfad / nichts zu tun -> kein Wartezeit.
                yield break;
        }
    }

    private IEnumerator AnimateMove(Vector3Int step)
    {
        Vector3 start  = transform.position;
        Vector3 target = tilemap.GetCellCenterWorld(step) /* Weltkoordinate von step, z.B. tilemap.GetCellCenterWorld(step) */;
        float elapsed  = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }
        transform.position = target;
    }

    private IEnumerator AnimateAttack(GameObject target)
    {
        Vector3 home       = transform.position;
        Vector3 impactPos  = tilemap.GetCellCenterWorld(playerController.entity.CurrentCell) /* Weltkoordinate des Spielerfelds */;

        // Hin
        yield return Hop(home, impactPos, attackSpeed);
        
        actionResolver.ApplyDamage(target, damage);
        // (ApplyDamage -> HP runter -> HealthComponent.OnDamaged -> UI/FX/Sound/Screenshake)

        // Zurück
        yield return Hop(impactPos, home, returnSpeed);
    }

    private IEnumerator Hop(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.position = to;
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
    
    
    private void onDeath()
    {
        //Todo: noch offen
    }
    
    private void SnapToCell()
    { 
        transform.position = tilemap.GetCellCenterWorld(entity.CurrentCell);
    }
}