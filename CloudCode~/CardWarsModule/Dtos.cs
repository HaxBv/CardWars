using System.Collections.Generic;
using CardWars.Rules;

namespace CardWars.CloudCode
{
    // ---------------------------------------------------------------------------------------
    // REGISTRO INTERNO (lo que se guarda). Contiene información OCULTA (los mazos, dentro del
    // Setup): jamás sale del servidor. Se guarda en Cloud Save Custom Data PRIVADO.
    // ---------------------------------------------------------------------------------------
    public sealed class MatchRecord
    {
        public string MatchId { get; set; } = string.Empty;
        public int ProtocolVersion { get; set; }

        // El replay debe usar el mismo catálogo con el que empezó la partida.
        public string CatalogVersion { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;

        // 1 jugador = esperando rival. 2 jugadores = [Player1, Player2], en orden de slot.
        public List<string> Players { get; set; } = new List<string>();

        // Todo el azar de la partida, sorteado una vez por el servidor. SECRETO.
        public GameSetup Setup { get; set; } = new GameSetup();

        // Solo se agregan acciones ya validadas. El estado se obtiene con GameEngine.Replay.
        public List<GameAction> Actions { get; set; } = new List<GameAction>();

        public long CreatedAtUnix { get; set; }

        // Cuándo empezó el paso actual (se reinicia con cada acción aceptada). Plazo por paso.
        public long TurnStartedUnix { get; set; }

        // Fin forzado por encima del replay (NO es una acción): abandono o plazo vencido.
        public string ForcedWinnerPlayerId { get; set; } = string.Empty;
        public string ForcedEndReason { get; set; } = string.Empty;   // "forfeit" | "timeout"
    }

    // ---------------------------------------------------------------------------------------
    // RESPUESTAS AL CLIENTE
    // ---------------------------------------------------------------------------------------
    public sealed class MatchStateDto
    {
        public string MatchId { get; set; } = string.Empty;
        public int ProtocolVersion { get; set; } = GameProtocol.Version;

        // "waiting" | "in_progress" | "finished"
        public string Status { get; set; } = "waiting";

        // "Player1" | "Player2" (null hasta que haya dos jugadores).
        public string YourSide { get; set; }

        // La partida desde el punto de vista de quien llama. Sin mano ni mazo del rival.
        public PlayerView View { get; set; }

        // Cuántas acciones lleva la partida. El cliente la reenvía como "actionsSeen".
        public int ActionCount { get; set; }

        // Eventos de las acciones que el cliente aún no ha visto (los robos del rival tapados).
        public List<GameEvent> NewEvents { get; set; } = new List<GameEvent>();

        public int SecondsLeftInStep { get; set; }

        // Solo si Status == "finished": "won" | "lost", y "portal" | "forfeit" | "timeout".
        public string Result { get; set; }
        public string EndReason { get; set; }
    }

    public sealed class MatchResponse
    {
        public bool Ok { get; set; }
        public string ErrorCode { get; set; }
        public MatchStateDto State { get; set; }

        public static MatchResponse Success(MatchStateDto state)
        {
            return new MatchResponse { Ok = true, State = state };
        }

        public static MatchResponse Fail(string errorCode, MatchStateDto state = null)
        {
            return new MatchResponse { Ok = false, ErrorCode = errorCode, State = state };
        }
    }

    public sealed class PlayMoveRequest
    {
        public int ProtocolVersion { get; set; }

        // "deploy" | "end"
        public string Kind { get; set; } = string.Empty;

        // Solo "deploy".
        public int CardId { get; set; }
        public int Lane { get; set; }
        public int Slot { get; set; }

        // TurnNumber que el cliente cree vigente (evita aplicar dos veces un reintento).
        public int ExpectedTurnNumber { get; set; }

        // Cuántas acciones había visto el cliente (para devolverle solo los eventos nuevos).
        public int ActionsSeen { get; set; }
    }

    // Lo que devuelve GetGameConfig: el cliente usa EXACTAMENTE el mismo catálogo y la misma
    // configuración que el servidor, en lugar de leer los suyos.
    public sealed class GameConfigDto
    {
        public int ProtocolVersion { get; set; } = GameProtocol.Version;
        public string CatalogVersion { get; set; } = string.Empty;
        public int StepTimeoutSeconds { get; set; }
        public List<string> LaneTerrains { get; set; } = new List<string>();
        public int InitialHandSize { get; set; }
        public int MaxHandSize { get; set; }
        public int PortalMaxHealth { get; set; }
        public int PortalShields { get; set; }
        public List<CardJson> Cards { get; set; } = new List<CardJson>();
    }
}
