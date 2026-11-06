using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CardWars.Rules;
using NUnit.Framework;

namespace CardWars.CloudCode.Tests
{
    // Pruebas de la AUTORIDAD: lo que el servidor acepta, rechaza y muestra a cada jugador.
    // Corren con `dotnet test` sin Unity y sin desplegar nada.
    public class MatchServiceTests
    {
        private static int P => GameProtocol.Version;

        // =====================================================
        // CreateMatch / JoinMatch
        // =====================================================

        [Test]
        public async Task CreateMatch_IsIdempotent_SameRequestReturnsTheSameMatch()
        {
            var game = new Game();

            MatchResponse a = await game.Service.CreateMatchAsync("alice", "req-1", P);
            MatchResponse b = await game.Service.CreateMatchAsync("alice", "req-1", P);

            Assert.IsTrue(a.Ok);
            Assert.AreEqual(a.State.MatchId, b.State.MatchId);
            Assert.AreEqual(1, game.Store.Count);
            Assert.AreEqual("waiting", b.State.Status);
        }

        [Test]
        public async Task CreateMatch_RetryAfterTheRivalJoined_DoesNotResetTheMatch()
        {
            var game = await new Game().StartAsync();
            await game.MoveAsync(await game.CurrentAsync(), "end");

            await game.Service.CreateMatchAsync("alice", "req-1", P);

            Assert.AreEqual(1, game.Store.Peek(game.MatchId).Actions.Count);
            Assert.AreEqual(2, game.Store.Peek(game.MatchId).Players.Count);
        }

        [Test]
        public async Task CreateMatch_DifferentCallersWithTheSameRequestId_GetDifferentMatches()
        {
            var game = new Game();

            MatchResponse a = await game.Service.CreateMatchAsync("alice", "req-1", P);
            MatchResponse b = await game.Service.CreateMatchAsync("bob", "req-1", P);

            Assert.AreNotEqual(a.State.MatchId, b.State.MatchId);
        }

        [Test]
        public async Task CreateMatch_WithAnotherProtocolVersion_IsRejected()
        {
            var game = new Game();

            MatchResponse r = await game.Service.CreateMatchAsync("alice", "req-1", P + 1);

            Assert.AreEqual(ErrorCodes.ProtocolMismatch, r.ErrorCode);
            Assert.AreEqual(0, game.Store.Count);
        }

        [Test]
        public async Task CreateMatch_TheServerDrawsTheRandomness_AndTheClientNeverSeesIt()
        {
            var game = new Game(seed: 99);

            MatchResponse r = await game.Service.CreateMatchAsync("alice", "req-1", P);
            MatchRecord stored = game.Store.Peek(r.State.MatchId);

            Assert.IsNotEmpty(stored.Setup.Player1Deck);
            Assert.IsNotEmpty(stored.Setup.Player2Deck);
            Assert.IsNull(r.State.View, "Esperando rival: todavía no hay vista de partida.");
            StringAssert.DoesNotContain("Setup", JsonSerializer.Serialize(r));
        }

        [Test]
        public async Task JoinMatch_StartsTheMatch_AssignsSidesBySortedIds_AndNotifiesTheCreator()
        {
            var game = new Game();
            MatchResponse created = await game.Service.CreateMatchAsync("bob", "req-1", P);

            MatchResponse joined = await game.Service.JoinMatchAsync("alice", created.State.MatchId, P);

            Assert.IsTrue(joined.Ok);
            Assert.AreEqual("in_progress", joined.State.Status);
            Assert.AreEqual("Player1", joined.State.YourSide, "'alice' < 'bob': alice es Player1 aunque se uniera después.");
            CollectionAssert.AreEqual(new[] { "alice", "bob" }, game.Store.Peek(created.State.MatchId).Players);
            Assert.Contains((created.State.MatchId, "bob"), game.Notifier.Sent);
        }

        [Test]
        public async Task JoinMatch_ThirdPlayer_IsRejected_AndRejoiningIsHarmless()
        {
            var game = await new Game().StartAsync();

            Assert.AreEqual(
                ErrorCodes.MatchFull,
                (await game.Service.JoinMatchAsync("carol", game.MatchId, P)).ErrorCode);
            Assert.IsTrue((await game.Service.JoinMatchAsync("bob", game.MatchId, P)).Ok);
        }

        [Test]
        public async Task JoinMatch_UnknownOrMalformedCode_IsRejected()
        {
            var game = new Game();

            Assert.AreEqual(ErrorCodes.NotFound, (await game.Service.JoinMatchAsync("bob", "ABCDEFGH", P)).ErrorCode);
            Assert.AreEqual(ErrorCodes.InvalidRequest, (await game.Service.JoinMatchAsync("bob", "../x", P)).ErrorCode);
        }

        // =====================================================
        // PlayMove: quién, cuándo y qué
        // =====================================================

        [Test]
        public async Task PlayMove_ByAStranger_IsRejected_AndChangesNothing()
        {
            var game = await new Game().StartAsync();
            int before = game.Store.Peek(game.MatchId).Actions.Count;

            MatchResponse r = await game.Service.PlayMoveAsync("mallory", game.MatchId, new PlayMoveRequest
            {
                ProtocolVersion = P, Kind = "end", ExpectedTurnNumber = 1
            });

            Assert.AreEqual(ErrorCodes.NotAPlayer, r.ErrorCode);
            Assert.AreEqual(before, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task PlayMove_OutOfTurn_IsRejected_AndNothingIsStoredOrSent()
        {
            var game = await new Game().StartAsync();
            string waiting = Game.Other(await game.CurrentAsync());
            int sent = game.Notifier.Sent.Count;

            MatchResponse r = await game.MoveAsync(waiting, "end");

            Assert.AreEqual(ErrorCodes.NotYourTurn, r.ErrorCode);
            Assert.AreEqual(0, game.Store.Peek(game.MatchId).Actions.Count);
            Assert.AreEqual(sent, game.Notifier.Sent.Count);
        }

        [Test]
        public async Task PlayMove_DeployingACardYouDoNotHave_IsRejected()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();
            List<int> myHand = (await game.StateAsync(me)).State.View.MyHand;
            int notMine = Enumerable.Range(1, 6).First(id => !myHand.Contains(id));

            Assert.AreEqual(ErrorCodes.CardNotInHand, (await game.MoveAsync(me, "deploy", notMine)).ErrorCode);
            Assert.AreEqual(ErrorCodes.CardNotInHand, (await game.MoveAsync(me, "deploy", 999)).ErrorCode);
            Assert.AreEqual(0, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task PlayMove_ValidDeploy_IsStored_NotifiesTheRival_AndShowsTheNewEvents()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();
            string rival = Game.Other(me);
            int card = (await game.StateAsync(me)).State.View.MyHand[0];

            MatchResponse r = await game.MoveAsync(me, "deploy", card, lane: 1, slot: 1);

            Assert.IsTrue(r.Ok, r.ErrorCode);
            Assert.AreEqual(1, r.State.ActionCount);
            Assert.AreEqual(1, game.Store.Peek(game.MatchId).Actions.Count);
            Assert.Contains((game.MatchId, rival), game.Notifier.Sent);
            Assert.AreEqual(1, r.State.NewEvents.Count(e => e.Type == GameEventType.TroopDeployed));
            Assert.AreEqual(3, r.State.View.MyHand.Count);
        }

        [Test]
        public async Task PlayMove_ARetriedMessage_IsStale_AndIsNotAppliedTwice()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();
            MatchStateDto s = (await game.StateAsync(me)).State;
            var request = new PlayMoveRequest
            {
                ProtocolVersion = P, Kind = "end",
                ExpectedTurnNumber = s.View.TurnNumber, ActionsSeen = s.ActionCount
            };

            MatchResponse first = await game.Service.PlayMoveAsync(me, game.MatchId, request);
            MatchResponse retry = await game.Service.PlayMoveAsync(me, game.MatchId, request);

            Assert.IsTrue(first.Ok);
            Assert.AreEqual(ErrorCodes.StaleTurn, retry.ErrorCode);
            Assert.AreEqual(1, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task PlayMove_InvalidKind_IsRejected()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();

            MatchResponse r = await game.MoveAsync(me, "teleport");

            Assert.AreEqual(ErrorCodes.InvalidRequest, r.ErrorCode);
        }

        [Test]
        public async Task PlayMove_WhenSavingConflicts_ReturnsConflict_AndKeepsTheStoredMatch()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();
            game.Store.AlwaysConflict = true;

            MatchResponse r = await game.MoveAsync(me, "end");

            Assert.AreEqual(ErrorCodes.Conflict, r.ErrorCode);
            Assert.AreEqual(0, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task PlayMove_ADeadPushNeverFailsTheMove()
        {
            var game = await new Game().StartAsync();
            game.Notifier.Throws = true;

            MatchResponse r = await game.MoveAsync(await game.CurrentAsync(), "end");

            Assert.IsTrue(r.Ok);
            Assert.AreEqual(1, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task PlayMove_AlternatesTheTurnBetweenPlayers()
        {
            var game = await new Game().StartAsync();
            string first = await game.CurrentAsync();

            await game.MoveAsync(first, "end");

            Assert.AreEqual(Game.Other(first), await game.CurrentAsync());
        }

        // =====================================================
        // Información oculta: lo que SÍ sale del servidor
        // =====================================================

        [Test]
        public async Task State_ShowsMyHand_ButOnlyTheCountOfTheOpponentHand()
        {
            var game = await new Game().StartAsync();
            GameState inside = game.Peek();

            MatchStateDto alice = (await game.StateAsync(Game.Alice)).State;
            MatchStateDto bob = (await game.StateAsync(Game.Bob)).State;

            CollectionAssert.AreEqual(inside.Get(PlayerId.Player1).Hand, alice.View.MyHand);
            CollectionAssert.AreEqual(inside.Get(PlayerId.Player2).Hand, bob.View.MyHand);
            Assert.AreEqual(inside.Get(PlayerId.Player2).Hand.Count, alice.View.OpponentHandCount);
            Assert.AreEqual(inside.Get(PlayerId.Player2).Deck.Count, alice.View.OpponentDeckCount);
        }

        [Test]
        public async Task State_NeverContainsTheStoredRecord_TheSetupOrTheDecks()
        {
            var game = await new Game().StartAsync();
            await game.MoveAsync(await game.CurrentAsync(), "end");

            foreach (string who in new[] { Game.Alice, Game.Bob })
            {
                string json = JsonSerializer.Serialize(await game.StateAsync(who));

                foreach (string secret in new[] { "Setup", "Player1Deck", "Player2Deck", "Actions", "CreatedBy" })
                    StringAssert.DoesNotContain(secret, json);
            }
        }

        [Test]
        public async Task State_TheOpponentDrawIsHidden_ButMyOwnDrawIsShown()
        {
            var game = await new Game().StartAsync();

            // Cuatro pasos seguidos: al terminar el Alba se resuelve el combate y ambos roban.
            for (int i = 0; i < 4; i++)
                await game.MoveAsync(await game.CurrentAsync(), "end");

            MatchStateDto alice = (await game.StateAsync(Game.Alice, seen: 0)).State;
            List<GameEvent> draws = alice.NewEvents.Where(e => e.Type == GameEventType.CardDrawn).ToList();

            Assert.AreEqual(2, draws.Count);
            Assert.Greater(draws.Single(d => d.Player == PlayerId.Player1).CardId, 0);
            Assert.AreEqual(-1, draws.Single(d => d.Player == PlayerId.Player2).CardId);
        }

        [Test]
        public async Task State_ReturnsOnlyTheEventsThatTheClientHasNotSeen()
        {
            var game = await new Game().StartAsync();
            for (int i = 0; i < 3; i++)
                await game.MoveAsync(await game.CurrentAsync(), "end");

            MatchStateDto all = (await game.StateAsync(Game.Alice, seen: 0)).State;
            MatchStateDto none = (await game.StateAsync(Game.Alice, seen: all.ActionCount)).State;
            MatchStateDto last = (await game.StateAsync(Game.Alice, seen: all.ActionCount - 1)).State;

            Assert.AreEqual(3, all.ActionCount);
            Assert.AreEqual(0, none.NewEvents.Count);
            Assert.AreEqual(1, last.NewEvents.Count(e => e.Type == GameEventType.StepStarted));
        }

        [Test]
        public async Task State_ByAStranger_IsRejected()
        {
            var game = await new Game().StartAsync();

            Assert.AreEqual(ErrorCodes.NotAPlayer, (await game.StateAsync("mallory")).ErrorCode);
        }

        [Test]
        public async Task State_Reconnecting_RebuildsTheSameGameFromTheStoredRecord()
        {
            var game = await new Game().StartAsync();
            string me = await game.CurrentAsync();
            int card = (await game.StateAsync(me)).State.View.MyHand[0];
            await game.MoveAsync(me, "deploy", card, 0, 1);

            // Otro MatchService, sin memoria: solo ve lo guardado.
            var fresh = new MatchService(game.Store, new RecordingNotifier(), game.Catalogs, game.Clock);
            MatchStateDto again = (await fresh.GetMatchStateAsync(me, game.MatchId, 0, P)).State;

            Assert.AreEqual(1, again.ActionCount);
            Assert.IsNotNull(again.View.Lanes[0].Troops[(int)(again.YourSide == "Player1" ? PlayerId.Player1 : PlayerId.Player2)][0]);
        }

        // =====================================================
        // Tiempo y abandono (reloj del servidor)
        // =====================================================

        [Test]
        public async Task Timeout_BeforeTheLimit_NothingHappens()
        {
            var game = await new Game().StartAsync();
            game.Clock.AdvanceSeconds(170);

            MatchStateDto s = (await game.StateAsync(Game.Alice)).State;

            Assert.AreEqual("in_progress", s.Status);
            Assert.AreEqual(10, s.SecondsLeftInStep);
        }

        [Test]
        public async Task Timeout_ThePlayerWhoDidNotActInTime_Loses_WhenTheRivalAsks()
        {
            var game = await new Game().StartAsync();
            string slow = await game.CurrentAsync();
            string waiting = Game.Other(slow);
            game.Clock.AdvanceSeconds(181);

            MatchStateDto s = (await game.StateAsync(waiting)).State;

            Assert.AreEqual("finished", s.Status);
            Assert.AreEqual("won", s.Result);
            Assert.AreEqual("timeout", s.EndReason);
            Assert.AreEqual("lost", (await game.StateAsync(slow)).State.Result);
            Assert.Contains((game.MatchId, slow), game.Notifier.Sent);
        }

        [Test]
        public async Task Timeout_ALateMoveIsRejected_BecauseTheServerClockAlreadyDecidedTheMatch()
        {
            var game = await new Game().StartAsync();
            string slow = await game.CurrentAsync();
            MatchStateDto s = (await game.StateAsync(slow)).State;
            game.Clock.AdvanceSeconds(200);

            MatchResponse r = await game.Service.PlayMoveAsync(slow, game.MatchId, new PlayMoveRequest
            {
                ProtocolVersion = P, Kind = "end", ExpectedTurnNumber = s.View.TurnNumber
            });

            Assert.AreEqual(ErrorCodes.MatchOver, r.ErrorCode);
            Assert.AreEqual(0, game.Store.Peek(game.MatchId).Actions.Count);
        }

        [Test]
        public async Task EveryAcceptedMove_RestartsTheStepClock()
        {
            var game = await new Game().StartAsync();
            game.Clock.AdvanceSeconds(150);
            await game.MoveAsync(await game.CurrentAsync(), "end");
            game.Clock.AdvanceSeconds(150);

            Assert.AreEqual("in_progress", (await game.StateAsync(Game.Alice)).State.Status);
        }

        [Test]
        public async Task LeaveMatch_GivesTheWinToTheRival_AndLeavingTwiceIsRejected()
        {
            var game = await new Game().StartAsync();

            MatchResponse left = await game.Service.LeaveMatchAsync(Game.Bob, game.MatchId, P);

            Assert.AreEqual("lost", left.State.Result);
            Assert.AreEqual("forfeit", left.State.EndReason);
            Assert.AreEqual("won", (await game.StateAsync(Game.Alice)).State.Result);
            Assert.AreEqual(ErrorCodes.MatchOver, (await game.Service.LeaveMatchAsync(Game.Bob, game.MatchId, P)).ErrorCode);
            Assert.AreEqual(ErrorCodes.MatchOver, (await game.MoveAsync(Game.Alice, "end")).ErrorCode);
        }

        [Test]
        public async Task LeaveMatch_WhileWaitingForARival_CancelsTheMatch()
        {
            var game = new Game();
            MatchResponse created = await game.Service.CreateMatchAsync("alice", "req-1", P);

            MatchResponse left = await game.Service.LeaveMatchAsync("alice", created.State.MatchId, P);

            Assert.AreEqual("finished", left.State.Status);
            Assert.AreEqual("cancelled", left.State.EndReason);
            Assert.AreEqual(ErrorCodes.MatchOver, (await game.Service.JoinMatchAsync("bob", created.State.MatchId, P)).ErrorCode);
        }

        // =====================================================
        // Versiones y datos dañados
        // =====================================================

        [Test]
        public async Task Catalog_AMatchKeepsPlayingWithTheVersionItStartedWith()
        {
            var v1 = TestCatalogs.Variant("v1");
            var v2 = TestCatalogs.Variant("v2", c => c.Cards.ForEach(card => card.Attack += 5));
            var catalogs = new TestCatalogs(v1, v2) { CurrentVersion = "v1" };
            var game = await new Game(catalogs).StartAsync();

            catalogs.CurrentVersion = "v2";   // sale una versión nueva a mitad de partida

            Assert.AreEqual("v1", game.Store.Peek(game.MatchId).CatalogVersion);
            Assert.IsTrue((await game.MoveAsync(await game.CurrentAsync(), "end")).Ok);

            var next = new MatchService(game.Store, game.Notifier, catalogs, game.Clock, () => 5);
            MatchResponse created = await next.CreateMatchAsync("carol", "r", P);
            Assert.AreEqual("v2", game.Store.Peek(created.State.MatchId).CatalogVersion);
        }

        [Test]
        public async Task Catalog_AMissingVersion_IsReportedInsteadOfGuessing()
        {
            var game = await new Game().StartAsync();
            game.Store.Tamper(game.MatchId, r => r.CatalogVersion = "v0");

            Assert.AreEqual(ErrorCodes.CatalogUnavailable, (await game.StateAsync(Game.Alice)).ErrorCode);
        }

        [Test]
        public async Task CorruptRecord_IsReportedLoudly_NotSilentlyIgnored()
        {
            var game = await new Game().StartAsync();
            game.Store.Tamper(game.MatchId, r => r.Actions.Add(GameAction.Deploy(PlayerId.Player1, 999, 0, 1)));

            Assert.AreEqual(ErrorCodes.CorruptMatch, (await game.StateAsync(Game.Alice)).ErrorCode);
        }

        [Test]
        public void GetGameConfig_GivesTheClientTheSameCatalogAsTheServer_WithoutSecrets()
        {
            var service = new MatchService(null, null, new EmbeddedCatalogProvider());

            GameConfigDto config = service.GetGameConfig();

            Assert.AreEqual("v1", config.CatalogVersion);
            Assert.AreEqual(GameProtocol.Version, config.ProtocolVersion);
            Assert.IsNotEmpty(config.Cards);
            StringAssert.DoesNotContain("Setup", JsonSerializer.Serialize(config));
        }

        // =====================================================
        // Una partida completa, de principio a fin
        // =====================================================

        [Test]
        public async Task FullMatch_PlayedOnlyThroughTheService_EndsWithOneWinnerAndOneLoser()
        {
            var catalogs = new TestCatalogs(TestCatalogs.Variant("v1", c =>
            {
                c.PortalMaxHealth = 12;
                c.Cards.ForEach(card => card.Attack = 6);
            }));
            var game = await new Game(catalogs, seed: 7).StartAsync();

            for (int i = 0; i < 800; i++)
            {
                string me = await game.CurrentAsync();
                MatchStateDto s = (await game.StateAsync(me)).State;

                if (s.Status == "finished")
                    break;

                bool deployed = false;

                if (s.View.MyHand.Count > 0 && s.View.CurrentPlayer == (me == Game.Alice ? PlayerId.Player1 : PlayerId.Player2)
                    && (s.View.Step == 1 || s.View.Step == 2))
                {
                    for (int lane = 0; lane < 3 && !deployed; lane++)
                        deployed = (await game.MoveAsync(me, "deploy", s.View.MyHand[0], lane, 1)).Ok;
                }

                if (!deployed)
                    await game.MoveAsync(me, "end");
            }

            MatchStateDto alice = (await game.StateAsync(Game.Alice)).State;
            MatchStateDto bob = (await game.StateAsync(Game.Bob)).State;

            Assert.AreEqual("finished", alice.Status);
            Assert.AreEqual("portal", alice.EndReason);
            CollectionAssert.AreEquivalent(new[] { "won", "lost" }, new[] { alice.Result, bob.Result });
        }
    }
}
