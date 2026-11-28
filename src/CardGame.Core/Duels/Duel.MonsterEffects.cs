using CardGame.Core.Cards;
using CardGame.Core.Effects;
using CardGame.Core.Players;

namespace CardGame.Core.Duels;

/// <summary>
/// 効果モンスターの効果(リバース・召喚成功時)。発動は強制で、罠で妨害はできない(チェーンは未対応)。
/// <list type="bullet">
/// <item>発動した効果は順番待ちに入り、罠を発動するかを決める機会(<see cref="PendingResponse"/>)が閉じてから処理する。</item>
/// <item>対象を取る効果は、効果を持つモンスターのプレイヤーが <see cref="ChooseEffectTarget"/> で対象を選ぶまで止まる
/// (<see cref="PendingEffectTarget"/>)。対象がいなければ、効果は発動しない。</item>
/// <item>効果を持つモンスターが場を離れていても効果は処理する(攻撃されて表になり、戦闘で破壊された場合など)。</item>
/// </list>
/// </summary>
public sealed partial class Duel
{
    private readonly Queue<PendingMonsterEffect> _triggeredEffects = new();

    /// <summary>対象の選択を待っている効果。待っていなければ null。</summary>
    public PendingMonsterEffect? PendingEffectTarget { get; private set; }

    /// <summary>対象を選んでいる効果で、対象にできるモンスター。</summary>
    public IReadOnlyList<FieldMonster> FindEffectTargetChoices() =>
        PendingEffectTarget is { } pending
            ? FindEffectTargets.Execute(pending.Effect, pending.Controller, OpponentOf(pending.Controller))
            : [];

    /// <summary>対象を選んで、待っていた効果を処理する。その後、順番待ちの効果があれば続けて処理する。</summary>
    public ActionResult ChooseEffectTarget(FieldMonster target)
    {
        if (PendingEffectTarget is not { } pending) return ActionResult.Fail(ActionError.NoPendingEffect);
        if (!FindEffectTargetChoices().Contains(target)) return ActionResult.Fail(ActionError.InvalidTarget);

        PendingEffectTarget = null;
        ResolveMonsterEffect(pending, target);
        ResolveTriggeredEffects();
        return ActionResult.Success;
    }

    /// <summary>モンスターが指定したタイミングの効果を持っていれば、順番待ちに入れる。</summary>
    private void TriggerMonsterEffect(FieldMonster monster, Player controller, MonsterEffectTrigger trigger)
    {
        if (monster.Card is { Kind: CardKind.EffectMonster, Effect: { } effect } && effect.MonsterTrigger == trigger)
        {
            _triggeredEffects.Enqueue(new PendingMonsterEffect(controller, monster, effect));
        }
    }

    /// <summary>
    /// 順番待ちの効果を順に処理する。罠の機会が開いている間、または対象の選択を待っている間は止める
    /// (それぞれが閉じたときに、もう一度呼ぶ)。
    /// </summary>
    private void ResolveTriggeredEffects()
    {
        while (!IsFinished && PendingResponse is null && PendingEffectTarget is null
               && _triggeredEffects.TryDequeue(out var effect))
        {
            var opponent = OpponentOf(effect.Controller);
            var condition = CheckEffectActivation.ExecuteBeforeTargeting(effect.Effect, effect.Controller, opponent);
            if (condition != ActionError.None)
            {
                // 対象がいない、デッキが足りないなど、発動できる状態でなければ何もしない
                _log.Add(DuelLogKind.MonsterEffectFizzled, effect.Controller, effect.Source.Card.Name);
                continue;
            }

            if (effect.Effect.TargetSide is not null)
            {
                PendingEffectTarget = effect;
                return;
            }

            ResolveMonsterEffect(effect, target: null);
        }

        if (IsFinished) _triggeredEffects.Clear();
    }

    private void ResolveMonsterEffect(PendingMonsterEffect effect, FieldMonster? target)
    {
        var isTargetHidden = target?.Position == BattlePosition.FaceDownDefense;
        _log.Add(DuelLogKind.MonsterEffectActivated, effect.Controller, effect.Source.Card.Name,
            target is null || isTargetHidden ? null : target.Card.Name, isTargetHidden: isTargetHidden);

        ResolveCardEffect.Execute(effect.Effect, effect.Controller, OpponentOf(effect.Controller), target, _log);
        UpdateWinner();
    }

    private Player OpponentOf(Player player) => player == FirstPlayer ? SecondPlayer : FirstPlayer;
}
