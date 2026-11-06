using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace CardWars.CloudCode
{
    // Guarda el registro de la partida en Cloud Save Custom Data, un documento por partida
    // (customId = matchId), con una sola clave "state".
    //
    // ACCESS CLASS Private: el registro contiene el Setup (el orden de los mazos de los dos
    // jugadores). Ningún cliente debe poder leerlo ni escribirlo, y menos con su token por la
    // API REST. Por eso se usan las variantes *Private* de Custom Data y NO las de Default
    // (la lección ERR-KB-006: lo que decide el servidor nunca va en una clase que el jugador
    // pueda escribir).
    //
    // SIN VERIFICAR CONTRA UN PROYECTO UGS REAL: ver Docs/PRUEBAS.md (actividad P13).
    public sealed class CloudSaveMatchStore : IMatchStore
    {
        private const string StateKey = "state";

        private static readonly JsonSerializerOptions Json =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly IExecutionContext context;
        private readonly IGameApiClient api;

        public CloudSaveMatchStore(IExecutionContext context, IGameApiClient api)
        {
            this.context = context;
            this.api = api;
        }

        public async Task<StoredMatch> LoadAsync(string matchId)
        {
            var response = await api.CloudSaveData.GetPrivateCustomItemsAsync(
                context, context.ServiceToken, context.ProjectId, matchId,
                new List<string> { StateKey });

            var item = response.Data.Results.FirstOrDefault(i => i.Key == StateKey);

            if (item == null)
                return null;

            string text = item.Value is JsonElement element
                ? (element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText())
                : item.Value?.ToString();

            var record = JsonSerializer.Deserialize<MatchRecord>(text ?? string.Empty, Json);

            return record == null
                ? null
                : new StoredMatch { Record = record, WriteLock = item.WriteLock };
        }

        public async Task<bool> TryCreateAsync(MatchRecord record)
        {
            // Primero se mira si ya existe, para no pisar una partida en curso si el candado
            // vacío no se comportara como "solo crear si no existe" (ver nota de arriba).
            if (await LoadAsync(record.MatchId) != null)
                return false;

            return await WriteAsync(record, writeLock: string.Empty);
        }

        public Task<bool> TrySaveAsync(StoredMatch match)
        {
            return WriteAsync(match.Record, match.WriteLock);
        }

        private async Task<bool> WriteAsync(MatchRecord record, string writeLock)
        {
            // Se guarda como TEXTO JSON para que el documento sea idéntico al que reproduce el motor.
            string json = JsonSerializer.Serialize(record, Json);

            try
            {
                await api.CloudSaveData.SetPrivateCustomItemAsync(
                    context, context.ServiceToken, context.ProjectId, record.MatchId,
                    new SetItemBody(StateKey, json, writeLock));

                return true;
            }
            catch (ApiException ex) when (IsConflict(ex))
            {
                // Otra llamada guardó antes: el candado de escritura ya no coincide.
                return false;
            }
        }

        private static bool IsConflict(ApiException ex)
        {
            return ex.Response != null && (int)ex.Response.StatusCode == 409;
        }
    }
}
