namespace CardGame.Core.Cards;

/// <summary>魔法・罠ゾーン上のカード(カード定義 + 場での状態)。</summary>
public sealed class SpellTrapCard
{
    /// <param name="setTurn">セットしたターン。罠はこのターンには発動できない。</param>
    public SpellTrapCard(CardData card, int setTurn = 0)
    {
        Card = card;
        SetTurn = setTurn;
    }

    public CardData Card { get; }

    /// <summary>セットしたターン。</summary>
    public int SetTurn { get; }

    /// <summary>表側か。セットした時点では裏側。</summary>
    public bool IsFaceUp { get; internal set; }
}
