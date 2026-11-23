using CardGame.Core.Battle;
using CardGame.Core.Cards;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 1回の対戦。ターン進行・召喚・攻撃のルールを検証し、盤面を更新する。
/// 操作が拒否された場合は例外ではなく <see cref="ActionResult"/> で理由を返す。
/// </summary>
public sealed class Duel
{
    private const int MaxLevelWithoutTribute = 4;

    private readonly GameRulesOptions _rules;
    private readonly Player[] _players;
    private readonly List<DuelLogEntry> _log = [];
    private int _currentIndex;

    /// <param name="firstPlayer">先攻プレイヤー。デッキはシャッフル済みで渡す。</param>
    public Duel(Player firstPlayer, Player secondPlayer, GameRulesOptions rules)
    {
        _rules = rules;
        _players = [firstPlayer, secondPlayer];
        TurnCount = 1;

        foreach (var player in _players)
        {
            DrawInitialHand(player, rules.InitialHandSize);
        }

        BeginTurn();
    }

    public Player FirstPlayer => _players[0];
    public Player SecondPlayer => _players[1];
    public Player CurrentPlayer => _players[_currentIndex];
    public Player OpponentPlayer => _players[1 - _currentIndex];

    public Phase CurrentPhase { get; private set; }
    public int TurnCount { get; private set; }
    public bool NormalSummonUsed { get; private set; }

    /// <summary>勝者。決着がついていなければ null。</summary>
    public Player? Winner { get; private set; }

    public bool IsFinished => Winner is not null;

    /// <summary>対戦中の出来事(古い順)。</summary>
    public IReadOnlyList<DuelLogEntry> Log => _log;

    public ActionResult AdvancePhase()
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);

        if (CurrentPhase == Phase.End)
        {
            EndTurn();
            return ActionResult.Success;
        }

        CurrentPhase = CurrentPhase switch
        {
            Phase.Draw => Phase.Standby,
            Phase.Standby => Phase.Main1,
            Phase.Main1 => Phase.Battle,
            Phase.Battle => Phase.Main2,
            Phase.Main2 => Phase.End,
            _ => throw new InvalidOperationException($"Unexpected phase: {CurrentPhase}"),
        };
        return ActionResult.Success;
    }

    /// <summary>召喚できるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanSummon(int handIndex, BattlePosition position)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (CurrentPhase is not (Phase.Main1 or Phase.Main2)) return ActionResult.Fail(ActionError.WrongPhase);
        if (position == BattlePosition.FaceUpDefense) return ActionResult.Fail(ActionError.InvalidPosition);
        if (handIndex < 0 || handIndex >= CurrentPlayer.Hand.Count) return ActionResult.Fail(ActionError.InvalidHandIndex);

        var card = CurrentPlayer.Hand[handIndex];
        if (card.Kind is not (CardKind.NormalMonster or CardKind.EffectMonster)) return ActionResult.Fail(ActionError.NotAMonster);
        if (card.Level > MaxLevelWithoutTribute) return ActionResult.Fail(ActionError.TributeRequired);
        if (NormalSummonUsed) return ActionResult.Fail(ActionError.NormalSummonAlreadyUsed);
        if (CurrentPlayer.MonsterZone.Count >= Player.MonsterZoneSize) return ActionResult.Fail(ActionError.MonsterZoneFull);

        return ActionResult.Success;
    }

    /// <summary>指定したマスに召喚できるかを判定する(盤面は変更しない)。</summary>
    public ActionResult CanSummon(int handIndex, BattlePosition position, int slotIndex)
    {
        var check = CanSummon(handIndex, position);
        if (!check.IsSuccess) return check;

        if (slotIndex < 0 || slotIndex >= Player.MonsterZoneSize) return ActionResult.Fail(ActionError.InvalidSlot);
        if (!CurrentPlayer.IsSlotEmpty(slotIndex)) return ActionResult.Fail(ActionError.SlotOccupied);

        return ActionResult.Success;
    }

    /// <summary>手札のモンスターを、空いているマスのうち一番左に通常召喚またはセットする。</summary>
    public ActionResult Summon(int handIndex, BattlePosition position)
    {
        var check = CanSummon(handIndex, position);
        if (!check.IsSuccess) return check;

        return Summon(handIndex, position, CurrentPlayer.FindEmptySlot());
    }

    /// <summary>手札のモンスターを、指定したマスに通常召喚(攻撃表示)またはセット(伏せ守備)する。</summary>
    public ActionResult Summon(int handIndex, BattlePosition position, int slotIndex)
    {
        var check = CanSummon(handIndex, position, slotIndex);
        if (!check.IsSuccess) return check;

        var card = CurrentPlayer.Hand[handIndex];
        CurrentPlayer.SummonFromHand(handIndex, position, slotIndex);
        NormalSummonUsed = true;

        var kind = position == BattlePosition.FaceDownDefense ? DuelLogKind.Set : DuelLogKind.Summoned;
        AddLog(kind, CurrentPlayer, card.Name);
        return ActionResult.Success;
    }

    /// <summary>攻撃できるかを判定する(盤面は変更しない)。</summary>
    /// <param name="target">攻撃対象。null は直接攻撃。</param>
    public ActionResult CanAttack(FieldMonster attacker, FieldMonster? target)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (CurrentPhase != Phase.Battle) return ActionResult.Fail(ActionError.WrongPhase);
        if (TurnCount == 1) return ActionResult.Fail(ActionError.CannotAttackOnFirstTurn);
        if (!CurrentPlayer.MonsterZone.Contains(attacker)) return ActionResult.Fail(ActionError.AttackerNotOnYourField);
        if (attacker.Position != BattlePosition.FaceUpAttack) return ActionResult.Fail(ActionError.AttackerNotInAttackPosition);
        if (attacker.HasAttacked) return ActionResult.Fail(ActionError.AlreadyAttacked);

        if (target is null)
        {
            if (OpponentPlayer.MonsterZone.Count > 0) return ActionResult.Fail(ActionError.DirectAttackNotAllowed);
        }
        else if (!OpponentPlayer.MonsterZone.Contains(target))
        {
            return ActionResult.Fail(ActionError.InvalidTarget);
        }

        return ActionResult.Success;
    }

    /// <param name="target">攻撃対象。null は直接攻撃(相手の場にモンスターがいない場合のみ)。</param>
    public ActionResult Attack(FieldMonster attacker, FieldMonster? target)
    {
        var check = CanAttack(attacker, target);
        if (!check.IsSuccess) return check;

        var attackerOwner = CurrentPlayer;
        var targetOwner = OpponentPlayer;
        attacker.HasAttacked = true;

        if (target is null)
        {
            AddLog(DuelLogKind.DirectAttacked, attackerOwner, attacker.Card.Name);
        }
        else
        {
            var isFaceDown = target.Position == BattlePosition.FaceDownDefense;
            AddLog(DuelLogKind.Attacked, attackerOwner, attacker.Card.Name, isFaceDown ? null : target.Card.Name);
            if (isFaceDown)
            {
                AddLog(DuelLogKind.Flipped, targetOwner, target.Card.Name);
            }
        }

        var outcome = ResolveBattle.Execute(attackerOwner, targetOwner, attacker, target);

        if (outcome.AttackerDestroyed) AddLog(DuelLogKind.Destroyed, attackerOwner, attacker.Card.Name);
        if (outcome.TargetDestroyed && target is not null) AddLog(DuelLogKind.Destroyed, targetOwner, target.Card.Name);
        if (outcome.DamageToAttackerOwner > 0) AddLog(DuelLogKind.Damaged, attackerOwner, amount: outcome.DamageToAttackerOwner);
        if (outcome.DamageToTargetOwner > 0) AddLog(DuelLogKind.Damaged, targetOwner, amount: outcome.DamageToTargetOwner);

        UpdateWinner();
        return ActionResult.Success;
    }

    private static void DrawInitialHand(Player player, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!player.Draw()) return;
        }
    }

    private void BeginTurn()
    {
        CurrentPhase = Phase.Draw;
        AddLog(DuelLogKind.TurnStarted, CurrentPlayer);

        var skipDraw = TurnCount == 1 && _rules.FirstPlayerSkipsFirstDraw;
        if (!skipDraw && !CurrentPlayer.Draw())
        {
            AddLog(DuelLogKind.DeckOut, CurrentPlayer);
            SetWinner(OpponentPlayer);
        }
    }

    private void EndTurn()
    {
        _currentIndex = 1 - _currentIndex;
        TurnCount++;
        NormalSummonUsed = false;

        foreach (var player in _players)
        {
            foreach (var monster in player.MonsterZone)
            {
                monster.HasAttacked = false;
            }
        }

        BeginTurn();
    }

    private void UpdateWinner()
    {
        if (FirstPlayer.LifePoints <= 0) SetWinner(SecondPlayer);
        else if (SecondPlayer.LifePoints <= 0) SetWinner(FirstPlayer);
    }

    private void SetWinner(Player winner)
    {
        Winner = winner;
        AddLog(DuelLogKind.Won, winner);
    }

    private void AddLog(
        DuelLogKind kind,
        Player player,
        string? cardName = null,
        string? targetCardName = null,
        int amount = 0)
    {
        _log.Add(new DuelLogEntry(TurnCount, kind, player.Name, cardName, targetCardName, amount));
    }
}
