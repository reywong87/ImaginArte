using ImaginArte.Application.Rooms;
using ImaginArte.Contracts.Rooms;
using ImaginArte.Domain;

internal static class RoomHttpMappings
{
    public static GetPlayerPrivateStateResponse ToHttpResponse(
        this GetPlayerPrivateStateResult result,
        RoomPresenceTracker presenceTracker)
    {
        ArgumentNullException.ThrowIfNull(presenceTracker);

        return new GetPlayerPrivateStateResponse(
            result.Room.ToHttpResponse(presenceTracker),
            new PlayerPrivateStateResponse(
                result.Player.PlayerId,
                result.Player.Name,
                result.Player.Hand
                    .Select(card => new CardStateResponse(card.CardId))
                    .ToArray(),
                result.Player.IsCurrentStoryteller,
                result.Player.HasSubmittedGuessCard,
                result.Player.SubmittedVotingCardId,
                result.Player.HasVoted));
    }

    public static RoomStateResponse ToHttpResponse(
        this RoomState room,
        RoomPresenceTracker presenceTracker)
    {
        ArgumentNullException.ThrowIfNull(presenceTracker);

        var players = room.Players
            .Select(player => new PlayerStateResponse(player.PlayerId, player.Name, player.AvatarId.Value)
            {
                IsConnected = presenceTracker.IsConnected(room.RoomCode, player.PlayerId)
            })
            .ToArray();
        var exposeIndividualVotes = room.Phase is not GamePhase.Voting;

        return new RoomStateResponse(
            room.RoomCode,
            (RoomPhase)room.Phase,
            room.TargetScore,
            players,
            room.CurrentStoryteller is { } storyteller
                ? new PlayerStateResponse(storyteller.PlayerId, storyteller.Name, storyteller.AvatarId.Value)
                {
                    IsConnected = presenceTracker.IsConnected(room.RoomCode, storyteller.PlayerId)
                }
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
                    card.DisplayNumber,
                    exposeIndividualVotes
                        ? card.Votes
                            .Select(vote => new VotingCardVoteStateResponse(
                                vote.VoterPlayerId,
                                vote.VoterName))
                            .ToArray()
                        : Array.Empty<VotingCardVoteStateResponse>()))
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
                .ToArray())
        {
            MinimumPlayers = Room.MinimumPlayers,
            MaximumPlayers = Room.MaximumPlayers,
            HostPlayerId = players.FirstOrDefault()?.PlayerId
        };
    }
}
