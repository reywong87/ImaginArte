using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record CastVoteCommand(
    string RoomCode,
    Guid PlayerId,
    string VotingCardId);

public sealed record CastVoteResult(RoomState Room);

public sealed class CastVoteHandler
{
    private readonly IRoomRepository _rooms;

    public CastVoteHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<CastVoteResult> HandleAsync(
        CastVoteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roomCode = new RoomCode(command.RoomCode);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        room.CastVote(
            new PlayerId(command.PlayerId),
            new VotingCardId(command.VotingCardId));

        await _rooms.SaveAsync(room, cancellationToken);

        return new CastVoteResult(RoomState.FromRoom(room));
    }
}
