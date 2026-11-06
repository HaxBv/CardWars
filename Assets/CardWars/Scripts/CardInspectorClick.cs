
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInspectorClick : MonoBehaviour,
    IPointerDownHandler,
    IPointerClickHandler
{
    [SerializeField] private CardDisplay cardDisplay;

    private CardDrag cardDrag;
    private TroopInGame troopInGame;

    private void Awake()
    {
        if (cardDisplay == null)
            cardDisplay = GetComponentInChildren<CardDisplay>(true);

        cardDrag = GetComponentInChildren<CardDrag>(true);

        // Buscar la tropa desplegada.
        troopInGame = GetComponentInChildren<TroopInGame>(true);

        if (troopInGame == null)
            troopInGame = GetComponentInParent<TroopInGame>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (cardDrag != null)
            cardDrag.ResetDragClickFlag();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (cardDrag != null && cardDrag.WasDraggedThisPress)
            return;

        if (CardInspector.Instance == null)
            return;

        CardInstance card = null;

        // ==========================================
        // CARTA EN LA MANO
        // ==========================================

        if (cardDisplay != null)
        {
            card = cardDisplay.CardInstance;
        }

        // ==========================================
        // TROPA DESPLEGADA
        // ==========================================

        if (card == null && troopInGame != null)
        {
            card = troopInGame.CardInstance;
        }

        // ==========================================
        // OPEN INSPECTOR
        // ==========================================

        if (card != null)
        {
            CardInspector.Instance.Open(card);
        }
        else
        {
            Debug.LogWarning(
                $"No se encontró CardInstance en {gameObject.name}"
            );
        }
    }
}
