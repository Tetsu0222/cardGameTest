namespace CardGame.Core.Cards;

/// <summary>
/// 動作確認用の仮デッキ(40枚)。現在のルールで使える、生け贄不要の通常モンスターのみで構成する。
/// カード名はすべてオリジナル。
/// </summary>
public static class SampleDeck
{
    private static readonly (CardData Card, int Copies)[] Entries =
    [
        (Monster(1, "ブレイズ・ウルフ", 4, 1800, 1000), 3),
        (Monster(2, "アイアン・ナイト", 4, 1600, 1500), 3),
        (Monster(3, "ストーン・ガーディアン", 4, 800, 2000), 3),
        (Monster(4, "スカイ・ハンター", 4, 1500, 1200), 3),
        (Monster(5, "フォレスト・エルフ", 3, 1200, 1400), 3),
        (Monster(6, "シャドウ・ラット", 2, 900, 600), 3),
        (Monster(7, "ゴブリン突撃兵", 3, 1300, 700), 3),
        (Monster(8, "クリスタル・ゴーレム", 4, 1400, 1800), 3),
        (Monster(9, "サンダー・バード", 4, 1700, 800), 3),
        (Monster(10, "アクア・サーペント", 3, 1100, 1300), 3),
        (Monster(11, "ナイト・バット", 2, 700, 500), 3),
        (Monster(12, "ロック・タートル", 3, 500, 1900), 3),
        (Monster(13, "フレイム・ソルジャー", 4, 1650, 1100), 2),
        (Monster(14, "ミスト・ウィザード", 3, 1000, 1600), 2),
    ];

    /// <summary>シャッフル前のデッキ(同名カードが連続して並ぶ)。</summary>
    public static IReadOnlyList<CardData> Create() =>
        [.. Entries.SelectMany(entry => Enumerable.Repeat(entry.Card, entry.Copies))];

    private static CardData Monster(int id, string name, int level, int attack, int defense) =>
        new(id, name, CardKind.NormalMonster, level, attack, defense);
}
