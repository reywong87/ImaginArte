using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ImaginArte.Application.Rooms;
using ImaginArte.Contracts.Rooms;
using ImaginArte.Domain;
using ImaginArte.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ImaginArte.Server.Tests;

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
    public async Task SubscribeAsPlayer_UpdatesPublicPresenceOnConnectDisconnectAndReconnect()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var playerId = Guid.NewGuid();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", playerId, "Alice");

        await using var tvConnection = CreateHubConnection(application);
        await using var playerConnection = CreateHubConnection(application);
        var initialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);

        await tvConnection.StartAsync();
        await SubscribeToRoomAsync(tvConnection, "ABC123");
        var initialRoom = await initialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(Assert.Single(initialRoom.Room.Players).IsConnected);

        var connectedEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await playerConnection.StartAsync();
        await SubscribeAsPlayerAsync(playerConnection, "abc123", playerId);

        var connectedRoom = await connectedEvent.WaitAsync(TimeSpan.FromSeconds(5));
        var connectedPlayer = Assert.Single(connectedRoom.Room.Players);
        Assert.Equal(playerId, connectedPlayer.PlayerId);
        Assert.True(connectedPlayer.IsConnected);
        Assert.True((await GetRoomAsync(httpClient, "ABC123")).Players.Single().IsConnected);

        var disconnectedEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await playerConnection.StopAsync();

        var disconnectedRoom = await disconnectedEvent.WaitAsync(TimeSpan.FromSeconds(5));
        var disconnectedPlayer = Assert.Single(disconnectedRoom.Room.Players);
        Assert.Equal(playerId, disconnectedPlayer.PlayerId);
        Assert.False(disconnectedPlayer.IsConnected);

        await using var reconnectedPlayerConnection = CreateHubConnection(application);
        var reconnectedEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await reconnectedPlayerConnection.StartAsync();
        await SubscribeAsPlayerAsync(reconnectedPlayerConnection, "ABC123", playerId);

        var reconnectedRoom = await reconnectedEvent.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(Assert.Single(reconnectedRoom.Room.Players).IsConnected);
    }

    [Fact]
    public async Task SubscribeAsPlayer_WithMultipleConnections_StaysConnectedUntilLastDisconnects()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var playerId = Guid.NewGuid();
        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", playerId, "Alice");

        await using var tvConnection = CreateHubConnection(application);
        await using var firstPlayerConnection = CreateHubConnection(application);
        await using var secondPlayerConnection = CreateHubConnection(application);
        var initialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);

        await tvConnection.StartAsync();
        await SubscribeToRoomAsync(tvConnection, "ABC123");
        await initialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5));

        var firstConnectedEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await firstPlayerConnection.StartAsync();
        await SubscribeAsPlayerAsync(firstPlayerConnection, "ABC123", playerId);
        Assert.True((await firstConnectedEvent.WaitAsync(TimeSpan.FromSeconds(5)))
            .Room.Players.Single().IsConnected);

        var secondInitialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            secondPlayerConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var unexpectedPresenceEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await secondPlayerConnection.StartAsync();
        await SubscribeAsPlayerAsync(secondPlayerConnection, "ABC123", playerId);
        Assert.True((await secondInitialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5)))
            .Room.Players.Single().IsConnected);
        await AssertNoEventAsync(unexpectedPresenceEvent);

        var unexpectedDisconnectEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await firstPlayerConnection.StopAsync();
        await AssertNoEventAsync(unexpectedDisconnectEvent);
        Assert.True((await GetRoomAsync(httpClient, "ABC123")).Players.Single().IsConnected);

        var lastDisconnectEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        await secondPlayerConnection.StopAsync();

        var disconnectedRoom = await lastDisconnectEvent.WaitAsync(TimeSpan.FromSeconds(5));
        var disconnectedPlayer = Assert.Single(disconnectedRoom.Room.Players);
        Assert.Equal(playerId, disconnectedPlayer.PlayerId);
        Assert.False(disconnectedPlayer.IsConnected);
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
        var beforeWithoutPresence = before with
        {
            Players = before.Players
                .Select(player => player with { IsConnected = false })
                .ToArray()
        };
        var afterWithoutPresence = after with
        {
            Players = after.Players
                .Select(player => player with { IsConnected = false })
                .ToArray()
        };

        Assert.Equal(
            JsonSerializer.Serialize(beforeWithoutPresence),
            JsonSerializer.Serialize(afterWithoutPresence));
        Assert.False(before.Players.Single().IsConnected);
        Assert.True(after.Players.Single().IsConnected);
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

    [Fact]
    public async Task PutTargetScore_BroadcastsUpdatedRoomAfterSavingAndDoesNotBroadcastRejectedChanges()
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

        await Task.WhenAll(roomConnection.StartAsync(), otherRoomConnection.StartAsync());
        await SubscribeToRoomAsync(roomConnection, "ABC123");
        await SubscribeToRoomAsync(otherRoomConnection, "OTHER");
        await Task.WhenAll(initialRoomEvent, initialOtherRoomEvent)
            .WaitAsync(TimeSpan.FromSeconds(5));

        var roomUpdated = WaitForEvent<RoomUpdatedEvent>(
            roomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var otherRoomUpdated = WaitForEvent<RoomUpdatedEvent>(
            otherRoomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var response = await httpClient.PutAsJsonAsync(
            "/rooms/abc123/target-score",
            new UpdateRoomTargetScoreRequest(35));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseRoom = await response.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(responseRoom);
        var publicEvent = await roomUpdated.WaitAsync(TimeSpan.FromSeconds(5));
        var persistedRoom = await GetRoomAsync(httpClient, "ABC123");
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(35, persistedRoom.TargetScore);
        Assert.Equal(
            JsonSerializer.Serialize(responseRoom),
            JsonSerializer.Serialize(publicEvent.Room));

        var rejectedUpdate = WaitForEvent<RoomUpdatedEvent>(
            roomConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var saveCountBeforeRejectedUpdate = repository.SaveCount;
        var invalidResponse = await httpClient.PutAsJsonAsync(
            "/rooms/ABC123/target-score",
            new UpdateRoomTargetScoreRequest(20));

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        await Task.WhenAll(
            AssertNoEventAsync(rejectedUpdate),
            AssertNoEventAsync(otherRoomUpdated));
        Assert.Equal(saveCountBeforeRejectedUpdate, repository.SaveCount);
    }

    [Fact]
    public async Task PostRevealStorytellerCard_PublishesRoomUpdatedOnlyAfterSaveCompletes()
    {
        var repository = new CountingRoomRepository();
        using var application = CreateApplication(repository);
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", carlaId, "Carla");
        await StartRoomAsync(httpClient, "ABC123");

        var aliceStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", aliceId);
        var bobStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", bobId);
        var carlaStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", carlaId);
        var storytellerCardId = aliceStarted.Player.Hand.First().CardId;
        var bobCardId = bobStarted.Player.Hand.First().CardId;
        var carlaCardId = carlaStarted.Player.Hand.First().CardId;

        var storytellerResponse = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(aliceId, storytellerCardId, "A clue"));
        storytellerResponse.EnsureSuccessStatusCode();
        var bobSubmission = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(bobId, bobCardId));
        bobSubmission.EnsureSuccessStatusCode();
        var carlaSubmission = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(carlaId, carlaCardId));
        carlaSubmission.EnsureSuccessStatusCode();
        var votingState = await GetRoomAsync(httpClient, "ABC123");
        var storytellerVotingCardId = votingState.VotingCards
            .Single(card => card.CardId == storytellerCardId)
            .VotingCardId;
        var bobVotingCardId = votingState.VotingCards
            .Single(card => card.CardId == bobCardId)
            .VotingCardId;

        var firstVote = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(bobId, storytellerVotingCardId));
        firstVote.EnsureSuccessStatusCode();
        var finalVote = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(carlaId, bobVotingCardId));
        finalVote.EnsureSuccessStatusCode();
        Assert.Equal(RoomPhase.VoteReveal, (await GetRoomAsync(httpClient, "ABC123")).Phase);

        await using var connection = CreateHubConnection(application);
        var initialRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);
        await connection.StartAsync();
        await SubscribeToRoomAsync(connection, "ABC123");
        Assert.Equal(
            RoomPhase.VoteReveal,
            (await initialRoomEvent.WaitAsync(TimeSpan.FromSeconds(5))).Room.Phase);

        var saveCountBeforeReveal = repository.SaveCount;
        var (saveStarted, releaseSave) = repository.BlockNextSave();
        var revealEvent = WaitForEvent<RoomUpdatedEvent>(
            connection,
            RoomRealtimeEventNames.RoomUpdated);
        try
        {
            var revealResponseTask = httpClient.PostAsync(
                "/rooms/ABC123/reveal-storyteller-card",
                content: null);
            await saveStarted.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(revealEvent.IsCompleted);

            releaseSave();
            var revealResponse = await revealResponseTask;
            Assert.Equal(HttpStatusCode.OK, revealResponse.StatusCode);
            var responseState = await revealResponse.Content.ReadFromJsonAsync<RoomStateResponse>();
            Assert.NotNull(responseState);

            var publicEvent = await revealEvent.WaitAsync(TimeSpan.FromSeconds(5));
            var persistedState = await GetRoomAsync(httpClient, "ABC123");
            Assert.Equal(saveCountBeforeReveal + 1, repository.SaveCount);
            Assert.Equal(RoomPhase.Reveal, responseState.Phase);
            Assert.Equal(RoomPhase.Reveal, publicEvent.Room.Phase);
            Assert.Equal(RoomPhase.Reveal, persistedState.Phase);
            Assert.Equal(
                JsonSerializer.Serialize(responseState),
                JsonSerializer.Serialize(publicEvent.Room));
        }
        finally
        {
            releaseSave();
        }
    }

    [Fact]
    public async Task PostStart_BroadcastsPublicStateAndEachPlayersOwnPrivateState()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(httpClient, "ABC123");
        await CreateRoomAsync(httpClient, "OTHER");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", carlaId, "Carla");

        await using var tvConnection = CreateHubConnection(application);
        await using var aliceConnection = CreateHubConnection(application);
        await using var bobConnection = CreateHubConnection(application);
        await using var carlaConnection = CreateHubConnection(application);
        await using var otherRoomConnection = CreateHubConnection(application);

        var tvPublicEvents = TrackEvents<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var alicePrivateEvents = TrackEvents<PlayerStateUpdatedEvent>(
            aliceConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var bobPrivateEvents = TrackEvents<PlayerStateUpdatedEvent>(
            bobConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var carlaPrivateEvents = TrackEvents<PlayerStateUpdatedEvent>(
            carlaConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var otherRoomPublicEvents = TrackEvents<RoomUpdatedEvent>(
            otherRoomConnection,
            RoomRealtimeEventNames.RoomUpdated);

        var initialTvEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var initialAliceEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            aliceConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialBobEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            bobConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialCarlaEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            carlaConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialOtherRoomEvent = WaitForEvent<RoomUpdatedEvent>(
            otherRoomConnection,
            RoomRealtimeEventNames.RoomUpdated);

        await Task.WhenAll(
            tvConnection.StartAsync(),
            aliceConnection.StartAsync(),
            bobConnection.StartAsync(),
            carlaConnection.StartAsync(),
            otherRoomConnection.StartAsync());
        await SubscribeToRoomAsync(tvConnection, "abc123");
        await SubscribeAsPlayerAsync(aliceConnection, "ABC123", aliceId);
        await SubscribeAsPlayerAsync(bobConnection, "abc123", bobId);
        await SubscribeAsPlayerAsync(carlaConnection, "ABC123", carlaId);
        await SubscribeToRoomAsync(otherRoomConnection, "other");
        await Task.WhenAll(
            initialTvEvent,
            initialAliceEvent,
            initialBobEvent,
            initialCarlaEvent,
            initialOtherRoomEvent).WaitAsync(TimeSpan.FromSeconds(5));

        tvPublicEvents.Clear();
        alicePrivateEvents.Clear();
        bobPrivateEvents.Clear();
        carlaPrivateEvents.Clear();
        otherRoomPublicEvents.Clear();

        var roomUpdated = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var aliceUpdated = WaitForEvent<PlayerStateUpdatedEvent>(
            aliceConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var bobUpdated = WaitForEvent<PlayerStateUpdatedEvent>(
            bobConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var carlaUpdated = WaitForEvent<PlayerStateUpdatedEvent>(
            carlaConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        var response = await httpClient.PostAsync("/rooms/abc123/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseRoom = await response.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(responseRoom);

        var publicEvent = await roomUpdated.WaitAsync(TimeSpan.FromSeconds(5));
        var privateEvents = await Task.WhenAll(
                aliceUpdated,
                bobUpdated,
                carlaUpdated)
            .WaitAsync(TimeSpan.FromSeconds(5));
        var expectedPrivateStates = await Task.WhenAll(
            GetPlayerPrivateStateAsync(httpClient, "ABC123", aliceId),
            GetPlayerPrivateStateAsync(httpClient, "ABC123", bobId),
            GetPlayerPrivateStateAsync(httpClient, "ABC123", carlaId));

        Assert.Equal(RoomPhase.StorytellerChoosing, publicEvent.Room.Phase);
        Assert.Equal(aliceId, publicEvent.Room.CurrentStoryteller?.PlayerId);
        Assert.Equal(
            JsonSerializer.Serialize(responseRoom),
            JsonSerializer.Serialize(publicEvent.Room));
        Assert.DoesNotContain(
            "\"hand\"",
            JsonSerializer.Serialize(publicEvent),
            StringComparison.OrdinalIgnoreCase);

        AssertPrivateState(privateEvents[0], expectedPrivateStates[0], true);
        AssertPrivateState(privateEvents[1], expectedPrivateStates[1], false);
        AssertPrivateState(privateEvents[2], expectedPrivateStates[2], false);

        await Task.Delay(TimeSpan.FromMilliseconds(300));

        Assert.Collection(
            alicePrivateEvents,
            playerEvent => Assert.Equal(aliceId, playerEvent.State.Player.PlayerId));
        Assert.Collection(
            bobPrivateEvents,
            playerEvent => Assert.Equal(bobId, playerEvent.State.Player.PlayerId));
        Assert.Collection(
            carlaPrivateEvents,
            playerEvent => Assert.Equal(carlaId, playerEvent.State.Player.PlayerId));
        Assert.Single(tvPublicEvents);
        Assert.Empty(otherRoomPublicEvents);
    }

    [Fact]
    public async Task PostStart_WhenRoomIsAlreadyStarted_DoesNotPublishOrSaveAgain()
    {
        var repository = new CountingRoomRepository();
        using var application = CreateApplication(repository);
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", carlaId, "Carla");
        await using var publicConnection = CreateHubConnection(application);
        await using var privateConnection = CreateHubConnection(application);
        var initialPublicEvent = WaitForEvent<RoomUpdatedEvent>(
            publicConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var initialPrivateEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            privateConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        await Task.WhenAll(
            publicConnection.StartAsync(),
            privateConnection.StartAsync());
        await SubscribeToRoomAsync(publicConnection, "ABC123");
        await SubscribeAsPlayerAsync(privateConnection, "ABC123", aliceId);
        await Task.WhenAll(initialPublicEvent, initialPrivateEvent)
            .WaitAsync(TimeSpan.FromSeconds(5));

        var saveCountBeforeStart = repository.SaveCount;
        var firstPublicEvent = WaitForEvent<RoomUpdatedEvent>(
            publicConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var firstPrivateEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            privateConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        var firstResponse = await httpClient.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        await Task.WhenAll(firstPublicEvent, firstPrivateEvent)
            .WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(saveCountBeforeStart + 1, repository.SaveCount);

        var unexpectedPublicEvent = WaitForEvent<RoomUpdatedEvent>(
            publicConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var unexpectedPrivateEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            privateConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        var secondResponse = await httpClient.PostAsync("/rooms/ABC123/start", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        await Task.WhenAll(
            AssertNoEventAsync(unexpectedPublicEvent),
            AssertNoEventAsync(unexpectedPrivateEvent));
        Assert.Equal(saveCountBeforeStart + 1, repository.SaveCount);
    }

    [Fact]
    public async Task RoundHttpEndpoints_BroadcastPublicStateAndEachPlayersPrivateState()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", carlaId, "Carla");
        await StartRoomAsync(httpClient, "ABC123");

        var aliceStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", aliceId);
        var bobStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", bobId);
        var carlaStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", carlaId);
        var storytellerCardId = aliceStarted.Player.Hand.First().CardId;
        var bobCardId = bobStarted.Player.Hand.First().CardId;
        var carlaCardId = carlaStarted.Player.Hand.First().CardId;

        await using var tvConnection = CreateHubConnection(application);
        await using var aliceConnection = CreateHubConnection(application);
        await using var bobConnection = CreateHubConnection(application);
        await using var carlaConnection = CreateHubConnection(application);

        var initialTvEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var initialAliceEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            aliceConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialBobEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            bobConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialCarlaEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            carlaConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        await Task.WhenAll(
            tvConnection.StartAsync(),
            aliceConnection.StartAsync(),
            bobConnection.StartAsync(),
            carlaConnection.StartAsync());
        await SubscribeToRoomAsync(tvConnection, "abc123");
        await SubscribeAsPlayerAsync(aliceConnection, "ABC123", aliceId);
        await SubscribeAsPlayerAsync(bobConnection, "abc123", bobId);
        await SubscribeAsPlayerAsync(carlaConnection, "ABC123", carlaId);
        await Task.WhenAll(
            initialTvEvent,
            initialAliceEvent,
            initialBobEvent,
            initialCarlaEvent).WaitAsync(TimeSpan.FromSeconds(5));

        var storytellerResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsJsonAsync(
                "/rooms/abc123/storyteller-card",
                new ChooseStorytellerCardRequest(
                    aliceId,
                    storytellerCardId,
                    "A strange doorway")));
        Assert.Equal(RoomPhase.GuessersSubmitting, storytellerResponse.Phase);

        var bobSubmissionResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsJsonAsync(
                "/rooms/ABC123/guess-cards",
                new SubmitGuessCardRequest(bobId, bobCardId)));
        Assert.Equal(RoomPhase.GuessersSubmitting, bobSubmissionResponse.Phase);
        Assert.Equal(1, bobSubmissionResponse.SubmissionCount);

        var votingResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsJsonAsync(
                "/rooms/ABC123/guess-cards",
                new SubmitGuessCardRequest(carlaId, carlaCardId)));
        Assert.Equal(RoomPhase.Voting, votingResponse.Phase);

        var storytellerVotingCardId = votingResponse.VotingCards
            .Single(card => card.CardId == storytellerCardId)
            .VotingCardId;
        var bobVotingCardId = votingResponse.VotingCards
            .Single(card => card.CardId == bobCardId)
            .VotingCardId;

        var firstVoteResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsJsonAsync(
                "/rooms/ABC123/votes",
                new CastVoteRequest(bobId, storytellerVotingCardId)));
        Assert.Equal(RoomPhase.Voting, firstVoteResponse.Phase);
        Assert.Equal(1, firstVoteResponse.VoteCount);
        var votedCard = firstVoteResponse.VotingCards.Single(
            card => card.VotingCardId == storytellerVotingCardId);
        Assert.Collection(
            votedCard.Votes,
            vote =>
            {
                Assert.Equal(bobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });

        var voteRevealResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsJsonAsync(
                "/rooms/ABC123/votes",
                new CastVoteRequest(carlaId, bobVotingCardId)));
        Assert.Equal(RoomPhase.VoteReveal, voteRevealResponse.Phase);

        var revealResponse = await PostAndAssertPublicRealtimeUpdateAsync(
            tvConnection,
            () => httpClient.PostAsync(
                "/rooms/ABC123/reveal-storyteller-card",
                content: null));
        Assert.Equal(RoomPhase.Reveal, revealResponse.Phase);

        var scoringResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsync("/rooms/ABC123/score", content: null));
        Assert.Equal(RoomPhase.Scoring, scoringResponse.Phase);

        var roundCompleteResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsync("/rooms/ABC123/complete-round", content: null));
        Assert.Equal(RoomPhase.RoundComplete, roundCompleteResponse.Phase);

        var nextRoundResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsync("/rooms/ABC123/advance-round", content: null));
        Assert.Equal(RoomPhase.StorytellerChoosing, nextRoundResponse.Phase);
        Assert.Equal(bobId, nextRoundResponse.CurrentStoryteller?.PlayerId);
    }

    [Fact]
    public async Task PostNextRound_BroadcastsFinalPublicStateAndEachPlayersPrivateState()
    {
        using var application = CreateApplication();
        using var httpClient = application.CreateClient();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await CreateRoomAsync(httpClient, "ABC123");
        await JoinPlayerAsync(httpClient, "ABC123", aliceId, "Alice");
        await JoinPlayerAsync(httpClient, "ABC123", bobId, "Bob");
        await JoinPlayerAsync(httpClient, "ABC123", carlaId, "Carla");
        await StartRoomAsync(httpClient, "ABC123");

        var aliceStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", aliceId);
        var bobStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", bobId);
        var carlaStarted = await GetPlayerPrivateStateAsync(httpClient, "ABC123", carlaId);
        var storytellerCardId = aliceStarted.Player.Hand.First().CardId;
        var bobCardId = bobStarted.Player.Hand.First().CardId;
        var carlaCardId = carlaStarted.Player.Hand.First().CardId;

        var storytellerResponse = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/storyteller-card",
            new ChooseStorytellerCardRequest(
                aliceId,
                storytellerCardId,
                "A strange doorway"));
        storytellerResponse.EnsureSuccessStatusCode();

        var bobSubmissionResponse = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(bobId, bobCardId));
        bobSubmissionResponse.EnsureSuccessStatusCode();

        var votingResponseMessage = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/guess-cards",
            new SubmitGuessCardRequest(carlaId, carlaCardId));
        votingResponseMessage.EnsureSuccessStatusCode();
        var votingResponse = await votingResponseMessage.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(votingResponse);

        var storytellerVotingCardId = votingResponse.VotingCards
            .Single(card => card.CardId == storytellerCardId)
            .VotingCardId;
        var bobVotingCardId = votingResponse.VotingCards
            .Single(card => card.CardId == bobCardId)
            .VotingCardId;

        var firstVoteResponse = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(bobId, storytellerVotingCardId));
        firstVoteResponse.EnsureSuccessStatusCode();

        var revealResponse = await httpClient.PostAsJsonAsync(
            "/rooms/ABC123/votes",
            new CastVoteRequest(carlaId, bobVotingCardId));
        revealResponse.EnsureSuccessStatusCode();

        var storytellerRevealResponse = await httpClient.PostAsync(
            "/rooms/ABC123/reveal-storyteller-card",
            content: null);
        storytellerRevealResponse.EnsureSuccessStatusCode();

        var scoringResponse = await httpClient.PostAsync("/rooms/ABC123/score", content: null);
        scoringResponse.EnsureSuccessStatusCode();

        await using var tvConnection = CreateHubConnection(application);
        await using var aliceConnection = CreateHubConnection(application);
        await using var bobConnection = CreateHubConnection(application);
        await using var carlaConnection = CreateHubConnection(application);

        var initialTvEvent = WaitForEvent<RoomUpdatedEvent>(
            tvConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var initialAliceEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            aliceConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialBobEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            bobConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);
        var initialCarlaEvent = WaitForEvent<PlayerStateUpdatedEvent>(
            carlaConnection,
            RoomRealtimeEventNames.PlayerStateUpdated);

        await Task.WhenAll(
            tvConnection.StartAsync(),
            aliceConnection.StartAsync(),
            bobConnection.StartAsync(),
            carlaConnection.StartAsync());
        await SubscribeToRoomAsync(tvConnection, "ABC123");
        await SubscribeAsPlayerAsync(aliceConnection, "ABC123", aliceId);
        await SubscribeAsPlayerAsync(bobConnection, "ABC123", bobId);
        await SubscribeAsPlayerAsync(carlaConnection, "ABC123", carlaId);
        await Task.WhenAll(
            initialTvEvent,
            initialAliceEvent,
            initialBobEvent,
            initialCarlaEvent).WaitAsync(TimeSpan.FromSeconds(5));

        var nextRoundResponse = await PostAndAssertRealtimeUpdateAsync(
            httpClient,
            tvConnection,
            [aliceConnection, bobConnection, carlaConnection],
            () => httpClient.PostAsync("/rooms/ABC123/next-round", content: null));

        Assert.Equal(RoomPhase.StorytellerChoosing, nextRoundResponse.Phase);
        Assert.Equal(bobId, nextRoundResponse.CurrentStoryteller?.PlayerId);
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
            {
                builder.UseSetting("Persistence:Provider", "InMemory");
                builder.ConfigureServices(services =>
                    services.Configure<HubOptions>(
                        options => options.EnableDetailedErrors = true));
            });
    }

    private static WebApplicationFactory<Program> CreateApplication(
        IRoomRepository repository)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Persistence:Provider", "InMemory");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRoomRepository>();
                    services.AddSingleton(repository);
                    services.Configure<HubOptions>(
                        options => options.EnableDetailedErrors = true);
                });
            });
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

    private static ConcurrentQueue<TEvent> TrackEvents<TEvent>(
        HubConnection connection,
        string eventName)
    {
        var events = new ConcurrentQueue<TEvent>();
        connection.On<TEvent>(eventName, events.Enqueue);
        return events;
    }

    private static async Task AssertNoEventAsync(Task eventTask)
    {
        await Assert.ThrowsAsync<TimeoutException>(
            () => eventTask.WaitAsync(TimeSpan.FromMilliseconds(500)));
    }

    private static async Task<RoomStateResponse> PostAndAssertRealtimeUpdateAsync(
        HttpClient httpClient,
        HubConnection publicConnection,
        IReadOnlyList<HubConnection> privateConnections,
        Func<Task<HttpResponseMessage>> postAsync)
    {
        var publicUpdated = WaitForEvent<RoomUpdatedEvent>(
            publicConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var privateUpdates = privateConnections
            .Select(connection => WaitForEvent<PlayerStateUpdatedEvent>(
                connection,
                RoomRealtimeEventNames.PlayerStateUpdated))
            .ToArray();

        var response = await postAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseRoom = await response.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(responseRoom);

        var publicEvent = await publicUpdated.WaitAsync(TimeSpan.FromSeconds(5));
        var playerEvents = await Task.WhenAll(privateUpdates)
            .WaitAsync(TimeSpan.FromSeconds(5));
        var expectedPrivateStates = await Task.WhenAll(
            responseRoom.Players.Select(player => GetPlayerPrivateStateAsync(
                httpClient,
                responseRoom.RoomCode,
                player.PlayerId)));

        Assert.Equal(
            JsonSerializer.Serialize(responseRoom),
            JsonSerializer.Serialize(publicEvent.Room));
        Assert.DoesNotContain(
            "\"hand\"",
            JsonSerializer.Serialize(publicEvent),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            responseRoom.Players.Select(player => player.PlayerId).Order(),
            playerEvents.Select(playerEvent => playerEvent.State.Player.PlayerId).Order());

        foreach (var expectedState in expectedPrivateStates)
        {
            var playerEvent = playerEvents.Single(
                value => value.State.Player.PlayerId == expectedState.Player.PlayerId);
            Assert.Equal(
                JsonSerializer.Serialize(expectedState),
                JsonSerializer.Serialize(playerEvent.State));
        }

        return responseRoom;
    }

    private static async Task<RoomStateResponse> PostAndAssertPublicRealtimeUpdateAsync(
        HubConnection publicConnection,
        Func<Task<HttpResponseMessage>> postAsync)
    {
        var publicUpdated = WaitForEvent<RoomUpdatedEvent>(
            publicConnection,
            RoomRealtimeEventNames.RoomUpdated);
        var response = await postAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseRoom = await response.Content.ReadFromJsonAsync<RoomStateResponse>();
        Assert.NotNull(responseRoom);
        var publicEvent = await publicUpdated.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(
            JsonSerializer.Serialize(responseRoom),
            JsonSerializer.Serialize(publicEvent.Room));

        return responseRoom;
    }

    private static void AssertPrivateState(
        PlayerStateUpdatedEvent playerEvent,
        GetPlayerPrivateStateResponse expectedState,
        bool expectedIsCurrentStoryteller)
    {
        Assert.Equal("ABC123", playerEvent.State.Room.RoomCode);
        Assert.Equal(RoomPhase.StorytellerChoosing, playerEvent.State.Room.Phase);
        Assert.Equal(expectedState.Player.PlayerId, playerEvent.State.Player.PlayerId);
        Assert.Equal(
            expectedState.Player.Hand.Select(card => card.CardId),
            playerEvent.State.Player.Hand.Select(card => card.CardId));
        Assert.Equal(Room.InitialHandSize, playerEvent.State.Player.Hand.Count);
        Assert.Equal(
            expectedIsCurrentStoryteller,
            playerEvent.State.Player.IsCurrentStoryteller);
        Assert.Equal(
            expectedState.Player.HasSubmittedGuessCard,
            playerEvent.State.Player.HasSubmittedGuessCard);
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

    private static async Task<GetPlayerPrivateStateResponse> GetPlayerPrivateStateAsync(
        HttpClient client,
        string roomCode,
        Guid playerId)
    {
        var state = await client.GetFromJsonAsync<GetPlayerPrivateStateResponse>(
            $"/rooms/{roomCode}/players/{playerId}");
        Assert.NotNull(state);
        return state;
    }

    private sealed class CountingRoomRepository : IRoomRepository
    {
        private readonly InMemoryRoomRepository _inner = new();
        private SaveGate? _nextSaveGate;
        private int _saveCount;

        public int SaveCount => Volatile.Read(ref _saveCount);

        public (Task Started, Action Release) BlockNextSave()
        {
            var gate = new SaveGate();
            if (Interlocked.CompareExchange(ref _nextSaveGate, gate, null) is not null)
            {
                throw new InvalidOperationException("A save is already blocked.");
            }

            return (gate.Started, gate.Release);
        }

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
            var saveGate = Interlocked.Exchange(ref _nextSaveGate, null);
            if (saveGate is not null)
            {
                await saveGate.WaitAsync(cancellationToken);
            }

            await _inner.SaveAsync(room, cancellationToken);
            Interlocked.Increment(ref _saveCount);
        }

        private sealed class SaveGate
        {
            private readonly TaskCompletionSource _started = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource _release = new(
                TaskCreationOptions.RunContinuationsAsynchronously);

            public Task Started => _started.Task;

            public void Release() => _release.TrySetResult();

            public async Task WaitAsync(CancellationToken cancellationToken)
            {
                _started.TrySetResult();
                await _release.Task.WaitAsync(cancellationToken);
            }
        }
    }
}
