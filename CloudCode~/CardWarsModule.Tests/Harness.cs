using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CardWars.Rules;

namespace CardWars.CloudCode.Tests
{
    // Almacén en memoria. Guarda el registro como TEXTO JSON, igual que Cloud Save, así las
    // pruebas también comprueban que el registro sobrevive a serializar y deserializar.
    // Imita el candado de escritura: si alguien guardó antes, TrySave devuelve false.
    public sealed class InMemoryMatchStore : IMatchStore
    {
        private readonly Dictionary<string, (string Json, int Version)> docs =
            new Dictionary<string, (string, int)>();

        public bool AlwaysConflict { get; set; }

        public Task<StoredMatch> LoadAsync(string matchId)
        {
            if (!docs.TryGetValue(matchId, out var doc))
                return Task.FromResult<StoredMatch>(null);

            return Task.FromResult(new StoredMatch
            {
                Record = JsonSerializer.Deserialize<MatchRecord>(doc.Json),
                WriteLock = doc.Version.ToString()
            });
        }

        public Task<bool> TryCreateAsync(MatchRecord record)
        {
            if (docs.ContainsKey(record.MatchId))
                return Task.FromResult(false);

            docs[record.MatchId] = (JsonSerializer.Serialize(record), 1);
            return Task.FromResult(true);
        }

        public Task<bool> TrySaveAsync(StoredMatch match)
        {
            if (AlwaysConflict || !docs.TryGetValue(match.Record.MatchId, out var doc)
                || doc.Version.ToString() != match.WriteLock)
            {
                return Task.FromResult(false);
            }

            docs[match.Record.MatchId] = (JsonSerializer.Serialize(match.Record), doc.Version + 1);
            return Task.FromResult(true);
        }

        // Para las pruebas: lo que hay guardado, tal cual (es lo que NUNCA debe salir al cliente).
        public MatchRecord Peek(string matchId)
        {
            return JsonSerializer.Deserialize<MatchRecord>(docs[matchId].Json);
        }

        public string PeekJson(string matchId)
        {
            return docs[matchId].Json;
        }

        public int Count => docs.Count;

        // Simula datos corruptos.
        public void Tamper(string matchId, Action<MatchRecord> edit)
        {
            MatchRecord record = Peek(matchId);
            edit(record);
            docs[matchId] = (JsonSerializer.Serialize(record), docs[matchId].Version + 1);
        }
    }

    public sealed class RecordingNotifier : IMatchNotifier
    {
        public List<(string MatchId, string PlayerId)> Sent { get; } =
            new List<(string, string)>();

        public bool Throws { get; set; }

        public Task NotifyMatchUpdatedAsync(string matchId, string playerId)
        {
            if (Throws)
                throw new InvalidOperationException("push caído");

            Sent.Add((matchId, playerId));
            return Task.CompletedTask;
        }
    }

    public sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

        public void AdvanceSeconds(int seconds)
        {
            UtcNow = UtcNow.AddSeconds(seconds);
        }
    }

    // Catálogos de prueba: permite tener varias versiones a la vez.
    public sealed class TestCatalogs : ICatalogProvider
    {
        private readonly Dictionary<string, CatalogVersion> versions = new Dictionary<string, CatalogVersion>();

        public string CurrentVersion { get; set; }

        public TestCatalogs(params CatalogVersion[] catalogs)
        {
            foreach (CatalogVersion c in catalogs)
                versions[c.Version] = c;

            CurrentVersion = catalogs.Last().Version;
        }

        public CatalogVersion Get(string version)
        {
            return versions.TryGetValue(version ?? string.Empty, out CatalogVersion v) ? v : null;
        }

        // Una copia del catálogo embebido con otra versión y retoques.
        public static CatalogVersion Variant(string version, Action<CatalogFile> tweak = null)
        {
            CatalogFile source = new EmbeddedCatalogProvider().Get("v1").File;
            var json = JsonSerializer.Serialize(source);
            var copy = JsonSerializer.Deserialize<CatalogFile>(json);
            copy.Version = version;
            tweak?.Invoke(copy);
            return new CatalogVersion(copy);
        }
    }

    // Una partida entre "alice" y "bob" con todo lo necesario para probarla.
    public sealed class Game
    {
        public const string Alice = "alice";   // ordenados: alice = Player1, bob = Player2
        public const string Bob = "bob";

        public InMemoryMatchStore Store { get; } = new InMemoryMatchStore();
        public RecordingNotifier Notifier { get; } = new RecordingNotifier();
        public FakeClock Clock { get; } = new FakeClock();
        public ICatalogProvider Catalogs { get; set; }
        public MatchService Service { get; private set; }
        public string MatchId { get; private set; }

        public Game(ICatalogProvider catalogs = null, int seed = 1234)
        {
            Catalogs = catalogs ?? new EmbeddedCatalogProvider();
            Service = new MatchService(Store, Notifier, Catalogs, Clock, () => seed);
        }

        public async Task<Game> StartAsync()
        {
            MatchResponse created = await Service.CreateMatchAsync(Alice, "req-1", GameProtocol.Version);
            MatchId = created.State.MatchId;
            await Service.JoinMatchAsync(Bob, MatchId, GameProtocol.Version);
            return this;
        }

        public Task<MatchResponse> StateAsync(string who, int seen = 0)
        {
            return Service.GetMatchStateAsync(who, MatchId, seen, GameProtocol.Version);
        }

        // Quién tiene el turno ahora (según la vista que recibe el servidor).
        public async Task<string> CurrentAsync()
        {
            MatchResponse r = await StateAsync(Alice);
            return r.State.View.CurrentPlayer == PlayerId.Player1 ? Alice : Bob;
        }

        public static string Other(string who)
        {
            return who == Alice ? Bob : Alice;
        }

        // Un PlayMove como lo haría un cliente honesto: usa el turno y las acciones que vio.
        public async Task<MatchResponse> MoveAsync(
            string who, string kind, int cardId = 0, int lane = 0, int slot = 1)
        {
            MatchStateDto s = (await StateAsync(who)).State;

            return await Service.PlayMoveAsync(who, MatchId, new PlayMoveRequest
            {
                ProtocolVersion = GameProtocol.Version,
                Kind = kind,
                CardId = cardId,
                Lane = lane,
                Slot = slot,
                ExpectedTurnNumber = s.View.TurnNumber,
                ActionsSeen = s.ActionCount
            });
        }

        // Reconstruye la partida desde lo guardado (solo para que la prueba "mire" por dentro).
        public GameState Peek()
        {
            MatchRecord r = Store.Peek(MatchId);
            CatalogVersion c = Catalogs.Get(r.CatalogVersion);
            return c.CreateEngine().Replay(r.Setup, r.Actions).State;
        }
    }
}
