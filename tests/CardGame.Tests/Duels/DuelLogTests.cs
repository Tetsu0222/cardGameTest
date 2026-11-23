using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

public class DuelLogTests
{
    [Fact]
    public void Constructor_LogsFirstTurnStart()
    {
        var duel = DuelBuilder.Create().Build();

        var entry = Assert.Single(duel.Log);
        Assert.Equal(new DuelLogEntry(1, DuelLogKind.TurnStarted, duel.FirstPlayer.Name), entry);
    }

    [Fact]
    public void Summon_LogsSummoned_WhenAttackPosition()
    {
        var card = TestCards.Monster();
        var duel = DuelBuilder.Create().WithHand(Side.First, card).Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(new DuelLogEntry(1, DuelLogKind.Summoned, duel.FirstPlayer.Name, card.Name), duel.Log[^1]);
    }

    [Fact]
    public void Summon_LogsSet_WhenFaceDown()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.Summon(0, BattlePosition.FaceDownDefense);

        Assert.Equal(DuelLogKind.Set, duel.Log[^1].Kind);
    }

    [Fact]
    public void Summon_DoesNotLog_WhenRejected()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();
        var countBefore = duel.Log.Count;

        duel.Summon(0, BattlePosition.FaceUpAttack); // ドローフェーズなので拒否される

        Assert.Equal(countBefore, duel.Log.Count);
    }

    [Fact]
    public void Attack_LogsAttackDestroyAndDamage_InOrder()
    {
        var attacker = TestCards.Monster(attack: 2000);
        var target = TestCards.Monster(attack: 1500);
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, attacker)
            .WithFieldMonster(Side.First, target)
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        var countBefore = duel.Log.Count;

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        DuelLogEntry[] expected =
        [
            new(2, DuelLogKind.Attacked, duel.SecondPlayer.Name, attacker.Name, target.Name),
            new(2, DuelLogKind.Destroyed, duel.FirstPlayer.Name, target.Name),
            new(2, DuelLogKind.Damaged, duel.FirstPlayer.Name, Amount: 500),
        ];
        Assert.Equal(expected, duel.Log.Skip(countBefore));
    }

    [Fact]
    public void Attack_HidesTargetNameThenLogsFlip_WhenTargetFaceDown()
    {
        var target = TestCards.Monster(defense: 500);
        var duel = DuelBuilder.Create()
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.First, target, BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(2, Phase.Battle);
        var countBefore = duel.Log.Count;

        duel.Attack(duel.SecondPlayer.MonsterZone[0], duel.FirstPlayer.MonsterZone[0]);

        var entries = duel.Log.Skip(countBefore).ToList();
        Assert.Equal(DuelLogKind.Attacked, entries[0].Kind);
        Assert.Null(entries[0].TargetCardName);
        Assert.Equal(new DuelLogEntry(2, DuelLogKind.Flipped, duel.FirstPlayer.Name, target.Name), entries[1]);
    }

    [Fact]
    public void Attack_LogsWinner_WhenLifePointsReachZero()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 1000 })
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        duel.Attack(duel.SecondPlayer.MonsterZone[0], target: null);

        Assert.Equal(new DuelLogEntry(2, DuelLogKind.Won, duel.SecondPlayer.Name), duel.Log[^1]);
    }

    [Fact]
    public void AdvancePhase_LogsDeckOutAndWinner_WhenDeckEmpty()
    {
        var duel = DuelBuilder.Create().WithDeckSize(Side.Second, 0).Build();
        duel.AdvanceTo(1, Phase.End);

        duel.AdvancePhase();

        Assert.Equal(new DuelLogEntry(2, DuelLogKind.DeckOut, duel.SecondPlayer.Name), duel.Log[^2]);
        Assert.Equal(new DuelLogEntry(2, DuelLogKind.Won, duel.FirstPlayer.Name), duel.Log[^1]);
    }
}
