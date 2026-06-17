namespace ImagoraGame.Domain;

public sealed class Player
{
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
}
