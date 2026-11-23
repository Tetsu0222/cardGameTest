namespace CardGame.Core.Duels;

/// <summary>プレイヤー操作が拒否された理由。</summary>
public enum ActionError
{
    None,
    DuelFinished,
    WrongPhase,
    InvalidHandIndex,
    NotAMonster,
    InvalidPosition,
    TributeRequired,
    NormalSummonAlreadyUsed,
    MonsterZoneFull,
    CannotAttackOnFirstTurn,
    AttackerNotOnYourField,
    AttackerNotInAttackPosition,
    AlreadyAttacked,
    InvalidTarget,
    DirectAttackNotAllowed,
}

/// <summary>操作の結果。ゲーム上の通常の失敗は例外ではなくこの戻り値で表す。</summary>
public sealed record ActionResult(ActionError Error)
{
    public static ActionResult Success { get; } = new(ActionError.None);

    public bool IsSuccess => Error == ActionError.None;

    public static ActionResult Fail(ActionError error) => new(error);
}
