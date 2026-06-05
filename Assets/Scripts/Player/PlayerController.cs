using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private ActionResolver actionResolver;
    [SerializeField] private GridState gridState;
    [SerializeField] private Tilemap tilemap;

    [SerializeField] private float moveDuration;
    
    
    
    //Todo: SCriptableObject mit allen Relevanten Stats und mechanismen zum füllen der Variablen
    //Todo: Logiken der Stats erweitern
    [Header("Stats (Readonly)")] 
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxActionPoints = 3;
    private int currentActionPoints;
    
    private bool  canMove = true;
    public GridEntity entity;
    private Vector3Int? bufferedDirection = null;
    
    
    void Awake() { entity = GetComponent<GridEntity>(); }
    
    private void Start()
    {
        entity.CurrentCell = tilemap.WorldToCell(transform.position);
        gridState.Register(entity.CurrentCell, gameObject);  // neu
        SnapToCell();
        
        currentActionPoints = maxActionPoints;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        
        //Todo: Irgendein Button druck zum abbruch (Enter?)

        // Richtung einlesen
        Vector3Int? input = null;
        if (kb.wKey.wasPressedThisFrame) input = Vector3Int.up;
        if (kb.sKey.wasPressedThisFrame) input = Vector3Int.down;
        if (kb.aKey.wasPressedThisFrame) input = Vector3Int.left;
        if (kb.dKey.wasPressedThisFrame) input = Vector3Int.right;

        if (input == null) return;

        if (canMove)
            Movement(input.Value);
        else
            bufferedDirection = input;   // merken statt verwerfen
    }

    private void Movement(Vector3Int direction)
    {
        Vector3Int targetCell = entity.CurrentCell + direction;
        
        ActionResolver.MoveReturn result = actionResolver.MoveRequest(entity, targetCell);

        switch (result.moveResult)
        {
            case ActionResolver.MoveResult.Moved:
                canMove = false;
                StartCoroutine(MovePlayer());
                break;
            case ActionResolver.MoveResult.Occupied:
                // Damage-Logik
                // TODO
                //if enemy -> deductActionPoint()
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

    public void setActiveTurn()
    {
        canMove = true;
        currentActionPoints = maxActionPoints;
    }

    private IEnumerator MovePlayer()
    {
        Vector3 start  = transform.position;
        Vector3 target = tilemap.GetCellCenterWorld(entity.CurrentCell);
        float elapsed  = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }

        transform.position = target;
        canMove = true;
        deductActionPoints();
        
        //turnManager.nextTurn();

        // Input Buffer
        if (canMove && bufferedDirection.HasValue)
        {
            Vector3Int dir = bufferedDirection.Value;
            bufferedDirection = null;
            Movement(dir);
        }
    }

    private void deductActionPoints()
    {
        currentActionPoints--;
        if (currentActionPoints == 0)
        {
            EndTurn();
        }
    }

    private void EndTurn()
    {
        canMove = false;
        bufferedDirection = null;
        turnManager.nextTurn();
    }
}