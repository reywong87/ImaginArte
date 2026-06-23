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
        Assert.Empty(room.Scoreboard);
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
    public void Scoreboard_WhenPlayersJoin_KeepsZeroPointEntriesInJoinOrder()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();

        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");

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
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(PlayerId.New(), "Carla");
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
        var (room, _, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions();
        var storytellerCardId = room.StorytellerCardId;
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        Assert.Collection(
            room.VotingCards,
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-001"), card.VotingCardId);
                Assert.Equal(storytellerCardId, card.CardId);
                Assert.Equal(1, card.DisplayNumber);
            },
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-002"), card.VotingCardId);
                Assert.Equal(bobCardId, card.CardId);
                Assert.Equal(2, card.DisplayNumber);
            },
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-003"), card.VotingCardId);
                Assert.Equal(carlaCardId, card.CardId);
                Assert.Equal(3, card.DisplayNumber);
            });
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
    public void VotingCards_PublicIdentifiers_AreStable()
    {
        var (room, _, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(carlaId, carlaCardId);
        room.SubmitGuessCard(bobId, bobCardId);

        Assert.Collection(
            room.VotingCards,
            card => Assert.Equal("VOTE-001", card.VotingCardId.Value),
            card =>
            {
                Assert.Equal("VOTE-002", card.VotingCardId.Value);
                Assert.Equal(bobCardId, card.CardId);
            },
            card =>
            {
                Assert.Equal("VOTE-003", card.VotingCardId.Value);
                Assert.Equal(carlaCardId, card.CardId);
            });
    }

    [Fact]
    public void VotingCardOwnerships_WhenVotingCardsAreCreated_RetainRealOwners()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions();
        var storytellerCardId = room.StorytellerCardId;
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        Assert.Collection(
            room.VotingCardOwnerships,
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-001"), card.VotingCardId);
                Assert.Equal(storytellerCardId, card.CardId);
                Assert.Equal(aliceId, card.OwnerPlayerId);
                Assert.True(card.IsStorytellerCard);
            },
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-002"), card.VotingCardId);
                Assert.Equal(bobCardId, card.CardId);
                Assert.Equal(bobId, card.OwnerPlayerId);
                Assert.False(card.IsStorytellerCard);
            },
            card =>
            {
                Assert.Equal(new VotingCardId("VOTE-003"), card.VotingCardId);
                Assert.Equal(carlaCardId, card.CardId);
                Assert.Equal(carlaId, card.OwnerPlayerId);
                Assert.False(card.IsStorytellerCard);
            });
    }

    [Fact]
    public void CastVote_WithValidGuesserAndVotingCard_StoresVote()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room.VotingCards.First().VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);

        Assert.Equal(1, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
        Assert.Equal(GamePhase.Voting, room.Phase);
    }

    [Fact]
    public void CastVote_WhenPlayerIsStoryteller_Throws()
    {
        var (room, aliceId, _, _) = CreateRoomWaitingForVoting();
        var votingCardId = room.VotingCards.First().VotingCardId;

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
        var votingCardId = room.VotingCards.First().VotingCardId;

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
        var votingCardId = room.VotingCards.First().VotingCardId;

        room.CastVote(bobId, votingCardId);

        Assert.Equal(GamePhase.Voting, room.Phase);
        Assert.Equal(1, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
    }

    [Fact]
    public void RevealedVotingCards_BeforeReveal_IsEmpty()
    {
        var (room, _, bobId, _) = CreateRoomWaitingForVoting();
        var votingCardId = room.VotingCards.First().VotingCardId;

        room.CastVote(bobId, votingCardId);

        Assert.Equal(GamePhase.Voting, room.Phase);
        Assert.Empty(room.RevealedVotingCards);
    }

    [Fact]
    public void CastVote_WhenAllGuessersVote_MovesRoomToReveal()
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

        Assert.Equal(GamePhase.Reveal, room.Phase);
        Assert.Equal(2, room.VoteCount);
        Assert.Equal(2, room.ExpectedVoteCount);
    }

    [Fact]
    public void RevealedVotingCards_WhenAllGuessersVote_IncludesEveryVotableCard()
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

        Assert.Equal(GamePhase.Reveal, room.Phase);
        Assert.Equal(3, room.RevealedVotingCards.Count);
    }

    [Fact]
    public void RevealedVotingCards_MarksStorytellerCard()
    {
        var (room, _, _, _) = CreateRoomAfterReveal();

        var storytellerCard = room.RevealedVotingCards.Single(card => card.IsStorytellerCard);

        Assert.Equal(new VotingCardId("VOTE-001"), storytellerCard.VotingCardId);
        Assert.Equal(1, storytellerCard.DisplayNumber);
    }

    [Fact]
    public void RevealedVotingCards_IncludesRealOwners()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterReveal();

        Assert.Collection(
            room.RevealedVotingCards,
            card =>
            {
                Assert.Equal(aliceId, card.OwnerPlayerId);
                Assert.Equal("Alice", card.OwnerName);
            },
            card =>
            {
                Assert.Equal(bobId, card.OwnerPlayerId);
                Assert.Equal("Bob", card.OwnerName);
            },
            card =>
            {
                Assert.Equal(carlaId, card.OwnerPlayerId);
                Assert.Equal("Carla", card.OwnerName);
            });
    }

    [Fact]
    public void RevealedVotingCards_AssociatesVotesWithVotedCards()
    {
        var (room, _, bobId, carlaId) = CreateRoomAfterReveal();

        var storytellerCard = room.RevealedVotingCards.Single(card => card.VotingCardId == new VotingCardId("VOTE-001"));
        var bobCard = room.RevealedVotingCards.Single(card => card.VotingCardId == new VotingCardId("VOTE-002"));

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
    public void AdvanceRound_PreservesPlayersAndHands()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomAfterRoundComplete();
        var players = room.Players.ToArray();
        var aliceHand = room.GetHandFor(aliceId).ToArray();
        var bobHand = room.GetHandFor(bobId).ToArray();
        var carlaHand = room.GetHandFor(carlaId).ToArray();

        room.AdvanceRound();

        Assert.Equal(players, room.Players);
        Assert.Equal(aliceHand, room.GetHandFor(aliceId));
        Assert.Equal(bobHand, room.GetHandFor(bobId));
        Assert.Equal(carlaHand, room.GetHandFor(carlaId));
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
        room.Join(PlayerId.New(), "Alice");
        room.Join(PlayerId.New(), "Bob");
        room.Join(PlayerId.New(), "Carla");
        room.StartGame();

        var exception = Assert.Throws<DomainRuleViolationException>(room.StartGame);
        Assert.Equal(DomainErrorCode.RoomNotWaitingForPlayers, exception.Code);
        Assert.Equal(GamePhase.StorytellerChoosing, room.Phase);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomWaitingForGuesserSubmissions()
    {
        var room = Room.Create(new RoomCode("ABC123"));
        var aliceId = PlayerId.New();
        var bobId = PlayerId.New();
        var carlaId = PlayerId.New();
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");
        room.StartGame();
        var storytellerCardId = room.GetHandFor(aliceId).First().Id;
        room.ChooseStorytellerCard(aliceId, storytellerCardId, "A strange doorway");

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomWaitingForVoting()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForGuesserSubmissions();
        var bobCardId = room.GetHandFor(bobId).First().Id;
        var carlaCardId = room.GetHandFor(carlaId).First().Id;

        room.SubmitGuessCard(bobId, bobCardId);
        room.SubmitGuessCard(carlaId, carlaCardId);

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterReveal()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting();
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
        room.Join(aliceId, "Alice");
        room.Join(bobId, "Bob");
        room.Join(carlaId, "Carla");
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

        room.ScoreRound();
        room.CompleteRound();
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterRevealWithAllGuessersCorrect()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting();
        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;

        room.CastVote(bobId, storytellerVotingCardId);
        room.CastVote(carlaId, storytellerVotingCardId);

        return (room, aliceId, bobId, carlaId);
    }

    private static (Room Room, PlayerId AliceId, PlayerId BobId, PlayerId CarlaId)
        CreateRoomAfterRevealWithNoGuessersCorrect()
    {
        var (room, aliceId, bobId, carlaId) = CreateRoomWaitingForVoting();
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
}
