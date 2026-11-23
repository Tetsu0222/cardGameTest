namespace CardGame.Core.Cards;

public enum CardKind
{
    NormalMonster,
    EffectMonster,
    Spell,
    Trap,
}

/// <summary>カードの静的定義(データ)。</summary>
public sealed record CardData(
    int Id,
    string Name,
    CardKind Kind,
    int Level = 0,
    int Attack = 0,
    int Defense = 0);
