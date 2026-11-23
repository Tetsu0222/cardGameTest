using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Tests.TestSupport;

namespace CardGame.Tests.Effects;

public class ResolveCardEffectTests
{
    private const int InitialLifePoints = 8000;

    private readonly Player _controller = CreatePlayer("Controller");
    private readonly Player _opponent = CreatePlayer("Opponent");
    private readonly DuelLog _log = new(() => 1);

    private static Player CreatePlayer(string name) =>
        new(name, Enumerable.Repeat(TestCards.Filler(), 10), new GameRulesOptions());

    private static FieldMonster PlaceMonster(Player player) =>
        player.PlaceMonster(TestCards.Monster(), BattlePosition.FaceUpAttack)
        ?? throw new InvalidOperationException("No empty monster slot.");

    private void Resolve(CardEffect effect, FieldMonster? target = null) =>
        ResolveCardEffect.Execute(effect, _controller, _opponent, target, _log);

    [Fact]
    public void Execute_DrawsCardsForController()
    {
        Resolve(new CardEffect([new DrawCardsAction(2)]));

        Assert.Equal(2, _controller.Hand.Count);
        Assert.Empty(_opponent.Hand);
        Assert.Equal(new DuelLogEntry(1, DuelLogKind.Drew, "Controller", Amount: 2), _log.Entries[^1]);
    }

    [Fact]
    public void Execute_DamagesOpponent()
    {
        Resolve(new CardEffect([new DealDamageAction(600)]));

        Assert.Equal(InitialLifePoints - 600, _opponent.LifePoints);
        Assert.Equal(InitialLifePoints, _controller.LifePoints);
    }

    [Fact]
    public void Execute_HealsController_AboveInitialLifePoints()
    {
        Resolve(new CardEffect([new GainLifePointsAction(1000)]));

        Assert.Equal(InitialLifePoints + 1000, _controller.LifePoints);
    }

    [Fact]
    public void Execute_RunsActionsInOrder()
    {
        Resolve(new CardEffect([new DealDamageAction(300), new DrawCardsAction(1)]));

        Assert.Equal([DuelLogKind.Damaged, DuelLogKind.Drew], _log.Entries.Select(e => e.Kind));
    }

    [Fact]
    public void Execute_DestroysTargetOnly()
    {
        var target = PlaceMonster(_opponent);
        var other = PlaceMonster(_opponent);

        Resolve(new CardEffect([new DestroyTargetMonsterAction()], MonsterSide.Opponent), target);

        Assert.Same(other, Assert.Single(_opponent.MonsterZone));
        Assert.Single(_opponent.Graveyard);
    }

    [Fact]
    public void Execute_DoesNothing_WhenTargetAlreadyLeftField()
    {
        var target = PlaceMonster(_opponent);
        var effect = new CardEffect(
            [new DestroyAllMonstersAction(MonsterSide.Opponent), new DestroyTargetMonsterAction()],
            MonsterSide.Opponent);

        Resolve(effect, target);

        // 全体破壊で墓地へ送られた後なので、対象の破壊では何も起きない(二重に墓地へ送らない)
        Assert.Single(_opponent.Graveyard);
    }

    [Theory]
    [InlineData(MonsterSide.Own, 0, 2)]
    [InlineData(MonsterSide.Opponent, 2, 0)]
    [InlineData(MonsterSide.Both, 0, 0)]
    public void Execute_DestroysAllMonstersOnSide(MonsterSide side, int ownRemaining, int opponentRemaining)
    {
        PlaceMonster(_controller);
        PlaceMonster(_controller);
        PlaceMonster(_opponent);
        PlaceMonster(_opponent);

        Resolve(new CardEffect([new DestroyAllMonstersAction(side)]));

        Assert.Equal(ownRemaining, _controller.MonsterZone.Count);
        Assert.Equal(opponentRemaining, _opponent.MonsterZone.Count);
    }
}
