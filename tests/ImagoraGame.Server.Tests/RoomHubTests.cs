using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImagoraGame.Application.Rooms;
using ImagoraGame.Contracts.Rooms;
using ImagoraGame.Domain;
using ImagoraGame.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

    [Fact]
    public async Task PostPlayers_BroadcastsUpdatedPublicRoomOnlyToSubscribedRoom()
    {
        var repository = new CountingRoomRepository();
        using var application = CreateApplication(repository);
        using var httpClient = application.CreateClient();
        await CreateRoomAsync(httpClient, "ABC123");
        await CreateRoomAsync(httpClient, "OTHER");
        await using var roomConnection = CreateHubConnection(application);
        await using var otherRoomConnection = CreateHubConnection(application);
        var initialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            roomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var initialOtherRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            otherRoomConnection,
            RoomRealtimeEventNames.RoomUpdated);

        await roomConnection.StartAsync();
        await otherRoomConnection.StartAsync();
        await SubscribeToRoomAsync(roomConnection, "abc123");
        await SubscribeToRoomAsync(otherRoomConnection, "other");
        await initialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5));
        await initialOtherRoomEvent.WaitAsync(TimeSpan.FromSeconds(5));

        var roomUpdated = WaitForEvent<RoomUpdatedEvent>(
            roomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var otherRoomUpdated = WaitForEvent<RoomUpdatedEvent>(
            otherRoomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var playerId = Guid.NewGuid();

        var response = await httpClient.PostAsJsonAsync(
            "/rooms/abc123/players",
            new JoinRoomRequest(playerId, " Alice "));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseRoom = await response.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(responseRoom);

        var receivedEvent = await roomUpdated.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("ABC123", receivedEvent.Room.RoomCode);
        Assert.Collection(
            receivedEvent.Room.Players,
            player =>
            {
                Assert.Equal(playerId, player.PlayerId);
                Assert.Equal("Alice", player.Name);
            });
        Assert.Equal(
            JsonSerializer.Serialize(responseRoom),
            JsonSerializer.Serialize(receivedEvent.Room));
        Assert.DoesNotContain(
            "\"hand\"",
            JsonSerializer.Serialize(receivedEvent),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, repository.SaveCount);

        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(otherRoomUpdated.IsCompleted);
    }

    [Fact]
    public async Task PostPlayers_WhenJoinFails_DoesNotBroadcastRoomUpdated()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var playerId = Guid.NewGuid();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", playerId, "Alice");
        await using var connection = CreateHubConnection(application);
        var initialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);

        await connection.StartAsync();
        await SubscribeToRoomAsync(connection, "ABC123");
        await initialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5));

        var unexpectedUpdate = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);

        var response = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/players",
            new JoinRoomRequest(playerId, "Alice Again"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        Assert.False(unexpectedUpdate.IsCompleted);
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

    private static WebApplicationFactory<Program> CreateApplication(
        IRoomRepository repository)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRoomRepository>();
                    services.AddSingleton(repository);
                    services.Configure<HubOptions>(
                        options => options.EnableDetailedErrors = true);
                }));
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

    private sealed class CountingRoomRepository : IRoomRepository
    {
        private readonly InMemoryRoomRepository _inner = new();

        public int SaveCount { get; private set; }

        public Task<Room?> GetByCodeAsync(
            RoomCode code,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetByCodeAsync(code, cancellationToken);
        }

        public Task AddAsync(
            Room room,
            CancellationToken cancellationToken = default)
        {
            return _inner.AddAsync(room, cancellationToken);
        }

        public async Task SaveAsync(
            Room room,
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            await _inner.SaveAsync(room, cancellationToken);
        }
    }
}
