using CardGame.Core.Effects;

namespace CardGame.Core.Cards;

/// <summary>
/// プレイヤーが作ったカードを検証する。カードの定義として成り立っているか(<see cref="ValidateCardData"/>)に加え、
/// カード作成で選べる範囲(<see cref="CardCreationOptions"/>)に収まっているかを見る。
/// </summary>
public static class ValidateCustomCard
{
    /// <summary>自作カードの ID はこの値から振る(組み込みのカードと重ならないように)。</summary>
    public const int FirstCustomCardId = 10_000;

    public static bool IsCustomCardId(int cardId) => cardId >= FirstCustomCardId;

    /// <returns>ルールに合っていない理由。合っていれば空。</returns>
    public static IReadOnlyList<CustomCardError> Execute(CardData card, CardCreationOptions options)
    {
        List<CustomCardError> errors = [];

        if (!IsCustomCardId(card.Id)) errors.Add(CustomCardError.InvalidId);
        if (string.IsNullOrWhiteSpace(card.Name)) errors.Add(CustomCardError.NameRequired);
        else if (card.Name.Length > options.MaxNameLength) errors.Add(CustomCardError.NameTooLong);

        switch (card.Kind)
        {
            case CardKind.NormalMonster:
                CheckMonster(card, options, errors);
                break;

            case CardKind.Spell or CardKind.Trap:
                CheckEffect(card, options, errors);
                break;

            default:
                errors.Add(CustomCardError.UnsupportedKind);
                break;
        }

        // 個別の範囲は満たしていても、組み合わせとして成り立たない場合
        if (errors.Count == 0 && !ValidateCardData.Execute(card)) errors.Add(CustomCardError.InvalidCombination);

        return errors;
    }

    private static void CheckMonster(CardData card, CardCreationOptions options, List<CustomCardError> errors)
    {
        if (card.Level < options.MinLevel || card.Level > options.MaxLevel) errors.Add(CustomCardError.LevelOutOfRange);
        if (!IsOnStep(card.Attack, 0, options.MaxStat, options.StatStep)
            || !IsOnStep(card.Defense, 0, options.MaxStat, options.StatStep))
        {
            errors.Add(CustomCardError.StatOutOfRange);
        }
    }

    private static void CheckEffect(CardData card, CardCreationOptions options, List<CustomCardError> errors)
    {
        if (card.Effect is not { Actions.Count: > 0 } effect)
        {
            errors.Add(CustomCardError.NoEffect);
            return;
        }

        if (effect.Actions.Count > options.MaxEffectActions) errors.Add(CustomCardError.TooManyActions);
        if (!effect.Actions.All(action => IsAmountInRange(action, options))) errors.Add(CustomCardError.AmountOutOfRange);
        if (card.Kind == CardKind.Trap && effect.TargetSide is not null) errors.Add(CustomCardError.TrapCannotTarget);
    }

    private static bool IsAmountInRange(EffectAction action, CardCreationOptions options) => action switch
    {
        DrawCardsAction draw => draw.Count >= 1 && draw.Count <= options.MaxDrawCount,
        DealDamageAction damage => IsOnStep(damage.Amount, options.LifeAmountStep, options.MaxLifeAmount, options.LifeAmountStep),
        GainLifePointsAction gain => IsOnStep(gain.Amount, options.LifeAmountStep, options.MaxLifeAmount, options.LifeAmountStep),
        _ => true,
    };

    private static bool IsOnStep(int value, int min, int max, int step) =>
        value >= min && value <= max && step > 0 && value % step == 0;
}
