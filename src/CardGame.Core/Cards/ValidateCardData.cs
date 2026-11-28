using CardGame.Core.Effects;

namespace CardGame.Core.Cards;

/// <summary>
/// カードの定義として成り立っているかを検証する。カードデータの読み込み時と、将来のカード作成機能で使う。
/// </summary>
public static class ValidateCardData
{
    public const int MinLevel = 1;
    public const int MaxLevel = 12;

    public static bool Execute(CardData card)
    {
        if (card.Id <= 0 || string.IsNullOrWhiteSpace(card.Name)) return false;

        return card.Kind switch
        {
            // 効果モンスターの効果はまだ実装していないため、効果を持たない通常モンスターだけを受け付ける
            CardKind.NormalMonster => card.Level is >= MinLevel and <= MaxLevel
                && card.Attack >= 0
                && card.Defense >= 0
                && card.Effect is null,

            CardKind.Spell => card.Effect is { Trigger: null } effect && ValidateCardEffect.Execute(effect),
            CardKind.Trap => card.Effect is { Trigger: not null } effect && ValidateCardEffect.Execute(effect),
            _ => false,
        };
    }
}
