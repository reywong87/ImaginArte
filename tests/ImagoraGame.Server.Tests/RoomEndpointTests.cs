using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;
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
    public async Task PostStorytellerCard_WithValidRequest_ReturnsUpdatedPublicRoomState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId);
        var privateState = await GetPlayerPrivateStateAsync(client, "ABC123", aliceId);
        var chosenCardId = privateState.Player.Hand.First().CardId;

        var response = await client.PostAsJsonAsync(
            "/rooms/abc123/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                chosenCardId,
                "  A strange doorway  "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var room = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.GuessersSubmitting, room.Phase);
        Assert.Equal("A strange doorway", room.Clue);
        Assert.Equal(aliceId, room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Alice", room.CurrentStoryteller?.Name);
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(chosenCardId, responseBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostStorytellerCard_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/rooms/missing/storyteller-card",
            new ChooseStorytellerCardRequest(
                Guid.NewGuid(),
                "TEMP-001",
                "A strange doorway"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostStorytellerCard_WhenPlayerIsNotStoryteller_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId, bobId);
        var bobPrivateState = await GetPlayerPrivateStateAsync(client, "ABC123", bobId);

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(
                bobId,
                bobPrivateState.Player.Hand.First().CardId,
                "A strange doorway"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostStorytellerCard_WithEmptyClue_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var aliceId = Guid.NewGuid();

        await CreateStartedRoomAsync(client, "ABC123", aliceId);
        var privateState = await GetPlayerPrivateStateAsync(client, "ABC123", aliceId);

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                privateState.Player.Hand.First().CardId,
                string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WithFirstValidSubmission_ReturnsPublicSubmittingState()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/abc123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var state = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.GuessersSubmitting, state.Phase);
        Assert.Equal(1, state.SubmissionCount);
        Assert.Equal(2, state.ExpectedSubmissionCount);
        Assert.Empty(state.VotingCards);
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostGuessCards_WithLastSubmission_MovesToVotingWithoutRevealingCardOwnership()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.CarlaId, room.CarlaCardIds[0]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var state = ReadRequiredJson<RoomStateResponse>(responseBody);
        Assert.Equal(RoomPhase.Voting, state.Phase);
        Assert.Equal(state.ExpectedSubmissionCount, state.SubmissionCount);
        Assert.Equal(
            [room.StorytellerCardId, room.BobCardIds[0], room.CarlaCardIds[0]],
            state.VotingCards.Select(card => card.CardId).Order().ToArray());
        Assert.Equal(3, state.VotingCards.Select(card => card.DisplayNumber).Distinct().Count());
        Assert.All(
            state.VotingCards,
            card => Assert.InRange(card.DisplayNumber, 1, state.VotingCards.Count));
        Assert.DoesNotContain("\"hand\"", responseBody, StringComparison.OrdinalIgnoreCase);

        using var json = JsonDocument.Parse(responseBody);
        foreach (var votingCard in json.RootElement.GetProperty("votingCards").EnumerateArray())
        {
            Assert.False(votingCard.TryGetProperty("ownerPlayerId", out _));
            Assert.False(votingCard.TryGetProperty("ownerName", out _));
            Assert.False(votingCard.TryGetProperty("isStorytellerCard", out _));
        }
    }

    [Fact]
    public async Task PostGuessCards_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/rooms/missing/guess-cards",
            new SubmitGuessCardRequest(Guid.NewGuid(), "TEMP-001"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenStorytellerSubmits_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.AliceId, room.StorytellerCardId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenGuesserSubmitsTwice_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var firstResponse = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[0]));
        firstResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.BobCardIds[1]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
    }

    [Fact]
    public async Task PostGuessCards_WhenCardIsNotInPlayerHand_ReturnsBadRequest()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();
        var room = await PrepareGuessSubmissionRoomAsync(client, "ABC123");

        var response = await client.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(room.BobId, room.CarlaCardIds[0]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, problem.Status);
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
    public async Task GetPlayerPrivateState_ReturnsPublicRoomAndPrivatePlayerSnapshot()
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

        var responseBody = await response.Content.ReadAsStringAsync();
        var snapshot = ReadRequiredJson<GetPlayerPrivateStateResponse>(responseBody);

        Assert.Equal("ABC123", snapshot.Room.RoomCode);
        Assert.Equal(RoomPhase.StorytellerChoosing, snapshot.Room.Phase);
        Assert.Equal(3, snapshot.Room.Players.Count);
        Assert.Equal(bobId, snapshot.Player.PlayerId);
        Assert.Equal("Bob", snapshot.Player.Name);
        Assert.False(snapshot.Player.IsCurrentStoryteller);
        Assert.Collection(
            snapshot.Player.Hand,
            card => Assert.Equal("TEMP-007", card.CardId),
            card => Assert.Equal("TEMP-008", card.CardId),
            card => Assert.Equal("TEMP-009", card.CardId),
            card => Assert.Equal("TEMP-010", card.CardId),
            card => Assert.Equal("TEMP-011", card.CardId),
            card => Assert.Equal("TEMP-012", card.CardId));

        using var json = JsonDocument.Parse(responseBody);
        var roomJson = json.RootElement.GetProperty("room").GetRawText();
        Assert.DoesNotContain("\"hand\"", roomJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TEMP-", roomJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPlayerPrivateState_WhenPlayerDoesNotExist_ReturnsNotFound()
    {
        using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        await CreateRoomAsync(client, "ABC123");

        var response = await client.GetAsync($"/rooms/ABC123/players/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await ReadRequiredJsonAsync<ProblemDetails>(response);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
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

    private static async Task CreateStartedRoomAsync(
        HttpClient client,
        string roomCode,
        Guid storytellerId,
        Guid? secondPlayerId = null)
    {
        await CreateRoomAsync(client, roomCode);
        await JoinPlayerAsync(client, roomCode, storytellerId, "Alice");
        await JoinPlayerAsync(client, roomCode, secondPlayerId ?? Guid.NewGuid(), "Bob");
        await JoinPlayerAsync(client, roomCode, Guid.NewGuid(), "Carla");

        var response = await client.PostAsync($"/rooms/{roomCode}/start", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<GuessSubmissionRoom> PrepareGuessSubmissionRoomAsync(
        HttpClient client,
        string roomCode)
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(client, roomCode);
        await JoinPlayerAsync(client, roomCode, aliceId, "Alice");
        await JoinPlayerAsync(client, roomCode, bobId, "Bob");
        await JoinPlayerAsync(client, roomCode, carlaId, "Carla");

        var startResponse = await client.PostAsync($"/rooms/{roomCode}/start", content: null);
        startResponse.EnsureSuccessStatusCode();

        var aliceState = await GetPlayerPrivateStateAsync(client, roomCode, aliceId);
        var bobState = await GetPlayerPrivateStateAsync(client, roomCode, bobId);
        var carlaState = await GetPlayerPrivateStateAsync(client, roomCode, carlaId);
        var storytellerCardId = aliceState.Player.Hand.First().CardId;

        var storytellerResponse = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                storytellerCardId,
                "A strange doorway"));
        storytellerResponse.EnsureSuccessStatusCode();

        return new GuessSubmissionRoom(
            aliceId,
            bobId,
            carlaId,
            storytellerCardId,
            bobState.Player.Hand.Select(card => card.CardId).ToArray(),
            carlaState.Player.Hand.Select(card => card.CardId).ToArray());
    }

    private static async Task<GetPlayerPrivateStateResponse> GetPlayerPrivateStateAsync(
        HttpClient client,
        string roomCode,
        Guid playerId)
    {
        var response = await client.GetAsync($"/rooms/{roomCode}/players/{playerId}");
        response.EnsureSuccessStatusCode();
        return await ReadRequiredJsonAsync<GetPlayerPrivateStateResponse>(response);
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

    private sealed record GuessSubmissionRoom(
        Guid AliceId,
        Guid BobId,
        Guid CarlaId,
        string StorytellerCardId,
        IReadOnlyList<string> BobCardIds,
        IReadOnlyList<string> CarlaCardIds);
}
