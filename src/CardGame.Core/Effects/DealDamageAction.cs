namespace CardGame.Core.Effects;

/// <summary>相手プレイヤーにダメージを与える。</summary>
public sealed record DealDamageAction(int Amount) : EffectAction;
