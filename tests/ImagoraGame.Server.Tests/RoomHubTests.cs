using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Contracts.Rooms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace ImagoraGame.Server.Tests;

public sealed class RoomHubTests
{
    [Fact]
    public async Task Client_CanConnectToRoomsHub()
    {
        using var application = CreateApplication();
        await using var connection = CreateHubConnection(application);

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task SubscribeToRoom_ForExistingRoom_ReceivesNormalizedPublicSnapshotWithoutHands()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", Guid.NewGuid(), "Alice");
        await using var connection = CreateHubConnection(application);
        var roomUpdated = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);

        await connection.StartAsync();
        await SubscribeToRoomAsync(connection, " abc123 ");
        var receivedEvent = await roomUpdated.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("ABC123", receivedEvent.Room.RoomCode);
        Assert.Single(receivedEvent.Room.Players);
        Assert.DoesNotContain(
            "\"hand\"",
            JsonSerializer.Serialize(receivedEvent),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubscribeAsPlayer_ReceivesPublicAndCorrectPrivateSnapshots()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", Guid.NewGuid(), "Carla");
        await StartRoomAsync(httpClient, "ABC123");
        await using var connection = CreateHubConnection(application);
        var roomUpdated = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);
        var playerUpdated = WaitForEvent<PlayerStateUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        await connection.StartAsync();
        await SubscribeAsPlayerAsync(connection, "abc123", bobId);
        var publicEvent = await roomUpdated.WaitAsync(TimeSpan.FromSeconds(5));
        var privateEvent = await playerUpdated.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("ABC123", publicEvent.Room.RoomCode);
        Assert.Equal(bobId, privateEvent.State.Player.PlayerId);
        Assert.Equal("Bob", privateEvent.State.Player.Name);
        Assert.Collection(
            privateEvent.State.Player.Hand,
            card => Assert.Equal("TEMP-007", card.CardId),
            card => Assert.Equal("TEMP-008", card.CardId),
            card => Assert.Equal("TEMP-009", card.CardId),
            card => Assert.Equal("TEMP-010", card.CardId),
            card => Assert.Equal("TEMP-011", card.CardId),
            card => Assert.Equal("TEMP-012", card.CardId));
        Assert.DoesNotContain(
            "\"hand\"",
            JsonSerializer.Serialize(privateEvent.State.Room),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubscribeToRoom_WhenRoomDoesNotExist_FailsWithHubException()
    {
        using var application = CreateApplication();
        await using var connection = CreateHubConnection(application);
        await connection.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => SubscribeToRoomAsync(connection, "missing"));

        Assert.Contains("Room 'MISSING' was not found.", exception.Message);
    }

    [Fact]
    public async Task SubscribeAsPlayer_WhenPlayerDoesNotExist_FailsWithHubException()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        await CreateRoomAsync(httpClient, "ABC123");
        await using var connection = CreateHubConnection(application);
        await connection.StartAsync();
        var missingPlayerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => SubscribeAsPlayerAsync(connection, "ABC123", missingPlayerId));

        Assert.Contains($"Player '{missingPlayerId}' was not found", exception.Message);
    }

    [Fact]
    public async Task UppercaseAndLowercaseCodes_AddressTheSameLogicalRoom()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        await CreateRoomAsync(httpClient, "ABC123");
        await using var uppercaseConnection = CreateHubConnection(application);
        await using var lowercaseConnection = CreateHubConnection(application);
        var uppercaseEvent = WaitForEvent<RoomUpdatedEvent>(
            uppercaseConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var lowercaseEvent = WaitForEvent<RoomUpdatedEvent>(
            lowercaseConnection,
            RoomRealtimeEventNames.RoomUpdated);

        await uppercaseConnection.StartAsync();
        await lowercaseConnection.StartAsync();
        await SubscribeToRoomAsync(uppercaseConnection, "ABC123");
        await SubscribeToRoomAsync(lowercaseConnection, "abc123");

        var uppercaseRoom = (await uppercaseEvent.WaitAsync(TimeSpan.FromSeconds(5))).Room;
        var lowercaseRoom = (await lowercaseEvent.WaitAsync(TimeSpan.FromSeconds(5))).Room;
        Assert.Equal("ABC123", uppercaseRoom.RoomCode);
        Assert.Equal(
            JsonSerializer.Serialize(uppercaseRoom),
            JsonSerializer.Serialize(lowercaseRoom));
    }

    [Fact]
    public async Task Subscriptions_DoNotModifyRoomState()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var playerId = Guid.NewGuid();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", playerId, "Alice");
        var before = await GetRoomAsync(httpClient, "ABC123");
        await using var connection = CreateHubConnection(application);
        var publicEvent = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);
        var privateEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        await connection.StartAsync();
        await SubscribeToRoomAsync(connection, "abc123");
        await publicEvent.WaitAsync(TimeSpan.FromSeconds(5));
        await SubscribeAsPlayerAsync(connection, "ABC123", playerId);
        await privateEvent.WaitAsync(TimeSpan.FromSeconds(5));
        var after = await GetRoomAsync(httpClient, "ABC123");

        Assert.Equal(
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(after));
    }

    private static HubConnection CreateHubConnection(
        WebApplicationFactory<Program> application)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(application.Server.BaseAddress, "/hubs/rooms"),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => application.Server.CreateHandler();
                })
            .Build();
    }

    private static WebApplicationFactory<Program> CreateApplication()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.Configure<HubOptions>(
                        options => options.EnableDetailedErrors = true)));
    }

    private static Task<TEvent> WaitForEvent<TEvent>(
        HubConnection connection,
        string eventName)
    {
        var completion = new TaskCompletionSource<TEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TEvent>(eventName, value => completion.TrySetResult(value));
        return completion.Task;
    }

    private static Task SubscribeToRoomAsync(
        HubConnection connection,
        string roomCode)
    {
        return connection.InvokeAsync("SubscribeToRoom", roomCode);
    }

    private static Task SubscribeAsPlayerAsync(
        HubConnection connection,
        string roomCode,
        Guid playerId)
    {
        return connection.InvokeAsync("SubscribeAsPlayer", roomCode, playerId);
    }

    private static async Task CreateRoomAsync(HttpClient client, string roomCode)
    {
        var response = await client.PostAsJsonAsync(
            "/rooms",
            new CreateRoomRequest(roomCode));
        response.EnsureSuccessStatusCode();
    }

    private static async Task JoinPlayerAsync(
        HttpClient client,
        string roomCode,
        Guid playerId,
        string playerName)
    {
        var response = await client.PostAsJsonAsync(
            $"/rooms/{roomCode}/players",
            new JoinRoomRequest(playerId, playerName));
        response.EnsureSuccessStatusCode();
    }

    private static async Task StartRoomAsync(HttpClient client, string roomCode)
    {
        var response = await client.PostAsync($"/rooms/{roomCode}/start", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<RoomStateResponse> GetRoomAsync(
        HttpClient client,
        string roomCode)
    {
        var room = await client.GetFromJsonAsync<RoomStateResponse>($"/rooms/{roomCode}");
        Assert.NotNull(room);
        return room;
    }
}
