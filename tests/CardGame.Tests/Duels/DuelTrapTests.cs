using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>
/// 罠の発動(相手の攻撃宣言・召喚に割り込む)。基本は、後攻(Second)の2ターン目の行動に、先攻(First)が罠で応じる。
/// 盤面構築で置いた罠はセットしたターンが 0 として扱われ、どのターンでも発動できる。
/// </summary>
public class DuelTrapTests
{
    private const int InitialLifePoints = 8000;

    private static Duel CreateAttackScenario(CardData trap, int attackerAttack = 1800, CardData? defender = null)
    {
        var builder = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, trap)
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: attackerAttack));
        if (defender is not null) builder.WithFieldMonster(Side.First, defender);

        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Battle);
        return duel;
    }

    // ---- 攻撃宣言に対する機会 ----

    [Fact]
    public void Attack_OpensResponseWindow_WhenOpponentHasActivatableTrap()
    {
        var duel = CreateAttackScenario(TestCards.DestroyAttackerTrap());
        var attacker = duel.SecondPlayer.MonsterZone[0];

        var result = duel.Attack(attacker, target: null);

        Assert.True(result.IsSuccess);
        var window = Assert.IsType<ResponseWindow>(duel.PendingResponse);
        Assert.Equal(TrapTrigger.OpponentAttackDeclared, window.Trigger);
        Assert.Same(duel.FirstPlayer, window.Responder);
        Assert.Same(attacker, window.TriggerMonster);
        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints); // 戦闘は保留
    }

    [Fact]
    public void Attack_ResolvesImmediately_WhenOpponentHasNoTrap()
    {
        var duel = CreateAttackScenario(TestCards.Spell(new DealDamageAction(100)));

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Null(duel.PendingResponse);
        Assert.Equal(InitialLifePoints - 1800, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void Attack_DoesNotOpenWindow_ForTrapWithDifferentTrigger()
    {
        var duel = CreateAttackScenario(TestCards.DestroySummonedTrap());

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Null(duel.PendingResponse);
    }

    [Fact]
    public void Attack_DoesNotOpenWindow_ForTrapSetThisTurn()
    {
        var duel = DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()).Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.FirstPlayer.PlaceSpellTrap(TestCards.DestroyAttackerTrap(), 0, setTurn: 2);

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Null(duel.PendingResponse);
    }

    [Fact]
    public void Attack_OpensWindow_ForTrapSetOnEarlierTurn()
    {
        // 後攻が2ターン目にセットした罠を、先攻の3ターン目の攻撃に対して発動できる
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1800))
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        duel.SetSpellTrap(0);
        duel.AdvanceTo(3, Phase.Battle);

        duel.Attack(duel.FirstPlayer.MonsterZone[0], target: null);

        Assert.Same(duel.SecondPlayer, duel.PendingResponse?.Responder);
    }

    [Fact]
    public void SetSpellTrap_RecordsSetTurn()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.DestroySummonedTrap()).Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.SetSpellTrap(0);

        Assert.Equal(1, duel.FirstPlayer.SpellTrapZone[0].SetTurn);
    }

    [Fact]
    public void OtherActions_Fail_WhileResponsePending()
    {
        var duel = CreateAttackScenario(TestCards.DestroyAttackerTrap());
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.ResponsePending, duel.AdvancePhase().Error);
        Assert.Equal(ActionError.ResponsePending, duel.CanAttack(duel.SecondPlayer.MonsterZone[0], null).Error);
        Assert.Equal(ActionError.ResponsePending, duel.CanSummon(0, BattlePosition.FaceUpAttack).Error);
        Assert.Equal(ActionError.ResponsePending, duel.CanSetSpellTrap(0).Error);
    }

    // ---- 攻撃宣言に対する罠の発動 ----

    [Fact]
    public void ActivateTrap_DestroysAttacker_AndCancelsBattle()
    {
        var duel = CreateAttackScenario(TestCards.DestroyAttackerTrap());
        var trap = duel.FirstPlayer.SpellTrapZone[0];
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var result = duel.ActivateTrap(trap, target: null);

        Assert.True(result.IsSuccess);
        Assert.Null(duel.PendingResponse);
        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints);
        Assert.Equal(trap.Card, Assert.Single(duel.FirstPlayer.Graveyard));
        Assert.Empty(duel.FirstPlayer.SpellTrapZone);
    }

    [Fact]
    public void ActivateTrap_NegatesAttack_AndLogsIt()
    {
        var duel = CreateAttackScenario(TestCards.NegateAttackTrap());
        var attacker = duel.SecondPlayer.MonsterZone[0];
        duel.Attack(attacker, target: null);

        duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], target: null);

        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints);
        Assert.Same(attacker, Assert.Single(duel.SecondPlayer.MonsterZone));
        Assert.True(attacker.HasAttacked);
        Assert.Equal(new DuelLogEntry(2, DuelLogKind.AttackNegated, duel.SecondPlayer.Name, attacker.Card.Name), duel.Log[^1]);
    }

    [Fact]
    public void ActivateTrap_ContinuesBattle_WhenAttackerSurvives()
    {
        var duel = CreateAttackScenario(
            TestCards.Trap(TrapTrigger.OpponentAttackDeclared, new GainLifePointsAction(1000)));
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], target: null);

        Assert.Equal(InitialLifePoints + 1000 - 1800, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void ActivateTrap_LogsActivationBeforeEffect()
    {
        var duel = CreateAttackScenario(TestCards.DestroyAttackerTrap());
        var trap = duel.FirstPlayer.SpellTrapZone[0];
        var attacker = duel.SecondPlayer.MonsterZone[0];
        duel.Attack(attacker, target: null);
        var countBefore = duel.Log.Count;

        duel.ActivateTrap(trap, target: null);

        DuelLogEntry[] expected =
        [
            new(2, DuelLogKind.TrapActivated, duel.FirstPlayer.Name, trap.Card.Name),
            new(2, DuelLogKind.Destroyed, duel.SecondPlayer.Name, attacker.Card.Name),
        ];
        Assert.Equal(expected, duel.Log.Skip(countBefore));
    }

    [Fact]
    public void ActivateTrap_EndsDuel_WhenTrapDamageIsLethal()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 500 })
            .WithSetSpellTrap(Side.First, TestCards.Trap(TrapTrigger.OpponentAttackDeclared, new DealDamageAction(500)))
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], target: null);

        Assert.Same(duel.FirstPlayer, duel.Winner);
        Assert.Equal(500, duel.FirstPlayer.LifePoints); // 決着後に保留中の戦闘は行わない
        Assert.Single(duel.Log, e => e.Kind == DuelLogKind.Won);
    }

    // ---- 見送り ----

    [Fact]
    public void PassResponse_ResolvesPendingBattle()
    {
        var duel = CreateAttackScenario(TestCards.DestroyAttackerTrap());
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var result = duel.PassResponse();

        Assert.True(result.IsSuccess);
        Assert.Null(duel.PendingResponse);
        Assert.Equal(InitialLifePoints - 1800, duel.FirstPlayer.LifePoints);
        Assert.Single(duel.FirstPlayer.SpellTrapZone);
    }

    [Fact]
    public void PassResponse_FlipsFaceDownTargetDuringBattle()
    {
        var target = TestCards.Monster(defense: 500);
        var builder = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .WithFieldMonster(Side.First, target, BattlePosition.FaceDownDefense);
        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);
        Assert.DoesNotContain(duel.Log, e => e.Kind == DuelLogKind.Flipped); // 表にするのは戦闘時

        duel.PassResponse();

        Assert.Contains(duel.Log, e => e.Kind == DuelLogKind.Flipped);
        Assert.Contains(target, duel.FirstPlayer.Graveyard);
    }

    [Fact]
    public void PassResponse_Fails_WhenNoWindow()
    {
        var duel = DuelBuilder.Create().Build();

        Assert.Equal(ActionError.NoResponseWindow, duel.PassResponse().Error);
    }

    // ---- 召喚に対する罠 ----

    [Fact]
    public void Summon_OpensResponseWindow_AndTrapDestroysSummonedMonster()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithHand(Side.Second, TestCards.Monster(attack: 1800))
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);
        var window = Assert.IsType<ResponseWindow>(duel.PendingResponse);
        Assert.Equal(TrapTrigger.OpponentMonsterSummoned, window.Trigger);

        duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], target: null);

        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.True(duel.NormalSummonUsed);
        Assert.Null(duel.PendingResponse);
    }

    [Fact]
    public void Summon_DoesNotOpenWindow_WhenSet()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithHand(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Main1);

        duel.Summon(0, BattlePosition.FaceDownDefense);

        Assert.Null(duel.PendingResponse);
    }

    // ---- 発動できない罠 ----

    [Fact]
    public void ActivateTrap_Fails_WhenNoWindow()
    {
        var duel = DuelBuilder.Create().WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap()).Build();

        Assert.Equal(ActionError.NoResponseWindow, duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], null).Error);
    }

    [Fact]
    public void ActivateTrap_Fails_ForTurnPlayersOwnTrap()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithSetSpellTrap(Side.Second, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var result = duel.ActivateTrap(duel.SecondPlayer.SpellTrapZone[0], null);

        Assert.Equal(ActionError.CardNotOnYourField, result.Error);
    }

    [Fact]
    public void ActivateTrap_Fails_ForSpellCard()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.Spell(new DealDamageAction(100)))
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(ActionError.NotATrap, duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], null).Error);
    }

    [Fact]
    public void FindActivatableTraps_ReturnsOnlyMatchingTraps()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithSetSpellTrap(Side.First, TestCards.NegateAttackTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var traps = duel.FindActivatableTraps();

        Assert.Equal(2, traps.Count);
        Assert.All(traps, t => Assert.Equal(TrapTrigger.OpponentAttackDeclared, t.Card.Effect?.Trigger));
    }

    [Fact]
    public void SpellWithTrigger_CannotBeActivatedAsSpell()
    {
        var spell = new CardData(7, "TriggerSpell", CardKind.Spell,
            Effect: new CardEffect([new DealDamageAction(100)], Trigger: TrapTrigger.OpponentAttackDeclared));
        var duel = DuelBuilder.Create().WithHand(Side.First, spell).Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.ActivationConditionNotMet, duel.ActivateSpellFromHand(0, null).Error);
    }
}
