using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Core.Effects;

/// <summary>
/// 効果の発動条件を判定する(盤面は変更しない)。
/// 「いつ・どこから発動できるか」はカードの種類ごとに <see cref="Duel"/> が判定し、ここは効果そのものの条件だけを見る。
/// </summary>
public static class CheckEffectActivation
{
    /// <summary>対象を決めた状態で発動できるか。</summary>
    /// <param name="target">対象。対象を取らない効果では null を渡す。</param>
    public static ActionError Execute(CardEffect effect, Player controller, Player opponent, FieldMonster? target)
    {
        var common = CheckCommon(effect, controller);
        if (common != ActionError.None) return common;

        if (effect.TargetSide is null)
        {
            return target is null ? ActionError.None : ActionError.TargetNotAllowed;
        }

        if (target is null) return ActionError.TargetRequired;

        return FindEffectTargets.Execute(effect, controller, opponent).Contains(target)
            ? ActionError.None
            : ActionError.InvalidTarget;
    }

    /// <summary>対象をまだ決めていない状態で、発動できる可能性があるか(対象を取る効果なら、対象が1体以上いるか)。</summary>
    public static ActionError ExecuteBeforeTargeting(CardEffect effect, Player controller, Player opponent)
    {
        var common = CheckCommon(effect, controller);
        if (common != ActionError.None) return common;

        if (effect.TargetSide is not null && FindEffectTargets.Execute(effect, controller, opponent).Count == 0)
        {
            return ActionError.NoValidTarget;
        }

        return ActionError.None;
    }

    private static ActionError CheckCommon(CardEffect effect, Player controller)
    {
        if (!ValidateCardEffect.Execute(effect)) return ActionError.ActivationConditionNotMet;

        // ドローする枚数がデッキに残っていなければ発動できない
        var drawCount = effect.Actions.OfType<DrawCardsAction>().Sum(a => a.Count);
        if (controller.Deck.Count < drawCount) return ActionError.ActivationConditionNotMet;

        return ActionError.None;
    }
}
