
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TroopStatIndicator : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image iconImage;

    [Header("OPTIONAL SECONDARY VALUE")]
    [SerializeField] private TextMeshProUGUI secondaryValueText;

    public void SetValue(int value)
    {
        bool visible = value > 0;

        gameObject.SetActive(visible);

        if (visible && valueText != null)
            valueText.text = value.ToString();
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;
    }

    public void SetSecondaryValue(int value)
    {
        if (secondaryValueText == null)
            return;

        secondaryValueText.gameObject.SetActive(value > 0);

        if (value > 0)
            secondaryValueText.text = value.ToString();
    }
}
