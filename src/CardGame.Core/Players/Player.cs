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
    public const int SpellTrapZoneSize = 5;

    private readonly List<CardData> _deck;
    private readonly List<CardData> _hand = [];
    private readonly CardZone<FieldMonster> _monsterZone = new(MonsterZoneSize);
    private readonly CardZone<SpellTrapCard> _spellTrapZone = new(SpellTrapZoneSize);
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
    public IReadOnlyList<CardData> Graveyard => _graveyard;

    /// <summary>モンスターゾーンの5つのマス(左から順)。空きマスは null。</summary>
    public IReadOnlyList<FieldMonster?> MonsterSlots => _monsterZone.Slots;

    /// <summary>場にいるモンスター(マスの左から順。空きマスは含まない)。</summary>
    public IReadOnlyList<FieldMonster> MonsterZone => _monsterZone.Occupied;

    /// <summary>魔法・罠ゾーンの5つのマス(左から順)。空きマスは null。</summary>
    public IReadOnlyList<SpellTrapCard?> SpellTrapSlots => _spellTrapZone.Slots;

    /// <summary>魔法・罠ゾーンにあるカード(マスの左から順。空きマスは含まない)。</summary>
    public IReadOnlyList<SpellTrapCard> SpellTrapZone => _spellTrapZone.Occupied;

    /// <summary>デッキの一番上を手札に加える。デッキが空なら false。</summary>
    public bool Draw()
    {
        if (_deck.Count == 0) return false;
        _hand.Add(_deck[0]);
        _deck.RemoveAt(0);
        return true;
    }

    /// <summary>手札からカードを取り出す(発動した魔法など)。範囲外なら null。</summary>
    public CardData? TakeFromHand(int handIndex)
    {
        if (handIndex < 0 || handIndex >= _hand.Count) return null;
        var card = _hand[handIndex];
        _hand.RemoveAt(handIndex);
        return card;
    }

    public void SendToGraveyard(CardData card) => _graveyard.Add(card);

    // ---- モンスターゾーン ----

    /// <summary>手札のカードを指定したマスに出す。手札の範囲外、またはマスが範囲外・使用中なら null。</summary>
    public FieldMonster? SummonFromHand(int handIndex, BattlePosition position, int slotIndex)
    {
        if (!IsSlotEmpty(slotIndex)) return null;
        var card = TakeFromHand(handIndex);
        return card is null ? null : PlaceMonster(card, position, slotIndex);
    }

    /// <summary>カードを空いているマスのうち一番左に置く(盤面の初期構築用)。空きがなければ null。</summary>
    public FieldMonster? PlaceMonster(CardData card, BattlePosition position)
    {
        var slot = FindEmptySlot();
        return slot < 0 ? null : PlaceMonster(card, position, slot);
    }

    /// <summary>カードを指定したマスに置く。マスが範囲外・使用中なら null。</summary>
    public FieldMonster? PlaceMonster(CardData card, BattlePosition position, int slotIndex)
    {
        var monster = new FieldMonster(card, position);
        return _monsterZone.Place(slotIndex, monster) ? monster : null;
    }

    /// <summary>モンスターゾーンのマスが範囲内かつ空いているか。</summary>
    public bool IsSlotEmpty(int slotIndex) => _monsterZone.IsSlotEmpty(slotIndex);

    /// <summary>モンスターゾーンの空いているマスのうち一番左の位置。空きがなければ -1。</summary>
    public int FindEmptySlot() => _monsterZone.FindEmptySlot();

    /// <summary>モンスターがいるマスの位置。場にいなければ -1。</summary>
    public int FindMonsterSlot(FieldMonster monster) => _monsterZone.IndexOf(monster);

    /// <summary>場のモンスターを墓地に送る。そのマスは空きになり、他のモンスターは動かない。場にいなければ false。</summary>
    public bool DestroyMonster(FieldMonster monster) => SendMonsterToGraveyard(monster);

    /// <summary>アドバンス召喚のためにモンスターをリリースする(墓地へ送る)。場にいなければ false。</summary>
    public bool TributeMonster(FieldMonster monster) => SendMonsterToGraveyard(monster);

    // ---- 魔法・罠ゾーン ----

    /// <summary>手札のカードを指定したマスに裏側でセットする。手札の範囲外、またはマスが範囲外・使用中なら null。</summary>
    /// <param name="setTurn">セットしたターン(罠はこのターンには発動できない)。</param>
    public SpellTrapCard? SetSpellTrapFromHand(int handIndex, int slotIndex, int setTurn)
    {
        if (!IsSpellTrapSlotEmpty(slotIndex)) return null;
        var card = TakeFromHand(handIndex);
        return card is null ? null : PlaceSpellTrap(card, slotIndex, setTurn);
    }

    /// <summary>カードを指定したマスに裏側で置く(セット処理の内部、および盤面の初期構築用)。マスが範囲外・使用中なら null。</summary>
    /// <param name="setTurn">セットしたターン。盤面の初期構築では 0(どのターンでも発動できる)。</param>
    public SpellTrapCard? PlaceSpellTrap(CardData card, int slotIndex, int setTurn = 0)
    {
        var setCard = new SpellTrapCard(card, setTurn);
        return _spellTrapZone.Place(slotIndex, setCard) ? setCard : null;
    }

    /// <summary>魔法・罠ゾーンのマスが範囲内かつ空いているか。</summary>
    public bool IsSpellTrapSlotEmpty(int slotIndex) => _spellTrapZone.IsSlotEmpty(slotIndex);

    /// <summary>魔法・罠ゾーンの空いているマスのうち一番左の位置。空きがなければ -1。</summary>
    public int FindEmptySpellTrapSlot() => _spellTrapZone.FindEmptySlot();

    /// <summary>魔法・罠ゾーンのカードを墓地に送る。場になければ false。</summary>
    public bool SendSpellTrapToGraveyard(SpellTrapCard setCard)
    {
        if (!_spellTrapZone.Remove(setCard)) return false;
        _graveyard.Add(setCard.Card);
        return true;
    }

    // ---- ライフ ----

    public void TakeDamage(int amount) => LifePoints = Math.Max(0, LifePoints - amount);

    public void GainLifePoints(int amount) => LifePoints += amount;

    private bool SendMonsterToGraveyard(FieldMonster monster)
    {
        if (!_monsterZone.Remove(monster)) return false;
        _graveyard.Add(monster.Card);
        return true;
    }
}
