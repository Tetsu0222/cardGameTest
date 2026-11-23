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
    public FieldMonster(CardData card, BattlePosition position)
    {
        Card = card;
        Position = position;
    }

    public CardData Card { get; }
    public BattlePosition Position { get; internal set; }
    public bool HasAttacked { get; internal set; }
}
