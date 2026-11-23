using CardGame.Core.Cards;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>CanSummon / CanAttack が判定だけを行い、盤面を変えないことの確認。</summary>
public class DuelQueryTests
{
    [Fact]
    public void CanSummon_DoesNotChangeState_WhenSummonable()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();
        duel.AdvanceTo(1, Phase.Main1);
        var logCount = duel.Log.Count;

        Assert.True(duel.CanSummon(0, BattlePosition.FaceUpAttack).IsSuccess);

        Assert.Single(duel.FirstPlayer.Hand);
        Assert.Empty(duel.FirstPlayer.MonsterZone);
        Assert.False(duel.NormalSummonUsed);
        Assert.Equal(logCount, duel.Log.Count);
    }

    [Fact]
    public void CanAttack_DoesNotChangeState_WhenAttackable()
    {
        var duel = DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()).Build();
        duel.AdvanceTo(2, Phase.Battle);
        var attacker = duel.SecondPlayer.MonsterZone[0];
        var lifeBefore = duel.FirstPlayer.LifePoints;

        Assert.True(duel.CanAttack(attacker, target: null).IsSuccess);

        Assert.False(attacker.HasAttacked);
        Assert.Equal(lifeBefore, duel.FirstPlayer.LifePoints);
    }
}
