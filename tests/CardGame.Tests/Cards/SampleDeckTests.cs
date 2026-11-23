using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Summons;

namespace CardGame.Tests.Cards;

public class SampleDeckTests
{
    private const int DeckSize = 40;
    private const int MaxCopiesPerCard = 3;
    private const int MinLevel = 1;
    private const int MaxLevelInDeck = 8;

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
    public void Create_ContainsMonstersWithValidLevels()
    {
        var monsters = SampleDeck.Create().Where(c => c.Kind is CardKind.NormalMonster or CardKind.EffectMonster);

        Assert.All(monsters, card => Assert.InRange(card.Level, MinLevel, MaxLevelInDeck));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Create_ContainsMonstersForEachTributeCount(int tributeCount)
    {
        var monsters = SampleDeck.Create().Where(c => c.Kind is CardKind.NormalMonster or CardKind.EffectMonster);

        Assert.Contains(monsters, card => CountRequiredTributes.Execute(card) == tributeCount);
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
    public void Create_ContainsOnlyTrapsWithValidEffectsAndTrigger()
    {
        var traps = SampleDeck.Create().Where(c => c.Kind == CardKind.Trap).ToList();

        Assert.NotEmpty(traps);
        Assert.All(traps, card =>
        {
            Assert.NotNull(card.Effect);
            Assert.NotNull(card.Effect.Trigger);
            Assert.True(ValidateCardEffect.Execute(card.Effect));
        });
    }

    [Fact]
    public void Create_ContainsOnlySpellsWithoutTrigger()
    {
        var spells = SampleDeck.Create().Where(c => c.Kind == CardKind.Spell);

        Assert.All(spells, card => Assert.Null(card.Effect?.Trigger));
    }

    [Fact]
    public void Create_UsesUniqueIdPerCardName()
    {
        var namesPerId = SampleDeck.Create().GroupBy(c => c.Id).Select(g => g.Select(c => c.Name).Distinct().Count());

        Assert.All(namesPerId, count => Assert.Equal(1, count));
    }
}
