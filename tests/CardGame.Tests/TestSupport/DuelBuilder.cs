using CardGame.Core.Cards;
using CardGame.Core.Duels;
using CardGame.Core.Players;

namespace CardGame.Tests.TestSupport;

internal enum Side
{
    First,
    Second,
}

/// <summary>
/// テスト用に任意の盤面から対戦を開始する。
/// 既定では初期手札 0 枚・デッキ 10 枚(Filler)で、指定した手札・場のモンスターだけが存在する。
/// </summary>
internal sealed class DuelBuilder
{
    private const int DefaultDeckSize = 10;

    private GameRulesOptions _rules = new() { InitialHandSize = 0 };
    private readonly List<CardData>[] _hands = [[], []];
    private readonly List<(CardData Card, BattlePosition Position)>[] _fields = [[], []];
    private readonly List<CardData>[] _setSpellTraps = [[], []];
    private readonly List<CardData>[] _deckTops = [[], []];
    private readonly int[] _deckSizes = [DefaultDeckSize, DefaultDeckSize];

    public static DuelBuilder Create() => new();

    public DuelBuilder WithRules(GameRulesOptions rules)
    {
        _rules = rules;
        return this;
    }

    public DuelBuilder WithHand(Side side, params CardData[] cards)
    {
        _hands[(int)side].AddRange(cards);
        return this;
    }

    public DuelBuilder WithFieldMonster(
        Side side,
        CardData card,
        BattlePosition position = BattlePosition.FaceUpAttack)
    {
        _fields[(int)side].Add((card, position));
        return this;
    }

    /// <summary>魔法・罠ゾーンの空いているマスのうち一番左に裏側で置く。</summary>
    public DuelBuilder WithSetSpellTrap(Side side, CardData card)
    {
        _setSpellTraps[(int)side].Add(card);
        return this;
    }

    /// <summary>デッキの一番上に積むカード(先頭から順に引かれる)。手札の指定より後、水増し用カードより前に置く。</summary>
    public DuelBuilder WithDeckTop(Side side, params CardData[] cards)
    {
        _deckTops[(int)side].AddRange(cards);
        return this;
    }

    public DuelBuilder WithDeckSize(Side side, int size)
    {
        _deckSizes[(int)side] = size;
        return this;
    }

    public Duel Build()
    {
        var first = CreatePlayer(Side.First);
        var second = CreatePlayer(Side.Second);
        return new Duel(first, second, _rules);
    }

    private Player CreatePlayer(Side side)
    {
        var index = (int)side;
        var hand = _hands[index];
        var deck = hand.Concat(_deckTops[index]).Concat(Enumerable.Repeat(TestCards.Filler(), _deckSizes[index]));
        var player = new Player(side.ToString(), deck, _rules);

        // 指定した手札はデッキの先頭に積んであるので、その枚数だけ引く
        foreach (var _ in hand)
        {
            player.Draw();
        }

        foreach (var (card, position) in _fields[index])
        {
            player.PlaceMonster(card, position);
        }

        foreach (var card in _setSpellTraps[index])
        {
            player.PlaceSpellTrap(card, player.FindEmptySpellTrapSlot());
        }

        return player;
    }
}
