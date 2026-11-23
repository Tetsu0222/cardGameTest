using CardGame.Core.Cards;
using CardGame.Core.Effects;

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
        var monsters = SampleDeck.Create().Where(c => c.Kind is CardKind.NormalMonster or CardKind.EffectMonster);

        Assert.All(monsters, card => Assert.InRange(card.Level, 1, MaxLevelWithoutTribute));
    }

    [Fact]
    public void Create_ContainsOnlySpellsWithValidEffects()
    {
        var spells = SampleDeck.Create().Where(c => c.Kind == CardKind.Spell).ToList();

        Assert.NotEmpty(spells);
        Assert.All(spells, card =>
        {
            Assert.NotNull(card.Effect);
            Assert.True(ValidateCardEffect.Execute(card.Effect));
        });
    }

    [Fact]
    public void Create_ContainsNoTraps_UntilTrapsAreImplemented()
    {
        Assert.DoesNotContain(SampleDeck.Create(), c => c.Kind == CardKind.Trap);
    }

    [Fact]
    public void Create_UsesUniqueIdPerCardName()
    {
        var namesPerId = SampleDeck.Create().GroupBy(c => c.Id).Select(g => g.Select(c => c.Name).Distinct().Count());

        Assert.All(namesPerId, count => Assert.Equal(1, count));
    }
}
