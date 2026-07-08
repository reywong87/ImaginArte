namespace ImaginArte.Domain;

public sealed record PlayerRoundScore(
    PlayerId PlayerId,
    string PlayerName,
    int PointsAwarded);
