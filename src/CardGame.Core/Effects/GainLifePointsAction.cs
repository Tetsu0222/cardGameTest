namespace CardGame.Core.Effects;

/// <summary>効果を使ったプレイヤーのライフを回復する。</summary>
public sealed record GainLifePointsAction(int Amount) : EffectAction;
