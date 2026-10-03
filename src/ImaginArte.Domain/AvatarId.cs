namespace ImaginArte.Domain;

public readonly record struct AvatarId
{
    private AvatarId(string value)
    {
        Value = value;
    }

    public static AvatarId Fox { get; } = new("fox");

    public static AvatarId Owl { get; } = new("owl");

    public static AvatarId Deer { get; } = new("deer");

    public static AvatarId Moth { get; } = new("moth");

    public static AvatarId Turtle { get; } = new("turtle");

    public static AvatarId Octopus { get; } = new("octopus");

    public static AvatarId Mushroom { get; } = new("mushroom");

    public static AvatarId Dragon { get; } = new("dragon");

    public static AvatarId Rabbit { get; } = new("rabbit");

    public static AvatarId Raven { get; } = new("raven");

    public static AvatarId Hummingbird { get; } = new("hummingbird");

    public static AvatarId Koi { get; } = new("koi");

    public static AvatarId Lynx { get; } = new("lynx");

    public static AvatarId Swan { get; } = new("swan");

    public static AvatarId Phoenix { get; } = new("phoenix");

    public static AvatarId Snail { get; } = new("snail");

    public static IReadOnlyList<AvatarId> All { get; } = Array.AsReadOnly(
    [
        Fox,
        Owl,
        Deer,
        Moth,
        Turtle,
        Octopus,
        Mushroom,
        Dragon,
        Rabbit,
        Raven,
        Hummingbird,
        Koi,
        Lynx,
        Swan,
        Phoenix,
        Snail
    ]);

    public string Value { get; }

    public bool IsDefined => All.Contains(this);

    public static bool TryParse(string? value, out AvatarId avatarId)
    {
        foreach (var candidate in All)
        {
            if (string.Equals(candidate.Value, value, StringComparison.Ordinal))
            {
                avatarId = candidate;
                return true;
            }
        }

        avatarId = default;
        return false;
    }

    public override string ToString() => Value;
}
