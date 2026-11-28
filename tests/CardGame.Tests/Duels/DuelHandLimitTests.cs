using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>エンドフェーズの手札枚数制限(上限 6 枚。超えた分を選んで捨てる)。</summary>
public class DuelHandLimitTests
{
    private const int MaxHandSize = 6;

    private static Duel CreateDuelAtEnd(int handSize, GameRulesOptions? rules = null)
    {
        var hand = Enumerable.Range(0, handSize).Select(i => TestCards.Monster(attack: 100 * i)).ToArray();
        var builder = DuelBuilder.Create().WithHand(Side.First, hand);
        if (rules is not null) builder.WithRules(rules);

        var duel = builder.Build();
        duel.AdvanceTo(1, Phase.End);
        return duel;
    }

    // ---- 捨てる必要がある枚数 ----

    [Theory]
    [InlineData(MaxHandSize, 0)]
    [InlineData(MaxHandSize + 1, 1)]
    [InlineData(MaxHandSize + 3, 3)]
    [InlineData(0, 0)]
    public void RequiredDiscardCount_IsExcessOverLimit_InEndPhase(int handSize, int expected)
    {
        var duel = CreateDuelAtEnd(handSize);

        Assert.Equal(expected, duel.RequiredDiscardCount);
    }

    [Theory]
    [InlineData(Phase.Main1)]
    [InlineData(Phase.Battle)]
    [InlineData(Phase.Main2)]
    public void RequiredDiscardCount_IsZero_BeforeEndPhase(Phase phase)
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, [.. Enumerable.Repeat(TestCards.Monster(), MaxHandSize + 2)])
            .Build();
        duel.AdvanceTo(1, phase);

        Assert.Equal(0, duel.RequiredDiscardCount);
    }

    [Fact]
    public void RequiredDiscardCount_UsesConfiguredLimit()
    {
        var duel = CreateDuelAtEnd(5, new GameRulesOptions { InitialHandSize = 0, MaxHandSize = 3 });

        Assert.Equal(2, duel.RequiredDiscardCount);
    }

    [Fact]
    public void RequiredDiscardCount_IgnoresOpponentsHand()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, [.. Enumerable.Repeat(TestCards.Monster(), MaxHandSize + 2)])
            .Build();
        duel.AdvanceTo(1, Phase.End);

        Assert.Equal(0, duel.RequiredDiscardCount);
    }

    // ---- ターン終了 ----

    [Fact]
    public void AdvancePhase_FailsToEndTurn_WhileOverLimit()
    {
        var duel = CreateDuelAtEnd(MaxHandSize + 1);

        var result = duel.AdvancePhase();

        Assert.Equal(ActionError.MustDiscard, result.Error);
        Assert.Equal(1, duel.TurnCount);
        Assert.Equal(Phase.End, duel.CurrentPhase);
    }

    [Fact]
    public void AdvancePhase_EndsTurn_AfterDiscardingDownToLimit()
    {
        var duel = CreateDuelAtEnd(MaxHandSize + 2);
        duel.Discard(0);
        duel.Discard(0);

        var result = duel.AdvancePhase();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, duel.TurnCount);
        Assert.Equal(MaxHandSize, duel.FirstPlayer.Hand.Count);
    }

    [Fact]
    public void AdvancePhase_EndsTurn_WhenExactlyAtLimit()
    {
        var duel = CreateDuelAtEnd(MaxHandSize);

        Assert.True(duel.AdvancePhase().IsSuccess);
    }

    // ---- 捨てる ----

    [Fact]
    public void Discard_MovesChosenCardToGraveyard_AndLogs()
    {
        var duel = CreateDuelAtEnd(MaxHandSize + 1);
        var chosen = duel.FirstPlayer.Hand[3];

        var result = duel.Discard(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(chosen, Assert.Single(duel.FirstPlayer.Graveyard));
        Assert.Equal(MaxHandSize, duel.FirstPlayer.Hand.Count);
        Assert.Equal(new DuelLogEntry(1, DuelLogKind.Discarded, duel.FirstPlayer.Name, chosen.Name), duel.Log[^1]);
    }

    [Fact]
    public void Discard_Fails_WhenAlreadyAtLimit()
    {
        var duel = CreateDuelAtEnd(MaxHandSize + 1);
        duel.Discard(0);

        var result = duel.Discard(0);

        Assert.Equal(ActionError.NoDiscardRequired, result.Error);
        Assert.Equal(MaxHandSize, duel.FirstPlayer.Hand.Count);
    }

    [Fact]
    public void Discard_Fails_OutsideEndPhase()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, [.. Enumerable.Repeat(TestCards.Monster(), MaxHandSize + 1)])
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.NoDiscardRequired, duel.Discard(0).Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(MaxHandSize + 1)]
    public void Discard_Fails_WhenHandIndexOutOfRange(int handIndex)
    {
        var duel = CreateDuelAtEnd(MaxHandSize + 1);

        Assert.Equal(ActionError.InvalidHandIndex, duel.Discard(handIndex).Error);
    }
}
