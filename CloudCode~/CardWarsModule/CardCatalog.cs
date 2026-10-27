using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CardWars.Rules;

namespace CardWars.CloudCode
{
    // Forma del archivo Data/cards.<versión>.json. Es lo que un menú del Editor de Unity debería
    // generar a partir de los ScriptableObject (tarea para el alumno).
    public sealed class CardJson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxDeckCopies { get; set; }
        public bool HasTeam { get; set; }
        public bool AllTerrain { get; set; }
        public bool Highground { get; set; }
        public bool Aquatic { get; set; }
    }

    public sealed class CatalogFile
    {
        public string Version { get; set; } = string.Empty;
        public int StepTimeoutSeconds { get; set; } = 180;
        public List<string> LaneTerrains { get; set; } = new List<string>();
        public int InitialHandSize { get; set; } = 4;
        public int MaxHandSize { get; set; } = 8;
        public int PortalMaxHealth { get; set; } = 50;
        public int PortalShields { get; set; } = 3;
        public List<CardJson> Cards { get; set; } = new List<CardJson>();
    }

    // Una versión del catálogo ya convertida a los tipos del motor.
    public sealed class CatalogVersion
    {
        public string Version { get; }
        public int StepTimeoutSeconds { get; }
        public GameConfig Config { get; }
        public IReadOnlyList<CardDefinition> Cards { get; }
        public CatalogFile File { get; }

        public CatalogVersion(CatalogFile file)
        {
            File = file;
            Version = file.Version;
            StepTimeoutSeconds = file.StepTimeoutSeconds;

            Config = new GameConfig
            {
                LaneTerrains = file.LaneTerrains
                    .Select(t => (LaneTerrainKind)Enum.Parse(typeof(LaneTerrainKind), t, true))
                    .ToList(),
                InitialHandSize = file.InitialHandSize,
                MaxHandSize = file.MaxHandSize,
                PortalMaxHealth = file.PortalMaxHealth,
                PortalShields = file.PortalShields
            };

            Cards = file.Cards
                .Select(c => new CardDefinition(
                    c.Id, c.Name, c.Attack, c.Health, c.MaxDeckCopies,
                    c.HasTeam, c.AllTerrain, c.Highground, c.Aquatic))
                .ToList();
        }

        public GameEngine CreateEngine()
        {
            return new GameEngine(Cards, Config);
        }
    }

    public interface ICatalogProvider
    {
        // La versión con la que empiezan las partidas nuevas.
        string CurrentVersion { get; }

        // null si esa versión ya no está disponible.
        CatalogVersion Get(string version);
    }

    // Lee los catálogos de los recursos embebidos Data/*.json.
    // Una partida guarda su CatalogVersion; para poder reproducirla, la versión debe seguir aquí.
    // NUNCA se edita un archivo publicado: se crea uno nuevo con otra versión.
    public sealed class EmbeddedCatalogProvider : ICatalogProvider
    {
        private readonly Dictionary<string, CatalogVersion> versions =
            new Dictionary<string, CatalogVersion>();

        public string CurrentVersion { get; }

        public EmbeddedCatalogProvider(string currentVersion = null)
        {
            Assembly assembly = typeof(EmbeddedCatalogProvider).Assembly;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (string name in assembly.GetManifestResourceNames()
                         .Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                using Stream stream = assembly.GetManifestResourceStream(name);
                using var reader = new StreamReader(stream);
                var file = JsonSerializer.Deserialize<CatalogFile>(reader.ReadToEnd(), options);

                if (file == null || string.IsNullOrEmpty(file.Version))
                    throw new InvalidOperationException($"Catálogo inválido: {name}");

                versions[file.Version] = new CatalogVersion(file);
            }

            if (versions.Count == 0)
                throw new InvalidOperationException("No hay ningún catálogo embebido en Data/.");

            // Por defecto, la versión más alta por orden alfabético (v1 < v2 < ...).
            CurrentVersion = currentVersion ?? versions.Keys.OrderBy(k => k, StringComparer.Ordinal).Last();
        }

        public CatalogVersion Get(string version)
        {
            return versions.TryGetValue(version ?? string.Empty, out CatalogVersion v) ? v : null;
        }
    }
}
