namespace CardGame.Core.Effects;

/// <summary>指定した側のモンスターをすべて破壊する。</summary>
public sealed record DestroyAllMonstersAction(MonsterSide Side) : EffectAction;
