using System;
using UnityEngine;

public class UnitStats : MonoBehaviour
{
    [Header("Konfiguration")]
    [SerializeField] private SO_UnitStats config;
    [SerializeField] private SO_Tier tier;   // optional, beim Spieler leer lassen

    [Header("Laufzeit (Readonly)")]
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private int damage;

    public int MaxHealth     => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Damage        => damage;
    public bool IsDead       => currentHealth <= 0;
    public SO_Tier Tier      => tier;
    
    public bool IsVictoryTarget => config.IsVictoryTarget;

    public event Action<int> OnDamaged;   // Parameter: tatsaechlich abgezogener Schaden
    public event Action OnDied;
    public event System.Action<int, int> OnHealthChanged;
    private void NotifyHealthChanged() => OnHealthChanged?.Invoke(currentHealth, maxHealth);
    
    private void Awake()
    {
        float hpMul  = tier != null ? tier.HealthMultiplier : 1f;
        float dmgMul = tier != null ? tier.DamageMultiplier : 1f;

        // Mindestens 1, damit ein schlecht gesetzter Multiplikator keine 0-HP-Einheit erzeugt
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(config.MaxHealth * hpMul));
        damage    = Mathf.Max(1, Mathf.RoundToInt(config.Damage * dmgMul));

        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;   // schuetzt vor doppeltem OnDied bei Splash

        int applied = Mathf.Min(amount, currentHealth);
        currentHealth -= applied;

        OnDamaged?.Invoke(applied);
        NotifyHealthChanged();

        if (currentHealth <= 0)
            OnDied?.Invoke();
    }

    // --- Fuer F7: Progression schreibt hier rein, nie ins SO ---

    public void RaiseMaxHealth(int delta, bool healUp = true)
    {
        maxHealth = Mathf.Max(1, maxHealth + delta);
        if (healUp) currentHealth = Mathf.Min(currentHealth + Mathf.Max(0, delta), maxHealth);
        NotifyHealthChanged();
    }

    public void RaiseDamage(int delta)
    {
        damage = Mathf.Max(1, damage + delta);
    }
    
    
}
