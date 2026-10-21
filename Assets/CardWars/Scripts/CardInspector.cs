
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CardInspector : MonoBehaviour
{
    public static CardInspector Instance;

    [Header("PANEL")]
    [SerializeField] private GameObject inspectorPanel;

    [Header("CARD PREVIEW")]
    [SerializeField] private CardDisplay cardPreview;

    [Header("EXTRA INFORMATION")]
    [SerializeField] private TextMeshProUGUI cardDescription;
    [SerializeField] private TextMeshProUGUI cardPhrase;

    [Header("CHARACTER PREVIEW")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Animator characterAnimator;

    private int openedFrame;

    private void Awake()
    {
        Instance = this;

        if (inspectorPanel != null)
            inspectorPanel.SetActive(false);
    }
    private void AdjustCharacterImage()
    {
        if (characterImage == null || characterImage.sprite == null)
            return;

        Sprite sprite = characterImage.sprite;
        RectTransform rect = characterImage.rectTransform;

        // Obtener el Custom Pivot del sprite (0 a 1).
        Vector2 spritePivot = new Vector2(
            sprite.pivot.x / sprite.rect.width,
            sprite.pivot.y / sprite.rect.height
        );

        // Aplicar ese pivote al RectTransform.
        rect.pivot = spritePivot;

        // Conservar proporciones y tamaño nativo.
        characterImage.preserveAspect = true;
        characterImage.SetNativeSize();
    }
    public void Open(TroopCardData cardData)
    {
        if (cardData == null || inspectorPanel == null)
            return;

        inspectorPanel.SetActive(true);
        // Actualizar la carta de la izquierda.
        if (cardPreview != null)
            cardPreview.MostrarTropa(cardData);

        // Actualizar los textos de la derecha.
        if (cardDescription != null)
            cardDescription.text = cardData.DescriptionAbility;

        if (cardPhrase != null)
            cardPhrase.text = cardData.Phrase;

        // Mostrar el sprite del personaje.
        if (characterImage != null)
        {
            characterImage.sprite = cardData.ImgCharInGame;
            characterImage.enabled = cardData.ImgCharInGame != null;
            AdjustCharacterImage();
            if (characterImage.sprite != null)
            {
                characterImage.preserveAspect = true;
                characterImage.SetNativeSize();
            }
        }
        // Configurar la animación, si es un Animator Controller.
        if (characterAnimator != null)
        {
            characterAnimator.runtimeAnimatorController = null;

            UnityEngine.Object animationAsset = cardData.Anim;

            if (animationAsset is RuntimeAnimatorController controller)
            {
                characterAnimator.runtimeAnimatorController = controller;
                characterAnimator.Rebind();
                characterAnimator.Update(0f);
            }
        }

        openedFrame = Time.frameCount;
    }

    public void Close()
    {
        if (inspectorPanel != null)
            inspectorPanel.SetActive(false);
    }

    private void Update()
    {
        if (inspectorPanel == null || !inspectorPanel.activeSelf)
            return;

        // Evitar que el clic de apertura cierre el panel.
        if (Time.frameCount == openedFrame)
            return;

        bool clicked = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            clicked = Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        clicked = Input.GetMouseButtonDown(0);
#endif

        if (clicked)
            Close();
    }
}
