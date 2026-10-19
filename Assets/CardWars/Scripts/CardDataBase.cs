using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDataBase", menuName = "CardWars/CardDataBase")]
public class CardDataBase : SerializedScriptableObject
{

    [Header("CARD DATA")]
    [SerializeField] private Dictionary<int, CardData> cardsByID;
    [SerializeField] private Dictionary<CardRarity, List<CardData>> cardsByQuality;
    [SerializeField] private Dictionary<CardFamily, List<CardData>> cardsByFamily;
    [SerializeField] private Dictionary<int, List<CardData>> cardsByCost;



    [Header("TROOPS CARDS")]
    [SerializeField] private Dictionary<int, TroopCardData> troopCardByID;
    [SerializeField] private Dictionary<CardRarity, List<TroopCardData>> troopCardsByQuality;
    [SerializeField] private Dictionary<CardFamily, List<TroopCardData>> troopCardsByFamily;
    [SerializeField] private Dictionary<int, List<TroopCardData>> troopCardsByCost;

    [Header("SPELL CARDS")]
    [SerializeField] private Dictionary<int, SpellCardData> spellCardByID;
    [SerializeField] private Dictionary<CardRarity, List<SpellCardData>> spellCardsByQuality;
    [SerializeField] private Dictionary<CardFamily, List<SpellCardData>> spellCardsByFamily;
    [SerializeField] private Dictionary<int, List<SpellCardData>> spellCardsByCost;

    [Header("DOMAIN CARDS")]
    [SerializeField] private Dictionary<int, DomainCardData> domainCardByID;
    [SerializeField] private Dictionary<CardRarity, List<DomainCardData>> domainCardsByQuality;
    [SerializeField] private Dictionary<CardFamily, List<DomainCardData>> domainCardsByFamily;
    [SerializeField] private Dictionary<int, List<DomainCardData>> domainCardsByCost;


    public Dictionary<int, CardData> CardsByID => cardsByID;
    public Dictionary<CardRarity, List<CardData>> CardsByQuality => cardsByQuality;
    public Dictionary<CardFamily, List<CardData>> CardsByFamily => cardsByFamily;
    public Dictionary<int, List<CardData>> CardsByCost => cardsByCost;


    public Dictionary<int, TroopCardData> TroopCardByID => troopCardByID;
    public Dictionary<CardRarity, List<TroopCardData>> TroopCardsByQuality => troopCardsByQuality;
    public Dictionary<CardFamily, List<TroopCardData>> TroopCardsByFamily => troopCardsByFamily;
    public Dictionary<int, List<TroopCardData>> TroopCardsByCost => troopCardsByCost;


    public Dictionary<int, SpellCardData> SpellCardByID => spellCardByID;


    public Dictionary<CardRarity, List<SpellCardData>> SpellCardsByQuality => spellCardsByQuality;
    public Dictionary<CardFamily, List<SpellCardData>> SpellCardsByFamily => spellCardsByFamily;
    public Dictionary<int, List<SpellCardData>> SpellCardsByCost => spellCardsByCost;

    public Dictionary<int, DomainCardData> DomainCardByID => domainCardByID;
    public Dictionary<CardRarity, List<DomainCardData>> DomainCardsByQuality => domainCardsByQuality;
    public Dictionary<CardFamily, List<DomainCardData>> DomainCardsByFamily => domainCardsByFamily;
    public Dictionary<int, List<DomainCardData>> DomainCardsByCost => domainCardsByCost;
    

}
