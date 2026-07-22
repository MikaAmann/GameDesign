using UnityEngine;

// Verwaltet Level, Erfahrungspunkte und die Belohnungen beim Aufstieg.
// Einzige oeffentliche Schnittstelle: GainXp. Egal ob Gegner oder Truhe.
public class PlayerLeveling : MonoBehaviour
{
    [Header("Fortschritt")]
    [SerializeField] private int xpPerLevel = 10;    // konstant, bewusst nicht skalierend

    [Header("Belohnung pro Level")]
    [SerializeField] private int damagePerLevel = 2;
    [SerializeField] private int healthPerLevel = 2;
    [SerializeField] private int apEveryNLevels = 2; // jedes 2. Level ein Aktionspunkt
    
    [Header("Debug Level")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXp;

    
    private UnitStats stats;
    private PlayerController player;

    public event System.Action<int, int, int> OnLevelChanged;
    private void NotifyLevelChanged()
        => OnLevelChanged?.Invoke(currentXp, xpPerLevel, currentLevel);

    public int CurrentLevel => currentLevel;
    public int CurrentXp    => currentXp;
    public int XpThreshold  => xpPerLevel;

    private void Awake()
    {
        stats  = GetComponent<UnitStats>();
        player = GetComponent<PlayerController>();
        
    }

    private void Start()
    {
        // Startwerte an die UI melden, damit die Anzeige nicht leer bleibt
        NotifyLevelChanged();
    }

    // Zentrale Eintrittsstelle fuer jede XP-Quelle.
    public void GainXp(int amount)
    {
        if (amount <= 0) return;

        currentXp += amount;

        // while, nicht if: ein starker Gegner kann zwei Level auf einmal ausloesen
        while (currentXp >= xpPerLevel)
        {
            currentXp -= xpPerLevel;   // Rest uebertragen, nichts verfaellt
            LevelUp();
        }

        NotifyLevelChanged();
    }

    private void LevelUp()
    {
        xpPerLevel += currentLevel * 2;
        
        currentLevel++;
        
        stats.RaiseDamage(damagePerLevel);
        stats.RaiseMaxHealth(healthPerLevel);

        // Voll heilen: der Aufstieg soll sich nach Staerke anfuehlen,
        // nicht nach einem groesseren, aber leereren Balken
        stats.HealFull();

        if (currentLevel % apEveryNLevels == 0)
            player.IncreaseMaxActionPoints(1);
    }
}