using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TroopCardData", menuName = "CardWars/TroopCardData")]
[InlineEditor]
public class TroopCardData : CardData
{
    [SerializeField] private int attack;
    [SerializeField] private int health;
    [SerializeField] private int armor;
    [SerializeField] private int shield;
    [SerializeField] private int shieldArmor;
    [SerializeField] private int burn;
    [SerializeField] private int directAttack;

    [SerializeField] private List<AbilityPassive> abilityPassives;

    public List<AbilityPassive> AbilityPassives => abilityPassives;
    public int Attack => attack;
    public int Health => health;
    public int Armor => armor;
    public int Shield => shield;
    public int ShieldArmor => shieldArmor;
    public int Burn => burn;
    public int DirectAttack => directAttack;

}
