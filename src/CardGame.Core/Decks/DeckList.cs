namespace CardGame.Core.Decks;

/// <summary>
/// デッキの構成(どのカードを何枚入れるか)。カードの定義そのものは持たず、カード ID で参照する。
/// 対戦で使うカードの並びは <see cref="BuildDeck"/> で作る。
/// </summary>
public sealed record DeckList(string Name, IReadOnlyList<DeckEntry> Entries)
{
    /// <summary>デッキの合計枚数。</summary>
    public int TotalCount => Entries.Sum(e => e.Count);

    /// <summary>指定したカードの枚数。入っていなければ 0。</summary>
    public int CountOf(int cardId) => Entries.Where(e => e.CardId == cardId).Sum(e => e.Count);
}
