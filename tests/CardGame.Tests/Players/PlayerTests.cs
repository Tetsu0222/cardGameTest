using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Tests.Players;

public class PlayerTests
{
    private static Player CreatePlayer(int deckSize, GameRulesOptions? rules = null)
    {
        var deck = Enumerable.Range(1, deckSize)
            .Select(i => new CardData(i, $"Card{i}", CardKind.NormalMonster, 4, 1000, 1000));
        return new Player("P1", deck, rules ?? new GameRulesOptions());
    }

    [Fact]
    public void Constructor_SetsLifePoints_FromRules()
    {
        var p = CreatePlayer(0, new GameRulesOptions { InitialLifePoints = 4000 });
        Assert.Equal(4000, p.LifePoints);
    }

    [Fact]
    public void Draw_MovesTopCardToHand()
    {
        var p = CreatePlayer(3);
        Assert.True(p.Draw());
        Assert.Single(p.Hand);
        Assert.Equal(2, p.Deck.Count);
    }

    [Fact]
    public void Draw_ReturnsFalse_WhenDeckEmpty()
    {
        var p = CreatePlayer(0);
        Assert.False(p.Draw());
    }

    [Fact]
    public void TakeDamage_NeverGoesBelowZero_WhenDamageExceedsLifePoints()
    {
        var p = CreatePlayer(0);
        p.TakeDamage(p.LifePoints + 1000);
        Assert.Equal(0, p.LifePoints);
    }
}
