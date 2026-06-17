using ImagoraGame.Domain;

namespace ImagoraGame.Domain.Tests;

public sealed class RoomTests
{
    [Fact]
    public void Create_WithValidRoomCode_CreatesWaitingRoom()
    {
        var room = Room.Create(new RoomCode("family-room"));

        Assert.Equal("FAMILY-ROOM", room.Code.Value);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
        Assert.Empty(room.Players);
    }

    [Fact]
    public void Create_WithNullRoomCode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Room.Create(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RoomCode_WithBlankValue_Throws(string code)
    {
        Assert.Throws<ArgumentException>(() => new RoomCode(code));
    }

    [Fact]
    public void Join_WithValidPlayer_AddsPlayerToRoom()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var playerId = PlayerId.New();

        var player = room.Join(playerId, "Alice");

        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
        Assert.Collection(room.Players, joinedPlayer => Assert.Equal(playerId, joinedPlayer.Id));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Join_WithBlankPlayerName_Throws(string playerName)
    {
        var room = Room.Create(new RoomCode("ABC123"));

        Assert.Throws<ArgumentException>(() => room.Join(PlayerId.New(), playerName));
    }

    [Fact]
    public void Join_WithEmptyPlayerId_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        Assert.Throws<ArgumentException>(() => room.Join(new PlayerId(Guid.Empty), "Alice"));
    }

    [Fact]
    public void Join_WhenRoomHasExactlyMaximumPlayers_IsAllowed()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        for (var i = 1; i <= Room.MaximumPlayers; i++)
        {
            room.Join(PlayerId.New(), $"Player {i}");
        }

        Assert.Equal(Room.MaximumPlayers, room.Players.Count);
    }

    [Fact]
    public void Join_WhenRoomHasMaximumPlayers_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        for (var i = 1; i <= Room.MaximumPlayers; i++)
        {
            room.Join(PlayerId.New(), $"Player {i}");
        }

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(PlayerId.New(), "Player 9"));
        Assert.Equal(DomainErrorCode.RoomIsFull, exception.Code);
        Assert.Contains(Room.MaximumPlayers.ToString(), exception.Message);
    }

    [Fact]
    public void Join_WithSamePlayerIdTwice_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var playerId = PlayerId.New();

        room.Join(playerId, "Alice");

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(playerId, "Bob"));
        Assert.Equal(DomainErrorCode.PlayerAlreadyJoined, exception.Code);
    }

    [Fact]
    public void Join_AfterGameStarts_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(PlayerId.New(), "Diego"));
        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
    }

    [Fact]
    public void StartGame_WithTooFewPlayers_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");

        var exception = Assert.Throws<DomainRuleViolationException>(room.StartGame);
        Assert.Equal(DomainErrorCode.NotEnoughPlayers, exception.Code);
        Assert.Contains(Room.MinimumPlayers.ToString(), exception.Message);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
    }

    [Fact]
    public void StartGame_WithMinimumPlayers_MovesRoomToInProgress()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");

        room.StartGame();

        Assert.Equal(GamePhase.InProgress, room.Phase);
    }

    [Fact]
    public void StartGame_WhenAlreadyStarted_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(room.StartGame);
        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
        Assert.Equal(GamePhase.InProgress, room.Phase);
    }
}
