using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record CreateRoomCommand(string RoomCode);

public sealed record CreateRoomResult(RoomState Room);

public sealed class CreateRoomHandler
{
    private readonly IRoomRepository _rooms;

    public CreateRoomHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<CreateRoomResult> HandleAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var existingRoom = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (existingRoom is not null)
        {
            throw new RoomAlreadyExistsException(roomCode.Value);
        }

        var room = Room.Create(roomCode);

        await _rooms.AddAsync(room, cancellationToken);

        return new CreateRoomResult(RoomState.FromRoom(room));
    }
}
