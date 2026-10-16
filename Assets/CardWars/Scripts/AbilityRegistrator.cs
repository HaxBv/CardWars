using UnityEngine;
using System;

public class AbilityRegistrator : MonoBehaviour
{
    private void Awake()
    {
        RegisterBasicAbilities();
    }

    public void RegisterBasicAbilities()
    {
        // === PASIVAS (Passive) ===
        // Passive + ModifyArmor -> Otorga armadura proporcional a vida faltante
        // Condición: solo si hay vida faltante (MissingHealth) O si condition es None
        AbilityEffectExecutor.Register(TriggerType.Passive, EffectType.ModifyArmor,
            (data, tropo, board) =>
            {
                // 1. Verificar TargetType: Passive típicamente afecta al poseedor (Self)
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self || data.Effects?[0]?.Target == TargetType.OwnPortal;

                // 2. Verificar FilterType: Passive usualmente es None o Self
                bool filterOk = data.Filter == FilterType.None || data.Filter == FilterType.Self;

                // 3. Verificar ConditionType: Si es MissingHealth, verificar vida faltante
                // Si es None, siempre aplicar
                bool conditionOk = data.Condition == ConditionType.None ||
                    (data.Condition == ConditionType.MissingHealth && 
                     (tropo.VidaMaxima - tropo.VidaActual) > 0);

                // Solo ejecutar si pasaron todas las verificaciones
                if (targetOk && filterOk && conditionOk)
                {
                    int vidaFaltante = tropo.VidaMaxima - tropo.VidaActual;
                    int valorEfecto = data.Effects?[0]?.Value ?? 0;
                    int armaduraGanada = vidaFaltante * valorEfecto;
                    tropo.armadura += armaduraGanada;
                    Debug.Log($"Passive MissingHealth otorga {armaduraGanada} de armadura (vida faltante: {vidaFaltante})");
                }
                else
                {
                    // Ejecutar versión "solo datos" sin filtros estrictos si queremos compatibilidad
                    // Por ahora, solo registrar warning si no coinciden los filtros
                    if (data.Condition != ConditionType.None && data.Condition != ConditionType.MissingHealth)
                    {
                        Debug.LogWarning($"Passive GainArmor: Condition {data.Condition} no mapeado para este caso");
                    }
                }
            });

        // Passive + otras condiciones pueden añadirse aquí

        // === END TURN ===
        // EndTurn + Heal -> Cura al persona al terminar turno
        AbilityEffectExecutor.Register(TriggerType.EndRound, EffectType.Heal,
            (data, tropo, board) =>
            {
                // Verificar TargetType: Heal típico es Self o Ally
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self || 
                               data.Effects?[0]?.Target == TargetType.SelectedAlly || 
                               data.Effects?[0]?.Target == TargetType.OwnPortal;

                // Verificar FilterType: Heal usualmente es Self o Ally
                bool filterOk = data.Filter == FilterType.None || 
                               data.Filter == FilterType.Self || 
                               data.Filter == FilterType.AllyUnit;

                // Verificar ConditionType: Si es MissingHealth, solo si vida faltante
                bool conditionOk = data.Condition == ConditionType.None ||
                    (data.Condition == ConditionType.MissingHealth && 
                     (tropo.VidaMaxima - tropo.VidaActual) > 0);

                if (targetOk && filterOk && conditionOk)
                {
                    int valorCuracion = data.Effects?[0]?.Value ?? 0;
                    tropo.Curar(valorCuracion);
                    Debug.Log($"EndTurn Heal cura {valorCuracion} de vida (target={data.Effects?[0]?.Target}, filter={data.Filter})");
                }
                else
                {
                    Debug.LogWarning($"EndTurn Heal: filtros no coinciden - target={data.Effects?[0]?.Target}, filter={data.Filter}, condition={data.Condition}");
                }
            });

        // EndTurn + Damage -> Aplica daño al personaje al terminar turno
        AbilityEffectExecutor.Register(TriggerType.EndRound, EffectType.DealDamage,
            (data, tropo, board) =>
            {
                // Verificar TargetType: Damage típico es Enemy o Ally (depende de quién sea el dueño)
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.SelectedEnemy || 
                               data.Effects?[0]?.Target == TargetType.SelectedAlly || 
                               data.Effects?[0]?.Target == TargetType.OwnPortal;

                // Verificar FilterType: Damage usualmente es Enemy o Ally
                bool filterOk = data.Filter == FilterType.None || 
                               data.Filter == FilterType.EnemyUnit || 
                               data.Filter == FilterType.AllyUnit;

                // Condition: Typically None for EndTurn damage
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int dano = data.Effects?[0]?.Value ?? 0;
                    tropo.RecibirDaño(dano);
                    Debug.Log($"EndTurn DealDamage inflige {dano} de daño");
                }
                else
                {
                    Debug.LogWarning($"EndTurn DealDamage: filtros descartados - target={data.Effects?[0]?.Target}, filter={data.Filter}");
                }
            });

        // === ON HIT ===
        // OnHit + DealDamage -> Inflige daño cuando la tropa es golpeada
        AbilityEffectExecutor.Register(TriggerType.OnHit, EffectType.DealDamage,
            (data, tropoAtacante, board) =>
            {
                // --- LÓGICA DE TARGET ---
                // Determinar quién es el objetivo basado en TargetType
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target switch
                {
                    TargetType.Self => true, // Se asume que el atacante se afecta a sí mismo o se usa para referencia
                    TargetType.SelectedEnemy => true,
                    TargetType.SelectedAlly => true,
                    TargetType.OwnPortal => true,
                    TargetType.AllyInLane => tropoAtacante.laneIndex == tropoAtacante.GetComponent<TropaEnTablero>().laneIndex,
                    TargetType.EnemyInLane => /* lógica compleja */ true,
                    _ => true // Default: permitir
                };

                // --- LÓGICA DE FILTER ---
                // Filter determina a quién afecta el daño dentro del objetivo seleccionado
                bool filterOk = data.Filter switch
                {
                    FilterType.None => true,
                    FilterType.AllyUnit => tropoAtacante.EsAliada,
                    FilterType.EnemyUnit => !tropoAtacante.EsAliada,
                    FilterType.NotSelf => true, // Siempre true si ya verificamos target
                    _ => true
                };

                // --- LÓGICA DE CONDITION ---
                // Condition podría ser MissingHealth, Fury, Bleed, etc.
                // Si condition es None, siempre aplicar
                bool conditionOk = data.Condition == ConditionType.None ||
                    data.Condition == ConditionType.MissingHealth; // Para efectos de daño "bonus" por vida baja

                if (targetOk && filterOk && conditionOk)
                {
                    int dano = data.Effects?[0]?.Value ?? 0;
                    // Aplicar daño - la lógica exacta dependerá de a quién vayamos dirigidos
                    // Por ahora, aplicar al tropo atacante o al objetivo designada
                    Debug.Log($"OnHit DealDamage: target={data.Effects?[0]?.Target}, filter={data.Filter}, condition={data.Condition} - dano={dano}");
                    // TODO: Implementar objetivo específico basado en targetType y filter
                }
                else
                {
                    Debug.LogWarning($"OnHit DealDamage: filtros fallaron - target={data.Effects?[0]?.Target}, filter={data.Filter}, condition={data.Condition}");
                }
            });

        // OnHit + ModifyAttack -> Aumenta ataque al ser golpeado
        AbilityEffectExecutor.Register(TriggerType.OnHit, EffectType.ModifyAttack,
            (data, tropoAtacante, board) =>
            {
                // Para ModifyAttack, típicamente afecta al propio tropo que es golpeado
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self || data.Effects?[0]?.Target == TargetType.OwnPortal;
                bool filterOk = data.Filter == FilterType.None || data.Filter == FilterType.Self;
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int bonificacion = data.Effects?[0]?.Value ?? 0;
                    tropoAtacante.ataqueActual += bonificacion;
                    Debug.Log($"OnHit ModifyAttack +{bonificacion} de ataque");
                }
                else
                {
                    Debug.LogWarning($"OnHit ModifyAttack: filtros no aplicables - target={data.Effects?[0]?.Target}, filter={data.Filter}");
                }
            });

        // === ON DAMAGED ===
        // OnDamaged + DealDamage -> Inflige daño cuando la tropa recibe daño
        AbilityEffectExecutor.Register(TriggerType.OnDamaged, EffectType.DealDamage,
            (data, tropoQueRecibe, board) =>
            {
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target switch
                {
                    TargetType.Self => true, // Afecta al que recibe el daño
                    TargetType.SelectedEnemy => true,
                    TargetType.SelectedAlly => true,
                    _ => true
                };

                // Filter typically None for OnDamaged
                bool filterOk = data.Filter == FilterType.None;

                // Condition
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int dano = data.Effects?[0]?.Value ?? 0;
                    tropoQueRecibe.RecibirDaño(dano);
                    Debug.Log($"OnDamaged DealDamage: inflige {dano} de daño a la tropa");
                }
                else
                {
                    Debug.LogWarning($"OnDamaged DealDamage: filtros fallaron - target={data.Effects?[0]?.Target}, filter={data.Filter}");
                }
            });

        // OnDamaged + ModifyArmor -> Otorga armadura al recibir daño
        AbilityEffectExecutor.Register(TriggerType.OnDamaged, EffectType.ModifyArmor,
            (data, tropoQueRecibe, board) =>
            {
                // Target es el propio tropo
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self;

                // Filter typically None
                bool filterOk = data.Filter == FilterType.None;

                // Condition
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int vidaFaltante = tropoQueRecibe.VidaMaxima - tropoQueRecibe.VidaActual;
                    int valorEfecto = data.Effects?[0]?.Value ?? 0;
                    int armaduraGanada = vidaFaltante * valorEfecto;
                    tropoQueRecibe.armadura += armaduraGanada;
                    Debug.Log($"OnDamaged GainArmor: otorga {armaduraGanada} de armadura (vida faltante: {vidaFaltante})");
                }
                else
                {
                    Debug.LogWarning($"OnDamaged GainArmor: filtros no aplicables");
                }
            });

        // OnDeploy + SummonCard -> Invocar otra carta al jugar esta
        AbilityEffectExecutor.Register(TriggerType.OnDeploy, EffectType.SummonCard,
            (data, tropo, board) =>
            {
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self || data.Effects?[0]?.Target == TargetType.OwnPortal;
                bool filterOk = data.Filter == FilterType.None || data.Filter == FilterType.SummonedFamily;
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int valor = data.Effects?[0]?.Value ?? 0;
                    Debug.Log($"OnDeploy SummonCard: valor={valor} (target={data.Effects?[0]?.Target}, filter={data.Filter})");
                }
                else
                {
                    Debug.LogWarning($"OnDeploy SummonCard: filtros no coinciden");
                }
            });

        // OnDeploy + DrawCard -> Robar una carta al jugar
        AbilityEffectExecutor.Register(TriggerType.OnDeploy, EffectType.DrawCard,
            (data, tropo, board) =>
            {
                // Robo de cartas típicamente no tienen target/filter/condition complejos
                // Pero los respetamos para consistencia
                // Target ahora está en el primer efecto: data.Effects[0].Target
                bool targetOk = data.Effects?[0]?.Target == TargetType.Self || data.Effects?[0]?.Target == TargetType.OwnPortal;
                bool filterOk = data.Filter == FilterType.None;
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    Debug.Log($"OnDeploy DrawCard: robar una carta");
                }
                else
                {
                    Debug.LogWarning($"OnDeploy DrawCard: filtros inesperados");
                }
            });

        Debug.Log("AbilityRegistrator: Registradas " + AbilityEffectExecutor._executors.Count + " combinaciones de habilidades (con lógica Condition/Filter/Target)");
    }
}