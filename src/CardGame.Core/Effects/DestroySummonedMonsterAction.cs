namespace CardGame.Core.Effects;

/// <summary>召喚されたモンスターを破壊する。<see cref="TrapTrigger.OpponentMonsterSummoned"/> の罠でのみ使える。</summary>
public sealed record DestroySummonedMonsterAction() : EffectAction;
