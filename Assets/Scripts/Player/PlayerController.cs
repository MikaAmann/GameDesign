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

    private UnitStats stats;
    private GameManager gameManager;
    //[SerializeField] private float moveDuration;
    
    
    [SerializeField] private int maxActionPoints = 3;
    private int currentActionPoints;
    
    private bool isMyTurn = true;    // Zug-Gate: darf ich ueberhaupt handeln?
    private bool isAnimating;        // Animationslock: laeuft gerade eine Coroutine?
    
    public GridEntity entity;
    private Vector3Int? bufferedDirection = null;
    private PlayerLeveling leveling;
    

    void Awake()
    {
        entity = GetComponent<GridEntity>();
        view = GetComponent<UnitView>();
        stats =  GetComponent<UnitStats>();
        leveling = GetComponent<PlayerLeveling>();
        
        //walkabilityService = Services.I.Walkability;
        gridState          = Services.I.Grid;
        actionResolver     = Services.I.Resolver;
        tilemap            = Services.I.Tilemap;
        gameManager        = Services.I.Game;
        
        
        stats.OnDied += HandleDeath;
        stats.OnHealthChanged += RefreshHealthUI;
        leveling.OnLevelChanged += RefreshLevelUI;
    }
    
    private void Start()
    {
        entity.CurrentCell = tilemap.WorldToCell(transform.position);
        gridState.Register(entity.CurrentCell, gameObject);  // neu
        view.SnapToCell(entity.CurrentCell);
        
        currentActionPoints = maxActionPoints;
        RefreshActionPointUI();
        RefreshHealthUI(stats.CurrentHealth, stats.MaxHealth);
    }
    
    private void HandleDeath()
    {
        isMyTurn = false;
        bufferedDirection = null;
        gameManager.ReportPlayerDeath();
    }
    
    private void RefreshHealthUI(int current, int max)
        => Services.I.UI.SetHealth(current, max);
    
    private void RefreshLevelUI(int currentXp, int maxXp, int level)
        => Services.I.UI.SetLevel(currentXp, maxXp, level);

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.OnDied -= HandleDeath;
            stats.OnHealthChanged -= RefreshHealthUI;
        }
        if (leveling != null)
            leveling.OnLevelChanged -= RefreshLevelUI;
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

        //Cheats for leveldebug
        if (kb.cKey.wasPressedThisFrame) GetComponent<PlayerLeveling>().GainXp(1);
        if (kb.vKey.wasPressedThisFrame) GetComponent<PlayerLeveling>().GainXp(2);
        if (kb.bKey.wasPressedThisFrame) GetComponent<PlayerLeveling>().GainXp(3);
        
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
                // Nur angreifbar, wenn dort etwas mit Kampfwerten steht (Truhen haben keine)
                if (result.targetObject != null && result.targetObject.TryGetComponent(out UnitStats _))
                {
                    isAnimating = true;
                    StartCoroutine(AttackRoutine(targetCell, result.targetObject));
                }
                break;
            case ActionResolver.MoveResult.Blocked:
                // nichts tun
                break;
        }
    }
    
    private IEnumerator AttackRoutine(Vector3Int targetCell, GameObject target)
    {
        yield return view.AttackHop(
            targetCell,
            () => actionResolver.ApplyDamage(target, stats.Damage));

        isAnimating = false;
        deductActionPoints();

        ConsumeBufferedInput();
    }
    
    private void ConsumeBufferedInput()
    {
        if (!isMyTurn || !bufferedDirection.HasValue) return;

        Vector3Int dir = bufferedDirection.Value;
        bufferedDirection = null;
        Movement(dir);
    }
    
    public void setActiveTurn()
    {
        isMyTurn = true;
        isAnimating = false;
        bufferedDirection = null;       // alter Input aus dem Gegnerzug wird nicht nachgeholt
        currentActionPoints = maxActionPoints;
        RefreshActionPointUI();
    }

    private IEnumerator MovePlayer()
    {
        yield return view.MoveTo(entity.CurrentCell);

        isAnimating = false;
        deductActionPoints();

        ConsumeBufferedInput();
    }

    private void deductActionPoints()
    {
        currentActionPoints--;
        RefreshActionPointUI();
        if (currentActionPoints <= 0)
            EndTurn();
    }
    
    private void RefreshActionPointUI()
        => Services.I.UI.SetActionPoints(currentActionPoints, maxActionPoints);

    private void EndTurn()
    {
        isMyTurn = false;
        bufferedDirection = null;
        turnManager.nextTurn();
    }
    
    public void IncreaseMaxActionPoints(int amount){
        maxActionPoints += amount;
        RefreshActionPointUI();
    }
}