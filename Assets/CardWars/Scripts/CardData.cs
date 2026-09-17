using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AbilityData
{
    [SerializeField] private TriggerType trigger;

    [SerializeField] private FilterType filter;

    [SerializeField] private TargetType target;
    [SerializeField] private ConditionType condition;


    [SerializeField] private CardFamily family;

    [SerializeField] private int laneRadius;

    [SerializeField] private List<EffectData> effects;

    public TriggerType Trigger => trigger;

    public FilterType Filter => filter;

    public TargetType Target => target;

    public ConditionType Condition => condition;

    public CardFamily Family => family;

    public List<EffectData> Effects => effects;
    public int LaneRadius => laneRadius;




}

[System.Serializable]
public class EffectData
{
    [SerializeField] private EffectType effectType;
    [SerializeField] private int value;
    public EffectType EffectType => effectType;
    public int Value => value;
}


[CreateAssetMenu(fileName = "CardData", menuName = "CardWars/CardData")]
public class CardData : ScriptableObject
{
    [SerializeField] private string cardID;
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
    public string CardID => cardID;
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
