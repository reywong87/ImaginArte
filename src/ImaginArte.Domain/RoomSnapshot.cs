namespace ImaginArte.Domain;

public sealed record RoomSnapshot(
    RoomCode Code,
    GamePhase Phase,
    int TargetScore,
    PlayerId? CurrentStorytellerId,
    CardId? StorytellerCardId,
    string? Clue,
    IReadOnlyCollection<PlayerSnapshot> Players,
    IReadOnlyCollection<Card> AvailableDeck,
    IReadOnlyCollection<Card> DiscardPile,
    IReadOnlyCollection<GuessCardSubmission> GuessCardSubmissions,
    IReadOnlyCollection<VotingCardOwnership> VotingCardOwnerships,
    IReadOnlyCollection<Vote> Votes,
    IReadOnlyCollection<PlayerScore> Scoreboard,
    IReadOnlyCollection<PlayerRoundScore> RoundScores);

public sealed record PlayerSnapshot(
    PlayerId Id,
    string Name,
    AvatarId AvatarId,
    IReadOnlyCollection<Card> Hand);
