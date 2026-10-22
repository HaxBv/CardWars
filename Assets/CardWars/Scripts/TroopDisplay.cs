
using TMPro;
using UnityEngine;

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
    }
    public void RefreshStats(int attack, int health)
    {
        if (AttackText != null)
            AttackText.text = attack.ToString();

        if (HealthText != null)
            HealthText.text = health.ToString();
    }
}
