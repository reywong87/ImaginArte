namespace ImaginArte.Domain;

public sealed record PlayerScore(
    PlayerId PlayerId,
    string PlayerName,
    int TotalPoints);
