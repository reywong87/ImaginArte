using System.Net;
using System.Net.Http.Json;
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
    public async Task PlayerStateUpdated_WhenGuesserHasSubmittedCard_ExposesWaitingState()
    {
        var playerId = Guid.NewGuid();
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.GuessersSubmitting,
            isCurrentStoryteller: false,
            hasSubmittedGuessCard: true)));

        Assert.True(client.HasSubmittedGuessCard);
    }

    [Fact]
    public async Task PlayerStateUpdated_WhenVoting_IdentifiesOwnSubmittedCardAsNotVotable()
    {
        var playerId = Guid.NewGuid();
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.Voting,
            isCurrentStoryteller: false,
            submittedVotingCardId: "vote-2",
            votingCards:
            [
                new VotingCardStateResponse("vote-1", "card-1", 1, []),
                new VotingCardStateResponse("vote-2", "card-2", 2, [])
            ])));

        Assert.False(client.IsOwnSubmittedVotingCard("vote-1"));
        Assert.True(client.CanVoteForCard("vote-1"));
        Assert.True(client.IsOwnSubmittedVotingCard("vote-2"));
        Assert.False(client.CanVoteForCard("vote-2"));
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

    [Fact]
    public async Task ChooseStorytellerCardAsync_PostsToExpectedEndpointAndUsesRequest()
    {
        var playerId = Guid.NewGuid();
        var updatedRoom = CreateRoom("ABC123", playerId, "Alice", RoomPhase.GuessersSubmitting);
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(updatedRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Alice"));
        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Alice",
            RoomPhase.StorytellerChoosing,
            isCurrentStoryteller: true,
            hand: [new CardStateResponse("card-1")])));

        await client.ChooseStorytellerCardAsync("card-1", "  Una pista  ");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/storyteller-card",
            request.RequestUri?.ToString());

        var chooseRequest = await request.Content!.ReadFromJsonAsync<ChooseStorytellerCardRequest>();
        Assert.Equal(playerId, chooseRequest!.PlayerId);
        Assert.Equal("card-1", chooseRequest.CardId);
        Assert.Equal("Una pista", chooseRequest.Clue);
        Assert.Equal(RoomPhase.GuessersSubmitting, client.PrivateState!.Room.Phase);
        Assert.Null(client.ErrorMessage);
    }

    [Fact]
    public async Task ChooseStorytellerCardAsync_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new
                {
                    Detail = "Solo el narrador puede elegir carta."
                })
            });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Alice"));

        await client.ChooseStorytellerCardAsync("card-1", "Pista");

        Assert.Equal("Solo el narrador puede elegir carta.", client.ErrorMessage);
        Assert.False(client.IsChoosingStorytellerCard);
    }

    [Fact]
    public async Task ChooseStorytellerCardAsync_SetsSendingStateWhileRequestIsInFlight()
    {
        var playerId = Guid.NewGuid();
        var responseSource = new TaskCompletionSource<HttpResponseMessage>();
        var handler = new RecordingHttpMessageHandler(_ => responseSource.Task);
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Alice"));

        var chooseTask = client.ChooseStorytellerCardAsync("card-1", "Pista");

        Assert.True(client.IsChoosingStorytellerCard);

        responseSource.SetResult(JsonResponse(CreateRoom(
            "ABC123",
            playerId,
            "Alice",
            RoomPhase.GuessersSubmitting)));
        await chooseTask;

        Assert.False(client.IsChoosingStorytellerCard);
    }

    [Fact]
    public async Task SubmitGuessCardAsync_PostsToExpectedEndpointAndUsesRequest()
    {
        var playerId = Guid.NewGuid();
        var updatedRoom = CreateRoom("ABC123", playerId, "Bob", RoomPhase.Voting);
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(updatedRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));
        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.GuessersSubmitting,
            isCurrentStoryteller: false,
            hand: [new CardStateResponse("card-2")])));

        await client.SubmitGuessCardAsync("card-2");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/guess-cards",
            request.RequestUri?.ToString());

        var submitRequest = await request.Content!.ReadFromJsonAsync<SubmitGuessCardRequest>();
        Assert.Equal(playerId, submitRequest!.PlayerId);
        Assert.Equal("card-2", submitRequest.CardId);
        Assert.Equal(RoomPhase.Voting, client.PrivateState!.Room.Phase);
        Assert.Null(client.ErrorMessage);
    }

    [Fact]
    public async Task SubmitGuessCardAsync_WhenRoomRemainsGuessersSubmitting_MarksPlayerAsSubmitted()
    {
        var playerId = Guid.NewGuid();
        var updatedRoom = CreateRoom("ABC123", playerId, "Bob", RoomPhase.GuessersSubmitting);
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(updatedRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));
        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.GuessersSubmitting,
            isCurrentStoryteller: false,
            hasSubmittedGuessCard: false,
            hand: [new CardStateResponse("card-2")])));

        await client.SubmitGuessCardAsync("card-2");

        Assert.Equal(RoomPhase.GuessersSubmitting, client.PrivateState!.Room.Phase);
        Assert.True(client.HasSubmittedGuessCard);
        Assert.True(client.PrivateState.Player.HasSubmittedGuessCard);
        Assert.Null(client.ErrorMessage);
    }

    [Fact]
    public async Task SubmitGuessCardAsync_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new
                {
                    Detail = "Solo los adivinadores pueden entregar carta."
                })
            });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        await client.SubmitGuessCardAsync("card-2");

        Assert.Equal("Solo los adivinadores pueden entregar carta.", client.ErrorMessage);
        Assert.False(client.IsSubmittingGuessCard);
    }

    [Fact]
    public async Task SubmitGuessCardAsync_SetsSendingStateWhileRequestIsInFlight()
    {
        var playerId = Guid.NewGuid();
        var responseSource = new TaskCompletionSource<HttpResponseMessage>();
        var handler = new RecordingHttpMessageHandler(_ => responseSource.Task);
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        var submitTask = client.SubmitGuessCardAsync("card-2");

        Assert.True(client.IsSubmittingGuessCard);

        responseSource.SetResult(JsonResponse(CreateRoom(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.Voting)));
        await submitTask;

        Assert.False(client.IsSubmittingGuessCard);
    }

    [Fact]
    public async Task CastVoteAsync_PostsToExpectedEndpointAndUsesRequest()
    {
        var playerId = Guid.NewGuid();
        var updatedRoom = CreateRoom("ABC123", playerId, "Bob", RoomPhase.Voting);
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(updatedRoom));
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));
        realtime.Publish(new PlayerStateUpdatedEvent(CreatePrivateState(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.Voting,
            isCurrentStoryteller: false,
            votingCards:
            [
                new VotingCardStateResponse("vote-1", "card-1", 1, [])
            ])));

        await client.CastVoteAsync("vote-1");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(
            "http://localhost:5080/rooms/ABC123/votes",
            request.RequestUri?.ToString());

        var voteRequest = await request.Content!.ReadFromJsonAsync<CastVoteRequest>();
        Assert.Equal(playerId, voteRequest!.PlayerId);
        Assert.Equal("vote-1", voteRequest.VotingCardId);
        Assert.Equal(RoomPhase.Voting, client.PrivateState!.Room.Phase);
        Assert.Null(client.ErrorMessage);
    }

    [Fact]
    public async Task CastVoteAsync_WhenServerRejectsOperation_ExposesProblemDetail()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new
                {
                    Detail = "No puedes votar tu propia carta."
                })
            });
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        await client.CastVoteAsync("vote-1");

        Assert.Equal("No puedes votar tu propia carta.", client.ErrorMessage);
        Assert.False(client.IsCastingVote);
    }

    [Fact]
    public async Task CastVoteAsync_SetsSendingStateWhileRequestIsInFlight()
    {
        var playerId = Guid.NewGuid();
        var responseSource = new TaskCompletionSource<HttpResponseMessage>();
        var handler = new RecordingHttpMessageHandler(_ => responseSource.Task);
        var realtime = new FakeRoomRealtimeConnection();
        await using var client = CreateClient(handler, realtime);
        await client.ConnectAsync(new MobileLobbyState("ABC123", playerId, "Bob"));

        var voteTask = client.CastVoteAsync("vote-1");

        Assert.True(client.IsCastingVote);

        responseSource.SetResult(JsonResponse(CreateRoom(
            "ABC123",
            playerId,
            "Bob",
            RoomPhase.Voting)));
        await voteTask;

        Assert.False(client.IsCastingVote);
    }

    private static MobileRoomClient CreateClient(FakeRoomRealtimeConnection realtime)
    {
        return CreateClient(
            new RecordingHttpMessageHandler(_ => JsonResponse(CreateRoom(
                "ABC123",
                Guid.NewGuid(),
                "Alice",
                RoomPhase.WaitingForPlayers))),
            realtime);
    }

    private static MobileRoomClient CreateClient(
        HttpMessageHandler handler,
        FakeRoomRealtimeConnection realtime)
    {
        var baseUri = new Uri("http://localhost:5080/");
        return new MobileRoomClient(
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
        Guid playerId,
        string playerName,
        RoomPhase phase)
    {
        var player = new PlayerStateResponse(playerId, playerName);
        return new RoomStateResponse(
            roomCode,
            phase,
            25,
            [player],
            CurrentStoryteller: phase == RoomPhase.StorytellerChoosing ? player : null,
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
    }

    private static GetPlayerPrivateStateResponse CreatePrivateState(
        string roomCode,
        Guid playerId,
        string playerName,
        RoomPhase phase = RoomPhase.WaitingForPlayers,
        bool isCurrentStoryteller = false,
        bool hasSubmittedGuessCard = false,
        string? submittedVotingCardId = null,
        IReadOnlyCollection<CardStateResponse>? hand = null,
        IReadOnlyCollection<VotingCardStateResponse>? votingCards = null)
    {
        var player = new PlayerStateResponse(playerId, playerName);
        var room = new RoomStateResponse(
            roomCode,
            phase,
            25,
            [player],
            CurrentStoryteller: isCurrentStoryteller ? player : null,
            Clue: null,
            SubmissionCount: 0,
            ExpectedSubmissionCount: 0,
            VoteCount: 0,
            ExpectedVoteCount: 0,
            VotingCards: votingCards ?? [],
            RevealedCards: [],
            RoundScores: [],
            Scoreboard: [],
            Winners: []);

        return new GetPlayerPrivateStateResponse(
            room,
            new PlayerPrivateStateResponse(
                playerId,
                playerName,
                Hand: hand ?? [],
                IsCurrentStoryteller: isCurrentStoryteller,
                HasSubmittedGuessCard: hasSubmittedGuessCard,
                SubmittedVotingCardId: submittedVotingCardId));
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        public RecordingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
            : this(request => Task.FromResult(responseFactory(request)))
        {
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return responseFactory(request);
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
