using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の罠のセットと、相手の行動への応じ方。</summary>
public class SimpleCpuStrategyTrapTests
{
    private const int InitialLifePoints = 8000;

    private readonly SimpleCpuStrategy _cpu = new();

    /// <summary>CPU(First)が罠をセットしてあり、相手(Second)が2ターン目に攻撃を宣言した状態。</summary>
    private static Duel CreateAttackAgainstCpu(CardData trap, int attackerAttack, CardData? cpuMonster = null, BattlePosition position = BattlePosition.FaceUpAttack)
    {
        var builder = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, trap)
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: attackerAttack));
        if (cpuMonster is not null) builder.WithFieldMonster(Side.First, cpuMonster, position);

        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone.FirstOrDefault());
        Assert.NotNull(duel.PendingResponse);
        return duel;
    }

    // ---- セット ----

    [Fact]
    public void DecideNextAction_SetsTrap_AfterSummonInMain1()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(), TestCards.DestroyAttackerTrap())
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);

        var action = Assert.IsType<CpuSetSpellTrapAction>(_cpu.DecideNextAction(duel));

        Assert.Equal(0, action.HandIndex);
    }

    [Fact]
    public void PlayTurn_SetsAllTraps()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.DestroyAttackerTrap(), TestCards.NegateAttackTrap())
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Equal(2, duel.SecondPlayer.SpellTrapZone.Count);
    }

    // ---- 攻撃宣言への応じ方 ----

    [Fact]
    public void DecideNextAction_ActivatesDestroyTrap_AgainstAttack()
    {
        var duel = CreateAttackAgainstCpu(TestCards.DestroyAttackerTrap(), attackerAttack: 1000);

        var action = Assert.IsType<CpuActivateTrapAction>(_cpu.DecideNextAction(duel));

        Assert.Same(duel.FirstPlayer.SpellTrapZone[0], action.Trap);
    }

    [Fact]
    public void DecideNextAction_UsesNegateTrap_AgainstDirectAttack()
    {
        var duel = CreateAttackAgainstCpu(TestCards.NegateAttackTrap(), attackerAttack: 1000);

        Assert.IsType<CpuActivateTrapAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_KeepsNegateTrap_WhenAttackIsHarmless()
    {
        // 攻撃力1000 で守備力2000 の裏守備に攻撃しても、CPU は損をしない(攻撃側が反射ダメージを受ける)
        var duel = CreateAttackAgainstCpu(
            TestCards.NegateAttackTrap(),
            attackerAttack: 1000,
            TestCards.Monster(defense: 2000),
            BattlePosition.FaceDownDefense);

        Assert.IsType<CpuPassResponseAction>(_cpu.DecideNextAction(duel));
    }

    [Fact]
    public void DecideNextAction_PrefersDestroyTrap_OverNegateTrap()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.NegateAttackTrap())
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        var action = Assert.IsType<CpuActivateTrapAction>(_cpu.DecideNextAction(duel));

        Assert.IsType<DestroyAttackingMonsterAction>(Assert.Single(action.Trap.Card.Effect?.Actions ?? []));
    }

    // ---- 召喚への応じ方 ----

    [Theory]
    [InlineData(SimpleCpuStrategy.MinAttackWorthTrapping, true)]
    [InlineData(SimpleCpuStrategy.MinAttackWorthTrapping - 1, false)]
    public void DecideNextAction_DestroysSummonedMonster_OnlyWhenStrongEnough(int attack, bool expectActivate)
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroySummonedTrap())
            .WithHand(Side.Second, TestCards.Monster(attack: attack))
            .Build();
        duel.AdvanceTo(2, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);

        var action = _cpu.DecideNextAction(duel);

        Assert.Equal(expectActivate, action is CpuActivateTrapAction);
    }

    // ---- 一連の流れ ----

    [Fact]
    public void PlayTurn_RespondsForOpponent_WhenAttackTriggersTheirTrap()
    {
        // CPU 同士の対戦では、攻撃したターンプレイヤーの手番中に、相手側の罠の判断も行う
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroyAttackerTrap())
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500))
            .Build();
        duel.AdvanceTo(2, Phase.Draw);

        _cpu.PlayTurn(duel);

        Assert.Empty(duel.SecondPlayer.MonsterZone);
        Assert.Equal(InitialLifePoints, duel.FirstPlayer.LifePoints);
        Assert.Equal(3, duel.TurnCount);
    }

    [Fact]
    public void Execute_PassesResponse_WhenTrapActivationRejected()
    {
        var duel = CreateAttackAgainstCpu(TestCards.DestroyAttackerTrap(), attackerAttack: 1000);
        var notOnField = new SpellTrapCard(TestCards.DestroyAttackerTrap());

        var result = SimpleCpuStrategy.Execute(duel, new CpuActivateTrapAction(notOnField, Target: null));

        Assert.False(result.IsSuccess);
        Assert.Null(duel.PendingResponse);
        Assert.Equal(InitialLifePoints - 1000, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void PlayTurn_CompletesWholeDuelWithTraps_WithoutGettingStuck()
    {
        var rules = new GameRulesOptions();
        var random = new Random(7);
        var duel = new Duel(
            new Player("A", ShuffleDeck.Execute(SampleDeck.Create(), random), rules),
            new Player("B", ShuffleDeck.Execute(SampleDeck.Create(), random), rules),
            rules);

        const int maxTurns = 100;
        while (!duel.IsFinished && duel.TurnCount <= maxTurns)
        {
            _cpu.PlayTurn(duel);
        }

        Assert.True(duel.IsFinished);
        Assert.Null(duel.PendingResponse);
    }
}
