using UnityEngine;

public class AbilityRegistrator : MonoBehaviour
{
    private void Awake()
    {
        RegisterBasicAbilities();
    }

    public void RegisterBasicAbilities()
    {
        // === PASIVAS (Passive) ===
        // Passive + GainArmor -> Otorga armadura proporcional a vida faltante
        // Condición: solo si hay vida faltante (MissingHealth) O si condition es None
        AbilityEffectExecutor.Register(TriggerType.Passive, EffectType.GainArmor,
            (data, tropo, board) =>
            {
                // 1. Verificar TargetType: Passive típicamente afecta al poseedor (Self)
                bool targetOk = data.Target == TargetType.Self || data.Target == TargetType.Player;

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
                    int valorEfecto = data.Effects[0].Value;
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
        // EndTurn + Heal -> Cura al personaje al terminar turno
        AbilityEffectExecutor.Register(TriggerType.EndTurn, EffectType.Heal,
            (data, tropo, board) =>
            {
                // Verificar TargetType: Heal típico es Self o Ally
                bool targetOk = data.Target == TargetType.Self || 
                               data.Target == TargetType.Ally || 
                               data.Target == TargetType.Player;

                // Verificar FilterType: Heal usualmente es Self o Ally
                bool filterOk = data.Filter == FilterType.None || 
                               data.Filter == FilterType.Self || 
                               data.Filter == FilterType.Ally;

                // Verificar ConditionType: Si es MissingHealth, solo si vida faltante
                bool conditionOk = data.Condition == ConditionType.None ||
                    (data.Condition == ConditionType.MissingHealth && 
                     (tropo.VidaMaxima - tropo.VidaActual) > 0);

                if (targetOk && filterOk && conditionOk)
                {
                    int valorCuracion = data.Effects[0].Value;
                    tropo.Curar(valorCuracion);
                    Debug.Log($"EndTurn Heal cura {valorCuracion} de vida (target={data.Target}, filter={data.Filter})");
                }
                else
                {
                    Debug.LogWarning($"EndTurn Heal: filtros no coinciden - target={data.Target}, filter={data.Filter}, condition={data.Condition}");
                }
            });

        // EndTurn + Damage -> Aplica daño al personaje al terminar turno
        AbilityEffectExecutor.Register(TriggerType.EndTurn, EffectType.DealDamage,
            (data, tropo, board) =>
            {
                // Verificar TargetType: Damage típico es Enemy o Ally (depende de quién sea el dueño)
                bool targetOk = data.Target == TargetType.Enemy || 
                               data.Target == TargetType.Ally || 
                               data.Target == TargetType.Player;

                // Verificar FilterType: Damage usualmente es Enemy o Ally
                bool filterOk = data.Filter == FilterType.None || 
                               data.Filter == FilterType.Enemy || 
                               data.Filter == FilterType.Ally;

                // Condition: Typically None for EndTurn damage
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int dano = data.Effects[0].Value;
                    tropo.RecibirDaño(dano);
                    Debug.Log($"EndTurn DealDamage inflige {dano} de daño");
                }
                else
                {
                    Debug.LogWarning($"EndTurn DealDamage: filtros descartados - target={data.Target}, filter={data.Filter}");
                }
            });

        // === ON ATTACK ===
        // OnAttack + DealDamage -> Inflige daño cuando la tropa ataca
        AbilityEffectExecutor.Register(TriggerType.OnAttack, EffectType.DealDamage,
            (data, tropoAtacante, board) =>
            {
                // --- LÓGICA DE TARGET ---
                // Determinar quién es el objetivo basado en TargetType
                bool targetOk = data.Target switch
                {
                    TargetType.Self => true, // Se asume que el atacante se afecta a sí mismo o se usa para referencia
                    TargetType.Enemy => true,
                    TargetType.Ally => true,
                    TargetType.Player => true,
                    TargetType.AllyInLane => tropoAtacante.laneIndex == tropoAtacante.GetComponent<TropaEnTablero>().laneIndex,
                    TargetType.EnemyInLane => /* lógica compleja */ true,
                    _ => true // Default: permitir
                };

                // --- LÓGICA DE FILTER ---
                // Filter determina a quién afecta el daño dentro del objetivo seleccionado
                bool filterOk = data.Filter switch
                {
                    FilterType.None => true,
                    FilterType.Ally => tropoAtacante.EsAliada,
                    FilterType.Enemy => !tropoAtacante.EsAliada,
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
                    int dano = data.Effects[0].Value;
                    // Aplicar daño - la lógica exacta dependerá de a quién vayamos dirigidos
                    // Por ahora, aplicar al tropo atacante o al objetivo designada
                    Debug.Log($"OnAttack DealDamage: target={data.Target}, filter={data.Filter}, condition={data.Condition} - dano={dano}");
                    // TODO: Implementar objetivo específico basado en targetType y filter
                }
                else
                {
                    Debug.LogWarning($"OnAttack DealDamage: filtros fallaron - target={data.Target}, filter={data.Filter}, condition={data.Condition}");
                }
            });

        // OnAttack + GainAttack -> Aumenta ataque al atacar
        AbilityEffectExecutor.Register(TriggerType.OnAttack, EffectType.GainAttack,
            (data, tropoAtacante, board) =>
            {
                // Para GainAttack, típicamente afecta al propio tropo que ataca
                bool targetOk = data.Target == TargetType.Self || data.Target == TargetType.Player;
                bool filterOk = data.Filter == FilterType.None || data.Filter == FilterType.Self;
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int bonificacion = data.Effects[0].Value;
                    tropoAtacante.ataqueActual += bonificacion;
                    Debug.Log($"OnAttack GainAttack +{bonificacion} de ataque");
                }
                else
                {
                    Debug.LogWarning($"OnAttack GainAttack: filtros no aplicables - target={data.Target}, filter={data.Filter}");
                }
            });

        // === ON DEPLOY ===
        // OnDeploy + SummonCard -> Invocar otra carta al jugar esta
        AbilityEffectExecutor.Register(TriggerType.OnDeploy, EffectType.SummonCard,
            (data, tropo, board) =>
            {
                bool targetOk = data.Target == TargetType.Self || data.Target == TargetType.Player;
                bool filterOk = data.Filter == FilterType.None || data.Filter == FilterType.SummonedFamily;
                bool conditionOk = data.Condition == ConditionType.None;

                if (targetOk && filterOk && conditionOk)
                {
                    int valor = data.Effects[0].Value;
                    Debug.Log($"OnDeploy SummonCard: valor={valor} (target={data.Target}, filter={data.Filter})");
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
                bool targetOk = data.Target == TargetType.Self || data.Target == TargetType.Player;
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