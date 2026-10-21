
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDisplay : MonoBehaviour
{
    [Header("DATABASE")]
    public CardDataBase DataBase;
    public TroopCardData cardDataSO;

    [Header("CARD DATA")]
    public TextMeshProUGUI CardName;
    public TextMeshProUGUI CardCost;
    public TextMeshProUGUI CardWeight;
    public TextMeshProUGUI CardDescription;
    public GameObject CardDescriptionPanel;
    public TextMeshProUGUI CardAttack;
    public TextMeshProUGUI CardHealth;

    public Image CardImg;

    [Header("CARD RARITY DESIGN")]
    public Image CardDesing;
    public List<Sprite> RaritySprites;


    [Header("CARD STATE")]
    public GameObject DisabledOverlay;

    private void Start()
    {
        if (cardDataSO == null)
            MostrarTropaAleatoria();
        else
            AplicarData();
    }

    [Button]
    public void MostrarTropaAleatoria()
    {
        if (DataBase == null || DataBase.TroopCardByID == null ||
            DataBase.TroopCardByID.Count == 0)
            return;

        List<int> ids = new List<int>(DataBase.TroopCardByID.Keys);

        int randomID = ids[Random.Range(0, ids.Count)];

        MostrarTropa(DataBase.TroopCardByID[randomID]);
    }

    public void MostrarTropa(TroopCardData data)
    {
        cardDataSO = data;
        AplicarData();
    }
    private void AplicarRareza()
    {
        int index = (int)cardDataSO.Rarity;

        if (index >= 0 && index < RaritySprites.Count &&
            RaritySprites[index] != null)
        {
            CardDesing.sprite = RaritySprites[index];
        }
    }
    [Button]
    public void AplicarData()
    {
        if (cardDataSO == null)
            return;

        var data = cardDataSO;

        CardName.text = data.CardName;
        CardCost.text = data.Cost.ToString();
        CardWeight.text = data.Weight.ToString();
        CardDescription.text = data.DescriptionAbility;
        CardAttack.text = data.Attack.ToString();
        CardHealth.text = data.Health.ToString();

        if (data.ImageCard != null)
            CardImg.sprite = data.ImageCard;
        AplicarRareza();
    }

    [Button]
    public void MostrarTropaPorID(int id)
    {
        if (DataBase == null || DataBase.TroopCardByID == null)
            return;

        if (DataBase.TroopCardByID.TryGetValue(id, out TroopCardData data))
            MostrarTropa(data);
    }
}
