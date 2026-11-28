namespace CardGame.Core.Cards;

/// <summary>新しく作るカードの ID を決める。使われている自作カードの ID の最大値の次(なければ最初の番号)。</summary>
public static class IssueCustomCardId
{
    public static int Execute(IEnumerable<CardData> existingCards) =>
        existingCards
            .Select(c => c.Id)
            .Where(ValidateCustomCard.IsCustomCardId)
            .DefaultIfEmpty(ValidateCustomCard.FirstCustomCardId - 1)
            .Max() + 1;
}
