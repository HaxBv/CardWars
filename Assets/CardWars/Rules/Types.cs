using System.Collections.Generic;

// Motor de reglas de CardWars: C# puro, sin UnityEngine ni Odin.
// Es lo que un servidor (Cloud Code) podría ejecutar tal cual.
namespace CardWars.Rules
{
    public enum PlayerId
    {
        Player1 = 0,
        Player2 = 1
    }

    public enum TimeOfDayKind
    {
        Alba,
        Ocaso
    }

    public enum LaneTerrainKind
    {
        Land,
        Highground,
        Aquatic
    }

    // Datos de una carta de tropa que el motor necesita para aplicar las reglas.
    // En el juego salen de TroopCardData (ScriptableObject); aquí son datos simples.
    public sealed class CardDefinition
    {
        public int Id { get; }
        public string Name { get; }
        public int Attack { get; }
        public int Health { get; }
        public int MaxDeckCopies { get; }

        // Pasivas que afectan al despliegue (AbilityPassive.Team / AllTerrain / Highground / Aquatic).
        public bool HasTeam { get; }
        public bool AllTerrain { get; }
        public bool Highground { get; }
        public bool Aquatic { get; }

        public CardDefinition(
            int id,
            string name,
            int attack,
            int health,
            int maxDeckCopies = 1,
            bool hasTeam = false,
            bool allTerrain = false,
            bool highground = false,
            bool aquatic = false)
        {
            Id = id;
            Name = name;
            Attack = attack;
            Health = health;
            MaxDeckCopies = maxDeckCopies;
            HasTeam = hasTeam;
            AllTerrain = allTerrain;
            Highground = highground;
            Aquatic = aquatic;
        }

        // Igual que LandLane.CanDeployOnTerrain.
        public bool CanDeployOn(LaneTerrainKind terrain)
        {
            if (terrain == LaneTerrainKind.Land || AllTerrain)
                return true;

            return terrain == LaneTerrainKind.Highground ? Highground : Aquatic;
        }
    }

    public sealed class GameConfig
    {
        // Un elemento por carril; el índice es el LaneID.
        public List<LaneTerrainKind> LaneTerrains { get; set; } =
            new List<LaneTerrainKind>
            {
                LaneTerrainKind.Land,
                LaneTerrainKind.Land,
                LaneTerrainKind.Land
            };

        public int InitialHandSize { get; set; } = 4;      // DeckHandManager.InitialCards
        public int MaxHandSize { get; set; } = 8;          // DeckHandManager.MaxHandSize
        public int PortalMaxHealth { get; set; } = 50;     // Portals.maxHealth
        public int PortalShields { get; set; } = 3;        // Portals.MaxShields
    }
}
