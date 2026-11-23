using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

public class DuelTurnTests
{
    [Fact]
    public void Constructor_DrawsInitialHand_ForBothPlayers()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 5 })
            .Build();

        Assert.Equal(5, duel.FirstPlayer.Hand.Count);
        Assert.Equal(5, duel.SecondPlayer.Hand.Count);
    }

    [Fact]
    public void Constructor_FirstPlayerDraws_WhenSkipFirstDrawDisabled()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 5, FirstPlayerSkipsFirstDraw = false })
            .Build();

        Assert.Equal(6, duel.FirstPlayer.Hand.Count);
    }

    [Fact]
    public void Constructor_StartsAtFirstPlayerDrawPhase()
    {
        var duel = DuelBuilder.Create().Build();

        Assert.Equal(1, duel.TurnCount);
        Assert.Equal(Phase.Draw, duel.CurrentPhase);
        Assert.Same(duel.FirstPlayer, duel.CurrentPlayer);
    }

    [Fact]
    public void AdvancePhase_FollowsPhaseOrder_ThenPassesTurn()
    {
        var duel = DuelBuilder.Create().Build();
        Phase[] expected = [Phase.Standby, Phase.Main1, Phase.Battle, Phase.Main2, Phase.End];

        foreach (var phase in expected)
        {
            Assert.True(duel.AdvancePhase().IsSuccess);
            Assert.Equal(phase, duel.CurrentPhase);
        }

        Assert.True(duel.AdvancePhase().IsSuccess);
        Assert.Equal(2, duel.TurnCount);
        Assert.Equal(Phase.Draw, duel.CurrentPhase);
        Assert.Same(duel.SecondPlayer, duel.CurrentPlayer);
    }

    [Fact]
    public void AdvancePhase_TurnPlayerDraws_WhenTurnStarts()
    {
        var duel = DuelBuilder.Create().Build();

        duel.AdvanceTo(2, Phase.Draw);

        Assert.Single(duel.SecondPlayer.Hand);
    }

    [Fact]
    public void AdvancePhase_ResetsNormalSummon_WhenTurnChanges()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);

        duel.AdvanceTo(2, Phase.Main1);

        Assert.False(duel.NormalSummonUsed);
    }

    [Fact]
    public void AdvancePhase_TurnPlayerLoses_WhenDeckEmptyAtDraw()
    {
        var duel = DuelBuilder.Create()
            .WithDeckSize(Side.Second, 0)
            .Build();

        duel.AdvanceTo(1, Phase.End);
        duel.AdvancePhase();

        Assert.True(duel.IsFinished);
        Assert.Same(duel.FirstPlayer, duel.Winner);
    }

    [Fact]
    public void AdvancePhase_Fails_WhenDuelFinished()
    {
        var duel = DuelBuilder.Create()
            .WithDeckSize(Side.Second, 0)
            .Build();
        duel.AdvanceTo(1, Phase.End);
        duel.AdvancePhase();

        var result = duel.AdvancePhase();

        Assert.Equal(ActionError.DuelFinished, result.Error);
    }
}
