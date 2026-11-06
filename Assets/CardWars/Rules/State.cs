using System.Collections.Generic;

namespace CardWars.Rules
{
    public sealed class TroopState
    {
        public int CardId { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }

        public bool IsAlive => Health > 0;

        // Igual que TroopInGame.CanAttack.
        public bool CanAttack => IsAlive && Attack > 0;
    }

    public sealed class PortalState
    {
        public int MaxHealth { get; set; }
        public int Health { get; set; }
        public int RemainingShields { get; set; }

        public bool IsDestroyed => Health <= 0;
    }

    // Un carril tiene 2 espacios por jugador. Espacio 1 = FirstSlot, espacio 2 = SecondSlot.
    // En el combate el espacio 2 es el "adelantado" (CombatManager.FindTarget).
    public sealed class LaneState
    {
        public const int SlotsPerSide = 2;

        public LaneTerrainKind Terrain { get; set; }

        private readonly TroopState[][] troops =
        {
            new TroopState[SlotsPerSide],
            new TroopState[SlotsPerSide]
        };

        public LaneState(LaneTerrainKind terrain)
        {
            Terrain = terrain;
        }

        // slot: 1 o 2.
        public TroopState GetTroop(PlayerId player, int slot)
        {
            return troops[(int)player][slot - 1];
        }

        public void SetTroop(PlayerId player, int slot, TroopState troop)
        {
            troops[(int)player][slot - 1] = troop;
        }

        public int TroopCount(PlayerId player)
        {
            int count = 0;

            for (int slot = 1; slot <= SlotsPerSide; slot++)
            {
                if (GetTroop(player, slot) != null)
                    count++;
            }

            return count;
        }

        // Si el espacio 1 queda libre y el 2 ocupado, la tropa pasa al 1
        // (LandLane.RefreshTroopPositions).
        public void Compact(PlayerId player)
        {
            if (GetTroop(player, 1) == null && GetTroop(player, 2) != null)
            {
                SetTroop(player, 1, GetTroop(player, 2));
                SetTroop(player, 2, null);
            }
        }
    }

    public sealed class PlayerState
    {
        // El orden del mazo es información oculta: la última carta es la que se roba.
        public List<int> Deck { get; } = new List<int>();
        public List<int> Hand { get; } = new List<int>();
        public PortalState Portal { get; } = new PortalState();
    }

    public sealed class GameState
    {
        public PlayerState[] Players { get; } =
            { new PlayerState(), new PlayerState() };

        public List<LaneState> Lanes { get; } = new List<LaneState>();

        public int Round { get; set; } = 1;
        public TimeOfDayKind TimeOfDay { get; set; } = TimeOfDayKind.Alba;

        // 1..5 como PhasesSystem.pasoTurnoActual:
        // 1-2 tropas, 3-4 hechizos y dominios, 5 combate.
        public int Step { get; set; } = 1;

        public PlayerId FirstRoundPriority { get; set; }
        public PlayerId Priority { get; set; }

        // Sube con cada paso. Pensado para el "turnoEsperado" de los mensajes online.
        public int TurnNumber { get; set; } = 1;

        public PlayerId? Winner { get; set; }
        public bool IsOver => Winner.HasValue;

        public PlayerState Get(PlayerId player)
        {
            return Players[(int)player];
        }

        // Jugador al que le toca actuar en el paso actual: los pasos 1 y 3 son de quien
        // tiene la prioridad y los 2 y 4, del otro.
        public PlayerId CurrentPlayer =>
            (Step == 1 || Step == 3) ? Priority : GameEngine.Opponent(Priority);
    }
}
