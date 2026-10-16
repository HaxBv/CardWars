using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[System.Serializable]
public class AbilityData
{
    [SerializeField] private TriggerType trigger;

    [SerializeField] private FilterType filter;


    [SerializeField] private ConditionType condition;

    [SerializeField] private int laneRadius;

    [SerializeField] private StatType statFilter;
    [SerializeField] private ComparisonType comparison;
    [SerializeField] private int compareValue;


    [SerializeField] private List<EffectData> effects;

    public TriggerType Trigger => trigger;

    public FilterType Filter => filter;


    public ConditionType Condition => condition;
    public int LaneRadius => laneRadius;

    public StatType StatFilter => statFilter;
    public ComparisonType Comparison => comparison;
    public int CompareValue => compareValue;

    public List<EffectData> Effects => effects;




}

[System.Serializable]
public class EffectData
{

    [SerializeField] private EffectType effectType;

    [SerializeField] private EffectDuration duration;
    [SerializeField] private TargetType target;

    [SerializeField] private int laneRadius;
    [SerializeField] private CardFamily familyTarget;
    [SerializeField] private CardRarity rarityTarget;
    [SerializeField] private LaneDestination laneDestination;
    [SerializeField] private int value;
    [SerializeField] private int quantity;

    [SerializeField] private List<AbilityPassive> abilitiesToGrant;



    public EffectType EffectType => effectType;
    public EffectDuration Duration => duration;
    public TargetType Target => target;

    public int LaneRadius => laneRadius;
    public CardFamily FamilyTarget => familyTarget;
    public CardRarity RarityTarget => rarityTarget;
    public LaneDestination LaneDestination => laneDestination;
    public int Value => value;
    public int Quantity => quantity;

    public List<AbilityPassive> AbilitiesToGrant => abilitiesToGrant;
}


[CreateAssetMenu(fileName = "CardData", menuName = "CardWars/CardData")]
public class CardData : ScriptableObject
{
    [SerializeField] private string cardName;
    [SerializeField] private int cost;
    [SerializeField, TextArea(3, 10)] private string descriptionAbility;
    [SerializeField, TextArea(3, 10)] private string phrase;

    [SerializeField] private CardType type;
    [SerializeField] private CardRarity rarity;
    [SerializeField] private List<CardFamily> family;

    [PreviewField(100)]
    [SerializeField] private Sprite imgType;
    [PreviewField(100)]
    [SerializeField] private Sprite imgRarity;
    [PreviewField(100)]
    [SerializeField] private Sprite imgCard;

    [SerializeField] private List<AbilityData> abilities;
    public string CardName => cardName;
    public int Cost => cost;
    public string DescriptionAbility => descriptionAbility;
    public string Phrase => phrase;
    public CardType Type => type;
    public CardRarity Rarity => rarity;
    public List<CardFamily> Family => family;
    public Sprite ImageType => imgType;
    public Sprite ImageRarity => imgRarity;
    public Sprite ImageCard => imgCard;
    public List<AbilityData> Abilities => abilities;


}
