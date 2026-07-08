using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record AdvanceRoundCommand(string RoomCode);

public sealed record AdvanceRoundResult(RoomState Room);

public sealed class AdvanceRoundHandler
{
    private readonly IRoomRepository _rooms;

    public AdvanceRoundHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<AdvanceRoundResult> HandleAsync(
        AdvanceRoundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.AdvanceRound();

        await _rooms.SaveAsync(room, cancellationToken);

        return new AdvanceRoundResult(RoomState.FromRoom(room));
    }
}
