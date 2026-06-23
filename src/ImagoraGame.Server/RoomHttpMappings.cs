using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;

internal static class RoomHttpMappings
{
    public static GetPlayerPrivateStateResponse ToHttpResponse(
        this GetPlayerPrivateStateResult result)
    {
        return new GetPlayerPrivateStateResponse(
            result.Room.ToHttpResponse(),
            new PlayerPrivateStateResponse(
                result.Player.PlayerId,
                result.Player.Name,
                result.Player.Hand
                    .Select(card => new CardStateResponse(card.CardId))
                    .ToArray(),
                result.Player.IsCurrentStoryteller));
    }

    private static RoomStateResponse ToHttpResponse(this RoomState room)
    {
        return new RoomStateResponse(
            room.RoomCode,
            (RoomPhase)room.Phase,
            room.TargetScore,
            room.Players
                .Select(player => new PlayerStateResponse(player.PlayerId, player.Name))
                .ToArray(),
            room.CurrentStoryteller is { } storyteller
                ? new PlayerStateResponse(storyteller.PlayerId, storyteller.Name)
                : null,
            room.Clue,
            room.SubmissionCount,
            room.ExpectedSubmissionCount,
            room.VoteCount,
            room.ExpectedVoteCount,
            room.VotingCards
                .Select(card => new VotingCardStateResponse(
                    card.VotingCardId,
                    card.CardId,
                    card.DisplayNumber))
                .ToArray(),
            room.RevealedCards
                .Select(card => new RevealCardStateResponse(
                    card.VotingCardId,
                    card.CardId,
                    card.DisplayNumber,
                    card.OwnerPlayerId,
                    card.OwnerName,
                    card.IsStorytellerCard,
                    card.Votes
                        .Select(vote => new RevealedVoteStateResponse(
                            vote.VoterPlayerId,
                            vote.VoterName))
                        .ToArray()))
                .ToArray(),
            room.RoundScores
                .Select(score => new RoundScoreStateResponse(
                    score.PlayerId,
                    score.PlayerName,
                    score.PointsAwarded))
                .ToArray(),
            room.Scoreboard
                .Select(score => new ScoreboardEntryStateResponse(
                    score.PlayerId,
                    score.PlayerName,
                    score.TotalPoints))
                .ToArray(),
            room.Winners
                .Select(winner => new WinningPlayerStateResponse(
                    winner.PlayerId,
                    winner.PlayerName,
                    winner.TotalPoints))
                .ToArray());
    }
}
