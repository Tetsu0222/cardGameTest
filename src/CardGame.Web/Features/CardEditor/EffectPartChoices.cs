using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Web.Features.CardEditor;

/// <summary>カード作成画面で、効果部品の名前と、選べる部品・数値の候補を決める。</summary>
public static class EffectPartChoices
{
    private static readonly EffectPartType[] CommonParts =
    [
        EffectPartType.DealDamage,
        EffectPartType.GainLifePoints,
        EffectPartType.DrawCards,
        EffectPartType.DestroyAllMonsters,
    ];

    public static string Label(EffectPartType type) => type switch
    {
        EffectPartType.DrawCards => "カードをドロー",
        EffectPartType.DealDamage => "相手にダメージ",
        EffectPartType.GainLifePoints => "ライフを回復",
        EffectPartType.DestroyTargetMonster => "モンスター1体を対象に破壊",
        EffectPartType.DestroyAllMonsters => "モンスターをすべて破壊",
        EffectPartType.DestroyAttackingMonster => "攻撃モンスターを破壊",
        EffectPartType.NegateAttack => "攻撃を無効にする",
        EffectPartType.DestroySummonedMonster => "召喚したモンスターを破壊",
        _ => type.ToString(),
    };

    public static string Label(MonsterSide side) => side switch
    {
        MonsterSide.Opponent => "相手",
        MonsterSide.Own => "自分",
        _ => "お互い",
    };

    public static string Label(TrapTrigger trigger) => trigger switch
    {
        TrapTrigger.OpponentAttackDeclared => "相手の攻撃宣言時",
        _ => "相手の召喚時",
    };

    /// <summary>
    /// カードの種類と発動タイミングで選べる部品。
    /// 罠は対象を選ぶ画面がまだないため、対象を取る部品は魔法だけで選べる。
    /// </summary>
    public static IReadOnlyList<EffectPartType> Available(CardKind kind, TrapTrigger trigger) => kind switch
    {
        CardKind.Spell => [.. CommonParts, EffectPartType.DestroyTargetMonster],
        CardKind.Trap when trigger == TrapTrigger.OpponentAttackDeclared =>
            [.. CommonParts, EffectPartType.DestroyAttackingMonster, EffectPartType.NegateAttack],
        CardKind.Trap => [.. CommonParts, EffectPartType.DestroySummonedMonster],
        _ => [],
    };

    /// <summary>数値(枚数・量)を持つ部品か。</summary>
    public static bool HasValue(EffectPartType type) =>
        type is EffectPartType.DrawCards or EffectPartType.DealDamage or EffectPartType.GainLifePoints;

    /// <summary>数値の候補。</summary>
    public static IReadOnlyList<int> Values(EffectPartType type, CardCreationOptions options) => type switch
    {
        EffectPartType.DrawCards => [.. Enumerable.Range(1, options.MaxDrawCount)],
        EffectPartType.DealDamage or EffectPartType.GainLifePoints =>
            Steps(options.LifeAmountStep, options.MaxLifeAmount, options.LifeAmountStep),
        _ => [],
    };

    /// <summary>新しく追加したときの数値(候補の最初)。</summary>
    public static int DefaultValue(EffectPartType type, CardCreationOptions options) =>
        Values(type, options) is [var first, ..] ? first : 0;

    public static string ValueUnit(EffectPartType type) => type == EffectPartType.DrawCards ? "枚" : "";

    /// <summary>min から max まで step 刻みの候補。</summary>
    public static IReadOnlyList<int> Steps(int min, int max, int step) =>
        step <= 0 ? [] : [.. Enumerable.Range(0, (max - min) / step + 1).Select(i => min + i * step)];
}
