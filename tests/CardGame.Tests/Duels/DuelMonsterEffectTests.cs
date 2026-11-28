using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>効果モンスターの効果(リバース・召喚成功時)。</summary>
public class DuelMonsterEffectTests
{
    private const int InitialLifePoints = 8000;

    private static CardData SummonDamageMonster(int amount = 400) =>
        TestCards.EffectMonster(MonsterEffectTrigger.Summoned, 1000, 800, new DealDamageAction(amount));

    private static CardData FlipDrawMonster() =>
        TestCards.EffectMonster(MonsterEffectTrigger.Flipped, 800, 1300, new DrawCardsAction(1));

    private static Duel CreateDuelAtMain1(DuelBuilder builder, int turn = 1)
    {
        var duel = builder.Build();
        duel.AdvanceTo(turn, Phase.Main1);
        return duel;
    }

    // ---- 召喚成功時 ----

    [Fact]
    public void Summon_ResolvesSummonEffect()
    {
        var card = SummonDamageMonster();
        var duel = CreateDuelAtMain1(DuelBuilder.Create().WithHand(Side.First, card));

        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(InitialLifePoints - 400, duel.SecondPlayer.LifePoints);
        Assert.Contains(new DuelLogEntry(1, DuelLogKind.MonsterEffectActivated, duel.FirstPlayer.Name, card.Name), duel.Log);
    }

    [Fact]
    public void Summon_LogsEffectAfterSummon()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create().WithHand(Side.First, SummonDamageMonster()));

        duel.Summon(0, BattlePosition.FaceUpAttack);

        var kinds = duel.Log.Select(e => e.Kind).ToList();
        Assert.True(kinds.IndexOf(DuelLogKind.Summoned) < kinds.IndexOf(DuelLogKind.MonsterEffectActivated));
    }

    [Fact]
    public void Set_DoesNotTriggerSummonEffect()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create().WithHand(Side.First, SummonDamageMonster()));

        duel.Summon(0, BattlePosition.FaceDownDefense);

        Assert.Equal(InitialLifePoints, duel.SecondPlayer.LifePoints);
        Assert.DoesNotContain(duel.Log, e => e.Kind == DuelLogKind.MonsterEffectActivated);
    }

    [Fact]
    public void Summon_DoesNotTriggerFlipEffect()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create().WithHand(Side.First, FlipDrawMonster()));

        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Empty(duel.FirstPlayer.Hand);
    }

    [Fact]
    public void Summon_EndsDuel_WhenEffectDamageIsLethal()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 400 })
            .WithHand(Side.First, SummonDamageMonster(400)));

        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Same(duel.FirstPlayer, duel.Winner);
    }

    // ---- リバース ----

    [Fact]
    public void FlipSummon_ResolvesFlipEffect()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, FlipDrawMonster(), BattlePosition.FaceDownDefense));

        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]);

        Assert.Single(duel.FirstPlayer.Hand);
    }

    [Fact]
    public void BeingAttacked_ResolvesFlipEffect_AfterBattle_EvenIfDestroyed()
    {
        // 守備力1300 の裏側モンスターを攻撃力1800で攻撃して破壊しても、リバース効果は発動する(効果を使うのは攻撃された側)
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .WithFieldMonster(Side.First, FlipDrawMonster(), BattlePosition.FaceDownDefense), turn: 2);
        duel.AdvanceTo(2, Phase.Battle);
        var handBefore = duel.FirstPlayer.Hand.Count;

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        Assert.Empty(duel.FirstPlayer.MonsterZone);
        Assert.Equal(handBefore + 1, duel.FirstPlayer.Hand.Count);
        var kinds = duel.Log.Select(e => e.Kind).ToList();
        Assert.True(kinds.IndexOf(DuelLogKind.Destroyed) < kinds.IndexOf(DuelLogKind.MonsterEffectActivated));
    }

    [Fact]
    public void AttackingFaceUpMonster_DoesNotTriggerFlipEffect()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .WithFieldMonster(Side.First, FlipDrawMonster(), BattlePosition.FaceUpDefense), turn: 2);
        duel.AdvanceTo(2, Phase.Battle);
        var handBefore = duel.FirstPlayer.Hand.Count;

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        Assert.Equal(handBefore, duel.FirstPlayer.Hand.Count);
    }

    // ---- 対象を取る効果 ----

    [Fact]
    public void TargetEffect_WaitsForTargetChoice()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster()));
        var source = duel.FirstPlayer.MonsterZone[0];

        duel.ChangePosition(source);

        var pending = Assert.IsType<PendingMonsterEffect>(duel.PendingEffectTarget);
        Assert.Same(duel.FirstPlayer, pending.Controller);
        Assert.Same(source, pending.Source);
        Assert.Equal(2, duel.FindEffectTargetChoices().Count); // お互いのモンスターが対象にできる
    }

    [Fact]
    public void ChooseEffectTarget_DestroysChosenMonster()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster()));
        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]);
        var target = duel.SecondPlayer.MonsterZone[0];

        var result = duel.ChooseEffectTarget(target);

        Assert.True(result.IsSuccess);
        Assert.Null(duel.PendingEffectTarget);
        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.Equal(target.Card.Name, duel.Log.Last(e => e.Kind == DuelLogKind.MonsterEffectActivated).TargetCardName);
    }

    [Fact]
    public void ChooseEffectTarget_Fails_ForInvalidTarget()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(MonsterSide.Opponent), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster()));
        var source = duel.FirstPlayer.MonsterZone[0];
        duel.ChangePosition(source);

        Assert.Equal(ActionError.InvalidTarget, duel.ChooseEffectTarget(source).Error);
        Assert.NotNull(duel.PendingEffectTarget);
    }

    [Fact]
    public void ChooseEffectTarget_Fails_WhenNothingPending()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create().WithFieldMonster(Side.Second, TestCards.Monster()));

        Assert.Equal(ActionError.NoPendingEffect, duel.ChooseEffectTarget(duel.SecondPlayer.MonsterZone[0]).Error);
    }

    [Fact]
    public void OtherActions_Fail_WhileEffectTargetPending()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(), BattlePosition.FaceDownDefense)
            .WithFieldMonster(Side.Second, TestCards.Monster()));
        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]);

        Assert.Equal(ActionError.EffectTargetPending, duel.AdvancePhase().Error);
        Assert.Equal(ActionError.EffectTargetPending, duel.CanSummon(0, BattlePosition.FaceUpAttack).Error);
        Assert.Equal(ActionError.EffectTargetPending, duel.CanSetSpellTrap(0).Error);
    }

    [Fact]
    public void TargetEffect_Fizzles_WhenNoTarget()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(MonsterSide.Opponent), BattlePosition.FaceDownDefense));

        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]);

        Assert.Null(duel.PendingEffectTarget);
        Assert.Equal(DuelLogKind.MonsterEffectFizzled, duel.Log[^1].Kind);
    }

    [Fact]
    public void FlipEffectOfAttackedMonster_IsChosenByItsController()
    {
        // 後攻の攻撃で表になった先攻のモンスターの効果は、先攻(ターンプレイヤーではない側)が対象を選ぶ
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 2000))
            .WithFieldMonster(Side.First, TestCards.FlipDestroyMonster(defense: 1000), BattlePosition.FaceDownDefense), turn: 2);
        duel.AdvanceTo(2, Phase.Battle);

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        Assert.Same(duel.FirstPlayer, duel.PendingEffectTarget?.Controller);
        duel.ChooseEffectTarget(duel.SecondPlayer.MonsterSlots[1] ?? throw new InvalidOperationException());
        Assert.Single(duel.SecondPlayer.MonsterZone);
        Assert.Equal(500, duel.SecondPlayer.MonsterZone[0].Card.Attack);
    }

    // ---- 罠との順番 ----

    [Fact]
    public void SummonEffect_ResolvesAfterOpponentsTrapResponse()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithHand(Side.Second, SummonDamageMonster()), turn: 2);

        duel.Summon(0, BattlePosition.FaceUpAttack);
        Assert.NotNull(duel.PendingResponse);
        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints); // 罠の機会が閉じるまで効果は待つ

        duel.ActivateTrap(duel.FirstPlayer.SpellTrapZone[0], target: null);

        // 罠でモンスターは破壊されたが、召喚成功時の効果は処理される
        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.Equal(InitialLifePoints - 400, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void FlipSummonEffect_ResolvesAfterOpponentPasses()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithFieldMonster(Side.Second, FlipDrawMonster(), BattlePosition.FaceDownDefense), turn: 2);
        var handBefore = duel.SecondPlayer.Hand.Count;
        duel.ChangePosition(duel.SecondPlayer.MonsterZone[0]);
        Assert.NotNull(duel.PendingResponse);

        duel.PassResponse();

        Assert.Equal(handBefore + 1, duel.SecondPlayer.Hand.Count);
    }

    // ---- 発動できない状態 ----

    [Fact]
    public void DrawEffect_Fizzles_WhenDeckEmpty()
    {
        var duel = CreateDuelAtMain1(DuelBuilder.Create()
            .WithFieldMonster(Side.First, FlipDrawMonster(), BattlePosition.FaceDownDefense)
            .WithDeckSize(Side.First, 0));

        duel.ChangePosition(duel.FirstPlayer.MonsterZone[0]);

        Assert.Equal(DuelLogKind.MonsterEffectFizzled, duel.Log[^1].Kind);
        Assert.False(duel.IsFinished);
    }
}
