using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CardWars.Rules;

namespace CardWars.CloudCode
{
    // La AUTORIDAD de la partida. Cada método sigue el mismo patrón:
    //   cargar registro -> comprobar quién llama -> reproducir (Replay) con el motor ->
    //   validar y aplicar con el MISMO motor que usa el cliente -> guardar -> avisar al rival ->
    //   devolver la VISTA de quien llama.
    // El identificador de quien llama (callerId) sale de context.PlayerId en la función; nunca
    // de un parámetro que mande el cliente.
    public sealed class MatchService
    {
        private readonly IMatchStore store;
        private readonly IMatchNotifier notifier;
        private readonly ICatalogProvider catalogs;
        private readonly IClock clock;
        private readonly Func<int> seedSource;

        public MatchService(
            IMatchStore store,
            IMatchNotifier notifier,
            ICatalogProvider catalogs,
            IClock clock = null,
            Func<int> seedSource = null)
        {
            this.store = store;
            this.notifier = notifier;
            this.catalogs = catalogs;
            this.clock = clock ?? new SystemClock();

            // El azar de la partida lo decide el servidor (nunca el cliente).
            this.seedSource = seedSource ?? (() => RandomNumberGenerator.GetInt32(int.MaxValue));
        }

        // =====================================================
        // GetGameConfig
        // =====================================================

        public GameConfigDto GetGameConfig()
        {
            CatalogVersion catalog = catalogs.Get(catalogs.CurrentVersion);
            CatalogFile file = catalog.File;

            return new GameConfigDto
            {
                CatalogVersion = catalog.Version,
                StepTimeoutSeconds = file.StepTimeoutSeconds,
                LaneTerrains = new List<string>(file.LaneTerrains),
                InitialHandSize = file.InitialHandSize,
                MaxHandSize = file.MaxHandSize,
                PortalMaxHealth = file.PortalMaxHealth,
                PortalShields = file.PortalShields,
                Cards = new List<CardJson>(file.Cards)
            };
        }

        // =====================================================
        // CreateMatch / JoinMatch
        // =====================================================

        // Idempotente: el mismo (callerId, requestId) siempre devuelve la misma partida.
        public async Task<MatchResponse> CreateMatchAsync(
            string callerId, string requestId, int protocolVersion)
        {
            if (protocolVersion != GameProtocol.Version)
                return MatchResponse.Fail(ErrorCodes.ProtocolMismatch);

            if (string.IsNullOrEmpty(callerId) || string.IsNullOrEmpty(requestId) || requestId.Length > 64)
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            string matchId = MatchIds.ForCreate(callerId, requestId);
            long now = clock.UtcNow.ToUnixTimeSeconds();

            CatalogVersion catalog = catalogs.Get(catalogs.CurrentVersion);

            var record = new MatchRecord
            {
                MatchId = matchId,
                ProtocolVersion = GameProtocol.Version,
                CatalogVersion = catalog.Version,
                CreatedBy = callerId,
                Players = new List<string> { callerId },
                Setup = catalog.CreateEngine().CreateSetup(seedSource()),
                CreatedAtUnix = now,
                TurnStartedUnix = now
            };

            if (!await store.TryCreateAsync(record))
            {
                // Ya existía: si la creó el mismo jugador es un reintento; si no, choque de códigos.
                StoredMatch existing = await store.LoadAsync(matchId);

                if (existing == null || existing.Record.CreatedBy != callerId)
                    return MatchResponse.Fail(ErrorCodes.CodeTaken);

                record = existing.Record;
            }

            return ReadState(callerId, record, 0);
        }

        public async Task<MatchResponse> JoinMatchAsync(
            string callerId, string matchId, int protocolVersion)
        {
            if (protocolVersion != GameProtocol.Version)
                return MatchResponse.Fail(ErrorCodes.ProtocolMismatch);

            if (!MatchIds.LooksValid(matchId))
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            StoredMatch stored = await store.LoadAsync(matchId);

            if (stored == null)
                return MatchResponse.Fail(ErrorCodes.NotFound);

            MatchRecord record = stored.Record;

            // Reintento de quien ya está dentro: no hace nada y devuelve el estado.
            if (record.Players.Contains(callerId))
                return ReadState(callerId, record, 0);

            if (record.Players.Count >= 2)
                return MatchResponse.Fail(ErrorCodes.MatchFull);

            if (IsFinished(record, null))
                return MatchResponse.Fail(ErrorCodes.MatchOver);

            // Orden determinista de los lados: los ids se ordenan y el primero es Player1.
            // Los mazos y el primer jugador ya están sorteados en el Setup (por lado, no por id).
            string creator = record.Players[0];
            record.Players = new List<string> { creator, callerId }
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();
            record.TurnStartedUnix = clock.UtcNow.ToUnixTimeSeconds();

            if (!await store.TrySaveAsync(stored))
                return MatchResponse.Fail(ErrorCodes.Conflict);

            await NotifySafeAsync(matchId, creator);
            return ReadState(callerId, record, 0);
        }

        // =====================================================
        // PlayMove
        // =====================================================

        public async Task<MatchResponse> PlayMoveAsync(
            string callerId, string matchId, PlayMoveRequest request)
        {
            if (request == null || request.ProtocolVersion != GameProtocol.Version)
                return MatchResponse.Fail(ErrorCodes.ProtocolMismatch);

            // 1. Cargar la partida.
            if (!MatchIds.LooksValid(matchId))
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            StoredMatch stored = await store.LoadAsync(matchId);

            if (stored == null)
                return MatchResponse.Fail(ErrorCodes.NotFound);

            MatchRecord record = stored.Record;

            // 2. Quien llama debe ser jugador de ESTA partida (por context.PlayerId).
            int sideIndex = record.Players.IndexOf(callerId);

            if (sideIndex < 0)
                return MatchResponse.Fail(ErrorCodes.NotAPlayer);

            if (record.Players.Count < 2)
                return MatchResponse.Fail(ErrorCodes.NotStarted);

            // 3. Reconstruir la partida con el motor (setup + acciones).
            if (!TryReplay(record, out CatalogVersion catalog, out GameEngine engine,
                    out ReplayResult replay, out string replayError))
            {
                return MatchResponse.Fail(replayError);
            }

            // 4. Reloj del servidor: un plazo vencido ya decidió la partida.
            long now = clock.UtcNow.ToUnixTimeSeconds();
            bool changedByClock = ApplyClockRules(record, replay.State, catalog, now);

            if (IsFinished(record, replay.State))
            {
                if (changedByClock)
                    await store.TrySaveAsync(stored);

                return MatchResponse.Fail(
                    ErrorCodes.MatchOver, BuildState(callerId, record, replay, catalog, now, request.ActionsSeen));
            }

            // 5. Traducir el mensaje a una acción del motor y VALIDARLA con las reglas.
            PlayerId side = (PlayerId)sideIndex;
            GameAction action = ToAction(side, request);

            if (action == null)
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            ActionOutcome outcome = engine.Apply(replay.State, action, request.ExpectedTurnNumber);

            if (!outcome.Ok)
            {
                // Rechazada: no se guarda nada. Se devuelve la vista para que el cliente se ponga al día.
                return MatchResponse.Fail(
                    ErrorCodes.FromResult(outcome.Result),
                    BuildState(callerId, record, replay, catalog, now, request.ActionsSeen));
            }

            // 6. Agregar la acción (solo de agregar), reiniciar el plazo y guardar.
            replay.EventsPerAction.Add(outcome.Events);
            record.Actions.Add(action);
            record.TurnStartedUnix = now;

            if (!await store.TrySaveAsync(stored))
                return MatchResponse.Fail(ErrorCodes.Conflict);

            // 7. Avisar al rival (liviano, sin estado). Si falla, la jugada ya quedó guardada.
            await NotifySafeAsync(matchId, record.Players[1 - sideIndex]);

            return MatchResponse.Success(
                BuildState(callerId, record, replay, catalog, now, request.ActionsSeen));
        }

        // =====================================================
        // GetMatchState
        // =====================================================

        // Sirve para refrescar tras un push, para reconectar y como sondeo de respaldo.
        // También resuelve el plazo vencido (reactivo, con el reloj del servidor).
        public async Task<MatchResponse> GetMatchStateAsync(
            string callerId, string matchId, int actionsSeen, int protocolVersion)
        {
            if (protocolVersion != GameProtocol.Version)
                return MatchResponse.Fail(ErrorCodes.ProtocolMismatch);

            if (!MatchIds.LooksValid(matchId))
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            StoredMatch stored = await store.LoadAsync(matchId);

            if (stored == null)
                return MatchResponse.Fail(ErrorCodes.NotFound);

            if (!stored.Record.Players.Contains(callerId))
                return MatchResponse.Fail(ErrorCodes.NotAPlayer);

            if (stored.Record.Players.Count < 2)
                return ReadState(callerId, stored.Record, actionsSeen);

            if (!TryReplay(stored.Record, out CatalogVersion catalog, out _,
                    out ReplayResult replay, out string replayError))
            {
                return MatchResponse.Fail(replayError);
            }

            long now = clock.UtcNow.ToUnixTimeSeconds();

            if (ApplyClockRules(stored.Record, replay.State, catalog, now))
            {
                // Si otra llamada guardó antes, el resultado es el mismo y se recalcula la próxima vez.
                await store.TrySaveAsync(stored);

                // Avisar al OTRO jugador de que la partida terminó por plazo vencido.
                await NotifySafeAsync(
                    stored.Record.MatchId,
                    stored.Record.Players.First(p => p != callerId));
            }

            return MatchResponse.Success(
                BuildState(callerId, stored.Record, replay, catalog, now, actionsSeen));
        }

        // =====================================================
        // LeaveMatch
        // =====================================================

        // Abandono voluntario: el rival gana por "forfeit". Una partida que aún espera rival se cancela.
        public async Task<MatchResponse> LeaveMatchAsync(
            string callerId, string matchId, int protocolVersion)
        {
            if (protocolVersion != GameProtocol.Version)
                return MatchResponse.Fail(ErrorCodes.ProtocolMismatch);

            if (!MatchIds.LooksValid(matchId))
                return MatchResponse.Fail(ErrorCodes.InvalidRequest);

            StoredMatch stored = await store.LoadAsync(matchId);

            if (stored == null)
                return MatchResponse.Fail(ErrorCodes.NotFound);

            MatchRecord record = stored.Record;
            int sideIndex = record.Players.IndexOf(callerId);

            if (sideIndex < 0)
                return MatchResponse.Fail(ErrorCodes.NotAPlayer);

            if (record.Players.Count == 2 && TryReplay(record, out _, out _,
                    out ReplayResult replay, out _) && replay.State.IsOver)
            {
                return MatchResponse.Fail(ErrorCodes.MatchOver);
            }

            if (!string.IsNullOrEmpty(record.ForcedEndReason))
                return MatchResponse.Fail(ErrorCodes.MatchOver);

            record.ForcedEndReason = record.Players.Count == 2 ? "forfeit" : "cancelled";
            record.ForcedWinnerPlayerId = record.Players.Count == 2 ? record.Players[1 - sideIndex] : string.Empty;

            if (!await store.TrySaveAsync(stored))
                return MatchResponse.Fail(ErrorCodes.Conflict);

            if (record.Players.Count == 2)
                await NotifySafeAsync(matchId, record.Players[1 - sideIndex]);

            return ReadState(callerId, record, 0);
        }

        // =====================================================
        // AYUDANTES
        // =====================================================

        private MatchResponse ReadState(
            string callerId, MatchRecord record, int actionsSeen)
        {
            long now = clock.UtcNow.ToUnixTimeSeconds();

            if (record.Players.Count < 2)
            {
                return MatchResponse.Success(new MatchStateDto
                {
                    MatchId = record.MatchId,
                    Status = string.IsNullOrEmpty(record.ForcedEndReason) ? "waiting" : "finished",
                    EndReason = string.IsNullOrEmpty(record.ForcedEndReason) ? null : record.ForcedEndReason
                });
            }

            if (!TryReplay(record, out CatalogVersion catalog, out _,
                    out ReplayResult replay, out string replayError))
            {
                return MatchResponse.Fail(replayError);
            }

            return MatchResponse.Success(BuildState(callerId, record, replay, catalog, now, actionsSeen));
        }

        // Reconstruye la partida: Replay(setup, acciones) con el catálogo de la versión guardada.
        private bool TryReplay(
            MatchRecord record,
            out CatalogVersion catalog,
            out GameEngine engine,
            out ReplayResult replay,
            out string errorCode)
        {
            catalog = catalogs.Get(record.CatalogVersion);
            engine = null;
            replay = null;
            errorCode = null;

            if (catalog == null)
            {
                errorCode = ErrorCodes.CatalogUnavailable;
                return false;
            }

            engine = catalog.CreateEngine();

            try
            {
                replay = engine.Replay(record.Setup, record.Actions);
                return true;
            }
            catch (InvalidOperationException)
            {
                // El registro guardado ya no se puede reproducir: datos corruptos o reglas cambiadas.
                errorCode = ErrorCodes.CorruptMatch;
                return false;
            }
        }

        // Plazo por paso, con el reloj del SERVIDOR: si el jugador al que le toca no actuó a
        // tiempo, pierde. No es una acción del motor: se resuelve por encima del replay y se
        // guarda como fin forzado. Devuelve true si cambió el registro.
        private static bool ApplyClockRules(
            MatchRecord record, GameState state, CatalogVersion catalog, long now)
        {
            if (IsFinished(record, state) || record.Players.Count < 2)
                return false;

            if (now - record.TurnStartedUnix <= catalog.StepTimeoutSeconds)
                return false;

            int loser = (int)state.CurrentPlayer;
            record.ForcedWinnerPlayerId = record.Players[1 - loser];
            record.ForcedEndReason = "timeout";
            return true;
        }

        private static bool IsFinished(MatchRecord record, GameState state)
        {
            return !string.IsNullOrEmpty(record.ForcedEndReason) || (state != null && state.IsOver);
        }

        private static GameAction ToAction(PlayerId side, PlayMoveRequest request)
        {
            switch (request.Kind)
            {
                case "deploy":
                    return GameAction.Deploy(side, request.CardId, request.Lane, request.Slot);

                case "end":
                    return GameAction.End(side);

                default:
                    return null;
            }
        }

        // La VISTA para quien llama. Es lo único que sale del servidor: nunca el registro, ni el
        // Setup (orden de los mazos), ni la mano del rival.
        private MatchStateDto BuildState(
            string callerId, MatchRecord record, ReplayResult replay,
            CatalogVersion catalog, long now, int actionsSeen)
        {
            PlayerId side = (PlayerId)record.Players.IndexOf(callerId);
            GameEngine engine = catalog.CreateEngine();
            bool finished = IsFinished(record, replay.State);

            int seen = Math.Max(0, Math.Min(actionsSeen, replay.EventsPerAction.Count));
            List<GameEvent> news = GameEngine.RedactFor(
                replay.EventsPerAction.Skip(seen).SelectMany(e => e), side);

            var dto = new MatchStateDto
            {
                MatchId = record.MatchId,
                Status = finished ? "finished" : "in_progress",
                YourSide = side.ToString(),
                View = engine.CreateView(replay.State, side),
                ActionCount = replay.EventsPerAction.Count,
                NewEvents = news
            };

            if (!finished)
            {
                dto.SecondsLeftInStep = (int)Math.Max(0, catalog.StepTimeoutSeconds - (now - record.TurnStartedUnix));
                return dto;
            }

            if (replay.State.IsOver)
            {
                dto.EndReason = "portal";
                dto.Result = replay.State.Winner == side ? "won" : "lost";
            }
            else
            {
                dto.EndReason = record.ForcedEndReason;
                dto.Result = record.ForcedWinnerPlayerId == callerId ? "won" : "lost";
            }

            return dto;
        }

        private async Task NotifySafeAsync(string matchId, string playerId)
        {
            try
            {
                await notifier.NotifyMatchUpdatedAsync(matchId, playerId);
            }
            catch (Exception)
            {
                // Un push perdido nunca falla la jugada: ya quedó guardada y el rival hace sondeo.
            }
        }
    }

    // Códigos de error estables que entiende el cliente (no son mensajes para mostrar).
    public static class ErrorCodes
    {
        public const string ProtocolMismatch = "protocol_mismatch";
        public const string InvalidRequest = "invalid_request";
        public const string NotFound = "not_found";
        public const string NotAPlayer = "not_a_player";
        public const string NotStarted = "not_started";
        public const string MatchFull = "match_full";
        public const string MatchOver = "match_over";
        public const string CodeTaken = "code_taken";
        public const string Conflict = "conflict";
        public const string CatalogUnavailable = "catalog_unavailable";
        public const string CorruptMatch = "corrupt_match";
        public const string StaleTurn = "stale_turn";
        public const string NotYourTurn = "not_your_turn";
        public const string WrongStep = "wrong_step";
        public const string CardNotInHand = "card_not_in_hand";
        public const string InvalidLane = "invalid_lane";
        public const string InvalidSlot = "invalid_slot";
        public const string TerrainNotAllowed = "terrain_not_allowed";
        public const string LaneFull = "lane_full";
        public const string CannotShareLane = "cannot_share_lane";

        public static string FromResult(ActionResult result)
        {
            switch (result)
            {
                case ActionResult.GameOver: return MatchOver;
                case ActionResult.StaleTurn: return StaleTurn;
                case ActionResult.NotYourTurn: return NotYourTurn;
                case ActionResult.WrongStep: return WrongStep;
                case ActionResult.CardNotInHand: return CardNotInHand;
                case ActionResult.InvalidLane: return InvalidLane;
                case ActionResult.InvalidSlot: return InvalidSlot;
                case ActionResult.TerrainNotAllowed: return TerrainNotAllowed;
                case ActionResult.LaneFull: return LaneFull;
                case ActionResult.CannotShareLane: return CannotShareLane;
                default: return InvalidRequest;
            }
        }
    }
}
