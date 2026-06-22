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
        Assert.Null(room.CurrentStorytellerId);
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
    public void StartGame_WithMinimumPlayers_MovesRoomToStorytellerChoosing()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");

        room.StartGame();

        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void StartGame_DealsInitialHandToEachPlayer()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");

        room.StartGame();

        Assert.Equal(Room.InitialHandSize, room.GetHandFor(aliceId).Count);
        Assert.Equal(Room.InitialHandSize, room.GetHandFor(bobId).Count);
        Assert.Equal(Room.InitialHandSize, room.GetHandFor(carlaId).Count);
    }

    [Fact]
    public void StartGame_DealsUniqueCardsAcrossPlayers()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");

        room.StartGame();

        var dealtCardIds = new[]
            {
                room.GetHandFor(aliceId),
                room.GetHandFor(bobId),
                room.GetHandFor(carlaId)
            }
            .SelectMany(hand => hand)
            .Select(card => card.Id)
            .ToArray();

        Assert.Equal(dealtCardIds.Length, dealtCardIds.Distinct().Count());
    }

    [Fact]
    public void Players_BeforeStartGame_DoNotHaveInitialPlayableHands()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");

        Assert.Empty(room.GetHandFor(aliceId));
        Assert.Empty(room.GetHandFor(bobId));
        Assert.Empty(room.GetHandFor(carlaId));
    }

    [Fact]
    public void StartGame_SelectsFirstJoinedPlayerAsStoryteller()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");

        room.StartGame();

        Assert.Equal(aliceId, room.CurrentStorytellerId);
    }

    [Fact]
    public void ChooseStorytellerCard_WithCardInStorytellerHand_MovesRoomToGuessersSubmitting()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();
        var cardId = room.GetHandFor(aliceId).First().Id;

        room.ChooseStorytellerCard(aliceId, cardId, "A strange doorway");

        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
        Assert.Equal(cardId, room.StorytellerCardId);
        Assert.Equal("A strange doorway", room.Clue);
    }

    [Fact]
    public void ChooseStorytellerCard_WhenPlayerIsNotStoryteller_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();
        var bobCardId = room.GetHandFor(bobId).First().Id;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.ChooseStorytellerCard(bobId, bobCardId, "A strange doorway"));

        Assert.Equal(DomainErrorCode.PlayerIsNotCurrentStoryteller, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void ChooseStorytellerCard_WhenRoomIsNotWaitingForStoryteller_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();
        var cardId = room.GetHandFor(aliceId).First().Id;
        room.ChooseStorytellerCard(aliceId, cardId, "A strange doorway");

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.ChooseStorytellerCard(aliceId, cardId, "Another clue"));

        Assert.Equal(DomainErrorCode.RoomNotWaitingForStoryteller, exception.Code);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void ChooseStorytellerCard_WhenCardIsNotInStorytellerHand_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.ChooseStorytellerCard(aliceId, new CardId("missing-card"), "A strange doorway"));

        Assert.Equal(DomainErrorCode.StorytellerCardNotInHand, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
        Assert.Null(room.StorytellerCardId);
        Assert.Null(room.Clue);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ChooseStorytellerCard_WithBlankClue_Throws(string clue)
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();
        var cardId = room.GetHandFor(aliceId).First().Id;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.ChooseStorytellerCard(aliceId, cardId, clue));

        Assert.Equal(DomainErrorCode.ClueIsRequired, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
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
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }
}
