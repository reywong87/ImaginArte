using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record ScoreRoundCommand(string RoomCode);

public sealed record ScoreRoundResult(RoomState Room);

public sealed class ScoreRoundHandler
{
    private readonly IRoomRepository _rooms;

    public ScoreRoundHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<ScoreRoundResult> HandleAsync(
        ScoreRoundCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.ScoreRound();

        await _rooms.SaveAsync(room, cancellationToken);

        return new ScoreRoundResult(RoomState.FromRoom(room));
    }
}
