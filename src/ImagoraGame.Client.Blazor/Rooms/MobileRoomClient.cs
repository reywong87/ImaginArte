using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Rooms;

public sealed class MobileRoomClient : IAsyncDisposable
{
    private readonly RoomClientOptions _options;
    private readonly IRoomRealtimeConnectionFactory _connectionFactory;
    private IRoomRealtimeConnection? _connection;
    private IDisposable? _playerStateUpdatedSubscription;
    private MobileLobbyState? _lobby;
    private bool _disposed;

    public MobileRoomClient(
        RoomClientOptions options,
        IRoomRealtimeConnectionFactory connectionFactory)
    {
        _options = options;
        _connectionFactory = connectionFactory;
    }

    public event Action? StateChanged;

    public MobileLobbyState? Lobby => _lobby;

    public GetPlayerPrivateStateResponse? PrivateState { get; private set; }

    public RoomConnectionStatus ConnectionStatus { get; private set; } =
        RoomConnectionStatus.Loading;

    public string? ErrorMessage { get; private set; }

    public async Task ConnectAsync(
        MobileLobbyState lobby,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_connection is not null)
        {
            throw new InvalidOperationException("The mobile room client is already connected.");
        }

        _lobby = lobby;
        PrivateState = null;
        ErrorMessage = null;
        SetStatus(RoomConnectionStatus.Loading);

        try
        {
            var hubUri = new Uri(_options.ApiBaseUri, "hubs/rooms");
            _connection = _connectionFactory.Create(hubUri);
            _playerStateUpdatedSubscription =
                _connection.OnPlayerStateUpdated(HandlePlayerStateUpdated);
            _connection.Reconnecting += HandleReconnectingAsync;
            _connection.Reconnected += HandleReconnectedAsync;
            _connection.Closed += HandleClosedAsync;

            await _connection.StartAsync(cancellationToken);
            await _connection.SubscribeAsPlayerAsync(
                lobby.RoomCode,
                lobby.PlayerId,
                cancellationToken);
            SetStatus(RoomConnectionStatus.Connected);
        }
        catch (Exception exception)
        {
            ErrorMessage = ToUserMessage(exception);
            SetStatus(RoomConnectionStatus.Error);
        }
    }

    private void HandlePlayerStateUpdated(PlayerStateUpdatedEvent playerStateUpdated)
    {
        PrivateState = playerStateUpdated.State;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private Task HandleReconnectingAsync(Exception? exception)
    {
        SetStatus(RoomConnectionStatus.Reconnecting);
        return Task.CompletedTask;
    }

    private async Task HandleReconnectedAsync(string? connectionId)
    {
        if (_connection is null || _lobby is null)
        {
            return;
        }

        try
        {
            await _connection.SubscribeAsPlayerAsync(
                _lobby.RoomCode,
                _lobby.PlayerId,
                CancellationToken.None);
            ErrorMessage = null;
            SetStatus(RoomConnectionStatus.Connected);
        }
        catch (Exception exception)
        {
            ErrorMessage = ToUserMessage(exception);
            SetStatus(RoomConnectionStatus.Error);
        }
    }

    private Task HandleClosedAsync(Exception? exception)
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        ErrorMessage = exception is null
            ? null
            : "Se perdió la conexión en tiempo real con la sala.";
        SetStatus(RoomConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    private void SetStatus(RoomConnectionStatus status)
    {
        ConnectionStatus = status;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    private static string ToUserMessage(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => "La operación fue cancelada.",
            _ => "No se pudo comunicar con la sala. Comprueba que el servidor está disponible."
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StateChanged = null;
        _playerStateUpdatedSubscription?.Dispose();

        if (_connection is not null)
        {
            _connection.Reconnecting -= HandleReconnectingAsync;
            _connection.Reconnected -= HandleReconnectedAsync;
            _connection.Closed -= HandleClosedAsync;
            await _connection.DisposeAsync();
        }
    }
}
