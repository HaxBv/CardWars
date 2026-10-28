using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CardWars.CloudCode
{
    // La lógica de partidas (MatchService) solo habla con estas interfaces. Los adaptadores que
    // usan el SDK de Cloud Code (CloudSaveMatchStore, CloudCodePushNotifier) las implementan,
    // y las pruebas usan versiones en memoria. Así la autoridad se prueba sin desplegar nada.

    public sealed class StoredMatch
    {
        public MatchRecord Record { get; set; }

        // Candado de escritura de Cloud Save leído junto con el registro. Al guardar se envía
        // de vuelta: si otra llamada guardó antes, el guardado falla (TrySave devuelve false).
        public string WriteLock { get; set; }
    }

    public interface IMatchStore
    {
        // null si no existe.
        Task<StoredMatch> LoadAsync(string matchId);

        // false si ya existe una partida con ese id.
        Task<bool> TryCreateAsync(MatchRecord record);

        // false si hubo un conflicto de escritura (otra llamada guardó primero).
        Task<bool> TrySaveAsync(StoredMatch match);
    }

    public interface IMatchNotifier
    {
        // Aviso liviano "match_updated" {type, matchId}. Nunca lleva estado.
        // Puede fallar sin consecuencias: el rival tiene el sondeo de respaldo.
        Task NotifyMatchUpdatedAsync(string matchId, string playerId);
    }

    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    public static class MatchIds
    {
        // Alfabeto sin 0/O/1/I para que el código se pueda dictar o escribir.
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        // El id de la partida (que también sirve de código para unirse) sale de un hash de
        // (quien crea + requestId). Así CreateMatch es IDEMPOTENTE: reintentar con el mismo
        // requestId devuelve la misma partida y nunca crea dos. El cliente no elige el id, por lo
        // que tampoco puede usarlo para escribir en un documento arbitrario.
        public static string ForCreate(string creatorPlayerId, string requestId)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(creatorPlayerId + "|" + requestId));
            var sb = new StringBuilder(8);

            for (int i = 0; i < 8; i++)
                sb.Append(Alphabet[hash[i] % Alphabet.Length]);

            return sb.ToString();
        }

        public static bool LooksValid(string matchId)
        {
            if (matchId == null || matchId.Length != 8)
                return false;

            foreach (char c in matchId)
            {
                if (Alphabet.IndexOf(c) < 0)
                    return false;
            }

            return true;
        }
    }
}
