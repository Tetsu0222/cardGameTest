namespace CardGame.Core.Duels;

public enum DuelLogKind
{
    TurnStarted,
    Summoned,
    Set,
    Attacked,
    DirectAttacked,
    Flipped,
    Damaged,
    Destroyed,
    DeckOut,
    Won,
    Tributed,
    TrapActivated,
    AttackNegated,
    ChangedToAttackPosition,
    ChangedToDefensePosition,
    FlipSummoned,
    Discarded,
    MonsterEffectActivated,
    MonsterEffectFizzled,
    SpellActivated,
    SpellTrapSet,
    Drew,
    LifeGained,
}

/// <summary>
/// 対戦中に起きた出来事。表示用の文言には変換せず、構造化したまま持つ(文言化は UI の責務)。
/// </summary>
/// <param name="PlayerName">出来事の主体となるプレイヤー(攻撃した側、ダメージを受けた側など)。</param>
/// <param name="TargetCardName">攻撃・効果の対象のカード名。裏側のモンスターが対象のときは null。</param>
/// <param name="Amount">ダメージ量・回復量・ドロー枚数など。</param>
/// <param name="IsTargetHidden">対象が裏側のモンスターで、名前を伏せているか(<see cref="TargetCardName"/> は null)。</param>
public sealed record DuelLogEntry(
    int Turn,
    DuelLogKind Kind,
    string PlayerName,
    string? CardName = null,
    string? TargetCardName = null,
    int Amount = 0,
    bool IsTargetHidden = false);
