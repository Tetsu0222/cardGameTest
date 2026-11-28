using CardGame.Core.Decks;

namespace CardGame.Core.Cards;

/// <summary>
/// ゲームで使えるすべてのカードの定義と、初期デッキ。<see cref="CardCatalogJson"/> で JSON から作る。
/// </summary>
public sealed class CardCatalog
{
    private readonly Dictionary<int, CardData> _cardsById;

    /// <exception cref="ArgumentException">カード ID が重複している場合。</exception>
    public CardCatalog(IReadOnlyList<CardData> cards, DeckList defaultDeck)
    {
        var duplicated = cards.GroupBy(c => c.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new ArgumentException($"Duplicate card id: {duplicated.Key}", nameof(cards));
        }

        Cards = cards;
        DefaultDeck = defaultDeck;
        _cardsById = cards.ToDictionary(c => c.Id);
    }

    /// <summary>すべてのカード(定義順)。</summary>
    public IReadOnlyList<CardData> Cards { get; }

    /// <summary>初期デッキ。デッキを保存していないプレイヤーと CPU が使う。</summary>
    public DeckList DefaultDeck { get; }

    /// <summary>カード ID からカードを探す。なければ null。</summary>
    public CardData? Find(int cardId) => _cardsById.GetValueOrDefault(cardId);

    /// <summary>カードを加えた新しいカードデータを作る(組み込みのカードに、プレイヤーが作ったカードを足すときなど)。</summary>
    /// <exception cref="ArgumentException">カード ID が重複する場合。</exception>
    public CardCatalog WithAdditionalCards(IEnumerable<CardData> cards) => new([.. Cards, .. cards], DefaultDeck);
}
