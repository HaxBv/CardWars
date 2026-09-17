using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentCardData", menuName = "CardWars/EnvironmentCardData")]
[InlineEditor]
public class EnvironmentCardData : ScriptableObject
{
    [SerializeField] private List<AbilityData> passiveEffects;

    public List<AbilityData> PassiveEffects => passiveEffects;
}
