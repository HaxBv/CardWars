using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PhasesSystem : MonoBehaviour
{
    // ==========================================
    // EVENTS - COMPATIBILIDAD
    // ==========================================

    public static event Action OnFinDeTurno;
    public static event Action OnFaseCombateIniciada;
    public static event Action OnFinDelCombate;

    // ==========================================
    // EVENTS - RONDA Y MOMENTO DEL DÍA
    // ==========================================

    public static event Action OnRoundStarted;
    public static event Action OnRoundEnded;

    public static event Action OnTimeOfDayStarted;
    public static event Action OnTimeOfDayEnded;

    public static event Action OnAlbaStarted;
    public static event Action OnAlbaEnded;

    public static event Action OnOcasoStarted;
    public static event Action OnOcasoEnded;

    // ==========================================
    // ROUND / TIME OF DAY
    // ==========================================

    [Header("Round")]
    [SerializeField, Min(1)] private int currentRound = 1;

    [SerializeField]
    private TimeOfDay currentTimeOfDay =
        TimeOfDay.Alba;

    [Header("Turn")]
    [SerializeField, Range(1, 5)]
    private int pasoTurnoActual = 1;

    [SerializeField] private TurnPhase currentPhase;
    [SerializeField] private PlayerTurn currentPlayerTurn;

    [Header("Priority")]
    [SerializeField] private PlayerTurn firstRoundPriority;
    [SerializeField] private PlayerTurn currentPriority;

    [Header("Board")]
    [SerializeField]
    private List<TropaEnTablero> tropasEnTablero = new();

    // ==========================================
    // PROPERTIES
    // ==========================================

    public int CurrentRound => currentRound;

    public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;

    public TurnPhase CurrentPhase => currentPhase;

    public PlayerTurn CurrentPlayerTurn => currentPlayerTurn;

    public PlayerTurn CurrentPriority => currentPriority;

    public int CurrentStep => pasoTurnoActual;

    [Header("Map Sprites")]
    [SerializeField] private SpriteRenderer mapRenderer;
    [SerializeField] private Sprite albaMap;
    [SerializeField] private Sprite ocasoMap;

    private void ActualizarMapa()
    {
        if (mapRenderer == null)
            return;

        mapRenderer.sprite = currentTimeOfDay == TimeOfDay.Alba
            ? albaMap
            : ocasoMap;
    }

    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        currentRound = 1;

        // El jugador inicial se decide al azar.
        firstRoundPriority = UnityEngine.Random.Range(0, 2) == 0
            ? PlayerTurn.Player1
            : PlayerTurn.Player2;

        IniciarRonda();
    }

    // ==========================================
    // ROUND
    // ==========================================

    private void IniciarRonda()
    {
        Debug.Log($"========== ROUND {currentRound} ==========");

        currentTimeOfDay = TimeOfDay.Alba;

        // La prioridad cambia cada ronda completa.
        bool rondaImpar = currentRound % 2 == 1;

        currentPriority = rondaImpar
            ? firstRoundPriority
            : ObtenerOponente(firstRoundPriority);

        Debug.Log($"Round Priority: {currentPriority}");

        OnRoundStarted?.Invoke();

        IniciarMomentoDelDia(TimeOfDay.Alba);
    }
    private void FinalizarRonda()
    {
        Debug.Log($"ROUND {currentRound} ENDED");

        OnRoundEnded?.Invoke();

        currentRound++;

        IniciarRonda();
    }

    // ==========================================
    // ALBA / OCASO
    // ==========================================
    private void IniciarMomentoDelDia(TimeOfDay nuevoMomento)
    {
        currentTimeOfDay = nuevoMomento;
        pasoTurnoActual = 1;

        // Cambiar el sprite del mapa.
        ActualizarMapa();

        Debug.Log(
            $"START {currentTimeOfDay} | " +
            $"Round: {currentRound} | " +
            $"Priority: {currentPriority}"
        );

        OnTimeOfDayStarted?.Invoke();

        if (currentTimeOfDay == TimeOfDay.Alba)
        {
            OnAlbaStarted?.Invoke();
        }
        else
        {
            OnOcasoStarted?.Invoke();
        }

        ActualizarEstadoTurno();
    }

    private void FinalizarMomentoDelDia()
    {
        Debug.Log($"END {currentTimeOfDay}");

        OnTimeOfDayEnded?.Invoke();

        if (currentTimeOfDay == TimeOfDay.Alba)
        {
            OnAlbaEnded?.Invoke();

            IniciarMomentoDelDia(TimeOfDay.Ocaso);
        }
        else
        {
            OnOcasoEnded?.Invoke();

            // La ronda termina solamente
            // después de Ocaso.
            FinalizarRonda();
        }
    }

    // ==========================================
    // TURN ADVANCE
    // ==========================================

    [Button]
    public void AvanzarTurno()
    {
        OnFinDeTurno?.Invoke();

        if (pasoTurnoActual == 5)
        {
            Debug.Log("Combat phase ended.");

            // Mantiene compatibilidad con RuntimeAbility.
            OnFinDelCombate?.Invoke();

            FinalizarMomentoDelDia();

            return;
        }

        pasoTurnoActual++;

        ActualizarEstadoTurno();
    }

    // ==========================================
    // TURN STATE
    // ==========================================

    private void ActualizarEstadoTurno()
    {
        PlayerTurn segundoJugador =
            ObtenerOponente(currentPriority);

        switch (pasoTurnoActual)
        {
            case 1:
                currentPhase = TurnPhase.TroopTurn;
                currentPlayerTurn = currentPriority;
                break;

            case 2:
                currentPhase = TurnPhase.TroopTurn;
                currentPlayerTurn = segundoJugador;
                break;

            case 3:
                currentPhase =
                    TurnPhase.SpellAndEnvironmentTurn;

                currentPlayerTurn = currentPriority;
                break;

            case 4:
                currentPhase =
                    TurnPhase.SpellAndEnvironmentTurn;

                currentPlayerTurn = segundoJugador;
                break;

            case 5:
                currentPhase = TurnPhase.Combat;

                Debug.Log(
                    $"Combat started - {currentTimeOfDay}"
                );

                OnFaseCombateIniciada?.Invoke();
                break;
        }

        Debug.Log(
            $"Round {currentRound} | " +
            $"{currentTimeOfDay} | " +
            $"Step {pasoTurnoActual} | " +
            $"Phase: {currentPhase} | " +
            $"Player: {currentPlayerTurn}"
        );
    }

    // ==========================================
    // PRIORITY
    // ==========================================

    private PlayerTurn ObtenerOponente(PlayerTurn jugador)
    {
        return jugador == PlayerTurn.Player1
            ? PlayerTurn.Player2
            : PlayerTurn.Player1;
    }

    // ==========================================
    // TROOP REGISTRATION
    // ==========================================

    public void RegistrarTropa(TropaEnTablero tropa)
    {
        if (tropa == null || tropasEnTablero.Contains(tropa))
            return;

        tropasEnTablero.Add(tropa);

        Debug.Log(
            $"Troop registered: {tropa.name}"
        );
    }

    public void QuitarTropa(TropaEnTablero tropa)
    {
        if (tropa == null)
            return;

        tropasEnTablero.Remove(tropa);
    }
}