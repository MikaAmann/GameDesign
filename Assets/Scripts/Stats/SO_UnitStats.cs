using UnityEngine;

[CreateAssetMenu(fileName = "UnitStats", menuName = "Scriptable Objects/UnitStats")]
public class SO_UnitStats : ScriptableObject
{
    
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int damage = 2;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private int xpReward = 2;
    [SerializeField] private bool isVictoryTarget; // true nur beim Koenig
    
    public bool IsVictoryTarget => isVictoryTarget;
    
    public int MaxHealth => maxHealth;
    public int Damage => damage;
    
    public float DetectionRadius => detectionRadius;
    
    public int XpReward => xpReward;
}
