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
}

/// <summary>
/// 対戦中に起きた出来事。表示用の文言には変換せず、構造化したまま持つ(文言化は UI の責務)。
/// </summary>
/// <param name="PlayerName">出来事の主体となるプレイヤー(攻撃した側、ダメージを受けた側など)。</param>
/// <param name="TargetCardName">攻撃対象のカード名。裏側守備への攻撃宣言時は null。</param>
public sealed record DuelLogEntry(
    int Turn,
    DuelLogKind Kind,
    string PlayerName,
    string? CardName = null,
    string? TargetCardName = null,
    int Amount = 0);
