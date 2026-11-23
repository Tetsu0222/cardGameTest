using CardGame.Core.Duels;
using CardGame.Core.Turns;

namespace CardGame.Tests.TestSupport;

internal static class DuelTestExtensions
{
    private const int MaxSteps = 100;

    /// <summary>指定したターン・フェーズまでフェーズを進める。</summary>
    public static void AdvanceTo(this Duel duel, int turn, Phase phase)
    {
        for (var i = 0; i < MaxSteps; i++)
        {
            if (duel.TurnCount == turn && duel.CurrentPhase == phase) return;
            Assert.True(duel.AdvancePhase().IsSuccess, $"AdvancePhase failed at turn {duel.TurnCount} {duel.CurrentPhase}");
        }

        Assert.Fail($"Could not reach turn {turn} {phase}");
    }
}
