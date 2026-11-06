using System.Collections.Generic;

namespace CardWars.Rules
{
    // Lo que un jugador PUEDE saber de la partida. No contiene la mano ni el mazo del rival,
    // solo cuántas cartas tiene. Es lo único que un servidor debería devolver a un cliente
    // (el módulo lo copia a su DTO de respuesta, como hace MatchStateStore.ToDto en TTTXO).
    // Son propiedades (no campos) para que cualquier serializador JSON las incluya.
    public sealed class TroopView
    {
        public int CardId { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
    }

    public sealed class PortalView
    {
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int RemainingShields { get; set; }
    }

    public sealed class LaneView
    {
        public LaneTerrainKind Terrain { get; set; }

        // [jugador][espacio - 1]; null si está vacío. La mesa es pública.
        public TroopView[][] Troops { get; set; }
    }

    public sealed class PlayerView
    {
        public PlayerId Me { get; set; }
        public int Round { get; set; }
        public TimeOfDayKind TimeOfDay { get; set; }
        public int Step { get; set; }
        public int TurnNumber { get; set; }
        public PlayerId CurrentPlayer { get; set; }
        public PlayerId Priority { get; set; }
        public PlayerId? Winner { get; set; }

        public List<int> MyHand { get; set; }
        public int MyDeckCount { get; set; }
        public int OpponentHandCount { get; set; }
        public int OpponentDeckCount { get; set; }

        public PortalView MyPortal { get; set; }
        public PortalView OpponentPortal { get; set; }
        public List<LaneView> Lanes { get; set; }
    }
}
