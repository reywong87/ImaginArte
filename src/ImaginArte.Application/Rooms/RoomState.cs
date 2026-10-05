using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record PlayerState(Guid PlayerId, string Name, AvatarId AvatarId);

public sealed record VotingCardState(
    string VotingCardId,
    string CardId,
    int DisplayNumber,
    IReadOnlyCollection<VotingCardVoteState> Votes);

public sealed record VotingCardVoteState(
    Guid VoterPlayerId,
    string VoterName);

public sealed record RevealedVoteState(
    Guid VoterPlayerId,
    string VoterName);

public sealed record RevealCardState(
    string VotingCardId,
    string CardId,
    int DisplayNumber,
    Guid OwnerPlayerId,
    string OwnerName,
    bool IsStorytellerCard,
    IReadOnlyCollection<RevealedVoteState> Votes);

public sealed record RoundScoreState(
    Guid PlayerId,
    string PlayerName,
    int PointsAwarded);

public sealed record ScoreboardEntryState(
    Guid PlayerId,
    string PlayerName,
    int TotalPoints);

public sealed record WinningPlayerState(
    Guid PlayerId,
    string PlayerName,
    int TotalPoints);

public sealed record RoomState(
    string RoomCode,
    GamePhase Phase,
    int TargetScore,
    IReadOnlyCollection<PlayerState> Players,
    PlayerState? CurrentStoryteller,
    string? Clue,
    int SubmissionCount,
    int ExpectedSubmissionCount,
    int VoteCount,
    int ExpectedVoteCount,
    IReadOnlyCollection<VotingCardState> VotingCards,
    IReadOnlyCollection<RevealCardState> RevealedCards,
    IReadOnlyCollection<RoundScoreState> RoundScores,
    IReadOnlyCollection<ScoreboardEntryState> Scoreboard,
    IReadOnlyCollection<WinningPlayerState> Winners)
{
    public static RoomState FromRoom(Room room)
    {
        var players = room.Players
            .Select(player => new PlayerState(player.Id.Value, player.Name, player.AvatarId))
            .ToArray();
        var currentStoryteller = room.CurrentStorytellerId is { } storytellerId
            ? players.Single(player => player.PlayerId == storytellerId.Value)
            : null;
        var votingCards = room.VotingCards
            .Select(card => new VotingCardState(
                card.VotingCardId.Value,
                card.CardId.Value,
                card.DisplayNumber,
                card.Votes
                    .Select(vote => new VotingCardVoteState(vote.VoterPlayerId.Value, vote.VoterName))
                    .ToArray()))
            .ToArray();
        var revealedCards = room.RevealedVotingCards
            .Select(card => new RevealCardState(
                card.VotingCardId.Value,
                card.CardId.Value,
                card.DisplayNumber,
                card.OwnerPlayerId.Value,
                card.OwnerName,
                card.IsStorytellerCard,
                card.Votes
                    .Select(vote => new RevealedVoteState(vote.VoterPlayerId.Value, vote.VoterName))
                    .ToArray()))
            .ToArray();
        var roundScores = room.RoundScores
            .Select(score => new RoundScoreState(
                score.PlayerId.Value,
                score.PlayerName,
                score.PointsAwarded))
            .ToArray();
        var scoreboard = room.Scoreboard
            .Select(score => new ScoreboardEntryState(
                score.PlayerId.Value,
                score.PlayerName,
                score.TotalPoints))
            .ToArray();
        var winners = room.Winners
            .Select(score => new WinningPlayerState(
                score.PlayerId.Value,
                score.PlayerName,
                score.TotalPoints))
            .ToArray();

        return new RoomState(
            room.Code.Value,
            room.Phase,
            room.TargetScore,
            players,
            currentStoryteller,
            room.Clue,
            room.SubmissionCount,
            room.ExpectedSubmissionCount,
            room.VoteCount,
            room.ExpectedVoteCount,
            votingCards,
            revealedCards,
            roundScores,
            scoreboard,
            winners);
    }
}
