using CardGame.Core.Effects;

namespace CardGame.Web.Features.CardEditor;

/// <summary>カード作成画面で編集中の効果部品1つ(フォームの入力値)。</summary>
public sealed class EffectPartDraft
{
    public EffectPartType Type { get; set; }

    /// <summary>ドロー枚数・ダメージ量・回復量。数値を持たない部品では使わない。</summary>
    public int Value { get; set; }

    /// <summary>全体破壊の対象の側。</summary>
    public MonsterSide Side { get; set; } = MonsterSide.Opponent;

    /// <summary>全体破壊で、攻撃表示のモンスターだけを破壊するか。</summary>
    public bool AttackPositionOnly { get; set; }

    public EffectAction ToAction() => Type switch
    {
        EffectPartType.DrawCards => new DrawCardsAction(Value),
        EffectPartType.DealDamage => new DealDamageAction(Value),
        EffectPartType.GainLifePoints => new GainLifePointsAction(Value),
        EffectPartType.DestroyTargetMonster => new DestroyTargetMonsterAction(),
        EffectPartType.DestroyAllMonsters => new DestroyAllMonstersAction(Side, AttackPositionOnly),
        EffectPartType.DestroyAttackingMonster => new DestroyAttackingMonsterAction(),
        EffectPartType.NegateAttack => new NegateAttackAction(),
        EffectPartType.DestroySummonedMonster => new DestroySummonedMonsterAction(),
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown effect part type."),
    };

    public static EffectPartDraft FromAction(EffectAction action) => action switch
    {
        DrawCardsAction draw => new() { Type = EffectPartType.DrawCards, Value = draw.Count },
        DealDamageAction damage => new() { Type = EffectPartType.DealDamage, Value = damage.Amount },
        GainLifePointsAction gain => new() { Type = EffectPartType.GainLifePoints, Value = gain.Amount },
        DestroyTargetMonsterAction => new() { Type = EffectPartType.DestroyTargetMonster },
        DestroyAllMonstersAction all => new() { Type = EffectPartType.DestroyAllMonsters, Side = all.Side, AttackPositionOnly = all.AttackPositionOnly },
        DestroyAttackingMonsterAction => new() { Type = EffectPartType.DestroyAttackingMonster },
        NegateAttackAction => new() { Type = EffectPartType.NegateAttack },
        DestroySummonedMonsterAction => new() { Type = EffectPartType.DestroySummonedMonster },
        _ => throw new ArgumentOutOfRangeException(nameof(action), action.GetType().Name, "Unknown effect action."),
    };
}
