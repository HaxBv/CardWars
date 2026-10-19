
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

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

    public TextMeshProUGUI CardAttack;
    public TextMeshProUGUI CardHealth;

    public Image CardImg;

    private void Start()
    {
        MostrarTropaAleatoria();
    }

    [Button]
    public void MostrarTropaAleatoria()
    {
        if (DataBase == null || DataBase.TroopCardByID == null ||
            DataBase.TroopCardByID.Count == 0)
        {
            Debug.LogWarning("No hay tropas disponibles en la base de datos.");
            return;
        }

        // Obtener todos los IDs registrados
        List<int> ids = new List<int>(DataBase.TroopCardByID.Keys);

        // Escoger un ID aleatorio
        int randomIndex = Random.Range(0, ids.Count);
        int randomID = ids[randomIndex];

        // Buscar la tropa correspondiente
        cardDataSO = DataBase.TroopCardByID[randomID];

        Debug.Log($"Tropa seleccionada: {cardDataSO.CardName} | ID: {randomID}");

        AplicarData();
    }

    [Button]
    public void AplicarData()
    {
        if (cardDataSO == null)
        {
            Debug.LogWarning("No se ha asignado un TroopCardData.");
            return;
        }

        var Data = cardDataSO;

        CardName.text = Data.CardName;
        CardCost.text = Data.Cost.ToString();
        CardWeight.text = Data.Weight.ToString();
        CardDescription.text = Data.DescriptionAbility;

        CardAttack.text = Data.Attack.ToString();
        CardHealth.text = Data.Health.ToString();

        if(Data.ImageCard != null)
        CardImg.sprite = Data.ImageCard;
    }
}
