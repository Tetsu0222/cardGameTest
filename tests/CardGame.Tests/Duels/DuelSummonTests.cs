using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

public class DuelSummonTests
{
    private static Duel CreateDuelAtMain1(params CardData[] hand)
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, hand)
            .Build();
        duel.AdvanceTo(1, Phase.Main1);
        return duel;
    }

    [Fact]
    public void Summon_MovesCardFromHandToField_InAttackPosition()
    {
        var card = TestCards.Monster(attack: 1800);
        var duel = CreateDuelAtMain1(card);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.True(result.IsSuccess);
        Assert.Empty(duel.FirstPlayer.Hand);
        var monster = Assert.Single(duel.FirstPlayer.MonsterZone);
        Assert.Equal(card, monster.Card);
        Assert.Equal(BattlePosition.FaceUpAttack, monster.Position);
        Assert.True(duel.NormalSummonUsed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public void Summon_PlacesMonsterInChosenSlot(int slotIndex)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, slotIndex);

        Assert.True(result.IsSuccess);
        Assert.NotNull(duel.FirstPlayer.MonsterSlots[slotIndex]);
        Assert.Single(duel.FirstPlayer.MonsterZone);
    }

    [Fact]
    public void Summon_UsesLeftmostEmptySlot_WhenSlotNotSpecified()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.NotNull(duel.FirstPlayer.MonsterSlots[1]);
    }

    [Fact]
    public void Summon_Fails_WhenChosenSlotOccupied()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, 0);

        Assert.Equal(ActionError.SlotOccupied, result.Error);
        Assert.Single(duel.FirstPlayer.Hand);
        Assert.False(duel.NormalSummonUsed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Player.MonsterZoneSize)]
    public void Summon_Fails_WhenChosenSlotOutOfRange(int slotIndex)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        var result = duel.Summon(0, BattlePosition.FaceUpAttack, slotIndex);

        Assert.Equal(ActionError.InvalidSlot, result.Error);
        Assert.False(duel.NormalSummonUsed);
    }

    [Fact]
    public void Summon_ReportsRuleErrorBeforeSlotError_WhenBothInvalid()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster()).Build();

        // ドローフェーズかつマスも範囲外。フェーズの誤りを優先して返す
        var result = duel.Summon(0, BattlePosition.FaceUpAttack, -1);

        Assert.Equal(ActionError.WrongPhase, result.Error);
    }

    [Fact]
    public void Summon_PlacesFaceDown_WhenSet()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        var result = duel.Summon(0, BattlePosition.FaceDownDefense);

        Assert.True(result.IsSuccess);
        Assert.Equal(BattlePosition.FaceDownDefense, duel.FirstPlayer.MonsterZone[0].Position);
        Assert.True(duel.NormalSummonUsed);
    }

    [Fact]
    public void Summon_Succeeds_InMain2()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main2);

        Assert.True(duel.Summon(0, BattlePosition.FaceUpAttack).IsSuccess);
    }

    [Theory]
    [InlineData(Phase.Draw)]
    [InlineData(Phase.Standby)]
    [InlineData(Phase.Battle)]
    [InlineData(Phase.End)]
    public void Summon_Fails_WhenNotMainPhase(Phase phase)
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, phase);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.WrongPhase, result.Error);
        Assert.Empty(duel.FirstPlayer.MonsterZone);
    }

    [Fact]
    public void Summon_Fails_WhenNormalSummonAlreadyUsed()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(), TestCards.Monster());
        duel.Summon(0, BattlePosition.FaceUpAttack);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.NormalSummonAlreadyUsed, result.Error);
        Assert.Single(duel.FirstPlayer.MonsterZone);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    public void Summon_Fails_WhenNoMonsterToTribute(int level)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster(level: level));

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.NotEnoughTributes, result.Error);
    }

    [Fact]
    public void Summon_Fails_WhenCardIsNotMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell());

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.NotAMonster, result.Error);
    }

    [Fact]
    public void Summon_Fails_WhenFaceUpDefense()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        var result = duel.Summon(0, BattlePosition.FaceUpDefense);

        Assert.Equal(ActionError.InvalidPosition, result.Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Summon_Fails_WhenHandIndexOutOfRange(int handIndex)
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        var result = duel.Summon(handIndex, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.InvalidHandIndex, result.Error);
    }

    [Fact]
    public void Summon_Fails_WhenMonsterZoneFull()
    {
        var builder = DuelBuilder.Create().WithHand(Side.First, TestCards.Monster());
        for (var i = 0; i < Player.MonsterZoneSize; i++)
        {
            builder.WithFieldMonster(Side.First, TestCards.Monster());
        }
        var duel = builder.Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.Summon(0, BattlePosition.FaceUpAttack);

        Assert.Equal(ActionError.MonsterZoneFull, result.Error);
        Assert.Single(duel.FirstPlayer.Hand);
    }
}
