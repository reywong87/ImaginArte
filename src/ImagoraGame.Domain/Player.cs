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

    internal Card RemoveCard(CardId cardId)
    {
        var cardIndex = _hand.FindIndex(card => card.Id == cardId);

        if (cardIndex < 0)
        {
            throw new InvalidOperationException("The card to discard must belong to the player.");
        }

        var card = _hand[cardIndex];
        _hand.RemoveAt(cardIndex);

        return card;
    }

    internal void AddCard(Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (_hand.Any(existingCard => existingCard.Id == card.Id))
        {
            throw new InvalidOperationException("A player's hand cannot contain duplicate cards.");
        }

        _hand.Add(card);
    }
}
