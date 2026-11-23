using CardGame.AI.Strategies;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Strategies;

/// <summary>CPU の魔法の使い方。CPU は後攻(Second)として、2ターン目に行動させる。</summary>
public class SimpleCpuStrategySpellTests
{
    private const int InitialLifePoints = 8000;

    private static Duel StartCpuTurn(DuelBuilder builder)
    {
        var duel = builder.Build();
        duel.AdvanceTo(2, Phase.Draw);
        return duel;
    }

    [Fact]
    public void PlayTurn_ActivatesDamageSpell()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Spell(new DealDamageAction(600))));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Equal(InitialLifePoints - 600, duel.FirstPlayer.LifePoints);
        Assert.DoesNotContain(duel.SecondPlayer.Hand, c => c.Effect is not null);
    }

    [Fact]
    public void PlayTurn_ActivatesSpellsBeforeSummoning()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Monster(), TestCards.Spell(new DealDamageAction(100))));

        new SimpleCpuStrategy().PlayTurn(duel);

        var kinds = duel.Log.Where(e => e.Turn == 2).Select(e => e.Kind).ToList();
        Assert.True(kinds.IndexOf(DuelLogKind.SpellActivated) < kinds.IndexOf(DuelLogKind.Summoned));
    }

    [Fact]
    public void PlayTurn_DestroysStrongestVisibleOpponentMonster_WithTargetSpell()
    {
        var weak = TestCards.Monster(attack: 1000);
        var strong = TestCards.Monster(attack: 2500);
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.DestroyTargetSpell(MonsterSide.Both))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 3000))
            .WithFieldMonster(Side.First, weak)
            .WithFieldMonster(Side.First, strong));

        new SimpleCpuStrategy().PlayTurn(duel);

        // 自分のモンスター(攻撃力3000)は対象にせず、相手の攻撃力2500を破壊する
        Assert.Contains(strong, duel.FirstPlayer.Graveyard);
        Assert.Contains(duel.SecondPlayer.MonsterZone, m => m.Card.Attack == 3000);
    }

    [Fact]
    public void PlayTurn_KeepsTargetSpell_WhenNoOpponentMonster()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.DestroyTargetSpell(MonsterSide.Both))
            .WithFieldMonster(Side.Second, TestCards.Monster()));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Single(duel.SecondPlayer.MonsterZone);
        Assert.Contains(duel.SecondPlayer.Hand, c => c.Effect is not null);
    }

    [Fact]
    public void PlayTurn_UsesBoardWipe_WhenOpponentHasMoreMonsters()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Spell(new DestroyAllMonstersAction(MonsterSide.Both)))
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster()));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Empty(duel.FirstPlayer.MonsterZone);
    }

    [Fact]
    public void PlayTurn_KeepsBoardWipe_WhenItWouldNotGainAdvantage()
    {
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Spell(new DestroyAllMonstersAction(MonsterSide.Both)))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 500))
            .WithFieldMonster(Side.First, TestCards.Monster(attack: 500)));

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Single(duel.FirstPlayer.MonsterZone);
        Assert.Contains(duel.SecondPlayer.Hand, c => c.Effect is not null);
    }

    [Fact]
    public void PlayTurn_ActivatesSpellsDrawnByDrawSpell()
    {
        // ターン開始時のドローで1枚目(Filler)を引き、ドロー魔法で2枚目のダメージ魔法を引いて、それも続けて発動する
        var duel = StartCpuTurn(DuelBuilder.Create()
            .WithHand(Side.Second, TestCards.Spell(new DrawCardsAction(1)))
            .WithDeckTop(Side.Second, TestCards.Filler(), TestCards.Spell(new DealDamageAction(100))));
        Assert.DoesNotContain(duel.SecondPlayer.Hand, c => c.Effect is { Actions: [DealDamageAction] });

        new SimpleCpuStrategy().PlayTurn(duel);

        Assert.Equal(InitialLifePoints - 100, duel.FirstPlayer.LifePoints);
        Assert.Equal(2, duel.SecondPlayer.Graveyard.Count);
    }
}
