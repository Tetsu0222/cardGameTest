namespace CardGame.Core.Cards;

public enum BattlePosition
{
    FaceUpAttack,
    FaceUpDefense,
    FaceDownDefense,
}

/// <summary>モンスターゾーン上のモンスター(カード定義 + 場での状態)。</summary>
public sealed class FieldMonster
{
    /// <param name="summonedTurn">場に出たターン。そのターンには表示形式を変更できない。盤面の初期構築では 0。</param>
    public FieldMonster(CardData card, BattlePosition position, int summonedTurn = 0)
    {
        Card = card;
        Position = position;
        SummonedTurn = summonedTurn;
    }

    public CardData Card { get; }
    public BattlePosition Position { get; internal set; }

    /// <summary>場に出た(召喚・セットした)ターン。</summary>
    public int SummonedTurn { get; }

    public bool HasAttacked { get; internal set; }

    /// <summary>このターンに表示形式を変更したか(反転召喚を含む)。</summary>
    public bool PositionChangedThisTurn { get; internal set; }
}
