using CardGame.Core.Cards;
using CardGame.Core.Players;

namespace CardGame.Core.Effects;

/// <summary>効果の対象にできるモンスターを列挙する。対象を取らない効果なら空。</summary>
public static class FindEffectTargets
{
    public static IReadOnlyList<FieldMonster> Execute(CardEffect effect, Player controller, Player opponent) =>
        effect.TargetSide switch
        {
            MonsterSide.Opponent => opponent.MonsterZone,
            MonsterSide.Own => controller.MonsterZone,
            MonsterSide.Both => [.. controller.MonsterZone, .. opponent.MonsterZone],
            _ => [],
        };
}
