using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record PlayerState(Guid PlayerId, string Name);

public sealed record RoomState(string RoomCode, GamePhase Phase, IReadOnlyCollection<PlayerState> Players)
{
    public static RoomState FromRoom(Room room)
    {
        return new RoomState(
            room.Code.Value,
            room.Phase,
            room.Players
                .Select(player => new PlayerState(player.Id.Value, player.Name))
                .ToArray());
    }
}
