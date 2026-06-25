using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public interface IRoomRealtimeConnection : IAsyncDisposable
{
    event Func<Exception?, Task>? Reconnecting;

    event Func<string?, Task>? Reconnected;

    event Func<Exception?, Task>? Closed;

    IDisposable OnRoomUpdated(Action<RoomUpdatedEvent> handler);

    Task StartAsync(CancellationToken cancellationToken);

    Task SubscribeToRoomAsync(string roomCode, CancellationToken cancellationToken);
}

public interface IRoomRealtimeConnectionFactory
{
    IRoomRealtimeConnection Create(Uri hubUri);
}
