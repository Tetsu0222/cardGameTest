using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Core.Duels;

/// <summary>
/// 罠の発動。ターンプレイヤーの攻撃宣言・召喚に対して、相手が罠を発動できる機会(<see cref="ResponseWindow"/>)を開く。
/// <list type="bullet">
/// <item>機会を開くのは、相手が今発動できる罠を持っているときだけ(持っていなければ、これまでどおりすぐに処理が進む)。</item>
/// <item>罠は前のターン以前にセットしたもので、発動タイミング(<see cref="CardEffect.Trigger"/>)が合うものだけ発動できる。</item>
/// <item>発動できる罠は1枚だけで、罠に対してさらに罠を発動することはできない(チェーンは未対応)。</item>
/// <item>攻撃宣言に対する罠の処理後、攻撃モンスターが場に残っていて攻撃が無効になっていなければ戦闘を行う。
/// 攻撃対象が場を離れていた場合も戦闘は行わない(攻撃の巻き戻しは未対応)。</item>
/// </list>
/// </summary>
public sealed partial class Duel
{
    /// <summary>罠を発動するかを決める機会。開いていなければ null。</summary>
    public ResponseWindow? PendingResponse { get; private set; }

    /// <summary>今開いている機会で、発動できる罠(対象はまだ決めていない状態での判定)。</summary>
    public IReadOnlyList<SpellTrapCard> FindActivatableTraps() =>
        PendingResponse is { } window
            ? [.. window.Responder.SpellTrapZone.Where(card => CanActivateTrap(card).IsSuccess)]
            : [];

    /// <summary>罠を発動できる可能性があるかを判定する(対象はまだ決めていない状態。盤面は変更しない)。</summary>
    public ActionResult CanActivateTrap(SpellTrapCard setCard) =>
        CheckTrapActivation(setCard, target: null, isTargetChosen: false);

    /// <summary>対象を決めた状態で、罠を発動できるかを判定する(盤面は変更しない)。</summary>
    /// <param name="target">対象。対象を取らない罠では null。</param>
    public ActionResult CanActivateTrap(SpellTrapCard setCard, FieldMonster? target) =>
        CheckTrapActivation(setCard, target, isTargetChosen: true);

    /// <summary>
    /// 罠を表にして発動し、効果を処理して墓地へ送る。その後、保留していた攻撃の戦闘を行い、機会を閉じる。
    /// </summary>
    /// <param name="target">対象。対象を取らない罠では null。</param>
    public ActionResult ActivateTrap(SpellTrapCard setCard, FieldMonster? target)
    {
        var check = CanActivateTrap(setCard, target);
        if (!check.IsSuccess) return check;
        if (PendingResponse is not { } window || setCard.Card.Effect is not { } effect)
        {
            throw new InvalidOperationException("Trap activation passed validation without a window or effect.");
        }

        setCard.IsFaceUp = true;
        var isTargetHidden = target?.Position == BattlePosition.FaceDownDefense;
        _log.Add(DuelLogKind.TrapActivated, window.Responder, setCard.Card.Name,
            target is null || isTargetHidden ? null : target.Card.Name, isTargetHidden: isTargetHidden);

        var outcome = ResolveCardEffect.Execute(effect, window.Responder, CurrentPlayer, target, _log, window.TriggerMonster);
        window.Responder.SendSpellTrapToGraveyard(setCard);

        PendingResponse = null;

        // 罠の効果で決着がついたら、保留していた戦闘は行わない
        UpdateWinner();
        if (!IsFinished)
        {
            if (outcome.IsAttackNegated)
            {
                _log.Add(DuelLogKind.AttackNegated, CurrentPlayer, window.TriggerMonster.Card.Name);
            }
            else
            {
                ContinueAfterResponse(window);
            }
        }

        // 召喚に応じた罠の場合、召喚成功時・リバースの効果はここで処理する
        ResolveTriggeredEffects();
        return ActionResult.Success;
    }

    /// <summary>罠を発動せずに見送る。保留していた攻撃の戦闘を行い、機会を閉じる。</summary>
    public ActionResult PassResponse()
    {
        if (PendingResponse is not { } window) return ActionResult.Fail(ActionError.NoResponseWindow);

        PendingResponse = null;
        ContinueAfterResponse(window);
        ResolveTriggeredEffects();
        return ActionResult.Success;
    }

    /// <summary>相手が今発動できる罠を持っていれば、機会を開く。</summary>
    /// <returns>機会を開いたか。</returns>
    private bool TryOpenResponseWindow(TrapTrigger trigger, FieldMonster triggerMonster, FieldMonster? attackTarget)
    {
        PendingResponse = new ResponseWindow(trigger, OpponentPlayer, triggerMonster, attackTarget);
        if (FindActivatableTraps().Count > 0) return true;

        PendingResponse = null;
        return false;
    }

    private void ContinueAfterResponse(ResponseWindow window)
    {
        if (window.Trigger != TrapTrigger.OpponentAttackDeclared) return;

        var attackerStillOnField = CurrentPlayer.MonsterZone.Contains(window.TriggerMonster);
        var targetStillOnField = window.AttackTarget is null || OpponentPlayer.MonsterZone.Contains(window.AttackTarget);
        if (attackerStillOnField && targetStillOnField)
        {
            ResolveAttack(window.TriggerMonster, window.AttackTarget);
        }
    }

    private ActionResult CheckTrapActivation(SpellTrapCard setCard, FieldMonster? target, bool isTargetChosen)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not { } window) return ActionResult.Fail(ActionError.NoResponseWindow);

        var responder = window.Responder;
        if (!responder.SpellTrapZone.Contains(setCard)) return ActionResult.Fail(ActionError.CardNotOnYourField);
        if (setCard.Card.Kind != CardKind.Trap) return ActionResult.Fail(ActionError.NotATrap);
        if (setCard.IsFaceUp) return ActionResult.Fail(ActionError.ActivationConditionNotMet);
        if (setCard.SetTurn >= TurnCount) return ActionResult.Fail(ActionError.TrapSetThisTurn);
        if (setCard.Card.Effect is not { } effect || effect.Trigger != window.Trigger)
        {
            return ActionResult.Fail(ActionError.ActivationConditionNotMet);
        }

        // 罠を持つプレイヤーから見ると、ターンプレイヤーが相手になる
        var error = isTargetChosen
            ? CheckEffectActivation.Execute(effect, responder, CurrentPlayer, target)
            : CheckEffectActivation.ExecuteBeforeTargeting(effect, responder, CurrentPlayer);
        return ActionResult.From(error);
    }
}
