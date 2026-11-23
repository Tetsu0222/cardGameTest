using CardGame.Core.Effects;

namespace CardGame.Web.Features.Duels;

/// <summary>カードの効果を画面表示用の説明文に変換する。</summary>
public static class CardEffectText
{
    public static string Describe(CardEffect effect) =>
        string.Concat(effect.Actions.Select(action => DescribeAction(action, effect.TargetSide) + "。"));

    private static string DescribeAction(EffectAction action, MonsterSide? targetSide) => action switch
    {
        DrawCardsAction draw => $"カードを{draw.Count}枚ドローする",
        DealDamageAction damage => $"相手に{damage.Amount}ダメージを与える",
        GainLifePointsAction gain => $"自分は{gain.Amount}ライフポイント回復する",
        DestroyTargetMonsterAction => $"{SideText(targetSide)}モンスター1体を対象として破壊する",
        DestroyAllMonstersAction all => $"{SideText(all.Side)}モンスターをすべて破壊する",
        _ => action.GetType().Name,
    };

    private static string SideText(MonsterSide? side) => side switch
    {
        MonsterSide.Opponent => "相手フィールド上の",
        MonsterSide.Own => "自分フィールド上の",
        _ => "フィールド上の",
    };
}
