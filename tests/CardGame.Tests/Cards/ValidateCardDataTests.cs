using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Tests.Cards;

public class ValidateCardDataTests
{
    private static readonly CardEffect DamageEffect = new([new DealDamageAction(500)]);
    private static readonly CardEffect AttackTrapEffect = new([new NegateAttackAction()], Trigger: TrapTrigger.OpponentAttackDeclared);

    [Theory]
    [InlineData(ValidateCardData.MinLevel)]
    [InlineData(ValidateCardData.MaxLevel)]
    public void Execute_AcceptsMonster_WithLevelInRange(int level)
    {
        Assert.True(ValidateCardData.Execute(new CardData(1, "M", CardKind.NormalMonster, level, 1000, 1000)));
    }

    [Theory]
    [InlineData(ValidateCardData.MinLevel - 1, 1000, 1000)]
    [InlineData(ValidateCardData.MaxLevel + 1, 1000, 1000)]
    [InlineData(4, -1, 1000)]
    [InlineData(4, 1000, -1)]
    public void Execute_RejectsMonster_WithInvalidStats(int level, int attack, int defense)
    {
        Assert.False(ValidateCardData.Execute(new CardData(1, "M", CardKind.NormalMonster, level, attack, defense)));
    }

    [Fact]
    public void Execute_RejectsNormalMonster_WithEffect()
    {
        Assert.False(ValidateCardData.Execute(new CardData(1, "M", CardKind.NormalMonster, 4, Effect: DamageEffect)));
    }

    [Fact]
    public void Execute_RejectsEffectMonster_WithoutEffectOrTrigger()
    {
        Assert.False(ValidateCardData.Execute(new CardData(1, "M", CardKind.EffectMonster, 4)));
        Assert.False(ValidateCardData.Execute(new CardData(1, "M", CardKind.EffectMonster, 4, Effect: DamageEffect)));
    }

    [Theory]
    [InlineData(MonsterEffectTrigger.Flipped)]
    [InlineData(MonsterEffectTrigger.Summoned)]
    public void Execute_AcceptsEffectMonster_WithMonsterTrigger(MonsterEffectTrigger trigger)
    {
        var effect = new CardEffect([new DealDamageAction(500)], MonsterTrigger: trigger);

        Assert.True(ValidateCardData.Execute(new CardData(1, "M", CardKind.EffectMonster, 4, 1000, 1000, effect)));
    }

    [Fact]
    public void Execute_RejectsEffectMonster_WithTrapTrigger()
    {
        var effect = new CardEffect([new DealDamageAction(500)], Trigger: TrapTrigger.OpponentAttackDeclared, MonsterTrigger: MonsterEffectTrigger.Flipped);

        Assert.False(ValidateCardData.Execute(new CardData(1, "M", CardKind.EffectMonster, 4, Effect: effect)));
    }

    [Fact]
    public void Execute_RejectsSpellAndTrap_WithMonsterTrigger()
    {
        var spellEffect = new CardEffect([new DealDamageAction(500)], MonsterTrigger: MonsterEffectTrigger.Summoned);
        var trapEffect = AttackTrapEffect with { MonsterTrigger = MonsterEffectTrigger.Summoned };

        Assert.False(ValidateCardData.Execute(new CardData(1, "S", CardKind.Spell, Effect: spellEffect)));
        Assert.False(ValidateCardData.Execute(new CardData(1, "T", CardKind.Trap, Effect: trapEffect)));
    }

    [Fact]
    public void Execute_AcceptsSpell_WithEffectWithoutTrigger()
    {
        Assert.True(ValidateCardData.Execute(new CardData(1, "S", CardKind.Spell, Effect: DamageEffect)));
    }

    [Fact]
    public void Execute_RejectsSpell_WithTriggerOrWithoutEffect()
    {
        Assert.False(ValidateCardData.Execute(new CardData(1, "S", CardKind.Spell, Effect: AttackTrapEffect)));
        Assert.False(ValidateCardData.Execute(new CardData(1, "S", CardKind.Spell)));
    }

    [Fact]
    public void Execute_AcceptsTrap_WithTrigger()
    {
        Assert.True(ValidateCardData.Execute(new CardData(1, "T", CardKind.Trap, Effect: AttackTrapEffect)));
    }

    [Fact]
    public void Execute_RejectsTrap_WithoutTrigger()
    {
        Assert.False(ValidateCardData.Execute(new CardData(1, "T", CardKind.Trap, Effect: DamageEffect)));
    }

    [Fact]
    public void Execute_RejectsCard_WithInvalidEffectDefinition()
    {
        var invalid = new CardEffect([new DrawCardsAction(0)]);

        Assert.False(ValidateCardData.Execute(new CardData(1, "S", CardKind.Spell, Effect: invalid)));
    }

    [Theory]
    [InlineData(0, "Name")]
    [InlineData(-1, "Name")]
    [InlineData(1, "")]
    [InlineData(1, "  ")]
    public void Execute_RejectsCard_WithInvalidIdOrName(int id, string name)
    {
        Assert.False(ValidateCardData.Execute(new CardData(id, name, CardKind.NormalMonster, 4)));
    }
}
