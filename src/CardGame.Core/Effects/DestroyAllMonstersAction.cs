namespace CardGame.Core.Effects;

/// <summary>指定した側のモンスターをすべて破壊する。</summary>
/// <param name="AttackPositionOnly">true なら攻撃表示のモンスターだけを破壊する。</param>
public sealed record DestroyAllMonstersAction(MonsterSide Side, bool AttackPositionOnly = false) : EffectAction;
