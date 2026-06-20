using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public sealed record GetPlayerPrivateStateQuery(string RoomCode, Guid PlayerId);

public sealed record GetPlayerPrivateStateResult(RoomState Room, PlayerPrivateState Player);

public sealed record PlayerPrivateState(
    Guid PlayerId,
    string Name,
    IReadOnlyCollection<CardState> Hand,
    bool IsCurrentStoryteller);

public sealed record CardState(string CardId);

public sealed class GetPlayerPrivateStateHandler
{
    private readonly IRoomRepository _rooms;

    public GetPlayerPrivateStateHandler(IRoomRepository rooms)
    {
        _rooms = rooms;
    }

    public async Task<GetPlayerPrivateStateResult> HandleAsync(
        GetPlayerPrivateStateQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var roomCode = new RoomCode(query.RoomCode);
        var playerId = new PlayerId(query.PlayerId);
        var room = await _rooms.GetByCodeAsync(roomCode, cancellationToken);

        if (room is null)
        {
            throw new RoomNotFoundException(roomCode.Value);
        }

        var player = room.Players.SingleOrDefault(existingPlayer => existingPlayer.Id == playerId);

        if (player is null)
        {
            throw new PlayerNotFoundInRoomException(roomCode.Value, playerId.Value);
        }

        var hand = room.GetHandFor(playerId)
            .Select(card => new CardState(card.Id.Value))
            .ToArray();
        var privateState = new PlayerPrivateState(
            player.Id.Value,
            player.Name,
            hand,
            room.CurrentStorytellerId == player.Id);

        return new GetPlayerPrivateStateResult(
            RoomState.FromRoom(room),
            privateState);
    }
}
