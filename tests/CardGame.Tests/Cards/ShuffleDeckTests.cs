using CardGame.Core.Cards;

namespace CardGame.Tests.Cards;

public class ShuffleDeckTests
{
    private const int Seed = 42;

    [Fact]
    public void Execute_KeepsSameCards()
    {
        var deck = SampleDeck.Create();

        var shuffled = ShuffleDeck.Execute(deck, new Random(Seed));

        Assert.Equal(deck.OrderBy(c => c.Id), shuffled.OrderBy(c => c.Id));
    }

    [Fact]
    public void Execute_ReturnsSameOrder_WhenSameSeed()
    {
        var deck = SampleDeck.Create();

        var first = ShuffleDeck.Execute(deck, new Random(Seed));
        var second = ShuffleDeck.Execute(deck, new Random(Seed));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Execute_DoesNotModifyInput()
    {
        var deck = SampleDeck.Create();
        CardData[] before = [.. deck];

        ShuffleDeck.Execute(deck, new Random(Seed));

        Assert.Equal(before, deck);
    }

    [Fact]
    public void Execute_ReturnsEmpty_WhenInputEmpty()
    {
        var shuffled = ShuffleDeck.Execute([], new Random(Seed));

        Assert.Empty(shuffled);
    }
}
