using System.Text.Json;
using ImaginArte.Domain;

namespace ImaginArte.Infrastructure.Persistence;

internal static class RoomJsonSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        return JsonSerializer.Serialize(ToDocument(room.ToSnapshot()), SerializerOptions);
    }

    public static Room Deserialize(string serializedRoom)
    {
        if (string.IsNullOrWhiteSpace(serializedRoom))
        {
            throw new ArgumentException("Serialized room is required.", nameof(serializedRoom));
        }

        var document = JsonSerializer.Deserialize<RoomDocument>(serializedRoom, SerializerOptions)
            ?? throw new InvalidOperationException("Stored room document could not be deserialized.");

        return Room.Restore(ToSnapshot(document));
    }

    private static RoomDocument ToDocument(RoomSnapshot snapshot)
    {
        return new RoomDocument(
            snapshot.Code.Value,
            snapshot.Phase.ToString(),
            snapshot.TargetScore,
            snapshot.CurrentStorytellerId?.Value,
            snapshot.StorytellerCardId?.Value,
            snapshot.Clue,
            snapshot.Players
                .Select(player => new PlayerDocument(
                    player.Id.Value,
                    player.Name,
                    player.Hand.Select(card => card.Id.Value).ToArray()))
                .ToArray(),
            snapshot.AvailableDeck.Select(card => card.Id.Value).ToArray(),
            snapshot.DiscardPile.Select(card => card.Id.Value).ToArray(),
            snapshot.GuessCardSubmissions
                .Select(submission => new GuessCardSubmissionDocument(
                    submission.PlayerId.Value,
                    submission.CardId.Value))
                .ToArray(),
            snapshot.VotingCardOwnerships
                .Select(ownership => new VotingCardOwnershipDocument(
                    ownership.VotingCardId.Value,
                    ownership.CardId.Value,
                    ownership.OwnerPlayerId.Value,
                    ownership.IsStorytellerCard))
                .ToArray(),
            snapshot.Votes
                .Select(vote => new VoteDocument(
                    vote.VoterId.Value,
                    vote.VotingCardId.Value))
                .ToArray(),
            snapshot.Scoreboard
                .Select(score => new PlayerScoreDocument(
                    score.PlayerId.Value,
                    score.PlayerName,
                    score.TotalPoints))
                .ToArray(),
            snapshot.RoundScores
                .Select(score => new PlayerRoundScoreDocument(
                    score.PlayerId.Value,
                    score.PlayerName,
                    score.PointsAwarded))
                .ToArray());
    }

    private static RoomSnapshot ToSnapshot(RoomDocument document)
    {
        return new RoomSnapshot(
            new RoomCode(document.RoomCode),
            Enum.Parse<GamePhase>(document.Phase),
            document.TargetScore,
            document.CurrentStorytellerId is { } storytellerId ? new PlayerId(storytellerId) : null,
            document.StorytellerCardId is { } storytellerCardId ? new CardId(storytellerCardId) : null,
            document.Clue,
            document.Players
                .Select(player => new PlayerSnapshot(
                    new PlayerId(player.PlayerId),
                    player.Name,
                    player.Hand.Select(cardId => new Card(new CardId(cardId))).ToArray()))
                .ToArray(),
            document.AvailableDeck.Select(cardId => new Card(new CardId(cardId))).ToArray(),
            document.DiscardPile.Select(cardId => new Card(new CardId(cardId))).ToArray(),
            document.GuessCardSubmissions
                .Select(submission => new GuessCardSubmission(
                    new PlayerId(submission.PlayerId),
                    new CardId(submission.CardId)))
                .ToArray(),
            document.VotingCardOwnerships
                .Select(ownership => new VotingCardOwnership(
                    new VotingCardId(ownership.VotingCardId),
                    new CardId(ownership.CardId),
                    new PlayerId(ownership.OwnerPlayerId),
                    ownership.IsStorytellerCard))
                .ToArray(),
            document.Votes
                .Select(vote => new Vote(
                    new PlayerId(vote.VoterId),
                    new VotingCardId(vote.VotingCardId)))
                .ToArray(),
            document.Scoreboard
                .Select(score => new PlayerScore(
                    new PlayerId(score.PlayerId),
                    score.PlayerName,
                    score.TotalPoints))
                .ToArray(),
            document.RoundScores
                .Select(score => new PlayerRoundScore(
                    new PlayerId(score.PlayerId),
                    score.PlayerName,
                    score.PointsAwarded))
                .ToArray());
    }

    private sealed record RoomDocument(
        string RoomCode,
        string Phase,
        int TargetScore,
        Guid? CurrentStorytellerId,
        string? StorytellerCardId,
        string? Clue,
        PlayerDocument[] Players,
        string[] AvailableDeck,
        string[] DiscardPile,
        GuessCardSubmissionDocument[] GuessCardSubmissions,
        VotingCardOwnershipDocument[] VotingCardOwnerships,
        VoteDocument[] Votes,
        PlayerScoreDocument[] Scoreboard,
        PlayerRoundScoreDocument[] RoundScores);

    private sealed record PlayerDocument(
        Guid PlayerId,
        string Name,
        string[] Hand);

    private sealed record GuessCardSubmissionDocument(
        Guid PlayerId,
        string CardId);

    private sealed record VotingCardOwnershipDocument(
        string VotingCardId,
        string CardId,
        Guid OwnerPlayerId,
        bool IsStorytellerCard);

    private sealed record VoteDocument(
        Guid VoterId,
        string VotingCardId);

    private sealed record PlayerScoreDocument(
        Guid PlayerId,
        string PlayerName,
        int TotalPoints);

    private sealed record PlayerRoundScoreDocument(
        Guid PlayerId,
        string PlayerName,
        int PointsAwarded);
}
