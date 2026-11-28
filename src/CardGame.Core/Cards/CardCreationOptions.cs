namespace CardGame.Core.Cards;

/// <summary>
/// カード作成で選べる数値の範囲と刻み(appsettings.json の "CardCreation")。
/// 数値は自由入力ではなく、この範囲と刻みの中から選ぶ。依存のない素のクラス。
/// </summary>
public sealed class CardCreationOptions
{
    public int MaxNameLength { get; init; } = 20;

    public int MinLevel { get; init; } = 1;
    public int MaxLevel { get; init; } = 12;

    /// <summary>攻撃力・守備力の上限と刻み。</summary>
    public int MaxStat { get; init; } = 4000;
    public int StatStep { get; init; } = 100;

    /// <summary>ダメージ量・回復量の上限と刻み(下限は刻みと同じ値)。</summary>
    public int MaxLifeAmount { get; init; } = 2000;
    public int LifeAmountStep { get; init; } = 100;

    public int MaxDrawCount { get; init; } = 3;

    /// <summary>1枚のカードに入れられる効果部品の数。</summary>
    public int MaxEffectActions { get; init; } = 3;
}
