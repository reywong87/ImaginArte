using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record ChooseStorytellerCardCommand(
    string RoomCode,
    Guid PlayerId,
    string CardId,
    string Clue);

public sealed record ChooseStorytellerCardResult(RoomState Room);

public sealed class ChooseStorytellerCardHandler
{
    private readonly IRoomRepository _rooms;

    public ChooseStorytellerCardHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<ChooseStorytellerCardResult> HandleAsync(
        ChooseStorytellerCardCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.ChooseStorytellerCard(
            new PlayerId(command.PlayerId),
            new CardId(command.CardId),
            command.Clue);

        await _rooms.SaveAsync(room, cancellationToken);

        return new ChooseStorytellerCardResult(RoomState.FromRoom(room));
    }
}
