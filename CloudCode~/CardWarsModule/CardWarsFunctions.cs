using System;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace CardWars.CloudCode
{
    // Las funciones que el cliente puede llamar. Son DELGADAS a propósito: solo saben de la SDK.
    // Toda la autoridad vive en MatchService, que se prueba sin desplegar nada.
    //
    // Regla de oro: quién llama es SIEMPRE context.PlayerId. Ninguna función recibe un
    // "playerId" como parámetro.
    public class CardWarsFunctions
    {
        private static readonly Lazy<EmbeddedCatalogProvider> Catalogs =
            new Lazy<EmbeddedCatalogProvider>(() => new EmbeddedCatalogProvider());

        private static MatchService CreateService(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient)
        {
            return new MatchService(
                new CloudSaveMatchStore(context, gameApiClient),
                new CloudCodePushNotifier(context, pushClient),
                Catalogs.Value);
        }

        // El cliente NO lee el catálogo por su cuenta: lo pide aquí, así usa el mismo que el servidor.
        [CloudCodeFunction("GetGameConfig")]
        public GameConfigDto GetGameConfig(IExecutionContext context)
        {
            return new MatchService(null, null, Catalogs.Value).GetGameConfig();
        }

        // Crea una partida a la espera de rival. Devuelve el código (matchId) para compartirlo.
        // Idempotente por requestId.
        [CloudCodeFunction("CreateMatch")]
        public Task<MatchResponse> CreateMatch(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient,
            string requestId, int protocolVersion)
        {
            return CreateService(context, gameApiClient, pushClient)
                .CreateMatchAsync(context.PlayerId, requestId, protocolVersion);
        }

        [CloudCodeFunction("JoinMatch")]
        public Task<MatchResponse> JoinMatch(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient,
            string matchId, int protocolVersion)
        {
            return CreateService(context, gameApiClient, pushClient)
                .JoinMatchAsync(context.PlayerId, matchId, protocolVersion);
        }

        // kind: "deploy" (usa cardId, lane, slot) o "end".
        [CloudCodeFunction("PlayMove")]
        public Task<MatchResponse> PlayMove(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient,
            string matchId, string kind, int cardId, int lane, int slot,
            int expectedTurnNumber, int actionsSeen, int protocolVersion)
        {
            var request = new PlayMoveRequest
            {
                ProtocolVersion = protocolVersion,
                Kind = kind,
                CardId = cardId,
                Lane = lane,
                Slot = slot,
                ExpectedTurnNumber = expectedTurnNumber,
                ActionsSeen = actionsSeen
            };

            return CreateService(context, gameApiClient, pushClient)
                .PlayMoveAsync(context.PlayerId, matchId, request);
        }

        // Refresca (tras un push, al reconectar o como sondeo). También resuelve el plazo vencido.
        [CloudCodeFunction("GetMatchState")]
        public Task<MatchResponse> GetMatchState(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient,
            string matchId, int actionsSeen, int protocolVersion)
        {
            return CreateService(context, gameApiClient, pushClient)
                .GetMatchStateAsync(context.PlayerId, matchId, actionsSeen, protocolVersion);
        }

        [CloudCodeFunction("LeaveMatch")]
        public Task<MatchResponse> LeaveMatch(
            IExecutionContext context, IGameApiClient gameApiClient, IPushClient pushClient,
            string matchId, int protocolVersion)
        {
            return CreateService(context, gameApiClient, pushClient)
                .LeaveMatchAsync(context.PlayerId, matchId, protocolVersion);
        }
    }
}
