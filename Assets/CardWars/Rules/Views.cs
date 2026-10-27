using System.Collections.Generic;

namespace CardWars.Rules
{
    // Lo que un jugador PUEDE saber de la partida. No contiene la mano ni el mazo del rival,
    // solo cuántas cartas tiene. Es lo único que un servidor debería devolver a un cliente.
    public sealed class TroopView
    {
        public int CardId;
        public int Attack;
        public int Health;
        public int MaxHealth;
    }

    public sealed class PortalView
    {
        public int Health;
        public int MaxHealth;
        public int RemainingShields;
    }

    public sealed class LaneView
    {
        public LaneTerrainKind Terrain;

        // [jugador][espacio - 1]; null si está vacío. La mesa es pública.
        public TroopView[][] Troops;
    }

    public sealed class PlayerView
    {
        public PlayerId Me;
        public int Round;
        public TimeOfDayKind TimeOfDay;
        public int Step;
        public int TurnNumber;
        public PlayerId CurrentPlayer;
        public PlayerId Priority;
        public PlayerId? Winner;

        public List<int> MyHand;
        public int MyDeckCount;
        public int OpponentHandCount;
        public int OpponentDeckCount;

        public PortalView MyPortal;
        public PortalView OpponentPortal;
        public List<LaneView> Lanes;
    }
}
