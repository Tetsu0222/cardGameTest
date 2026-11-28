using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 表示形式の変更と反転召喚。自分のメインフェーズに、モンスター1体につき1ターンに1回行える。
/// <list type="bullet">
/// <item>表側攻撃表示 ⇔ 表側守備表示を切り替える。</item>
/// <item>裏側守備表示のモンスターは、表側攻撃表示にする(反転召喚)。反転召喚は召喚の一種で、相手の召喚時の罠を発動できる機会を開く。</item>
/// <item>そのターンに召喚・セットしたモンスター、そのターンに攻撃したモンスターは変更できない。</item>
/// </list>
/// </summary>
public sealed partial class Duel
{
    /// <summary>表示形式を変更(または反転召喚)できるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanChangePosition(FieldMonster monster)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not null) return ActionResult.Fail(ActionError.ResponsePending);
        if (PendingEffectTarget is not null) return ActionResult.Fail(ActionError.EffectTargetPending);
        if (CurrentPhase is not (Phase.Main1 or Phase.Main2)) return ActionResult.Fail(ActionError.WrongPhase);
        if (!CurrentPlayer.MonsterZone.Contains(monster)) return ActionResult.Fail(ActionError.MonsterNotOnYourField);
        if (monster.SummonedTurn >= TurnCount) return ActionResult.Fail(ActionError.SummonedThisTurn);
        if (monster.PositionChangedThisTurn) return ActionResult.Fail(ActionError.PositionAlreadyChanged);
        if (monster.HasAttacked) return ActionResult.Fail(ActionError.AlreadyAttacked);

        return ActionResult.Success;
    }

    /// <summary>
    /// 表示形式を変更する。攻撃表示なら守備表示に、守備表示なら攻撃表示に、裏側守備表示なら反転召喚して攻撃表示にする。
    /// </summary>
    public ActionResult ChangePosition(FieldMonster monster)
    {
        var check = CanChangePosition(monster);
        if (!check.IsSuccess) return check;

        monster.PositionChangedThisTurn = true;
        switch (monster.Position)
        {
            case BattlePosition.FaceUpAttack:
                monster.Position = BattlePosition.FaceUpDefense;
                _log.Add(DuelLogKind.ChangedToDefensePosition, CurrentPlayer, monster.Card.Name);
                break;

            case BattlePosition.FaceUpDefense:
                monster.Position = BattlePosition.FaceUpAttack;
                _log.Add(DuelLogKind.ChangedToAttackPosition, CurrentPlayer, monster.Card.Name);
                break;

            case BattlePosition.FaceDownDefense:
                monster.Position = BattlePosition.FaceUpAttack;
                _log.Add(DuelLogKind.FlipSummoned, CurrentPlayer, monster.Card.Name);
                TriggerMonsterEffect(monster, CurrentPlayer, MonsterEffectTrigger.Flipped);
                if (!TryOpenResponseWindow(TrapTrigger.OpponentMonsterSummoned, monster, attackTarget: null))
                {
                    ResolveTriggeredEffects();
                }

                break;
        }

        return ActionResult.Success;
    }
}
