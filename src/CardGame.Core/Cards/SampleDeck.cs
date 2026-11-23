using CardGame.Core.Effects;

namespace CardGame.Core.Cards;

/// <summary>
/// 動作確認用の仮デッキ(40枚)。通常モンスター30枚(うちアドバンス召喚が必要な上級モンスター4枚)と通常魔法10枚。
/// カード名はすべてオリジナル。魔法の効果は効果部品の組み合わせで定義している。
/// </summary>
public static class SampleDeck
{
    private static readonly (CardData Card, int Copies)[] Entries =
    [
        (Monster(1, "ブレイズ・ウルフ", 4, 1800, 1000), 3),
        (Monster(2, "アイアン・ナイト", 4, 1600, 1500), 3),
        (Monster(3, "ストーン・ガーディアン", 4, 800, 2000), 3),
        (Monster(4, "スカイ・ハンター", 4, 1500, 1200), 3),
        (Monster(5, "フォレスト・エルフ", 3, 1200, 1400), 2),
        (Monster(6, "シャドウ・ラット", 2, 900, 600), 2),
        (Monster(7, "ゴブリン突撃兵", 3, 1300, 700), 2),
        (Monster(8, "クリスタル・ゴーレム", 4, 1400, 1800), 3),
        (Monster(9, "サンダー・バード", 4, 1700, 800), 3),
        (Monster(10, "アクア・サーペント", 3, 1100, 1300), 2),
        (Monster(11, "ドラゴン・ナイト", 5, 2300, 1800), 2),
        (Monster(12, "ギガント・ゴーレム", 6, 2400, 2600), 1),
        (Monster(13, "エンシェント・ドラゴン", 7, 2800, 2400), 1),

        (Spell(101, "叡智の書", new CardEffect([new DrawCardsAction(2)])), 1),
        (Spell(102, "火炎弾", new CardEffect([new DealDamageAction(600)])), 2),
        (Spell(103, "癒しの泉", new CardEffect([new GainLifePointsAction(1000)])), 2),
        (Spell(104, "ブレイク・ショット", new CardEffect([new DestroyTargetMonsterAction()], MonsterSide.Opponent)), 2),
        (Spell(105, "虚無の渦", new CardEffect([new DestroyAllMonstersAction(MonsterSide.Both)])), 1),
        (Spell(106, "炎の書", new CardEffect([new DealDamageAction(300), new DrawCardsAction(1)])), 2),
    ];

    /// <summary>シャッフル前のデッキ(同名カードが連続して並ぶ)。</summary>
    public static IReadOnlyList<CardData> Create() =>
        [.. Entries.SelectMany(entry => Enumerable.Repeat(entry.Card, entry.Copies))];

    private static CardData Monster(int id, string name, int level, int attack, int defense) =>
        new(id, name, CardKind.NormalMonster, level, attack, defense);

    private static CardData Spell(int id, string name, CardEffect effect) =>
        new(id, name, CardKind.Spell, Effect: effect);
}
