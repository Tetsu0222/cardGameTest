using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>
/// 表示形式の変更と反転召喚。盤面構築で置いたモンスターは場に出たターンが 0 として扱われ、どのターンでも変更できる。
/// </summary>
public class DuelPositionTests
{
    private static Duel CreateDuelAt(BattlePosition position, Phase phase = Phase.Main1, int turn = 1)
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1500, defense: 1200), position)
            .Build();
        duel.AdvanceTo(turn, phase);
        return duel;
    }

    // ---- 変更 ----

    [Theory]
    [InlineData(BattlePosition.FaceUpAttack, BattlePosition.FaceUpDefense, DuelLogKind.ChangedToDefensePosition)]
    [InlineData(BattlePosition.FaceUpDefense, BattlePosition.FaceUpAttack, DuelLogKind.ChangedToAttackPosition)]
    [InlineData(BattlePosition.FaceDownDefense, BattlePosition.FaceUpAttack, DuelLogKind.FlipSummoned)]
    public void ChangePosition_SwitchesPositionAndLogs(BattlePosition from, BattlePosition to, DuelLogKind logKind)
    {
        var duel = CreateDuelAt(from);
        var monster = duel.FirstPlayer.MonsterZone[0];

        var result = duel.ChangePosition(monster);

        Assert.True(result.IsSuccess);
        Assert.Equal(to, monster.Position);
        Assert.True(monster.PositionChangedThisTurn);
        Assert.Equal(new DuelLogEntry(1, logKind, duel.FirstPlayer.Name, monster.Card.Name), duel.Log[^1]);
    }

    [Fact]
    public void ChangePosition_WorksInMain2()
    {
        var duel = CreateDuelAt(BattlePosition.FaceUpAttack, Phase.Main2);

        Assert.True(duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]).IsSuccess);
    }

    [Fact]
    public void ChangePosition_DoesNotUseNormalSummon()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster(), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]); // 反転召喚

        Assert.False(duel.NormalSummonUsed);
        Assert.True(duel.Summon(0, BattlePosition.FaceUpAttack).IsSuccess);
    }

    [Fact]
    public void ChangePosition_CanChangeAgainOnNextOwnTurn()
    {
        var duel = CreateDuelAt(BattlePosition.FaceUpAttack);
        var monster = duel.FirstPlayer.MonsterZone[0];
        duel.ChangePosition(monster);

        duel.AdvanceTo(3, Phase.Main1);

        Assert.False(monster.PositionChangedThisTurn);
        Assert.True(duel.ChangePosition(monster).IsSuccess);
        Assert.Equal(BattlePosition.FaceUpAttack, monster.Position);
    }

    [Fact]
    public void FlipSummonedMonster_CanAttackSameTurn()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        var monster = duel.SecondPlayer.MonsterZone[0];
        duel.ChangePosition(monster);
        duel.AdvanceTo(2, Phase.Battle);

        Assert.True(duel.Attack(monster, target: null).IsSuccess);
    }

    // ---- 変更できない場合 ----

    [Fact]
    public void ChangePosition_Fails_WhenAlreadyChangedThisTurn()
    {
        var duel = CreateDuelAt(BattlePosition.FaceUpAttack);
        var monster = duel.FirstPlayer.MonsterZone[0];
        duel.ChangePosition(monster);

        var result = duel.ChangePosition(monster);

        Assert.Equal(ActionError.PositionAlreadyChanged, result.Error);
        Assert.Equal(BattlePosition.FaceUpDefense, monster.Position);
    }

    [Theory]
    [InlineData(BattlePosition.FaceUpAttack)]
    [InlineData(BattlePosition.FaceDownDefense)]
    public void ChangePosition_Fails_OnTurnSummoned(BattlePosition position)
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();
        duel.AdvanceTo(1, Phase.Main1);
        duel.Summon(0, position);
        var monster = duel.FirstPlayer.MonsterZone[0];

        var result = duel.ChangePosition(monster);

        Assert.Equal(ActionError.SummonedThisTurn, result.Error);
        Assert.Equal(1, monster.SummonedTurn);
    }

    [Fact]
    public void ChangePosition_Succeeds_OnTurnAfterSummoned()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();
        duel.AdvanceTo(1, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceDownDefense);

        duel.AdvanceTo(3, Phase.Main1);

        Assert.True(duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]).IsSuccess);
    }

    [Fact]
    public void ChangePosition_Fails_AfterAttackingThisTurn()
    {
        var duel = DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()).Build();
        duel.AdvanceTo(2, Phase.Battle);
        var monster = duel.SecondPlayer.MonsterZone[0];
        duel.Attack(monster, target: null);
        duel.AdvanceTo(2, Phase.Main2);

        Assert.Equal(ActionError.AlreadyAttacked, duel.ChangePosition(monster).Error);
    }

    [Theory]
    [InlineData(Phase.Draw)]
    [InlineData(Phase.Standby)]
    [InlineData(Phase.End)]
    public void ChangePosition_Fails_WhenNotMainPhase(Phase phase)
    {
        var duel = CreateDuelAt(BattlePosition.FaceUpAttack, phase);

        Assert.Equal(ActionError.WrongPhase, duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]).Error);
    }

    [Fact]
    public void ChangePosition_Fails_InBattlePhase()
    {
        var duel = DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()).Build();
        duel.AdvanceTo(2, Phase.Battle);

        Assert.Equal(ActionError.WrongPhase, duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]).Error);
    }

    [Fact]
    public void ChangePosition_Fails_ForOpponentMonster()
    {
        var duel = DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()).Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.MonsterNotOnYourField, duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]).Error);
    }

    // ---- 反転召喚と罠 ----

    [Fact]
    public void FlipSummon_OpensSummonTrapWindow()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        var monster = duel.SecondPlayer.MonsterZone[0];

        duel.ChangePosition(monster);

        var window = Assert.IsType<ResponseWindow>(duel.PendingResponse);
        Assert.Equal(TrapTrigger.OpponentMonsterSummoned, window.Trigger);
        Assert.Same(monster, window.TriggerMonster);
    }

    [Fact]
    public void PositionChange_DoesNotOpenSummonTrapWindow()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(), BattlePosition.FaceUpDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Main1);

        duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]);

        Assert.Null(duel.PendingResponse);
    }

    [Fact]
    public void ChangePosition_Fails_WhileResponsePending()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster(), BattlePosition.FaceUpAttack)
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]);

        Assert.Equal(ActionError.ResponsePending, duel.ChangePosition(duel.SecondPlayer.MonsterZone[1]).Error);
    }
}
