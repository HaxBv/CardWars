using System;
using System.Collections.Generic;

namespace CardWars.Rules
{
    // Todo el azar de una partida, sorteado una sola vez al empezar. Contiene el orden de los
    // mazos, o sea información oculta: es un dato PRIVADO del servidor.
    public sealed class GameSetup
    {
        public List<int> Player1Deck { get; set; } = new List<int>();
        public List<int> Player2Deck { get; set; } = new List<int>();
        public PlayerId FirstPlayer { get; set; }
    }

    public enum GameActionKind
    {
        DeployTroop,
        EndStep
    }

    // Una acción ya validada y aceptada. La partida se guarda como (GameSetup + lista de
    // GameAction, solo de agregar) y el estado se obtiene con GameEngine.Replay.
    public sealed class GameAction
    {
        public GameActionKind Kind { get; set; }
        public PlayerId Player { get; set; }

        // Solo en DeployTroop.
        public int CardId { get; set; }
        public int Lane { get; set; }
        public int Slot { get; set; }

        public static GameAction Deploy(PlayerId player, int cardId, int lane, int slot)
        {
            return new GameAction
            {
                Kind = GameActionKind.DeployTroop,
                Player = player,
                CardId = cardId,
                Lane = lane,
                Slot = slot
            };
        }

        public static GameAction End(PlayerId player)
        {
            return new GameAction { Kind = GameActionKind.EndStep, Player = player };
        }
    }

    public sealed class ReplayResult
    {
        public GameState State { get; set; }

        // Los eventos que produjo cada acción, en el mismo orden que la lista de acciones.
        // Permite devolver a un cliente solo lo ocurrido desde la última vez que consultó.
        public List<List<GameEvent>> EventsPerAction { get; } = new List<List<GameEvent>>();
    }

    public sealed partial class GameEngine
    {
        public ActionOutcome Apply(
            GameState state, GameAction action, int expectedTurnNumber = NoExpectedTurn)
        {
            switch (action.Kind)
            {
                case GameActionKind.DeployTroop:
                    return DeployTroop(
                        state, action.Player, action.CardId, action.Lane, action.Slot,
                        expectedTurnNumber);

                case GameActionKind.EndStep:
                    return EndStep(state, action.Player, expectedTurnNumber);

                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        // Reconstruye la partida desde cero. Es una función pura de (setup, acciones): sin reloj,
        // sin azar. Un registro guardado solo contiene acciones que eran válidas al guardarse;
        // si una deja de serlo al reproducirla, el registro está corrupto (o las reglas o el
        // catálogo de cartas cambiaron) y se falla de forma ruidosa.
        public ReplayResult Replay(GameSetup setup, IEnumerable<GameAction> actions)
        {
            var result = new ReplayResult { State = StartGame(setup) };
            int index = 0;

            foreach (GameAction action in actions)
            {
                ActionOutcome outcome = Apply(result.State, action);

                if (!outcome.Ok)
                {
                    throw new InvalidOperationException(
                        $"Registro de partida corrupto: la acción {index} ({action.Kind}) " +
                        $"es inválida al reproducirla ({outcome.Result}).");
                }

                result.EventsPerAction.Add(outcome.Events);
                index++;
            }

            return result;
        }
    }
}
