using ImaginArte.Domain;

namespace ImaginArte.Domain.Tests;

public sealed class RoomTests
{
    [Fact]
    public void AvatarId_ContainsOnlyTheCanonicalIds()
    {
        Assert.Equal(
            new[]
            {
                "fox", "owl", "deer", "moth", "turtle", "octopus", "mushroom", "dragon",
                "rabbit", "raven", "hummingbird", "koi", "lynx", "swan", "phoenix", "snail"
            },
            AvatarId.All.Select(avatarId => avatarId.Value));

        foreach (var avatarId in AvatarId.All)
        {
            Assert.True(AvatarId.TryParse(avatarId.Value, out var parsedAvatarId));
            Assert.Equal(avatarId, parsedAvatarId);
        }

        Assert.False(AvatarId.TryParse("unknown", out _));
        Assert.False(AvatarId.TryParse("FOX", out _));
    }

    [Fact]
    public void Create_WithValidRoomCode_CreatesWaitingRoom()
    {
        var room = Room.Create(new RoomCode("family-room"));

        Assert.Equal("FAMILY-ROOM", room.Code.Value);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
        Assert.Equal(25, room.TargetScore);
        Assert.Null(room.CurrentStorytellerId);
        Assert.Empty(room.Players);
        Assert.Empty(room.Scoreboard);
    }

    [Fact]
    public void Create_WithNullRoomCode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Room.Create(null!));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(25)]
    [InlineData(35)]
    public void SetTargetScore_WithAllowedScoreWhileWaiting_UpdatesTargetScore(int targetScore)
    {
        var room = Room.Create(new RoomCode("ABC123"));

        room.SetTargetScore(targetScore);

        Assert.Equal(targetScore, room.TargetScore);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(26)]
    [InlineData(50)]
    public void SetTargetScore_WithUnsupportedScore_ThrowsAndKeepsCurrentTarget(int targetScore)
    {
        var room = Room.Create(new RoomCode("ABC123"));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SetTargetScore(targetScore));

        Assert.Equal(DomainErrorCode.InvalidTargetScore, exception.Code);
        Assert.Equal(25, room.TargetScore);
    }

    [Fact]
    public void SetTargetScore_AfterGameStarts_ThrowsAndKeepsCurrentTarget()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SetTargetScore(15));

        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
        Assert.Equal(25, room.TargetScore);
    }

    [Fact]
    public void SetTargetScore_IsPreservedBySnapshotRestore()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.SetTargetScore(35);

        var restoredRoom = Room.Restore(room.ToSnapshot());

        Assert.Equal(35, restoredRoom.TargetScore);
    }

    [Fact]
    public void GamePhase_VoteRevealIsAppendedWithoutChangingExistingValues()
    {
        Assert.Equal(3, (int)GamePhase.Voting);
        Assert.Equal(4, (int)GamePhase.Reveal);
        Assert.Equal(7, (int)GamePhase.GameComplete);
        Assert.Equal(8, (int)GamePhase.VoteReveal);
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

        var player = room.Join(playerId, "Alice", AvatarId.Fox);

        Assert.Equal(playerId, player.Id);
        Assert.Equal("Alice", player.Name);
        Assert.Equal(AvatarId.Fox, player.AvatarId);
        Assert.Collection(room.Players, joinedPlayer => Assert.Equal(playerId, joinedPlayer.Id));
        Assert.Collection(
            room.Scoreboard,
            score =>
            {
                Assert.Equal(playerId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
    }

    [Fact]
    public void Join_WithExplicitAvatar_PreservesRequestedAvatar()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        var player = room.Join(PlayerId.New(), "Alice", AvatarId.Owl);

        Assert.Equal(AvatarId.Owl, player.AvatarId);
    }

    [Fact]
    public void Join_WithUndefinedAvatar_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.Join(PlayerId.New(), "Alice", default(AvatarId)));

        Assert.Equal(DomainErrorCode.InvalidAvatar, exception.Code);
        Assert.Empty(room.Players);
    }

    [Fact]
    public void Join_WithAvatarAlreadyChosen_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.Join(PlayerId.New(), "Bob", AvatarId.Fox));

        Assert.Equal(DomainErrorCode.AvatarAlreadyTaken, exception.Code);
        Assert.Single(room.Players);
    }

    [Fact]
    public void Restore_WithDuplicatePlayerAvatars_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        var snapshot = room.ToSnapshot();
        var firstAvatarId = snapshot.Players.First().AvatarId;
        var players = snapshot.Players
            .Select((player, index) => index == 1 ? player with { AvatarId = firstAvatarId } : player)
            .ToArray();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => Room.Restore(snapshot with { Players = players }));

        Assert.Equal(DomainErrorCode.AvatarAlreadyTaken, exception.Code);
    }

    [Fact]
    public void AvatarId_IsPreservedBySnapshotRestore()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Phoenix);

        var restoredRoom = Room.Restore(room.ToSnapshot());

        Assert.Equal(AvatarId.Phoenix, Assert.Single(restoredRoom.Players).AvatarId);
    }

    [Fact]
    public void Scoreboard_WhenPlayersJoin_KeepsZeroPointEntriesInJoinOrder()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();

        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);

        Assert.Collection(
            room.Scoreboard,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Join_WithBlankPlayerName_Throws(string playerName)
    {
        var room = Room.Create(new RoomCode("ABC123"));

        Assert.Throws<ArgumentException>(() => room.Join(PlayerId.New(), playerName, AvatarId.Fox));
    }

    [Fact]
    public void Join_WithEmptyPlayerId_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        Assert.Throws<ArgumentException>(() => room.Join(new PlayerId(Guid.Empty), "Alice", AvatarId.Fox));
    }

    [Fact]
    public void Join_WhenRoomHasExactlyMaximumPlayers_IsAllowed()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        for (var i = 1; i <= Room.MaximumPlayers; i++)
        {
            room.Join(PlayerId.New(), $"Player {i}", AvatarId.All[i - 1]);
        }

        Assert.Equal(Room.MaximumPlayers, room.Players.Count);
    }

    [Fact]
    public void Join_WhenRoomHasMaximumPlayers_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));

        for (var i = 1; i <= Room.MaximumPlayers; i++)
        {
            room.Join(PlayerId.New(), $"Player {i}", AvatarId.All[i - 1]);
        }

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(PlayerId.New(), "Player 9", AvatarId.Snail));
        Assert.Equal(DomainErrorCode.RoomIsFull, exception.Code);
        Assert.Contains(Room.MaximumPlayers.ToString(), exception.Message);
    }

    [Fact]
    public void Join_WithSamePlayerIdTwice_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var playerId = PlayerId.New();

        room.Join(playerId, "Alice", AvatarId.Fox);

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(playerId, "Bob", AvatarId.Owl));
        Assert.Equal(DomainErrorCode.PlayerAlreadyJoined, exception.Code);
    }

    [Fact]
    public void Join_AfterGameStarts_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(() => room.Join(PlayerId.New(), "Diego", AvatarId.Phoenix));
        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
    }

    [Fact]
    public void StartGame_WithTooFewPlayers_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);

        var exception = Assert.Throws<DomainRuleViolationException>(room.StartGame);
        Assert.Equal(DomainErrorCode.NotEnoughPlayers, exception.Code);
        Assert.Contains(Room.MinimumPlayers.ToString(), exception.Message);
        Assert.Equal(GamePhase.WaitingForPlayers, room.Phase);
    }

    [Fact]
    public void StartGame_WithMinimumPlayers_MovesRoomToStorytellerChoosing()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);

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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);

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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);

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
    public void StartGame_CreatesExactlyTheIllustratedTemporaryDeck()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);

        room.StartGame();

        var cardIds = room.Players
            .SelectMany(player => room.GetHandFor(player.Id))
            .Concat(room.ToSnapshot().AvailableDeck)
            .Select(card => card.Id.Value)
            .OrderBy(cardId => cardId)
            .ToArray();
        var expectedCardIds = Enumerable
            .Range(1, 98)
            .Select(cardNumber => $"TEMP-{cardNumber:000}")
            .ToArray();

        Assert.Equal(expectedCardIds, cardIds);
    }

    [Fact]
    public void Players_BeforeStartGame_DoNotHaveInitialPlayableHands()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);

        Assert.Empty(room.GetHandFor(aliceId));
        Assert.Empty(room.GetHandFor(bobId));
        Assert.Empty(room.GetHandFor(carlaId));
    }

    [Fact]
    public void StartGame_SelectsFirstJoinedPlayerAsStoryteller()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);

        room.StartGame();

        Assert.Equal(aliceId, room.CurrentStorytellerId);
    }

    [Fact]
    public void ChooseStorytellerCard_WithCardInStorytellerHand_MovesRoomToGuessersSubmitting()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
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
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
        room.StartGame();
        var cardId = room.GetHandFor(aliceId).First().Id;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.ChooseStorytellerCard(aliceId, cardId, clue));

        Assert.Equal(DomainErrorCode.ClueIsRequired, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WithCardInGuesserHand_StoresSubmission()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);

        var submission = Assert.Single(room.GuessCardSubmissions);
        Assert.Equal(bobId, submission.PlayerId);
        Assert.Equal(bobCardId, submission.CardId);
        Assert.Equal(1, room.SubmissionCount);
        Assert.Equal(2, room.ExpectedSubmissionCount);
    }

    [Fact]
    public void SubmitGuessCard_WhenPlayerIsStoryteller_Throws()
    {
        var (room, aliceId, _, _) = CreateRoomWaitingForGuesserSubmissions();
        var aliceCardId = room.GetHandFor(aliceId).First().Id;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SubmitGuessCard(aliceId, aliceCardId));

        Assert.Equal(DomainErrorCode.PlayerIsCurrentStoryteller, exception.Code);
        Assert.Empty(room.GuessCardSubmissions);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WhenRoomIsNotWaitingForGuesserSubmissions_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
        room.StartGame();
        var bobCardId = room.GetHandFor(bobId).First().Id;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SubmitGuessCard(bobId, bobCardId));

        Assert.Equal(DomainErrorCode.RoomNotWaitingForGuessers, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WhenPlayerIsNotInRoom_Throws()
    {
        var (room, _, _, _) = CreateRoomWaitingForGuesserSubmissions();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SubmitGuessCard(PlayerId.New(), new CardId("TEMP-999")));

        Assert.Equal(DomainErrorCode.PlayerNotInRoom, exception.Code);
        Assert.Empty(room.GuessCardSubmissions);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WhenCardIsNotInGuesserHand_Throws()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SubmitGuessCard(bobId, new CardId("missing-card")));

        Assert.Equal(DomainErrorCode.GuessCardNotInHand, exception.Code);
        Assert.Empty(room.GuessCardSubmissions);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WhenSameGuesserSubmitsTwice_Throws()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();
        var bobCards = room.GetHandFor(bobId).ToArray();
        room.SubmitGuessCard(bobId, bobCards[0].Id);

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.SubmitGuessCard(bobId, bobCards[1].Id));

        Assert.Equal(DomainErrorCode.GuessCardAlreadySubmitted, exception.Code);
        Assert.Single(room.GuessCardSubmissions);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void SubmitGuessCard_WhenSomeGuessersStillNeedToSubmit_KeepsRoomInGuessersSubmitting()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);

        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
        Assert.Equal(1, room.SubmissionCount);
        Assert.Equal(2, room.ExpectedSubmissionCount);
    }

    [Fact]
    public void VotingCards_BeforeVoting_IsEmpty()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);

        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
        Assert.Empty(room.VotingCards);
    }

    [Fact]
    public void SubmitGuessCard_WhenAllGuessersSubmit_MovesRoomToVoting()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        Assert.Equal(GamePhase.Voting, room.Phase);
        Assert.Equal(2, room.SubmissionCount);
        Assert.Equal(2, room.ExpectedSubmissionCount);
    }

    [Fact]
    public void VotingCards_WhenAllGuessersSubmit_IncludesStorytellerAndGuesserCards()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions(
            randomNumberGenerator: new ZeroRandomNumberGenerator());
        var storytellerCardId = room.StorytellerCardId
            ?? throw new InvalidOperationException("Test setup requires a storyteller card.");
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        var votingCards = room.VotingCards.ToArray();

        Assert.Equal(3, votingCards.Length);
        Assert.Equal(
            [storytellerCardId.Value, bobCardId.Value, carlaCardId.Value],
            votingCards.Select(card => card.CardId.Value).Order().ToArray());
        Assert.Equal(3, votingCards.Select(card => card.CardId).Distinct().Count());
        Assert.Equal([1, 2, 3], votingCards.Select(card => card.DisplayNumber).Order().ToArray());
        Assert.Equal(
            ["VOTE-001", "VOTE-002", "VOTE-003"],
            votingCards.Select(card => card.VotingCardId.Value).Order().ToArray());
        Assert.All(
            votingCards,
            card => Assert.Equal(
                $"VOTE-{card.DisplayNumber:000}",
                card.VotingCardId.Value));

        var storytellerCard = votingCards.Single(card => card.CardId == storytellerCardId);
        Assert.Equal(3, storytellerCard.DisplayNumber);
        Assert.Equal("VOTE-003", storytellerCard.VotingCardId.Value);
    }

    [Fact]
    public void VotingCards_PublicOptions_DoNotExposeOwnerOrStorytellerFlag()
    {
        var publicPropertyNames = typeof(VotingCard)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Storyteller", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VotingCards_SubmissionOrderDoesNotDeterminePublicPosition()
    {
        var (firstRoom, _, firstBobId, firstCarlaId) = CreateRoomWaitingForGuesserSubmissions(
            randomNumberGenerator: new ZeroRandomNumberGenerator());
        var firstBobCardId = firstRoom.GetHandFor(firstBobId).First().Id;
        var firstCarlaCardId = firstRoom.GetHandFor(firstCarlaId).First().Id;

        firstRoom.SubmitGuessCard(firstBobId, firstBobCardId);
        firstRoom.SubmitGuessCard(firstCarlaId, firstCarlaCardId);

        var (secondRoom, _, secondBobId, secondCarlaId) = CreateRoomWaitingForGuesserSubmissions(
            randomNumberGenerator: new ZeroRandomNumberGenerator());
        var secondBobCardId = secondRoom.GetHandFor(secondBobId).First().Id;
        var secondCarlaCardId = secondRoom.GetHandFor(secondCarlaId).First().Id;

        secondRoom.SubmitGuessCard(secondCarlaId, secondCarlaCardId);
        secondRoom.SubmitGuessCard(secondBobId, secondBobCardId);

        Assert.Equal(
            firstRoom.VotingCards.Select(card => (card.VotingCardId.Value, card.CardId.Value, card.DisplayNumber)),
            secondRoom.VotingCards.Select(card => (card.VotingCardId.Value, card.CardId.Value, card.DisplayNumber)));
    }

    [Fact]
    public void VotingCardOwnerships_WhenVotingCardsAreCreated_RetainRealOwners()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions(
            randomNumberGenerator: new ZeroRandomNumberGenerator());
        var storytellerCardId = room.StorytellerCardId
            ?? throw new InvalidOperationException("Test setup requires a storyteller card.");
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        var ownerships = room.VotingCardOwnerships.ToArray();

        Assert.Equal(3, ownerships.Length);
        AssertOwnership(ownerships, storytellerCardId, aliceId, isStorytellerCard: true);
        AssertOwnership(ownerships, bobCardId, bobId, isStorytellerCard: false);
        AssertOwnership(ownerships, carlaCardId, carlaId, isStorytellerCard: false);
        Assert.Equal(
            room.VotingCards.Select(card => (card.VotingCardId, card.CardId)),
            ownerships.Select(card => (card.VotingCardId, card.CardId)));
    }

    [Fact]
    public void CastVote_WithValidGuesserAndVotingCard_StoresVote()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);

        Assert.Equal(1, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void VotingCards_AfterVote_IncludeVisibleVoterNameOnVotedCard()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);

        var votedCard = room.VotingCards.Single(card => card.VotingCardId == storytellerVotingCardId);
        Assert.Collection(
            votedCard.Votes,
            vote =>
            {
                Assert.Equal(bobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });
        Assert.All(
            room.VotingCards.Where(card => card.VotingCardId != storytellerVotingCardId),
            card => Assert.Empty(card.Votes));
    }

    [Fact]
    public void CastVote_WhenPlayerIsStoryteller_Throws()
    {
        var (room, aliceId, _, _) = CreateRoomWaitingForVoting();
        var votingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(aliceId, votingCardId));

        Assert.Equal(DomainErrorCode.PlayerIsCurrentStoryteller, exception.Code);
        Assert.Equal(0, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenPlayerIsNotInRoom_Throws()
    {
        var (room, _, _, _) = CreateRoomWaitingForVoting();
        var votingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(PlayerId.New(), votingCardId));

        Assert.Equal(DomainErrorCode.PlayerNotInRoom, exception.Code);
        Assert.Equal(0, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenRoomIsNotWaitingForVotes_Throws()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForGuesserSubmissions();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(bobId, new VotingCardId("VOTE-001")));

        Assert.Equal(DomainErrorCode.RoomNotWaitingForVotes, exception.Code);
        Assert.Equal(GamePhase.GuessersSubmitting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenVotingCardDoesNotExist_Throws()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(bobId, new VotingCardId("missing-card")));

        Assert.Equal(DomainErrorCode.VotingCardNotFound, exception.Code);
        Assert.Equal(0, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenGuesserVotesForOwnSubmittedCard_Throws()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(bobId, bobVotingCardId));

        Assert.Equal(DomainErrorCode.PlayerCannotVoteForOwnCard, exception.Code);
        Assert.Equal(0, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenGuesserVotesForStorytellerCard_IsAllowed()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);

        Assert.Equal(1, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenGuesserVotesTwice_Throws()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var carlaVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == carlaId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        var exception = Assert.Throws<DomainRuleViolationException>(
            () => room.CastVote(bobId, carlaVotingCardId));

        Assert.Equal(DomainErrorCode.PlayerAlreadyVoted, exception.Code);
        Assert.Equal(1, room.VoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenSomeGuessersStillNeedToVote_KeepsRoomInVoting()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var votingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, votingCardId);

        Assert.Equal(GamePhase.Voting, room.Phase);
        Assert.Equal(1, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
    }

    [Fact]
    public void RevealedVotingCards_BeforeReveal_IsEmpty()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var votingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, votingCardId);

        Assert.Equal(GamePhase.Voting, room.Phase);
        Assert.Empty(room.RevealedVotingCards);
    }

    [Fact]
    public void CastVote_WhenAllGuessersVote_MovesRoomToVoteReveal()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);

        Assert.Equal(GamePhase.VoteReveal, room.Phase);
        Assert.Equal(2, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
        Assert.Empty(room.RevealedVotingCards);
    }

    [Fact]
    public void VotingCards_InVoteReveal_IncludeVotesWithoutRevealingStorytellerCard()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room.VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);

        Assert.Equal(GamePhase.VoteReveal, room.Phase);
        Assert.Contains(
            room.VotingCards.Single(card => card.VotingCardId == storytellerVotingCardId).Votes,
            vote => vote.VoterPlayerId == bobId && vote.VoterName == "Bob");
        Assert.Contains(
            room.VotingCards.Single(card => card.VotingCardId == bobVotingCardId).Votes,
            vote => vote.VoterPlayerId == carlaId && vote.VoterName == "Carla");
        Assert.Empty(room.RevealedVotingCards);
    }

    [Fact]
    public void RevealStorytellerCard_WhenVotesAreLocked_MovesRoomToReveal()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room.VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);
        room.RevealStorytellerCard();

        Assert.Equal(GamePhase.Reveal, room.Phase);
        Assert.Contains(room.RevealedVotingCards, card => card.IsStorytellerCard);
    }

    [Fact]
    public void RevealStorytellerCard_BeforeAllVotesAreLocked_Throws()
    {
        var (room, _, _, _) = CreateRoomWaitingForVoting();

        var exception = Assert.Throws<DomainRuleViolationException>(room.RevealStorytellerCard);

        Assert.Equal(DomainErrorCode.RoomNotReadyForStorytellerReveal, exception.Code);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void VoteReveal_IsPreservedBySnapshotRestore()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var votingCardIds = room.VotingCardOwnerships
            .ToDictionary(card => card.OwnerPlayerId, card => card.VotingCardId);

        room.CastVote(bobId, votingCardIds[room.CurrentStorytellerId!.Value]);
        room.CastVote(carlaId, votingCardIds[bobId]);

        var restoredRoom = Room.Restore(room.ToSnapshot());

        Assert.Equal(GamePhase.VoteReveal, restoredRoom.Phase);
        Assert.Equal(room.VoteCount, restoredRoom.VoteCount);
        Assert.Equal(
            room.VotingCards
                .SelectMany(card => card.Votes.Select(vote => (
                    VotingCardId: card.VotingCardId.Value,
                    VoterId: vote.VoterPlayerId.Value)))
                .OrderBy(vote => vote.VotingCardId)
                .ThenBy(vote => vote.VoterId),
            restoredRoom.VotingCards
                .SelectMany(card => card.Votes.Select(vote => (
                    VotingCardId: card.VotingCardId.Value,
                    VoterId: vote.VoterPlayerId.Value)))
                .OrderBy(vote => vote.VotingCardId)
                .ThenBy(vote => vote.VoterId));
        Assert.Empty(restoredRoom.RevealedVotingCards);
    }

    [Fact]
    public void RevealedVotingCards_AfterStorytellerCardReveal_IncludesEveryVotableCard()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var votingOrder = room.VotingCards
            .Select(card => (card.VotingCardId, card.CardId, card.DisplayNumber))
            .ToArray();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);

        Assert.Equal(GamePhase.VoteReveal, room.Phase);
        Assert.Empty(room.RevealedVotingCards);

        room.RevealStorytellerCard();

        Assert.Equal(GamePhase.Reveal, room.Phase);
        Assert.Equal(3, room.RevealedVotingCards.Count);
        Assert.Equal(
            votingOrder,
            room.RevealedVotingCards
                .Select(card => (card.VotingCardId, card.CardId, card.DisplayNumber))
                .ToArray());
    }

    [Fact]
    public void RevealedVotingCards_MarksStorytellerCard()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();

        var storytellerCard = room.RevealedVotingCards.Single(card => card.IsStorytellerCard);

        var ownership = room.VotingCardOwnerships.Single(card => card.IsStorytellerCard);

        Assert.Equal(ownership.VotingCardId, storytellerCard.VotingCardId);
        Assert.Equal(
            room.VotingCardOwnerships.ToList().IndexOf(ownership) + 1,
            storytellerCard.DisplayNumber);
    }

    [Fact]
    public void RevealedVotingCards_IncludesRealOwners()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal();

        var revealedCards = room.RevealedVotingCards;

        Assert.Contains(revealedCards, card => card.OwnerPlayerId == aliceId && card.OwnerName == "Alice");
        Assert.Contains(revealedCards, card => card.OwnerPlayerId == bobId && card.OwnerName == "Bob");
        Assert.Contains(revealedCards, card => card.OwnerPlayerId == carlaId && card.OwnerName == "Carla");
    }

    [Fact]
    public void RevealedVotingCards_AssociatesVotesWithVotedCards()
    {
        var (room, _, bobId, carlaId) = CreateRoomAfterReveal();

        var storytellerVotingCardId = room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room.VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;
        var storytellerCard = room.RevealedVotingCards.Single(card => card.VotingCardId == storytellerVotingCardId);
        var bobCard = room.RevealedVotingCards.Single(card => card.VotingCardId == bobVotingCardId);

        Assert.Collection(
            storytellerCard.Votes,
            vote =>
            {
                Assert.Equal(bobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });
        Assert.Collection(
            bobCard.Votes,
            vote =>
            {
                Assert.Equal(carlaId, vote.VoterPlayerId);
                Assert.Equal("Carla", vote.VoterName);
            });
    }

    [Fact]
    public void RevealedVotingCards_WhenCardHasNoVotes_HasEmptyVoteList()
    {
        var (room, _, _, carlaId) = CreateRoomAfterReveal();

        var carlaCard = room.RevealedVotingCards.Single(card => card.OwnerPlayerId == carlaId);

        Assert.Empty(carlaCard.Votes);
    }

    [Fact]
    public void ScoreRound_WhenRoomIsNotInReveal_Throws()
    {
        var (room, _, _, _) = CreateRoomWaitingForVoting();

        var exception = Assert.Throws<DomainRuleViolationException>(room.ScoreRound);

        Assert.Equal(DomainErrorCode.RoomNotReadyForScoring, exception.Code);
        Assert.Empty(room.RoundScores);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void ScoreRound_DuringVoteReveal_Throws()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, storytellerVotingCardId);

        var exception = Assert.Throws<DomainRuleViolationException>(room.ScoreRound);

        Assert.Equal(DomainErrorCode.RoomNotReadyForScoring, exception.Code);
        Assert.Equal(GamePhase.VoteReveal, room.Phase);
        Assert.Empty(room.RoundScores);
    }

    [Fact]
    public void ScoreRound_WithMixedCorrectAndIncorrectVotes_AwardsStorytellerCorrectGuessersAndCardVotes()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal();

        var result = room.ScoreRound();

        Assert.Equal(GamePhase.Scoring, room.Phase);
        Assert.Equal(room.RoundScores, result.PlayerScores);
        AssertPlayerScore(room.RoundScores, aliceId, "Alice", 3);
        AssertPlayerScore(room.RoundScores, bobId, "Bob", 4);
        AssertPlayerScore(room.RoundScores, carlaId, "Carla", 0);
        AssertPlayerScoreboardEntry(room.Scoreboard, aliceId, "Alice", 3);
        AssertPlayerScoreboardEntry(room.Scoreboard, bobId, "Bob", 4);
        AssertPlayerScoreboardEntry(room.Scoreboard, carlaId, "Carla", 0);
        Assert.Empty(room.Winners);
    }

    [Fact]
    public void ScoreRound_WhenAllGuessersFindStorytellerCard_AwardsGuessersTwoWithoutCardVoteBonuses()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterRevealWithAllGuessersCorrect();

        room.ScoreRound();

        Assert.Equal(GamePhase.Scoring, room.Phase);
        AssertPlayerScore(room.RoundScores, aliceId, "Alice", 0);
        AssertPlayerScore(room.RoundScores, bobId, "Bob", 2);
        AssertPlayerScore(room.RoundScores, carlaId, "Carla", 2);
    }

    [Fact]
    public void ScoreRound_WhenNoGuessersFindStorytellerCard_AwardsGuessersTwoAndCardVoteBonuses()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterRevealWithNoGuessersCorrect();

        room.ScoreRound();

        Assert.Equal(GamePhase.Scoring, room.Phase);
        AssertPlayerScore(room.RoundScores, aliceId, "Alice", 0);
        AssertPlayerScore(room.RoundScores, bobId, "Bob", 3);
        AssertPlayerScore(room.RoundScores, carlaId, "Carla", 3);
    }

    [Fact]
    public void ScoreRound_WhenPlayerReachesTarget_MovesRoomToGameComplete()
    {
        var (room, _, bobId, _) = CreateRoomAfterReveal(targetScore: 4);

        room.ScoreRound();

        Assert.Equal(GamePhase.GameComplete, room.Phase);
        var winner = Assert.Single(room.Winners);
        Assert.Equal(bobId, winner.PlayerId);
        Assert.Equal(4, winner.TotalPoints);
    }

    [Fact]
    public void ScoreRound_WhenMultiplePlayersReachTarget_OnlyHighestTotalWins()
    {
        var (room, _, bobId, _) = CreateRoomAfterReveal(targetScore: 3);

        room.ScoreRound();

        Assert.Equal(GamePhase.GameComplete, room.Phase);
        var winner = Assert.Single(room.Winners);
        Assert.Equal(bobId, winner.PlayerId);
        Assert.Equal(4, winner.TotalPoints);
    }

    [Fact]
    public void ScoreRound_WhenFirstPlaceIsTied_ExposesAllWinners()
    {
        var (room, _, bobId, carlaId) = CreateRoomAfterRevealWithAllGuessersCorrect(targetScore: 2);

        room.ScoreRound();

        Assert.Equal(GamePhase.GameComplete, room.Phase);
        Assert.Collection(
            room.Winners,
            winner =>
            {
                Assert.Equal(bobId, winner.PlayerId);
                Assert.Equal("Bob", winner.PlayerName);
                Assert.Equal(2, winner.TotalPoints);
            },
            winner =>
            {
                Assert.Equal(carlaId, winner.PlayerId);
                Assert.Equal("Carla", winner.PlayerName);
                Assert.Equal(2, winner.TotalPoints);
            });
    }

    [Fact]
    public void ScoreRound_WhenGameCompletes_PreservesScoreboardTotals()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal(targetScore: 4);

        room.ScoreRound();

        Assert.Equal(GamePhase.GameComplete, room.Phase);
        AssertPlayerScoreboardEntry(room.Scoreboard, aliceId, "Alice", 3);
        AssertPlayerScoreboardEntry(room.Scoreboard, bobId, "Bob", 4);
        AssertPlayerScoreboardEntry(room.Scoreboard, carlaId, "Carla", 0);
    }

    [Fact]
    public void ScoreRound_WhenCalledTwice_DoesNotDuplicateResults()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal();

        room.ScoreRound();
        var exception = Assert.Throws<DomainRuleViolationException>(room.ScoreRound);

        Assert.Equal(DomainErrorCode.RoomNotReadyForScoring, exception.Code);
        Assert.Equal(GamePhase.Scoring, room.Phase);
        Assert.Equal(3, room.RoundScores.Count);
        AssertPlayerScoreboardEntry(room.Scoreboard, aliceId, "Alice", 3);
        AssertPlayerScoreboardEntry(room.Scoreboard, bobId, "Bob", 4);
        AssertPlayerScoreboardEntry(room.Scoreboard, carlaId, "Carla", 0);
    }

    [Fact]
    public void CompleteRound_FromScoring_MovesRoomToRoundComplete()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();
        room.ScoreRound();

        room.CompleteRound();

        Assert.Equal(GamePhase.RoundComplete, room.Phase);
    }

    [Fact]
    public void CompleteRound_WhenRoomIsNotInScoring_Throws()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();

        var exception = Assert.Throws<DomainRuleViolationException>(room.CompleteRound);

        Assert.Equal(DomainErrorCode.RoomNotInScoring, exception.Code);
        Assert.Equal(GamePhase.Reveal, room.Phase);
    }

    [Fact]
    public void CompleteRound_PreservesScoreboardAndRoundScores()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal();
        room.ScoreRound();
        var roundScores = room.RoundScores.ToArray();
        var scoreboard = room.Scoreboard.ToArray();

        room.CompleteRound();

        Assert.Equal(GamePhase.RoundComplete, room.Phase);
        Assert.Equal(roundScores, room.RoundScores);
        Assert.Equal(scoreboard, room.Scoreboard);
        AssertPlayerScore(room.RoundScores, aliceId, "Alice", 3);
        AssertPlayerScore(room.RoundScores, bobId, "Bob", 4);
        AssertPlayerScore(room.RoundScores, carlaId, "Carla", 0);
        AssertPlayerScoreboardEntry(room.Scoreboard, aliceId, "Alice", 3);
        AssertPlayerScoreboardEntry(room.Scoreboard, bobId, "Bob", 4);
        AssertPlayerScoreboardEntry(room.Scoreboard, carlaId, "Carla", 0);
    }

    [Fact]
    public void CompleteRound_PreservesRevealedVotingCardsForResultsDisplay()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();
        room.ScoreRound();
        var revealedCards = room.RevealedVotingCards.ToArray();

        room.CompleteRound();

        Assert.Equal(GamePhase.RoundComplete, room.Phase);
        Assert.Equal(3, room.RevealedVotingCards.Count);
        Assert.Collection(
            room.RevealedVotingCards,
            card => AssertRevealedVotingCard(card, revealedCards[0]),
            card => AssertRevealedVotingCard(card, revealedCards[1]),
            card => AssertRevealedVotingCard(card, revealedCards[2]));
    }

    [Fact]
    public void AdvanceRound_FromRoundComplete_MovesRoomToStorytellerChoosing()
    {
        var (room, _, _, _) = CreateRoomAfterRoundComplete();

        room.AdvanceRound();

        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void AdvanceRound_RotatesStorytellerToNextJoinedPlayer()
    {
        var (room, _, bobId, _) = CreateRoomAfterRoundComplete();

        room.AdvanceRound();

        Assert.Equal(bobId, room.CurrentStorytellerId);
    }

    [Fact]
    public void AdvanceRound_WhenStorytellerWasLastJoinedPlayer_RotatesToFirstJoinedPlayer()
    {
        var (room, aliceId, _, _) = CreateRoomAfterRoundComplete();
        room.AdvanceRound();
        CompleteCurrentRound(room);
        room.AdvanceRound();
        CompleteCurrentRound(room);

        room.AdvanceRound();

        Assert.Equal(aliceId, room.CurrentStorytellerId);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    [Fact]
    public void AdvanceRound_ClearsPreviousRoundState()
    {
        var (room, _, _, _) = CreateRoomAfterRoundComplete();

        room.AdvanceRound();

        Assert.Null(room.Clue);
        Assert.Null(room.StorytellerCardId);
        Assert.Empty(room.GuessCardSubmissions);
        Assert.Empty(room.VotingCardOwnerships);
        Assert.Empty(room.VotingCards);
        Assert.Empty(room.RevealedVotingCards);
        Assert.Equal(0, room.VoteCount);
        Assert.Empty(room.RoundScores);
    }

    [Fact]
    public void AdvanceRound_PreservesAccumulatedScoreboard()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterRoundComplete();

        room.AdvanceRound();

        AssertPlayerScoreboardEntry(room.Scoreboard, aliceId, "Alice", 0);
        AssertPlayerScoreboardEntry(room.Scoreboard, bobId, "Bob", 2);
        AssertPlayerScoreboardEntry(room.Scoreboard, carlaId, "Carla", 2);
    }

    [Fact]
    public void AdvanceRound_RemovesStorytellerUsedCard()
    {
        var (room, aliceId, _, _) = CreateRoomAfterRoundComplete();
        var storytellerCardId = room.StorytellerCardId
            ?? throw new InvalidOperationException("Test setup requires a storyteller card.");

        room.AdvanceRound();

        Assert.DoesNotContain(room.GetHandFor(aliceId), card => card.Id == storytellerCardId);
    }

    [Fact]
    public void AdvanceRound_RemovesEachGuesserUsedCard()
    {
        var (room, _, bobId, carlaId) = CreateRoomAfterRoundComplete();
        var usedCardsByPlayerId = room.GuessCardSubmissions.ToDictionary(
            submission => submission.PlayerId,
            submission => submission.CardId);

        room.AdvanceRound();

        Assert.DoesNotContain(room.GetHandFor(bobId), card => card.Id == usedCardsByPlayerId[bobId]);
        Assert.DoesNotContain(room.GetHandFor(carlaId), card => card.Id == usedCardsByPlayerId[carlaId]);
    }

    [Fact]
    public void AdvanceRound_PreservesPlayersAndRefillsHandsWithReplacementCards()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterRoundComplete();
        var players = room.Players.ToArray();
        var previousHandsByPlayerId = players.ToDictionary(
            player => player.Id,
            player => room.GetHandFor(player.Id).Select(card => card.Id).ToHashSet());
        var usedCardsByPlayerId = room.GuessCardSubmissions.ToDictionary(
            submission => submission.PlayerId,
            submission => submission.CardId);
        usedCardsByPlayerId.Add(
            room.CurrentStorytellerId!.Value,
            room.StorytellerCardId!.Value);

        room.AdvanceRound();

        Assert.Equal(players, room.Players);
        Assert.Equal(Room.InitialHandSize, room.GetHandFor(aliceId).Count);
        Assert.Equal(Room.InitialHandSize, room.GetHandFor(bobId).Count);
        Assert.Equal(Room.InitialHandSize, room.GetHandFor(carlaId).Count);

        foreach (var player in players)
        {
            var currentCardIds = room.GetHandFor(player.Id).Select(card => card.Id).ToHashSet();
            var retainedCardIds = previousHandsByPlayerId[player.Id]
                .Where(cardId => cardId != usedCardsByPlayerId[player.Id])
                .ToHashSet();

            Assert.Subset(currentCardIds, retainedCardIds);
            Assert.Single(currentCardIds.Except(retainedCardIds));
        }
    }

    [Fact]
    public void AdvanceRound_DoesNotReturnDiscardedCardsToAnyHand()
    {
        var (room, _, _, _) = CreateRoomAfterRoundComplete();
        var discardedCardIds = room.GuessCardSubmissions
            .Select(submission => submission.CardId)
            .Append(room.StorytellerCardId!.Value)
            .ToHashSet();

        room.AdvanceRound();

        var activeCardIds = room.Players
            .SelectMany(player => room.GetHandFor(player.Id))
            .Select(card => card.Id);

        Assert.Empty(activeCardIds.Intersect(discardedCardIds));
    }

    [Fact]
    public void AdvanceRound_LeavesNoDuplicateCardsAcrossPlayerHands()
    {
        var (room, _, _, _) = CreateRoomAfterRoundComplete();

        room.AdvanceRound();

        var activeCardIds = room.Players
            .SelectMany(player => room.GetHandFor(player.Id))
            .Select(card => card.Id)
            .ToArray();

        Assert.Equal(activeCardIds.Length, activeCardIds.Distinct().Count());
    }

    [Fact]
    public void AdvanceRound_WhenRoomIsNotRoundComplete_Throws()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();

        var exception = Assert.Throws<DomainRuleViolationException>(room.AdvanceRound);

        Assert.Equal(DomainErrorCode.RoomNotRoundComplete, exception.Code);
        Assert.Equal(GamePhase.Reveal, room.Phase);
    }

    [Fact]
    public void StartGame_WhenAlreadyStarted_Throws()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        room.Join(PlayerId.New(), "Alice", AvatarId.Fox);
        room.Join(PlayerId.New(), "Bob", AvatarId.Owl);
        room.Join(PlayerId.New(), "Carla", AvatarId.Deer);
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(room.StartGame);
        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomWaitingForGuesserSubmissions(
            int targetScore = Room.DefaultTargetScore,
            IRandomNumberGenerator? randomNumberGenerator = null)
    {
        var room = Room.Create(new RoomCode("ABC123"), targetScore, randomNumberGenerator);
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);
        room.StartGame();
        var storytellerCardId = room.GetHandFor(aliceId).First().Id;
        room.ChooseStorytellerCard(aliceId, storytellerCardId, "A strange doorway");

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomWaitingForVoting(int targetScore = Room.DefaultTargetScore)
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions(targetScore);
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterReveal(int targetScore = Room.DefaultTargetScore)
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting(targetScore);
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);
        room.RevealStorytellerCard();

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterRoundComplete()
    {
        var (room, aliceId, bobId, carlaId) = CreateStartedRoom();

        CompleteCurrentRound(room);

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateStartedRoom()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice", AvatarId.Fox);
        room.Join(bobId, "Bob", AvatarId.Owl);
        room.Join(carlaId, "Carla", AvatarId.Deer);
        room.StartGame();

        return (room, aliceId, bobId, carlaId);
    }

    private static void CompleteCurrentRound(Room room)
    {
        var storytellerId = room.CurrentStorytellerId
            ?? throw new InvalidOperationException("Test setup requires a current storyteller.");
        var storytellerCardId = room.GetHandFor(storytellerId).First().Id;
        room.ChooseStorytellerCard(storytellerId, storytellerCardId, "A strange doorway");

        foreach (var guesser in room.Players.Where(player => player.Id != storytellerId))
        {
            var guessCardId = room.GetHandFor(guesser.Id).First().Id;

            room.SubmitGuessCard(guesser.Id, guessCardId);
        }

        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        foreach (var guesser in room.Players.Where(player => player.Id != storytellerId))
        {
            room.CastVote(guesser.Id, storytellerVotingCardId);
        }

        room.RevealStorytellerCard();
        room.ScoreRound();
        room.CompleteRound();
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterRevealWithAllGuessersCorrect(int targetScore = Room.DefaultTargetScore)
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting(targetScore);
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, storytellerVotingCardId);
        room.RevealStorytellerCard();

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterRevealWithNoGuessersCorrect(int targetScore = Room.DefaultTargetScore)
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting(targetScore);
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobId)
            .VotingCardId;
        var carlaVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == carlaId)
            .VotingCardId;

        room.CastVote(bobId, carlaVotingCardId);
        room.CastVote(carlaId, bobVotingCardId);
        room.RevealStorytellerCard();

        return (room, aliceId, bobId, carlaId);
    }

    private static void AssertPlayerScore(
        IReadOnlyCollection<PlayerRoundScore> scores,
        PlayerId playerId,
        string playerName,
        int pointsAwarded)
    {
        var score = scores.Single(score => score.PlayerId == playerId);

        Assert.Equal(playerName, score.PlayerName);
        Assert.Equal(pointsAwarded, score.PointsAwarded);
    }

    private static void AssertPlayerScoreboardEntry(
        IReadOnlyCollection<PlayerScore> scoreboard,
        PlayerId playerId,
        string playerName,
        int totalPoints)
    {
        var score = scoreboard.Single(score => score.PlayerId == playerId);

        Assert.Equal(playerName, score.PlayerName);
        Assert.Equal(totalPoints, score.TotalPoints);
    }

    private static void AssertRevealedVotingCard(RevealedVotingCard actual, RevealedVotingCard expected)
    {
        Assert.Equal(expected.VotingCardId, actual.VotingCardId);
        Assert.Equal(expected.CardId, actual.CardId);
        Assert.Equal(expected.DisplayNumber, actual.DisplayNumber);
        Assert.Equal(expected.OwnerPlayerId, actual.OwnerPlayerId);
        Assert.Equal(expected.OwnerName, actual.OwnerName);
        Assert.Equal(expected.IsStorytellerCard, actual.IsStorytellerCard);
        Assert.Equal(expected.Votes, actual.Votes);
    }

    private static void AssertOwnership(
        IReadOnlyCollection<VotingCardOwnership> ownerships,
        CardId cardId,
        PlayerId ownerPlayerId,
        bool isStorytellerCard)
    {
        var ownership = ownerships.Single(card => card.CardId == cardId);

        Assert.Equal(ownerPlayerId, ownership.OwnerPlayerId);
        Assert.Equal(isStorytellerCard, ownership.IsStorytellerCard);
    }

    private sealed class ZeroRandomNumberGenerator : IRandomNumberGenerator
    {
        public int Next(int exclusiveUpperBound)
        {
            return 0;
        }
    }
}
