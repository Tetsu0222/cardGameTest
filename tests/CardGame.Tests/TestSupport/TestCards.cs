using CardGame.Core.Cards;
using CardGame.Core.Effects;

namespace CardGame.Tests.TestSupport;

/// <summary>テスト用のカード定義ファクトリ。</summary>
internal static class TestCards
{
    public static CardData Monster(int attack = 1000, int defense = 1000, int level = 4) =>
        new(1, $"Monster{attack}/{defense}", CardKind.NormalMonster, level, attack, defense);

    /// <summary>効果を持たない魔法(発動できない)。</summary>
    public static CardData Spell() => new(2, "Spell", CardKind.Spell);

    /// <summary>対象を取らない魔法。</summary>
    public static CardData Spell(params EffectAction[] actions) =>
        new(3, "EffectSpell", CardKind.Spell, Effect: new CardEffect(actions));

    /// <summary>対象のモンスター1体を破壊する魔法。</summary>
    public static CardData DestroyTargetSpell(MonsterSide side = MonsterSide.Opponent) =>
        new(4, "DestroyTargetSpell", CardKind.Spell, Effect: new CardEffect([new DestroyTargetMonsterAction()], side));

    public static CardData Trap() => new(5, "Trap", CardKind.Trap);

    /// <summary>
    /// デッキの水増し用。ドローしても召喚・発動できない(効果のない)魔法カードにして、テスト内容に関与させない。
    /// </summary>
    public static CardData Filler() => new(999, "Filler", CardKind.Spell);
}
