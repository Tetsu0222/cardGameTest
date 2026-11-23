using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.Core.Players;

/// <summary>
/// プレイヤーの持つ領域(デッキ・手札・場・墓地)とライフ。
/// ルールの検証は <see cref="Duel"/> が行い、ここは領域の操作だけを担う。
/// </summary>
public sealed class Player
{
    public const int MonsterZoneSize = 5;

    private readonly List<CardData> _deck;
    private readonly List<CardData> _hand = [];
    private readonly List<FieldMonster> _monsterZone = [];
    private readonly List<CardData> _graveyard = [];

    public Player(string name, IEnumerable<CardData> deck, GameRulesOptions rules)
    {
        Name = name;
        _deck = [.. deck];
        LifePoints = rules.InitialLifePoints;
    }

    public string Name { get; }
    public int LifePoints { get; private set; }
    public IReadOnlyList<CardData> Deck => _deck;
    public IReadOnlyList<CardData> Hand => _hand;
    public IReadOnlyList<FieldMonster> MonsterZone => _monsterZone;
    public IReadOnlyList<CardData> Graveyard => _graveyard;

    /// <summary>デッキの一番上を手札に加える。デッキが空なら false。</summary>
    public bool Draw()
    {
        if (_deck.Count == 0) return false;
        _hand.Add(_deck[0]);
        _deck.RemoveAt(0);
        return true;
    }

    /// <summary>手札のカードをモンスターゾーンに出す。範囲外・ゾーン満杯なら null。</summary>
    public FieldMonster? SummonFromHand(int handIndex, BattlePosition position)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return null;
        if (_monsterZone.Count >= MonsterZoneSize) return null;

        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        return PlaceMonster(card, position);
    }

    /// <summary>カードを直接モンスターゾーンに置く(召喚処理の内部、および盤面の初期構築用)。</summary>
    public FieldMonster? PlaceMonster(CardData card, BattlePosition position)
    {
        if (_monsterZone.Count >= MonsterZoneSize) return null;

        var monster = new FieldMonster(card, position);
        _monsterZone.Add(monster);
        return monster;
    }

    /// <summary>場のモンスターを墓地に送る。場にいなければ false。</summary>
    public bool DestroyMonster(FieldMonster monster)
    {
        if (!_monsterZone.Remove(monster)) return false;
        _graveyard.Add(monster.Card);
        return true;
    }

    public void TakeDamage(int amount) => LifePoints = Math.Max(0, LifePoints - amount);
}
