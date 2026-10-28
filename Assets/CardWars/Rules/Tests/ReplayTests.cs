using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CardWars.Rules.Tests
{
    // Pruebas del patrón "setup + lista de acciones => Replay" que usa un servidor:
    // el estado no se guarda como foto, se reconstruye reproduciendo las acciones.
    public class ReplayTests
    {
        private static GameEngine NewEngine(GameConfig config = null)
        {
            return GameEngineTests.NewEngine(config);
        }

        // Texto que describe TODO el estado, incluidos mazos y manos (vista del servidor).
        private static string Fingerprint(GameState s)
        {
            var sb = new StringBuilder();
            sb.Append($"R{s.Round} {s.TimeOfDay} S{s.Step} T{s.TurnNumber} P{s.Priority} F{s.FirstRoundPriority} W{s.Winner};");

            foreach (PlayerId p in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                PlayerState ps = s.Get(p);
                sb.Append($"{p} deck[{string.Join(",", ps.Deck)}] hand[{string.Join(",", ps.Hand)}] ");
                sb.Append($"portal {ps.Portal.Health}/{ps.Portal.MaxHealth}/{ps.Portal.RemainingShields};");
            }

            for (int i = 0; i < s.Lanes.Count; i++)
            {
                foreach (PlayerId p in new[] { PlayerId.Player1, PlayerId.Player2 })
                {
                    for (int slot = 1; slot <= LaneState.SlotsPerSide; slot++)
                    {
                        TroopState t = s.Lanes[i].GetTroop(p, slot);
                        sb.Append(t == null ? "-" : $"[{i}{p}{slot}:{t.CardId},{t.Attack},{t.Health}/{t.MaxHealth}]");
                    }
                }
            }

            return sb.ToString();
        }

        // Juega al azar y devuelve la lista de acciones ACEPTADAS (lo que un servidor guardaría).
        private static List<GameAction> PlayRandomly(
            GameEngine engine, GameState state, int seed, int maxActions)
        {
            var recorded = new List<GameAction>();
            var rng = new Random(seed);

            for (int i = 0; i < maxActions && !state.IsOver; i++)
            {
                PlayerId actor = state.CurrentPlayer;
                PlayerState ps = state.Get(actor);

                GameAction action;

                if (rng.Next(3) != 0 && ps.Hand.Count > 0)
                {
                    action = GameAction.Deploy(
                        actor, ps.Hand[rng.Next(ps.Hand.Count)], rng.Next(0, 3), rng.Next(1, 3));
                }
                else
                {
                    action = GameAction.End(actor);
                }

                // Solo se guardan las acciones que el motor aceptó (como PlayMove en TTTXO).
                if (engine.Apply(state, action).Ok)
                    recorded.Add(action);
            }

            return recorded;
        }

        // =====================================================
        // EL AZAR QUEDA REGISTRADO
        // =====================================================

        [Test]
        public void Setup_HoldsAllTheRandomness_AndStartGameFromItIsDeterministic()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(5);

            string a = Fingerprint(engine.StartGame(setup));
            string b = Fingerprint(engine.StartGame(setup));
            string fromSeed = Fingerprint(engine.StartGame(5));

            Assert.AreEqual(a, b);
            Assert.AreEqual(a, fromSeed);
        }

        [Test]
        public void StartGame_CopiesTheSetup_SoPlayingDoesNotChangeIt()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(11);
            int deck1Before = setup.Player1Deck.Count;

            GameState state = engine.StartGame(setup);
            PlayToTheEnd(engine, state);

            Assert.AreEqual(deck1Before, setup.Player1Deck.Count);
        }

        private static void PlayToTheEnd(GameEngine engine, GameState state)
        {
            for (int i = 0; i < 400 && !state.IsOver; i++)
                engine.EndStep(state, state.CurrentPlayer);
        }

        [Test]
        public void Setup_SurvivesBeingRebuiltFromPlainLists()
        {
            // Lo que haría un servidor al leer el registro de Cloud Save: solo listas y enteros.
            GameEngine engine = NewEngine();
            GameSetup original = engine.CreateSetup(21);

            var restored = new GameSetup
            {
                Player1Deck = new List<int>(original.Player1Deck),
                Player2Deck = new List<int>(original.Player2Deck),
                FirstPlayer = original.FirstPlayer
            };

            Assert.AreEqual(
                Fingerprint(engine.StartGame(original)),
                Fingerprint(engine.StartGame(restored)));
        }

        // =====================================================
        // REPLAY
        // =====================================================

        [Test]
        public void Replay_RebuildsTheExactSameGame_ForManyRandomGames()
        {
            var config = new GameConfig { PortalMaxHealth = 14 };
            GameEngine engine = NewEngine(config);

            for (int seed = 1; seed <= 40; seed++)
            {
                GameSetup setup = engine.CreateSetup(seed);
                GameState live = engine.StartGame(setup);
                List<GameAction> actions = PlayRandomly(engine, live, seed * 17, 500);

                // Un servidor nuevo, sin memoria de la partida, solo con el registro.
                ReplayResult replayed = NewEngine(config).Replay(setup, actions);

                Assert.AreEqual(Fingerprint(live), Fingerprint(replayed.State), $"seed {seed}");
                Assert.AreEqual(actions.Count, replayed.EventsPerAction.Count);
            }
        }

        [Test]
        public void Replay_OfAnEmptyList_IsTheStartOfTheGame()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(3);

            Assert.AreEqual(
                Fingerprint(engine.StartGame(setup)),
                Fingerprint(engine.Replay(setup, new GameAction[0]).State));
        }

        [Test]
        public void Replay_WithACorruptAction_FailsLoudly()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(3);
            PlayerId first = setup.FirstPlayer;

            // La carta 999 no existe: el registro no puede contenerla.
            var actions = new[] { GameAction.Deploy(first, 999, 0, 1) };

            Assert.Throws<InvalidOperationException>(() => engine.Replay(setup, actions));
        }

        [Test]
        public void Replay_WithAnActionByTheWrongPlayer_FailsLoudly()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(3);
            PlayerId second = GameEngine.Opponent(setup.FirstPlayer);

            Assert.Throws<InvalidOperationException>(
                () => engine.Replay(setup, new[] { GameAction.End(second) }));
        }

        [Test]
        public void Replay_GivesTheEventsOfEachAction_SoAClientCanAskForJustTheNewOnes()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(8);
            GameState live = engine.StartGame(setup);
            List<GameAction> actions = PlayRandomly(engine, live, 1, 60);

            ReplayResult replayed = engine.Replay(setup, actions);

            // Un cliente que ya vio 10 acciones pide las que siguen, y sus robos propios se ven
            // pero los del rival van tapados.
            IEnumerable<GameEvent> news = replayed.EventsPerAction.Skip(10).SelectMany(e => e);
            List<GameEvent> forPlayer1 = GameEngine.RedactFor(news, PlayerId.Player1);

            Assert.IsTrue(forPlayer1
                .Where(e => e.Type == GameEventType.CardDrawn && e.Player == PlayerId.Player2)
                .All(e => e.CardId == -1));
        }

        [Test]
        public void ReplayedView_StillHidesTheOpponentHand()
        {
            GameEngine engine = NewEngine();
            GameSetup setup = engine.CreateSetup(8);
            ReplayResult replayed = engine.Replay(
                setup, PlayRandomly(engine, engine.StartGame(setup), 4, 30));

            PlayerView view = engine.CreateView(replayed.State, PlayerId.Player1);

            Assert.AreEqual(replayed.State.Get(PlayerId.Player2).Hand.Count, view.OpponentHandCount);
            CollectionAssert.AreEqual(replayed.State.Get(PlayerId.Player1).Hand, view.MyHand);
        }

        // =====================================================
        // IDEMPOTENCIA: turno esperado
        // =====================================================

        [Test]
        public void ExpectedTurn_Mismatch_IsStale_AndChangesNothing()
        {
            GameEngine engine = NewEngine();
            GameState state = GameEngineTests.NewGame(engine, PlayerId.Player1);
            GameEngineTests.Give(state, PlayerId.Player1, GameEngineTests.Grunt);

            ActionOutcome stale = engine.DeployTroop(
                state, PlayerId.Player1, GameEngineTests.Grunt, 0, 1, expectedTurnNumber: 99);

            Assert.AreEqual(ActionResult.StaleTurn, stale.Result);
            Assert.AreEqual(1, state.Get(PlayerId.Player1).Hand.Count);

            ActionOutcome ok = engine.DeployTroop(
                state, PlayerId.Player1, GameEngineTests.Grunt, 0, 1, expectedTurnNumber: state.TurnNumber);

            Assert.IsTrue(ok.Ok);
        }

        [Test]
        public void ExpectedTurn_StopsARetriedEndStep_WhenTheSamePlayerPlaysTwiceInARow()
        {
            // Al cambiar de ronda la prioridad se invierte: quien juega el último paso (4) del
            // Ocaso vuelve a jugar el paso 1 de la ronda siguiente. Un EndStep reenviado por un
            // fallo de red NO se rechazaría por "no es tu turno": saltaría su paso 1 sin querer.
            GameEngine engine = NewEngine();
            GameState state = GameEngineTests.NewGame(engine, PlayerId.Player1);

            for (int i = 0; i < 7; i++)
                engine.EndStep(state, state.CurrentPlayer);

            PlayerId last = state.CurrentPlayer;
            int turnSeen = state.TurnNumber;

            Assert.IsTrue(engine.EndStep(state, last, turnSeen).Ok);
            Assert.AreEqual(last, state.CurrentPlayer, "Mismo jugador otra vez, en la ronda 2.");

            ActionOutcome retry = engine.EndStep(state, last, turnSeen);

            Assert.AreEqual(ActionResult.StaleTurn, retry.Result);
            Assert.AreEqual(1, state.Step);
        }

        // =====================================================
        // EL MOTOR SIGUE SIENDO "PURO"
        // =====================================================

        [Test]
        public void RulesAssembly_DoesNotReferenceUnity()
        {
            var references = typeof(GameEngine).Assembly.GetReferencedAssemblies()
                .Select(a => a.Name)
                .ToList();

            Assert.IsFalse(references.Any(n => n.StartsWith("UnityEngine", StringComparison.Ordinal)));
            Assert.IsFalse(references.Any(n => n.StartsWith("Sirenix", StringComparison.Ordinal)));
        }
    }
}
