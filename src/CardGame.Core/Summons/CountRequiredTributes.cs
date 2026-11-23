using CardGame.Core.Cards;

namespace CardGame.Core.Summons;

/// <summary>通常召喚に必要なリリースの数。レベル4以下は0体、5・6は1体、7以上は2体。</summary>
public static class CountRequiredTributes
{
    private const int MaxLevelWithoutTribute = 4;
    private const int MaxLevelWithOneTribute = 6;

    public static int Execute(CardData card) => card.Level switch
    {
        <= MaxLevelWithoutTribute => 0,
        <= MaxLevelWithOneTribute => 1,
        _ => 2,
    };
}
