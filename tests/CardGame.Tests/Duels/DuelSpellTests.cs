using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Duels;

public class DuelSpellTests
{
    private const int InitialLifePoints = 8000;

    private static Duel CreateDuelAtMain1(params CardData[] hand)
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, hand).Build();
        duel.AdvanceTo(1, Phase.Main1);
        return duel;
    }

    // ---- 手札からの発動 ----

    [Fact]
    public void ActivateSpellFromHand_ResolvesEffectAndSendsToGraveyard()
    {
        var spell = TestCards.Spell(new DealDamageAction(600));
        var duel = CreateDuelAtMain1(spell);

        var result = duel.ActivateSpellFromHand(0, target: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints - 600, duel.SecondPlayer.LifePoints);
        Assert.Empty(duel.FirstPlayer.Hand);
        Assert.Equal(spell, Assert.Single(duel.FirstPlayer.Graveyard));
        Assert.Empty(duel.FirstPlayer.SpellTrapZone);
    }

    [Fact]
    public void ActivateSpellFromHand_DoesNotUseNormalSummon()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)), TestCards.Monster());
        duel.ActivateSpellFromHand(0, target: null);

        Assert.True(duel.Summon(0, BattlePosition.FaceUpAttack).IsSuccess);
    }

    [Fact]
    public void ActivateSpellFromHand_CanActivateMultipleSpellsInOneTurn()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)), TestCards.Spell(new DealDamageAction(100)));

        duel.ActivateSpellFromHand(0, target: null);
        var second = duel.ActivateSpellFromHand(0, target: null);

        Assert.True(second.IsSuccess);
        Assert.Equal(InitialLifePoints - 200, duel.SecondPlayer.LifePoints);
    }

    [Fact]
    public void ActivateSpellFromHand_LogsActivationBeforeEffect()
    {
        var spell = TestCards.Spell(new DealDamageAction(600));
        var duel = CreateDuelAtMain1(spell);
        var countBefore = duel.Log.Count;

        duel.ActivateSpellFromHand(0, target: null);

        DuelLogEntry[] expected =
        [
            new(1, DuelLogKind.SpellActivated, duel.FirstPlayer.Name, spell.Name),
            new(1, DuelLogKind.Damaged, duel.SecondPlayer.Name, Amount: 600),
        ];
        Assert.Equal(expected, duel.Log.Skip(countBefore));
    }

    [Fact]
    public void ActivateSpellFromHand_EndsDuel_WhenDamageReducesLifeToZero()
    {
        var duel = DuelBuilder.Create()
            .WithRules(new GameRulesOptions { InitialHandSize = 0, InitialLifePoints = 500 })
            .WithHand(Side.First, TestCards.Spell(new DealDamageAction(500)))
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.ActivateSpellFromHand(0, target: null);

        Assert.Same(duel.FirstPlayer, duel.Winner);
        Assert.Equal(DuelLogKind.Won, duel.Log[^1].Kind);
    }

    [Fact]
    public void ActivateSpellFromHand_DestroysChosenTarget()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.DestroyTargetSpell())
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 1000))
            .WithFieldMonster(Side.Second, TestCards.Monster(attack: 2000))
            .Build();
        duel.AdvanceTo(1, Phase.Main1);
        var target = duel.SecondPlayer.MonsterSlots[1];

        var result = duel.ActivateSpellFromHand(0, target);

        Assert.True(result.IsSuccess);
        Assert.Null(duel.SecondPlayer.MonsterSlots[1]);
        Assert.NotNull(duel.SecondPlayer.MonsterSlots[0]);
    }

    [Fact]
    public void ActivateSpellFromHand_HidesTargetName_WhenTargetFaceDown()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.DestroyTargetSpell())
            .WithFieldMonster(Side.Second, TestCards.Monster(), BattlePosition.FaceDownDefense)
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.ActivateSpellFromHand(0, duel.SecondPlayer.MonsterZone[0]);

        var activation = duel.Log.Single(e => e.Kind == DuelLogKind.SpellActivated);
        Assert.Null(activation.TargetCardName);
        Assert.True(activation.IsTargetHidden);
    }

    [Fact]
    public void ActivateSpellFromHand_LogsNoTarget_WhenSpellHasNoTarget()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)));

        duel.ActivateSpellFromHand(0, target: null);

        var activation = duel.Log.Single(e => e.Kind == DuelLogKind.SpellActivated);
        Assert.Null(activation.TargetCardName);
        Assert.False(activation.IsTargetHidden);
    }

    [Theory]
    [InlineData(Phase.Draw)]
    [InlineData(Phase.Standby)]
    [InlineData(Phase.Battle)]
    [InlineData(Phase.End)]
    public void ActivateSpellFromHand_Fails_WhenNotMainPhase(Phase phase)
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Spell(new DealDamageAction(100))).Build();
        duel.AdvanceTo(1, phase);

        var result = duel.ActivateSpellFromHand(0, target: null);

        Assert.Equal(ActionError.WrongPhase, result.Error);
        Assert.Single(duel.FirstPlayer.Hand);
    }

    [Fact]
    public void ActivateSpellFromHand_Fails_WhenCardIsMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        Assert.Equal(ActionError.NotASpell, duel.ActivateSpellFromHand(0, target: null).Error);
    }

    [Fact]
    public void ActivateSpellFromHand_Fails_WhenSpellHasNoEffect()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell());

        Assert.Equal(ActionError.ActivationConditionNotMet, duel.ActivateSpellFromHand(0, target: null).Error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ActivateSpellFromHand_Fails_WhenHandIndexOutOfRange(int handIndex)
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)));

        Assert.Equal(ActionError.InvalidHandIndex, duel.ActivateSpellFromHand(handIndex, target: null).Error);
    }

    [Fact]
    public void ActivateSpellFromHand_Fails_WhenSpellTrapZoneFull()
    {
        var builder = DuelBuilder.Create().WithHand(Side.First, TestCards.Spell(new DealDamageAction(100)));
        for (var i = 0; i < Player.SpellTrapZoneSize; i++)
        {
            builder.WithSetSpellTrap(Side.First, TestCards.Spell());
        }
        var duel = builder.Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.ActivateSpellFromHand(0, target: null);

        Assert.Equal(ActionError.SpellTrapZoneFull, result.Error);
        Assert.Equal(InitialLifePoints, duel.SecondPlayer.LifePoints);
    }

    [Fact]
    public void ActivateSpellFromHand_Fails_WhenDeckTooSmallToDraw()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Spell(new DrawCardsAction(2)))
            .WithDeckSize(Side.First, 1)
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.ActivationConditionNotMet, duel.ActivateSpellFromHand(0, target: null).Error);
    }

    [Fact]
    public void ActivateSpellFromHand_Fails_WhenTargetMissing()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.DestroyTargetSpell())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.ActivateSpellFromHand(0, target: null);

        Assert.Equal(ActionError.TargetRequired, result.Error);
        Assert.Single(duel.SecondPlayer.MonsterZone);
    }

    // ---- 対象をまだ決めていない状態の判定 ----

    [Fact]
    public void CanActivateSpellFromHand_ReturnsNoValidTarget_WhenNoTargetExists()
    {
        var duel = CreateDuelAtMain1(TestCards.DestroyTargetSpell());

        Assert.Equal(ActionError.NoValidTarget, duel.CanActivateSpellFromHand(0).Error);
    }

    [Fact]
    public void CanActivateSpellFromHand_Succeeds_WhenTargetExists()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.DestroyTargetSpell())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.True(duel.CanActivateSpellFromHand(0).IsSuccess);
        Assert.Single(duel.SecondPlayer.MonsterZone);
    }

    [Fact]
    public void FindSpellTargets_ReturnsMonstersOnTargetSide()
    {
        var spell = TestCards.DestroyTargetSpell(MonsterSide.Opponent);
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, spell)
            .WithFieldMonster(Side.First, TestCards.Monster())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();

        var targets = duel.FindSpellTargets(spell);

        Assert.Same(duel.SecondPlayer.MonsterZone[0], Assert.Single(targets));
    }

    // ---- セット ----

    [Fact]
    public void SetSpellTrap_PlacesFaceDownInChosenSlot()
    {
        var spell = TestCards.Spell(new DealDamageAction(100));
        var duel = CreateDuelAtMain1(spell);

        var result = duel.SetSpellTrap(0, 3);

        Assert.True(result.IsSuccess);
        var setCard = duel.FirstPlayer.SpellTrapSlots[3];
        Assert.NotNull(setCard);
        Assert.Equal(spell, setCard.Card);
        Assert.False(setCard.IsFaceUp);
        Assert.Empty(duel.FirstPlayer.Hand);
    }

    [Fact]
    public void SetSpellTrap_LogsWithoutCardName()
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)));

        duel.SetSpellTrap(0, 0);

        Assert.Equal(new DuelLogEntry(1, DuelLogKind.SpellTrapSet, duel.FirstPlayer.Name), duel.Log[^1]);
    }

    [Fact]
    public void SetSpellTrap_UsesLeftmostEmptySlot_WhenSlotNotSpecified()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Spell(new DealDamageAction(100)))
            .WithSetSpellTrap(Side.First, TestCards.Spell())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        duel.SetSpellTrap(0);

        Assert.NotNull(duel.FirstPlayer.SpellTrapSlots[1]);
    }

    [Fact]
    public void SetSpellTrap_AcceptsTrapCards()
    {
        var duel = CreateDuelAtMain1(TestCards.Trap());

        Assert.True(duel.SetSpellTrap(0, 0).IsSuccess);
    }

    [Fact]
    public void SetSpellTrap_Fails_WhenCardIsMonster()
    {
        var duel = CreateDuelAtMain1(TestCards.Monster());

        Assert.Equal(ActionError.NotASpellOrTrap, duel.SetSpellTrap(0, 0).Error);
    }

    [Fact]
    public void SetSpellTrap_Fails_WhenSlotOccupied()
    {
        var duel = DuelBuilder.Create()
            .WithHand(Side.First, TestCards.Spell(new DealDamageAction(100)))
            .WithSetSpellTrap(Side.First, TestCards.Spell())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.SlotOccupied, duel.SetSpellTrap(0, 0).Error);
        Assert.Single(duel.FirstPlayer.Hand);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Player.SpellTrapZoneSize)]
    public void SetSpellTrap_Fails_WhenSlotOutOfRange(int slotIndex)
    {
        var duel = CreateDuelAtMain1(TestCards.Spell(new DealDamageAction(100)));

        Assert.Equal(ActionError.InvalidSlot, duel.SetSpellTrap(0, slotIndex).Error);
    }

    [Fact]
    public void SetSpellTrap_Fails_WhenNotMainPhase()
    {
        var duel = DuelBuilder.Create().WithHand(Side.First, TestCards.Spell(new DealDamageAction(100))).Build();

        Assert.Equal(ActionError.WrongPhase, duel.SetSpellTrap(0, 0).Error);
    }

    // ---- セットした魔法の発動 ----

    [Fact]
    public void ActivateSetSpell_ResolvesEffectAndFreesSlot()
    {
        var spell = TestCards.Spell(new GainLifePointsAction(1000));
        var duel = CreateDuelAtMain1(spell);
        duel.SetSpellTrap(0, 2);
        var setCard = duel.FirstPlayer.SpellTrapZone[0];

        var result = duel.ActivateSetSpell(setCard, target: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints + 1000, duel.FirstPlayer.LifePoints);
        Assert.Null(duel.FirstPlayer.SpellTrapSlots[2]);
        Assert.Equal(spell, Assert.Single(duel.FirstPlayer.Graveyard));
    }

    [Fact]
    public void ActivateSetSpell_CanBeActivatedOnLaterTurn()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.Spell(new DealDamageAction(500)))
            .Build();
        duel.AdvanceTo(3, Phase.Main2);

        var result = duel.ActivateSetSpell(duel.FirstPlayer.SpellTrapZone[0], target: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(InitialLifePoints - 500, duel.SecondPlayer.LifePoints);
    }

    [Fact]
    public void ActivateSetSpell_DestroysChosenTarget()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.First, TestCards.DestroyTargetSpell())
            .WithFieldMonster(Side.Second, TestCards.Monster())
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.ActivateSetSpell(duel.FirstPlayer.SpellTrapZone[0], duel.SecondPlayer.MonsterZone[0]);

        Assert.True(result.IsSuccess);
        Assert.Empty(duel.SecondPlayer.MonsterZone);
    }

    [Fact]
    public void ActivateSetSpell_Fails_WhenCardIsOpponents()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.Second, TestCards.Spell(new DealDamageAction(500)))
            .Build();
        duel.AdvanceTo(1, Phase.Main1);

        var result = duel.ActivateSetSpell(duel.SecondPlayer.SpellTrapZone[0], target: null);

        Assert.Equal(ActionError.CardNotOnYourField, result.Error);
    }

    [Fact]
    public void ActivateSetSpell_Fails_WhenCardIsTrap()
    {
        var duel = DuelBuilder.Create().WithSetSpellTrap(Side.First, TestCards.Trap()).Build();
        duel.AdvanceTo(1, Phase.Main1);

        Assert.Equal(ActionError.NotASpell, duel.ActivateSetSpell(duel.FirstPlayer.SpellTrapZone[0], target: null).Error);
    }

    [Fact]
    public void ActivateSetSpell_Fails_WhenNotMainPhase()
    {
        var duel = DuelBuilder.Create()
            .WithSetSpellTrap(Side.Second, TestCards.Spell(new DealDamageAction(500)))
            .Build();
        duel.AdvanceTo(2, Phase.Battle);

        var result = duel.ActivateSetSpell(duel.SecondPlayer.SpellTrapZone[0], target: null);

        Assert.Equal(ActionError.WrongPhase, result.Error);
        Assert.Single(duel.SecondPlayer.SpellTrapZone);
    }
}
