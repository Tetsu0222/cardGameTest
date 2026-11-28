using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の効果モンスターの使い方。CPU は後攻(Second)として、2ターン目に行動させる。</summary>
public class SimpleCpuStrategyMonsterEffectTests
{
    private readonly SimpleCpuStrategy _cpu = new();

    private static Duel CreateCpuDuelAt(Phase phase, DuelBuilder builder)
    {
        var duel = builder.Build();
        duel.AdvanceTo(2, phase);
        return duel;
    }

    [Fact]
    public void DecideNextAction_ChoosesStrongestOpponentMonster_AsEffectTarget()
    {
        var weak = TestCards.Monster(attack: 1000);
        var strong = TestCards.Monster(attack: 2500);
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.FlipDestroyMonster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 3000))
            .WithFieldMonster(Side.First, weak)
            .WithFieldMonster(Side.First, strong));
        duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]);

        var action = Assert.IsType<CpuChooseEffectTargetAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(strong, action.Target.Card);
    }

    [Fact]
    public void DecideNextAction_ChoosesWeakestOwnMonster_WhenOnlyOwnTargets()
    {
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.FlipDestroyMonster(MonsterSide.Own), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 3000)));
        duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]);

        var action = Assert.IsType<CpuChooseEffectTargetAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(450, action.Target.Card.Attack); // 自分自身(攻撃力450)
    }

    [Fact]
    public void DecideNextAction_ChoosesTargetForOpponentSideController_DuringOtherPlayersTurn()
    {
        // CPU 同士の対戦で、ターンプレイヤー(Second)の攻撃で表になった First のモンスターの対象も決める
        var duel = CreateCpuDuelAt(Phase.Battle, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500))
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(defense: 1000), BattlePosition.FaceDownDefense));
        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        var action = Assert.IsType<CpuChooseEffectTargetAction>(_cpu.DecideNextAction(duel));

        Assert.Same(duel.SecondPlayer.MonsterZone[0], action.Target);
    }

    [Fact]
    public void DecideNextAction_PrefersSettingFlipMonster_WhenDefending()
    {
        var flip = TestCards.EffectMonster(MonsterEffectTrigger.Flipped, 500, 500, new DrawCardsAction(1));
        var wall = TestCards.Monster(attack: 500, defense: 2000);
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithHand(Side.Second, wall, flip)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 2500)));

        var action = Assert.IsType<CpuSummonAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(BattlePosition.FaceDownDefense, action.Position);
        Assert.Equal(1, action.HandIndex);
    }

    [Fact]
    public void DecideNextAction_FlipSummons_WhenEffectHasTarget()
    {
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.FlipDestroyMonster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 2500)));

        Assert.IsType<CpuChangePositionAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_KeepsFlipMonsterSet_WhenEffectIsNotWorthIt()
    {
        // 自分のモンスターだけを全滅させる効果は使わない。相手の方が強いので攻撃に回すこともしない
        var selfDestruct = TestCards.EffectMonster(
            MonsterEffectTrigger.Flipped, 450, 600, new DestroyAllMonstersAction(MonsterSide.Own));
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, selfDestruct, BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 2500)));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void PlayTurn_UsesSummonEffect()
    {
        var card = TestCards.EffectMonster(MonsterEffectTrigger.Summoned, 1500, 500, new DealDamageAction(400));
        var duel = DuelBuilder.Create().WithHand(Side.Second, card).Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Contains(duel.Log, e => e.Kind == DuelLogKind.MonsterEffectActivated);
        Assert.Equal(8000 - 400 - 1500, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void PlayTurn_ResolvesOpponentsFlipEffect_WithoutGettingStuck()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(defense: 600), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Equal(3, duel.TurnCount);
        Assert.Null(duel.PendingEffectTarget);
        Assert.Contains(duel.Log, e => e.Kind == DuelLogKind.MonsterEffectActivated);
    }
}
