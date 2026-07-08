namespace ImaginArte.Domain;

public readonly record struct VotingCardId
{
    public VotingCardId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Voting card id is required.", nameof(value));
        }

        Value = value.Trim().ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
