using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 魔法・罠ゾーンへのセットと、通常魔法の発動。
/// 通常魔法は自分のメインフェーズに、手札から、またはセットしたカードを表にして発動できる。
/// 効果の処理後は墓地へ送られる(チェーンは未対応のため、発動するとすぐに処理する)。
/// </summary>
public sealed partial class Duel
{
    // ---- セット ----

    /// <summary>手札の魔法・罠をセットできるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanSetSpellTrap(int handIndex)
    {
        var check = CheckMainPhaseAction();
        if (!check.IsSuccess) return check;
        if (handIndex < 0 || handIndex >= CurrentPlayer.Hand.Count) return ActionResult.Fail(ActionError.InvalidHandIndex);

        var card = CurrentPlayer.Hand[handIndex];
        if (card.Kind is not (CardKind.Spell or CardKind.Trap)) return ActionResult.Fail(ActionError.NotASpellOrTrap);
        if (CurrentPlayer.FindEmptySpellTrapSlot() < 0) return ActionResult.Fail(ActionError.SpellTrapZoneFull);

        return ActionResult.Success;
    }

    /// <summary>指定したマスにセットできるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanSetSpellTrap(int handIndex, int slotIndex)
    {
        var check = CanSetSpellTrap(handIndex);
        if (!check.IsSuccess) return check;

        if (slotIndex < 0 || slotIndex >= Player.SpellTrapZoneSize) return ActionResult.Fail(ActionError.InvalidSlot);
        if (!CurrentPlayer.IsSpellTrapSlotEmpty(slotIndex)) return ActionResult.Fail(ActionError.SlotOccupied);

        return ActionResult.Success;
    }

    /// <summary>手札の魔法・罠を、空いているマスのうち一番左にセットする。</summary>
    public ActionResult SetSpellTrap(int handIndex)
    {
        var check = CanSetSpellTrap(handIndex);
        if (!check.IsSuccess) return check;

        return SetSpellTrap(handIndex, CurrentPlayer.FindEmptySpellTrapSlot());
    }

    /// <summary>手札の魔法・罠を、指定したマスに裏側でセットする。</summary>
    public ActionResult SetSpellTrap(int handIndex, int slotIndex)
    {
        var check = CanSetSpellTrap(handIndex, slotIndex);
        if (!check.IsSuccess) return check;

        CurrentPlayer.SetSpellTrapFromHand(handIndex, slotIndex, TurnCount);
        _log.Add(DuelLogKind.SpellTrapSet, CurrentPlayer);
        return ActionResult.Success;
    }

    // ---- 手札からの発動 ----

    /// <summary>
    /// 手札の魔法を発動できる可能性があるかを判定する(対象はまだ決めていない状態。盤面は変更しない)。
    /// 対象を取る魔法なら、対象にできるモンスターが1体以上いるかまで見る。
    /// </summary>
    public ActionResult CanActivateSpellFromHand(int handIndex) =>
        CheckActivationFromHand(handIndex, target: null, isTargetChosen: false);

    /// <summary>対象を決めた状態で、手札の魔法を発動できるかを判定する(盤面は変更しない)。</summary>
    /// <param name="target">対象。対象を取らない魔法では null。</param>
    public ActionResult CanActivateSpellFromHand(int handIndex, FieldMonster? target) =>
        CheckActivationFromHand(handIndex, target, isTargetChosen: true);

    /// <summary>手札の魔法を発動し、効果を処理して墓地へ送る。</summary>
    /// <param name="target">対象。対象を取らない魔法では null。</param>
    public ActionResult ActivateSpellFromHand(int handIndex, FieldMonster? target)
    {
        var check = CanActivateSpellFromHand(handIndex, target);
        if (!check.IsSuccess) return check;

        var card = CurrentPlayer.Hand[handIndex];
        CurrentPlayer.TakeFromHand(handIndex);
        ResolveSpell(card, target);
        CurrentPlayer.SendToGraveyard(card);
        UpdateWinner();
        return ActionResult.Success;
    }

    // ---- セットしたカードの発動 ----

    /// <summary>セットした魔法を発動できる可能性があるかを判定する(対象はまだ決めていない状態。盤面は変更しない)。</summary>
    public ActionResult CanActivateSetSpell(SpellTrapCard setCard) =>
        CheckActivationFromField(setCard, target: null, isTargetChosen: false);

    /// <summary>対象を決めた状態で、セットした魔法を発動できるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanActivateSetSpell(SpellTrapCard setCard, FieldMonster? target) =>
        CheckActivationFromField(setCard, target, isTargetChosen: true);

    /// <summary>セットした魔法を表にして発動し、効果を処理して墓地へ送る。</summary>
    /// <param name="target">対象。対象を取らない魔法では null。</param>
    public ActionResult ActivateSetSpell(SpellTrapCard setCard, FieldMonster? target)
    {
        var check = CanActivateSetSpell(setCard, target);
        if (!check.IsSuccess) return check;

        var controller = CurrentPlayer;
        setCard.IsFaceUp = true;
        ResolveSpell(setCard.Card, target);
        controller.SendSpellTrapToGraveyard(setCard);
        UpdateWinner();
        return ActionResult.Success;
    }

    // ---- 対象 ----

    /// <summary>現在のターンプレイヤーがこのカードを発動するとき、対象にできるモンスター。対象を取らないカードなら空。</summary>
    public IReadOnlyList<FieldMonster> FindSpellTargets(CardData card) =>
        card.Effect is null ? [] : FindEffectTargets.Execute(card.Effect, CurrentPlayer, OpponentPlayer);

    // ---- 内部処理 ----

    private ActionResult CheckMainPhaseAction()
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not null) return ActionResult.Fail(ActionError.ResponsePending);
        if (PendingEffectTarget is not null) return ActionResult.Fail(ActionError.EffectTargetPending);
        if (CurrentPhase is not (Phase.Main1 or Phase.Main2)) return ActionResult.Fail(ActionError.WrongPhase);
        return ActionResult.Success;
    }

    private ActionResult CheckActivationFromHand(int handIndex, FieldMonster? target, bool isTargetChosen)
    {
        var check = CheckMainPhaseAction();
        if (!check.IsSuccess) return check;
        if (handIndex < 0 || handIndex >= CurrentPlayer.Hand.Count) return ActionResult.Fail(ActionError.InvalidHandIndex);

        var card = CurrentPlayer.Hand[handIndex];
        if (card.Kind != CardKind.Spell) return ActionResult.Fail(ActionError.NotASpell);

        // 手札から発動する魔法は、処理されるまで魔法・罠ゾーンに置かれるため空きが必要
        if (CurrentPlayer.FindEmptySpellTrapSlot() < 0) return ActionResult.Fail(ActionError.SpellTrapZoneFull);

        return CheckSpellEffect(card, target, isTargetChosen);
    }

    private ActionResult CheckActivationFromField(SpellTrapCard setCard, FieldMonster? target, bool isTargetChosen)
    {
        var check = CheckMainPhaseAction();
        if (!check.IsSuccess) return check;
        if (!CurrentPlayer.SpellTrapZone.Contains(setCard)) return ActionResult.Fail(ActionError.CardNotOnYourField);
        if (setCard.Card.Kind != CardKind.Spell) return ActionResult.Fail(ActionError.NotASpell);
        if (setCard.IsFaceUp) return ActionResult.Fail(ActionError.ActivationConditionNotMet);

        return CheckSpellEffect(setCard.Card, target, isTargetChosen);
    }

    private ActionResult CheckSpellEffect(CardData card, FieldMonster? target, bool isTargetChosen)
    {
        // 発動タイミングを持つ効果は罠用(魔法としては発動できない)
        if (card.Effect is null || card.Effect.Trigger is not null) return ActionResult.Fail(ActionError.ActivationConditionNotMet);

        var error = isTargetChosen
            ? CheckEffectActivation.Execute(card.Effect, CurrentPlayer, OpponentPlayer, target)
            : CheckEffectActivation.ExecuteBeforeTargeting(card.Effect, CurrentPlayer, OpponentPlayer);
        return ActionResult.From(error);
    }

    private void ResolveSpell(CardData card, FieldMonster? target)
    {
        if (card.Effect is not { } effect)
        {
            throw new InvalidOperationException($"Spell without effect passed validation: {card.Name}");
        }

        var isTargetHidden = target?.Position == BattlePosition.FaceDownDefense;
        var targetName = target is null || isTargetHidden ? null : target.Card.Name;
        _log.Add(DuelLogKind.SpellActivated, CurrentPlayer, card.Name, targetName, isTargetHidden: isTargetHidden);
        ResolveCardEffect.Execute(effect, CurrentPlayer, OpponentPlayer, target, _log);
    }
}
