using CardGame.Core.Cards;

namespace CardGame.Core.Battle;

/// <summary>戦闘結果の計算(純粋関数)。盤面は変更しない。</summary>
public static class CalculateBattleOutcome
{
    /// <param name="attackerAttack">攻撃モンスターの攻撃力。</param>
    /// <param name="target">攻撃対象。null は直接攻撃。伏せ守備の場合は表側にした後の状態を渡す。</param>
    public static BattleOutcome Execute(int attackerAttack, FieldMonster? target)
    {
        if (target is null)
        {
            return new BattleOutcome(false, false, 0, attackerAttack);
        }

        if (target.Position == BattlePosition.FaceUpAttack)
        {
            var diff = attackerAttack - target.Card.Attack;
            return diff switch
            {
                > 0 => new BattleOutcome(false, true, 0, diff),
                0 => new BattleOutcome(true, true, 0, 0),
                _ => new BattleOutcome(true, false, -diff, 0),
            };
        }

        // 守備表示: 攻撃力が守備力を上回れば破壊のみ。下回れば攻撃側の持ち主が差分のダメージ。
        var defenseDiff = attackerAttack - target.Card.Defense;
        return defenseDiff switch
        {
            > 0 => new BattleOutcome(false, true, 0, 0),
            0 => new BattleOutcome(false, false, 0, 0),
            _ => new BattleOutcome(false, false, -defenseDiff, 0),
        };
    }
}
