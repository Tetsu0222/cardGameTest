using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の手札の捨て方。CPU は後攻(Second)として、2ターン目のエンドフェーズで判断させる。</summary>
public class SimpleCpuStrategyDiscardTests
{
    private const int MaxHandSize = 6;

    private readonly SimpleCpuStrategy _cpu = new();

    /// <summary>ターン開始時のドローで <paramref name="drawn"/> を引いた後の、2ターン目のエンドフェーズ。</summary>
    private static Duel CreateCpuDuelAtEnd(CardData drawn, params CardData[] hand)
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, hand)
            .WithDeckTop(Side.Second, drawn)
            .Build();
        duel.AdvanceTo(2, Phase.End);
        return duel;
    }

    private static CardData[] Monsters(params int[] attacks) => [.. attacks.Select(a => TestCards.Monster(attack: a))];

    [Fact]
    public void DecideNextAction_DiscardsCardWithoutEffectFirst()
    {
        var duel = CreateCpuDuelAtEnd(TestCards.Filler(), Monsters(100, 200, 300, 400, 500, 600));

        var action = Assert.IsType<CpuDiscardAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(TestCards.Filler(), duel.SecondPlayer.Hand[action.HandIndex]);
    }

    [Fact]
    public void DecideNextAction_DiscardsWeakestMonster()
    {
        var duel = CreateCpuDuelAtEnd(TestCards.Monster(attack: 1400), Monsters(1500, 300, 1800, 1200, 1600, 1700));

        var action = Assert.IsType<CpuDiscardAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(300, duel.SecondPlayer.Hand[action.HandIndex].Attack);
    }

    [Fact]
    public void DecideNextAction_KeepsSpellsAndTraps()
    {
        CardData[] hand =
        [
            .. Monsters(2000, 2100, 2200, 2300),
            TestCards.Spell(new DealDamageAction(100)),
            TestCards.DestroyAttackerTrap(),
        ];
        var duel = CreateCpuDuelAtEnd(TestCards.Monster(attack: 2500), hand);

        var action = Assert.IsType<CpuDiscardAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(2000, duel.SecondPlayer.Hand[action.HandIndex].Attack);
    }

    [Fact]
    public void DecideNextAction_AdvancesPhase_WhenWithinLimit()
    {
        var duel = CreateCpuDuelAtEnd(TestCards.Monster(), Monsters(1000, 1000));

        Assert.IsType<CpuAdvancePhaseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void PlayTurn_DiscardsDownToLimit_AndPassesTurn()
    {
        // レベル8は召喚できない(リリースするモンスターがいない)ため、手札が減らずに上限を超える
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, [.. Enumerable.Repeat(TestCards.Monster(level: 8), MaxHandSize + 3)])
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Equal(3, duel.TurnCount);
        Assert.Equal(MaxHandSize, duel.SecondPlayer.Hand.Count);
        Assert.Equal(4, duel.Log.Count(e => e.Kind == DuelLogKind.Discarded));
    }
}
