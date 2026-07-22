using UnityEngine;

/// Zentrale Anlaufstelle fuer alle HUD-Aenderungen. Selbst logikfrei.
public class GameUI : MonoBehaviour
{
    [SerializeField] private ActionPointDisplay actionPoints;
    [SerializeField] private HealthDisplay health;

    public void SetActionPoints(int current, int max)
        => actionPoints.SetActionPoints(current, max);

    public void SetHealth(int current, int max)
        => health.SetHealth(current, max);
}