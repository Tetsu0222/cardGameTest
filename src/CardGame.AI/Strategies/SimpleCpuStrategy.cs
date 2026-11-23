using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;
using CardGame.Core.Turns;

namespace CardGame.AI.Strategies;

/// <summary>
/// 単純なルールベースの CPU。
/// <list type="bullet">
/// <item>相手の攻撃表示モンスター以上の攻撃力があれば、最も攻撃力の高いモンスターを召喚する。なければ最も守備力の高いモンスターをセットする。</item>
/// <item>戦闘で一方的に破壊できる相手にだけ攻撃する(相打ちや返り討ちは避ける)。相手の場が空なら直接攻撃する。</item>
/// </list>
/// 裏側守備モンスターの守備力は見えないため、<see cref="AssumedFaceDownDefense"/> と仮定する。
/// </summary>
public sealed class SimpleCpuStrategy
{
    public const int AssumedFaceDownDefense = 1500;

    /// <summary>現在のターンプレイヤーとして1ターンを最後まで行い、相手のターンに渡す。</summary>
    public void PlayTurn(Duel duel)
    {
        var me = duel.CurrentPlayer;

        AdvanceTo(duel, me, Phase.Main1);
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
