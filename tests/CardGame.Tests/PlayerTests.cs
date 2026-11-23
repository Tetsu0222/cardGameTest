using CardGame.Core;

namespace CardGame.Tests;

public class PlayerTests
{
    private static Player CreatePlayer(int deckSize)
    {
        var deck = Enumerable.Range(1, deckSize)
            .Select(i => new CardData(i, $"Card{i}", CardKind.NormalMonster, 4, 1000, 1000));
        return new Player("P1", deck);
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
    public void TakeDamage_NeverGoesBelowZero()
    {
        var p = CreatePlayer(0);
        p.TakeDamage(9000);
        Assert.Equal(0, p.LifePoints);
    }
}
