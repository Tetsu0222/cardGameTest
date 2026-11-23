using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

/// <summary>アドバンス召喚(レベル5以上のモンスターを、自分のモンスターをリリースして召喚する)。</summary>
public class DuelTributeSummonTests
{
    private static Duel CreateDuelAtMain1(CardData hand, int ownMonsterCount, BattlePosition fieldPosition = BattlePosition.FaceUpAttack)
    {
        var builder = DuelBuilder.Create().WithHand(Side.First, hand);
        for (var i = 0; i < ownMonsterCount; i++)
        {
            builder.WithFieldMonster(Side.First, TestCards.Monster(attack: 100 * (i + 1)), fieldPosition);
        }

        var duel = builder.Build();
        duel.AdvanceTo(1, Phase.Main1);
        return duel;
    }

    // ---- 成功 ----

    [Fact]
    public void Summon_TributesOneMonster_ForLevel5()
    {
        var card = TestCards.Monster(attack: 2300, level: 5);
        var duel = CreateDuelAtMain1(card, ownMonsterCount: 1);
        var tribute = duel.FirstPlayer.MonsterZone[0];

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 0, [tribute]);

        Assert.True(result.IsSuccess);
        var summoned = Assert.Single(duel.FirstPlayer.MonsterZone);
        Assert.Equal(card, summoned.Card);
        Assert.Equal(tribute.Card, Assert.Single(duel.FirstPlayer.Graveyard));
        Assert.True(duel.NormalSummonUsed);
    }

    [Fact]
    public void Summon_TributesTwoMonsters_ForLevel7()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(attack: 2800, level: 7), ownMonsterCount: 3);
        var tributes = duel.FirstPlayer.MonsterZone.Take(2).ToList();

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 0, tributes);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, duel.FirstPlayer.MonsterZone.Count);
        Assert.Equal(2, duel.FirstPlayer.Graveyard.Count);
    }

    [Fact]
    public void Summon_CanTributeFaceDownMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 1, BattlePosition.FaceDownDefense);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 0, [duel.FirstPlayer.MonsterZone[0]]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Summon_CanSetWithTribute()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 1);

        var result = duel.Summon(0, BattlePosition.FaceDownDefense, 0, [duel.FirstPlayer.MonsterZone[0]]);

        Assert.True(result.IsSuccess);
        Assert.Equal(BattlePosition.FaceDownDefense, duel.FirstPlayer.MonsterZone[0].Position);
    }

    [Fact]
    public void Summon_CanPlaceInEmptySlotOtherThanTributeSlot()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 1);
        var tribute = duel.FirstPlayer.MonsterZone[0];

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 3, [tribute]);

        Assert.True(result.IsSuccess);
        Assert.Null(duel.FirstPlayer.MonsterSlots[0]);
        Assert.NotNull(duel.FirstPlayer.MonsterSlots[3]);
    }

    [Fact]
    public void Summon_CanPlaceInTributeSlot_WhenZoneIsFull()
    {
        var card = TestCards.Monster(attack: 2300, level: 5);
        var duel = CreateDuelAtMain1(card, ownMonsterCount: Player.MonsterZoneSize);
        var tribute = duel.FirstPlayer.MonsterSlots[2] ?? throw new InvalidOperationException();
        var others = duel.FirstPlayer.MonsterSlots.Where(m => m != tribute).ToList();

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 2, [tribute]);

        Assert.True(result.IsSuccess);
        Assert.Equal(card, duel.FirstPlayer.MonsterSlots[2]?.Card);
        Assert.All(others, m => Assert.Contains(m, duel.FirstPlayer.MonsterSlots));
    }

    [Fact]
    public void Summon_UsesLeftmostOfEmptyAndTributeSlots_WhenSlotNotSpecified()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 3);
        var tribute = duel.FirstPlayer.MonsterSlots[1] ?? throw new InvalidOperationException();

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, [tribute]);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, duel.FirstPlayer.MonsterSlots[1]?.Card.Level);
    }

    [Fact]
    public void Summon_LogsTributesBeforeSummon()
    {
        var card = TestCards.Monster(level: 5);
        var duel = CreateDuelAtMain1(card, ownMonsterCount: 1);
        var tribute = duel.FirstPlayer.MonsterZone[0];
        var countBefore = duel.Log.Count;

        duel.Summon(0, BattlePosition.FaceUpAttack, 0, [tribute]);

        DuelLogEntry[] expected =
        [
            new(1, DuelLogKind.Tributed, duel.FirstPlayer.Name, tribute.Card.Name),
            new(1, DuelLogKind.Summoned, duel.FirstPlayer.Name, card.Name),
        ];
        Assert.Equal(expected, duel.Log.Skip(countBefore));
    }

    // ---- 召喚できる可能性の判定(リリース選択前) ----

    [Theory]
    [InlineData(5, 0, ActionError.NotEnoughTributes)]
    [InlineData(5, 1, ActionError.None)]
    [InlineData(7, 1, ActionError.NotEnoughTributes)]
    [InlineData(7, 2, ActionError.None)]
    public void CanSummon_ChecksEnoughMonstersToTribute(int level, int ownMonsterCount, ActionError expected)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: level), ownMonsterCount);

        Assert.Equal(expected, duel.CanSummon(0, BattlePosition.FaceUpAttack).Error);
    }

    [Fact]
    public void CanSummon_Succeeds_WhenZoneFullButTributeFreesSlot()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: Player.MonsterZoneSize);

        Assert.True(duel.CanSummon(0, BattlePosition.FaceUpAttack).IsSuccess);
    }

    // ---- 失敗 ----

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Summon_Fails_WhenTributeCountWrong(int tributeCount)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 2);
        var tributes = duel.FirstPlayer.MonsterZone.Take(tributeCount).ToList();

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 4, tributes);

        Assert.Equal(ActionError.WrongTributeCount, result.Error);
        Assert.Equal(2, duel.FirstPlayer.MonsterZone.Count);
        Assert.False(duel.NormalSummonUsed);
    }

    [Fact]
    public void Summon_Fails_WhenTributingSameMonsterTwice()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 7), ownMonsterCount: 2);
        var monster = duel.FirstPlayer.MonsterZone[0];

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 4, [monster, monster]);

        Assert.Equal(ActionError.InvalidTribute, result.Error);
    }

    [Fact]
    public void Summon_Fails_WhenTributingOpponentMonster()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster(level: 5))
            .WithFieldMonster(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 4, [duel.SecondPlayer.MonsterZone[0]]);

        Assert.Equal(ActionError.InvalidTribute, result.Error);
        Assert.Single(duel.SecondPlayer.MonsterZone);
    }

    [Fact]
    public void Summon_Fails_WhenSlotOccupiedByNonTributeMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 5), ownMonsterCount: 2);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 1, [duel.FirstPlayer.MonsterSlots[0] ?? throw new InvalidOperationException()]);

        Assert.Equal(ActionError.SlotOccupied, result.Error);
        Assert.Equal(2, duel.FirstPlayer.MonsterZone.Count);
    }

    [Fact]
    public void Summon_Fails_WhenTributeGivenForLowLevelMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: 4), ownMonsterCount: 1);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 4, [duel.FirstPlayer.MonsterZone[0]]);

        Assert.Equal(ActionError.WrongTributeCount, result.Error);
    }

    [Fact]
    public void Summon_Fails_WhenNormalSummonAlreadyUsed()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster(level: 4), TestCards.Monster(level: 5))
            .Build();
        duel.AdvanceTo(1, Phase.Main1);
        duel.Summon(0, BattlePosition.FaceUpAttack);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 1, [duel.FirstPlayer.MonsterZone[0]]);

        Assert.Equal(ActionError.NormalSummonAlreadyUsed, result.Error);
        Assert.Single(duel.FirstPlayer.MonsterZone);
    }
}
