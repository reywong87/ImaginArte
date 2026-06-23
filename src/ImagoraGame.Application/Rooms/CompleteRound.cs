using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record CompleteRoundCommand(string RoomCode);

public sealed record CompleteRoundResult(RoomState Room);

public sealed class CompleteRoundHandler
{
    private readonly IRoomRepository _rooms;

    public CompleteRoundHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<CompleteRoundResult> HandleAsync(
        CompleteRoundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.CompleteRound();

        await _rooms.SaveAsync(room, cancellationToken);

        return new CompleteRoundResult(RoomState.FromRoom(room));
    }
}
