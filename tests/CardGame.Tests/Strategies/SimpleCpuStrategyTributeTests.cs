using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU のアドバンス召喚の判断。CPU は後攻(Second)として、2ターン目に行動させる。</summary>
public class SimpleCpuStrategyTributeTests
{
    private readonly SimpleCpuStrategy _cpu = new();

    private static Duel StartCpuMain1(DuelBuilder builder)
    {
        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Main1);
        return duel;
    }

    [Fact]
    public void DecideNextAction_TributesWeakestMonster_ForHighLevelMonster()
    {
        var duel = StartCpuMain1(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(attack: 2300, level: 5))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 800)));

        var action = Assert.IsType<CpuSummonAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(BattlePosition.FaceUpAttack, action.Position);
        Assert.NotNull(action.Tributes);
        Assert.Equal(800, Assert.Single(action.Tributes).Card.Attack);
    }

    [Fact]
    public void DecideNextAction_TributesTwoMonsters_ForLevel7()
    {
        var duel = StartCpuMain1(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(attack: 2800, level: 7))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1200)));

        var action = Assert.IsType<CpuSummonAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(2, action.Tributes?.Count);
    }

    [Fact]
    public void DecideNextAction_PrefersStrongerTributeSummon_OverLowLevelSummon()
    {
        var duel = StartCpuMain1(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(attack: 1800), TestCards.Monster(attack: 2300, level: 5))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500)));

        var action = Assert.IsType<CpuSummonAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(1, action.HandIndex);
    }

    [Fact]
    public void DecideNextAction_DoesNotTribute_WhenTributeIsStronger()
    {
        var duel = StartCpuMain1(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(attack: 2000, level: 5))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 2500)));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_DoesNotTributeToSet_WhenOpponentIsStronger()
    {
        var duel = StartCpuMain1(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(attack: 2300, defense: 2000, level: 5))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 3000)));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void PlayTurn_PerformsTributeSummon()
    {
        var card = TestCards.Monster(attack: 2300, level: 5);
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, card)
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500))
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Equal(card, Assert.Single(duel.SecondPlayer.MonsterZone).Card);
        Assert.Contains(duel.Log, e => e.Kind == DuelLogKind.Tributed);
    }
}
