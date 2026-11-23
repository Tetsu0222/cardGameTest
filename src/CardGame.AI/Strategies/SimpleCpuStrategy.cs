using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.AI.Strategies;

/// <summary>
/// 単純なルールベースの CPU。盤面を見て「次の1手」を決める。
/// <list type="bullet">
/// <item>メインフェーズ1では、まず損をしない魔法を手札から発動する(全体破壊は相手のモンスターの方が多いときだけ)。対象を取る魔法は、相手の見えているモンスターのうち最も攻撃力の高いものを狙う。</item>
/// <item>次に、相手の攻撃表示モンスター以上の攻撃力があれば、最も攻撃力の高いモンスターを召喚する。なければ最も守備力の高いモンスターをセットする。</item>
/// <item>バトルフェーズでは、戦闘で一方的に破壊できる相手にだけ攻撃する(相打ちや返り討ちは避ける)。相手の場が空なら直接攻撃する。</item>
/// </list>
/// 裏側守備モンスターの守備力は見えないため、<see cref="AssumedFaceDownDefense"/> と仮定する。
/// </summary>
public sealed class SimpleCpuStrategy
{
    public const int AssumedFaceDownDefense = 1500;

    // 1手ごとに盤面が進む(カードを使う・召喚権を使う・攻撃済みになる・フェーズが進む)ため必ず終わるが、念のため上限を設ける
    private const int MaxStepsPerTurn = 200;

    /// <summary>
    /// 現在のターンプレイヤーとしての次の1手を決める(盤面は変更しない)。決着がついていれば null。
    /// </summary>
    public CpuAction? DecideNextAction(Duel duel)
    {
        if (duel.IsFinished) return null;

        CpuAction advance = new CpuAdvancePhaseAction();
        return duel.CurrentPhase switch
        {
            Phase.Main1 => DecideSpell(duel) ?? DecideSummon(duel) ?? advance,
            Phase.Battle => DecideAttack(duel) ?? advance,
            _ => advance,
        };
    }

    /// <summary>
    /// 決めた1手を実行する。想定外に拒否された場合は、同じ手を繰り返して止まらないよう次のフェーズへ進める。
    /// </summary>
    public static ActionResult Execute(Duel duel, CpuAction action)
    {
        var result = action.ExecuteOn(duel);
        if (!result.IsSuccess)
        {
            duel.AdvancePhase();
        }

        return result;
    }

    /// <summary>現在のターンプレイヤーとして1ターンを最後まで一気に行い、相手のターンに渡す。</summary>
    public void PlayTurn(Duel duel)
    {
        var me = duel.CurrentPlayer;
        for (var step = 0; step < MaxStepsPerTurn && !duel.IsFinished && duel.CurrentPlayer == me; step++)
        {
            var action = DecideNextAction(duel);
            if (action is null) return;
            Execute(duel, action);
        }
    }

    // ---- 魔法 ----

    private static CpuAction? DecideSpell(Duel duel)
    {
        var hand = duel.CurrentPlayer.Hand;
        for (var i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (card.Effect is not { } effect) continue;
            if (!duel.CanActivateSpellFromHand(i).IsSuccess) continue;
            if (!IsWorthActivating(duel, effect)) continue;

            if (effect.TargetSide is null) return new CpuActivateSpellAction(i, Target: null);

            var target = ChooseSpellTarget(duel, card);
            if (target is not null && duel.CanActivateSpellFromHand(i, target).IsSuccess)
            {
                return new CpuActivateSpellAction(i, target);
            }
        }

        return null;
    }

    private static bool IsWorthActivating(Duel duel, CardEffect effect)
    {
        var ownCount = duel.CurrentPlayer.MonsterZone.Count;
        var opponentCount = duel.OpponentPlayer.MonsterZone.Count;

        return effect.Actions.All(action => action switch
        {
            DestroyAllMonstersAction { Side: MonsterSide.Opponent } => opponentCount > 0,
            DestroyAllMonstersAction { Side: MonsterSide.Own } => false,
            DestroyAllMonstersAction => opponentCount > ownCount,
            _ => true,
        });
    }

    /// <summary>相手のモンスターのうち、見えている攻撃力が最も高いものを選ぶ。自分のモンスターは選ばない。</summary>
    private static FieldMonster? ChooseSpellTarget(Duel duel, CardData card) =>
        duel.FindSpellTargets(card)
            .Where(t => duel.OpponentPlayer.MonsterZone.Contains(t))
            .MaxBy(t => t.Position == BattlePosition.FaceDownDefense ? 0 : t.Card.Attack);

    // ---- 召喚 ----

    private static CpuAction? DecideSummon(Duel duel)
    {
        var hand = duel.CurrentPlayer.Hand;
        var candidates = Enumerable.Range(0, hand.Count)
            .Where(i => duel.CanSummon(i, BattlePosition.FaceUpAttack).IsSuccess)
            .ToList();
        if (candidates.Count == 0) return null;

        var opponentBestAttack = duel.OpponentPlayer.MonsterZone
            .Where(m => m.Position == BattlePosition.FaceUpAttack)
            .Select(m => m.Card.Attack)
            .DefaultIfEmpty(0)
            .Max();

        var strongest = candidates.MaxBy(i => hand[i].Attack);
        if (hand[strongest].Attack >= opponentBestAttack)
        {
            return new CpuSummonAction(strongest, BattlePosition.FaceUpAttack);
        }

        var sturdiest = candidates.MaxBy(i => hand[i].Defense);
        return new CpuSummonAction(sturdiest, BattlePosition.FaceDownDefense);
    }

    // ---- 攻撃 ----

    private static CpuAction? DecideAttack(Duel duel)
    {
        foreach (var attacker in duel.CurrentPlayer.MonsterZone)
        {
            if (duel.CanAttack(attacker, target: null).IsSuccess)
            {
                return new CpuAttackAction(attacker, Target: null);
            }

            var target = duel.OpponentPlayer.MonsterZone
                .Where(t => duel.CanAttack(attacker, t).IsSuccess && CanDestroyWithoutLoss(attacker, t))
                .MaxBy(t => t.Card.Attack);

            if (target is not null)
            {
                return new CpuAttackAction(attacker, target);
            }
        }

        return null;
    }

    private static bool CanDestroyWithoutLoss(FieldMonster attacker, FieldMonster target)
    {
        var attack = attacker.Card.Attack;
        return target.Position switch
        {
            BattlePosition.FaceUpAttack => attack > target.Card.Attack,
            BattlePosition.FaceUpDefense => attack > target.Card.Defense,
            _ => attack > AssumedFaceDownDefense,
        };
    }
}
