
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckHandManager : MonoBehaviour
{

    public static DeckHandManager Instance;
    [Header("DECK")]
    public CardDataBase DataBase;
    public GameObject CardPrefab;

    [Header("HAND")]
    public RectTransform CardsInHand;
    public int InitialCards = 4;
    public int MaxHandSize = 8;

    [Header("LAYOUT")]
    public float CardSpacing = 20f;

    private List<TroopCardData> deck = new List<TroopCardData>();
    [Header("HAND ANIMATION")]
    [SerializeField] private float HideDistance = 350f;
    [SerializeField] private float AnimationDuration = 0.4f;

    private Vector2 originalHandPosition;
    private Coroutine handAnimation;

    [Header("CARD SELECTION")]
    [Range(0f, 1f)]
    public float UnselectedAlpha = 0.35f;


    [Header("OBJECT POOL")]
    [SerializeField] private Transform CardPool;

    private Stack<GameObject> pooledCards = new Stack<GameObject>();


    [Header("CARD PHASE RESTRICTIONS")]
    [SerializeField] private PhasesSystem phasesSystem;
    [Range(0f, 1f)]
    [SerializeField] private float BlockedCardAlpha = 0.4f;

    private CardDisplay selectedCard;
    private bool isDraggingCard;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        if (phasesSystem == null)
            phasesSystem = FindFirstObjectByType<PhasesSystem>();

        originalHandPosition = CardsInHand.anchoredPosition;

        CreateDeck();

        for (int i = 0; i < InitialCards; i++)
            DrawCard();

        RefreshCardAvailability();
    }
    private void OnEnable()
    {
        PhasesSystem.OnTimeOfDayEnded += DrawCard;
        PhasesSystem.OnPhaseChanged += OnPhaseChanged;
        PhasesSystem.OnFaseCombateIniciada += HideHand;
        PhasesSystem.OnTimeOfDayStarted += ShowHand;
    }

    private void OnDisable()
    {
        PhasesSystem.OnTimeOfDayEnded -= DrawCard;

        PhasesSystem.OnPhaseChanged -= OnPhaseChanged;
        PhasesSystem.OnFaseCombateIniciada -= HideHand;
        PhasesSystem.OnTimeOfDayStarted -= ShowHand;
    }
    private void OnPhaseChanged(TurnPhase phase)
    {
        RefreshCardAvailability();
    }
    private bool CanDragCard(CardData card)
    {
        if (card == null || PhasesSystem.Instance == null)
            return false;

        switch (PhasesSystem.Instance.CurrentPhase)
        {
            case TurnPhase.TroopTurn:
                return card.Type == CardType.Troop;

            case TurnPhase.SpellAndDomainTurn:
                return card.Type == CardType.Spell ||
                       card.Type == CardType.Domain;

            case TurnPhase.CombatTurn:
                return false;
        }

        return false;
    }
    public void RefreshCardAvailability()
    {
        foreach (Transform child in CardsInHand)
        {
            if (!child.gameObject.activeSelf)
                continue;

            CardDisplay display =
                child.GetComponentInChildren<CardDisplay>(true);

            CardDrag drag =
                child.GetComponentInChildren<CardDrag>(true);

            if (display == null)
                continue;

            bool canDrag = CanDragCard(display.cardDataSO);

            // Bloquear o permitir el arrastre.
            if (drag != null)
                drag.SetDraggable(canDrag);

            // CanvasGroup de la carta completa.
            CanvasGroup group = child.GetComponent<CanvasGroup>();

            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();

            // Oscurecer las cartas no disponibles.
            if (display.DisabledOverlay != null)
                display.DisabledOverlay.SetActive(!canDrag);

            // La transparencia solo se usa durante el arrastre.
            group.alpha = isDraggingCard && display != selectedCard
                ? UnselectedAlpha
                : 1f;

            // Mantener la posibilidad de recibir clics.
            group.blocksRaycasts = true;
        }
    }

    public void SetOtherCardsAlpha(CardDisplay card, bool isSelected)
    {
        isDraggingCard = isSelected;
        selectedCard = isSelected ? card : null;

        RefreshCardAvailability();
    }
    private void CreateDeck()
    {
        deck.Clear();

        if (DataBase == null || DataBase.TroopCardByID == null)
            return;

        foreach (TroopCardData card in DataBase.TroopCardByID.Values)
            deck.Add(card);

        // Barajar el mazo.
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (deck[i], deck[randomIndex]) =
                (deck[randomIndex], deck[i]);
        }
    }

    public void DrawCard()
    {
        if (deck.Count == 0)
            return;

        if (GetHandCount() >= MaxHandSize)
            return;

        TroopCardData card = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);

        AddCardToHand(card);
    }

    // Para cartas devueltas por Bounce.
    // No aplica el límite de la mano.
    public void ReturnCardToPool(GameObject card)
    {
        if (card == null)
            return;

        card.SetActive(false);

        card.transform.SetParent(CardPool, false);

        pooledCards.Push(card);

        RefreshHand();
    }

    private void AddCardToHand(TroopCardData card)
    {
        GameObject newCard;

        if (pooledCards.Count > 0)
        {
            // Reutilizar una carta existente.
            newCard = pooledCards.Pop();

            newCard.transform.SetParent(CardsInHand, false);
        }
        else
        {
            // Crear una carta solamente si el pool está vacío.
            newCard = Instantiate(CardPrefab, CardsInHand);
        }

        CardDisplay display = newCard.GetComponentInChildren<CardDisplay>(true);

        if (display != null)
        {
            display.cardDataSO = card;
            display.AplicarData();
        }

        CanvasGroup group = newCard.GetComponentInChildren<CanvasGroup>(true);

        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        newCard.SetActive(true);

        RefreshHand();
        RefreshCardAvailability();

    }

    public int GetHandCount()
    {
        int count = 0;

        foreach (Transform child in CardsInHand)
        {
            if (child.gameObject.activeSelf)
                count++;
        }

        return count;
    }

    public void RefreshHand()
    {
        List<RectTransform> cards = new List<RectTransform>();

        foreach (Transform child in CardsInHand)
        {
            if (!child.gameObject.activeSelf)
                continue;

            if (child.TryGetComponent(out RectTransform rect))
                cards.Add(rect);
        }

        int count = cards.Count;

        if (count == 0)
            return;

        float cardWidth = cards[0].rect.width;
        float containerWidth = CardsInHand.rect.width;

        float step = cardWidth + CardSpacing;

        // Reducir separación si las cartas no caben.
        if (count > 1)
        {
            float availableStep =
                (containerWidth - cardWidth) / (count - 1);

            step = Mathf.Min(step, Mathf.Max(0f, availableStep));
        }

        // Distribución centrada.
        float startX = -(count - 1) * step / 2f;

        for (int i = 0; i < count; i++)
        {
            cards[i].anchoredPosition = new Vector2(
                startX + i * step,
                0f
            );
        }
    }

    public void HideHand()
    {
        AnimateHand(originalHandPosition + Vector2.down * HideDistance);
    }

    public void ShowHand()
    {
        AnimateHand(originalHandPosition);
    }

    private void AnimateHand(Vector2 targetPosition)
    {
        if (handAnimation != null)
            StopCoroutine(handAnimation);

        handAnimation = StartCoroutine(MoveHand(targetPosition));
    }

    private IEnumerator MoveHand(Vector2 targetPosition)
    {
        Vector2 startPosition = CardsInHand.anchoredPosition;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, AnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            CardsInHand.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                smooth
            );

            yield return null;
        }

        CardsInHand.anchoredPosition = targetPosition;
        handAnimation = null;
    }

}
