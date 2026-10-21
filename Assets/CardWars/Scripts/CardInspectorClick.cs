
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInspectorClick : MonoBehaviour,
    IPointerDownHandler,
    IPointerClickHandler
{
    [SerializeField] private CardDisplay cardDisplay;

    private CardDrag cardDrag;

    private void Awake()
    {
        if (cardDisplay == null)
            cardDisplay = GetComponentInChildren<CardDisplay>(true);

        cardDrag = GetComponentInChildren<CardDrag>(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cardDrag != null)
            cardDrag.ResetDragClickFlag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Click en: " + gameObject.name);
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (cardDrag != null && cardDrag.WasDraggedThisPress)
            return;

        if (CardInspector.Instance == null)
            return;

        TroopCardData cardData = null;

        if (cardDisplay != null)
        {
            cardData = cardDisplay.cardDataSO;
        }
        else if (BoardManager.Instance != null)
        {
            BoardManager.Instance.TryGetTroopData(
                gameObject,
                out cardData
            );
        }

        if (cardData != null)
            CardInspector.Instance.Open(cardData);
    }
}
