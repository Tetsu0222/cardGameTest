using CardGame.Core.Effects;

namespace CardGame.Web.Shared;

/// <summary>カードの効果を画面表示用の説明文に変換する。</summary>
public static class CardEffectText
{
    public static string Describe(CardEffect effect) =>
        MonsterTriggerText(effect.MonsterTrigger)
        + TriggerText(effect.Trigger)
        + string.Concat(effect.Actions.Select(action => DescribeAction(action, effect.TargetSide) + "。"));

    private static string MonsterTriggerText(MonsterEffectTrigger? trigger) => trigger switch
    {
        MonsterEffectTrigger.Flipped => "リバース:",
        MonsterEffectTrigger.Summoned => "このカードの召喚に成功した時:",
        _ => "",
    };

    private static string TriggerText(TrapTrigger? trigger) => trigger switch
    {
        TrapTrigger.OpponentAttackDeclared => "相手モンスターの攻撃宣言時に発動できる。",
        TrapTrigger.OpponentMonsterSummoned => "相手がモンスターを召喚した時に発動できる。",
        _ => "",
    };

    private static string DescribeAction(EffectAction action, MonsterSide? targetSide) => action switch
    {
        DrawCardsAction draw => $"カードを{draw.Count}枚ドローする",
        DealDamageAction damage => $"相手に{damage.Amount}ダメージを与える",
        GainLifePointsAction gain => $"自分は{gain.Amount}ライフポイント回復する",
        DestroyTargetMonsterAction => $"{SideText(targetSide)}モンスター1体を対象として破壊する",
        DestroyAllMonstersAction { AttackPositionOnly: true } all => $"{SideText(all.Side)}攻撃表示モンスターをすべて破壊する",
        DestroyAllMonstersAction all => $"{SideText(all.Side)}モンスターをすべて破壊する",
        DestroyAttackingMonsterAction => "攻撃モンスターを破壊する",
        NegateAttackAction => "その攻撃を無効にする",
        DestroySummonedMonsterAction => "召喚したモンスターを破壊する",
        _ => action.GetType().Name,
    };

    private static string SideText(MonsterSide? side) => side switch
    {
        MonsterSide.Opponent => "相手フィールド上の",
        MonsterSide.Own => "自分フィールド上の",
        _ => "フィールド上の",
    };
}
