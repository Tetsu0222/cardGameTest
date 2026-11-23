using CardGame.Core.Effects;

namespace CardGame.Tests.Effects;

public class ValidateCardEffectTests
{
    [Fact]
    public void Execute_ReturnsTrue_WhenCombiningActionsWithoutTarget()
    {
        var effect = new CardEffect([new DealDamageAction(300), new DrawCardsAction(1), new GainLifePointsAction(500)]);

        Assert.True(ValidateCardEffect.Execute(effect));
    }

    [Fact]
    public void Execute_ReturnsTrue_WhenTargetActionHasTargetSide()
    {
        var effect = new CardEffect([new DestroyTargetMonsterAction()], MonsterSide.Opponent);

        Assert.True(ValidateCardEffect.Execute(effect));
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenNoActions()
    {
        Assert.False(ValidateCardEffect.Execute(new CardEffect([])));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Execute_ReturnsFalse_WhenAmountNotPositive(int value)
    {
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new DrawCardsAction(value)])));
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new DealDamageAction(value)])));
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new GainLifePointsAction(value)])));
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenTargetActionWithoutTargetSide()
    {
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new DestroyTargetMonsterAction()])));
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenTargetSideWithoutTargetAction()
    {
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new DrawCardsAction(1)], MonsterSide.Opponent)));
    }

    [Fact]
    public void Execute_ReturnsFalse_WhenMoreThanOneTargetAction()
    {
        var effect = new CardEffect(
            [new DestroyTargetMonsterAction(), new DestroyTargetMonsterAction()],
            MonsterSide.Opponent);

        Assert.False(ValidateCardEffect.Execute(effect));
    }
}
