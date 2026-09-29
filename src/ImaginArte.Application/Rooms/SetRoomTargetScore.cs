using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record SetRoomTargetScoreCommand(string RoomCode, int TargetScore);

public sealed record SetRoomTargetScoreResult(RoomState Room);

public sealed class SetRoomTargetScoreHandler
{
    private readonly IRoomRepository _rooms;

    public SetRoomTargetScoreHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<SetRoomTargetScoreResult> HandleAsync(
        SetRoomTargetScoreCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.SetTargetScore(command.TargetScore);

        await _rooms.SaveAsync(room, cancellationToken);

        return new SetRoomTargetScoreResult(RoomState.FromRoom(room));
    }
}
