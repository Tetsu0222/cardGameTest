using CardGame.Core.Cards;

namespace CardGame.Core.Decks;

/// <summary>デッキ構成から、対戦で使うカードの並び(シャッフル前)を作る。</summary>
public static class BuildDeck
{
    /// <exception cref="InvalidOperationException">存在しないカードが入っている場合(<see cref="ValidateDeck"/> で検証してから呼ぶ)。</exception>
    public static IReadOnlyList<CardData> Execute(DeckList deck, CardCatalog catalog) =>
    [
        .. deck.Entries.SelectMany(entry =>
        {
            var card = catalog.Find(entry.CardId)
                ?? throw new InvalidOperationException($"Unknown card id in deck: {entry.CardId}");
            return Enumerable.Repeat(card, Math.Max(0, entry.Count));
        }),
    ];
}
