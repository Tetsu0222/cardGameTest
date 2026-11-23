using CardGame.Core.Cards;

namespace CardGame.Tests.TestSupport;

/// <summary>テスト用のカード定義ファクトリ。</summary>
internal static class TestCards
{
    public static CardData Monster(int attack = 1000, int defense = 1000, int level = 4) =>
        new(1, $"Monster{attack}/{defense}", CardKind.NormalMonster, level, attack, defense);

    public static CardData Spell() => new(2, "Spell", CardKind.Spell);

    /// <summary>
    /// デッキの水増し用。ドローしても召喚できない(まだ発動もできない)魔法カードにして、テスト内容に関与させない。
    /// </summary>
    public static CardData Filler() => new(999, "Filler", CardKind.Spell);
}
