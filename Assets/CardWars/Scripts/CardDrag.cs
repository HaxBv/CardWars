
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CardDisplay cardDisplay;
    private CanvasGroup canvasGroup;

    private Vector3 originalPosition;
    private Vector3 dragOffset;

    private Camera mainCamera;
    private LandLane currentLane;
    private LandLane[] lanes;

    private int originalSiblingIndex;
    private bool canDrag = true;

    public void SetDraggable(bool value)
    {
        canDrag = value;
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cardDisplay = GetComponentInParent<CardDisplay>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        mainCamera = Camera.main;

        DeckHandManager.Instance = GetComponentInParent<DeckHandManager>();

        ShowDescription(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;
        // Colocar la carta por encima de las demás.
        originalSiblingIndex = cardDisplay.transform.GetSiblingIndex();
        cardDisplay.transform.SetAsLastSibling();

        originalPosition = rectTransform.position;
        canvasGroup.blocksRaycasts = false;

        // Buscar todos los carriles.
        lanes = FindObjectsByType<LandLane>(
            FindObjectsSortMode.None
        );

        // Mostrar inmediatamente TODOS los slots disponibles.
        foreach (LandLane lane in lanes)
        {
            lane.SetPreviewCard(cardDisplay.cardDataSO);
            lane.ShowAvailable();
        }

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 mousePosition
        );

        dragOffset = rectTransform.position - mousePosition;

        UpdateLaneHighlight(eventData.position);

        // Volver transparentes las demás cartas.
        if (DeckHandManager.Instance != null)
        {
            DeckHandManager.Instance.SetOtherCardsAlpha(
                cardDisplay,
                true
            );
        }

        ShowDescription(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 mousePosition))
        {
            rectTransform.position = mousePosition + dragOffset;
        }

        UpdateLaneHighlight(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!canDrag)
            return;
        canvasGroup.blocksRaycasts = true;

        UpdateLaneHighlight(eventData.position);

        bool deployed = false;

        if (currentLane != null && cardDisplay != null)
        {
            deployed = currentLane.DeployTroop(
                cardDisplay.cardDataSO,
                GetWorldPosition(eventData.position)
            );
        }

        // Ocultar los indicadores de todos los carriles.
        HideAllLanes();

        currentLane = null;

        if (BoardManager.Instance != null)
            BoardManager.Instance.SetSelectedLane(null);

        // Restaurar la posición y el orden de la carta.
        rectTransform.position = originalPosition;
        cardDisplay.transform.SetSiblingIndex(originalSiblingIndex);

        // Restaurar la apariencia de la mano.
        if (DeckHandManager.Instance != null)
        {
            DeckHandManager.Instance.SetOtherCardsAlpha(
                cardDisplay,
                false
            );
        }

        ShowDescription(false);

        // Devolver la carta visual al pool si fue desplegada.
        if (deployed)
        {
            DeckHandManager hand =
                cardDisplay.GetComponentInParent<DeckHandManager>();

            if (hand != null)
                hand.ReturnCardToPool(cardDisplay.gameObject);
        }
    }

    private void UpdateLaneHighlight(Vector2 screenPosition)
    {
        if (lanes == null)
            return;

        Vector2 worldPosition = GetWorldPosition(screenPosition);

        LandLane detectedLane = null;

        foreach (LandLane lane in lanes)
        {
            if (lane.ContainsPosition(worldPosition))
            {
                detectedLane = lane;
                break;
            }
        }

        // Detectar si cambiamos de carril.
        if (currentLane != detectedLane)
        {
            // Restaurar los slots disponibles del carril anterior.
            if (currentLane != null)
                currentLane.RemoveHighlight();

            currentLane = detectedLane;

            // Informar al BoardManager.
            if (BoardManager.Instance != null)
                BoardManager.Instance.SetSelectedLane(currentLane);
        }

        // Resaltar la casilla bajo el cursor.
        if (currentLane != null)
            currentLane.UpdateSlotHighlight(worldPosition);
    }

    private void ShowDescription(bool show)
    {
        if (cardDisplay == null ||
            cardDisplay.CardDescriptionPanel == null)
            return;

        bool hasDescription =
            cardDisplay.cardDataSO != null &&
            !string.IsNullOrWhiteSpace(
                cardDisplay.cardDataSO.DescriptionAbility
            );

        cardDisplay.CardDescriptionPanel.SetActive(
            show && hasDescription
        );
    }

    private void HideAllLanes()
    {
        if (lanes == null)
            return;

        foreach (LandLane lane in lanes)
            lane.HideHighlight();
    }

    private void OnDisable()
    {
        if (DeckHandManager.Instance != null)
        {
            DeckHandManager.Instance.SetOtherCardsAlpha(
                cardDisplay,
                false
            );
        }

        ShowDescription(false);
        HideAllLanes();

        currentLane = null;

        if (BoardManager.Instance != null)
            BoardManager.Instance.SetSelectedLane(null);
    }

    private Vector2 GetWorldPosition(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return Vector2.zero;

        float distance = Mathf.Abs(
            mainCamera.transform.position.z
        );

        Vector3 position = mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                distance
            )
        );

        return new Vector2(position.x, position.y);
    }
}
