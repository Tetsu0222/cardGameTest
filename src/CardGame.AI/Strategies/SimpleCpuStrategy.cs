using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.AI.Strategies;

/// <summary>
/// 単純なルールベースの CPU。
/// <list type="bullet">
/// <item>メインフェーズ1の最初に、損をしない魔法を手札から発動する(全体破壊は相手のモンスターの方が多いときだけ)。対象を取る魔法は、相手の見えているモンスターのうち最も攻撃力の高いものを狙う。</item>
/// <item>相手の攻撃表示モンスター以上の攻撃力があれば、最も攻撃力の高いモンスターを召喚する。なければ最も守備力の高いモンスターをセットする。</item>
/// <item>戦闘で一方的に破壊できる相手にだけ攻撃する(相打ちや返り討ちは避ける)。相手の場が空なら直接攻撃する。</item>
/// </list>
/// 裏側守備モンスターの守備力は見えないため、<see cref="AssumedFaceDownDefense"/> と仮定する。
/// </summary>
public sealed class SimpleCpuStrategy
{
    public const int AssumedFaceDownDefense = 1500;

    // ドローで手札が増えて再び発動できることがあるため繰り返すが、念のため上限を設ける
    private const int MaxSpellActivationsPerTurn = 10;

    /// <summary>現在のターンプレイヤーとして1ターンを最後まで行い、相手のターンに渡す。</summary>
    public void PlayTurn(Duel duel)
    {
        var me = duel.CurrentPlayer;

        AdvanceTo(duel, me, Phase.Main1);
        if (!IsStillMyTurn(duel, me)) return;
        UseSpells(duel);
        if (!IsStillMyTurn(duel, me)) return;
        TrySummon(duel);

        AdvanceTo(duel, me, Phase.Battle);
        if (!IsStillMyTurn(duel, me)) return;
        AttackWithAll(duel);

        while (IsStillMyTurn(duel, me))
        {
            duel.AdvancePhase();
        }
    }

    private static bool IsStillMyTurn(Duel duel, Player me) => !duel.IsFinished && duel.CurrentPlayer == me;

    private static void AdvanceTo(Duel duel, Player me, Phase phase)
    {
        while (IsStillMyTurn(duel, me) && duel.CurrentPhase != phase)
        {
            duel.AdvancePhase();
        }
    }

    private static void UseSpells(Duel duel)
    {
        for (var i = 0; i < MaxSpellActivationsPerTurn; i++)
        {
            if (duel.IsFinished || !TryActivateOneSpell(duel)) return;
        }
    }

    private static bool TryActivateOneSpell(Duel duel)
    {
        var hand = duel.CurrentPlayer.Hand;
        for (var i = 0; i < hand.Count; i++)
        {
            var card = hand[i];
            if (card.Effect is not { } effect) continue;
            if (!duel.CanActivateSpellFromHand(i).IsSuccess) continue;
            if (!IsWorthActivating(duel, effect)) continue;

            var target = effect.TargetSide is null ? null : ChooseSpellTarget(duel, card);
            if (effect.TargetSide is not null && target is null) continue;

            if (duel.ActivateSpellFromHand(i, target).IsSuccess) return true;
        }

        return false;
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

    private static void TrySummon(Duel duel)
    {
        var hand = duel.CurrentPlayer.Hand;
        var candidates = Enumerable.Range(0, hand.Count)
            .Where(i => duel.CanSummon(i, BattlePosition.FaceUpAttack).IsSuccess)
            .ToList();
        if (candidates.Count == 0) return;

        var opponentBestAttack = duel.OpponentPlayer.MonsterZone
            .Where(m => m.Position == BattlePosition.FaceUpAttack)
            .Select(m => m.Card.Attack)
            .DefaultIfEmpty(0)
            .Max();

        var strongest = candidates.MaxBy(i => hand[i].Attack);
        if (hand[strongest].Attack >= opponentBestAttack)
        {
            duel.Summon(strongest, BattlePosition.FaceUpAttack);
            return;
        }

        var sturdiest = candidates.MaxBy(i => hand[i].Defense);
        duel.Summon(sturdiest, BattlePosition.FaceDownDefense);
    }

    private static void AttackWithAll(Duel duel)
    {
        // 戦闘で場のモンスターが減るため、開始時点の一覧を複製して回す
        foreach (var attacker in duel.CurrentPlayer.MonsterZone.ToList())
        {
            if (duel.IsFinished) return;

            if (duel.CanAttack(attacker, target: null).IsSuccess)
            {
                duel.Attack(attacker, target: null);
                continue;
            }

            var target = duel.OpponentPlayer.MonsterZone
                .Where(t => duel.CanAttack(attacker, t).IsSuccess && CanDestroyWithoutLoss(attacker, t))
                .MaxBy(t => t.Card.Attack);

            if (target is not null)
            {
                duel.Attack(attacker, target);
            }
        }
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
