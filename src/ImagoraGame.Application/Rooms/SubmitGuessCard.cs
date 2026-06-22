using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record SubmitGuessCardCommand(
    string RoomCode,
    Guid PlayerId,
    string CardId);

public sealed record SubmitGuessCardResult(RoomState Room);

public sealed class SubmitGuessCardHandler
{
    private readonly IRoomRepository _rooms;

    public SubmitGuessCardHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<SubmitGuessCardResult> HandleAsync(
        SubmitGuessCardCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.SubmitGuessCard(
            new PlayerId(command.PlayerId),
            new CardId(command.CardId));

        await _rooms.SaveAsync(room, cancellationToken);

        return new SubmitGuessCardResult(RoomState.FromRoom(room));
    }
}
