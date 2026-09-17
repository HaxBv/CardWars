using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpellCardData", menuName = "CardWars/SpellCardData")]
[InlineEditor]
public class SpellCardData : CardData
{

    [SerializeField] private List<AbilityData> effects;

    public List<AbilityData> Effects => effects;
}
