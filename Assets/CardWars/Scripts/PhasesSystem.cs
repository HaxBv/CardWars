using Sirenix.OdinInspector;
using System;
using UnityEngine;


public class PhasesSystem : MonoBehaviour
{
    // ==========================================
    // ¡AQUÍ SE CREA EL FIN DEL COMBATE Y LOS EVENTOS GENERALES!
    // ==========================================
    public static event Action OnFinDeTurno;
    public static event Action OnFaseCombateIniciada;
    public static event Action OnFinDelCombate; // El que espera RuntimeAbility

    [SerializeField] private int currentRound = 1;

    // Controla cuál de los 5 pasos de la ronda se está jugando actualmente
    [SerializeField] private int pasoTurnoActual = 1;

    [SerializeField] private TurnPhase currentPhase;
    [SerializeField] private PlayerTurn currentPlayerTurn;

    void Start()
    {
        IniciarRonda();
    }

    /// <block>
    /// Configura el estado inicial al arrancar cada nueva ronda
    /// </block>
    private void IniciarRonda()
    {
        Debug.Log($"--- INICIANDO RONDA {currentRound} ---");
        pasoTurnoActual = 1;
        ActualizarEstadoTurno();
    }

    /// <block>
    /// Este método debes enlazarlo al botón de "Terminar Turno" en tu interfaz de usuario.
    /// Es el encargado de avanzar a través de tus 5 pasos de juego.
    /// </block>
    [Button]
    public void AvanzarTurno()
    {
        // Avisamos al juego que el turno actual (sea cual sea de los 5) acaba de terminar
        OnFinDeTurno?.Invoke();

        // VALIDACIÓN CLAVE: Si estamos en el paso 5 (Fase de Combate) y se presiona avanzar...
        if (pasoTurnoActual == 5)
        {
            Debug.Log("La Fase de Combate ha concluido de forma oficial.");

            // ¡CREACIÓN EN JUEGO! Aquí es donde disparamos el evento para Konquest
            OnFinDelCombate?.Invoke();

            // Una vez resueltos todos los efectos post-combate, pasamos a la siguiente ronda
            currentRound++;
            IniciarRonda();
            return;
        }

        // Si no estábamos en el paso 5, avanzamos normalmente al siguiente turno de la lista
        pasoTurnoActual++;
        ActualizarEstadoTurno();
    }

    /// <block>
    /// Implementación estricta de tu sistema de juego basado en 5 pasos secuenciales
    /// </block>
    private void ActualizarEstadoTurno()
    {
        switch (pasoTurnoActual)
        {
            case 1: // Turno 1: Jugador 1 (Solo tropas)
                currentPhase = TurnPhase.TroopTurn;
                currentPlayerTurn = PlayerTurn.Player1;
                Debug.Log("Paso 1: Turno del Jugador 1 - Solo Tropas.");
                break;

            case 2: // Turno 2: Jugador 2 (Solo tropas)
                currentPhase = TurnPhase.TroopTurn;
                currentPlayerTurn = PlayerTurn.Player2;
                Debug.Log("Paso 2: Turno del Jugador 2 - Solo Tropas.");
                break;

            case 3: // Turno 3: Jugador 1 (Solo hechizos y dominios)
                currentPhase = TurnPhase.SpellAndEnvironmentTurn;
                currentPlayerTurn = PlayerTurn.Player1;
                Debug.Log("Paso 3: Turno del Jugador 1 - Hechizos y Dominios.");
                break;

            case 4: // Turno 4: Jugador 2 (Solo hechizos y dominios)
                currentPhase = TurnPhase.SpellAndEnvironmentTurn;
                currentPlayerTurn = PlayerTurn.Player2;
                Debug.Log("Paso 4: Turno del Jugador 2 - Hechizos y Dominios.");
                break;

            case 5: // Turno 5: Combate automático general
                currentPhase = TurnPhase.Combat;
                Debug.Log("Paso 5: ¡Se inicia la Fase de Combate automático!");

                // Dispara el evento que inicia el intercambio de ataques físicos en tu tablero
                OnFaseCombateIniciada?.Invoke();
                break;
        }
    }
}