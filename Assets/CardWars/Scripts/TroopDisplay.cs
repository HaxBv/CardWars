
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;

public class TroopDisplay : MonoBehaviour
{
    [Header("COMPONENTS")]
    public SpriteRenderer TroopRenderer;
    public Animator TroopAnimator;

    [Header("DATA")]
    public TroopCardData CardData;
    public bool IsAlly;

    [Header("STATS UI")]
    public TextMeshProUGUI AttackText;
    public TextMeshProUGUI HealthText;

    [Header("MAIN ICONS")]
    [SerializeField] private Image attackIcon;
    [SerializeField] private Image healthIcon;

    [Header("ICON DATABASE")]
    [SerializeField] private TroopIconDatabase iconDatabase;

    [Header("EXTRA STATS")]
    [SerializeField] private TroopStatIndicator overstrikeIndicator;
    [SerializeField] private TroopStatIndicator shieldIndicator;
    [SerializeField] private TroopStatIndicator armorIndicator;
    [SerializeField] private TroopStatIndicator burnIndicator;

    [Header("SHIELD SPRITES")]
    [SerializeField] private Sprite normalShieldSprite;
    [SerializeField] private Sprite armoredShieldSprite;

    private bool impactTriggered;
    public bool ImpactTriggered => impactTriggered;

    public bool attackFinished;
    public bool AttackFinished => attackFinished;

    public event Action OnAttackImpact;

    // =====================================================
    // INITIALIZE
    // =====================================================

    public void Initialize(TroopCardData data, bool isAlly)
    {
        if (data == null)
        {
            Debug.LogWarning("No se asignó TroopCardData.");
            return;
        }

        CardData = data;
        IsAlly = isAlly;

        // SPRITE
        if (TroopRenderer != null)
        {
            TroopRenderer.sprite = data.ImgCharInGame;
            TroopRenderer.flipX = !isAlly;
        }

        // ANIMACIÓN
        if (TroopAnimator != null && data.Anim != null)
        {
            TroopAnimator.runtimeAnimatorController = data.Anim;
        }

        // ESTADÍSTICAS
        RefreshStats(data.Attack, data.Health);

        // Evitar mostrar indicadores de una tropa anterior
        // cuando se reutiliza el prefab desde la pool.
        ClearExtraStats();
    }

    // =====================================================
    // STATS
    // =====================================================

    public void RefreshStats(int attack, int health)
    {
        if (AttackText != null)
            AttackText.text = attack.ToString();

        if (HealthText != null)
            HealthText.text = health.ToString();

    }

    // =====================================================
    // MAIN ICONS
    // =====================================================

    public void RefreshPassiveIcons(
        IReadOnlyList<AbilityPassive> passives)
    {
        if (iconDatabase == null)
            return;

        SetIcon(
            attackIcon,
            iconDatabase.ResolveIcon(
                passives,
                TroopIconCategory.Offensive
            )
        );

        SetIcon(
            healthIcon,
            iconDatabase.ResolveIcon(
                passives,
                TroopIconCategory.Defensive
            )
        );
    }

    private void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    // =====================================================
    // EXTRA STATS
    // =====================================================


    public void RefreshExtraStats(
        int overstrike,
        int shield,
        int shieldArmor,
        int armor,
        int burn)
    {
        if (overstrikeIndicator != null)
            overstrikeIndicator.SetValue(overstrike);

        if (shieldIndicator != null)
        {
            Sprite shieldSprite = shieldArmor > 0
                ? armoredShieldSprite
                : normalShieldSprite;

            shieldIndicator.SetIcon(shieldSprite);
            shieldIndicator.SetValue(shield);
            shieldIndicator.SetSecondaryValue(
                shield > 0 ? shieldArmor : 0
            );
        }

        if (armorIndicator != null)
            armorIndicator.SetValue(armor);

        if (burnIndicator != null)
            burnIndicator.SetValue(burn);
    }

    private void ClearExtraStats()
    {
        RefreshExtraStats(0, 0, 0, 0, 0);
    }

    // =====================================================
    // ATTACK ANIMATION
    // =====================================================

    public void PlayAttack()
    {
        if (TroopAnimator == null)
            return;

        impactTriggered = false;
        attackFinished = false;

        TroopAnimator.SetTrigger("Attack");
    }

    // Animation Event: frame del impacto.
    public void AttackImpact()
    {
        impactTriggered = true;

        OnAttackImpact?.Invoke();
    }

    // Animation Event: final del ataque.
    public void AttackEnd()
    {
        attackFinished = true;
    }

    public void PlayHurt()
    {
        if (TroopAnimator != null)
            TroopAnimator.SetTrigger("Hurt");
    }
}
