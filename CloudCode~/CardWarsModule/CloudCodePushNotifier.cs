using System.Text.Json;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Core;

namespace CardWars.CloudCode
{
    // Los mensajes push del curso: Cloud Code -> el otro jugador (Wire).
    //
    // El payload es SIEMPRE liviano, {"type":"match_updated","matchId":"..."}, y NUNCA lleva
    // estado, cartas ni jugadas: el rival no confía en el contenido del mensaje, solo lo usa
    // como señal para volver a pedir GetMatchState (que sí lo valida y le entrega SU vista).
    //
    // El cliente se suscribe con CloudCodeService.Instance.SubscribeToPlayerMessagesAsync y
    // mantiene un sondeo de respaldo (~10 s) por si se pierde un push.
    public sealed class CloudCodePushNotifier : IMatchNotifier
    {
        public const string MessageType = "match_updated";

        private readonly IExecutionContext context;
        private readonly IPushClient pushClient;

        public CloudCodePushNotifier(IExecutionContext context, IPushClient pushClient)
        {
            this.context = context;
            this.pushClient = pushClient;
        }

        public async Task NotifyMatchUpdatedAsync(string matchId, string playerId)
        {
            string payload = JsonSerializer.Serialize(new { type = MessageType, matchId });
            await pushClient.SendPlayerMessageAsync(context, payload, MessageType, playerId);
        }
    }
}
