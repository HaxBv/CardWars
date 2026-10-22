
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

    public bool IsAlive => CurrentHealth > 0;

    private TroopDisplay troopDisplay;
    private bool deployed;
    private bool deathProcessed;

    // Eventos para conectar habilidades posteriormente.
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
            troopDisplay.RefreshStats(
                CurrentAttack,
                CurrentHealth
            );
        }
    }

    public void NotifyDeploy()
    {
        if (deployed || CardData == null)
            return;

        deployed = true;
        OnDeploy?.Invoke(this);
    }
    [Button]
    public void TakeDamage(int damage, TroopInGame attacker = null)
    {
        if (!IsAlive || damage <= 0)
            return;

        // Temporalmente el daño se aplica directamente al HP.
        int actualDamage = Mathf.Min(damage, CurrentHealth);

        currentHealth -= actualDamage;

        RefreshDisplay();

        OnDamaged?.Invoke(this, actualDamage, attacker);

        if (CurrentHealth <= 0)
        {
            Die(attacker);
        }
    }

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

    private void Die(TroopInGame attacker)
    {
        if (deathProcessed)
            return;

        deathProcessed = true;

        // Notificar la muerte antes de retirar la tropa.
        OnDeath?.Invoke(this);

        // Notificar al atacante que eliminó una tropa.
        if (attacker != null && attacker != this)
        {
            attacker.OnKill?.Invoke(attacker, this);
        }

        // La retirada del tablero se conectará con LandLane.
    }

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
