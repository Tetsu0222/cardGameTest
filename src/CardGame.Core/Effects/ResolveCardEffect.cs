using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Core.Effects;

/// <summary>効果を処理し、盤面に反映する。発動できるかの判定は済んでいる前提。</summary>
public static class ResolveCardEffect
{
    /// <param name="controller">効果を使ったプレイヤー。</param>
    /// <param name="target">対象。対象を取らない効果では null。</param>
    public static void Execute(
        CardEffect effect,
        Player controller,
        Player opponent,
        FieldMonster? target,
        DuelLog log)
    {
        foreach (var action in effect.Actions)
        {
            switch (action)
            {
                case DrawCardsAction draw:
                    DrawCards(controller, draw.Count, log);
                    break;

                case DealDamageAction damage:
                    opponent.TakeDamage(damage.Amount);
                    log.Add(DuelLogKind.Damaged, opponent, amount: damage.Amount);
                    break;

                case GainLifePointsAction gain:
                    controller.GainLifePoints(gain.Amount);
                    log.Add(DuelLogKind.LifeGained, controller, amount: gain.Amount);
                    break;

                case DestroyTargetMonsterAction when target is not null:
                    DestroyIfOnField(target, controller, opponent, log);
                    break;

                case DestroyTargetMonsterAction:
                    break;

                case DestroyAllMonstersAction all:
                    foreach (var player in PlayersOn(all.Side, controller, opponent))
                    {
                        foreach (var monster in player.MonsterZone)
                        {
                            player.DestroyMonster(monster);
                            log.Add(DuelLogKind.Destroyed, player, monster.Card.Name);
                        }
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown effect action: {action.GetType().Name}");
            }
        }
    }

    private static void DrawCards(Player player, int count, DuelLog log)
    {
        var drawn = 0;
        while (drawn < count && player.Draw())
        {
            drawn++;
        }

        if (drawn > 0)
        {
            log.Add(DuelLogKind.Drew, player, amount: drawn);
        }
    }

    private static void DestroyIfOnField(FieldMonster target, Player controller, Player opponent, DuelLog log)
    {
        var owner = controller.MonsterZone.Contains(target) ? controller
            : opponent.MonsterZone.Contains(target) ? opponent
            : null;
        if (owner is null) return;

        owner.DestroyMonster(target);
        log.Add(DuelLogKind.Destroyed, owner, target.Card.Name);
    }

    private static IEnumerable<Player> PlayersOn(MonsterSide side, Player controller, Player opponent) => side switch
    {
        MonsterSide.Own => [controller],
        MonsterSide.Opponent => [opponent],
        _ => [controller, opponent],
    };
}
