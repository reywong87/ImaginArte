using ImagoraGame.Application.Rooms;
using ImagoraGame.Domain;
using ImagoraGame.Infrastructure.Persistence;

namespace ImagoraGame.Application.Tests;

public sealed class RoomUseCaseTests
{
    [Fact]
    public async Task CreateRoom_WithValidCode_CreatesWaitingRoom()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new CreateRoomHandler(repository);

        var result = await handler.HandleAsync(new CreateRoomCommand("family-room"));

        Assert.Equal("FAMILY-ROOM", result.Room.RoomCode);
        Assert.Equal(GamePhase.WaitingForPlayers, result.Room.Phase);
        Assert.Empty(result.Room.Players);
    }

    [Fact]
    public async Task CreateRoom_WhenRoomAlreadyExists_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new CreateRoomHandler(repository);

        await handler.HandleAsync(new CreateRoomCommand("ABC123"));

        var exception = await Assert.ThrowsAsync<RoomAlreadyExistsException>(
            () => handler.HandleAsync(new CreateRoomCommand("abc123")));

        Assert.Equal("ABC123", exception.RoomCode);
    }

    [Fact]
    public async Task JoinRoom_WithExistingRoom_AddsPlayerAndReturnsUpdatedState()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var playerId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));

        var result = await joinRoom.HandleAsync(new JoinRoomCommand("abc123", playerId, "  Alice  "));

        Assert.Equal(playerId, result.Player.PlayerId);
        Assert.Equal("Alice", result.Player.Name);
        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Collection(
            result.Room.Players,
            player =>
            {
                Assert.Equal(playerId, player.PlayerId);
                Assert.Equal("Alice", player.Name);
            });
    }

    [Fact]
    public async Task JoinRoom_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new JoinRoomHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new JoinRoomCommand("missing", Guid.NewGuid(), "Alice")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task JoinRoom_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var playerId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", playerId, "Alice"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => joinRoom.HandleAsync(new JoinRoomCommand("ABC123", playerId, "Alice Again")));

        Assert.Equal(DomainErrorCode.PlayerAlreadyJoined, exception.Code);
    }

    [Fact]
    public async Task GetRoomState_WithExistingRoom_ReturnsCurrentSnapshot()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var getRoomState = new GetRoomStateHandler(repository);
        var playerId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", playerId, "Alice"));

        var result = await getRoomState.HandleAsync(new GetRoomStateQuery("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.WaitingForPlayers, result.Room.Phase);
        Assert.Collection(
            result.Room.Players,
            player =>
            {
                Assert.Equal(playerId, player.PlayerId);
                Assert.Equal("Alice", player.Name);
            });
    }

    [Fact]
    public async Task GetRoomState_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new GetRoomStateHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new GetRoomStateQuery("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task StartGame_WithThreePlayers_MovesRoomToStorytellerChoosing()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var aliceId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));

        var result = await startGame.HandleAsync(new StartGameCommand("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, result.Room.Phase);
        Assert.Equal(3, result.Room.Players.Count);
        Assert.NotNull(result.Room.CurrentStoryteller);
        Assert.Equal(aliceId, result.Room.CurrentStoryteller.PlayerId);
        Assert.Equal("Alice", result.Room.CurrentStoryteller.Name);
    }

    [Fact]
    public async Task StartGame_DoesNotExposePrivateHandsInPublicRoomState()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));

        var result = await startGame.HandleAsync(new StartGameCommand("abc123"));

        Assert.DoesNotContain(
            typeof(RoomState).GetProperties(),
            property => property.Name.Contains("Hand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(PlayerState).GetProperties(),
            property => property.Name.Contains("Hand", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(GamePhase.StorytellerChoosing, result.Room.Phase);
    }

    [Fact]
    public async Task StartGame_WithFewerThanThreePlayers_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Bob"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => startGame.HandleAsync(new StartGameCommand("ABC123")));

        Assert.Equal(DomainErrorCode.NotEnoughPlayers, exception.Code);
    }

    [Fact]
    public async Task StartGame_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new StartGameHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new StartGameCommand("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }
}
