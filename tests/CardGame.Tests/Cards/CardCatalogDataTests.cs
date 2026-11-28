using CardGame.Core.Cards;
using CardGame.Core.Decks;
using CardGame.Core.Duels;
using CardGame.Core.Summons;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Cards;

/// <summary>配布するカードデータ(cards.json)の中身が、ゲームで使える状態になっているかの確認。</summary>
public class CardCatalogDataTests
{
    private static readonly CardCatalog Catalog = TestCatalog.Load();

    [Fact]
    public void AllCards_AreValidDefinitions()
    {
        Assert.All(Catalog.Cards, card => Assert.True(ValidateCardData.Execute(card), $"{card.Id} {card.Name}"));
    }

    [Fact]
    public void CardNames_AreUnique()
    {
        Assert.Equal(Catalog.Cards.Count, Catalog.Cards.Select(c => c.Name).Distinct().Count());
    }

    [Fact]
    public void DefaultDeck_FollowsDeckRules()
    {
        Assert.Empty(ValidateDeck.Execute(Catalog.DefaultDeck, Catalog, new GameRulesOptions()));
    }

    [Fact]
    public void DefaultDeck_Has40Cards()
    {
        Assert.Equal(40, Catalog.DefaultDeck.TotalCount);
    }

    [Theory]
    [InlineData(CardKind.NormalMonster)]
    [InlineData(CardKind.Spell)]
    [InlineData(CardKind.Trap)]
    public void DefaultDeck_ContainsEachCardKind(CardKind kind)
    {
        Assert.Contains(TestCatalog.DefaultDeckCards(), c => c.Kind == kind);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DefaultDeck_ContainsMonstersForEachTributeCount(int tributeCount)
    {
        var monsters = TestCatalog.DefaultDeckCards().Where(c => c.Kind == CardKind.NormalMonster);

        Assert.Contains(monsters, card => CountRequiredTributes.Execute(card) == tributeCount);
    }
}
