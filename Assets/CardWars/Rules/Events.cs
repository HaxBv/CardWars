using System.Collections.Generic;

namespace CardWars.Rules
{
    public enum ActionResult
    {
        Applied,
        GameOver,
        StaleTurn,
        NotYourTurn,
        WrongStep,
        CardNotInHand,
        InvalidLane,
        InvalidSlot,
        TerrainNotAllowed,
        LaneFull,
        CannotShareLane
    }

    public enum GameEventType
    {
        StepStarted,
        TroopDeployed,
        TroopMoved,
        CardDrawn,
        Attack,
        TroopDied,
        ShieldBroken,
        PortalDamaged,
        GameWon
    }

    // Un hecho que ocurrió. El cliente los "reproduce" con animaciones;
    // no decide nada con ellos.
    public sealed class GameEvent
    {
        public GameEventType Type { get; set; }

        // Jugador protagonista: quien despliega, ataca o roba, o el dueño de la tropa o portal afectado.
        public PlayerId Player { get; set; }
        public int Lane { get; set; }
        public int Slot { get; set; }
        public int CardId { get; set; } = -1;

        // Solo en Attack: -1 si el objetivo es el portal.
        public int TargetSlot { get; set; } = -1;
        public int Amount { get; set; }

        public GameEvent Clone()
        {
            return (GameEvent)MemberwiseClone();
        }
    }

    public sealed class ActionOutcome
    {
        public ActionResult Result { get; }
        public List<GameEvent> Events { get; }
        public bool Ok => Result == ActionResult.Applied;

        public ActionOutcome(ActionResult result, List<GameEvent> events = null)
        {
            Result = result;
            Events = events ?? new List<GameEvent>();
        }
    }
}
