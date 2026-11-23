using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の1手ずつの決定(画面で1手ずつ再生するために使う)。CPU は後攻(Second)。</summary>
public class SimpleCpuStrategyStepTests
{
    private readonly SimpleCpuStrategy _cpu = new();

    private static Duel CreateDuelAt(DuelBuilder builder, int turn, Phase phase)
    {
        var duel = builder.Build();
        duel.AdvanceTo(turn, phase);
        return duel;
    }

    [Theory]
    [InlineData(Phase.Draw)]
    [InlineData(Phase.Standby)]
    [InlineData(Phase.Main2)]
    [InlineData(Phase.End)]
    public void DecideNextAction_AdvancesPhase_OutsideMain1AndBattle(Phase phase)
    {
        var duel = CreateDuelAt(DuelBuilder.Create().WithHand(Side.Second, TestCards.Monster()), 2, phase);

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_ChoosesSpellBeforeSummon_InMain1()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create().WithHand(Side.Second, TestCards.Monster(), TestCards.Spell(new DealDamageAction(100))),
            2,
            Phase.Main1);

        var action = Assert.IsType<CpuActivateSpellAction>(_cpu.DecideNextAction(duel));
        Assert.Equal(1, action.HandIndex);
        Assert.Null(action.Target);
    }

    [Fact]
    public void DecideNextAction_ChoosesTargetForTargetSpell()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create()
                .WithHand(Side.Second, TestCards.DestroyTargetSpell())
                .WithFieldMonster(Side.First, TestCards.Monster()),
            2,
            Phase.Main1);

        var action = Assert.IsType<CpuActivateSpellAction>(_cpu.DecideNextAction(duel));
        Assert.Same(duel.FirstPlayer.MonsterZone[0], action.Target);
    }

    [Fact]
    public void DecideNextAction_ChoosesSummon_WhenNoSpellToUse()
    {
        var duel = CreateDuelAt(DuelBuilder.Create().WithHand(Side.Second, TestCards.Monster()), 2, Phase.Main1);

        var action = Assert.IsType<CpuSummonAction>(_cpu.DecideNextAction(duel));
        Assert.Equal(new CpuSummonAction(0, BattlePosition.FaceUpAttack), action);
    }

    [Fact]
    public void DecideNextAction_AdvancesPhase_AfterNormalSummonUsed()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create().WithHand(Side.Second, TestCards.Monster(), TestCards.Monster()),
            2,
            Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_ChoosesAttack_InBattle()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500)),
            2,
            Phase.Battle);

        var action = Assert.IsType<CpuAttackAction>(_cpu.DecideNextAction(duel));
        Assert.Same(duel.SecondPlayer.MonsterZone[0], action.Attacker);
        Assert.Null(action.Target);
    }

    [Fact]
    public void DecideNextAction_AdvancesPhase_AfterAllAttacksDone()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500)),
            2,
            Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_DoesNotChangeDuel()
    {
        var duel = CreateDuelAt(
            DuelBuilder.Create()
                .WithHand(Side.Second, TestCards.Spell(new DealDamageAction(100)), TestCards.Monster())
                .WithFieldMonster(Side.Second, TestCards.Monster()),
            2,
            Phase.Main1);
        var logCount = duel.Log.Count;
        var handCount = duel.SecondPlayer.Hand.Count;

        _cpu.DecideNextAction(duel);

        Assert.Equal(logCount, duel.Log.Count);
        Assert.Equal(handCount, duel.SecondPlayer.Hand.Count);
        Assert.Equal(Phase.Main1, duel.CurrentPhase);
    }

    [Fact]
    public void DecideNextAction_ReturnsNull_WhenDuelFinished()
    {
        var duel = DuelBuilder.Create().WithDeckSize(Side.Second, 0).Build();
        duel.AdvanceTo(1, Phase.End);
        duel.AdvancePhase();

        Assert.Null(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void Execute_AdvancesPhase_WhenActionRejected()
    {
        var duel = CreateDuelAt(DuelBuilder.Create(), 2, Phase.Main1);

        var result = SimpleCpuStrategy.Execute(duel, new CpuSummonAction(5, BattlePosition.FaceUpAttack));

        Assert.False(result.IsSuccess);
        Assert.Equal(Phase.Battle, duel.CurrentPhase);
    }

    [Fact]
    public void Execute_RunsAction_WhenValid()
    {
        var duel = CreateDuelAt(DuelBuilder.Create().WithHand(Side.Second, TestCards.Monster()), 2, Phase.Main1);

        var result = SimpleCpuStrategy.Execute(duel, new CpuSummonAction(0, BattlePosition.FaceUpAttack));

        Assert.True(result.IsSuccess);
        Assert.Single(duel.SecondPlayer.MonsterZone);
        Assert.Equal(Phase.Main1, duel.CurrentPhase);
    }
}
