using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Application.Rooms;
using ImagoraGame.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ImagoraGame.Server.Tests;

public sealed class RoomEndpointTests
{
    [Fact]
    public async Task PostRooms_CreatesRoomAndNormalizesRoomCode()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = " family-room "
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/rooms/FAMILY-ROOM", response.Headers.Location?.OriginalString);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal("FAMILY-ROOM", room.RoomCode);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
        Assert.Empty(room.Players);
    }

    [Fact]
    public async Task PostRooms_WithDuplicateRoomCode_ReturnsConflict()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = "abc123"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
    }

    [Fact]
    public async Task PostPlayers_JoinsPlayerAndReturnsUpdatedRoomState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var playerId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync("/rooms/abc123/players", new
        {
            PlayerId = playerId,
            PlayerName = " Alice "
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal("ABC123", room.RoomCode);
        Assert.Collection(
            room.Players,
            player =>
            {
                Assert.Equal(playerId, player.PlayerId);
                Assert.Equal("Alice", player.Name);
            });
    }

    [Fact]
    public async Task PostStart_WithFewerThanThreePlayers_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");

        var response = await client.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostStart_WithThreePlayers_MovesRoomToStorytellerChoosing()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");

        var response = await client.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var room = await ReadRequiredJsonAsync<RoomState>(response);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
        Assert.Equal(aliceId, room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Alice", room.CurrentStoryteller?.Name);
    }

    [Fact]
    public async Task GetRoom_ReturnsPublicRoomStateWithoutPrivateHands()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");
        await client.PostAsync("/rooms/ABC123/start", content: null);

        var response = await client.GetAsync("/rooms/abc123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var room = ReadRequiredJson<RoomState>(responseBody);
        Assert.Equal("ABC123", room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);

        using var json = JsonDocument.Parse(responseBody);
        Assert.False(json.RootElement.TryGetProperty("hand", out _));
        Assert.DoesNotContain("TEMP-", responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPlayerPrivateState_ReturnsPlayerSnapshotWithHand()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var bobId = Guid.NewGuid();

        await CreateRoomAsync(client, "ABC123");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Alice");
        await JoinPlayerAsync(client, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(client, "ABC123", Guid.NewGuid(), "Carla");
        await client.PostAsync("/rooms/ABC123/start", content: null);

        var response = await client.GetAsync($"/rooms/abc123/players/{bobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var player = await ReadRequiredJsonAsync<PlayerPrivateState>(response);
        Assert.Equal(bobId, player.PlayerId);
        Assert.Equal("Bob", player.Name);
        Assert.False(player.IsCurrentStoryteller);
        Assert.Collection(
            player.Hand,
            card => Assert.Equal("TEMP-007", card.CardId),
            card => Assert.Equal("TEMP-008", card.CardId),
            card => Assert.Equal("TEMP-009", card.CardId),
            card => Assert.Equal("TEMP-010", card.CardId),
            card => Assert.Equal("TEMP-011", card.CardId),
            card => Assert.Equal("TEMP-012", card.CardId));
    }

    private static async Task<RoomState> CreateRoomAsync(HttpClient client, string roomCode)
    {
        var response = await client.PostAsJsonAsync("/rooms", new
        {
            RoomCode = roomCode
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<RoomState>(response);
    }

    private static async Task<RoomState> JoinPlayerAsync(
        HttpClient client,
        string roomCode,
        Guid playerId,
        string playerName)
    {
        var response = await client.PostAsJsonAsync($"/rooms/{roomCode}/players", new
        {
            PlayerId = playerId,
            PlayerName = playerName
        });

        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<RoomState>(response);
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        return ReadRequiredJson<T>(responseBody);
    }

    private static T ReadRequiredJson<T>(string responseBody)
    {
        var value = JsonSerializer.Deserialize<T>(responseBody, JsonSerializerOptions.Web);
        Assert.NotNull(value);
        return value;
    }
}
