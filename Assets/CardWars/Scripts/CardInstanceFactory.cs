
public static class CardInstanceFactory
{
    public static CardInstance Create(CardData data)
    {
        if (data == null)
            return null;

        if (data is TroopCardData troopData)
            return new TroopCardInstance(troopData);

        return new CardInstance(data);
    }
}
