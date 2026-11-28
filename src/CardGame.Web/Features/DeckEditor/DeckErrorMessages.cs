using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;

namespace CardGame.Web.Features.DeckEditor;

/// <summary>デッキがルールに合っていない理由を画面表示用の文言に変換する。</summary>
public static class DeckErrorMessages
{
    public static string ToMessage(DeckError error, CardCatalog catalog, GameRulesOptions rules, int totalCount) => error.Kind switch
    {
        DeckErrorKind.TooFewCards => $"デッキは{rules.MinDeckSize}枚以上必要です(あと{rules.MinDeckSize - totalCount}枚)。",
        DeckErrorKind.TooManyCards => $"デッキは{rules.MaxDeckSize}枚までです({totalCount - rules.MaxDeckSize}枚多い)。",
        DeckErrorKind.TooManyCopies => $"「{CardName(error.CardId, catalog)}」は{rules.MaxCopiesPerCard}枚までです。",
        DeckErrorKind.UnknownCard => $"存在しないカード(ID {error.CardId})が入っています。",
        DeckErrorKind.InvalidCount => $"「{CardName(error.CardId, catalog)}」の枚数が正しくありません。",
        _ => error.Kind.ToString(),
    };

    private static string CardName(int? cardId, CardCatalog catalog) =>
        cardId is { } id && catalog.Find(id) is { } card ? card.Name : $"ID {cardId}";
}
