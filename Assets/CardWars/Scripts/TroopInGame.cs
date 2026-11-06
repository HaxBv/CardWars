
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TroopInGame : MonoBehaviour
{
    [Header("DATA")]
    public TroopCardData CardData { get; private set; }
    public TroopCardInstance CardInstance { get; private set; }
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
    public bool CanAttack => CardInstance != null && IsAlive && CurrentAttack > 0 && !deathProcessed;

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

        if (deployed)
            RefreshDisplay();
    }

    public void RemovePassive(AbilityPassive passive)
    {
        if (!currentPassives.Remove(passive))
            return;

        if (deployed)
            RefreshDisplay();
    }


    public void Initialize(TroopCardInstance card, bool isAlly)
    {
        if (card == null)
        {
            Debug.LogError("No se puede inicializar una tropa sin CardInstance.");
            return;
        }

        CardInstance = card;
        CardData = card.CardData as TroopCardData;
        IsAlly = isAlly;

        if (CardData == null)
        {
            Debug.LogError("La instancia no contiene un TroopCardData válido.");
            CardInstance = null;
            return;
        }

        // ==========================================
        // CURRENT STATS
        // ==========================================

        currentAttack = card.CurrentAttack;

        maxHealth = card.CurrentHealth;
        currentHealth = maxHealth;

        currentArmor = card.CurrentArmor;
        currentShield = card.CurrentShield;
        currentShieldArmor = card.CurrentShieldArmor;

        currentBurn = card.CurrentBurn;
        currentOverstrike = card.CurrentOverstrike;

        // ==========================================
        // CURRENT PASSIVES
        // ==========================================

        currentPassives.Clear();

        foreach (AbilityPassive passive in card.CurrentPassives)
        {
            AddPassive(passive);
        }

        // ==========================================
        // RESET STATUS MARKS
        // ==========================================

        isStunned = false;
        burnMark = 0;
        poisonRemainingPeriods = 0;
        isBleeding = false;

        // ==========================================
        // RESET DEPLOYMENT STATE
        // ==========================================

        deployed = false;
        deathProcessed = false;

        // ==========================================
        // RESET ANIMATOR
        // ==========================================

        Animator animator = GetComponentInChildren<Animator>(true);

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // ==========================================
        // TROOP DISPLAY
        // ==========================================

        troopDisplay = GetComponentInChildren<TroopDisplay>(true);

        if (troopDisplay != null)
        {
            troopDisplay.Initialize(CardData, isAlly);
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
    public DamageResult TakeDamage(
        int damage,
        TroopInGame attacker = null)
    {
        if (damage <= 0 || deathProcessed || !IsAlive)
            return new DamageResult(0, 0, 0, false);

        bool hasBreak = attacker != null &&
            attacker.HasPassive(AbilityPassive.Break);

        int healthBefore = currentHealth;
        int shieldBefore = currentShield;

        int healthDamage = 0;
        int shieldDamage = 0;
        int blockedDamage = 0;

        if (currentShield > 0)
        {
            if (hasBreak)
            {
                // Break destruye todo el escudo.
                currentShield = 0;
                shieldDamage = shieldBefore;

                // Solo el daño sobrante pasa al HP.
                int excessDamage = Mathf.Max(
                    0,
                    damage - shieldBefore
                );

                healthDamage = Mathf.Min(
                    currentHealth,
                    excessDamage
                );

                currentHealth -= healthDamage;
            }
            else
            {
                // El escudo utiliza Shield Armor.
                int effectiveDamage = Mathf.Max(
                    0,
                    damage - currentShieldArmor
                );

                blockedDamage = damage - effectiveDamage;

                shieldDamage = Mathf.Min(
                    currentShield,
                    effectiveDamage
                );

                currentShield -= shieldDamage;

                // Sin Break, el exceso no llega al HP.
            }
        }
        else
        {
            // Break ignora ARMOR.
            int effectiveDamage = hasBreak
                ? damage
                : Mathf.Max(0, damage - currentArmor);

            blockedDamage = damage - effectiveDamage;

            healthDamage = Mathf.Min(
                currentHealth,
                effectiveDamage
            );

            currentHealth -= healthDamage;
        }

        bool shieldDestroyed =
            shieldBefore > 0 && currentShield == 0;

        if (healthDamage > 0 || shieldDamage > 0)
        {
            RefreshDisplay();

            if (troopDisplay != null)
                troopDisplay.PlayHurt();
        }

        if (healthDamage > 0)
        {
            OnDamaged?.Invoke(
                this,
                healthDamage,
                attacker
            );
        }

        return new DamageResult(
            healthDamage,
            shieldDamage,
            blockedDamage,
            shieldDestroyed
        );
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
        if (troopDisplay == null)
            return;

        troopDisplay.RefreshStats(
            CurrentAttack,
            CurrentHealth
        );

        troopDisplay.RefreshPassiveIcons(
            CurrentPassives
        );

        troopDisplay.RefreshExtraStats(
            CurrentOverstrike,
            CurrentShield,
            CurrentShieldArmor,
            CurrentArmor,
            CurrentBurn
        );
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
