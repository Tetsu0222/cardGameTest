using CardGame.Core.Players;

namespace CardGame.Core.Duels;

/// <summary>対戦ログの記録先。現在のターン数を付けて記録する。</summary>
public sealed class DuelLog
{
    private readonly List<DuelLogEntry> _entries = [];
    private readonly Func<int> _currentTurn;

    /// <param name="currentTurn">記録時点のターン数を返す関数。</param>
    public DuelLog(Func<int> currentTurn)
    {
        _currentTurn = currentTurn;
    }

    /// <summary>記録された出来事(古い順)。</summary>
    public IReadOnlyList<DuelLogEntry> Entries => _entries;

    public void Add(
        DuelLogKind kind,
        Player player,
        string? cardName = null,
        string? targetCardName = null,
        int amount = 0,
        bool isTargetHidden = false)
    {
        _entries.Add(new DuelLogEntry(_currentTurn(), kind, player.Name, cardName, targetCardName, amount, isTargetHidden));
    }
}
