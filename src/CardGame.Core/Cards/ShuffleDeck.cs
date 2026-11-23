namespace CardGame.Core.Cards;

/// <summary>デッキのシャッフル。乱数は呼び出し側から受け取る(テストでシード固定するため)。</summary>
public static class ShuffleDeck
{
    public static IReadOnlyList<CardData> Execute(IEnumerable<CardData> cards, Random random)
    {
        CardData[] shuffled = [.. cards];
        random.Shuffle(shuffled);
        return shuffled;
    }
}
