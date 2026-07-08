using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record JoinRoomCommand(string RoomCode, Guid PlayerId, string PlayerName);

public sealed record JoinRoomResult(PlayerState Player, RoomState Room);

public sealed class JoinRoomHandler
{
    private readonly IRoomRepository _rooms;

    public JoinRoomHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<JoinRoomResult> HandleAsync(
        JoinRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        var joinedPlayer = room.Join(new PlayerId(command.PlayerId), command.PlayerName);

        await _rooms.SaveAsync(room, cancellationToken);

        return new JoinRoomResult(
            new PlayerState(joinedPlayer.Id.Value, joinedPlayer.Name),
            RoomState.FromRoom(room));
    }
}
