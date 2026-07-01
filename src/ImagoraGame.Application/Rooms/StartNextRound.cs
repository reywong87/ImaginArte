using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record StartNextRoundCommand(string RoomCode);

public sealed record StartNextRoundResult(RoomState Room);

public sealed class StartNextRoundHandler
{
    private readonly IRoomRepository _rooms;

    public StartNextRoundHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<StartNextRoundResult> HandleAsync(
        StartNextRoundCommand command,
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
        room.AdvanceRound();

        await _rooms.SaveAsync(room, cancellationToken);

        return new StartNextRoundResult(RoomState.FromRoom(room));
    }
}
