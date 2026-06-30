using System.Net;
using System.Net.Http.Json;
using ImagoraGame.Client.Blazor.Rooms;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Tests;

public sealed class RoomClientTests
{
    [Fact]
    public async Task Initialize_LoadsHttpSnapshotAndSubscribesToRoom()
    {
        var initialRoom = CreateRoom("ABC123", "Alice");
        var handler = new RecordingHttpMessageHandler(request =>
            JsonResponse(initialRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);

        await client.InitializeAsync(" abc123 ");

        Assert.Equal("ABC123", client.Room?.RoomCode);
        Assert.Equal("Alice", Assert.Single(client.Room!.Players).Name);
        Assert.Equal(HttpMethod.Get, Assert.Single(handler.Requests).Method);
        Assert.Equal("http://localhost:5080/rooms/abc123", handler.Requests[0].RequestUri?.ToString());
        Assert.Equal(" abc123 ", realtime.SubscribedRoomCode);
        Assert.Equal(RoomConnectionStatus.Connected, client.ConnectionStatus);
    }

    [Fact]
    public async Task RoomUpdated_ReplacesSnapshotAndNewPlayersAppear()
    {
        var initialRoom = CreateRoom("ABC123", "Alice");
        var updatedRoom = CreateRoom("ABC123", "Alice", "Bob");
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(initialRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        realtime.Publish(new RoomUpdatedEvent(updatedRoom));

        Assert.Same(updatedRoom, client.Room);
        Assert.Collection(
            client.Room!.Players,
            player => Assert.Equal("Alice", player.Name),
            player => Assert.Equal("Bob", player.Name));
    }

    [Fact]
    public async Task StartGame_PostsToExpectedEndpointAndUsesReturnedSnapshot()
    {
        var waitingRoom = CreateRoom("ABC123", "Alice", "Bob", "Carla");
        var startedRoom = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Alice",
            "Alice",
            "Bob",
            "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(waitingRoom)
                : JsonResponse(startedRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.StartGameAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/start",
            handler.Requests[1].RequestUri?.ToString());
        Assert.Equal(RoomPhase.StorytellerChoosing, client.Room?.Phase);
    }

    [Fact]
    public async Task StartGame_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var waitingRoom = CreateRoom("ABC123", "Alice", "Bob", "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(waitingRoom)
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        Detail = "La sala ya no se puede iniciar."
                    })
                });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.StartGameAsync();

        Assert.Equal("La sala ya no se puede iniciar.", client.ErrorMessage);
        Assert.False(client.IsStarting);
    }

    [Fact]
    public async Task Dispose_DisposesRealtimeConnectionAndListener()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            JsonResponse(CreateRoom("ABC123")));
        var realtime = new FakeRoomRealtimeConnection();
        var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.DisposeAsync();

        Assert.True(realtime.IsDisposed);
        Assert.True(realtime.ListenerWasDisposed);
    }

    [Fact]
    public void LobbyPresentation_DisablesStartWithFewerThanThreePlayers()
    {
        var room = CreateRoom("ABC123", "Alice", "Bob");

        Assert.True(RoomLobbyPresentation.ShowsStartButton(room));
        Assert.False(RoomLobbyPresentation.CanStart(room, isStarting: false));
    }

    [Fact]
    public void LobbyPresentation_StorytellerChoosingShowsStorytellerAndHidesStart()
    {
        var room = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Alice",
            "Alice",
            "Bob",
            "Carla");

        Assert.False(RoomLobbyPresentation.ShowsStartButton(room));
        Assert.Equal("Alice", RoomLobbyPresentation.StorytellerName(room));
    }

    [Fact]
    public void LobbyPresentation_RevealedCardsForDisplayOrdersByDisplayNumber()
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var room = new RoomStateResponse(
            "ABC123",
            RoomPhase.Reveal,
            25,
            [
                new PlayerStateResponse(aliceId, "Alice"),
                new PlayerStateResponse(bobId, "Bob")
            ],
            new PlayerStateResponse(aliceId, "Alice"),
            "Un faro en la niebla",
            0,
            0,
            0,
            0,
            [],
            [
                new RevealCardStateResponse(
                    "vote-3",
                    "card-c",
                    3,
                    bobId,
                    "Bob",
                    false,
                    []),
                new RevealCardStateResponse(
                    "vote-1",
                    "card-a",
                    1,
                    aliceId,
                    "Alice",
                    true,
                    [
                        new RevealedVoteStateResponse(bobId, "Bob")
                    ])
            ],
            [],
            [],
            []);

        var cards = RoomLobbyPresentation.RevealedCardsForDisplay(room).ToArray();

        Assert.Equal([1, 3], cards.Select(card => card.DisplayNumber));
        Assert.True(cards[0].IsStorytellerCard);
        Assert.Equal("Bob", Assert.Single(cards[0].Votes).VoterName);
    }

    private static RoomClient CreateClient(
        HttpMessageHandler handler,
        FakeRoomRealtimeConnection realtime)
    {
        var baseUri = new Uri("http://localhost:5080/");
        return new RoomClient(
            new HttpClient(handler) { BaseAddress = baseUri },
            new RoomClientOptions(baseUri),
            new FakeRoomRealtimeConnectionFactory(realtime));
    }

    private static HttpResponseMessage JsonResponse(RoomStateResponse room)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(room)
        };
    }

    private static RoomStateResponse CreateRoom(
        string roomCode,
        params string[] playerNames)
    {
        return CreateRoom(
            roomCode,
            RoomPhase.WaitingForPlayers,
            storytellerName: null,
            playerNames);
    }

    private static RoomStateResponse CreateRoom(
        string roomCode,
        RoomPhase phase,
        string? storytellerName,
        params string[] playerNames)
    {
        var players = playerNames
            .Select(name => new PlayerStateResponse(Guid.NewGuid(), name))
            .ToArray();
        var storyteller = storytellerName is null
            ? null
            : players.First(player => player.Name == storytellerName);

        return new RoomStateResponse(
            roomCode,
            phase,
            25,
            players,
            storyteller,
            null,
            0,
            0,
            0,
            0,
            [],
            [],
            [],
            [],
            []);
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
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
        private Action<RoomUpdatedEvent>? _roomUpdated;

        public event Func<Exception?, Task>? Reconnecting
        {
            add { }
            remove { }
        }

        public event Func<string?, Task>? Reconnected
        {
            add { }
            remove { }
        }

        public event Func<Exception?, Task>? Closed
        {
            add { }
            remove { }
        }

        public string? SubscribedRoomCode { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool ListenerWasDisposed { get; private set; }

        public IDisposable OnRoomUpdated(Action<RoomUpdatedEvent> handler)
        {
            _roomUpdated = handler;
            return new CallbackDisposable(() =>
            {
                _roomUpdated = null;
                ListenerWasDisposed = true;
            });
        }

        public IDisposable OnPlayerStateUpdated(Action<PlayerStateUpdatedEvent> handler)
        {
            return new CallbackDisposable(() => { });
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task SubscribeToRoomAsync(string roomCode, CancellationToken cancellationToken)
        {
            SubscribedRoomCode = roomCode;
            return Task.CompletedTask;
        }

        public Task SubscribeAsPlayerAsync(
            string roomCode,
            Guid playerId,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Publish(RoomUpdatedEvent roomUpdated)
        {
            _roomUpdated?.Invoke(roomUpdated);
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
