namespace CardGame.Core.Players;

/// <summary>
/// 決まった数のマスを持つ領域(モンスターゾーン、魔法・罠ゾーン)。
/// カードが場を離れてもそのマスが空くだけで、他のカードのマスは変わらない。
/// </summary>
public sealed class CardZone<T>
    where T : class
{
    private readonly T?[] _slots;

    public CardZone(int size)
    {
        _slots = new T?[size];
    }

    /// <summary>マス(左から順)。空きマスは null。</summary>
    public IReadOnlyList<T?> Slots => _slots;

    /// <summary>置かれているカード(マスの左から順。空きマスは含まない)。</summary>
    public IReadOnlyList<T> Occupied => [.. _slots.OfType<T>()];

    /// <summary>マスが範囲内かつ空いているか。</summary>
    public bool IsSlotEmpty(int slotIndex) =>
        slotIndex >= 0 && slotIndex < _slots.Length && _slots[slotIndex] is null;

    /// <summary>空いているマスのうち一番左の位置。空きがなければ -1。</summary>
    public int FindEmptySlot() => Array.IndexOf(_slots, null);

    public bool Contains(T item) => Array.IndexOf(_slots, item) >= 0;

    /// <summary>指定したマスに置く。範囲外・使用中なら false。</summary>
    public bool Place(int slotIndex, T item)
    {
        if (!IsSlotEmpty(slotIndex)) return false;
        _slots[slotIndex] = item;
        return true;
    }

    /// <summary>取り除いてマスを空ける。置かれていなければ false。</summary>
    public bool Remove(T item)
    {
        var slotIndex = Array.IndexOf(_slots, item);
        if (slotIndex < 0) return false;
        _slots[slotIndex] = null;
        return true;
    }
}
