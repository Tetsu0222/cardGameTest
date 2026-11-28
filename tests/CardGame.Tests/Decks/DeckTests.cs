using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;

namespace CardGame.Tests.Decks;

/// <summary>デッキの検証・組み立て・保存形式。</summary>
public class DeckTests
{
    private static readonly GameRulesOptions Rules = new();

    /// <summary>ID 1〜20 の通常モンスターだけを持つカードデータ。</summary>
    private static readonly CardCatalog Catalog = new(
        [.. Enumerable.Range(1, 20).Select(id => new CardData(id, $"M{id}", CardKind.NormalMonster, 4, 100 * id, 100))],
        new DeckList("Default", []));

    /// <summary>ID 1 から順に、各カードを指定枚数ずつ入れたデッキ。</summary>
    private static DeckList DeckOf(int kinds, int copies) =>
        new("Deck", [.. Enumerable.Range(1, kinds).Select(id => new DeckEntry(id, copies))]);

    // ---- 検証 ----

    [Theory]
    [InlineData(20, 2)] // 40枚(最小)
    [InlineData(20, 3)] // 60枚(最大)
    public void Validate_Accepts_WhenWithinLimits(int kinds, int copies)
    {
        Assert.Empty(ValidateDeck.Execute(DeckOf(kinds, copies), Catalog, Rules));
    }

    [Fact]
    public void Validate_ReportsTooFewCards()
    {
        var deck = new DeckList("Deck", [.. DeckOf(19, 2).Entries, new DeckEntry(20, 1)]); // 39枚

        Assert.Equal([new DeckError(DeckErrorKind.TooFewCards)], ValidateDeck.Execute(deck, Catalog, Rules));
    }

    [Fact]
    public void Validate_ReportsTooManyCards()
    {
        var bigCatalog = new CardCatalog(
            [.. Enumerable.Range(1, 21).Select(id => new CardData(id, $"M{id}", CardKind.NormalMonster, 4))],
            new DeckList("Default", []));
        var deck = new DeckList("Deck", [.. DeckOf(20, 3).Entries, new DeckEntry(21, 1)]); // 61枚

        Assert.Equal([new DeckError(DeckErrorKind.TooManyCards)], ValidateDeck.Execute(deck, bigCatalog, Rules));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReportsInvalidCount(int count)
    {
        var deck = new DeckList("Deck", [.. DeckOf(20, 2).Entries, new DeckEntry(1, count)]);

        Assert.Contains(new DeckError(DeckErrorKind.InvalidCount, 1), ValidateDeck.Execute(deck, Catalog, Rules));
    }

    [Fact]
    public void Validate_ReportsTooManyCopies_IncludingSplitEntries()
    {
        // 同じカードが別々の項目に分かれていても合計で数える
        var deck = new DeckList("Deck", [.. DeckOf(20, 2).Entries, new DeckEntry(5, 2)]);

        Assert.Contains(new DeckError(DeckErrorKind.TooManyCopies, 5), ValidateDeck.Execute(deck, Catalog, Rules));
    }

    [Fact]
    public void Validate_ReportsUnknownCard()
    {
        var deck = new DeckList("Deck", [.. DeckOf(20, 2).Entries, new DeckEntry(999, 1)]);

        Assert.Contains(new DeckError(DeckErrorKind.UnknownCard, 999), ValidateDeck.Execute(deck, Catalog, Rules));
    }

    [Fact]
    public void Validate_UsesConfiguredLimits()
    {
        var rules = new GameRulesOptions { MinDeckSize = 10, MaxDeckSize = 10, MaxCopiesPerCard = 1 };

        Assert.Empty(ValidateDeck.Execute(DeckOf(10, 1), Catalog, rules));
        Assert.Contains(new DeckError(DeckErrorKind.TooManyCopies, 1), ValidateDeck.Execute(DeckOf(5, 2), Catalog, rules));
    }

    // ---- 組み立て ----

    [Fact]
    public void Build_ExpandsEntriesToCards()
    {
        var deck = new DeckList("Deck", [new DeckEntry(1, 2), new DeckEntry(3, 1)]);

        var cards = BuildDeck.Execute(deck, Catalog);

        Assert.Equal([1, 1, 3], cards.Select(c => c.Id));
    }

    [Fact]
    public void Build_Throws_WhenUnknownCard()
    {
        var deck = new DeckList("Deck", [new DeckEntry(999, 1)]);

        Assert.Throws<InvalidOperationException>(() => BuildDeck.Execute(deck, Catalog));
    }

    // ---- 合計・枚数 ----

    [Fact]
    public void TotalCountAndCountOf_SumEntries()
    {
        var deck = new DeckList("Deck", [new DeckEntry(1, 2), new DeckEntry(2, 3), new DeckEntry(1, 1)]);

        Assert.Equal(6, deck.TotalCount);
        Assert.Equal(3, deck.CountOf(1));
        Assert.Equal(0, deck.CountOf(99));
    }

    // ---- 保存形式 ----

    [Fact]
    public void Json_RoundTrips()
    {
        var deck = new DeckList("マイデッキ", [new DeckEntry(1, 3), new DeckEntry(102, 2)]);

        var restored = DeckListJson.TryParse(DeckListJson.Serialize(deck));

        Assert.NotNull(restored);
        Assert.Equal(deck.Name, restored.Name);
        Assert.Equal(deck.Entries, restored.Entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("broken")]
    [InlineData("""{ "name": "Deck" }""")]
    [InlineData("""{ "entries": [] }""")]
    [InlineData("null")]
    public void Json_TryParse_ReturnsNull_WhenBrokenOrIncomplete(string json)
    {
        Assert.Null(DeckListJson.TryParse(json));
    }
}
