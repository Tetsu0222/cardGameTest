using CardGame.Core.Battle;
using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.Core.Duels;

/// <summary>
/// 1回の対戦。ターン進行・召喚・攻撃のルールを検証し、盤面を更新する(召喚は Duel.Summons.cs、表示形式の変更は Duel.Positions.cs、魔法は Duel.Spells.cs、罠は Duel.Traps.cs、効果モンスターは Duel.MonsterEffects.cs、手札の上限は Duel.HandLimit.cs)。
/// 操作が拒否された場合は例外ではなく <see cref="ActionResult"/> で理由を返す。
/// </summary>
public sealed partial class Duel
{
    private readonly GameRulesOptions _rules;
    private readonly Player[] _players;
    private readonly DuelLog _log;
    private int _currentIndex;

    /// <param name="firstPlayer">先攻プレイヤー。デッキはシャッフル済みで渡す。</param>
    public Duel(Player firstPlayer, Player secondPlayer, GameRulesOptions rules)
    {
        _rules = rules;
        _log = new DuelLog(() => TurnCount);
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
    public IReadOnlyList<DuelLogEntry> Log => _log.Entries;

    public ActionResult AdvancePhase()
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not null) return ActionResult.Fail(ActionError.ResponsePending);
        if (PendingEffectTarget is not null) return ActionResult.Fail(ActionError.EffectTargetPending);

        if (CurrentPhase == Phase.End)
        {
            // 手札が上限を超えていれば、捨て終わるまでターンを終えられない
            if (RequiredDiscardCount > 0) return ActionResult.Fail(ActionError.MustDiscard);
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

    /// <summary>攻撃できるかを判定する(盤面は変更しない)。</summary>
    /// <param name="target">攻撃対象。null は直接攻撃。</param>
    public ActionResult CanAttack(FieldMonster attacker, FieldMonster? target)
    {
        if (IsFinished) return ActionResult.Fail(ActionError.DuelFinished);
        if (PendingResponse is not null) return ActionResult.Fail(ActionError.ResponsePending);
        if (PendingEffectTarget is not null) return ActionResult.Fail(ActionError.EffectTargetPending);
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

    /// <summary>
    /// 攻撃を宣言する。相手が発動できる罠を持っていれば <see cref="PendingResponse"/> を開いて戦闘を保留し、
    /// 相手が罠を発動するか見送った後に戦闘を行う。持っていなければすぐに戦闘を行う。
    /// </summary>
    /// <param name="target">攻撃対象。null は直接攻撃(相手の場にモンスターがいない場合のみ)。</param>
    public ActionResult Attack(FieldMonster attacker, FieldMonster? target)
    {
        var check = CanAttack(attacker, target);
        if (!check.IsSuccess) return check;

        attacker.HasAttacked = true;

        if (target is null)
        {
            _log.Add(DuelLogKind.DirectAttacked, CurrentPlayer, attacker.Card.Name);
        }
        else
        {
            var isFaceDown = target.Position == BattlePosition.FaceDownDefense;
            _log.Add(DuelLogKind.Attacked, CurrentPlayer, attacker.Card.Name, isFaceDown ? null : target.Card.Name, isTargetHidden: isFaceDown);
        }

        if (!TryOpenResponseWindow(TrapTrigger.OpponentAttackDeclared, attacker, target))
        {
            ResolveAttack(attacker, target);
        }

        return ActionResult.Success;
    }

    /// <summary>宣言済みの攻撃の戦闘を行う。</summary>
    private void ResolveAttack(FieldMonster attacker, FieldMonster? target)
    {
        var attackerOwner = CurrentPlayer;
        var targetOwner = OpponentPlayer;

        var isTargetFlipped = target is { Position: BattlePosition.FaceDownDefense };
        if (isTargetFlipped && target is not null)
        {
            _log.Add(DuelLogKind.Flipped, targetOwner, target.Card.Name);
        }

        var outcome = ResolveBattle.Execute(attackerOwner, targetOwner, attacker, target);

        if (outcome.AttackerDestroyed) _log.Add(DuelLogKind.Destroyed, attackerOwner, attacker.Card.Name);
        if (outcome.TargetDestroyed && target is not null) _log.Add(DuelLogKind.Destroyed, targetOwner, target.Card.Name);
        if (outcome.DamageToAttackerOwner > 0) _log.Add(DuelLogKind.Damaged, attackerOwner, amount: outcome.DamageToAttackerOwner);
        if (outcome.DamageToTargetOwner > 0) _log.Add(DuelLogKind.Damaged, targetOwner, amount: outcome.DamageToTargetOwner);

        UpdateWinner();

        // 攻撃されて表になったモンスターのリバース効果は、戦闘の後に発動する(戦闘で破壊されていても発動する)
        if (isTargetFlipped && target is not null)
        {
            TriggerMonsterEffect(target, targetOwner, MonsterEffectTrigger.Flipped);
            ResolveTriggeredEffects();
        }
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
        _log.Add(DuelLogKind.TurnStarted, CurrentPlayer);

        var skipDraw = TurnCount == 1 && _rules.FirstPlayerSkipsFirstDraw;
        if (!skipDraw && !CurrentPlayer.Draw())
        {
            _log.Add(DuelLogKind.DeckOut, CurrentPlayer);
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
                monster.PositionChangedThisTurn = false;
            }
        }

        BeginTurn();
    }

    private void UpdateWinner()
    {
        if (FirstPlayer.LifePoints <= 0) SetWinner(SecondPlayer);
        else if (SecondPlayer.LifePoints <= 0) SetWinner(FirstPlayer);
    }

    /// <summary>勝者を決める。すでに決着がついていれば何もしない(後の処理で勝者が上書きされないように)。</summary>
    private void SetWinner(Player winner)
    {
        if (IsFinished) return;
        Winner = winner;
        _log.Add(DuelLogKind.Won, winner);
    }
}
