namespace CardGame.Core.Effects;

/// <summary>効果を使ったプレイヤーがデッキからカードを引く。</summary>
public sealed record DrawCardsAction(int Count) : EffectAction;
