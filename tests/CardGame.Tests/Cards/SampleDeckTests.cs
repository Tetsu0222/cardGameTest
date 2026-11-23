using CardGame.Core.Cards;

namespace CardGame.Tests.Cards;

public class SampleDeckTests
{
    private const int DeckSize = 40;
    private const int MaxCopiesPerCard = 3;
    private const int MaxLevelWithoutTribute = 4;

    [Fact]
    public void Create_Returns40Cards()
    {
        Assert.Equal(DeckSize, SampleDeck.Create().Count);
    }

    [Fact]
    public void Create_HasAtMostThreeCopiesOfEachCard()
    {
        var maxCopies = SampleDeck.Create().GroupBy(c => c.Id).Max(g => g.Count());

        Assert.True(maxCopies <= MaxCopiesPerCard);
    }

    [Fact]
    public void Create_ContainsOnlyMonstersSummonableWithoutTribute()
    {
        Assert.All(SampleDeck.Create(), card =>
        {
            Assert.Equal(CardKind.NormalMonster, card.Kind);
            Assert.InRange(card.Level, 1, MaxLevelWithoutTribute);
        });
    }

    [Fact]
    public void Create_UsesUniqueIdPerCardName()
    {
        var namesPerId = SampleDeck.Create().GroupBy(c => c.Id).Select(g => g.Select(c => c.Name).Distinct().Count());

        Assert.All(namesPerId, count => Assert.Equal(1, count));
    }
}
