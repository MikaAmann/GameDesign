using UnityEngine;

[CreateAssetMenu(fileName = "SO_Tier", menuName = "Scriptable Objects/SO_Tier")]
public class SO_Tier : ScriptableObject
{
    [SerializeField] private string displayName = "Basic";

    [Header("Skalierung")]
    [SerializeField] private float healthMultiplier = 1f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float xpMultiplier = 1f;

    [Header("Darstellung")]
    [SerializeField] private Color tint = Color.white;

    public string DisplayName => displayName;
    public float HealthMultiplier => healthMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public float XpMultiplier => xpMultiplier;
    public Color Tint => tint;
}
