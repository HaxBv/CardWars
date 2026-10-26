
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class TroopInGame : MonoBehaviour
{
    [Header("DATA")]
    public TroopCardData CardData { get; private set; }
    public bool IsAlly { get; private set; }

    [Header("CURRENT STATS")]
    [SerializeField] private int currentAttack;
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentArmor;
    [SerializeField] private int currentShield;
    [SerializeField] private int currentShieldArmor;
    [SerializeField] private int currentBurn;
    [SerializeField] private int currentOverstrike;

    public int CurrentAttack => currentAttack;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentArmor => currentArmor;
    public int CurrentShield => currentShield;
    public int CurrentShieldArmor => currentShieldArmor;
    public int CurrentBurn => currentBurn;
    public int CurrentOverstrike => currentOverstrike;

    public bool IsAlive => currentHealth > 0;

    // Se consulta al preparar los ataques del carril.
    public bool CanAttack =>
        CardData != null &&
        IsAlive &&
        CurrentAttack > 0 &&
        !deathProcessed;

    private TroopDisplay troopDisplay;

    private bool deployed;
    private bool deathProcessed;

    // Eventos reservados para futuras habilidades.
    public event Action<TroopInGame> OnDeploy;
    public event Action<TroopInGame, int, TroopInGame> OnDamaged;
    public event Action<TroopInGame, TroopInGame> OnKill;
    public event Action<TroopInGame> OnDeath;

    public void Initialize(TroopCardData data, bool isAlly)
    {
        if (data == null)
            return;

        CardData = data;
        IsAlly = isAlly;

        currentAttack = data.Attack;
        maxHealth = data.Health;
        currentHealth = maxHealth;

        currentArmor = data.Armor;
        currentShield = data.Shield;
        currentShieldArmor = data.ShieldArmor;
        currentBurn = data.Burn;
        currentOverstrike = data.Overstrike;

        deployed = false;
        deathProcessed = false;

        troopDisplay = GetComponentInChildren<TroopDisplay>();

        if (troopDisplay != null)
        {
            troopDisplay.Initialize(data, isAlly);
            RefreshDisplay();
        }
    }

    public void NotifyDeploy()
    {
        if (deployed || CardData == null)
            return;

        deployed = true;
        OnDeploy?.Invoke(this);
    }

    // =====================================================
    // DAMAGE
    // =====================================================

    [Button]
    public void TakeDamage(int damage, TroopInGame attacker = null)
    {
        if (damage <= 0 || deathProcessed)
            return;

        // La salud nunca baja de 0.
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Actualizar HP visual inmediatamente.
        RefreshDisplay();

        // Reproducir Hurt al recibir daño.
        if (troopDisplay != null)
            troopDisplay.PlayHurt();

        // No ejecutar Die() todavía.
        // La muerte se confirma al finalizar el carril.
    }


    // =====================================================
    // HEAL
    // =====================================================

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        currentHealth = Mathf.Min(
            currentHealth + amount,
            maxHealth
        );

        RefreshDisplay();
    }

    // =====================================================
    // DEATH
    // =====================================================

    public bool ConfirmDeath()
    {
        if (deathProcessed || CurrentHealth > 0)
            return false;

        deathProcessed = true;
        return true;
    }

    // Reservado para cuando implementemos
    // los triggers de muerte y eliminación.
    private void Die(TroopInGame attacker)
    {
        if (deathProcessed)
            return;

        deathProcessed = true;

        OnDeath?.Invoke(this);

        if (attacker != null && attacker != this)
        {
            attacker.OnKill?.Invoke(attacker, this);
        }
    }

    // =====================================================
    // DISPLAY
    // =====================================================

    private void RefreshDisplay()
    {
        if (troopDisplay != null)
        {
            troopDisplay.RefreshStats(
                CurrentAttack,
                CurrentHealth
            );
        }
    }
}
