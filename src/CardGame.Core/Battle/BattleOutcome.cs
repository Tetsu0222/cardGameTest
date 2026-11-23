namespace CardGame.Core.Battle;

/// <summary>戦闘の結果(どちらが破壊され、誰がいくらダメージを受けるか)。</summary>
public sealed record BattleOutcome(
    bool AttackerDestroyed,
    bool TargetDestroyed,
    int DamageToAttackerOwner,
    int DamageToTargetOwner);
