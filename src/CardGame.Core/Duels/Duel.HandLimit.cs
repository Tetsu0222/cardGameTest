using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 手札の枚数制限。エンドフェーズに手札が <see cref="GameRulesOptions.MaxHandSize"/> を超えていれば、
/// ターンプレイヤーは上限になるまで手札を選んで捨てる。捨て終わるまでターンを終えられない。
/// </summary>
public sealed partial class Duel
{
    /// <summary>今捨てなければならない手札の枚数。エンドフェーズで手札が上限を超えているときだけ 1 以上。</summary>
    public int RequiredDiscardCount =>
        !IsFinished && CurrentPhase == Phase.End
            ? Math.Max(0, CurrentPlayer.Hand.Count - _rules.MaxHandSize)
            : 0;

    /// <summary>手札を捨てられるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanDiscard(int handIndex)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (RequiredDiscardCount == 0) return ActionResult.Fail(ActionError.NoDiscardRequired);
        if (handIndex < 0 || handIndex >= CurrentPlayer.Hand.Count) return ActionResult.Fail(ActionError.InvalidHandIndex);

        return ActionResult.Success;
    }

    /// <summary>手札の上限を超えた分として、選んだカードを墓地へ送る。</summary>
    public ActionResult Discard(int handIndex)
    {
        var check = CanDiscard(handIndex);
        if (!check.IsSuccess) return check;

        var card = CurrentPlayer.TakeFromHand(handIndex)
            ?? throw new InvalidOperationException("Discard passed validation but the card could not be taken.");
        CurrentPlayer.SendToGraveyard(card);
        _log.Add(DuelLogKind.Discarded, CurrentPlayer, card.Name);
        return ActionResult.Success;
    }
}
