using CardGame.Core.Effects;

namespace CardGame.Core.Cards;

/// <summary>
/// カードの定義として成り立っているかを検証する。カードデータの読み込み時と、カード作成で使う。
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
            CardKind.NormalMonster => HasValidStats(card) && card.Effect is null,

            // 効果モンスターは、発動タイミング(リバース・召喚成功時)を持つ効果が必要
            CardKind.EffectMonster => HasValidStats(card)
                && card.Effect is { MonsterTrigger: not null, Trigger: null } effect
                && ValidateCardEffect.Execute(effect),

            CardKind.Spell => card.Effect is { Trigger: null, MonsterTrigger: null } effect && ValidateCardEffect.Execute(effect),
            CardKind.Trap => card.Effect is { Trigger: not null, MonsterTrigger: null } effect && ValidateCardEffect.Execute(effect),
            _ => false,
        };
    }

    private static bool HasValidStats(CardData card) =>
        card.Level is >= MinLevel and <= MaxLevel && card.Attack >= 0 && card.Defense >= 0;
}
