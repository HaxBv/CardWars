
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class LandLane : MonoBehaviour
{
    [Header("TERRAIN")]
    public int LaneID;
  
    public LaneTerrain Terrain = LaneTerrain.Land;
    private CardInstance previewCard;

    [Header("DEPLOYMENT")]
    public Transform AllyFirstSlot;
    public Transform AllySecondSlot;
    public Transform EnemyFirstSlot;
    public Transform EnemySecondSlot;

    public Transform TroopsContainer;
    public GameObject TroopPrefab;

    [Header("HIGHLIGHT")]
    public SpriteRenderer LaneRenderer;

    public Color AvailableColor = new Color(1f, 1f, 1f, 0.25f);
    public Color SelectedColor = new Color(0.4f, 1f, 0.5f, 0.55f);
    public Color SlotAvailableColor = new Color(0.4f, 1f, 0.5f, 0.35f);
    public Color SlotSelectedColor = new Color(0.2f, 1f, 0.3f, 0.8f);

    private BoxCollider2D laneCollider;

    // Tropas que ocupan las posiciones del carril.
    private GameObject allyFirstTroop;
    private GameObject allySecondTroop;
    private GameObject enemyFirstTroop;
    private GameObject enemySecondTroop;


    [Header("PORTAL ATTACK POINTS")]
    [SerializeField] private Transform allyPortalAttackPoint;
    [SerializeField] private Transform enemyPortalAttackPoint;

    public Transform GetPortalAttackPoint(bool attackerIsAlly)
    {
        return attackerIsAlly
            ? enemyPortalAttackPoint
            : allyPortalAttackPoint;
    }


    private void Awake()
    {
        laneCollider = GetComponent<BoxCollider2D>();

        if (LaneRenderer == null)
            LaneRenderer = GetComponent<SpriteRenderer>();

        HideHighlight();
    }
    public bool CanDeployOnTerrain(CardInstance card)
    {
        if (card is not TroopCardInstance troop)
            return false;

        if (Terrain == LaneTerrain.Land)
            return true;

        if (troop.HasPassive(AbilityPassive.AllTerrain))
            return true;

        switch (Terrain)
        {
            case LaneTerrain.Aquatic:
                return troop.HasPassive(AbilityPassive.Aquatic);

            case LaneTerrain.Highground:
                return troop.HasPassive(AbilityPassive.Highground);

            default:
                return false;
        }
    }
    public bool CanDeployOnTerrain(TroopInGame troop)
    {
        if (troop == null)
            return false;

        if (Terrain == LaneTerrain.Land)
            return true;

        if (troop.HasPassive(AbilityPassive.AllTerrain))
            return true;

        switch (Terrain)
        {
            case LaneTerrain.Aquatic:
                return troop.HasPassive(AbilityPassive.Aquatic);

            case LaneTerrain.Highground:
                return troop.HasPassive(AbilityPassive.Highground);

            default:
                return false;
        }
    }
    public void SetPreviewCard(CardInstance card)
    {
        previewCard = card;
    }
    public bool ContainsPosition(Vector2 worldPosition)
    {
        return laneCollider.OverlapPoint(worldPosition);
    }

    public bool IsAllySide(Vector2 worldPosition)
    {
        return worldPosition.x < laneCollider.bounds.center.x;
    }
    private void ShowSideSlots(bool isAlly)
    {
        GameObject firstTroop = isAlly ? allyFirstTroop : enemyFirstTroop;
        GameObject secondTroop = isAlly ? allySecondTroop : enemySecondTroop;

        Transform firstSlot = isAlly ? AllyFirstSlot : EnemyFirstSlot;
        Transform secondSlot = isAlly ? AllySecondSlot : EnemySecondSlot;

        // Carril vacío: mostrar FirstSlot.
        if (firstTroop == null)
        {
            SetSlotColor(firstSlot, SlotAvailableColor);
        }
        // Una tropa: comprobar si alguna de las dos tiene Team.
        else if (secondTroop == null &&
                 BoardManager.Instance.CanShareLane(previewCard, LaneID, isAlly))
        {
            SetSlotColor(firstSlot, SlotAvailableColor);
            SetSlotColor(secondSlot, SlotAvailableColor);
        }
    }
    private void RefreshTroopPositions()
    {
        // Las tropas conservan sus posiciones.
        // No se desplazan automáticamente cuando otra muere.
    }

    private bool IsSlotSelected(Transform slot, Vector2 worldPosition)
    {
        if (slot == null)
            return false;

        BoxCollider2D collider = slot.GetComponent<BoxCollider2D>();

        return collider != null && collider.OverlapPoint(worldPosition);
    }

    private void SetSlotColor(Transform slot, Color color)
    {
        if (slot == null)
            return;

        SpriteRenderer renderer = slot.GetComponent<SpriteRenderer>();

        if (renderer != null)
            renderer.color = color;
    }

    private void HideSlotHighlights()
    {
        SetSlotColor(AllyFirstSlot, Color.clear);
        SetSlotColor(AllySecondSlot, Color.clear);
        SetSlotColor(EnemyFirstSlot, Color.clear);
        SetSlotColor(EnemySecondSlot, Color.clear);
    }
    public void UpdateSlotHighlight(Vector2 worldPosition)
    {
        // Mostrar todas las casillas disponibles.
        ShowAvailable();

        if (!CanDeployOnTerrain(previewCard))
            return;


        bool isAlly = IsAllySide(worldPosition);

        GameObject firstTroop = isAlly ? allyFirstTroop : enemyFirstTroop;
        GameObject secondTroop = isAlly ? allySecondTroop : enemySecondTroop;

        Transform firstSlot = isAlly ? AllyFirstSlot : EnemyFirstSlot;
        Transform secondSlot = isAlly ? AllySecondSlot : EnemySecondSlot;

        // No permitir más de dos tropas.
        if (firstTroop != null && secondTroop != null)
            return;

        // Si hay una tropa, comprobar Team en ambas cartas.
        if (firstTroop != null &&
            !BoardManager.Instance.CanShareLane(previewCard, LaneID, isAlly))
        {
            return;
        }

        // Resaltar la primera casilla.
        if (IsSlotSelected(firstSlot, worldPosition))
        {
            SetSlotColor(firstSlot, SlotSelectedColor);
        }
        // Resaltar la segunda cuando ya existe una tropa.
        else if (firstTroop != null &&
                 IsSlotSelected(secondSlot, worldPosition))
        {
            SetSlotColor(secondSlot, SlotSelectedColor);
        }
    }

    public bool DeployTroop(CardInstance card, Vector2 worldPosition)
    {
        // Validar que la carta sea una tropa.
        if (card is not TroopCardInstance troopCard || TroopPrefab == null || TroopsContainer == null || BoardManager.Instance == null || !ContainsPosition(worldPosition))
            return false;

        TroopCardData cardData = troopCard.CardData as TroopCardData;

        if (cardData == null || !CanDeployOnTerrain(card))
            return false;

        RefreshTroopPositions();

        bool isAlly = IsAllySide(worldPosition);

        Transform firstSlot = isAlly ? AllyFirstSlot : EnemyFirstSlot;
        Transform secondSlot = isAlly ? AllySecondSlot : EnemySecondSlot;

        GameObject firstTroop = isAlly ? allyFirstTroop : enemyFirstTroop;
        GameObject secondTroop = isAlly ? allySecondTroop : enemySecondTroop;

        if (firstSlot == null || secondSlot == null)
            return false;

        // No permitir más de dos tropas por bando.
        if (firstTroop != null && secondTroop != null)
            return false;

        // Si ya existe una tropa, alguna debe tener Team.
        if (firstTroop != null &&
            !BoardManager.Instance.CanShareLane(card, LaneID, isAlly))
        {
            return false;
        }

        Transform spawnPoint = firstSlot;
        bool moveExistingTroop = false;

        if (firstTroop == null)
        {
            // Carril vacío: desplegar en FirstSlot.
            if (!IsSlotSelected(firstSlot, worldPosition))
                return false;
        }
        else
        {
            if (IsSlotSelected(firstSlot, worldPosition))
            {
                // La nueva tropa irá delante.
                // La existente pasará detrás al confirmar el despliegue.
                moveExistingTroop = true;
            }
            else if (IsSlotSelected(secondSlot, worldPosition))
            {
                // La nueva tropa irá detrás.
                spawnPoint = secondSlot;
            }
            else
            {
                return false;
            }
        }

        // Obtener el GameObject desde el pool.
        GameObject newTroop = BoardManager.Instance.GetTroopFromPool(
            TroopPrefab,
            spawnPoint.position,
            TroopsContainer
        );

        if (newTroop == null)
            return false;

        TroopInGame troop =
            newTroop.GetComponentInChildren<TroopInGame>();

        if (troop == null)
        {
            Debug.LogError("No se encontró TroopInGame en el prefab.");

            BoardManager.Instance.ReturnTroopToPool(newTroop);
            return false;
        }

        if (newTroop.GetComponentInChildren<TroopDisplay>() == null)
        {
            Debug.LogError("No se encontró TroopDisplay.");

            BoardManager.Instance.ReturnTroopToPool(newTroop);
            return false;
        }

        // Inicializar usando las estadísticas y pasivas
        // actuales de TroopCardInstance.
        troop.Initialize(troopCard, isAlly);

        // Suscribir el evento de muerte.
        troop.OnDeath -= HandleTroopDeath;
        troop.OnDeath += HandleTroopDeath;

        // Registrar la tropa en BoardManager.
        BoardManager.Instance.RegisterTroop(
            newTroop,
            cardData,
            LaneID,
            isAlly
        );

        // Mover la tropa anterior solo después
        // de haber creado e inicializado la nueva.
        if (moveExistingTroop)
        {
            firstTroop.transform.position = secondSlot.position;

            if (isAlly)
            {
                allySecondTroop = firstTroop;
                allyFirstTroop = null;
            }
            else
            {
                enemySecondTroop = firstTroop;
                enemyFirstTroop = null;
            }
        }

        // Registrar la nueva tropa en el slot correspondiente.
        if (isAlly)
        {
            if (spawnPoint == AllyFirstSlot)
                allyFirstTroop = newTroop;
            else
                allySecondTroop = newTroop;
        }
        else
        {
            if (spawnPoint == EnemyFirstSlot)
                enemyFirstTroop = newTroop;
            else
                enemySecondTroop = newTroop;
        }

        Debug.Log(
            $"Tropa desplegada | Carril: {LaneID} | " +
            $"Bando: {(isAlly ? "Aliado" : "Enemigo")} | " +
            $"Posición: {(spawnPoint == firstSlot ? "Frente" : "Atrás")}"
        );

        troop.NotifyDeploy();

        return true;
    }



    private void HandleTroopDeath(TroopInGame troop)
    {
        if (troop == null)
            return;

        GameObject deadTroop = troop.transform.parent.gameObject;

        if (allyFirstTroop == deadTroop)
            allyFirstTroop = null;
        else if (allySecondTroop == deadTroop)
            allySecondTroop = null;
        else if (enemyFirstTroop == deadTroop)
            enemyFirstTroop = null;
        else if (enemySecondTroop == deadTroop)
            enemySecondTroop = null;

        troop.OnDeath -= HandleTroopDeath;

        BoardManager.Instance?.UnregisterTroop(deadTroop);

        RefreshTroopPositions();

        BoardManager.Instance.ReturnTroopToPool(deadTroop);
    }

    public void ShowAvailable()
    {
        RefreshTroopPositions();

        if (LaneRenderer != null)
            LaneRenderer.color = Color.clear;

        HideSlotHighlights();

        // No iluminar nada si la carta no es válida
        // para este terreno.
        if (!CanDeployOnTerrain(previewCard))
            return;

        // Mantener la lógica existente de Team.
        ShowSideSlots(true);
        ShowSideSlots(false);
    }

    public void Highlight()
    {
        // Las tropas no utilizan el indicador del carril completo.
        if (LaneRenderer != null)
            LaneRenderer.color = Color.clear;
    }

    public void RemoveHighlight()
    {
        ShowAvailable();
    }

    public void HideHighlight()
    {
        HideSlotHighlights();

        if (LaneRenderer != null)
            LaneRenderer.color = Color.clear;
        previewCard = null;
    }

    public GameObject GetTroop(bool isAlly, int slot)
    {
        if (isAlly)
            return slot == 1 ? allyFirstTroop : allySecondTroop;

        return slot == 1 ? enemyFirstTroop : enemySecondTroop;
    }

    public Transform GetOccupiedSlot(GameObject troopRoot)
    {
        if (allyFirstTroop == troopRoot) return AllyFirstSlot;
        if (allySecondTroop == troopRoot) return AllySecondSlot;
        if (enemyFirstTroop == troopRoot) return EnemyFirstSlot;
        if (enemySecondTroop == troopRoot) return EnemySecondSlot;

        return null;
    }
    public void RemoveDefeatedTroop(TroopInGame troop)
    {
        if (troop == null || troop.CurrentHealth > 0)
            return;

        HandleTroopDeath(troop);
    }
}
