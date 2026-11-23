using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU は後攻(Second)として、2ターン目に行動させる。</summary>
public class SimpleCpuStrategyTests
{
    private const int InitialLifePoints = 8000;

    private static Duel StartCpuTurn(DuelBuilder builder)
    {
        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Draw);
        return duel;
    }

    [Fact]
    public void PlayTurn_PassesTurnToOpponent()
    {
        var duel = StartCpuTurn(DuelBuilder.Create());

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Equal(3, duel.TurnCount);
        Assert.Same(duel.FirstPlayer, duel.CurrentPlayer);
        Assert.Equal(Phase.Draw, duel.CurrentPhase);
    }

    [Fact]
    public void PlayTurn_SummonsStrongestMonster_WhenItIsNotWeakerThanOpponent()
    {
        var weak = TestCards.Monster(attack: 1000);
        var strong = TestCards.Monster(attack: 1800);
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, weak, strong)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 1800)));   // 同値なら攻撃表示で出す

        new SimpleCpuStrategy().PlayTurn(duel);

        var summoned = duel.SecondPlayer.MonsterZone.Single(m => m.Card == strong);
        Assert.Equal(BattlePosition.FaceUpAttack, summoned.Position);
    }

    [Fact]
    public void PlayTurn_SetsSturdiestMonster_WhenOpponentIsStronger()
    {
        var attacker = TestCards.Monster(attack: 1500, defense: 500);
        var wall = TestCards.Monster(attack: 500, defense: 2000);
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, attacker, wall)
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 2500)));

        new SimpleCpuStrategy().PlayTurn(duel);

        var monster = Assert.Single(duel.SecondPlayer.MonsterZone);
        Assert.Equal(wall, monster.Card);
        Assert.Equal(BattlePosition.FaceDownDefense, monster.Position);
    }

    [Fact]
    public void PlayTurn_DoesNotSummon_WhenNoSummonableMonster()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Spell(), TestCards.Monster(level: 7)));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Empty(duel.SecondPlayer.MonsterZone);
    }

    [Fact]
    public void PlayTurn_AttacksDirectly_WhenOpponentHasNoMonsters()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1200)));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Equal(InitialLifePoints - 1200, duel.FirstPlayer.LifePoints);
    }

    [Fact]
    public void PlayTurn_AttacksStrongestBeatableTarget()
    {
        var beatableWeak = TestCards.Monster(attack: 1000);
        var beatableStrong = TestCards.Monster(attack: 1500);
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1800))
            .WithFieldMonster(Side.First, beatableWeak)
            .WithFieldMonster(Side.First, beatableStrong));

        new SimpleCpuStrategy().PlayTurn(duel);

        var remaining = Assert.Single(duel.FirstPlayer.MonsterZone);
        Assert.Equal(beatableWeak, remaining.Card);
        Assert.Equal(InitialLifePoints - 300, duel.FirstPlayer.LifePoints);
    }

    [Theory]
    [InlineData(1500, BattlePosition.FaceUpAttack)]    // 相打ちになる
    [InlineData(2000, BattlePosition.FaceUpAttack)]    // 返り討ちにあう
    public void PlayTurn_DoesNotAttack_WhenItWouldLoseAttacker(int targetAttack, BattlePosition position)
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1500))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: targetAttack), position));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Single(duel.SecondPlayer.MonsterZone);
        Assert.Single(duel.FirstPlayer.MonsterZone);
        Assert.Equal(InitialLifePoints, duel.SecondPlayer.LifePoints);
    }

    [Fact]
    public void PlayTurn_AttacksFaceDown_OnlyWhenAboveAssumedDefense()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: SimpleCpuStrategy.AssumedFaceDownDefense))
            .WithFieldMonster(Side.First, TestCards.Monster(defense: 0), BattlePosition.FaceDownDefense));

        new SimpleCpuStrategy().PlayTurn(duel);

        // 攻撃力が仮定守備力と同じなので攻撃しない(実際の守備力は 0 でも見えない)
        Assert.Equal(BattlePosition.FaceDownDefense, duel.FirstPlayer.MonsterZone[0].Position);
    }

    [Fact]
    public void PlayTurn_StopsAndKeepsTurn_WhenDuelWon()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 1000 })
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000)));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Same(duel.SecondPlayer, duel.Winner);
        Assert.Equal(2, duel.TurnCount);
    }

    [Fact]
    public void PlayTurn_CompletesWholeDuel_WithoutGettingStuck()
    {
        var rules = new GameRulesOptions();
        var random = new Random(1);
        var cpu = new SimpleCpuStrategy();
        var duel = new Duel(
            new Player("A", ShuffleDeck.Execute(SampleDeck.Create(), random), rules),
            new Player("B", ShuffleDeck.Execute(SampleDeck.Create(), random), rules),
            rules);

        // 両者を CPU に任せ、必ず決着がつく(遅くともデッキ切れで終わる)ことを確認
        const int maxTurns = 100;
        while (!duel.IsFinished && duel.TurnCount <= maxTurns)
        {
            cpu.PlayTurn(duel);
        }

        Assert.True(duel.IsFinished);
    }
}
