
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [Header("COMBAT")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxAttackWait = 3f;

    [Header("TEAMS")]
    [SerializeField]
    private bool player1UsesAllySide = true;

    [Header("ATTACK TIMING")]
    [SerializeField]
    private float firstSlotAttackDelay = 0.5f;
    [SerializeField]
    private float nextLaneDelay = 0.5f;
    private Coroutine combatRoutine;

    private class AttackOrder
    {
        public GameObject Root;
        public TroopInGame Attacker;
        public TroopInGame Target;
        public TroopDisplay Display;

        public int Slot;
        public int TargetSlot;
        public int Damage;

        public Vector3 HomePosition;

        public Transform PortalAttackPoint;
        public Portals PortalTarget;
    }

    private void OnEnable()
    {
        PhasesSystem.OnFaseCombateIniciada += StartCombat;
    }

    private void OnDisable()
    {
        PhasesSystem.OnFaseCombateIniciada -= StartCombat;
    }

    private void StartCombat()
    {
        if (combatRoutine != null)
            return;

        combatRoutine = StartCoroutine(ResolveCombat());
    }

    // =====================================================
    // DISTANCE
    // =====================================================

    private Vector3 GetMeleeAttackPosition(AttackOrder attack)
    {
        Vector3 origin = attack.HomePosition;

        float direction = attack.Attacker.IsAlly ? 1f : -1f;

        float attackSpacing = attack.Attacker.CardData.AttackSpacing;

        float targetX;

        // =================================================
        // ATTACK TROOP
        // =================================================

        if (attack.Target != null)
        {
            Transform targetRoot =
                attack.Target.transform.parent;

            targetX = targetRoot != null
                ? targetRoot.position.x
                : attack.Target.transform.position.x;
        }

        // =================================================
        // ATTACK PORTAL
        // =================================================

        else if (attack.PortalAttackPoint != null)
        {
            targetX = attack.PortalAttackPoint.position.x;
        }

        else
        {
            Debug.LogWarning(
                $"No se encontró punto de ataque al portal " +
                $"para {attack.Attacker.CardData.CardName}"
            );

            return origin;
        }

        // =================================================
        // CALCULATE POSITION
        // =================================================

        float desiredX =
            targetX - direction * attackSpacing;

        float travelDistance = Mathf.Max(
            0f,
            (desiredX - origin.x) * direction
        );

        return origin +
               Vector3.right * direction * travelDistance;
    }


    // =====================================================
    // TARGET
    // =====================================================

    private TroopInGame FindTarget(
        LandLane lane,
        bool attackerIsAlly,
        out int targetSlot)
    {
        targetSlot = 0;

        // SecondSlot es el slot adelantado.
        for (int slot = 2; slot >= 1; slot--)
        {
            GameObject enemyRoot =
                lane.GetTroop(!attackerIsAlly, slot);

            if (enemyRoot == null)
                continue;

            TroopInGame enemy =
                enemyRoot.GetComponentInChildren<TroopInGame>();

            if (enemy == null || !enemy.IsAlive)
                continue;

            targetSlot = slot;
            return enemy;
        }

        // Sin tropas enemigas: portal.
        return null;
    }

    // =====================================================
    // COMBAT
    // =====================================================


    private IEnumerator ResolveCombat()
    {
        LandLane[] lanes =
            FindObjectsByType<LandLane>(
                FindObjectsSortMode.None
            );

        Array.Sort(lanes, (a, b) =>
            a.LaneID.CompareTo(b.LaneID));

        bool allyFirst =
            (PhasesSystem.Instance.CurrentPriority ==
             PlayerTurn.Player1) == player1UsesAllySide;

        for (int i = 0; i < lanes.Length; i++)
        {
            LandLane lane = lanes[i];

            if (lane == null)
                continue;

            Debug.Log($"COMBAT - Lane {lane.LaneID}");

            // Esperar a que termine completamente el carril.
            yield return ResolveLane(lane, allyFirst);

            // Esperar antes de pasar al siguiente carril.
            // No aplicar delay después del último carril.
            bool hasNextLane = false;

            for (int j = i + 1; j < lanes.Length; j++)
            {
                if (lanes[j] != null)
                {
                    hasNextLane = true;
                    break;
                }
            }

            if (hasNextLane && nextLaneDelay > 0f)
                yield return new WaitForSeconds(nextLaneDelay);
        }

        combatRoutine = null;

        PhasesSystem.Instance.CompleteCombat();
    }


    private IEnumerator ResolveLane(
        LandLane lane,
        bool allyFirst)
    {
        List<AttackOrder> attacks = new();

        // Preparar ambos bandos antes del daño.
        AddSideAttacks(lane, allyFirst, attacks);
        AddSideAttacks(lane, !allyFirst, attacks);

        // Primero ataca el jugador con prioridad.
        yield return ResolveSideAttacks(
            attacks,
            allyFirst
        );

        // Después el otro bando.
        // Conserva sus ataques preparados,
        // incluso si recibió daño letal.
        yield return ResolveSideAttacks(
            attacks,
            !allyFirst
        );

        // Al terminar todos los ataques del carril,
        // confirmar las muertes.
        RemoveDefeatedTroops(lane);

        Debug.Log($"COMBAT - Lane {lane.LaneID} finished");
    }

    // =====================================================
    // PREPARE ATTACKS
    // =====================================================

    private void AddSideAttacks(
        LandLane lane,
        bool isAlly,
        List<AttackOrder> attacks)
    {
        for (int slot = 2; slot >= 1; slot--)
        {
            GameObject root = lane.GetTroop(isAlly, slot);

            if (root == null)
                continue;

            TroopInGame troop =
                root.GetComponentInChildren<TroopInGame>();

            TroopDisplay display =
                root.GetComponentInChildren<TroopDisplay>();

            if (troop == null ||
                !troop.CanAttack ||
                display == null ||
                display.TroopAnimator == null)
            {
                continue;
            }

            int targetSlot;

            TroopInGame target = FindTarget(
                lane,
                isAlly,
                out targetSlot
            );

            Transform portalPoint = null;
            Portals portalTarget = null;

            if (target == null)
            {
                portalPoint = lane.GetPortalAttackPoint(isAlly);

                if (portalPoint != null)
                {
                    portalTarget =
                        portalPoint.GetComponentInParent<Portals>();
                }
            }
            attacks.Add(new AttackOrder
            {
                Root = root,
                Attacker = troop,
                Target = target,
                Display = display,

                Slot = slot,
                TargetSlot = targetSlot,
                Damage = troop.CurrentAttack,

                HomePosition = root.transform.position,

                PortalAttackPoint = portalPoint,
                PortalTarget = portalTarget
            });
        }
    }

    // =====================================================
    // SIDE ATTACK ORDER
    // =====================================================

    private IEnumerator ResolveSideAttacks(
        List<AttackOrder> attacks,
        bool isAlly)
    {
        AttackOrder secondSlotAttack = attacks.Find(
            a => a.Attacker.IsAlly == isAlly &&
                 a.Slot == 2
        );

        AttackOrder firstSlotAttack = attacks.Find(
            a => a.Attacker.IsAlly == isAlly &&
                 a.Slot == 1
        );

        Coroutine secondRoutine = null;
        Coroutine firstRoutine = null;

        // SecondSlot ataca primero.
        if (secondSlotAttack != null)
        {
            secondRoutine = StartCoroutine(
                ExecuteAttack(secondSlotAttack)
            );
        }

        // FirstSlot empieza 0.5 segundos después.
        if (firstSlotAttack != null)
        {
            if (secondRoutine != null)
            {
                yield return new WaitForSeconds(
                    firstSlotAttackDelay
                );
            }

            firstRoutine = StartCoroutine(
                ExecuteAttack(firstSlotAttack)
            );
        }

        // Esperar los ataques y regresos de este bando.
        if (secondRoutine != null)
            yield return secondRoutine;

        if (firstRoutine != null)
            yield return firstRoutine;
    }

    // =====================================================
    // EXECUTE ATTACK
    // =====================================================

    private IEnumerator ExecuteAttack(AttackOrder attack)
    {
        GameObject root = attack.Root;
        TroopInGame attacker = attack.Attacker;
        TroopDisplay display = attack.Display;

        if (root == null || attacker == null)
            yield break;

        if (attacker.ConsumeStun())
        {
            Debug.Log(
                $"{attacker.CardData.CardName} is stunned " +
                "and skips its next attack."
            );

            yield break;
        }

        bool isMelee = attacker.CardData.IsMelee;

        // -------------------------------------------------
        // MELEE MOVEMENT
        // -------------------------------------------------

        if (isMelee)
        {
            Vector3 attackPosition =
                GetMeleeAttackPosition(attack);

            while (root != null &&
                   Vector3.Distance(
                       root.transform.position,
                       attackPosition
                   ) > 0.01f)
            {
                root.transform.position =
                    Vector3.MoveTowards(
                        root.transform.position,
                        attackPosition,
                        moveSpeed * Time.deltaTime
                    );

                yield return null;
            }
        }

        // -------------------------------------------------
        // ATTACK IMPACT CALLBACK
        // -------------------------------------------------


        Action onImpact = () =>
        {
            // =================================================
            // DAMAGE TO TROOP
            // =================================================

            if (attack.Target != null)
            {
                attack.Target.TakeDamage(
                    attack.Damage,
                    attacker
                );


                if (attacker.HasPassive(AbilityPassive.Freeze))
                {
                    attack.Target.ApplyStun();

                    Debug.Log(
                        $"{attack.Target.CardData.CardName} " +
                        "has been stunned."
                    );
                }


                Debug.Log(
                    $"{attacker.CardData.CardName} attacks " +
                    $"{attack.Target.CardData.CardName} " +
                    $"for {attack.Damage} damage. " +
                    $"HP: {attack.Target.CurrentHealth}"
                );
            }

            // =================================================
            // DAMAGE TO PORTAL
            // =================================================

            else if (attack.PortalTarget != null)
            {
                attack.PortalTarget.TakeDamage(
                    attack.Damage
                );

                Debug.Log(
                    $"{attacker.CardData.CardName} attacks " +
                    $"{attack.PortalTarget.gameObject.name} " +
                    $"for {attack.Damage} damage. " +
                    $"HP: {attack.PortalTarget.CurrentHealth}"
                );
            }

            else
            {
                Debug.LogWarning(
                    $"No se encontró un objetivo válido para " +
                    $"{attacker.CardData.CardName}"
                );
            }
        };


        // Suscribir antes de iniciar la animación.
        display.OnAttackImpact += onImpact;

        display.PlayAttack();

        // -------------------------------------------------
        // WAIT FOR ATTACK END
        // -------------------------------------------------

        float elapsed = 0f;

        while (!display.AttackFinished && elapsed < maxAttackWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Desuscribir para evitar impactos duplicados
        // durante futuros ataques.
        display.OnAttackImpact -= onImpact;

        if (!display.ImpactTriggered)
        {
            Debug.LogWarning(
                $"No se recibió AttackImpact de " +
                $"{attacker.CardData.CardName}."
            );
        }

        if (!display.AttackFinished)
        {
            Debug.LogWarning(
                $"No se recibió AttackEnd de " +
                $"{attacker.CardData.CardName}."
            );
        }

        // -------------------------------------------------
        // RETURN TO SLOT
        // -------------------------------------------------

        // Las ranged nunca se desplazan.
        if (!isMelee || root == null)
            yield break;

        // La melee regresa apenas termina su Attack.
        while (Vector3.Distance(
                   root.transform.position,
                   attack.HomePosition
               ) > 0.01f)
        {
            root.transform.position =
                Vector3.MoveTowards(
                    root.transform.position,
                    attack.HomePosition,
                    moveSpeed * Time.deltaTime
                );

            yield return null;
        }

        root.transform.position = attack.HomePosition;
    }

    // =====================================================
    // DEATH CHECK
    // =====================================================

    private void RemoveDefeatedTroops(LandLane lane)
    {
        List<TroopInGame> defeated = new();

        // Buscar derrotados de ambos equipos.
        for (int side = 0; side < 2; side++)
        {
            bool isAlly = side == 0;

            for (int slot = 1; slot <= 2; slot++)
            {
                GameObject root =
                    lane.GetTroop(isAlly, slot);

                if (root == null)
                    continue;

                TroopInGame troop =
                    root.GetComponentInChildren<TroopInGame>();

                if (troop != null &&
                    troop.CurrentHealth <= 0)
                {
                    defeated.Add(troop);
                }
            }
        }

        // Confirmar y retirar después de comprobar
        // todo el carril.
        foreach (TroopInGame troop in defeated)
        {
            if (troop != null && troop.ConfirmDeath())
            {
                lane.RemoveDefeatedTroop(troop);
            }
        }
    }
}
