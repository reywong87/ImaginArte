using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record PlayerState(Guid PlayerId, string Name);

public sealed record RoomState(
    string RoomCode,
    GamePhase Phase,
    IReadOnlyCollection<PlayerState> Players,
    PlayerState? CurrentStoryteller,
    string? Clue,
    int SubmissionCount,
    int ExpectedSubmissionCount)
{
    public static RoomState FromRoom(Room room)
    {
        var players = room.Players
            .Select(player => new PlayerState(player.Id.Value, player.Name))
            .ToArray();
        var currentStoryteller = room.CurrentStorytellerId is { } storytellerId
            ? players.Single(player => player.PlayerId == storytellerId.Value)
            : null;

        return new RoomState(
            room.Code.Value,
            room.Phase,
            players,
            currentStoryteller,
            room.Clue,
            room.SubmissionCount,
            room.ExpectedSubmissionCount);
    }
}
