using UnityEngine;
using System.Collections.Generic;
using System;

public static class AbilityEffectExecutor
{
    public static readonly Dictionary<(TriggerType trigger, EffectType effect), Action<AbilityData, Tropa, BoardManager>> _executors =
        new();

    public static void Register(TriggerType trigger, EffectType effect, System.Action<AbilityData, Tropa, BoardManager> executor)
    {
        _executors[(trigger, effect)] = executor;
    }

    public static void Execute(TriggerType trigger, EffectType effect, AbilityData data, Tropa target, BoardManager board)
    {
        if (_executors.TryGetValue((trigger, effect), out var action))
        {
            action(data, target, board);
        }
        else
        {
            Debug.LogWarning($"No executor registered for trigger={trigger}, effect={effect}");
        }
    }
}