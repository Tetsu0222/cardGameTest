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
    private readonly FieldMonster?[] _monsterSlots = new FieldMonster?[MonsterZoneSize];
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
    /// <summary>
    /// モンスターゾーンの5つのマス(左から順)。空きマスは null。
    /// モンスターが場を離れても、他のモンスターのマスは変わらない。
    /// </summary>
    public IReadOnlyList<FieldMonster?> MonsterSlots => _monsterSlots;

    /// <summary>場にいるモンスター(マスの左から順。空きマスは含まない)。</summary>
    public IReadOnlyList<FieldMonster> MonsterZone => [.. _monsterSlots.OfType<FieldMonster>()];

    public IReadOnlyList<CardData> Graveyard => _graveyard;

    /// <summary>デッキの一番上を手札に加える。デッキが空なら false。</summary>
    public bool Draw()
    {
        if (_deck.Count == 0) return false;
        _hand.Add(_deck[0]);
        _deck.RemoveAt(0);
        return true;
    }

    /// <summary>手札のカードを指定したマスに出す。手札の範囲外、またはマスが範囲外・使用中なら null。</summary>
    public FieldMonster? SummonFromHand(int handIndex, BattlePosition position, int slotIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return null;
        if (!IsSlotEmpty(slotIndex)) return null;

        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        return PlaceMonster(card, position, slotIndex);
    }

    /// <summary>
    /// カードを空いているマスのうち一番左に置く(盤面の初期構築用)。空きがなければ null。
    /// </summary>
    public FieldMonster? PlaceMonster(CardData card, BattlePosition position)
    {
        var slot = FindEmptySlot();
        return slot < 0 ? null : PlaceMonster(card, position, slot);
    }

    /// <summary>カードを指定したマスに置く。マスが範囲外・使用中なら null。</summary>
    public FieldMonster? PlaceMonster(CardData card, BattlePosition position, int slotIndex)
    {
        if (!IsSlotEmpty(slotIndex)) return null;

        var monster = new FieldMonster(card, position);
        _monsterSlots[slotIndex] = monster;
        return monster;
    }

    /// <summary>マスが範囲内かつ空いているか。</summary>
    public bool IsSlotEmpty(int slotIndex) =>
        slotIndex >= 0 && slotIndex < MonsterZoneSize && _monsterSlots[slotIndex] is null;

    /// <summary>空いているマスのうち一番左の位置。空きがなければ -1。</summary>
    public int FindEmptySlot() => Array.IndexOf(_monsterSlots, null);

    /// <summary>場のモンスターを墓地に送る。そのマスは空きになり、他のモンスターは動かない。場にいなければ false。</summary>
    public bool DestroyMonster(FieldMonster monster)
    {
        var slot = Array.IndexOf(_monsterSlots, monster);
        if (slot < 0) return false;

        _monsterSlots[slot] = null;
        _graveyard.Add(monster.Card);
        return true;
    }

    public void TakeDamage(int amount) => LifePoints = Math.Max(0, LifePoints - amount);
}
