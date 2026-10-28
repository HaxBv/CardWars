
using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("BOARD")]
    public LandLane SelectedLane;

    private Dictionary<GameObject, TroopInfo> activeTroops = new();
    public static event Action<GameObject> OnTroopDeployed;
    public static event Action<GameObject> OnTroopDied;

    public static event Action<GameObject, int, int> OnTroopMoved;

    public static event Action OnCombatStarted;
    public static event Action OnCombatEnded;

    [Header("OBJECT POOL")]
    [SerializeField] private Transform TroopPool;

    private Stack<GameObject> pooledTroops = new Stack<GameObject>();
    private class TroopInfo
    {
        public TroopCardData CardData;
        public int LaneID;
        public bool IsAlly;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // =========================================
    // OBJECT POOLING
    // =========================================

    public GameObject GetTroopFromPool(
        GameObject prefab,
        Vector3 position,
        Transform parent)
    {
        if (prefab == null)
            return null;

        GameObject troop;

        if (pooledTroops.Count > 0)
        {
            // Reutilizar una tropa existente.
            troop = pooledTroops.Pop();

            troop.transform.SetParent(parent);
            troop.transform.SetPositionAndRotation(
                position,
                Quaternion.identity
            );

            troop.SetActive(true);
        }
        else
        {
            // Crear una nueva si el pool está vacío.
            troop = Instantiate(
                prefab,
                position,
                Quaternion.identity,
                parent
            );
        }

        return troop;
    }

    public void ReturnTroopToPool(GameObject troop)
    {
        if (troop == null)
            return;

        troop.SetActive(false);
        troop.transform.SetParent(TroopPool, false);

        pooledTroops.Push(troop);
    }

    public bool TryGetTroopData(
    GameObject troopObject,
    out TroopCardData cardData)
    {
        cardData = null;

        if (troopObject == null)
            return false;

        Transform current = troopObject.transform;

        while (current != null)
        {
            if (activeTroops.TryGetValue(
                current.gameObject, out TroopInfo info))
            {
                cardData = info.CardData;
                return true;
            }

            current = current.parent;
        }

        return false;
    }
    // Registrar una tropa cuando entra al tablero.
    public void RegisterTroop(
        GameObject troop,
        TroopCardData cardData,
        int laneID,
        bool isAlly)
    {
        if (troop == null || cardData == null)
            return;

        activeTroops[troop] = new TroopInfo
        {
            CardData = cardData,
            LaneID = laneID,
            IsAlly = isAlly
        };
    }

    // Quitar una tropa cuando muere o abandona el tablero.
    public void UnregisterTroop(GameObject troop)
    {
        if (troop != null)
            activeTroops.Remove(troop);
    }

    // Comprobar si una carta posee Team.
    public bool HasTeam(TroopCardData cardData)
    {
        return cardData != null &&
               cardData.AbilityPassives != null &&
               cardData.AbilityPassives.Contains(AbilityPassive.Team);
    }

    // Comprobar si alguna tropa del bando tiene Team en ese carril.
    public bool HasTeamInLane(int laneID, bool isAlly)
    {
        foreach (var entry in activeTroops)
        {
            if (entry.Key == null)
                continue;

            TroopInfo troop = entry.Value;

            if (troop.LaneID == laneID &&
                troop.IsAlly == isAlly &&
                HasTeam(troop.CardData))
            {
                return true;
            }
        }

        return false;
    }

    // Determinar si dos tropas pueden compartir carril.
    public bool CanShareLane(
        TroopCardData incomingCard,
        int laneID,
        bool isAlly)
    {
        return HasTeam(incomingCard) ||
               HasTeamInLane(laneID, isAlly);
    }

    // Registrar el carril que estamos seleccionando.
    public void SetSelectedLane(LandLane lane)
    {
        SelectedLane = lane;
    }
}
