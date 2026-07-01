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
        Assert.Equal(25, result.Room.TargetScore);
        Assert.Empty(result.Room.Players);
        Assert.Empty(result.Room.Winners);
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
        Assert.Collection(
            result.Room.Scoreboard,
            score =>
            {
                Assert.Equal(playerId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
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
    public async Task GetPlayerPrivateState_WithJoinedPlayer_ReturnsPublicRoomAndPrivatePlayerSnapshot()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", bobId, "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));
        await startGame.HandleAsync(new StartGameCommand("ABC123"));

        var result = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("abc123", bobId));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, result.Room.Phase);
        Assert.Equal(3, result.Room.Players.Count);
        Assert.Equal(aliceId, result.Room.CurrentStoryteller?.PlayerId);
        Assert.Equal(bobId, result.Player.PlayerId);
        Assert.Equal("Bob", result.Player.Name);
        Assert.False(result.Player.IsCurrentStoryteller);
        Assert.False(result.Player.HasSubmittedGuessCard);
        Assert.Collection(
            result.Player.Hand,
            card => Assert.Equal("TEMP-007", card.CardId),
            card => Assert.Equal("TEMP-008", card.CardId),
            card => Assert.Equal("TEMP-009", card.CardId),
            card => Assert.Equal("TEMP-010", card.CardId),
            card => Assert.Equal("TEMP-011", card.CardId),
            card => Assert.Equal("TEMP-012", card.CardId));
    }

    [Fact]
    public async Task GetPlayerPrivateState_ForCurrentStoryteller_MarksPlayerAsStoryteller()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var aliceId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));
        await startGame.HandleAsync(new StartGameCommand("ABC123"));

        var result = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", aliceId));

        Assert.True(result.Player.IsCurrentStoryteller);
        Assert.Equal(Room.InitialHandSize, result.Player.Hand.Count);
    }

    [Fact]
    public async Task GetPlayerPrivateState_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new GetPlayerPrivateStateHandler(repository);
        var playerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new GetPlayerPrivateStateQuery("missing", playerId)));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task GetPlayerPrivateState_WhenPlayerIsNotInRoom_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var handler = new GetPlayerPrivateStateHandler(repository);
        var missingPlayerId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Alice"));

        var exception = await Assert.ThrowsAsync<PlayerNotFoundInRoomException>(
            () => handler.HandleAsync(new GetPlayerPrivateStateQuery("abc123", missingPlayerId)));

        Assert.Equal("ABC123", exception.RoomCode);
        Assert.Equal(missingPlayerId, exception.PlayerId);
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

    [Fact]
    public async Task ChooseStorytellerCard_WithValidCommand_UpdatesPhaseAndReturnsRoomStateWithClue()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var chooseStorytellerCard = new ChooseStorytellerCardHandler(repository);
        var aliceId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));
        await startGame.HandleAsync(new StartGameCommand("ABC123"));
        var storytellerState = await getPlayerPrivateState.HandleAsync(
            new GetPlayerPrivateStateQuery("ABC123", aliceId));
        var cardId = storytellerState.Player.Hand.First().CardId;

        var result = await chooseStorytellerCard.HandleAsync(
            new ChooseStorytellerCardCommand("abc123", aliceId, cardId, "  A strange doorway  "));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.GuessersSubmitting, result.Room.Phase);
        Assert.Equal(aliceId, result.Room.CurrentStoryteller?.PlayerId);
        Assert.Equal("A strange doorway", result.Room.Clue);
    }

    [Fact]
    public async Task ChooseStorytellerCard_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new ChooseStorytellerCardHandler(repository);
        var playerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new ChooseStorytellerCardCommand(
                "missing",
                playerId,
                "TEMP-001",
                "A strange doorway")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task ChooseStorytellerCard_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var chooseStorytellerCard = new ChooseStorytellerCardHandler(repository);
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", bobId, "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", Guid.NewGuid(), "Carla"));
        await startGame.HandleAsync(new StartGameCommand("ABC123"));
        var bobState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", bobId));
        var bobCardId = bobState.Player.Hand.First().CardId;

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => chooseStorytellerCard.HandleAsync(new ChooseStorytellerCardCommand(
                "ABC123",
                bobId,
                bobCardId,
                "A strange doorway")));

        Assert.Equal(DomainErrorCode.PlayerIsNotCurrentStoryteller, exception.Code);
    }

    [Fact]
    public async Task SubmitGuessCard_WithValidCommand_UpdatesSubmissionCountAndReturnsCurrentPhase()
    {
        var repository = new InMemoryRoomRepository();
        var submitGuessCard = new SubmitGuessCardHandler(repository);
        var (_, bobId, _) = await CreateRoomWaitingForGuesserSubmissionsAsync(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var bobState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", bobId));
        var bobCardId = bobState.Player.Hand.First().CardId;

        var result = await submitGuessCard.HandleAsync(new SubmitGuessCardCommand("abc123", bobId, bobCardId));
        var bobStateAfterSubmission = await getPlayerPrivateState.HandleAsync(
            new GetPlayerPrivateStateQuery("ABC123", bobId));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.GuessersSubmitting, result.Room.Phase);
        Assert.Equal(1, result.Room.SubmissionCount);
        Assert.Equal(2, result.Room.ExpectedSubmissionCount);
        Assert.Empty(result.Room.VotingCards);
        Assert.Empty(result.Room.RevealedCards);
        Assert.True(bobStateAfterSubmission.Player.HasSubmittedGuessCard);
    }

    [Fact]
    public async Task SubmitGuessCard_WithLastSubmission_ReturnsVotingStateWithPublicVotingCards()
    {
        var repository = new InMemoryRoomRepository();
        var submitGuessCard = new SubmitGuessCardHandler(repository);
        var (_, bobId, carlaId) = await CreateRoomWaitingForGuesserSubmissionsAsync(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var bobState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", bobId));
        var carlaState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", carlaId));
        var bobCardId = bobState.Player.Hand.First().CardId;
        var carlaCardId = carlaState.Player.Hand.First().CardId;

        await submitGuessCard.HandleAsync(new SubmitGuessCardCommand("ABC123", bobId, bobCardId));
        var result = await submitGuessCard.HandleAsync(new SubmitGuessCardCommand("abc123", carlaId, carlaCardId));

        Assert.Equal(GamePhase.Voting, result.Room.Phase);
        var votingCards = result.Room.VotingCards.ToArray();

        Assert.Equal(3, votingCards.Length);
        Assert.Empty(result.Room.RevealedCards);
        Assert.Equal(
            ["TEMP-001", bobCardId, carlaCardId],
            votingCards.Select(card => card.CardId).Order().ToArray());
        Assert.Equal(3, votingCards.Select(card => card.CardId).Distinct().Count());
        Assert.Equal([1, 2, 3], votingCards.Select(card => card.DisplayNumber).Order().ToArray());
        Assert.Equal(
            ["VOTE-001", "VOTE-002", "VOTE-003"],
            votingCards.Select(card => card.VotingCardId).Order().ToArray());
        Assert.All(
            votingCards,
            card => Assert.Equal($"VOTE-{card.DisplayNumber:000}", card.VotingCardId));
    }

    [Fact]
    public void VotingCardState_DoesNotExposeOwnerOrStorytellerFlag()
    {
        var publicPropertyNames = typeof(VotingCardState)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Player", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicPropertyNames, name => name.Contains("Storyteller", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SubmitGuessCard_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new SubmitGuessCardHandler(repository);
        var playerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new SubmitGuessCardCommand("missing", playerId, "TEMP-001")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task SubmitGuessCard_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var submitGuessCard = new SubmitGuessCardHandler(repository);
        var (aliceId, _, _) = await CreateRoomWaitingForGuesserSubmissionsAsync(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var aliceState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", aliceId));
        var aliceCardId = aliceState.Player.Hand.First().CardId;

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => submitGuessCard.HandleAsync(new SubmitGuessCardCommand("ABC123", aliceId, aliceCardId)));

        Assert.Equal(DomainErrorCode.PlayerIsCurrentStoryteller, exception.Code);
    }

    [Fact]
    public async Task CastVote_WithValidCommand_UpdatesVoteCountAndPhase()
    {
        var repository = new InMemoryRoomRepository();
        var castVote = new CastVoteHandler(repository);
        var (_, bobId, carlaId, votingState) = await CreateRoomWaitingForVotingAsync(repository);
        var storytellerVotingCardId = await GetStorytellerVotingCardIdAsync(repository);
        var bobVotingCardId = await GetVotingCardIdForOwnerAsync(repository, bobId);

        var firstVote = await castVote.HandleAsync(new CastVoteCommand("abc123", bobId, storytellerVotingCardId));
        var result = await castVote.HandleAsync(new CastVoteCommand("ABC123", carlaId, bobVotingCardId));

        Assert.Equal(GamePhase.Voting, firstVote.Room.Phase);
        Assert.Equal(1, firstVote.Room.VoteCount);
        Assert.Equal(2, firstVote.Room.ExpectedVoteCount);
        Assert.Empty(firstVote.Room.RevealedCards);
        Assert.Equal(GamePhase.Reveal, result.Room.Phase);
        Assert.Equal(2, result.Room.VoteCount);
        Assert.Equal(2, result.Room.ExpectedVoteCount);
    }

    [Fact]
    public async Task CastVote_WithLastVote_ReturnsRevealStateWithAllRevealedCards()
    {
        var repository = new InMemoryRoomRepository();
        var castVote = new CastVoteHandler(repository);
        var (_, bobId, carlaId, votingState) = await CreateRoomWaitingForVotingAsync(repository);
        var storytellerVotingCardId = await GetStorytellerVotingCardIdAsync(repository);
        var bobVotingCardId = await GetVotingCardIdForOwnerAsync(repository, bobId);

        await castVote.HandleAsync(new CastVoteCommand("ABC123", bobId, storytellerVotingCardId));
        var result = await castVote.HandleAsync(new CastVoteCommand("ABC123", carlaId, bobVotingCardId));

        Assert.Equal(GamePhase.Reveal, result.Room.Phase);
        Assert.Equal(3, result.Room.RevealedCards.Count);
        Assert.Equal(
            votingState.VotingCards.Select(card => (card.VotingCardId, card.CardId, card.DisplayNumber)),
            result.Room.RevealedCards.Select(card => (card.VotingCardId, card.CardId, card.DisplayNumber)));

        var storytellerCard = result.Room.RevealedCards.Single(card => card.IsStorytellerCard);
        Assert.Equal("Alice", storytellerCard.OwnerName);
        Assert.Collection(
            storytellerCard.Votes,
            vote =>
            {
                Assert.Equal(bobId, vote.VoterPlayerId);
                Assert.Equal("Bob", vote.VoterName);
            });

        var bobCard = result.Room.RevealedCards.Single(card => card.OwnerPlayerId == bobId);
        Assert.Equal("Bob", bobCard.OwnerName);
        Assert.False(bobCard.IsStorytellerCard);
        Assert.Collection(
            bobCard.Votes,
            vote =>
            {
                Assert.Equal(carlaId, vote.VoterPlayerId);
                Assert.Equal("Carla", vote.VoterName);
            });

        var carlaCard = result.Room.RevealedCards.Single(card => card.OwnerPlayerId == carlaId);
        Assert.Equal("Carla", carlaCard.OwnerName);
        Assert.False(carlaCard.IsStorytellerCard);
        Assert.Empty(carlaCard.Votes);
    }

    [Fact]
    public async Task CastVote_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new CastVoteHandler(repository);
        var playerId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new CastVoteCommand("missing", playerId, "VOTE-001")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task CastVote_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var castVote = new CastVoteHandler(repository);
        var (aliceId, _, _, votingState) = await CreateRoomWaitingForVotingAsync(repository);
        var votingCardId = votingState.VotingCards.First().VotingCardId;

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => castVote.HandleAsync(new CastVoteCommand("ABC123", aliceId, votingCardId)));

        Assert.Equal(DomainErrorCode.PlayerIsCurrentStoryteller, exception.Code);
    }

    [Fact]
    public async Task CastVote_RoomStateDoesNotExposeVotersOrVotingCardOwners()
    {
        var repository = new InMemoryRoomRepository();
        var castVote = new CastVoteHandler(repository);
        var (_, bobId, _, votingState) = await CreateRoomWaitingForVotingAsync(repository);
        var storytellerVotingCardId = await GetStorytellerVotingCardIdAsync(repository);

        var result = await castVote.HandleAsync(new CastVoteCommand("ABC123", bobId, storytellerVotingCardId));

        Assert.Equal(1, result.Room.VoteCount);
        Assert.Equal(2, result.Room.ExpectedVoteCount);
        Assert.DoesNotContain(
            typeof(RoomState).GetProperties(),
            property => property.Name.Contains("Voter", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(RoomState).GetProperties(),
            property => property.Name.Equals("Votes", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(RoomState).GetProperties(),
            property => property.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(VotingCardState).GetProperties(),
            property => property.Name.Contains("Owner", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(VotingCardState).GetProperties(),
            property => property.Name.Contains("Player", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScoreRound_WithRevealRoom_ReturnsScoringRoomStateWithRoundScores()
    {
        var repository = new InMemoryRoomRepository();
        var scoreRound = new ScoreRoundHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomAfterRevealAsync(repository);

        var result = await scoreRound.HandleAsync(new ScoreRoundCommand("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.Scoring, result.Room.Phase);
        Assert.Equal(25, result.Room.TargetScore);
        Assert.Collection(
            result.Room.RoundScores,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.PointsAwarded);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.PointsAwarded);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.PointsAwarded);
            });
        Assert.Collection(
            result.Room.Scoreboard,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
        Assert.Empty(result.Room.Winners);
    }

    [Fact]
    public async Task ScoreRound_WhenTargetIsReached_ReturnsGameCompleteAndWinners()
    {
        var repository = new InMemoryRoomRepository();
        var scoreRound = new ScoreRoundHandler(repository);
        var (_, bobId, _) = await AddDomainRoomAfterRevealAsync(repository, targetScore: 4);

        var result = await scoreRound.HandleAsync(new ScoreRoundCommand("target"));

        Assert.Equal("TARGET", result.Room.RoomCode);
        Assert.Equal(GamePhase.GameComplete, result.Room.Phase);
        Assert.Equal(4, result.Room.TargetScore);
        var winner = Assert.Single(result.Room.Winners);
        Assert.Equal(bobId, winner.PlayerId);
        Assert.Equal("Bob", winner.PlayerName);
        Assert.Equal(4, winner.TotalPoints);
    }

    [Fact]
    public async Task ScoreRound_WhenTargetIsNotReached_ReturnsNoWinners()
    {
        var repository = new InMemoryRoomRepository();
        var scoreRound = new ScoreRoundHandler(repository);
        await AddDomainRoomAfterRevealAsync(repository, targetScore: 25);

        var result = await scoreRound.HandleAsync(new ScoreRoundCommand("target"));

        Assert.Equal(GamePhase.Scoring, result.Room.Phase);
        Assert.Empty(result.Room.Winners);
    }

    [Fact]
    public async Task ScoreRound_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new ScoreRoundHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new ScoreRoundCommand("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task ScoreRound_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var scoreRound = new ScoreRoundHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => scoreRound.HandleAsync(new ScoreRoundCommand("ABC123")));

        Assert.Equal(DomainErrorCode.RoomNotReadyForScoring, exception.Code);
    }

    [Fact]
    public async Task CompleteRound_WithScoringRoom_ReturnsRoundCompleteRoomState()
    {
        var repository = new InMemoryRoomRepository();
        var completeRound = new CompleteRoundHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomAfterScoringAsync(repository);

        var result = await completeRound.HandleAsync(new CompleteRoundCommand("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.RoundComplete, result.Room.Phase);
        Assert.Equal(3, result.Room.RevealedCards.Count);
        Assert.Collection(
            result.Room.RoundScores,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.PointsAwarded);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.PointsAwarded);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.PointsAwarded);
            });
        Assert.Collection(
            result.Room.Scoreboard,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
    }

    [Fact]
    public async Task CompleteRound_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new CompleteRoundHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new CompleteRoundCommand("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task CompleteRound_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var completeRound = new CompleteRoundHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => completeRound.HandleAsync(new CompleteRoundCommand("ABC123")));

        Assert.Equal(DomainErrorCode.RoomNotInScoring, exception.Code);
    }

    [Fact]
    public async Task AdvanceRound_WithRoundCompleteRoom_ReturnsStorytellerChoosingStateWithNextStoryteller()
    {
        var repository = new InMemoryRoomRepository();
        var advanceRound = new AdvanceRoundHandler(repository);
        var (_, bobId, _) = await CreateRoomAfterRoundCompleteAsync(repository);

        var result = await advanceRound.HandleAsync(new AdvanceRoundCommand("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, result.Room.Phase);
        Assert.Equal(bobId, result.Room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Bob", result.Room.CurrentStoryteller?.Name);
        Assert.DoesNotContain(
            typeof(RoomState).GetProperties(),
            property => property.Name.Contains("Hand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            typeof(PlayerState).GetProperties(),
            property => property.Name.Contains("Hand", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdvanceRound_ReturnedRoomStateDoesNotExposePreviousRoundState()
    {
        var repository = new InMemoryRoomRepository();
        var advanceRound = new AdvanceRoundHandler(repository);
        await CreateRoomAfterRoundCompleteAsync(repository);

        var result = await advanceRound.HandleAsync(new AdvanceRoundCommand("ABC123"));

        Assert.Null(result.Room.Clue);
        Assert.Equal(0, result.Room.SubmissionCount);
        Assert.Equal(2, result.Room.ExpectedSubmissionCount);
        Assert.Equal(0, result.Room.VoteCount);
        Assert.Equal(2, result.Room.ExpectedVoteCount);
        Assert.Empty(result.Room.VotingCards);
        Assert.Empty(result.Room.RevealedCards);
        Assert.Empty(result.Room.RoundScores);
    }

    [Fact]
    public async Task AdvanceRound_PreservesAccumulatedScoreboardTotals()
    {
        var repository = new InMemoryRoomRepository();
        var advanceRound = new AdvanceRoundHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomAfterRoundCompleteAsync(repository);

        var result = await advanceRound.HandleAsync(new AdvanceRoundCommand("ABC123"));

        Assert.Collection(
            result.Room.Scoreboard,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
    }

    [Fact]
    public async Task AdvanceRound_PrivateSnapshotsContainRefilledHandsWithoutUsedCards()
    {
        var repository = new InMemoryRoomRepository();
        var advanceRound = new AdvanceRoundHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomAfterRoundCompleteAsync(repository);
        var roomBeforeAdvance = await GetRequiredRoomAsync(repository);
        var usedCardIdsByPlayerId = roomBeforeAdvance.GuessCardSubmissions.ToDictionary(
            submission => submission.PlayerId.Value,
            submission => submission.CardId.Value);
        usedCardIdsByPlayerId.Add(
            roomBeforeAdvance.CurrentStorytellerId!.Value.Value,
            roomBeforeAdvance.StorytellerCardId!.Value.Value);

        await advanceRound.HandleAsync(new AdvanceRoundCommand("ABC123"));

        foreach (var playerId in new[] { aliceId, bobId, carlaId })
        {
            var snapshot = await getPlayerPrivateState.HandleAsync(
                new GetPlayerPrivateStateQuery("ABC123", playerId));

            Assert.Equal(Room.InitialHandSize, snapshot.Player.Hand.Count);
            Assert.DoesNotContain(
                snapshot.Player.Hand,
                card => card.CardId == usedCardIdsByPlayerId[playerId]);
        }
    }

    [Fact]
    public async Task AdvanceRound_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new AdvanceRoundHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new AdvanceRoundCommand("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task AdvanceRound_WhenDomainRuleIsViolated_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var advanceRound = new AdvanceRoundHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => advanceRound.HandleAsync(new AdvanceRoundCommand("ABC123")));

        Assert.Equal(DomainErrorCode.RoomNotRoundComplete, exception.Code);
    }

    [Fact]
    public async Task StartNextRound_WithScoringRoom_ReturnsNextRoundState()
    {
        var repository = new InMemoryRoomRepository();
        var startNextRound = new StartNextRoundHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomAfterScoringAsync(repository);

        var result = await startNextRound.HandleAsync(new StartNextRoundCommand("abc123"));

        Assert.Equal("ABC123", result.Room.RoomCode);
        Assert.Equal(GamePhase.StorytellerChoosing, result.Room.Phase);
        Assert.Equal(bobId, result.Room.CurrentStoryteller?.PlayerId);
        Assert.Equal("Bob", result.Room.CurrentStoryteller?.Name);
        Assert.Null(result.Room.Clue);
        Assert.Equal(0, result.Room.SubmissionCount);
        Assert.Equal(2, result.Room.ExpectedSubmissionCount);
        Assert.Equal(0, result.Room.VoteCount);
        Assert.Equal(2, result.Room.ExpectedVoteCount);
        Assert.Empty(result.Room.VotingCards);
        Assert.Empty(result.Room.RevealedCards);
        Assert.Empty(result.Room.RoundScores);
        Assert.Collection(
            result.Room.Scoreboard,
            score =>
            {
                Assert.Equal(aliceId, score.PlayerId);
                Assert.Equal("Alice", score.PlayerName);
                Assert.Equal(3, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(bobId, score.PlayerId);
                Assert.Equal("Bob", score.PlayerName);
                Assert.Equal(4, score.TotalPoints);
            },
            score =>
            {
                Assert.Equal(carlaId, score.PlayerId);
                Assert.Equal("Carla", score.PlayerName);
                Assert.Equal(0, score.TotalPoints);
            });
    }

    [Fact]
    public async Task StartNextRound_WhenRoomDoesNotExist_Throws()
    {
        var repository = new InMemoryRoomRepository();
        var handler = new StartNextRoundHandler(repository);

        var exception = await Assert.ThrowsAsync<RoomNotFoundException>(
            () => handler.HandleAsync(new StartNextRoundCommand("missing")));

        Assert.Equal("MISSING", exception.RoomCode);
    }

    [Fact]
    public async Task StartNextRound_WhenRoomIsNotScoring_PropagatesDomainException()
    {
        var repository = new InMemoryRoomRepository();
        var createRoom = new CreateRoomHandler(repository);
        var startNextRound = new StartNextRoundHandler(repository);

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));

        var exception = await Assert.ThrowsAsync<DomainRuleViolationException>(
            () => startNextRound.HandleAsync(new StartNextRoundCommand("ABC123")));

        Assert.Equal(DomainErrorCode.RoomNotInScoring, exception.Code);
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId)> CreateRoomWaitingForGuesserSubmissionsAsync(
        InMemoryRoomRepository repository)
    {
        var createRoom = new CreateRoomHandler(repository);
        var joinRoom = new JoinRoomHandler(repository);
        var startGame = new StartGameHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var chooseStorytellerCard = new ChooseStorytellerCardHandler(repository);
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();

        await createRoom.HandleAsync(new CreateRoomCommand("ABC123"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", aliceId, "Alice"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", bobId, "Bob"));
        await joinRoom.HandleAsync(new JoinRoomCommand("ABC123", carlaId, "Carla"));
        await startGame.HandleAsync(new StartGameCommand("ABC123"));
        var storytellerState = await getPlayerPrivateState.HandleAsync(
            new GetPlayerPrivateStateQuery("ABC123", aliceId));
        var storytellerCardId = storytellerState.Player.Hand.First().CardId;
        await chooseStorytellerCard.HandleAsync(new ChooseStorytellerCardCommand(
            "ABC123",
            aliceId,
            storytellerCardId,
            "A strange doorway"));

        return (aliceId, bobId, carlaId);
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId, RoomState VotingState)>
        CreateRoomWaitingForVotingAsync(InMemoryRoomRepository repository)
    {
        var submitGuessCard = new SubmitGuessCardHandler(repository);
        var getPlayerPrivateState = new GetPlayerPrivateStateHandler(repository);
        var (aliceId, bobId, carlaId) = await CreateRoomWaitingForGuesserSubmissionsAsync(repository);
        var bobState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", bobId));
        var carlaState = await getPlayerPrivateState.HandleAsync(new GetPlayerPrivateStateQuery("ABC123", carlaId));
        var bobCardId = bobState.Player.Hand.First().CardId;
        var carlaCardId = carlaState.Player.Hand.First().CardId;

        await submitGuessCard.HandleAsync(new SubmitGuessCardCommand("ABC123", bobId, bobCardId));
        var votingState = await submitGuessCard.HandleAsync(new SubmitGuessCardCommand("ABC123", carlaId, carlaCardId));

        return (aliceId, bobId, carlaId, votingState.Room);
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId)> CreateRoomAfterRevealAsync(
        InMemoryRoomRepository repository)
    {
        var castVote = new CastVoteHandler(repository);
        var (aliceId, bobId, carlaId, votingState) = await CreateRoomWaitingForVotingAsync(repository);
        var storytellerVotingCardId = await GetStorytellerVotingCardIdAsync(repository);
        var bobVotingCardId = await GetVotingCardIdForOwnerAsync(repository, bobId);

        await castVote.HandleAsync(new CastVoteCommand("ABC123", bobId, storytellerVotingCardId));
        await castVote.HandleAsync(new CastVoteCommand("ABC123", carlaId, bobVotingCardId));

        return (aliceId, bobId, carlaId);
    }

    private static async Task<string> GetStorytellerVotingCardIdAsync(InMemoryRoomRepository repository)
    {
        var room = await GetRequiredRoomAsync(repository);

        return room.VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId
            .Value;
    }

    private static async Task<string> GetVotingCardIdForOwnerAsync(
        InMemoryRoomRepository repository,
        Guid ownerPlayerId)
    {
        var room = await GetRequiredRoomAsync(repository);

        return room.VotingCardOwnerships
            .Single(card => card.OwnerPlayerId.Value == ownerPlayerId)
            .VotingCardId
            .Value;
    }

    private static async Task<Room> GetRequiredRoomAsync(InMemoryRoomRepository repository)
    {
        return await repository.GetByCodeAsync(new RoomCode("ABC123"))
            ?? throw new InvalidOperationException("Test setup requires room ABC123.");
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId)> CreateRoomAfterScoringAsync(
        InMemoryRoomRepository repository)
    {
        var scoreRound = new ScoreRoundHandler(repository);
        var playerIds = await CreateRoomAfterRevealAsync(repository);

        await scoreRound.HandleAsync(new ScoreRoundCommand("ABC123"));

        return playerIds;
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId)> CreateRoomAfterRoundCompleteAsync(
        InMemoryRoomRepository repository)
    {
        var completeRound = new CompleteRoundHandler(repository);
        var playerIds = await CreateRoomAfterScoringAsync(repository);

        await completeRound.HandleAsync(new CompleteRoundCommand("ABC123"));

        return playerIds;
    }

    private static async Task<(Guid AliceId, Guid BobId, Guid CarlaId)> AddDomainRoomAfterRevealAsync(
        InMemoryRoomRepository repository,
        int targetScore)
    {
        var room = Room.Create(new RoomCode("TARGET"), targetScore);
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var carlaId = Guid.NewGuid();
        var alicePlayerId = new PlayerId(aliceId);
        var bobPlayerId = new PlayerId(bobId);
        var carlaPlayerId = new PlayerId(carlaId);

        room.Join(alicePlayerId, "Alice");
        room.Join(bobPlayerId, "Bob");
        room.Join(carlaPlayerId, "Carla");
        room.StartGame();
        room.ChooseStorytellerCard(alicePlayerId, room.GetHandFor(alicePlayerId).First().Id, "A strange doorway");
        room.SubmitGuessCard(bobPlayerId, room.GetHandFor(bobPlayerId).First().Id);
        room.SubmitGuessCard(carlaPlayerId, room.GetHandFor(carlaPlayerId).First().Id);

        var storytellerVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.IsStorytellerCard)
            .VotingCardId;
        var bobVotingCardId = room
            .VotingCardOwnerships
            .Single(card => card.OwnerPlayerId == bobPlayerId)
            .VotingCardId;

        room.CastVote(bobPlayerId, storytellerVotingCardId);
        room.CastVote(carlaPlayerId, bobVotingCardId);

        await repository.AddAsync(room);

        return (aliceId, bobId, carlaId);
    }
}
