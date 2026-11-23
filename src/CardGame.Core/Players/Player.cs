using CardGame.Core.Cards;
using CardGame.Core.Duels;

namespace CardGame.Core.Players;

public sealed class Player
{
    private readonly List<CardData> _deck;
    private readonly List<CardData> _hand = [];
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

    /// <summary>デッキの一番上を手札に加える。デッキが空なら false。</summary>
    public bool Draw()
    {
        if (_deck.Count == 0) return false;
        _hand.Add(_deck[0]);
        _deck.RemoveAt(0);
        return true;
    }

    public void TakeDamage(int amount) => LifePoints = Math.Max(0, LifePoints - amount);
}
