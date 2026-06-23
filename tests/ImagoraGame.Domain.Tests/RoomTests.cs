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
}
