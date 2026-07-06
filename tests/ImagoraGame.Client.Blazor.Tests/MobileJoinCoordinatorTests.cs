using System.Net;
using System.Net.Http.Json;
using ImagoraGame.Client.Blazor.Rooms;
using ImagoraGame.Contracts.Rooms;

namespace ImagoraGame.Client.Blazor.Tests;

public sealed class MobileJoinCoordinatorTests
{
    [Fact]
    public async Task JoinNewAsync_WhenNoSession_PostsJoinRequestAndSavesSession()
    {
        var room = CreateRoom("ABC123", new PlayerStateResponse(Guid.NewGuid(), "Alice"));
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(room));
        var storage = new FakeBrowserStorage();
        var coordinator = CreateCoordinator(handler, storage);

        var result = await coordinator.JoinNewAsync(" abc123 ", " Alice ");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://localhost:5080/rooms/ABC123/players", request.RequestUri?.ToString());
        Assert.Equal(result.Lobby.PlayerId, (await CreateStore(storage).LoadAsync("ABC123"))!.PlayerId);
        Assert.Equal("Alice", (await CreateStore(storage).LoadAsync("ABC123"))!.PlayerName);
    }

    [Fact]
    public async Task TryResumeAsync_WhenSessionExists_GetsPrivateStateAndDoesNotPostJoin()
    {
        var playerId = Guid.NewGuid();
        var privateState = CreatePrivateState("ABC123", playerId, "Alice");
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(privateState));
        var storage = new FakeBrowserStorage();
        var store = CreateStore(storage);
        await store.SaveAsync(new MobilePlayerSession("ABC123", playerId, "Alice"));
        var coordinator = CreateCoordinator(handler, storage);

        var result = await coordinator.TryResumeAsync(" abc123 ");

        Assert.NotNull(result);
        Assert.Equal(playerId, result.Lobby.PlayerId);
        Assert.Equal("Alice", result.Lobby.PlayerName);
        Assert.Equal(privateState.Player.PlayerId, result.PrivateState!.Player.PlayerId);
        Assert.Equal(privateState.Room.RoomCode, result.PrivateState.Room.RoomCode);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            $"http://localhost:5080/rooms/ABC123/players/{playerId:D}",
            request.RequestUri?.ToString());
    }

    [Fact]
    public async Task TryResumeAsync_WhenServerNoLongerRecognizesPlayer_ClearsSession()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new { Detail = "No existe ese jugador." })
            });
        var storage = new FakeBrowserStorage();
        var store = CreateStore(storage);
        await store.SaveAsync(new MobilePlayerSession("ABC123", playerId, "Alice"));
        var coordinator = CreateCoordinator(handler, storage);

        var result = await coordinator.TryResumeAsync("ABC123");

        Assert.Null(result);
        Assert.Null(await store.LoadAsync("ABC123"));
        Assert.DoesNotContain("imagora.rooms.ABC123.player", storage.Items.Keys);
    }

    private static MobileJoinCoordinator CreateCoordinator(
        HttpMessageHandler handler,
        FakeBrowserStorage storage)
    {
        return new MobileJoinCoordinator(
            new MobileJoinClient(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5080/")
            }),
            CreateStore(storage));
    }

    private static MobilePlayerSessionStore CreateStore(FakeBrowserStorage storage)
    {
        return new MobilePlayerSessionStore(storage);
    }

    private static HttpResponseMessage JsonResponse(RoomStateResponse room)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(room)
        };
    }

    private static HttpResponseMessage JsonResponse(GetPlayerPrivateStateResponse privateState)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(privateState)
        };
    }

    private static RoomStateResponse CreateRoom(
        string roomCode,
        params PlayerStateResponse[] players)
    {
        return new RoomStateResponse(
            roomCode,
            RoomPhase.WaitingForPlayers,
            25,
            players,
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
    }

    private static GetPlayerPrivateStateResponse CreatePrivateState(
        string roomCode,
        Guid playerId,
        string playerName)
    {
        return new GetPlayerPrivateStateResponse(
            CreateRoom(roomCode, new PlayerStateResponse(playerId, playerName)),
            new PlayerPrivateStateResponse(
                playerId,
                playerName,
                Hand: [],
                IsCurrentStoryteller: false,
                HasSubmittedGuessCard: false,
                SubmittedVotingCardId: null));
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

    private sealed class FakeBrowserStorage : IBrowserStorage
    {
        public Dictionary<string, string> Items { get; } = [];

        public ValueTask<string?> GetItemAsync(string key)
        {
            Items.TryGetValue(key, out var value);
            return ValueTask.FromResult<string?>(value);
        }

        public ValueTask SetItemAsync(string key, string value)
        {
            Items[key] = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemAsync(string key)
        {
            Items.Remove(key);
            return ValueTask.CompletedTask;
        }
    }
}
