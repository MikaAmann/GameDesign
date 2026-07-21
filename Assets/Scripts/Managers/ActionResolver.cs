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

    private void Unregister(Vector3Int cell)
    {
        gridState.Unregister(cell);
    }

    public void ApplyDamage(GameObject target, int damageValue)
    {
        Debug.Log("Dealt Damage");
    }
}
