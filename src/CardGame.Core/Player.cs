namespace CardGame.Core;

public sealed class Player
{
    public const int InitialLifePoints = 8000;

    public Player(string name, IEnumerable<CardData> deck)
    {
        Name = name;
        Deck = new List<CardData>(deck);
    }

    public string Name { get; }
    public int LifePoints { get; private set; } = InitialLifePoints;
    public List<CardData> Deck { get; }
    public List<CardData> Hand { get; } = new();
    public List<CardData> Graveyard { get; } = new();

    public bool Draw()
    {
        if (Deck.Count == 0) return false;
        Hand.Add(Deck[0]);
        Deck.RemoveAt(0);
        return true;
    }

    public void TakeDamage(int amount) => LifePoints = Math.Max(0, LifePoints - amount);
}
