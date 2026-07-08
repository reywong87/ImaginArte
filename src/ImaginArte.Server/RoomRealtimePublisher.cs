using ImaginArte.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR;

internal sealed class RoomRealtimePublisher
{
    private readonly IHubContext<RoomHub> _hubContext;

    public RoomRealtimePublisher(IHubContext<RoomHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishRoomUpdatedAsync(
        RoomStateResponse room,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);

        return _hubContext.Clients
            .Group(RoomHubGroupNames.PublicRoom(room.RoomCode))
            .SendAsync(
                RoomRealtimeEventNames.RoomUpdated,
                new RoomUpdatedEvent(room),
                cancellationToken);
    }

    public Task PublishPlayerStateUpdatedAsync(
        GetPlayerPrivateStateResponse state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);

        return _hubContext.Clients
            .Group(RoomHubGroupNames.PrivatePlayer(
                state.Room.RoomCode,
                state.Player.PlayerId))
            .SendAsync(
                RoomRealtimeEventNames.PlayerStateUpdated,
                new PlayerStateUpdatedEvent(state),
                cancellationToken);
    }
}
