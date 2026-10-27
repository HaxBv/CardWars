using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace CardWars.Rules.Tests
{
    // Pruebas EditMode del motor de reglas. No necesitan escena ni GameObjects:
    // si el motor corre aquí, también corre en un servidor.
    public class GameEngineTests
    {
        private const int Grunt = 1;   // ataque 3, vida 5
        private const int Tank = 2;    // ataque 1, vida 10
        private const int Team = 3;    // ataque 2, vida 4, con Team
        private const int Glass = 4;   // ataque 10, vida 1
        private const int Climber = 5; // ataque 2, vida 4, solo Highground

        private static List<CardDefinition> Cards()
        {
            return new List<CardDefinition>
            {
                new CardDefinition(Grunt, "Grunt", 3, 5, maxDeckCopies: 4),
                new CardDefinition(Tank, "Tank", 1, 10, maxDeckCopies: 3),
                new CardDefinition(Team, "Team", 2, 4, maxDeckCopies: 3, hasTeam: true),
                new CardDefinition(Glass, "Glass", 10, 1, maxDeckCopies: 2),
                new CardDefinition(Climber, "Climber", 2, 4, maxDeckCopies: 2, highground: true)
            };
        }

        private static GameEngine NewEngine(GameConfig config = null)
        {
            return new GameEngine(Cards(), config);
        }

        // Partida con la prioridad fijada y manos vacías, para armar cada escenario a mano.
        private static GameState NewGame(
            GameEngine engine, PlayerId priority = PlayerId.Player1, int seed = 7)
        {
            GameState state = engine.StartGame(seed);
            state.FirstRoundPriority = priority;
            state.Priority = priority;
            state.Get(PlayerId.Player1).Hand.Clear();
            state.Get(PlayerId.Player2).Hand.Clear();
            return state;
        }

        private static void Give(GameState state, PlayerId player, params int[] cardIds)
        {
            state.Get(player).Hand.AddRange(cardIds);
        }

        // Pone una tropa saltándose las reglas de despliegue (arma el escenario del test).
        private static void Place(
            GameState state, int lane, PlayerId player, int slot, int cardId)
        {
            CardDefinition card = Cards().First(c => c.Id == cardId);

            state.Lanes[lane].SetTroop(player, slot, new TroopState
            {
                CardId = card.Id,
                Attack = card.Attack,
                Health = card.Health,
                MaxHealth = card.Health
            });
        }

        // Termina los 4 primeros pasos; el último EndStep dispara el combate.
        private static ActionOutcome PlayToCombat(GameEngine engine, GameState state)
        {
            ActionOutcome last = null;

            for (int i = 0; i < 4; i++)
                last = engine.EndStep(state, state.CurrentPlayer);

            return last;
        }

        private static int Count(IEnumerable<GameEvent> events, GameEventType type)
        {
            return events.Count(e => e.Type == type);
        }

        // =====================================================
        // INICIO, AZAR E INFORMACIÓN OCULTA
        // =====================================================

        [Test]
        public void StartGame_SameSeed_ProducesSameGame()
        {
            GameEngine engine = NewEngine();

            GameState a = engine.StartGame(123);
            GameState b = engine.StartGame(123);

            Assert.AreEqual(a.Priority, b.Priority);
            Assert.AreEqual(a.Get(PlayerId.Player1).Deck, b.Get(PlayerId.Player1).Deck);
            Assert.AreEqual(a.Get(PlayerId.Player2).Hand, b.Get(PlayerId.Player2).Hand);
        }

        [Test]
        public void StartGame_DealsInitialHands_AndBuildsDecksFromCopies()
        {
            GameEngine engine = NewEngine();
            GameState state = engine.StartGame(1);
            int totalCopies = Cards().Sum(c => c.MaxDeckCopies);

            foreach (PlayerId player in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                PlayerState ps = state.Get(player);
                Assert.AreEqual(4, ps.Hand.Count);
                Assert.AreEqual(totalCopies - 4, ps.Deck.Count);
                Assert.AreEqual(50, ps.Portal.Health);
                Assert.AreEqual(3, ps.Portal.RemainingShields);
            }
        }

        [Test]
        public void StartGame_DecksAreIndependent()
        {
            GameEngine engine = NewEngine();
            GameState state = engine.StartGame(99);

            CollectionAssert.AreNotEqual(
                state.Get(PlayerId.Player1).Deck,
                state.Get(PlayerId.Player2).Deck);
        }

        [Test]
        public void View_ShowsOwnHand_ButOnlyTheCountOfTheOpponentHand()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt, Tank);
            Give(state, PlayerId.Player2, Glass, Team, Team);

            PlayerView view = engine.CreateView(state, PlayerId.Player1);

            CollectionAssert.AreEqual(new[] { Grunt, Tank }, view.MyHand);
            Assert.AreEqual(3, view.OpponentHandCount);
            Assert.AreEqual(state.Get(PlayerId.Player2).Deck.Count, view.OpponentDeckCount);
        }

        [Test]
        public void View_HasNoFieldThatCouldCarryTheOpponentCards()
        {
            // La única lista de ids de carta de la vista es la mano propia.
            var listFields = typeof(PlayerView).GetFields()
                .Where(f => f.FieldType == typeof(List<int>))
                .Select(f => f.Name)
                .ToList();

            CollectionAssert.AreEqual(new[] { "MyHand" }, listFields);
        }

        [Test]
        public void View_IsACopy_ChangingItDoesNotChangeTheGame()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt);

            PlayerView view = engine.CreateView(state, PlayerId.Player1);
            view.MyHand.Add(Glass);
            view.MyPortal.Health = 1;

            Assert.AreEqual(1, state.Get(PlayerId.Player1).Hand.Count);
            Assert.AreEqual(50, state.Get(PlayerId.Player1).Portal.Health);
        }

        [Test]
        public void Redact_HidesWhichCardTheOpponentDrew()
        {
            var events = new List<GameEvent>
            {
                new GameEvent { Type = GameEventType.CardDrawn, Player = PlayerId.Player1, CardId = Glass },
                new GameEvent { Type = GameEventType.CardDrawn, Player = PlayerId.Player2, CardId = Tank }
            };

            List<GameEvent> forPlayer1 = GameEngine.RedactFor(events, PlayerId.Player1);

            Assert.AreEqual(Glass, forPlayer1[0].CardId);
            Assert.AreEqual(-1, forPlayer1[1].CardId);
            Assert.AreEqual(Tank, events[1].CardId, "El original no debe modificarse.");
        }

        // =====================================================
        // TURNOS Y PASOS
        // =====================================================

        [Test]
        public void Steps_AlternatePlayers_StartingWithPriority()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player2);

            var order = new List<PlayerId>();

            for (int i = 0; i < 4; i++)
            {
                order.Add(state.CurrentPlayer);
                engine.EndStep(state, state.CurrentPlayer);
            }

            CollectionAssert.AreEqual(
                new[] { PlayerId.Player2, PlayerId.Player1, PlayerId.Player2, PlayerId.Player1 },
                order);
        }

        [Test]
        public void EndStep_OutOfTurn_IsRejected_AndChangesNothing()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);

            ActionOutcome outcome = engine.EndStep(state, PlayerId.Player2);

            Assert.AreEqual(ActionResult.NotYourTurn, outcome.Result);
            Assert.AreEqual(1, state.Step);
            Assert.AreEqual(1, state.TurnNumber);
            Assert.AreEqual(0, outcome.Events.Count);
        }

        [Test]
        public void TurnNumber_GrowsWithEveryStep()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);

            engine.EndStep(state, state.CurrentPlayer);
            Assert.AreEqual(2, state.TurnNumber);

            engine.EndStep(state, state.CurrentPlayer);
            Assert.AreEqual(3, state.TurnNumber);
        }

        [Test]
        public void FullRound_AlbaThenOcaso_ThenPriorityFlips()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);

            PlayToCombat(engine, state);

            Assert.AreEqual(TimeOfDayKind.Ocaso, state.TimeOfDay);
            Assert.AreEqual(1, state.Round);
            Assert.AreEqual(1, state.Step);
            Assert.AreEqual(PlayerId.Player1, state.Priority, "La prioridad no cambia entre Alba y Ocaso.");

            PlayToCombat(engine, state);

            Assert.AreEqual(TimeOfDayKind.Alba, state.TimeOfDay);
            Assert.AreEqual(2, state.Round);
            Assert.AreEqual(PlayerId.Player2, state.Priority);

            PlayToCombat(engine, state);
            PlayToCombat(engine, state);

            Assert.AreEqual(3, state.Round);
            Assert.AreEqual(PlayerId.Player1, state.Priority);
        }

        [Test]
        public void EndOfTimeOfDay_BothPlayersDrawOneCard()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);

            ActionOutcome outcome = PlayToCombat(engine, state);

            Assert.AreEqual(1, state.Get(PlayerId.Player1).Hand.Count);
            Assert.AreEqual(1, state.Get(PlayerId.Player2).Hand.Count);
            Assert.AreEqual(2, Count(outcome.Events, GameEventType.CardDrawn));
        }

        [Test]
        public void Draw_DoesNotExceedMaxHandSize()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt, Grunt, Grunt, Grunt, Grunt, Grunt, Grunt, Grunt);

            PlayToCombat(engine, state);

            Assert.AreEqual(8, state.Get(PlayerId.Player1).Hand.Count);
            Assert.AreEqual(1, state.Get(PlayerId.Player2).Hand.Count);
        }

        [Test]
        public void Draw_WithEmptyDeck_DoesNothing()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            state.Get(PlayerId.Player1).Deck.Clear();

            Assert.DoesNotThrow(() => PlayToCombat(engine, state));
            Assert.AreEqual(0, state.Get(PlayerId.Player1).Hand.Count);
        }

        // =====================================================
        // DESPLIEGUE
        // =====================================================

        [Test]
        public void Deploy_OutOfTurn_IsRejected_AndKeepsTheCard()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);
            Give(state, PlayerId.Player2, Grunt);

            ActionOutcome outcome = engine.DeployTroop(state, PlayerId.Player2, Grunt, 0, 1);

            Assert.AreEqual(ActionResult.NotYourTurn, outcome.Result);
            Assert.AreEqual(1, state.Get(PlayerId.Player2).Hand.Count);
            Assert.IsNull(state.Lanes[0].GetTroop(PlayerId.Player2, 1));
        }

        [Test]
        public void Deploy_CardNotInHand_IsRejected()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player2, Glass);   // el rival tiene una carta que yo no

            Assert.AreEqual(
                ActionResult.CardNotInHand,
                engine.DeployTroop(state, PlayerId.Player1, Glass, 0, 1).Result);
            Assert.AreEqual(
                ActionResult.CardNotInHand,
                engine.DeployTroop(state, PlayerId.Player1, 999, 0, 1).Result);
        }

        [Test]
        public void Deploy_InSpellStep_IsWrongStep()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt);

            engine.EndStep(state, PlayerId.Player1);
            engine.EndStep(state, PlayerId.Player2);

            Assert.AreEqual(3, state.Step);
            Assert.AreEqual(
                ActionResult.WrongStep,
                engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 1).Result);
        }

        [Test]
        public void Deploy_UsesTheCallerSide_NotAnyOtherSide()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);
            Give(state, PlayerId.Player1, Grunt);

            ActionOutcome outcome = engine.DeployTroop(state, PlayerId.Player1, Grunt, 1, 1);

            Assert.IsTrue(outcome.Ok);
            Assert.IsNotNull(state.Lanes[1].GetTroop(PlayerId.Player1, 1));
            Assert.IsNull(state.Lanes[1].GetTroop(PlayerId.Player2, 1));
            Assert.AreEqual(0, state.Get(PlayerId.Player1).Hand.Count);
            Assert.AreEqual(1, Count(outcome.Events, GameEventType.TroopDeployed));
        }

        [Test]
        public void Deploy_InvalidLaneOrSlot_IsRejected()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt);

            Assert.AreEqual(ActionResult.InvalidLane, engine.DeployTroop(state, PlayerId.Player1, Grunt, -1, 1).Result);
            Assert.AreEqual(ActionResult.InvalidLane, engine.DeployTroop(state, PlayerId.Player1, Grunt, 3, 1).Result);
            Assert.AreEqual(ActionResult.InvalidSlot, engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 0).Result);
            Assert.AreEqual(ActionResult.InvalidSlot, engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 3).Result);
            Assert.AreEqual(1, state.Get(PlayerId.Player1).Hand.Count);
        }

        [Test]
        public void Deploy_OnEmptyLane_OnlySlot1IsAllowed()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt);

            Assert.AreEqual(
                ActionResult.InvalidSlot,
                engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 2).Result);
        }

        [Test]
        public void Deploy_SecondTroop_NeedsTeamOnEitherCard()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt, Tank, Team);

            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 1).Ok);

            // Ni Grunt ni Tank tienen Team.
            Assert.AreEqual(
                ActionResult.CannotShareLane,
                engine.DeployTroop(state, PlayerId.Player1, Tank, 0, 2).Result);

            // La tropa que entra tiene Team.
            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Team, 0, 2).Ok);
        }

        [Test]
        public void Deploy_SecondTroop_AllowedWhenTheOneInTheLaneHasTeam()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Team, Grunt);

            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Team, 0, 1).Ok);
            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 2).Ok);
        }

        [Test]
        public void Deploy_DroppingOnSlot1_PushesTheExistingTroopToSlot2()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Team, Grunt);

            engine.DeployTroop(state, PlayerId.Player1, Team, 0, 1);
            ActionOutcome outcome = engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 1);

            Assert.IsTrue(outcome.Ok);
            Assert.AreEqual(Grunt, state.Lanes[0].GetTroop(PlayerId.Player1, 1).CardId);
            Assert.AreEqual(Team, state.Lanes[0].GetTroop(PlayerId.Player1, 2).CardId);
            Assert.AreEqual(1, Count(outcome.Events, GameEventType.TroopMoved));
        }

        [Test]
        public void Deploy_ThirdTroop_LaneIsFull()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Team, Team, Team);

            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Team, 0, 1).Ok);
            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Team, 0, 2).Ok);
            Assert.AreEqual(
                ActionResult.LaneFull,
                engine.DeployTroop(state, PlayerId.Player1, Team, 0, 1).Result);
            Assert.AreEqual(1, state.Get(PlayerId.Player1).Hand.Count);
        }

        [Test]
        public void Deploy_Terrain_RequiresThePassive()
        {
            var config = new GameConfig
            {
                LaneTerrains = new List<LaneTerrainKind>
                {
                    LaneTerrainKind.Land,
                    LaneTerrainKind.Highground,
                    LaneTerrainKind.Aquatic
                }
            };
            GameEngine engine = NewEngine(config);
            GameState state = NewGame(engine);
            Give(state, PlayerId.Player1, Grunt, Climber, Climber);

            Assert.AreEqual(
                ActionResult.TerrainNotAllowed,
                engine.DeployTroop(state, PlayerId.Player1, Grunt, 1, 1).Result);
            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Climber, 1, 1).Ok);
            Assert.AreEqual(
                ActionResult.TerrainNotAllowed,
                engine.DeployTroop(state, PlayerId.Player1, Climber, 2, 1).Result);
            Assert.IsTrue(engine.DeployTroop(state, PlayerId.Player1, Grunt, 0, 1).Ok);
        }

        [Test]
        public void Lane_Compact_MovesTheRearTroopToSlot1()
        {
            var lane = new LaneState(LaneTerrainKind.Land);
            lane.SetTroop(PlayerId.Player1, 2, new TroopState { CardId = Grunt, Health = 1 });

            lane.Compact(PlayerId.Player1);

            Assert.AreEqual(Grunt, lane.GetTroop(PlayerId.Player1, 1).CardId);
            Assert.IsNull(lane.GetTroop(PlayerId.Player1, 2));
        }

        // =====================================================
        // COMBATE
        // =====================================================

        [Test]
        public void Combat_WithNoEnemyTroops_HitsThePortal()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Place(state, 0, PlayerId.Player1, 1, Grunt);

            ActionOutcome outcome = PlayToCombat(engine, state);

            Assert.AreEqual(47, state.Get(PlayerId.Player2).Portal.Health);
            Assert.AreEqual(3, state.Get(PlayerId.Player2).Portal.RemainingShields);
            Assert.AreEqual(50, state.Get(PlayerId.Player1).Portal.Health);

            GameEvent attack = outcome.Events.First(e => e.Type == GameEventType.Attack);
            Assert.AreEqual(-1, attack.TargetSlot);
            Assert.AreEqual(3, attack.Amount);
        }

        [Test]
        public void Combat_TargetsTheForwardSlot2First()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Place(state, 0, PlayerId.Player1, 1, Grunt);
            Place(state, 0, PlayerId.Player2, 1, Tank);
            Place(state, 0, PlayerId.Player2, 2, Grunt);

            PlayToCombat(engine, state);

            Assert.AreEqual(2, state.Lanes[0].GetTroop(PlayerId.Player2, 2).Health);   // 5 - 3
            Assert.AreEqual(10, state.Lanes[0].GetTroop(PlayerId.Player2, 1).Health);
            Assert.AreEqual(50, state.Get(PlayerId.Player2).Portal.Health);
        }

        [Test]
        public void Combat_TroopThatDiesInTheLane_StillStrikesBack()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);
            Place(state, 0, PlayerId.Player1, 1, Glass);
            Place(state, 0, PlayerId.Player2, 1, Glass);

            ActionOutcome outcome = PlayToCombat(engine, state);

            Assert.IsNull(state.Lanes[0].GetTroop(PlayerId.Player1, 1));
            Assert.IsNull(state.Lanes[0].GetTroop(PlayerId.Player2, 1));
            Assert.AreEqual(2, Count(outcome.Events, GameEventType.TroopDied));
            Assert.AreEqual(2, Count(outcome.Events, GameEventType.Attack));
        }

        [Test]
        public void Combat_PlannedTargets_AreNotReassignedWhenTheTargetDies()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Place(state, 0, PlayerId.Player1, 1, Grunt);
            Place(state, 0, PlayerId.Player1, 2, Grunt);
            Place(state, 0, PlayerId.Player2, 1, Glass);   // 1 de vida

            PlayToCombat(engine, state);

            // Los dos atacantes apuntaban a Glass: el segundo golpe no "salta" al portal.
            Assert.AreEqual(50, state.Get(PlayerId.Player2).Portal.Health);
            Assert.IsNull(state.Lanes[0].GetTroop(PlayerId.Player2, 1));
        }

        [Test]
        public void Combat_ResolvesLaneByLane_AndOnlyTheSideWithPriorityStrikesFirst()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player2);
            Place(state, 0, PlayerId.Player1, 1, Glass);
            Place(state, 0, PlayerId.Player2, 1, Glass);
            Place(state, 1, PlayerId.Player1, 1, Grunt);

            ActionOutcome outcome = PlayToCombat(engine, state);
            List<GameEvent> attacks = outcome.Events.Where(e => e.Type == GameEventType.Attack).ToList();

            Assert.AreEqual(PlayerId.Player2, attacks[0].Player, "Ataca primero quien tiene la prioridad.");
            Assert.AreEqual(0, attacks[0].Lane);
            Assert.AreEqual(1, attacks.Last().Lane, "El carril 1 se resuelve después del 0.");
        }

        [Test]
        public void Combat_DeadTroopsAreRemoved_AfterTheLane()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            Place(state, 0, PlayerId.Player1, 1, Glass);
            Place(state, 0, PlayerId.Player2, 1, Tank);
            Place(state, 0, PlayerId.Player2, 2, Glass);

            ActionOutcome outcome = PlayToCombat(engine, state);

            Assert.IsNull(state.Lanes[0].GetTroop(PlayerId.Player2, 2));
            Assert.IsNotNull(state.Lanes[0].GetTroop(PlayerId.Player2, 1));
            GameEvent died = outcome.Events.Single(e => e.Type == GameEventType.TroopDied && e.Player == PlayerId.Player2);
            Assert.AreEqual(Glass, died.CardId);
        }

        // =====================================================
        // PORTALES Y VICTORIA
        // =====================================================

        [Test]
        public void Portal_ShieldThresholds_With50Hp_Are40_30_20()
        {
            var portal = new PortalState { MaxHealth = 50, Health = 50, RemainingShields = 3 };
            Assert.AreEqual(40, GameEngine.NextShieldThreshold(portal));

            portal.RemainingShields = 2;
            Assert.AreEqual(30, GameEngine.NextShieldThreshold(portal));

            portal.RemainingShields = 1;
            Assert.AreEqual(20, GameEngine.NextShieldThreshold(portal));

            portal.RemainingShields = 0;
            Assert.AreEqual(-1, GameEngine.NextShieldThreshold(portal));
        }

        [Test]
        public void Portal_BigHit_BreaksOnlyOneShield_AndStopsAtTheThreshold()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            state.Get(PlayerId.Player2).Portal.Health = 41;
            Place(state, 0, PlayerId.Player1, 1, Glass);   // 10 de daño

            ActionOutcome outcome = PlayToCombat(engine, state);
            PortalState portal = state.Get(PlayerId.Player2).Portal;

            Assert.AreEqual(40, portal.Health);
            Assert.AreEqual(2, portal.RemainingShields);
            Assert.AreEqual(1, Count(outcome.Events, GameEventType.ShieldBroken));
            Assert.AreEqual(1, outcome.Events.Single(e => e.Type == GameEventType.PortalDamaged).Amount);
        }

        [Test]
        public void Portal_WithoutShields_TakesFullDamage()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine);
            PortalState portal = state.Get(PlayerId.Player2).Portal;
            portal.RemainingShields = 0;
            portal.Health = 30;
            Place(state, 0, PlayerId.Player1, 1, Glass);

            PlayToCombat(engine, state);

            Assert.AreEqual(20, portal.Health);
        }

        [Test]
        public void Portal_Destroyed_EndsTheGame_AndBlocksFurtherActions()
        {
            GameEngine engine = NewEngine();
            GameState state = NewGame(engine, PlayerId.Player1);
            PortalState portal = state.Get(PlayerId.Player2).Portal;
            portal.RemainingShields = 0;
            portal.Health = 3;
            Place(state, 0, PlayerId.Player1, 1, Grunt);
            Give(state, PlayerId.Player1, Grunt);

            ActionOutcome outcome = PlayToCombat(engine, state);

            Assert.IsTrue(state.IsOver);
            Assert.AreEqual(PlayerId.Player1, state.Winner);
            Assert.AreEqual(1, Count(outcome.Events, GameEventType.GameWon));
            Assert.AreEqual(ActionResult.GameOver, engine.EndStep(state, state.CurrentPlayer).Result);
            Assert.AreEqual(
                ActionResult.GameOver,
                engine.DeployTroop(state, state.CurrentPlayer, Grunt, 1, 1).Result);
        }

        // =====================================================
        // PRUEBA DE ESTRÉS: jugadas válidas, inválidas y fuera de turno
        // =====================================================

        [Test]
        public void Fuzz_RandomPlayers_NeverBreakInvariants()
        {
            var config = new GameConfig { PortalMaxHealth = 12 };
            GameEngine engine = NewEngine(config);
            int finished = 0;

            for (int seed = 1; seed <= 60; seed++)
            {
                GameState state = engine.StartGame(seed);
                var rng = new Random(seed * 31);

                for (int i = 0; i < 600 && !state.IsOver; i++)
                {
                    PlayerId actor = rng.Next(8) == 0
                        ? GameEngine.Opponent(state.CurrentPlayer)   // a veces, fuera de turno
                        : state.CurrentPlayer;

                    PlayerState ps = state.Get(actor);
                    int handBefore = ps.Hand.Count;

                    if (rng.Next(3) != 0 && ps.Hand.Count > 0)
                    {
                        int cardId = rng.Next(10) == 0 ? 999 : ps.Hand[rng.Next(ps.Hand.Count)];
                        ActionOutcome outcome =
                            engine.DeployTroop(state, actor, cardId, rng.Next(-1, 4), rng.Next(0, 4));

                        Assert.AreEqual(outcome.Ok ? handBefore - 1 : handBefore, ps.Hand.Count);
                    }
                    else
                    {
                        engine.EndStep(state, actor);
                    }

                    AssertInvariants(state, engine.Config);
                }

                if (state.IsOver)
                    finished++;
            }

            Assert.Greater(finished, 0, "Con portales de 12 de vida, alguna partida debería terminar.");
        }

        private static void AssertInvariants(GameState state, GameConfig config)
        {
            foreach (PlayerId player in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                PlayerState ps = state.Get(player);

                Assert.LessOrEqual(ps.Hand.Count, config.MaxHandSize);
                Assert.GreaterOrEqual(ps.Portal.Health, 0);
                Assert.LessOrEqual(ps.Portal.Health, ps.Portal.MaxHealth);
                Assert.GreaterOrEqual(ps.Portal.RemainingShields, 0);

                foreach (LaneState lane in state.Lanes)
                {
                    Assert.LessOrEqual(lane.TroopCount(player), LaneState.SlotsPerSide);

                    for (int slot = 1; slot <= LaneState.SlotsPerSide; slot++)
                    {
                        TroopState troop = lane.GetTroop(player, slot);

                        if (troop == null)
                            continue;

                        Assert.GreaterOrEqual(troop.Health, 0);
                        Assert.LessOrEqual(troop.Health, troop.MaxHealth);
                    }
                }
            }

            bool p1Destroyed = state.Get(PlayerId.Player1).Portal.IsDestroyed;
            bool p2Destroyed = state.Get(PlayerId.Player2).Portal.IsDestroyed;

            Assert.IsFalse(p1Destroyed && p2Destroyed, "Solo puede caer un portal: la partida termina al primero.");
            Assert.AreEqual(p1Destroyed || p2Destroyed, state.IsOver);
        }
    }
}
