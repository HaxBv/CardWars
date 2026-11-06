
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TroopCardInstance : CardInstance
{
    public int CurrentAttack { get; private set; }
    public int CurrentHealth { get; private set; }
    public int CurrentArmor { get; private set; }
    public int CurrentShield { get; private set; }
    public int CurrentShieldArmor { get; private set; }
    public int CurrentBurn { get; private set; }
    public int CurrentOverstrike { get; private set; }

    private List<AbilityPassive> currentPassives = new();

    public IReadOnlyList<AbilityPassive> CurrentPassives =>
        currentPassives;

    public TroopCardInstance(TroopCardData data)
        : base(data)
    {
        CurrentAttack = data.Attack;
        CurrentHealth = data.Health;
        CurrentArmor = data.Armor;
        CurrentShield = data.Shield;
        CurrentShieldArmor = data.ShieldArmor;
        CurrentBurn = data.Burn;
        CurrentOverstrike = data.Overstrike;

        if (data.AbilityPassives != null)
        {
            foreach (AbilityPassive passive
                in data.AbilityPassives)
            {
                AddPassive(passive);
            }
        }
    }

    public bool HasPassive(AbilityPassive passive)
    {
        return currentPassives.Contains(passive);
    }

    public void AddPassive(AbilityPassive passive)
    {
        if (passive == AbilityPassive.None ||
            HasPassive(passive))
            return;

        currentPassives.Add(passive);
    }

    public void RemovePassive(AbilityPassive passive)
    {
        currentPassives.Remove(passive);
    }

    public void ModifyAttack(int amount)
    {
        CurrentAttack = Mathf.Max(
            0, CurrentAttack + amount);
    }

    public void ModifyHealth(int amount)
    {
        CurrentHealth = Mathf.Max(
            1, CurrentHealth + amount);
    }
}
