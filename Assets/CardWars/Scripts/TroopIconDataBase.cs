
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TroopIconCategory
{
    Offensive,
    Defensive,
    Utility
}

[CreateAssetMenu(
    fileName = "TroopIconDatabase",
    menuName = "CardWars/Troop Icon Database"
)]
public class TroopIconDatabase : ScriptableObject
{
    [Serializable]
    public class PassiveIcon
    {
        public AbilityPassive Passive;
        public TroopIconCategory Category;
        [PreviewField(100)]
        public Sprite Icon;
    }

    [Header("DEFAULT ICONS")]
    [PreviewField(100)]
    public Sprite DefaultAttack;
    [PreviewField(100)]
    public Sprite DefaultHealth;

    [Header("MULTIPLE PASSIVES - OPTIONAL")]
    [PreviewField(100)]
    public Sprite MultipleOffensive;
    [PreviewField(100)]
    public Sprite MultipleDefensive;

    [Header("PASSIVE ICONS")]
    public List<PassiveIcon> PassiveIcons = new();

    public bool TryGetPassive(
        AbilityPassive passive,
        out PassiveIcon result)
    {
        result = PassiveIcons.Find(
            entry => entry.Passive == passive
        );

        return result != null;
    }

    public Sprite ResolveIcon(
        IReadOnlyList<AbilityPassive> passives,
        TroopIconCategory category)
    {
        int count = 0;
        Sprite selected = null;

        if (passives != null)
        {
            foreach (AbilityPassive passive in passives)
            {
               
                if (IsExtraStat(passive))
                    continue;

                if (!TryGetPassive(passive, out PassiveIcon entry))
                    continue;

                if (entry.Category != category)
                    continue;

                count++;
                selected = entry.Icon;
            }
        }

        if (count == 0)
        {
            return category == TroopIconCategory.Offensive
                ? DefaultAttack
                : DefaultHealth;
        }

        if (count == 1)
            return selected;

        return category == TroopIconCategory.Offensive
            ? MultipleOffensive
            : MultipleDefensive;
    }

    private bool IsExtraStat(AbilityPassive passive)
    {
        string name = passive.ToString();

        return name == "Overstrike"
            || name == "Shield"
            || name == "ShieldArmored"
            || name == "ShieldArmor"
            || name == "Armored"
            || name == "Burn";
    }
}
