using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.Core.Decks;

/// <summary>デッキがルール(枚数・同名カードの上限・存在するカードか)に合っているかを検証する。</summary>
public static class ValidateDeck
{
    /// <returns>ルールに合っていない理由。合っていれば空。</returns>
    public static IReadOnlyList<DeckError> Execute(DeckList deck, CardCatalog catalog, GameRulesOptions rules)
    {
        List<DeckError> errors = [];

        foreach (var group in deck.Entries.GroupBy(e => e.CardId))
        {
            if (group.Any(e => e.Count <= 0)) errors.Add(new DeckError(DeckErrorKind.InvalidCount, group.Key));
            if (catalog.Find(group.Key) is null) errors.Add(new DeckError(DeckErrorKind.UnknownCard, group.Key));
            if (group.Sum(e => e.Count) > rules.MaxCopiesPerCard) errors.Add(new DeckError(DeckErrorKind.TooManyCopies, group.Key));
        }

        var total = deck.TotalCount;
        if (total < rules.MinDeckSize) errors.Add(new DeckError(DeckErrorKind.TooFewCards));
        if (total > rules.MaxDeckSize) errors.Add(new DeckError(DeckErrorKind.TooManyCards));

        return errors;
    }
}
