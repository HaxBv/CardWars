
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
