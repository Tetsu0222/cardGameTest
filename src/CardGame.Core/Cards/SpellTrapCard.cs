namespace CardGame.Core.Cards;

/// <summary>魔法・罠ゾーン上のカード(カード定義 + 場での状態)。</summary>
public sealed class SpellTrapCard
{
    public SpellTrapCard(CardData card)
    {
        Card = card;
    }

    public CardData Card { get; }

    /// <summary>表側か。セットした時点では裏側。</summary>
    public bool IsFaceUp { get; internal set; }
}
