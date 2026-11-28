namespace CardGame.Core.Decks;

/// <summary>デッキがルールに合っていない理由。</summary>
public enum DeckErrorKind
{
    /// <summary>枚数が最小枚数に足りない。</summary>
    TooFewCards,

    /// <summary>枚数が最大枚数を超えている。</summary>
    TooManyCards,

    /// <summary>同じカードが上限を超えて入っている。</summary>
    TooManyCopies,

    /// <summary>存在しないカードが入っている。</summary>
    UnknownCard,

    /// <summary>枚数が 0 以下の項目がある。</summary>
    InvalidCount,
}

/// <param name="CardId">問題のあるカード。デッキ全体の問題なら null。</param>
public sealed record DeckError(DeckErrorKind Kind, int? CardId = null);
