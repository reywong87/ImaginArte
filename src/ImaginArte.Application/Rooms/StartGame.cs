using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record StartGameCommand(string RoomCode);

public sealed record StartGameResult(RoomState Room);

public sealed class StartGameHandler
{
    private readonly IRoomRepository _rooms;

    public StartGameHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<StartGameResult> HandleAsync(
        StartGameCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.StartGame();

        await _rooms.SaveAsync(room, cancellationToken);

        return new StartGameResult(RoomState.FromRoom(room));
    }
}
