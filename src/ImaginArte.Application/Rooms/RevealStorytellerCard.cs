using ImaginArte.Domain;

namespace ImaginArte.Application.Rooms;

public sealed record RevealStorytellerCardCommand(string RoomCode);

public sealed record RevealStorytellerCardResult(RoomState Room);

public sealed class RevealStorytellerCardHandler
{
    private readonly IRoomRepository _rooms;

    public RevealStorytellerCardHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<RevealStorytellerCardResult> HandleAsync(
        RevealStorytellerCardCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.RevealStorytellerCard();

        await _rooms.SaveAsync(room, cancellationToken);

        return new RevealStorytellerCardResult(RoomState.FromRoom(room));
    }
}
