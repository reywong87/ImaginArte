namespace ImagoraGame.Domain;

public sealed class Player
{
    private readonly List<Card> _hand = [];

    public Player(PlayerId id, string name)
    {
        if (id.Value == Guid.Empty)
        {
            throw new ArgumentException("Player id cannot be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Player name is required.", nameof(name));
        }

        Id = id;
        Name = name.Trim();
    }

    public PlayerId Id { get; }

    public string Name { get; }

    internal IReadOnlyCollection<Card> Hand => _hand.AsReadOnly();

    internal void DealInitialHand(IEnumerable<Card> cards)
    {
        _hand.Clear();
        _hand.AddRange(cards);
    }
}
