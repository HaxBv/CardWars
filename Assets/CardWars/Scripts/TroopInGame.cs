
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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


    [Header("STATUS MARKS")]
    [SerializeField] private bool isStunned;
    [SerializeField, Min(0)] private int burnMark;
    [SerializeField, Min(0)] private int poisonRemainingPeriods;
    [SerializeField] private bool isBleeding;

    public bool IsStunned => isStunned;
    public int BurnMark => burnMark;
    public int PoisonRemainingPeriods => poisonRemainingPeriods;
    public bool IsPoisoned => poisonRemainingPeriods > 0;
    public bool IsBleeding => isBleeding;

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

    [Header("CURRENT PASSIVES")]
    [SerializeField] private List<AbilityPassive> currentPassives = new();

    public IReadOnlyList<AbilityPassive> CurrentPassives => currentPassives;

    public bool HasPassive(AbilityPassive passive)
    {
        return currentPassives.Contains(passive);
    }

    public void AddPassive(AbilityPassive passive)
    {
        if (passive == AbilityPassive.None ||
            currentPassives.Contains(passive))
            return;

        currentPassives.Add(passive);
    }

    public void RemovePassive(AbilityPassive passive)
    {
        currentPassives.Remove(passive);
    }

    public void Initialize(TroopCardData data, bool isAlly)
    {
        if (data == null)
            return;

        CardData = data;
        IsAlly = isAlly;

        currentPassives.Clear();

        if (data.AbilityPassives != null)
        {
            foreach (AbilityPassive passive in data.AbilityPassives)
            {
                AddPassive(passive);
            }
        }

        // Reiniciar estadísticas.
        currentAttack = data.Attack;
        maxHealth = data.Health;
        currentHealth = maxHealth;

        currentArmor = data.Armor;
        currentShield = data.Shield;
        currentShieldArmor = data.ShieldArmor;
        currentBurn = data.Burn;
        currentOverstrike = data.Overstrike;

        // Reiniciar estados.
        deployed = false;
        deathProcessed = false;

        isStunned = false;
        burnMark = 0;
        poisonRemainingPeriods = 0;
        isBleeding = false;

        // Obtener componentes, incluso si están inactivos.
        troopDisplay = GetComponentInChildren<TroopDisplay>(true);

        // Reiniciar animaciones anteriores.
        Animator animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // Actualizar apariencia y estadísticas.
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


    #region STATUS MARKS

    // STUN: no acumulable.
    public void ApplyStun()
    {
        if (!IsAlive || deathProcessed)
            return;

        isStunned = true;
    }

    public bool ConsumeStun()
    {
        if (!isStunned)
            return false;

        isStunned = false;
        return true;
    }

    // BURN: almacena el daño adicional pendiente.
    public void ApplyBurn(int amount)
    {
        if (!IsAlive || deathProcessed || amount <= 0)
            return;

        burnMark = amount;
    }

    public int ConsumeBurn()
    {
        int amount = burnMark;
        burnMark = 0;
        return amount;
    }

    // POISON: la duración se acumula.
    public void ApplyPoison(int duration)
    {
        if (!IsAlive || deathProcessed || duration <= 0)
            return;

        poisonRemainingPeriods =
            Mathf.Min(10, poisonRemainingPeriods + duration);
    }

    public bool ConsumePoisonPeriod()
    {
        if (poisonRemainingPeriods <= 0)
            return false;

        poisonRemainingPeriods--;
        return true;
    }

    // BLEEDING: marca de un solo uso.
    public void ApplyBleeding()
    {
        if (!IsAlive || deathProcessed)
            return;

        isBleeding = true;
    }

    public bool ConsumeBleeding()
    {
        if (!isBleeding)
            return false;

        isBleeding = false;
        return true;
    }

    #endregion

}
