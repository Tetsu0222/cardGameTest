using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Effects;

/// <summary>罠用の効果部品の定義検証と処理。</summary>
public class TrapEffectTests
{
    private readonly Player _controller = CreatePlayer("Controller");
    private readonly Player _opponent = CreatePlayer("Opponent");
    private readonly DuelLog _log = new(() => 1);

    private static Player CreatePlayer(string name) =>
        new(name, Enumerable.Repeat(TestCards.Filler(), 10), new GameRulesOptions());

    private static FieldMonster Place(Player player, BattlePosition position = BattlePosition.FaceUpAttack) =>
        player.PlaceMonster(TestCards.Monster(), position) ?? throw new InvalidOperationException("No empty monster slot.");

    // ---- 定義の検証 ----

    [Theory]
    [InlineData(TrapTrigger.OpponentAttackDeclared, true)]
    [InlineData(TrapTrigger.OpponentMonsterSummoned, false)]
    public void Validate_AttackActions_RequireAttackTrigger(TrapTrigger trigger, bool expected)
    {
        Assert.Equal(expected, ValidateCardEffect.Execute(new CardEffect([new DestroyAttackingMonsterAction()], Trigger: trigger)));
        Assert.Equal(expected, ValidateCardEffect.Execute(new CardEffect([new NegateAttackAction()], Trigger: trigger)));
    }

    [Theory]
    [InlineData(TrapTrigger.OpponentMonsterSummoned, true)]
    [InlineData(TrapTrigger.OpponentAttackDeclared, false)]
    public void Validate_SummonAction_RequiresSummonTrigger(TrapTrigger trigger, bool expected)
    {
        Assert.Equal(expected, ValidateCardEffect.Execute(new CardEffect([new DestroySummonedMonsterAction()], Trigger: trigger)));
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenTriggerActionWithoutTrigger()
    {
        Assert.False(ValidateCardEffect.Execute(new CardEffect([new DestroyAttackingMonsterAction()])));
    }

    [Fact]
    public void Validate_AllowsGeneralActionsWithTrigger()
    {
        var effect = new CardEffect([new DealDamageAction(500)], Trigger: TrapTrigger.OpponentAttackDeclared);

        Assert.True(ValidateCardEffect.Execute(effect));
    }

    // ---- 処理 ----

    [Fact]
    public void Resolve_DestroysTriggerMonster()
    {
        var attacker = Place(_opponent);
        var other = Place(_opponent);
        var effect = new CardEffect([new DestroyAttackingMonsterAction()], Trigger: TrapTrigger.OpponentAttackDeclared);

        ResolveCardEffect.Execute(effect, _controller, _opponent, null, _log, attacker);

        Assert.Same(other, Assert.Single(_opponent.MonsterZone));
    }

    [Fact]
    public void Resolve_ReportsNegatedAttack()
    {
        var effect = new CardEffect([new NegateAttackAction()], Trigger: TrapTrigger.OpponentAttackDeclared);

        var outcome = ResolveCardEffect.Execute(effect, _controller, _opponent, null, _log, Place(_opponent));

        Assert.True(outcome.IsAttackNegated);
    }

    [Fact]
    public void Resolve_ReportsNoNegation_ForOtherEffects()
    {
        var outcome = ResolveCardEffect.Execute(new CardEffect([new DealDamageAction(100)]), _controller, _opponent, null, _log);

        Assert.False(outcome.IsAttackNegated);
    }

    [Fact]
    public void Resolve_DestroysOnlyAttackPositionMonsters_WhenAttackPositionOnly()
    {
        Place(_opponent);
        var defender = Place(_opponent, BattlePosition.FaceUpDefense);
        var faceDown = Place(_opponent, BattlePosition.FaceDownDefense);

        ResolveCardEffect.Execute(
            new CardEffect([new DestroyAllMonstersAction(MonsterSide.Opponent, AttackPositionOnly: true)]),
            _controller, _opponent, null, _log);

        Assert.Equal([defender, faceDown], _opponent.MonsterZone);
    }
}
