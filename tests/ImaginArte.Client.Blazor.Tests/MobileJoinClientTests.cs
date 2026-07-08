using System.Net;
using System.Net.Http.Json;
using ImaginArte.Client.Blazor.Rooms;
using ImaginArte.Contracts.Rooms;

namespace ImaginArte.Client.Blazor.Tests;

public sealed class MobileJoinClientTests
{
    [Fact]
    public async Task JoinAsync_PostsJoinRequestAndReturnsMobileLobbyState()
    {
        var playerId = Guid.NewGuid();
        var room = CreateRoom("ABC123", new PlayerStateResponse(playerId, "Alice"));
        var handler = new RecordingHttpMessageHandler(_ => JsonResponse(room));
        var client = CreateClient(handler);

        var lobby = await client.JoinAsync(" abc123 ", " Alice ", playerId);

        Assert.Equal("ABC123", lobby.RoomCode);
        Assert.Equal(playerId, lobby.PlayerId);
        Assert.Equal("Alice", lobby.PlayerName);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("http://localhost:5080/rooms/ABC123/players", request.RequestUri?.ToString());

        var joinRequest = await request.Content!.ReadFromJsonAsync<JoinRoomRequest>();
        Assert.Equal(playerId, joinRequest!.PlayerId);
        Assert.Equal("Alice", joinRequest.PlayerName);
    }

    [Fact]
    public async Task JoinAsync_WhenServerRejectsJoin_ExposesProblemDetail()
    {
        var handler = new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new
                {
                    Detail = "No existe una sala con ese código."
                })
            });
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.JoinAsync("ABC123", "Alice", Guid.NewGuid()));

        Assert.Equal("No existe una sala con ese código.", exception.Message);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    private static MobileJoinClient CreateClient(HttpMessageHandler handler)
    {
        return new MobileJoinClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5080/")
        });
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
}
