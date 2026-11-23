namespace CardGame.Core.Effects;

/// <summary>
/// 効果の定義(部品の組み合わせ)として成り立っているかを検証する。
/// カードデータの作成時・読み込み時に使う(対戦中の発動条件は <see cref="CheckEffectActivation"/>)。
/// </summary>
public static class ValidateCardEffect
{
    public static bool Execute(CardEffect effect)
    {
        if (effect.Actions.Count == 0) return false;
        if (!effect.Actions.All(action => IsValidAction(action, effect.Trigger))) return false;

        // 対象を使う部品があるときだけ、対象の側が決まっている必要がある(1枚で取れる対象は1体まで)
        var targetActionCount = effect.Actions.OfType<DestroyTargetMonsterAction>().Count();
        return targetActionCount switch
        {
            0 => effect.TargetSide is null,
            1 => effect.TargetSide is not null,
            _ => false,
        };
    }

    private static bool IsValidAction(EffectAction action, TrapTrigger? trigger) => action switch
    {
        DrawCardsAction draw => draw.Count > 0,
        DealDamageAction damage => damage.Amount > 0,
        GainLifePointsAction gain => gain.Amount > 0,
        DestroyTargetMonsterAction => true,
        DestroyAllMonstersAction => true,

        // 発動したきっかけ(攻撃・召喚)のモンスターを使う部品は、そのタイミングの罠でしか使えない
        DestroyAttackingMonsterAction => trigger == TrapTrigger.OpponentAttackDeclared,
        NegateAttackAction => trigger == TrapTrigger.OpponentAttackDeclared,
        DestroySummonedMonsterAction => trigger == TrapTrigger.OpponentMonsterSummoned,
        _ => false,
    };
}
