using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Core.Effects;

/// <summary>効果を処理し、盤面に反映する。発動できるかの判定は済んでいる前提。</summary>
public static class ResolveCardEffect
{
    /// <param name="controller">効果を使ったプレイヤー。</param>
    /// <param name="target">対象。対象を取らない効果では null。</param>
    /// <param name="triggerMonster">罠の発動のきっかけになったモンスター(攻撃したモンスター・召喚されたモンスター)。罠以外では null。</param>
    public static EffectOutcome Execute(
        CardEffect effect,
        Player controller,
        Player opponent,
        FieldMonster? target,
        DuelLog log,
        FieldMonster? triggerMonster = null)
    {
        var isAttackNegated = false;

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

                case DestroyTargetMonsterAction:
                    DestroyIfOnField(target, controller, opponent, log);
                    break;

                case DestroyAttackingMonsterAction:
                case DestroySummonedMonsterAction:
                    DestroyIfOnField(triggerMonster, controller, opponent, log);
                    break;

                case NegateAttackAction:
                    isAttackNegated = true;
                    break;

                case DestroyAllMonstersAction all:
                    foreach (var player in PlayersOn(all.Side, controller, opponent))
                    {
                        var monsters = player.MonsterZone
                            .Where(m => !all.AttackPositionOnly || m.Position == BattlePosition.FaceUpAttack);
                        foreach (var monster in monsters)
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

        return isAttackNegated ? new EffectOutcome(IsAttackNegated: true) : EffectOutcome.None;
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

    /// <summary>モンスターが場にいれば破壊する。すでに場を離れていれば何もしない。</summary>
    private static void DestroyIfOnField(FieldMonster? monster, Player controller, Player opponent, DuelLog log)
    {
        if (monster is null) return;

        var owner = controller.MonsterZone.Contains(monster) ? controller
            : opponent.MonsterZone.Contains(monster) ? opponent
            : null;
        if (owner is null) return;

        owner.DestroyMonster(monster);
        log.Add(DuelLogKind.Destroyed, owner, monster.Card.Name);
    }

    private static IEnumerable<Player> PlayersOn(MonsterSide side, Player controller, Player opponent) => side switch
    {
        MonsterSide.Own => [controller],
        MonsterSide.Opponent => [opponent],
        _ => [controller, opponent],
    };
}
