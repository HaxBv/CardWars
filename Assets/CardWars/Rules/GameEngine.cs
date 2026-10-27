using System;
using System.Collections.Generic;

namespace CardWars.Rules
{
    // Reglas de CardWars sin Unity: el cliente PIDE (DeployTroop, EndStep) y el motor DECIDE.
    // Cada acción devuelve un resultado y la lista de eventos que ocurrieron.
    //
    // Es un port de las reglas que hoy viven repartidas en PhasesSystem (pasos y prioridad),
    // LandLane (despliegue), CombatManager (combate), Portals (escudos) y DeckHandManager
    // (mazo y mano). Donde el juego no define una regla (por ejemplo, la victoria) se anota.
    public sealed partial class GameEngine
    {
        private readonly Dictionary<int, CardDefinition> catalog =
            new Dictionary<int, CardDefinition>();

        // Valor de "expectedTurnNumber" que significa "no comprobar" (juego local).
        public const int NoExpectedTurn = -1;

        public GameConfig Config { get; }

        public GameEngine(IEnumerable<CardDefinition> cards, GameConfig config = null)
        {
            Config = config ?? new GameConfig();

            foreach (CardDefinition card in cards)
                catalog[card.Id] = card;
        }

        public static PlayerId Opponent(PlayerId player)
        {
            return player == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
        }

        // =====================================================
        // START
        // =====================================================

        // TODO el azar de la partida (barajar y primer jugador) se sortea AQUÍ y queda escrito
        // en un GameSetup. Después de esto el motor no vuelve a usar azar, así que una partida
        // es una función pura de (setup + lista de acciones) y se puede reproducir (Replay)
        // aunque cambie la versión de .NET o de Random. En online el setup lo crea y lo guarda
        // el servidor, y NUNCA se envía al cliente (contiene el orden de los mazos).
        public GameSetup CreateSetup(int seed)
        {
            var rng = new Random(seed);
            var setup = new GameSetup();

            BuildDeck(setup.Player1Deck, rng);
            BuildDeck(setup.Player2Deck, rng);

            // PhasesSystem.Start: el jugador inicial se decide al azar.
            setup.FirstPlayer = rng.Next(2) == 0 ? PlayerId.Player1 : PlayerId.Player2;
            return setup;
        }

        // Atajo para juego local y pruebas: sortea el setup con la semilla y empieza.
        public GameState StartGame(int seed)
        {
            return StartGame(CreateSetup(seed));
        }

        public GameState StartGame(GameSetup setup)
        {
            var state = new GameState();

            foreach (LaneTerrainKind terrain in Config.LaneTerrains)
                state.Lanes.Add(new LaneState(terrain));

            state.Get(PlayerId.Player1).Deck.AddRange(setup.Player1Deck);
            state.Get(PlayerId.Player2).Deck.AddRange(setup.Player2Deck);

            foreach (PlayerId player in new[] { PlayerId.Player1, PlayerId.Player2 })
            {
                PortalState portal = state.Get(player).Portal;
                portal.MaxHealth = Config.PortalMaxHealth;
                portal.Health = Config.PortalMaxHealth;
                portal.RemainingShields = Config.PortalShields;
            }

            state.FirstRoundPriority = setup.FirstPlayer;
            state.Priority = setup.FirstPlayer;

            var ignored = new List<GameEvent>();

            for (int i = 0; i < Config.InitialHandSize; i++)
            {
                Draw(state, PlayerId.Player1, ignored);
                Draw(state, PlayerId.Player2, ignored);
            }

            return state;
        }

        // DeckHandManager.CreateDeck: MaxDeckCopies copias de cada carta, barajadas.
        private void BuildDeck(List<int> deck, Random rng)
        {
            var ids = new List<int>(catalog.Keys);
            ids.Sort();

            foreach (int id in ids)
            {
                for (int i = 0; i < catalog[id].MaxDeckCopies; i++)
                    deck.Add(id);
            }

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int tmp = deck[i];
                deck[i] = deck[j];
                deck[j] = tmp;
            }
        }

        // DeckHandManager.DrawCard: no roba si el mazo está vacío o la mano está llena.
        private void Draw(GameState state, PlayerId player, List<GameEvent> events)
        {
            PlayerState ps = state.Get(player);

            if (ps.Deck.Count == 0 || ps.Hand.Count >= Config.MaxHandSize)
                return;

            int last = ps.Deck.Count - 1;
            int cardId = ps.Deck[last];
            ps.Deck.RemoveAt(last);
            ps.Hand.Add(cardId);

            events.Add(new GameEvent
            {
                Type = GameEventType.CardDrawn,
                Player = player,
                CardId = cardId
            });
        }

        // =====================================================
        // DEPLOY
        // =====================================================

        // slot: 1 o 2, el espacio donde el jugador suelta la carta (como en LandLane.DeployTroop).
        // Carril vacío: solo el 1. Con una tropa: hace falta Team en alguna de las dos; soltar en
        // el 1 empuja a la que estaba al 2, y soltar en el 2 la deja detrás.
        //
        // expectedTurnNumber: el TurnNumber que el cliente cree vigente. Si no coincide, la acción
        // es de un turno que ya pasó (un reintento de red, por ejemplo) y se rechaza con StaleTurn.
        // Importa porque a veces el mismo jugador juega dos pasos seguidos (al cambiar de ronda).
        public ActionOutcome DeployTroop(
            GameState state, PlayerId player, int cardId, int laneIndex, int slot,
            int expectedTurnNumber = NoExpectedTurn)
        {
            if (state.IsOver)
                return new ActionOutcome(ActionResult.GameOver);

            if (expectedTurnNumber != NoExpectedTurn && expectedTurnNumber != state.TurnNumber)
                return new ActionOutcome(ActionResult.StaleTurn);

            if (state.CurrentPlayer != player)
                return new ActionOutcome(ActionResult.NotYourTurn);

            // Los pasos 1 y 2 son de tropas; los 3 y 4 (hechizos y dominios) aún no están en el motor.
            if (state.Step != 1 && state.Step != 2)
                return new ActionOutcome(ActionResult.WrongStep);

            PlayerState ps = state.Get(player);

            if (!ps.Hand.Contains(cardId) || !catalog.TryGetValue(cardId, out CardDefinition card))
                return new ActionOutcome(ActionResult.CardNotInHand);

            if (laneIndex < 0 || laneIndex >= state.Lanes.Count)
                return new ActionOutcome(ActionResult.InvalidLane);

            if (slot < 1 || slot > LaneState.SlotsPerSide)
                return new ActionOutcome(ActionResult.InvalidSlot);

            LaneState lane = state.Lanes[laneIndex];

            if (!card.CanDeployOn(lane.Terrain))
                return new ActionOutcome(ActionResult.TerrainNotAllowed);

            lane.Compact(player);
            int count = lane.TroopCount(player);

            if (count >= LaneState.SlotsPerSide)
                return new ActionOutcome(ActionResult.LaneFull);

            TroopState existing = lane.GetTroop(player, 1);

            if (count == 0)
            {
                if (slot != 1)
                    return new ActionOutcome(ActionResult.InvalidSlot);
            }
            else if (!card.HasTeam && !catalog[existing.CardId].HasTeam)
            {
                return new ActionOutcome(ActionResult.CannotShareLane);
            }

            var events = new List<GameEvent>();

            if (count == 1 && slot == 1)
            {
                lane.SetTroop(player, 2, existing);
                lane.SetTroop(player, 1, null);

                events.Add(new GameEvent
                {
                    Type = GameEventType.TroopMoved,
                    Player = player,
                    Lane = laneIndex,
                    Slot = 2,
                    CardId = existing.CardId,
                    Amount = 1
                });
            }

            ps.Hand.Remove(cardId);

            lane.SetTroop(player, slot, new TroopState
            {
                CardId = card.Id,
                Attack = card.Attack,
                Health = card.Health,
                MaxHealth = card.Health
            });

            events.Add(new GameEvent
            {
                Type = GameEventType.TroopDeployed,
                Player = player,
                Lane = laneIndex,
                Slot = slot,
                CardId = card.Id
            });

            return new ActionOutcome(ActionResult.Applied, events);
        }

        // =====================================================
        // END STEP
        // =====================================================

        // Termina el paso del jugador. Tras el paso 4 el motor resuelve el combate (paso 5),
        // roba las cartas del final del momento del día y deja la partida en el siguiente paso 1.
        public ActionOutcome EndStep(
            GameState state, PlayerId player, int expectedTurnNumber = NoExpectedTurn)
        {
            if (state.IsOver)
                return new ActionOutcome(ActionResult.GameOver);

            if (expectedTurnNumber != NoExpectedTurn && expectedTurnNumber != state.TurnNumber)
                return new ActionOutcome(ActionResult.StaleTurn);

            if (state.CurrentPlayer != player)
                return new ActionOutcome(ActionResult.NotYourTurn);

            var events = new List<GameEvent>();

            if (state.Step < 4)
            {
                StartStep(state, state.Step + 1, events);
                return new ActionOutcome(ActionResult.Applied, events);
            }

            StartStep(state, 5, events);
            ResolveCombat(state, events);

            if (state.IsOver)
                return new ActionOutcome(ActionResult.Applied, events);

            EndTimeOfDay(state, events);
            return new ActionOutcome(ActionResult.Applied, events);
        }

        private static void StartStep(GameState state, int step, List<GameEvent> events)
        {
            state.Step = step;
            state.TurnNumber++;

            events.Add(new GameEvent
            {
                Type = GameEventType.StepStarted,
                Player = step == 5 ? state.Priority : state.CurrentPlayer,
                Amount = step
            });
        }

        // PhasesSystem.FinalizarMomentoDelDia: al terminar cada Alba u Ocaso se roba una carta.
        // La ronda termina solo después del Ocaso, y la prioridad cambia cada ronda.
        private void EndTimeOfDay(GameState state, List<GameEvent> events)
        {
            Draw(state, state.Priority, events);
            Draw(state, Opponent(state.Priority), events);

            if (state.TimeOfDay == TimeOfDayKind.Alba)
            {
                state.TimeOfDay = TimeOfDayKind.Ocaso;
            }
            else
            {
                state.Round++;
                state.TimeOfDay = TimeOfDayKind.Alba;
                state.Priority = state.Round % 2 == 1
                    ? state.FirstRoundPriority
                    : Opponent(state.FirstRoundPriority);
            }

            StartStep(state, 1, events);
        }

        // =====================================================
        // COMBAT
        // =====================================================

        private sealed class PlannedAttack
        {
            public PlayerId Attacker;
            public int Slot;
            public int Damage;
            public int TargetSlot;   // 0 = el portal
        }

        // CombatManager.ResolveCombat: carril por carril, en orden de LaneID.
        private void ResolveCombat(GameState state, List<GameEvent> events)
        {
            for (int laneIndex = 0; laneIndex < state.Lanes.Count; laneIndex++)
            {
                ResolveLane(state, laneIndex, events);

                if (state.IsOver)
                    return;
            }
        }

        private void ResolveLane(GameState state, int laneIndex, List<GameEvent> events)
        {
            LaneState lane = state.Lanes[laneIndex];
            PlayerId first = state.Priority;
            PlayerId second = Opponent(first);

            // Los dos bandos preparan sus ataques (objetivos incluidos) ANTES de que haya daño,
            // así que una tropa que muere en este carril todavía ataca y no se reasignan objetivos.
            var plan = new List<PlannedAttack>();
            Prepare(lane, first, plan);
            Prepare(lane, second, plan);

            ExecuteSide(state, laneIndex, lane, plan, first, events);
            if (state.IsOver) return;

            ExecuteSide(state, laneIndex, lane, plan, second, events);
            if (state.IsOver) return;

            // Las muertes se confirman al terminar todos los ataques del carril.
            RemoveDefeated(lane, laneIndex, PlayerId.Player1, events);
            RemoveDefeated(lane, laneIndex, PlayerId.Player2, events);
        }

        // CombatManager.AddSideAttacks: el espacio 2 primero, luego el 1.
        private static void Prepare(LaneState lane, PlayerId attacker, List<PlannedAttack> plan)
        {
            for (int slot = LaneState.SlotsPerSide; slot >= 1; slot--)
            {
                TroopState troop = lane.GetTroop(attacker, slot);

                if (troop == null || !troop.CanAttack)
                    continue;

                plan.Add(new PlannedAttack
                {
                    Attacker = attacker,
                    Slot = slot,
                    Damage = troop.Attack,
                    TargetSlot = FindTargetSlot(lane, Opponent(attacker))
                });
            }
        }

        // CombatManager.FindTarget: el espacio 2 enemigo vivo, luego el 1; si no hay, el portal.
        private static int FindTargetSlot(LaneState lane, PlayerId defender)
        {
            for (int slot = LaneState.SlotsPerSide; slot >= 1; slot--)
            {
                TroopState troop = lane.GetTroop(defender, slot);

                if (troop != null && troop.IsAlive)
                    return slot;
            }

            return 0;
        }

        private void ExecuteSide(
            GameState state, int laneIndex, LaneState lane,
            List<PlannedAttack> plan, PlayerId side, List<GameEvent> events)
        {
            // El espacio 2 ataca antes que el 1 (ResolveSideAttacks).
            for (int slot = LaneState.SlotsPerSide; slot >= 1; slot--)
            {
                PlannedAttack attack = plan.Find(a => a.Attacker == side && a.Slot == slot);

                if (attack == null)
                    continue;

                PlayerId defender = Opponent(side);

                events.Add(new GameEvent
                {
                    Type = GameEventType.Attack,
                    Player = side,
                    Lane = laneIndex,
                    Slot = attack.Slot,
                    TargetSlot = attack.TargetSlot == 0 ? -1 : attack.TargetSlot,
                    Amount = attack.Damage
                });

                if (attack.TargetSlot > 0)
                {
                    // TroopInGame.TakeDamage: la salud nunca baja de 0. Un objetivo que ya
                    // llegó a 0 en este carril sigue recibiendo el golpe (sin efecto).
                    TroopState target = lane.GetTroop(defender, attack.TargetSlot);
                    target.Health = Math.Max(0, target.Health - attack.Damage);
                    continue;
                }

                DamagePortal(state, defender, attack.Damage, events);

                if (state.IsOver)
                    return;
            }
        }

        private void RemoveDefeated(
            LaneState lane, int laneIndex, PlayerId owner, List<GameEvent> events)
        {
            for (int slot = 1; slot <= LaneState.SlotsPerSide; slot++)
            {
                TroopState troop = lane.GetTroop(owner, slot);

                if (troop == null || troop.Health > 0)
                    continue;

                lane.SetTroop(owner, slot, null);

                events.Add(new GameEvent
                {
                    Type = GameEventType.TroopDied,
                    Player = owner,
                    Lane = laneIndex,
                    Slot = slot,
                    CardId = troop.CardId
                });
            }

            if (lane.GetTroop(owner, 1) == null && lane.GetTroop(owner, 2) != null)
            {
                int cardId = lane.GetTroop(owner, 2).CardId;
                lane.Compact(owner);

                events.Add(new GameEvent
                {
                    Type = GameEventType.TroopMoved,
                    Player = owner,
                    Lane = laneIndex,
                    Slot = 1,
                    CardId = cardId,
                    Amount = 2
                });
            }
        }

        // =====================================================
        // PORTAL
        // =====================================================

        // Portals.GetNextThreshold: con N escudos pendientes, el siguiente se rompe al llegar a
        // (N + 1) * 20 % de la vida máxima. Se hace con enteros a propósito (ver Docs).
        public static int NextShieldThreshold(PortalState portal)
        {
            if (portal.RemainingShields <= 0)
                return -1;

            return (portal.MaxHealth * (portal.RemainingShields + 1) * 20 + 99) / 100;
        }

        // Portals.TakeDamage: un golpe rompe como mucho un escudo y la vida se queda en su
        // umbral. Sin escudos, el daño es completo.
        private static void DamagePortal(
            GameState state, PlayerId owner, int damage, List<GameEvent> events)
        {
            PortalState portal = state.Get(owner).Portal;

            if (damage <= 0 || portal.IsDestroyed)
                return;

            int previous = portal.Health;
            int resulting = Math.Max(0, portal.Health - damage);
            int threshold = NextShieldThreshold(portal);

            if (threshold >= 0)
            {
                int shieldLimit = Math.Min(portal.Health, threshold);

                if (resulting <= shieldLimit)
                {
                    portal.Health = shieldLimit;
                    portal.RemainingShields--;

                    events.Add(new GameEvent
                    {
                        Type = GameEventType.ShieldBroken,
                        Player = owner,
                        Amount = portal.RemainingShields
                    });
                }
                else
                {
                    portal.Health = resulting;
                }
            }
            else
            {
                portal.Health = resulting;
            }

            events.Add(new GameEvent
            {
                Type = GameEventType.PortalDamaged,
                Player = owner,
                Amount = previous - portal.Health
            });

            // REGLA NUEVA (el juego aún no la tiene): el portal destruido da la victoria al rival
            // y la partida termina en ese momento.
            if (portal.IsDestroyed)
            {
                state.Winner = Opponent(owner);

                events.Add(new GameEvent
                {
                    Type = GameEventType.GameWon,
                    Player = Opponent(owner)
                });
            }
        }

        // =====================================================
        // VIEW (información oculta)
        // =====================================================

        // Lo que "viewer" puede ver. De su rival solo se sabe cuántas cartas tiene.
        public PlayerView CreateView(GameState state, PlayerId viewer)
        {
            PlayerId other = Opponent(viewer);
            PlayerState me = state.Get(viewer);
            PlayerState rival = state.Get(other);

            var view = new PlayerView
            {
                Me = viewer,
                Round = state.Round,
                TimeOfDay = state.TimeOfDay,
                Step = state.Step,
                TurnNumber = state.TurnNumber,
                CurrentPlayer = state.CurrentPlayer,
                Priority = state.Priority,
                Winner = state.Winner,
                MyHand = new List<int>(me.Hand),
                MyDeckCount = me.Deck.Count,
                OpponentHandCount = rival.Hand.Count,
                OpponentDeckCount = rival.Deck.Count,
                MyPortal = ToView(me.Portal),
                OpponentPortal = ToView(rival.Portal),
                Lanes = new List<LaneView>()
            };

            foreach (LaneState lane in state.Lanes)
            {
                var laneView = new LaneView
                {
                    Terrain = lane.Terrain,
                    Troops = new[]
                    {
                        new TroopView[LaneState.SlotsPerSide],
                        new TroopView[LaneState.SlotsPerSide]
                    }
                };

                foreach (PlayerId player in new[] { PlayerId.Player1, PlayerId.Player2 })
                {
                    for (int slot = 1; slot <= LaneState.SlotsPerSide; slot++)
                    {
                        TroopState troop = lane.GetTroop(player, slot);

                        if (troop == null)
                            continue;

                        laneView.Troops[(int)player][slot - 1] = new TroopView
                        {
                            CardId = troop.CardId,
                            Attack = troop.Attack,
                            Health = troop.Health,
                            MaxHealth = troop.MaxHealth
                        };
                    }
                }

                view.Lanes.Add(laneView);
            }

            return view;
        }

        private static PortalView ToView(PortalState portal)
        {
            return new PortalView
            {
                Health = portal.Health,
                MaxHealth = portal.MaxHealth,
                RemainingShields = portal.RemainingShields
            };
        }

        // Los robos del rival no revelan qué carta fue: el id se oculta (-1).
        public static List<GameEvent> RedactFor(IEnumerable<GameEvent> events, PlayerId viewer)
        {
            var result = new List<GameEvent>();

            foreach (GameEvent e in events)
            {
                GameEvent copy = e.Clone();

                if (copy.Type == GameEventType.CardDrawn && copy.Player != viewer)
                    copy.CardId = -1;

                result.Add(copy);
            }

            return result;
        }
    }
}
