
using System;
using UnityEngine;

[Serializable]
public class CardInstance
{
    public CardData CardData { get; private set; }

    public int CurrentCost { get; private set; }
    public int CurrentWeight { get; private set; }

    public string CurrentDescription { get; private set; }

    public string CurrentPhrase { get; private set; }
    public CardInstance(CardData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        CardData = data;

        CurrentCost = data.Cost;
        CurrentWeight = data.Weight;
        CurrentDescription = data.DescriptionAbility;
        CurrentPhrase = data.Phrase;
    }

    public void ModifyCost(int amount)
    {
        CurrentCost = Mathf.Max(
            0, CurrentCost + amount);
    }

    public void ModifyWeight(int amount)
    {
        CurrentWeight = Mathf.Max(
            0, CurrentWeight + amount);
    }

    public void SetDescription(string description)
    {
        CurrentDescription = description;
    }

    public void SetPhrase(string phrase)
    {
        CurrentPhrase = phrase;
    }
}
