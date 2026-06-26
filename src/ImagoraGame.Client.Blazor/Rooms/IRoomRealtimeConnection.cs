using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public interface IRoomRealtimeConnection : IAsyncDisposable
{
    event Func<Exception?, Task>? Reconnecting;

    event Func<string?, Task>? Reconnected;

    event Func<Exception?, Task>? Closed;

    IDisposable OnRoomUpdated(Action<RoomUpdatedEvent> handler);

    IDisposable OnPlayerStateUpdated(Action<PlayerStateUpdatedEvent> handler);

    Task StartAsync(CancellationToken cancellationToken);

    Task SubscribeToRoomAsync(string roomCode, CancellationToken cancellationToken);

    Task SubscribeAsPlayerAsync(
        string roomCode,
        Guid playerId,
        CancellationToken cancellationToken);
}

public interface IRoomRealtimeConnectionFactory
{
    IRoomRealtimeConnection Create(Uri hubUri);
}
