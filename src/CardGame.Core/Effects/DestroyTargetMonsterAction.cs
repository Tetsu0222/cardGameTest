namespace CardGame.Core.Effects;

/// <summary>
/// 対象のモンスターを破壊する。対象にできる側は <see cref="CardEffect.TargetSide"/> で決める。
/// 処理時に対象がすでに場を離れていれば何もしない。
/// </summary>
public sealed record DestroyTargetMonsterAction() : EffectAction;
