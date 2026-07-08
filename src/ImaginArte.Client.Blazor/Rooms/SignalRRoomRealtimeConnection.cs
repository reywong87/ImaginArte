using ImaginArte.Contracts.Rooms;
using Microsoft.AspNetCore.SignalR.Client;

namespace ImaginArte.Client.Blazor.Rooms;

public sealed class SignalRRoomRealtimeConnectionFactory : IRoomRealtimeConnectionFactory
{
    public IRoomRealtimeConnection Create(Uri hubUri)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        return new SignalRRoomRealtimeConnection(connection);
    }
}

internal sealed class SignalRRoomRealtimeConnection : IRoomRealtimeConnection
{
    private readonly HubConnection _connection;

    public SignalRRoomRealtimeConnection(HubConnection connection)
    {
        _connection = connection;
        _connection.Reconnecting += exception => Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
        _connection.Reconnected += connectionId => Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
        _connection.Closed += exception => Closed?.Invoke(exception) ?? Task.CompletedTask;
    }

    public event Func<Exception?, Task>? Reconnecting;

    public event Func<string?, Task>? Reconnected;

    public event Func<Exception?, Task>? Closed;

    public IDisposable OnRoomUpdated(Action<RoomUpdatedEvent> handler)
    {
        return _connection.On(RoomRealtimeEventNames.RoomUpdated, handler);
    }

    public IDisposable OnPlayerStateUpdated(Action<PlayerStateUpdatedEvent> handler)
    {
        return _connection.On(RoomRealtimeEventNames.PlayerStateUpdated, handler);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _connection.StartAsync(cancellationToken);
    }

    public Task SubscribeToRoomAsync(string roomCode, CancellationToken cancellationToken)
    {
        return _connection.InvokeAsync("SubscribeToRoom", roomCode, cancellationToken);
    }

    public Task SubscribeAsPlayerAsync(
        string roomCode,
        Guid playerId,
        CancellationToken cancellationToken)
    {
        return _connection.InvokeAsync("SubscribeAsPlayer", roomCode, playerId, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _connection.DisposeAsync();
    }
}
