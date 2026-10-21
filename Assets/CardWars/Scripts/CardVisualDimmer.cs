
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardVisualDimmer : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float darkness = 0.45f;

    private Graphic[] graphics;
    private readonly List<Color> originalColors = new();

    private bool isDarkened;

    private void Awake()
    {
        CacheOriginalColors();
    }

    public void CacheOriginalColors()
    {
        graphics = GetComponentsInChildren<Graphic>(true);

        originalColors.Clear();

        foreach (Graphic graphic in graphics)
            originalColors.Add(graphic.color);

        isDarkened = false;
    }

    public void SetDarkened(bool darkened)
    {
        if (graphics == null)
            CacheOriginalColors();

        if (isDarkened == darkened)
            return;

        isDarkened = darkened;

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null)
                continue;

            Color original = originalColors[i];

            if (darkened)
            {
                graphics[i].color = new Color(
                    original.r * darkness,
                    original.g * darkness,
                    original.b * darkness,
                    original.a
                );
            }
            else
            {
                graphics[i].color = original;
            }
        }
    }
}
