using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Effects;

public class CheckEffectActivationTests
{
    private static readonly CardEffect DestroyOpponentMonster = new([new DestroyTargetMonsterAction()], MonsterSide.Opponent);

    private static Player CreatePlayer(int deckSize = 10) =>
        new("P", Enumerable.Repeat(TestCards.Filler(), deckSize), new GameRulesOptions());

    private static FieldMonster PlaceMonster(Player player) =>
        player.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack)
        ?? throw new InvalidOperationException("No empty monster slot.");

    [Fact]
    public void Execute_ReturnsNone_WhenNoTargetNeeded()
    {
        var effect = new CardEffect([new DealDamageAction(500)]);

        Assert.Equal(ActionError.None, CheckEffectActivation.Execute(effect, CreatePlayer(), CreatePlayer(), null));
    }

    [Theory]
    [InlineData(2, ActionError.None)]
    [InlineData(1, ActionError.ActivationConditionNotMet)]
    [InlineData(0, ActionError.ActivationConditionNotMet)]
    public void Execute_RequiresEnoughCardsInDeck_WhenDrawing(int deckSize, ActionError expected)
    {
        var effect = new CardEffect([new DrawCardsAction(2)]);

        Assert.Equal(expected, CheckEffectActivation.Execute(effect, CreatePlayer(deckSize), CreatePlayer(), null));
    }

    [Fact]
    public void Execute_SumsDrawCounts_WhenMultipleDrawActions()
    {
        var effect = new CardEffect([new DrawCardsAction(1), new DrawCardsAction(2)]);

        Assert.Equal(
            ActionError.ActivationConditionNotMet,
            CheckEffectActivation.Execute(effect, CreatePlayer(deckSize: 2), CreatePlayer(), null));
    }

    [Fact]
    public void Execute_ReturnsActivationConditionNotMet_WhenEffectDefinitionInvalid()
    {
        var invalid = new CardEffect([new DestroyTargetMonsterAction()]); // 対象の側が未指定

        Assert.Equal(
            ActionError.ActivationConditionNotMet,
            CheckEffectActivation.Execute(invalid, CreatePlayer(), CreatePlayer(), null));
    }

    [Fact]
    public void Execute_ReturnsTargetNotAllowed_WhenTargetGivenToNonTargetEffect()
    {
        var opponent = CreatePlayer();
        var monster = PlaceMonster(opponent);
        var effect = new CardEffect([new DealDamageAction(500)]);

        Assert.Equal(ActionError.TargetNotAllowed, CheckEffectActivation.Execute(effect, CreatePlayer(), opponent, monster));
    }

    [Fact]
    public void Execute_ReturnsTargetRequired_WhenTargetMissing()
    {
        var opponent = CreatePlayer();
        PlaceMonster(opponent);

        Assert.Equal(
            ActionError.TargetRequired,
            CheckEffectActivation.Execute(DestroyOpponentMonster, CreatePlayer(), opponent, null));
    }

    [Fact]
    public void Execute_ReturnsInvalidTarget_WhenTargetOnWrongSide()
    {
        var controller = CreatePlayer();
        var ownMonster = PlaceMonster(controller);

        Assert.Equal(
            ActionError.InvalidTarget,
            CheckEffectActivation.Execute(DestroyOpponentMonster, controller, CreatePlayer(), ownMonster));
    }

    [Theory]
    [InlineData(MonsterSide.Own, true, false)]
    [InlineData(MonsterSide.Opponent, false, true)]
    [InlineData(MonsterSide.Both, true, true)]
    public void Execute_AllowsTargetsOnConfiguredSide(MonsterSide side, bool ownAllowed, bool opponentAllowed)
    {
        var controller = CreatePlayer();
        var opponent = CreatePlayer();
        var ownMonster = PlaceMonster(controller);
        var opponentMonster = PlaceMonster(opponent);
        var effect = new CardEffect([new DestroyTargetMonsterAction()], side);

        Assert.Equal(ownAllowed, CheckEffectActivation.Execute(effect, controller, opponent, ownMonster) == ActionError.None);
        Assert.Equal(opponentAllowed, CheckEffectActivation.Execute(effect, controller, opponent, opponentMonster) == ActionError.None);
    }

    [Fact]
    public void ExecuteBeforeTargeting_ReturnsNoValidTarget_WhenNoMonsterOnTargetSide()
    {
        var controller = CreatePlayer();
        PlaceMonster(controller); // 自分の場にはいるが、対象は相手側のみ

        Assert.Equal(
            ActionError.NoValidTarget,
            CheckEffectActivation.ExecuteBeforeTargeting(DestroyOpponentMonster, controller, CreatePlayer()));
    }

    [Fact]
    public void ExecuteBeforeTargeting_ReturnsNone_WhenTargetExists()
    {
        var opponent = CreatePlayer();
        PlaceMonster(opponent);

        Assert.Equal(
            ActionError.None,
            CheckEffectActivation.ExecuteBeforeTargeting(DestroyOpponentMonster, CreatePlayer(), opponent));
    }
}
