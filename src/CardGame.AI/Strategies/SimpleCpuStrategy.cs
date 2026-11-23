using CardGame.Core.Battle;
using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Effects;
using CardGame.Core.Players;
using CardGame.Core.Summons;
using CardGame.Core.Turns;

namespace CardGame.AI.Strategies;

/// <summary>
/// 単純なルールベースの CPU。盤面を見て「次の1手」を決める。
/// <list type="bullet">
/// <item>メインフェーズ1では、まず損をしない魔法を手札から発動する(全体破壊は相手のモンスターの方が多いときだけ)。対象を取る魔法は、相手の見えているモンスターのうち最も攻撃力の高いものを狙う。</item>
/// <item>次に、相手の攻撃表示モンスター以上の攻撃力があれば、最も攻撃力の高いモンスターを召喚する。なければ最も守備力の高いモンスターをセットする。上級モンスターは、攻撃力の低いモンスターから順にリリースし、リリースするどのモンスターよりも強くなる場合だけアドバンス召喚する(アドバンスセットはしない)。</item>
/// <item>メインフェーズ1では、召喚の後、攻撃に回せる守備表示・裏側守備表示のモンスターを攻撃表示にする(裏側は反転召喚)。
/// 攻撃に回せるのは、相手の攻撃表示モンスター以上の攻撃力があり、相手の場が空か一方的に破壊できる相手がいる場合。</item>
/// <item>メインフェーズ2では、相手の攻撃表示モンスターより攻撃力が低く、守備力では耐えられる攻撃表示のモンスターを守備表示にする。</item>
/// <item>メインフェーズでは、召喚の後に手札の罠をすべてセットする。</item>
/// <item>バトルフェーズでは、戦闘で一方的に破壊できる相手にだけ攻撃する(相打ちや返り討ちは避ける)。相手の場が空なら直接攻撃する。</item>
/// <item>相手の攻撃宣言・召喚に対しては、破壊する罠を優先して発動する。攻撃を無効にするだけの罠は、その戦闘で損をするときだけ使う。
/// 召喚されたモンスターを破壊する罠は、攻撃力が <see cref="MinAttackWorthTrapping"/> 以上のときだけ使う。</item>
/// </list>
/// 裏側守備モンスターの守備力は見えないため、<see cref="AssumedFaceDownDefense"/> と仮定する。
/// </summary>
public sealed class SimpleCpuStrategy
{
    public const int AssumedFaceDownDefense = 1500;

    /// <summary>召喚されたモンスターを罠で破壊する価値がある攻撃力の下限。</summary>
    public const int MinAttackWorthTrapping = 1500;

    // 1手ごとに盤面が進む(カードを使う・召喚権を使う・攻撃済みになる・フェーズが進む)ため必ず終わるが、念のため上限を設ける
    private const int MaxStepsPerTurn = 200;

    /// <summary>
    /// 次の1手を決める(盤面は変更しない)。決着がついていれば null。
    /// 罠を発動するかを決める機会が開いていれば、罠を持つ側(ターンプレイヤーの相手)としての1手を返す。
    /// それ以外は、ターンプレイヤーとしての1手を返す。
    /// </summary>
    public CpuAction? DecideNextAction(Duel duel)
    {
        if (duel.IsFinished) return null;
        if (duel.PendingResponse is { } window) return DecideResponse(duel, window);

        CpuAction advance = new CpuAdvancePhaseAction();
        return duel.CurrentPhase switch
        {
            Phase.Main1 => DecideSpell(duel) ?? DecideSummon(duel) ?? DecideOffensivePositionChange(duel) ?? DecideSetTrap(duel) ?? advance,
            Phase.Battle => DecideAttack(duel) ?? advance,
            Phase.Main2 => DecideDefensivePositionChange(duel) ?? DecideSetTrap(duel) ?? advance,
            _ => advance,
        };
    }

    /// <summary>
    /// 決めた1手を実行する。想定外に拒否された場合は、同じ手を繰り返して止まらないよう先へ進める
    /// (罠の機会が開いていれば見送り、そうでなければ次のフェーズへ進む)。
    /// </summary>
    public static ActionResult Execute(Duel duel, CpuAction action)
    {
        var result = action.ExecuteOn(duel);
        if (!result.IsSuccess)
        {
            if (duel.PendingResponse is not null)
            {
                duel.PassResponse();
            }
            else
            {
                duel.AdvancePhase();
            }
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
            .Select(i => (HandIndex: i, Card: hand[i], Tributes: ChooseTributes(duel, hand[i])))
            .Where(c => IsWorthTributing(c.Card, c.Tributes))
            .ToList();
        if (candidates.Count == 0) return null;

        var opponentBestAttack = OpponentBestAttack(duel);

        var strongest = candidates.MaxBy(c => c.Card.Attack);
        if (strongest.Card.Attack >= opponentBestAttack)
        {
            return new CpuSummonAction(strongest.HandIndex, BattlePosition.FaceUpAttack, NullIfEmpty(strongest.Tributes));
        }

        // 守りに回るときは、場のモンスターを減らすアドバンスセットはしない
        var withoutTribute = candidates.Where(c => c.Tributes.Count == 0).ToList();
        if (withoutTribute.Count == 0) return null;

        var sturdiest = withoutTribute.MaxBy(c => c.Card.Defense);
        return new CpuSummonAction(sturdiest.HandIndex, BattlePosition.FaceDownDefense);
    }

    /// <summary>リリースに使うモンスター。攻撃力の低いものから必要な数だけ選ぶ。</summary>
    private static IReadOnlyList<FieldMonster> ChooseTributes(Duel duel, CardData card) =>
        [.. duel.CurrentPlayer.MonsterZone
            .OrderBy(m => m.Card.Attack)
            .Take(CountRequiredTributes.Execute(card))];

    /// <summary>リリースするモンスターのどれよりも攻撃力が高くなる場合だけ、アドバンス召喚する価値がある。</summary>
    private static bool IsWorthTributing(CardData card, IReadOnlyList<FieldMonster> tributes) =>
        tributes.All(t => t.Card.Attack < card.Attack);

    private static IReadOnlyList<FieldMonster>? NullIfEmpty(IReadOnlyList<FieldMonster> tributes) =>
        tributes.Count == 0 ? null : tributes;

    // ---- 表示形式 ----

    /// <summary>攻撃に回せる守備表示・裏側守備表示のモンスターを攻撃表示にする(裏側は反転召喚)。</summary>
    private static CpuAction? DecideOffensivePositionChange(Duel duel)
    {
        var opponentBestAttack = OpponentBestAttack(duel);
        var monster = duel.CurrentPlayer.MonsterZone
            .Where(m => m.Position != BattlePosition.FaceUpAttack && duel.CanChangePosition(m).IsSuccess)
            .Where(m => m.Card.Attack >= opponentBestAttack && CanAttackProfitably(duel, m.Card.Attack))
            .MaxBy(m => m.Card.Attack);

        return monster is null ? null : new CpuChangePositionAction(monster);
    }

    /// <summary>相手の攻撃に耐えられない攻撃表示のモンスターを、守備力で耐えられるなら守備表示にする。</summary>
    private static CpuAction? DecideDefensivePositionChange(Duel duel)
    {
        var opponentBestAttack = OpponentBestAttack(duel);
        var monster = duel.CurrentPlayer.MonsterZone
            .Where(m => m.Position == BattlePosition.FaceUpAttack && duel.CanChangePosition(m).IsSuccess)
            .FirstOrDefault(m => m.Card.Attack < opponentBestAttack && m.Card.Defense >= opponentBestAttack);

        return monster is null ? null : new CpuChangePositionAction(monster);
    }

    /// <summary>相手の場が空か、その攻撃力で一方的に破壊できる相手がいるか。</summary>
    private static bool CanAttackProfitably(Duel duel, int attack) =>
        duel.OpponentPlayer.MonsterZone.Count == 0
        || duel.OpponentPlayer.MonsterZone.Any(t => CanDestroyWithoutLoss(attack, t));

    private static int OpponentBestAttack(Duel duel) =>
        duel.OpponentPlayer.MonsterZone
            .Where(m => m.Position == BattlePosition.FaceUpAttack)
            .Select(m => m.Card.Attack)
            .DefaultIfEmpty(0)
            .Max();

    // ---- 罠 ----

    private static CpuAction? DecideSetTrap(Duel duel)
    {
        var hand = duel.CurrentPlayer.Hand;
        for (var i = 0; i < hand.Count; i++)
        {
            if (hand[i].Kind == CardKind.Trap && duel.CanSetSpellTrap(i).IsSuccess)
            {
                return new CpuSetSpellTrapAction(i);
            }
        }

        return null;
    }

    /// <summary>罠を持つ側として、発動する罠を選ぶ。発動する価値のある罠がなければ見送る。</summary>
    private static CpuAction DecideResponse(Duel duel, ResponseWindow window)
    {
        var trap = duel.FindActivatableTraps()
            .Where(t => t.Card.Effect is { TargetSide: null } effect && IsWorthResponding(effect, window))
            .OrderByDescending(t => DestroysMonster(t.Card.Effect))
            .FirstOrDefault();

        return trap is null ? new CpuPassResponseAction() : new CpuActivateTrapAction(trap, Target: null);
    }

    private static bool IsWorthResponding(CardEffect effect, ResponseWindow window) =>
        effect.Actions.All(action => action switch
        {
            NegateAttackAction => IsAttackHarmful(window),
            DestroySummonedMonsterAction => window.TriggerMonster.Card.Attack >= MinAttackWorthTrapping,
            DestroyAllMonstersAction { Side: MonsterSide.Own or MonsterSide.Both } => false,
            _ => true,
        });

    /// <summary>この攻撃をそのまま受けると、罠を持つ側がダメージを受けるかモンスターを失うか。</summary>
    private static bool IsAttackHarmful(ResponseWindow window)
    {
        var outcome = CalculateBattleOutcome.Execute(window.TriggerMonster.Card.Attack, FaceUpView(window.AttackTarget));
        return outcome.TargetDestroyed || outcome.DamageToTargetOwner > 0;
    }

    /// <summary>自分のモンスターは裏側でも中身が分かるため、表になった状態として戦闘結果を見積もる。</summary>
    private static FieldMonster? FaceUpView(FieldMonster? monster) =>
        monster is { Position: BattlePosition.FaceDownDefense }
            ? new FieldMonster(monster.Card, BattlePosition.FaceUpDefense)
            : monster;

    private static bool DestroysMonster(CardEffect? effect) =>
        effect is not null && effect.Actions.Any(a => a is DestroyAttackingMonsterAction or DestroySummonedMonsterAction or DestroyAllMonstersAction);

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

    private static bool CanDestroyWithoutLoss(FieldMonster attacker, FieldMonster target) =>
        CanDestroyWithoutLoss(attacker.Card.Attack, target);

    private static bool CanDestroyWithoutLoss(int attack, FieldMonster target)
    {
        return target.Position switch
        {
            BattlePosition.FaceUpAttack => attack > target.Card.Attack,
            BattlePosition.FaceUpDefense => attack > target.Card.Defense,
            _ => attack > AssumedFaceDownDefense,
        };
    }
}
