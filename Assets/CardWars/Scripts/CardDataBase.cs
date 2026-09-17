using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardDataBase", menuName = "CardWars/CardDataBase")]
public class CardDataBase : SerializedScriptableObject
{
    [SerializeField] private Dictionary<string, CardData> cardsByID;
    [SerializeField] private Dictionary<CardRarity, List<CardData>> cardsByQuality;
    [SerializeField] private Dictionary<CardFamily, List<CardData>> cardsByFamily;
    [SerializeField] private Dictionary<int, List<CardData>> cardsByCost;



    public Dictionary<string, CardData> CardsByID => cardsByID;
    public Dictionary<CardRarity, List<CardData>> CardsByQuality => cardsByQuality;
    public Dictionary<CardFamily, List<CardData>> CardsByFamily => cardsByFamily;
    public Dictionary<int, List<CardData>> CardsByCost => cardsByCost;
}
