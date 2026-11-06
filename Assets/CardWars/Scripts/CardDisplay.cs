
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardDisplay : MonoBehaviour
{
    [Header("DATABASE")]
    public CardDataBase DataBase;

    // Solo para pruebas desde el Inspector.
    public TroopCardData cardDataSO;

    // Instancia real de la carta durante la partida.
    public CardInstance CardInstance { get; private set; }

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

    private void Start()
    {
        // No sobrescribir cartas asignadas por DeckHandManager.
        if (CardInstance != null)
        {
            AplicarData();
            return;
        }

        if (cardDataSO != null)
            MostrarTropa(cardDataSO);
        else
            MostrarTropaAleatoria();
    }

    public void SetCard(CardInstance card)
    {
        CardInstance = card;
        cardDataSO = card?.CardData as TroopCardData;

        AplicarData();
    }

    public void ClearCard()
    {
        CardInstance = null;
        cardDataSO = null;
    }

    [Button]
    public void MostrarTropaAleatoria()
    {
        if (DataBase == null ||
            DataBase.TroopCardByID == null ||
            DataBase.TroopCardByID.Count == 0)
            return;

        List<int> ids = new(
            DataBase.TroopCardByID.Keys);

        int randomID = ids[
            Random.Range(0, ids.Count)];

        MostrarTropa(
            DataBase.TroopCardByID[randomID]);
    }

    public void MostrarTropa(TroopCardData data)
    {
        if (data == null)
            return;

        SetCard(CardInstanceFactory.Create(data));
    }

    private void AplicarRareza(CardData data)
    {
        if (data == null ||
            CardDesing == null ||
            RaritySprites == null)
            return;

        int index = (int)data.Rarity;

        if (index >= 0 &&
            index < RaritySprites.Count &&
            RaritySprites[index] != null)
        {
            CardDesing.sprite = RaritySprites[index];
        }
    }

    [Button]
    public void AplicarData()
    {
        if (CardInstance == null)
        {
            // Permitir pruebas desde el Inspector.
            if (cardDataSO == null)
                return;

            CardInstance =
                CardInstanceFactory.Create(cardDataSO);
        }

        CardData data = CardInstance.CardData;

        if (CardName != null)
            CardName.text = data.CardName;

        if (CardCost != null)
            CardCost.text =
                CardInstance.CurrentCost.ToString();

        if (CardWeight != null)
            CardWeight.text =
                CardInstance.CurrentWeight.ToString();

        if (CardDescription != null)
            CardDescription.text =
                CardInstance.CurrentDescription;

        if (CardImg != null)
            CardImg.sprite = data.ImageCard;

        // Estadísticas exclusivas de tropas.
        if (CardInstance is TroopCardInstance troop)
        {
            if (CardAttack != null)
            {
                CardAttack.gameObject.SetActive(true);
                CardAttack.text =
                    troop.CurrentAttack.ToString();
            }

            if (CardHealth != null)
            {
                CardHealth.gameObject.SetActive(true);
                CardHealth.text =
                    troop.CurrentHealth.ToString();
            }
        }
        else
        {
            // Los hechizos y dominios no tienen ATK/HP.
            if (CardAttack != null)
                CardAttack.gameObject.SetActive(false);

            if (CardHealth != null)
                CardHealth.gameObject.SetActive(false);
        }

        AplicarRareza(data);
    }

    [Button]
    public void MostrarTropaPorID(int id)
    {
        if (DataBase == null ||
            DataBase.TroopCardByID == null)
            return;

        if (DataBase.TroopCardByID.TryGetValue(
            id, out TroopCardData data))
        {
            MostrarTropa(data);
        }
    }
}
