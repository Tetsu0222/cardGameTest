namespace CardGame.Core.Effects;

/// <summary>効果を処理した結果のうち、効果の外の処理(戦闘など)に影響するもの。</summary>
/// <param name="IsAttackNegated">攻撃が無効にされたか。</param>
public sealed record EffectOutcome(bool IsAttackNegated)
{
    public static EffectOutcome None { get; } = new(IsAttackNegated: false);
}
