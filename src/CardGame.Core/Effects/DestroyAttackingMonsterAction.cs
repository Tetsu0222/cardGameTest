namespace CardGame.Core.Effects;

/// <summary>攻撃を宣言したモンスターを破壊する。<see cref="TrapTrigger.OpponentAttackDeclared"/> の罠でのみ使える。</summary>
public sealed record DestroyAttackingMonsterAction() : EffectAction;
