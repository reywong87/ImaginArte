using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record GetRoomStateQuery(string RoomCode);

public sealed record GetRoomStateResult(RoomState Room);

public sealed class GetRoomStateHandler
{
    private readonly IRoomRepository _rooms;

    public GetRoomStateHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<GetRoomStateResult> HandleAsync(
        GetRoomStateQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var roomCode = new RoomCode(query.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        return new GetRoomStateResult(RoomState.FromRoom(room));
    }
}
