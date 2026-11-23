using CardGame.Core.Cards;
using CardGame.Core.Players;

namespace CardGame.Core.Battle;

/// <summary>戦闘を実行し、結果を盤面(ライフ・フィールド・墓地)に反映する。</summary>
public static class ResolveBattle
{
    /// <returns>反映した戦闘結果。</returns>
    public static BattleOutcome Execute(
        Player attackerOwner,
        Player targetOwner,
        FieldMonster attacker,
        FieldMonster? target)
    {
        if (target is { Position: BattlePosition.FaceDownDefense })
        {
            target.Position = BattlePosition.FaceUpDefense;
        }

        var outcome = CalculateBattleOutcome.Execute(attacker.Card.Attack, target);

        attackerOwner.TakeDamage(outcome.DamageToAttackerOwner);
        targetOwner.TakeDamage(outcome.DamageToTargetOwner);

        if (outcome.AttackerDestroyed)
        {
            attackerOwner.DestroyMonster(attacker);
        }

        if (outcome.TargetDestroyed && target is not null)
        {
            targetOwner.DestroyMonster(target);
        }

        return outcome;
    }
}
