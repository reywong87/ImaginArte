namespace ImaginArte.Contracts.Rooms;

public sealed record CreateRoomRequest(string RoomCode);

public sealed record UpdateRoomTargetScoreRequest(int TargetScore);

public sealed record JoinRoomRequest(Guid PlayerId, string PlayerName);

public sealed record ChooseStorytellerCardRequest(
    Guid PlayerId,
    string CardId,
    string Clue);

public sealed record SubmitGuessCardRequest(
    Guid PlayerId,
    string CardId);

public sealed record CastVoteRequest(
    Guid PlayerId,
    string VotingCardId);

public sealed record GetPlayerPrivateStateRequest(string RoomCode, Guid PlayerId);

public sealed record GetPlayerPrivateStateResponse(
    RoomStateResponse Room,
    PlayerPrivateStateResponse Player);

public sealed record PlayerPrivateStateResponse(
    Guid PlayerId,
    string Name,
    IReadOnlyCollection<CardStateResponse> Hand,
    bool IsCurrentStoryteller,
    bool HasSubmittedGuessCard,
    string? SubmittedVotingCardId);

public sealed record CardStateResponse(string CardId);

public sealed record RoomStateResponse(
    string RoomCode,
    RoomPhase Phase,
    int TargetScore,
    IReadOnlyCollection<PlayerStateResponse> Players,
    PlayerStateResponse? CurrentStoryteller,
    string? Clue,
    int SubmissionCount,
    int ExpectedSubmissionCount,
    int VoteCount,
    int ExpectedVoteCount,
    IReadOnlyCollection<VotingCardStateResponse> VotingCards,
    IReadOnlyCollection<RevealCardStateResponse> RevealedCards,
    IReadOnlyCollection<RoundScoreStateResponse> RoundScores,
    IReadOnlyCollection<ScoreboardEntryStateResponse> Scoreboard,
    IReadOnlyCollection<WinningPlayerStateResponse> Winners)
{
    public int MinimumPlayers { get; init; }

    public int MaximumPlayers { get; init; }

    public Guid? HostPlayerId { get; init; }
}

public sealed record PlayerStateResponse(Guid PlayerId, string Name)
{
    public bool IsConnected { get; init; }
}

public sealed record VotingCardStateResponse(
    string VotingCardId,
    string CardId,
    int DisplayNumber,
    IReadOnlyCollection<VotingCardVoteStateResponse> Votes);

public sealed record VotingCardVoteStateResponse(
    Guid VoterPlayerId,
    string VoterName);

public sealed record RevealedVoteStateResponse(
    Guid VoterPlayerId,
    string VoterName);

public sealed record RevealCardStateResponse(
    string VotingCardId,
    string CardId,
    int DisplayNumber,
    Guid OwnerPlayerId,
    string OwnerName,
    bool IsStorytellerCard,
    IReadOnlyCollection<RevealedVoteStateResponse> Votes);

public sealed record RoundScoreStateResponse(
    Guid PlayerId,
    string PlayerName,
    int PointsAwarded);

public sealed record ScoreboardEntryStateResponse(
    Guid PlayerId,
    string PlayerName,
    int TotalPoints);

public sealed record WinningPlayerStateResponse(
    Guid PlayerId,
    string PlayerName,
    int TotalPoints);

public enum RoomPhase
{
    WaitingForPlayers = 0,
    StorytellerChoosing = 1,
    GuessersSubmitting = 2,
    Voting = 3,
    Reveal = 4,
    Scoring = 5,
    RoundComplete = 6,
    GameComplete = 7,
    VoteReveal = 8
}
