using UnityEngine;

public class ActionResolver : MonoBehaviour
{

    public enum MoveResult
    {
        Moved, Blocked, Occupied
    }
    public struct MoveReturn
    {
        public MoveResult moveResult;
        public GameObject targetObject;

        public MoveReturn(MoveResult moveResult, GameObject targetObject)
        {
            this.moveResult = moveResult;
            this.targetObject = targetObject;
        }
    }

    [SerializeField] private WalkabilityService walkabilityService;
    [SerializeField] private GridState gridState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public MoveReturn MoveRequest(GridEntity unit, Vector3Int destinationCell)
    {
        if (!walkabilityService.IsWalkable(destinationCell)|| walkabilityService.BlocksEdge(unit.CurrentCell, destinationCell))
            return new MoveReturn(MoveResult.Blocked, null);    // Zielzelle ist nicht begehbar

        GameObject objectAtTargetCell = gridState.GetAt(destinationCell);
        if (objectAtTargetCell == null)
        {
            gridState.Move(unit.CurrentCell, destinationCell);
            unit.CurrentCell = destinationCell;
            return new MoveReturn(MoveResult.Moved, null);     // Zielzelle ist leer, Bewegung erfolgt 
        }
        else
        {
            return new MoveReturn(MoveResult.Occupied, objectAtTargetCell);     // Zielzelle ist belegt
        }
    }

    public void ReleaseCell(Vector3Int cell, GameObject expectedOccupant)
    {
        if (gridState.GetAt(cell) == expectedOccupant)
            gridState.Unregister(cell);
    }

    public void ApplyDamage(GameObject target, int damageValue)
    {
        if (target == null) return;
        if (!target.TryGetComponent(out UnitStats stats)) return;
        if (stats.IsDead) return;

        // Zelle VOR dem Schaden merken: OnDied kann Destroy ausloesen
        GridEntity targetEntity = target.GetComponent<GridEntity>();
        Vector3Int? cell = targetEntity != null ? targetEntity.CurrentCell : (Vector3Int?)null;

        stats.TakeDamage(damageValue);

        if (!stats.IsDead) return;

        // Zelle nur freigeben, wenn dort wirklich noch dieses Objekt steht
        if (cell.HasValue && gridState.GetAt(cell.Value) == target)
            gridState.Unregister(cell.Value);
    }
}
