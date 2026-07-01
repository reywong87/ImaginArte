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
    public async Task ScoreRound_PostsToExpectedEndpointAndUsesReturnedSnapshot()
    {
        var revealRoom = CreateRoom("ABC123", RoomPhase.Reveal, "Alice", "Alice", "Bob", "Carla");
        var scoringRoom = CreateRoom("ABC123", RoomPhase.Scoring, "Alice", "Alice", "Bob", "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(revealRoom)
                : JsonResponse(scoringRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.ScoreRoundAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/score",
            handler.Requests[1].RequestUri?.ToString());
        Assert.Equal(RoomPhase.Scoring, client.Room?.Phase);
    }

    [Fact]
    public async Task ScoreRound_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var revealRoom = CreateRoom("ABC123", RoomPhase.Reveal, "Alice", "Alice", "Bob", "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(revealRoom)
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        Detail = "La ronda aún no se puede puntuar."
                    })
                });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.ScoreRoundAsync();

        Assert.Equal("La ronda aún no se puede puntuar.", client.ErrorMessage);
        Assert.False(client.IsScoringRound);
        Assert.Equal(RoomPhase.Reveal, client.Room?.Phase);
    }

    [Fact]
    public async Task ScoreRound_TracksSendingStateWhileRequestIsPending()
    {
        var revealRoom = CreateRoom("ABC123", RoomPhase.Reveal, "Alice", "Alice", "Bob", "Carla");
        var scoringRoom = CreateRoom("ABC123", RoomPhase.Scoring, "Alice", "Alice", "Bob", "Carla");
        var handler = new BlockingScoreHttpMessageHandler(revealRoom, JsonResponse(scoringRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        var scoreTask = client.ScoreRoundAsync();
        await handler.ScoreRequestStarted.Task;

        Assert.True(client.IsScoringRound);

        handler.CompleteScoreRequest();
        await scoreTask;

        Assert.False(client.IsScoringRound);
        Assert.Equal(RoomPhase.Scoring, client.Room?.Phase);
    }

    [Fact]
    public async Task CompleteRound_PostsToExpectedEndpointAndUsesReturnedSnapshot()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var roundCompleteRoom = scoringRoom with
        {
            Phase = RoomPhase.RoundComplete
        };
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(scoringRoom)
                : JsonResponse(roundCompleteRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.CompleteRoundAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/complete-round",
            handler.Requests[1].RequestUri?.ToString());
        Assert.Equal(RoomPhase.RoundComplete, client.Room?.Phase);
        Assert.Equal(scoringRoom.RoundScores, client.Room?.RoundScores);
        Assert.Equal(scoringRoom.Scoreboard, client.Room?.Scoreboard);
    }

    [Fact]
    public async Task CompleteRound_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(scoringRoom)
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        Detail = "La ronda aún no se puede cerrar."
                    })
                });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.CompleteRoundAsync();

        Assert.Equal("La ronda aún no se puede cerrar.", client.ErrorMessage);
        Assert.False(client.IsCompletingRound);
        Assert.Equal(RoomPhase.Scoring, client.Room?.Phase);
    }

    [Fact]
    public async Task CompleteRound_TracksSendingStateWhileRequestIsPending()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var roundCompleteRoom = scoringRoom with
        {
            Phase = RoomPhase.RoundComplete
        };
        var handler = new BlockingCompleteRoundHttpMessageHandler(
            scoringRoom,
            JsonResponse(roundCompleteRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        var completeRoundTask = client.CompleteRoundAsync();
        await handler.CompleteRoundRequestStarted.Task;

        Assert.True(client.IsCompletingRound);

        handler.CompleteRoundRequest();
        await completeRoundTask;

        Assert.False(client.IsCompletingRound);
        Assert.Equal(RoomPhase.RoundComplete, client.Room?.Phase);
    }

    [Fact]
    public async Task AdvanceRound_PostsToExpectedEndpointAndUsesReturnedSnapshot()
    {
        var roundCompleteRoom = CreateRoundCompleteRoomWithScores("ABC123");
        var nextRoundRoom = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Bob",
            "Alice",
            "Bob",
            "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(roundCompleteRoom)
                : JsonResponse(nextRoundRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.AdvanceRoundAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/advance-round",
            handler.Requests[1].RequestUri?.ToString());
        Assert.Equal(RoomPhase.StorytellerChoosing, client.Room?.Phase);
        Assert.Equal("Bob", client.Room?.CurrentStoryteller?.Name);
    }

    [Fact]
    public async Task AdvanceRound_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var roundCompleteRoom = CreateRoundCompleteRoomWithScores("ABC123");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(roundCompleteRoom)
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        Detail = "La siguiente ronda aún no se puede iniciar."
                    })
                });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.AdvanceRoundAsync();

        Assert.Equal("La siguiente ronda aún no se puede iniciar.", client.ErrorMessage);
        Assert.False(client.IsAdvancingRound);
        Assert.Equal(RoomPhase.RoundComplete, client.Room?.Phase);
    }

    [Fact]
    public async Task AdvanceRound_TracksSendingStateWhileRequestIsPending()
    {
        var roundCompleteRoom = CreateRoundCompleteRoomWithScores("ABC123");
        var nextRoundRoom = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Bob",
            "Alice",
            "Bob",
            "Carla");
        var handler = new BlockingAdvanceRoundHttpMessageHandler(
            roundCompleteRoom,
            JsonResponse(nextRoundRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        var advanceRoundTask = client.AdvanceRoundAsync();
        await handler.AdvanceRoundRequestStarted.Task;

        Assert.True(client.IsAdvancingRound);

        handler.CompleteAdvanceRoundRequest();
        await advanceRoundTask;

        Assert.False(client.IsAdvancingRound);
        Assert.Equal(RoomPhase.StorytellerChoosing, client.Room?.Phase);
    }

    [Fact]
    public async Task StartNextRound_PostsToExpectedEndpointAndUsesReturnedSnapshot()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var nextRoundRoom = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Bob",
            "Alice",
            "Bob",
            "Carla");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(scoringRoom)
                : JsonResponse(nextRoundRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.StartNextRoundAsync();

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Post, handler.Requests[1].Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/next-round",
            handler.Requests[1].RequestUri?.ToString());
        Assert.Equal(RoomPhase.StorytellerChoosing, client.Room?.Phase);
        Assert.Equal("Bob", client.Room?.CurrentStoryteller?.Name);
    }

    [Fact]
    public async Task StartNextRound_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var handler = new RecordingHttpMessageHandler(request =>
            request.Method == HttpMethod.Get
                ? JsonResponse(scoringRoom)
                : new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        Detail = "La nueva ronda aún no se puede iniciar."
                    })
                });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        await client.StartNextRoundAsync();

        Assert.Equal("La nueva ronda aún no se puede iniciar.", client.ErrorMessage);
        Assert.False(client.IsStartingNextRound);
        Assert.Equal(RoomPhase.Scoring, client.Room?.Phase);
    }

    [Fact]
    public async Task StartNextRound_TracksSendingStateWhileRequestIsPending()
    {
        var scoringRoom = CreateScoringRoomWithScores("ABC123");
        var nextRoundRoom = CreateRoom(
            "ABC123",
            RoomPhase.StorytellerChoosing,
            "Bob",
            "Alice",
            "Bob",
            "Carla");
        var handler = new BlockingStartNextRoundHttpMessageHandler(
            scoringRoom,
            JsonResponse(nextRoundRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.InitializeAsync("ABC123");

        var startNextRoundTask = client.StartNextRoundAsync();
        await handler.StartNextRoundRequestStarted.Task;

        Assert.True(client.IsStartingNextRound);

        handler.CompleteStartNextRoundRequest();
        await startNextRoundTask;

        Assert.False(client.IsStartingNextRound);
        Assert.Equal(RoomPhase.StorytellerChoosing, client.Room?.Phase);
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

    [Fact]
    public void LobbyPresentation_GameCompleteUsesSnapshotWinnersAndScoreboard()
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();
        var room = new RoomStateResponse(
            "ABC123",
            RoomPhase.GameComplete,
            25,
            [
                new PlayerStateResponse(aliceId, "Alice"),
                new PlayerStateResponse(bobId, "Bob"),
                new PlayerStateResponse(carlaId, "Carla")
            ],
            null,
            "Un faro en la niebla",
            2,
            2,
            2,
            2,
            [],
            [],
            [
                new RoundScoreStateResponse(aliceId, "Alice", 3),
                new RoundScoreStateResponse(bobId, "Bob", 3),
                new RoundScoreStateResponse(carlaId, "Carla", 0)
            ],
            [
                new ScoreboardEntryStateResponse(aliceId, "Alice", 25),
                new ScoreboardEntryStateResponse(bobId, "Bob", 25),
                new ScoreboardEntryStateResponse(carlaId, "Carla", 12)
            ],
            [
                new WinningPlayerStateResponse(aliceId, "Alice", 25),
                new WinningPlayerStateResponse(bobId, "Bob", 25)
            ]);

        var winners = RoomLobbyPresentation.WinnersForDisplay(room).ToArray();
        var scoreboard = RoomLobbyPresentation.FinalScoreboardForDisplay(room).ToArray();

        Assert.Equal(25, room.TargetScore);
        Assert.Collection(
            winners,
            winner =>
            {
                Assert.Equal(aliceId, winner.PlayerId);
                Assert.Equal("Alice", winner.PlayerName);
                Assert.Equal(25, winner.TotalPoints);
            },
            winner =>
            {
                Assert.Equal(bobId, winner.PlayerId);
                Assert.Equal("Bob", winner.PlayerName);
                Assert.Equal(25, winner.TotalPoints);
            });
        Assert.Equal(["Alice", "Bob", "Carla"], scoreboard.Select(score => score.PlayerName));
        Assert.Equal([25, 25, 12], scoreboard.Select(score => score.TotalPoints));
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

    private static RoomStateResponse CreateScoringRoomWithScores(string roomCode)
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        return new RoomStateResponse(
            roomCode,
            RoomPhase.Scoring,
            25,
            [
                new PlayerStateResponse(aliceId, "Alice"),
                new PlayerStateResponse(bobId, "Bob"),
                new PlayerStateResponse(carlaId, "Carla")
            ],
            new PlayerStateResponse(aliceId, "Alice"),
            "Un faro en la niebla",
            2,
            2,
            2,
            2,
            [],
            [],
            [
                new RoundScoreStateResponse(aliceId, "Alice", 3),
                new RoundScoreStateResponse(bobId, "Bob", 4),
                new RoundScoreStateResponse(carlaId, "Carla", 0)
            ],
            [
                new ScoreboardEntryStateResponse(aliceId, "Alice", 3),
                new ScoreboardEntryStateResponse(bobId, "Bob", 4),
                new ScoreboardEntryStateResponse(carlaId, "Carla", 0)
            ],
            []);
    }

    private static RoomStateResponse CreateRoundCompleteRoomWithScores(string roomCode)
    {
        return CreateScoringRoomWithScores(roomCode) with
        {
            Phase = RoomPhase.RoundComplete
        };
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

    private sealed class BlockingScoreHttpMessageHandler(
        RoomStateResponse initialRoom,
        HttpResponseMessage scoreResponse)
        : HttpMessageHandler
    {
        private readonly TaskCompletionSource _completeScoreRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ScoreRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteScoreRequest()
        {
            _completeScoreRequest.SetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(initialRoom);
            }

            ScoreRequestStarted.SetResult();
            await _completeScoreRequest.Task.WaitAsync(cancellationToken);
            return scoreResponse;
        }
    }

    private sealed class BlockingCompleteRoundHttpMessageHandler(
        RoomStateResponse initialRoom,
        HttpResponseMessage completeRoundResponse)
        : HttpMessageHandler
    {
        private readonly TaskCompletionSource _completeRoundRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource CompleteRoundRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteRoundRequest()
        {
            _completeRoundRequest.SetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(initialRoom);
            }

            CompleteRoundRequestStarted.SetResult();
            await _completeRoundRequest.Task.WaitAsync(cancellationToken);
            return completeRoundResponse;
        }
    }

    private sealed class BlockingAdvanceRoundHttpMessageHandler(
        RoomStateResponse initialRoom,
        HttpResponseMessage advanceRoundResponse)
        : HttpMessageHandler
    {
        private readonly TaskCompletionSource _completeAdvanceRoundRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AdvanceRoundRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteAdvanceRoundRequest()
        {
            _completeAdvanceRoundRequest.SetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(initialRoom);
            }

            AdvanceRoundRequestStarted.SetResult();
            await _completeAdvanceRoundRequest.Task.WaitAsync(cancellationToken);
            return advanceRoundResponse;
        }
    }

    private sealed class BlockingStartNextRoundHttpMessageHandler(
        RoomStateResponse initialRoom,
        HttpResponseMessage startNextRoundResponse)
        : HttpMessageHandler
    {
        private readonly TaskCompletionSource _completeStartNextRoundRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource StartNextRoundRequestStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteStartNextRoundRequest()
        {
            _completeStartNextRoundRequest.SetResult();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse(initialRoom);
            }

            StartNextRoundRequestStarted.SetResult();
            await _completeStartNextRoundRequest.Task.WaitAsync(cancellationToken);
            return startNextRoundResponse;
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
