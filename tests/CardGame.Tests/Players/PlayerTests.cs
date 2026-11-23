using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Players;

public class PlayerTests
{
    private static Player CreatePlayer(int deckSize, GameRulesOptions? rules = null)
    {
        var deck = Enumerable.Range(1, deckSize)
            .Select(i => new CardData(i, $"Card{i}", CardKind.NormalMonster, 4, 1000, 1000));
        return new Player("P1", deck, rules ?? new GameRulesOptions());
    }

    /// <summary>空きマスがある前提でモンスターを置く。</summary>
    private static FieldMonster Place(Player player) =>
        player.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack)
        ?? throw new InvalidOperationException("No empty monster slot.");

    [Fact]
    public void Constructor_SetsLifePoints_FromRules()
    {
        var p = CreatePlayer(0, new GameRulesOptions { InitialLifePoints = 4000 });
        Assert.Equal(4000, p.LifePoints);
    }

    [Fact]
    public void Draw_MovesTopCardToHand()
    {
        var p = CreatePlayer(3);
        Assert.True(p.Draw());
        Assert.Single(p.Hand);
        Assert.Equal(2, p.Deck.Count);
    }

    [Fact]
    public void Draw_ReturnsFalse_WhenDeckEmpty()
    {
        var p = CreatePlayer(0);
        Assert.False(p.Draw());
    }

    [Fact]
    public void TakeDamage_NeverGoesBelowZero_WhenDamageExceedsLifePoints()
    {
        var p = CreatePlayer(0);
        p.TakeDamage(p.LifePoints + 1000);
        Assert.Equal(0, p.LifePoints);
    }

    [Fact]
    public void PlaceMonster_UsesLeftmostEmptySlot()
    {
        var p = CreatePlayer(0);

        var first = Place(p);
        var second = Place(p);

        Assert.Same(first, p.MonsterSlots[0]);
        Assert.Same(second, p.MonsterSlots[1]);
        Assert.All(p.MonsterSlots.Skip(2), Assert.Null);
    }

    [Fact]
    public void DestroyMonster_KeepsOtherMonstersInTheirSlots()
    {
        var p = CreatePlayer(0);
        var left = Place(p);
        var middle = Place(p);
        var right = Place(p);

        Assert.True(p.DestroyMonster(middle));

        Assert.Same(left, p.MonsterSlots[0]);
        Assert.Null(p.MonsterSlots[1]);
        Assert.Same(right, p.MonsterSlots[2]);
        Assert.Single(p.Graveyard);
    }

    [Fact]
    public void PlaceMonster_FillsVacatedSlot_WhenItIsLeftmostEmpty()
    {
        var p = CreatePlayer(0);
        Place(p);
        var middle = Place(p);
        var right = Place(p);
        p.DestroyMonster(middle);

        var added = Place(p);

        Assert.Same(added, p.MonsterSlots[1]);
        Assert.Same(right, p.MonsterSlots[2]);
    }

    [Fact]
    public void PlaceMonster_ReturnsNull_WhenAllSlotsFull()
    {
        var p = CreatePlayer(0);
        for (var i = 0; i < Player.MonsterZoneSize; i++)
        {
            Place(p);
        }

        Assert.Null(p.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack));
        Assert.Equal(Player.MonsterZoneSize, p.MonsterZone.Count);
    }

    [Fact]
    public void PlaceMonster_PlacesInGivenSlot_WhenSlotSpecified()
    {
        var p = CreatePlayer(0);

        var monster = p.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack, 3);

        Assert.Same(monster, p.MonsterSlots[3]);
        Assert.Single(p.MonsterZone);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Player.MonsterZoneSize)]
    public void PlaceMonster_ReturnsNull_WhenSlotOutOfRange(int slotIndex)
    {
        var p = CreatePlayer(0);

        Assert.Null(p.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack, slotIndex));
        Assert.Empty(p.MonsterZone);
    }

    [Fact]
    public void PlaceMonster_ReturnsNull_WhenSlotOccupied()
    {
        var p = CreatePlayer(0);
        var existing = Place(p);

        Assert.Null(p.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack, 0));
        Assert.Same(existing, p.MonsterSlots[0]);
    }

    [Fact]
    public void SummonFromHand_KeepsCardInHand_WhenSlotOccupied()
    {
        var p = CreatePlayer(1);
        p.Draw();
        Place(p);

        Assert.Null(p.SummonFromHand(0, BattlePosition.FaceUpAttack, 0));
        Assert.Single(p.Hand);
    }

    [Fact]
    public void DestroyMonster_ReturnsFalse_WhenMonsterNotOnField()
    {
        var p = CreatePlayer(0);
        var stranger = new FieldMonster(TestCards.Monster(), BattlePosition.FaceUpAttack);

        Assert.False(p.DestroyMonster(stranger));
        Assert.Empty(p.Graveyard);
    }
}
