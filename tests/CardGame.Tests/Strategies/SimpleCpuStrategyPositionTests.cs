using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の表示形式の変更。CPU は後攻(Second)として、2ターン目に行動させる。</summary>
public class SimpleCpuStrategyPositionTests
{
    private readonly SimpleCpuStrategy _cpu = new();

    private static Duel CreateCpuDuelAt(Phase phase, DuelBuilder builder)
    {
        var duel = builder.Build();
        duel.AdvanceTo(2, phase);
        return duel;
    }

    // ---- メインフェーズ1(攻めに切り替える) ----

    [Theory]
    [InlineData(BattlePosition.FaceUpDefense)]
    [InlineData(BattlePosition.FaceDownDefense)]
    public void DecideNextAction_SwitchesToAttack_WhenItCanAttackProfitably(BattlePosition position)
    {
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800), position)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1200)));

        var action = Assert.IsType<CpuChangePositionAction>(_cpu.DecideNextAction(duel));

        Assert.Same(duel.SecondPlayer.MonsterZone[0], action.Monster);
    }

    [Fact]
    public void DecideNextAction_SwitchesToAttack_WhenOpponentFieldEmpty()
    {
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500), BattlePosition.FaceUpDefense));

        Assert.IsType<CpuChangePositionAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_KeepsDefense_WhenOpponentIsStronger()
    {
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500), BattlePosition.FaceUpDefense)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 2000)));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_KeepsDefense_WhenNothingToDestroy()
    {
        // 相手は守備力の高い守備表示モンスターだけで、攻撃しても破壊できない
        var duel = CreateCpuDuelAt(Phase.Main1, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500), BattlePosition.FaceUpDefense)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 0, defense: 2500), BattlePosition.FaceUpDefense));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    // ---- メインフェーズ2(守りに切り替える) ----

    [Fact]
    public void DecideNextAction_SwitchesToDefense_InMain2_WhenDefenseHolds()
    {
        var duel = CreateCpuDuelAt(Phase.Main2, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000, defense: 2000))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1800)));

        var action = Assert.IsType<CpuChangePositionAction>(_cpu.DecideNextAction(duel));

        Assert.Same(duel.SecondPlayer.MonsterZone[0], action.Monster);
    }

    [Fact]
    public void DecideNextAction_KeepsAttack_InMain2_WhenDefenseWouldNotHold()
    {
        var duel = CreateCpuDuelAt(Phase.Main2, DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000, defense: 1200))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1800)));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void PlayTurn_FlipSummonsAndAttacks()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Contains(duel.Log, e => e.Kind == DuelLogKind.FlipSummoned);
        Assert.Equal(8000 - 1800, duel.FirstPlayer.LifePoints);
    }
}
