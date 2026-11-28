using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Summons;
using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 通常召喚・セット(アドバンス召喚・アドバンスセットを含む)。1ターンに1回だけ行える。
/// レベル5以上のモンスターは、自分の場のモンスターをリリースして召喚する(<see cref="CountRequiredTributes"/>)。
/// </summary>
public sealed partial class Duel
{
    /// <summary>
    /// 召喚できる可能性があるかを判定する(リリースとマスはまだ決めていない状態。盤面は変更しない)。
    /// アドバンス召喚なら、リリースできるモンスターが足りているかまで見る。
    /// </summary>
    public ActionResult CanSummon(int handIndex, BattlePosition position)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not null) return ActionResult.Fail(ActionError.ResponsePending);
        if (PendingEffectTarget is not null) return ActionResult.Fail(ActionError.EffectTargetPending);
        if (CurrentPhase is not (Phase.Main1 or Phase.Main2)) return ActionResult.Fail(ActionError.WrongPhase);
        if (position == BattlePosition.FaceUpDefense) return ActionResult.Fail(ActionError.InvalidPosition);
        if (handIndex < 0 || handIndex >= CurrentPlayer.Hand.Count) return ActionResult.Fail(ActionError.InvalidHandIndex);

        var card = CurrentPlayer.Hand[handIndex];
        if (card.Kind is not (CardKind.NormalMonster or CardKind.EffectMonster)) return ActionResult.Fail(ActionError.NotAMonster);
        if (NormalSummonUsed) return ActionResult.Fail(ActionError.NormalSummonAlreadyUsed);

        var required = CountRequiredTributes.Execute(card);
        if (required == 0)
        {
            // リリースしない場合だけ空きマスが必要(リリースすればそのマスが空く)
            if (CurrentPlayer.FindEmptySlot() < 0) return ActionResult.Fail(ActionError.MonsterZoneFull);
        }
        else if (CurrentPlayer.MonsterZone.Count < required)
        {
            return ActionResult.Fail(ActionError.NotEnoughTributes);
        }

        return ActionResult.Success;
    }

    /// <summary>リリースとマスを決めた状態で召喚できるかを判定する(盤面は変更しない)。</summary>
    /// <param name="slotIndex">出すマス。空きマスか、リリースするモンスターのマス。</param>
    /// <param name="tributes">リリースするモンスター。リリースが不要なら null または空。</param>
    public ActionResult CanSummon(
        int handIndex,
        BattlePosition position,
        int slotIndex,
        IReadOnlyList<FieldMonster>? tributes = null)
    {
        var check = CanSummon(handIndex, position);
        if (!check.IsSuccess) return check;

        tributes ??= [];
        var required = CountRequiredTributes.Execute(CurrentPlayer.Hand[handIndex]);
        if (tributes.Count != required) return ActionResult.Fail(ActionError.WrongTributeCount);
        if (tributes.Distinct().Count() != tributes.Count) return ActionResult.Fail(ActionError.InvalidTribute);
        if (!tributes.All(CurrentPlayer.MonsterZone.Contains)) return ActionResult.Fail(ActionError.InvalidTribute);

        if (slotIndex < 0 || slotIndex >= Player.MonsterZoneSize) return ActionResult.Fail(ActionError.InvalidSlot);
        var isFreedByTribute = tributes.Any(t => CurrentPlayer.FindMonsterSlot(t) == slotIndex);
        if (!CurrentPlayer.IsSlotEmpty(slotIndex) && !isFreedByTribute) return ActionResult.Fail(ActionError.SlotOccupied);

        return ActionResult.Success;
    }

    /// <summary>
    /// 手札のモンスターを通常召喚(攻撃表示)またはセット(裏側守備表示)する。
    /// 出すマスは、空きマスとリリースするモンスターのマスのうち一番左。
    /// </summary>
    /// <param name="tributes">リリースするモンスター。リリースが不要なら null または空。</param>
    public ActionResult Summon(int handIndex, BattlePosition position, IReadOnlyList<FieldMonster>? tributes = null)
    {
        var check = CanSummon(handIndex, position);
        if (!check.IsSuccess) return check;

        tributes ??= [];
        var slot = Enumerable.Range(0, Player.MonsterZoneSize)
            .FirstOrDefault(i => CurrentPlayer.IsSlotEmpty(i) || tributes.Any(t => CurrentPlayer.FindMonsterSlot(t) == i), -1);
        return Summon(handIndex, position, slot, tributes);
    }

    /// <summary>手札のモンスターを、指定したマスに通常召喚(攻撃表示)またはセット(裏側守備表示)する。</summary>
    /// <param name="slotIndex">出すマス。空きマスか、リリースするモンスターのマス。</param>
    /// <param name="tributes">リリースするモンスター。リリースが不要なら null または空。</param>
    public ActionResult Summon(
        int handIndex,
        BattlePosition position,
        int slotIndex,
        IReadOnlyList<FieldMonster>? tributes = null)
    {
        var check = CanSummon(handIndex, position, slotIndex, tributes);
        if (!check.IsSuccess) return check;

        foreach (var tribute in tributes ?? [])
        {
            CurrentPlayer.TributeMonster(tribute);
            _log.Add(DuelLogKind.Tributed, CurrentPlayer, tribute.Card.Name);
        }

        var card = CurrentPlayer.Hand[handIndex];
        var summoned = CurrentPlayer.SummonFromHand(handIndex, position, slotIndex, TurnCount)
            ?? throw new InvalidOperationException("Summon passed validation but could not be placed.");
        NormalSummonUsed = true;

        if (position == BattlePosition.FaceDownDefense)
        {
            _log.Add(DuelLogKind.Set, CurrentPlayer, card.Name);
        }
        else
        {
            _log.Add(DuelLogKind.Summoned, CurrentPlayer, card.Name);

            // 召喚成功時(セットは含まない)の効果を発動させ、相手が罠を発動できる機会を開く。
            // 効果は罠の機会が閉じてから処理する(機会を開かなければすぐに処理する)
            TriggerMonsterEffect(summoned, CurrentPlayer, MonsterEffectTrigger.Summoned);
            if (!TryOpenResponseWindow(TrapTrigger.OpponentMonsterSummoned, summoned, attackTarget: null))
            {
                ResolveTriggeredEffects();
            }
        }

        return ActionResult.Success;
    }
}
