using ImagoraGame.Client.Blazor.Rooms;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Tests;

public sealed class MobileRoomClientTests
{
    [Fact]
    public async Task ConnectAsync_StartsConnectionAndSubscribesAsPlayer()
    {
        var playerId = Guid.NewGuid();
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(realtime);
        var lobby = new MobileLobbyState("ABC123", playerId, "Alice");

        await client.ConnectAsync(lobby);

        Assert.True(realtime.Started);
        Assert.Equal("ABC123", realtime.SubscribedPlayerRoomCode);
        Assert.Equal(playerId, realtime.SubscribedPlayerId);
        Assert.Equal(RoomConnectionStatus.Connected, client.ConnectionStatus);
    }

    [Fact]
    public async Task PlayerStateUpdated_StoresPrivateState()
    {
        var playerId = Guid.NewGuid();
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(realtime);
        var lobby = new MobileLobbyState("ABC123", playerId, "Alice");
        await client.ConnectAsync(lobby);
        var privateState = CreatePrivateState("ABC123", playerId, "Alice");

        realtime.Publish(new PlayerStateUpdatedEvent(privateState));

        Assert.Same(privateState, client.PrivateState);
        Assert.Equal("Alice", client.PrivateState!.Player.Name);
        Assert.Null(client.ErrorMessage);
    }

    [Fact]
    public async Task Reconnected_ResubscribesAsPlayer()
    {
        var playerId = Guid.NewGuid();
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(realtime);
        var lobby = new MobileLobbyState("ABC123", playerId, "Alice");
        await client.ConnectAsync(lobby);

        realtime.ClearPlayerSubscription();
        await realtime.RaiseReconnectedAsync();

        Assert.Equal("ABC123", realtime.SubscribedPlayerRoomCode);
        Assert.Equal(playerId, realtime.SubscribedPlayerId);
        Assert.Equal(RoomConnectionStatus.Connected, client.ConnectionStatus);
    }

    [Fact]
    public async Task Dispose_DisposesRealtimeConnectionAndListener()
    {
        var realtime = new FakeRoomRealtimeConnection();
        var client = CreateClient(realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", Guid.NewGuid(), "Alice"));

        await client.DisposeAsync();

        Assert.True(realtime.IsDisposed);
        Assert.True(realtime.PlayerStateListenerWasDisposed);
    }

    private static MobileRoomClient CreateClient(FakeRoomRealtimeConnection realtime)
    {
        var baseUri = new Uri("http://localhost:5080/");
        return new MobileRoomClient(
            new RoomClientOptions(baseUri),
            new FakeRoomRealtimeConnectionFactory(realtime));
    }

    private static GetPlayerPrivateStateResponse CreatePrivateState(
        string roomCode,
        Guid playerId,
        string playerName)
    {
        var player = new PlayerStateResponse(playerId, playerName);
        var room = new RoomStateResponse(
            roomCode,
            RoomPhase.WaitingForPlayers,
            25,
            [player],
            CurrentStoryteller: null,
            Clue: null,
            SubmissionCount: 0,
            ExpectedSubmissionCount: 0,
            VoteCount: 0,
            ExpectedVoteCount: 0,
            VotingCards: [],
            RevealedCards: [],
            RoundScores: [],
            Scoreboard: [],
            Winners: []);

        return new GetPlayerPrivateStateResponse(
            room,
            new PlayerPrivateStateResponse(
                playerId,
                playerName,
                Hand: [],
                IsCurrentStoryteller: false));
    }

    private sealed class FakeRoomRealtimeConnectionFactory(
        FakeRoomRealtimeConnection connection)
        : IRoomRealtimeConnectionFactory
    {
        public IRoomRealtimeConnection Create(Uri hubUri)
        {
            Assert.Equal("http://localhost:5080/hubs/rooms", hubUri.ToString());
            return connection;
        }
    }

    private sealed class FakeRoomRealtimeConnection : IRoomRealtimeConnection
    {
        private Action<PlayerStateUpdatedEvent>? _playerStateUpdated;
        private Func<string?, Task>? _reconnected;

        public event Func<Exception?, Task>? Reconnecting
        {
            add { }
            remove { }
        }

        public event Func<string?, Task>? Reconnected
        {
            add => _reconnected += value;
            remove => _reconnected -= value;
        }

        public event Func<Exception?, Task>? Closed
        {
            add { }
            remove { }
        }

        public bool Started { get; private set; }

        public string? SubscribedPlayerRoomCode { get; private set; }

        public Guid? SubscribedPlayerId { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool PlayerStateListenerWasDisposed { get; private set; }

        public IDisposable OnRoomUpdated(Action<RoomUpdatedEvent> handler)
        {
            return new CallbackDisposable(() => { });
        }

        public IDisposable OnPlayerStateUpdated(Action<PlayerStateUpdatedEvent> handler)
        {
            _playerStateUpdated = handler;
            return new CallbackDisposable(() =>
            {
                _playerStateUpdated = null;
                PlayerStateListenerWasDisposed = true;
            });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task SubscribeToRoomAsync(string roomCode, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SubscribeAsPlayerAsync(
            string roomCode,
            Guid playerId,
            CancellationToken cancellationToken)
        {
            SubscribedPlayerRoomCode = roomCode;
            SubscribedPlayerId = playerId;
            return Task.CompletedTask;
        }

        public void Publish(PlayerStateUpdatedEvent playerStateUpdated)
        {
            _playerStateUpdated?.Invoke(playerStateUpdated);
        }

        public void ClearPlayerSubscription()
        {
            SubscribedPlayerRoomCode = null;
            SubscribedPlayerId = null;
        }

        public Task RaiseReconnectedAsync()
        {
            return _reconnected?.Invoke("reconnected") ?? Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose()
        {
            callback();
        }
    }
}
