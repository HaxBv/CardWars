
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;

public class Portals : MonoBehaviour
{
    [Header("HEALTH")]
    [SerializeField, Min(1)]
    private int maxHealth = 50;

    [SerializeField, ReadOnly]
    private int currentHealth;

    [Header("SHIELDS")]
    [SerializeField, ReadOnly]
    private int remainingShields = 3;

    [Header("UI")]
    [SerializeField] private TMP_Text healthText;

    private const int MaxShields = 3;
    private const int MaxMegaCards = 4;

    private bool destructionProcessed;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int RemainingShields => remainingShields;

    public int MegaCardsUnlocked =>
        MaxMegaCards - remainingShields;

    public bool IsAlive => currentHealth > 0;

    public event Action<Portals> OnMegaCardRequested;
    public event Action<Portals> OnPortalDestroyed;

    private void Awake()
    {
        Initialize();
    }

    // =============================================
    // INITIALIZE
    // =============================================

    public void Initialize()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;

        remainingShields = MaxShields;
        destructionProcessed = false;

        RefreshDisplay();
    }

    // =============================================
    // NEXT SHIELD THRESHOLD
    // =============================================

    private int GetNextThreshold()
    {
        if (remainingShields <= 0)
            return -1;

        // 3 shields = 80%
        // 2 shields = 60%
        // 1 shield  = 40%

        float percentage =
            (remainingShields + 1) * 0.2f;

        return Mathf.CeilToInt(
            maxHealth * percentage
        );
    }

    // =============================================
    // DAMAGE
    // =============================================

    [Button]
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || destructionProcessed)
            return;

        int previousHealth = currentHealth;

        int resultingHealth = Mathf.Max(
            0,
            currentHealth - damage
        );

        int nextThreshold = GetNextThreshold();

        if (nextThreshold >= 0)
        {
            // Un escudo pendiente también protege si
            // su umbral quedó por encima de la salud
            // actual tras cambiar MaxHealth.
            int shieldLimit = Mathf.Min(
                currentHealth,
                nextThreshold
            );

            if (resultingHealth <= shieldLimit)
            {
                currentHealth = shieldLimit;

                remainingShields--;

                Debug.Log(
                    $"{gameObject.name}: Shield broken! " +
                    $"Remaining shields: {remainingShields}"
                );

                OnMegaCardRequested?.Invoke(this);
            }
            else
            {
                currentHealth = resultingHealth;
            }
        }
        else
        {
            // Sin escudos: daño completo.
            currentHealth = resultingHealth;
        }

        RefreshDisplay();

        Debug.Log(
            $"{gameObject.name}: " +
            $"{previousHealth} -> {currentHealth} HP"
        );

        if (currentHealth <= 0)
        {
            ConfirmDestruction();
        }
    }

    // =============================================
    // HEAL
    // =============================================

    [Button]
    public void Heal(int amount)
    {
        if (amount <= 0 || destructionProcessed)
            return;

        currentHealth = Mathf.Min(
            maxHealth,
            currentHealth + amount
        );

        RefreshDisplay();
    }

    // =============================================
    // INCREASE MAX HEALTH
    // =============================================

    [Button]
    public void IncreaseMaxHealth(int amount)
    {
        if (amount <= 0 || destructionProcessed)
            return;

        maxHealth += amount;
        currentHealth += amount;

        // Los escudos restantes usan el nuevo MaxHealth.
        // Los escudos rotos no se restauran.

        RefreshDisplay();
    }

    // =============================================
    // DESTRUCTION
    // =============================================

    private void ConfirmDestruction()
    {
        if (destructionProcessed)
            return;

        destructionProcessed = true;

        Debug.Log(
            $"{gameObject.name} was destroyed!"
        );

        OnPortalDestroyed?.Invoke(this);
    }

    // =============================================
    // DISPLAY
    // =============================================

    private void RefreshDisplay()
    {
        if (healthText == null)
            return;

        int nextThreshold = GetNextThreshold();

        string shieldInfo = remainingShields > 0
            ? $"<color=#70C8FF> {remainingShields}/3</color> " +
              $"<color=#AAAAAA>({nextThreshold} HP)</color>"
            : "<color=#888888> 0/3</color>";

        healthText.text =
            $"{currentHealth}<size=60%>/{maxHealth}</size>\n" +
            $"<size=55%>{shieldInfo}</size>";
    }
}
