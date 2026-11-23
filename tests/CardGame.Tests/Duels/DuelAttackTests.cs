using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>
/// 先攻1ターン目は攻撃できないため、基本的に後攻(Second)の2ターン目で攻撃する。
/// </summary>
public class DuelAttackTests
{
    private const int InitialLifePoints = 8000;

    [Fact]
    public void Attack_DestroysTargetAndDamagesOwner_WhenAttackHigher()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 2000))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1500))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints - 500, duel.FirstPlayer.LifePoints);
        Assert.Equal(InitialLifePoints, duel.SecondPlayer.LifePoints);
        Assert.Empty(duel.FirstPlayer.MonsterZone);
        Assert.Single(duel.FirstPlayer.Graveyard);
        Assert.Single(duel.SecondPlayer.MonsterZone);
    }

    [Fact]
    public void Attack_DestroysAttackerAndDamagesAttackerOwner_WhenAttackLower()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1500))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        Assert.Equal(InitialLifePoints - 500, duel.SecondPlayer.LifePoints);
        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.Single(duel.SecondPlayer.Graveyard);
        Assert.Single(duel.FirstPlayer.MonsterZone);
    }

    [Fact]
    public void Attack_FlipsFaceDownTarget_BeforeDamageCalculation()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.First, TestCards.Monster(defense: 2000), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        var target = duel.FirstPlayer.MonsterZone[0];

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target);

        Assert.Equal(BattlePosition.FaceUpDefense, target.Position);
        Assert.Equal(InitialLifePoints - 1000, duel.SecondPlayer.LifePoints);
        Assert.Single(duel.FirstPlayer.MonsterZone);
    }

    [Fact]
    public void Attack_DealsDirectDamage_WhenOpponentHasNoMonsters()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints - 1800, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void Attack_EndsDuel_WhenLifePointsReachZero()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 1000 })
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(0, duel.FirstPlayer.LifePoints);
        Assert.True(duel.IsFinished);
        Assert.Same(duel.SecondPlayer, duel.Winner);
    }

    [Fact]
    public void Attack_CanAttackAgain_OnNextOwnTurn()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .Build();
        var attacker = duel.SecondPlayer.MonsterZone[0];
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(attacker, target: null);

        duel.AdvanceTo(4, Phase.Battle);
        var result = duel.Attack(attacker, target: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints - 2000, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void Attack_Fails_OnFirstTurn()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Battle);

        var result = duel.Attack(duel.FirstPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.CannotAttackOnFirstTurn, result.Error);
        Assert.Equal(InitialLifePoints, duel.SecondPlayer.LifePoints);
    }

    [Fact]
    public void Attack_Fails_WhenNotBattlePhase()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Main1);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.WrongPhase, result.Error);
    }

    [Fact]
    public void Attack_Fails_WhenDirectAttackWhileOpponentHasMonsters()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster(), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.DirectAttackNotAllowed, result.Error);
        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void Attack_Fails_WhenAlreadyAttackedThisTurn()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        var attacker = duel.SecondPlayer.MonsterZone[0];
        duel.Attack(attacker, target: null);

        var result = duel.Attack(attacker, target: null);

        Assert.Equal(ActionError.AlreadyAttacked, result.Error);
        Assert.Equal(InitialLifePoints - 1000, duel.FirstPlayer.LifePoints);
    }

    [Theory]
    [InlineData(BattlePosition.FaceUpDefense)]
    [InlineData(BattlePosition.FaceDownDefense)]
    public void Attack_Fails_WhenAttackerInDefensePosition(BattlePosition position)
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(), position)
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.AttackerNotInAttackPosition, result.Error);
    }

    [Fact]
    public void Attack_Fails_WhenAttackerIsOpponentMonster()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.FirstPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.AttackerNotOnYourField, result.Error);
    }

    [Fact]
    public void Attack_Fails_WhenTargetIsOwnMonster()
    {
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.SecondPlayer.MonsterZone[1]);

        Assert.Equal(ActionError.InvalidTarget, result.Error);
    }

    [Fact]
    public void Attack_Fails_WhenDuelFinished()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 1000 })
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var result = duel.Attack(duel.SecondPlayer.MonsterZone[1], target: null);

        Assert.Equal(ActionError.DuelFinished, result.Error);
    }
}
