namespace ImaginArte.Domain;

public sealed class RoomCode : IEquatable<RoomCode>
{
    public RoomCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Room code is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        Value = normalized;
    }

    public string Value { get; }

    public bool Equals(RoomCode? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is RoomCode other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
