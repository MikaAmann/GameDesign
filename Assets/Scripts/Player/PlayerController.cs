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
    [SerializeField] private UnitView view; 

    [SerializeField] private float moveDuration;
    
    
    
    //Todo: SCriptableObject mit allen Relevanten Stats und mechanismen zum füllen der Variablen
    //Todo: Logiken der Stats erweitern
    [Header("Stats (Readonly)")] 
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxActionPoints = 3;
    private int currentActionPoints;
    
    private bool isMyTurn = true;    // Zug-Gate: darf ich ueberhaupt handeln?
    private bool isAnimating;        // Animationslock: laeuft gerade eine Coroutine?
    
    public GridEntity entity;
    private Vector3Int? bufferedDirection = null;


    void Awake()
    {
        entity = GetComponent<GridEntity>();
        view = GetComponent<UnitView>();
        
        //walkabilityService = Services.I.Walkability;
        gridState          = Services.I.Grid;
        actionResolver     = Services.I.Resolver;
        tilemap            = Services.I.Tilemap;
    }
    
    private void Start()
    {
        entity.CurrentCell = tilemap.WorldToCell(transform.position);
        gridState.Register(entity.CurrentCell, gameObject);  // neu
        view.SnapToCell(entity.CurrentCell);
        
        currentActionPoints = maxActionPoints;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        
        if (isMyTurn && !isAnimating && kb.enterKey.wasPressedThisFrame)
        {
            EndTurn();
            return;
        }

        // Richtung einlesen
        Vector3Int? input = null;
        if (kb.wKey.wasPressedThisFrame) input = Vector3Int.up;
        if (kb.sKey.wasPressedThisFrame) input = Vector3Int.down;
        if (kb.aKey.wasPressedThisFrame) input = Vector3Int.left;
        if (kb.dKey.wasPressedThisFrame) input = Vector3Int.right;

        if (input == null) return;

        if (!isMyTurn) return;                  // Fremder Zug: Eingabe verwerfen, NICHT puffern

        if (isAnimating)
        {
            bufferedDirection = input;          // Puffern nur waehrend der eigenen Animation
            return;
        }

        Movement(input.Value);
    }

    private void Movement(Vector3Int direction)
    {
        Vector3Int targetCell = entity.CurrentCell + direction;
        
        ActionResolver.MoveReturn result = actionResolver.MoveRequest(entity, targetCell);

        switch (result.moveResult)
        {
            case ActionResolver.MoveResult.Moved:
                isAnimating = true;
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
    
    public void setActiveTurn()
    {
        isMyTurn = true;
        isAnimating = false;
        bufferedDirection = null;       // alter Input aus dem Gegnerzug wird nicht nachgeholt
        currentActionPoints = maxActionPoints;
    }

    private IEnumerator MovePlayer()
    {
        yield return view.MoveTo(entity.CurrentCell);

        isAnimating = false;
        deductActionPoints();

        if (isMyTurn && bufferedDirection.HasValue)
        {
            Vector3Int dir = bufferedDirection.Value;
            bufferedDirection = null;
            Movement(dir);
        }
    }

    private void deductActionPoints()
    {
        currentActionPoints--;
        if (currentActionPoints <= 0)
            EndTurn();
    }

    private void EndTurn()
    {
        isMyTurn = false;
        bufferedDirection = null;
        turnManager.nextTurn();
    }
}