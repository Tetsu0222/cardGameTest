namespace CardGame.Core.Effects;

/// <summary>宣言された攻撃を無効にする(戦闘を行わない)。<see cref="TrapTrigger.OpponentAttackDeclared"/> の罠でのみ使える。</summary>
public sealed record NegateAttackAction() : EffectAction;
