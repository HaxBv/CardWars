
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

    [SerializeField]private List<TroopCardData> deck = new List<TroopCardData>();
    [Header("HAND ANIMATION")]
    [SerializeField] private float HideDistance = 350f;
    [SerializeField] private float AnimationDuration = 0.4f;

    private Vector2 originalHandPosition;
    private Coroutine handAnimation;



    [Header("OBJECT POOL")]
    [SerializeField] private Transform CardPool;

    private Stack<GameObject> pooledCards = new Stack<GameObject>();


    [Header("CARD PHASE RESTRICTIONS")]
    [SerializeField] private PhasesSystem phasesSystem;


    [Header("CARD DRAG EFFECT")]
    [SerializeField, Range(0f, 300f)]
    private float OtherCardsLowerDistance = 100f;

    [SerializeField, Min(0.01f)]
    private float OtherCardsAnimationDuration = 0.25f;

    private Dictionary<RectTransform, Coroutine> cardAnimations = new();

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

            // Permitir o bloquear el arrastre.
            if (drag != null)
                drag.SetDraggable(canDrag);

            // Oscurecer la carta modificando sus colores.
            CardVisualDimmer dimmer =
                child.GetComponent<CardVisualDimmer>();

            if (dimmer != null)
                dimmer.SetDarkened(!canDrag);

            // CanvasGroup de toda la carta.
            CanvasGroup group = child.GetComponent<CanvasGroup>();

            if (group == null)
                group = child.gameObject.AddComponent<CanvasGroup>();

            // Transparencia únicamente durante el arrastre.
            group.alpha = 1f;
            // Mantener los clics habilitados.
            group.blocksRaycasts = true;
        }
    }

    public void SetCardDragState(CardDisplay selectedCard, bool isDragging)
    {
        foreach (Transform child in CardsInHand)
        {
            if (!child.gameObject.activeSelf)
                continue;

            RectTransform rect = child as RectTransform;
            CardDisplay display = child.GetComponentInChildren<CardDisplay>(true);

            if (rect == null || display == null)
                continue;

            // Durante el arrastre, la carta seleccionada permanece arriba.
            if (isDragging && display == selectedCard)
            {
                StopCardAnimation(rect);

                rect.anchoredPosition = new Vector2(
                    rect.anchoredPosition.x,
                    0f
                );

                continue;
            }

            // Las demás bajan; al soltar, todas regresan.
            float targetY = isDragging ? -OtherCardsLowerDistance : 0f;

            AnimateCard(rect, targetY);
        }
    }
    private void StopCardAnimation(RectTransform rect)
    {
        if (rect == null)
            return;

        if (cardAnimations.TryGetValue(rect, out Coroutine animation))
        {
            if (animation != null)
                StopCoroutine(animation);

            cardAnimations.Remove(rect);
        }
    }
    private void AnimateCard(RectTransform card, float targetY)
    {
        // Detener cualquier animación anterior de esta carta.
        StopCardAnimation(card);

        // Iniciar la nueva animación.
        cardAnimations[card] =
            StartCoroutine(MoveCard(card, targetY));
    }

    private IEnumerator MoveCard(RectTransform card, float targetY)
    {
        float startY = card.anchoredPosition.y;
        float elapsed = 0f;

        float duration = Mathf.Max(
            0.01f,
            OtherCardsAnimationDuration
        );

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            // Animar solamente Y y conservar la X actual.
            card.anchoredPosition = new Vector2(
                card.anchoredPosition.x,
                Mathf.Lerp(startY, targetY, smooth)
            );

            yield return null;
        }

        card.anchoredPosition = new Vector2(
            card.anchoredPosition.x,
            targetY
        );

        cardAnimations.Remove(card);
    }



    private void CreateDeck()
    {
        deck.Clear();

        if (DataBase == null || DataBase.TroopCardByID == null)
            return;

        foreach (TroopCardData card in DataBase.TroopCardByID.Values)
        {
            if (card == null)
                continue;

            // Obtener las copias máximas según la rareza.
            int copies = card.MaxDeckCopies;

            // Excluir Token y rarezas no permitidas.
            if (copies <= 0)
                continue;

            // Agregar las copias de esta carta al mazo.
            for (int i = 0; i < copies; i++)
            {
                deck.Add(card);
            }
        }

        // Barajar el mazo.
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (deck[i], deck[randomIndex]) =
                (deck[randomIndex], deck[i]);
        }

        Debug.Log($"Mazo generado con {deck.Count} cartas.");
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

        // Buscar el objeto principal de la carta en CardsInHand.
        Transform cardRoot = card.transform;

        while (cardRoot.parent != null &&
               cardRoot.parent != CardsInHand)
        {
            cardRoot = cardRoot.parent;
        }

        if (cardRoot.parent != CardsInHand)
        {
            Debug.LogWarning("La carta no pertenece a CardsInHand.");
            return;
        }

        GameObject rootObject = cardRoot.gameObject;
        RectTransform rect = rootObject.GetComponent<RectTransform>();

        if (rect != null)
        {
            StopCardAnimation(rect);

            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                0f
            );
        }

        rootObject.SetActive(false);
        rootObject.transform.SetParent(CardPool, false);

        pooledCards.Push(rootObject);

        RefreshHand();
    }
    private void AddCardToHand(TroopCardData card)
    {
        GameObject newCard;

        // Obtener una carta del pool o crear una nueva.
        if (pooledCards.Count > 0)
        {
            newCard = pooledCards.Pop();
            newCard.transform.SetParent(CardsInHand, false);
        }
        else
        {
            newCard = Instantiate(CardPrefab, CardsInHand);
        }

        // Cancelar animaciones anteriores y restaurar posición.
        RectTransform newRect = newCard.GetComponent<RectTransform>();

        if (newRect != null)
        {
            StopCardAnimation(newRect);

            newRect.anchoredPosition = new Vector2(
                newRect.anchoredPosition.x,
                0f
            );
        }

        CardDisplay display =
            newCard.GetComponentInChildren<CardDisplay>(true);

        CardVisualDimmer dimmer =
            newCard.GetComponent<CardVisualDimmer>();

        // Restaurar los colores originales.
        if (dimmer != null)
            dimmer.SetDarkened(false);

        if (display != null)
        {
            display.cardDataSO = card;
            display.AplicarData();
        }

        // Guardar los colores actuales de la nueva carta.
        if (dimmer != null)
            dimmer.CacheOriginalColors();

        CanvasGroup group = newCard.GetComponent<CanvasGroup>();

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
            cards[i].anchoredPosition.y);

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
