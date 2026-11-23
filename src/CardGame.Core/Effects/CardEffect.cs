namespace CardGame.Core.Effects;

/// <summary>効果の対象になるモンスターの側(効果を使うプレイヤーから見て)。</summary>
public enum MonsterSide
{
    Opponent,
    Own,
    Both,
}

/// <summary>
/// カードの効果。処理はコードで実装した効果部品(<see cref="EffectAction"/>)で行い、
/// カードごとの違いは「どの部品をどの数値で、どの順に並べるか」というデータで表す。
/// </summary>
/// <param name="Actions">上から順に処理する効果部品。</param>
/// <param name="TargetSide">対象を取る場合、対象にできるモンスターの側。対象を取らない効果は null。</param>
public sealed record CardEffect(IReadOnlyList<EffectAction> Actions, MonsterSide? TargetSide = null);
