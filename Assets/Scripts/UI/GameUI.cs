using UnityEngine;

/// Zentrale Anlaufstelle fuer alle HUD-Aenderungen. Selbst logikfrei.
public class GameUI : MonoBehaviour
{
    [SerializeField] private ActionPointDisplay actionPoints;
    [SerializeField] private HealthDisplay health;
    [SerializeField] private LevelDisplay level;
    

    public void SetActionPoints(int current, int max)
        => actionPoints.SetActionPoints(current, max);

    public void SetHealth(int current, int max)
        => health.SetHealth(current, max);
    
    public void SetLevel(int current, int max, int currentLevel)
        => level.SetLevel(current, max, currentLevel);
    
    
}