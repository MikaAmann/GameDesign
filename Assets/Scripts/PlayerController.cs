using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    
    [SerializeField] private Tilemap tilemap;
    public GridState gridState;   // neu
    
    public ActionResolver ActionResolver;

    private GridEntity entity;
    void Awake() { entity = GetComponent<GridEntity>(); }
    
    private void Start()
    {
        entity.CurrentCell = tilemap.WorldToCell(transform.position);
        gridState.Register(entity.CurrentCell, gameObject);  // neu
        SnapToCell();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;   // kein Keyboard angeschlossen

        if (kb.wKey.wasPressedThisFrame) Movement(Vector3Int.up);
        if (kb.sKey.wasPressedThisFrame) Movement(Vector3Int.down);
        if (kb.aKey.wasPressedThisFrame) Movement(Vector3Int.left);
        if (kb.dKey.wasPressedThisFrame) Movement(Vector3Int.right);

        //SnapToCell();
    }

    private void Movement(Vector3Int direction)
    {
        Vector3Int targetCell = entity.CurrentCell + direction;
        
        ActionResolver.MoveReturn result = ActionResolver.MoveRequest(entity, targetCell);

        switch (result.moveResult)
        {
            case ActionResolver.MoveResult.Moved:
                SnapToCell();                 // nur jetzt bewegen
                break;
            case ActionResolver.MoveResult.Occupied:
                // Damage-Logik
                break;
            case ActionResolver.MoveResult.Blocked:
                // nichts tun
                break;
        }
    }

    private void SnapToCell()
    {
        transform.position = tilemap.GetCellCenterWorld(entity.CurrentCell);
    }
}